using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class ShooterNet : NetworkBehaviour
    {
        public VehicleRoot vehicleRoot;

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

        private readonly HashSet<int> _processedShots = new HashSet<int>();
        private readonly Dictionary<int, Projectile> _predictedProjectiles = new Dictionary<int, Projectile>();

        private enum ShotHudStatus : byte
        {
            Miss = 0,
            Penetrated = 1,
            NotPenetrated = 2
        }

        private struct ResolvedShot
        {
            public Vector3 Point;
            public Vector3 Normal;
            public bool TargetIsRobot;
            public float Damage;
            public Health TargetHealth;
            public VehicleRoot TargetRoot;
            public ShotHudStatus HudStatus;
        }

        public void PredictAndRequest()
        {
            if (!vehicleRoot.IsOwner)
            {
                return;
            }

            Vector3 startPos = muzzleTransform.position;
            Vector3 aimPoint = vehicleRoot.weaponAimAtCamera.CurrentAimPoint;
            int shotId = ++_shotSeq;

            Projectile predicted = SpawnLocal(startPos, aimPoint, 0f, authoritative: false, Vector3.up);
            _predictedProjectiles[shotId] = predicted;

            if (!IsSpawned)
            {
                return;
            }

            FireRequestServerRpc(shotId, startPos, aimPoint, base.TimeManager.Tick);
        }

        private Projectile SpawnLocal(
            Vector3 startPos,
            Vector3 aimPoint,
            float passedTime,
            bool authoritative,
            Vector3 impactNormal,
            System.Action onAuthoritativeImpact = null)
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
            proj.ConfigureResolvedImpact(aimPoint, impactNormal, onAuthoritativeImpact);
            return proj;
        }

        [ServerRpc(RequireOwnership = true)]
        private void FireRequestServerRpc(int shotId, Vector3 startPos, Vector3 aimPoint, uint clientTick, NetworkConnection sender = null)
        {
            if (!IsServerInitialized || sender == null)
            {
                return;
            }

            if (sender != base.Owner)
            {
                return;
            }

            if (_processedShots.Contains(shotId))
            {
                return;
            }
            _processedShots.Add(shotId);

            float passed = (float)base.TimeManager.TimePassed(clientTick, allowNegative: false);
            passed = Mathf.Min(MAX_PASSED_TIME * 0.5f, passed);

            ResolvedShot shot = ResolveShot(startPos, aimPoint);
            ConfigureOwnerProjectileTargetRpc(sender, shotId, shot.Point, shot.Normal);

            SpawnLocal(
                startPos,
                shot.Point,
                passed,
                authoritative: true,
                shot.Normal,
                onAuthoritativeImpact: () => ApplyResolvedShotDamage(sender, shot)
            );

            FireObserversRpc(shotId, startPos, shot.Point, shot.Normal, clientTick);
        }

        private ResolvedShot ResolveShot(Vector3 startPos, Vector3 aimPoint)
        {
            ServerHitResolver.HitResult hr = ServerHitResolver.ResolveShot(
                startPos,
                aimPoint,
                hitMask,
                shellPenetrationMm,
                normalizationDeg
            );

            Health targetHealth = null;
            VehicleRoot targetRoot = null;

            if (hr.hit && hr.collider != null)
            {
                targetHealth = hr.collider.GetComponentInParent<Health>();
                if (targetHealth != null)
                {
                    targetRoot = targetHealth.GetComponentInParent<VehicleRoot>();
                }
            }

            bool targetIsRobot = targetHealth != null && targetRoot != null;
            ShotHudStatus status = ShotHudStatus.Miss;
            if (targetIsRobot)
            {
                if (hr.penetrated && hr.damage > 0f)
                {
                    status = ShotHudStatus.Penetrated;
                }
                else
                {
                    status = ShotHudStatus.NotPenetrated;
                }
            }

            ResolvedShot shot = new ResolvedShot
            {
                Point = hr.hit ? hr.point : aimPoint,
                Normal = hr.hit && hr.normal.sqrMagnitude > 1e-6f ? hr.normal : Vector3.up,
                TargetIsRobot = targetIsRobot,
                Damage = targetIsRobot ? hr.damage : 0f,
                TargetHealth = targetHealth,
                TargetRoot = targetRoot,
                HudStatus = status
            };

            return shot;
        }

        private void ApplyResolvedShotDamage(NetworkConnection shooterConnection, ResolvedShot shot)
        {
            if (!IsServerInitialized)
            {
                return;
            }

            if (shot.TargetIsRobot && shot.TargetHealth != null && shot.Damage > 0f)
            {
                bool wasDead = shot.TargetHealth.IsDead;
                bool willKill = !wasDead && shot.TargetHealth.Current > 0f && shot.TargetHealth.Current - shot.Damage <= 0f;

                if (GameplaySpawner.In != null)
                {
                    GameplaySpawner.In.RecordHitStats(vehicleRoot, shot.TargetRoot, shot.Damage, willKill);
                }

                shot.TargetHealth.ServerApplyDamage(shot.Damage);
            }

            ShowShotResultTargetRpc(shooterConnection, (byte)shot.HudStatus);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void FireObserversRpc(int shotId, Vector3 startPos, Vector3 aimPoint, Vector3 impactNormal, uint clientTick)
        {
            if (IsOwner)
            {
                return;
            }

            float passed = (float)base.TimeManager.TimePassed(clientTick, allowNegative: false);
            passed = Mathf.Min(MAX_PASSED_TIME, passed);

            SpawnLocal(startPos, aimPoint, passed, authoritative: false, impactNormal);
        }

        [TargetRpc]
        private void ConfigureOwnerProjectileTargetRpc(NetworkConnection conn, int shotId, Vector3 impactPoint, Vector3 impactNormal)
        {
            if (!_predictedProjectiles.TryGetValue(shotId, out Projectile projectile))
            {
                return;
            }

            if (projectile == null)
            {
                _predictedProjectiles.Remove(shotId);
                return;
            }

            projectile.ConfigureResolvedImpact(impactPoint, impactNormal);
        }

        [TargetRpc]
        private void ShowShotResultTargetRpc(NetworkConnection conn, byte status)
        {
            if (GameplayGUI.In == null)
            {
                return;
            }

            if (status == (byte)ShotHudStatus.Penetrated)
            {
                GameplayGUI.In.ShowShotResult("Penetrated");
            }
            else if (status == (byte)ShotHudStatus.NotPenetrated)
            {
                GameplayGUI.In.ShowShotResult("No penetration");
            }
            else
            {
                GameplayGUI.In.ShowShotResult("Miss");
            }
        }
    }
}
