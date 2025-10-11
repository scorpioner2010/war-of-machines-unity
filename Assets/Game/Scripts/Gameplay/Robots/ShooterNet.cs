using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class ShooterNet : NetworkBehaviour
    {
        public TankRoot tankRoot;

        public Projectile projectilePrefab;
        public Transform muzzleTransform;

        public float projectileSpeed = 70f;
        public float projectileLifeTime = 8f;

        public bool projectileUseArc = true;
        public float projectileArcScale = 0.02f;
        public float projectileArcMin = 0f;
        public float projectileArcMax = 50f;
        public float projectileArcExponent = 1f;
        public AnimationCurve projectileArcCurve;
        public bool projectileArcAlongWorldUp = true;

        public bool projectileUseSlowdown = true;
        [Range(0f, 1f)] public float projectileSlowdownAmount = 0.5f;
        public float projectileSlowdownExponent = 1f;
        public AnimationCurve projectileSlowdownCurve;
        [Range(0f, 1f)] public float projectileMinSpeedMultiplier = 0.1f;

        public LayerMask hitMask = ~0;
        public float shellPenetrationMm = 200f;
        public float normalizationDeg = 0f;

        private const float MAX_PASSED_TIME = 0.30f;
        private int _shotSeq;

        // дедуплікація пострілів на сервері
        private readonly HashSet<int> _processedShots = new HashSet<int>();

        public void PredictAndRequest()
        {
            if (!tankRoot.IsOwner)
            {
                return;
            }

            Vector3 startPos = muzzleTransform.position;
            Vector3 aimPoint = tankRoot.weaponAimAtCamera.CurrentAimPoint;
            int shotId = ++_shotSeq;

            // 1) миттєвий візуал на клієнті
            SpawnLocal(startPos, aimPoint, 0f, authoritative: false);

            // 2) завжди надсилаємо запит на сервер (жодних умов за sendRequest)
            if (!IsSpawned)
            {
                Debug.LogWarning("[ShooterNet] PredictAndRequest called before IsSpawned");
                return;
            }

            FireRequestServerRpc(shotId, startPos, aimPoint, base.TimeManager.Tick);
        }

        private void SpawnLocal(Vector3 startPos, Vector3 aimPoint, float passedTime, bool authoritative)
        {
            Projectile proj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            proj.Init(
                targetPoint: aimPoint,
                initialSpeed: projectileSpeed,
                lifeTime: projectileLifeTime,
                useArc: projectileUseArc,
                arcScale: projectileArcScale,
                arcMin: projectileArcMin,
                arcMax: projectileArcMax,
                arcExponent: projectileArcExponent,
                arcCurve: projectileArcCurve,
                arcAlongWorldUp: projectileArcAlongWorldUp,
                useSlowdown: projectileUseSlowdown,
                slowdownAmount: projectileSlowdownAmount,
                slowdownExponent: projectileSlowdownExponent,
                slowdownCurve: projectileSlowdownCurve,
                minSpeedMultiplier: projectileMinSpeedMultiplier,
                passedTime: passedTime,
                authoritative: authoritative
            );
        }

        [ServerRpc(RequireOwnership = true)]
        private void FireRequestServerRpc(int shotId, Vector3 startPos, Vector3 aimPoint, uint clientTick, NetworkConnection sender = null)
        {
            if (!IsServer || sender == null)
            {
                return;
            }

            // перевірка власника
            if (sender != base.Owner)
            {
                Debug.LogWarning($"[ShooterNet] Reject shot: sender {sender.ClientId} is not owner {base.Owner?.ClientId}");
                return;
            }

            // дедуп
            if (_processedShots.Contains(shotId))
            {
                return;
            }
            _processedShots.Add(shotId);

            // компенсація затримки
            float passed = (float)base.TimeManager.TimePassed(clientTick, allowNegative: false);
            passed = Mathf.Min(MAX_PASSED_TIME * 0.5f, passed);

            // авторитетний снаряд + хіт
            SpawnLocal(startPos, aimPoint, passed, authoritative: true);
            ResolveAndBroadcastHit(shotId, startPos, aimPoint);

            // повідомити спостерігачів (власнику не треба)
            FireObserversRpc(shotId, startPos, aimPoint, clientTick);
        }

        private void ResolveAndBroadcastHit(int shotId, Vector3 startPos, Vector3 aimPoint)
        {
            var hr = ServerHitResolver.ResolveShot(
                startPos,
                aimPoint,
                hitMask,
                shellPenetrationMm,
                normalizationDeg
            );

            int targetId = 0;
            NetworkObject targetNetObj = null;
            Health targetHealth = null;

            if (hr.hit && hr.collider != null)
            {
                targetNetObj = hr.collider.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null)
                {
                    targetId = targetNetObj.ObjectId;
                    targetHealth = targetNetObj.GetComponent<Health>();
                }
            }

            if (hr.hit && hr.damage > 0f)
            {
                if (targetHealth != null)
                {
                    targetHealth.ServerApplyDamage(hr.damage);
                }
                else
                {
                    string goName = hr.collider ? hr.collider.gameObject.name : "null";
                    string path = hr.collider ? GetPath(hr.collider.transform) : "null";
                    int oid = targetNetObj != null ? targetNetObj.ObjectId : 0;
                    Debug.LogWarning($"[ShooterNet] shotId={shotId} hit but NO Health found. colliderGO={goName} path={path} netObjId={oid}");
                }
            }

            if (hr.hit)
            {
                string tgtGo = hr.collider != null ? hr.collider.gameObject.name : "null";
                string tgtRoot = "n/a";
                var armor = hr.collider != null ? hr.collider.GetComponentInParent<ArmorMap>() : null;
                if (armor != null && armor.tankRoot != null)
                {
                    tgtRoot = armor.tankRoot.name;
                }

                Debug.Log($"[ShooterNet] shotId={shotId} hit={hr.hit} pen={hr.penetrated} dmg={hr.damage} targetGO={tgtGo} targetTank={tgtRoot} netObjId={targetId}");
            }
            else
            {
                Debug.Log($"[ShooterNet] shotId={shotId} MISS");
            }

            BroadcastHitObserversRpc(
                shotId,
                hr.hit,
                hr.point,
                hr.normal,
                targetId,
                hr.penetrated,
                hr.damage
            );
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
            {
                return "null";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(t.name);
            Transform p = t.parent;
            while (p != null)
            {
                sb.Insert(0, p.name + "/");
                p = p.parent;
            }
            return sb.ToString();
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void FireObserversRpc(int shotId, Vector3 startPos, Vector3 aimPoint, uint clientTick)
        {
            if (IsOwner)
            {
                return;
            }

            float passed = (float)base.TimeManager.TimePassed(clientTick, allowNegative: false);
            passed = Mathf.Min(MAX_PASSED_TIME, passed);

            SpawnLocal(startPos, aimPoint, passed, authoritative: false);
        }

        [ObserversRpc(BufferLast = false)]
        private void BroadcastHitObserversRpc(
            int shotId,
            bool hit,
            Vector3 point,
            Vector3 normal,
            int targetObjectId,
            bool penetrated,
            float damage)
        {
            // локальні VFX/хіт-маркери — за потреби
        }
    }
}
