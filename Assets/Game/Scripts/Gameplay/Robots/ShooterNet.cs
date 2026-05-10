using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class ShooterNet : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable, IVehicleStatsConsumer
    {
        public VehicleRoot vehicleRoot;

        public Projectile projectilePrefab;
        public Transform muzzleTransform;

        public GunDispersionSettings dispersion = new GunDispersionSettings();

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
        public float shellDamage = 100f;
        public float shellPenetrationMm = 200f;
        public float normalizationDeg = 0f;

        private const float MAX_PASSED_TIME = 0.30f;

        private int _shotSeq;
        private float _nextServerDispersionSyncTime;
        private GunCrosshair _crosshair;
        private bool _ownerDispersionInitialized;
        private bool _serverDispersionInitialized;

        private readonly HashSet<int> _processedShots = new HashSet<int>();
        private readonly Dictionary<int, Projectile> _predictedProjectiles = new Dictionary<int, Projectile>();
        private readonly GunDispersionModel _ownerDispersion = new GunDispersionModel();
        private readonly GunDispersionModel _serverDispersion = new GunDispersionModel();
        private readonly SyncVar<float> _serverDispersionDeg = new();

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                return;
            }

            if (stats.Damage > 0f)
            {
                shellDamage = stats.Damage;
            }

            if (stats.Penetration > 0f)
            {
                shellPenetrationMm = stats.Penetration;
            }

            if (stats.Accuracy > 0f)
            {
                dispersion.minDispersionDeg = stats.Accuracy;
            }

            if (stats.AimTime > 0f)
            {
                dispersion.aimTime = stats.AimTime;
            }
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsServer)
            {
                InitServerDispersion();
            }

            if (context.IsOwner && !context.IsMenu)
            {
                InitOwnerDispersion();
            }
        }

        private void Update()
        {
            if (IsServerInitialized)
            {
                if (!_serverDispersionInitialized)
                {
                    InitServerDispersion();
                }

                GunDispersionGlobalSettings globalDispersion = GetGlobalDispersion();
                float serverDeg = _serverDispersion.Tick(vehicleRoot, dispersion, globalDispersion, Time.deltaTime, includeCameraAimMotion: false);
                SyncServerDispersion(serverDeg, globalDispersion, force: false);
            }

            if (IsOwner && vehicleRoot != null && !vehicleRoot.IsMenu)
            {
                if (!_ownerDispersionInitialized)
                {
                    InitOwnerDispersion();
                }

                _ownerDispersion.Tick(vehicleRoot, dispersion, GetGlobalDispersion(), Time.deltaTime, includeCameraAimMotion: true);
                ApplyCrosshairDispersion();
            }
        }

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
            Vector3 baseAimPoint = vehicleRoot.weaponAimAtCamera.CurrentAimPoint;
            int shotId = ++_shotSeq;
            if (!_ownerDispersionInitialized)
            {
                InitOwnerDispersion();
            }

            Vector3 predictedAimPoint = GetDispersedAimPoint(startPos, baseAimPoint, shotId, _ownerDispersion.CurrentDeg, GetGlobalDispersion());

            Projectile predicted = SpawnLocal(startPos, predictedAimPoint, 0f, authoritative: false, Vector3.up);
            _predictedProjectiles[shotId] = predicted;
            AddOwnerShotBloom();

            if (!IsSpawned)
            {
                return;
            }

            FireRequestServerRpc(shotId, startPos, baseAimPoint, base.TimeManager.Tick);
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

            if (!_serverDispersionInitialized)
            {
                InitServerDispersion();
            }

            GunDispersionGlobalSettings globalDispersion = GetGlobalDispersion();
            Vector3 dispersedAimPoint = GetDispersedAimPoint(startPos, aimPoint, shotId, _serverDispersion.CurrentDeg, globalDispersion);
            ResolvedShot shot = ResolveShot(startPos, dispersedAimPoint);
            AddServerShotBloom();
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

        private void InitOwnerDispersion()
        {
            _ownerDispersion.Reset(vehicleRoot, dispersion);
            _crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            _ownerDispersionInitialized = true;
            ApplyCrosshairDispersion();
        }

        private void InitServerDispersion()
        {
            _serverDispersion.Reset(vehicleRoot, dispersion);
            _serverDispersionInitialized = true;
            SyncServerDispersion(_serverDispersion.CurrentDeg, GetGlobalDispersion(), force: true);
        }

        private void AddOwnerShotBloom()
        {
            if (!_ownerDispersionInitialized)
            {
                InitOwnerDispersion();
            }

            _ownerDispersion.AddShotBloom(dispersion, GetGlobalDispersion());
            ApplyCrosshairDispersion();
        }

        private void AddServerShotBloom()
        {
            if (!_serverDispersionInitialized)
            {
                InitServerDispersion();
            }

            GunDispersionGlobalSettings globalDispersion = GetGlobalDispersion();
            _serverDispersion.AddShotBloom(dispersion, globalDispersion);
            SyncServerDispersion(_serverDispersion.CurrentDeg, globalDispersion, force: true);
        }

        private void SyncServerDispersion(float value, GunDispersionGlobalSettings globalDispersion, bool force)
        {
            if (!IsServerInitialized)
            {
                return;
            }

            globalDispersion ??= GunDispersionGlobalSettings.Default;
            if (!force && Time.time < _nextServerDispersionSyncTime && Mathf.Abs(_serverDispersionDeg.Value - value) < globalDispersion.serverSyncDeadZoneDeg)
            {
                return;
            }

            _serverDispersionDeg.Value = value;
            _nextServerDispersionSyncTime = Time.time + Mathf.Max(0.001f, globalDispersion.serverSyncInterval);
        }

        private void ApplyCrosshairDispersion()
        {
            if (_crosshair == null)
            {
                _crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            }

            if (_crosshair == null || dispersion == null)
            {
                return;
            }

            GunDispersionGlobalSettings globalDispersion = GetGlobalDispersion();
            float localDiameter = globalDispersion.GetUiDiameter(_ownerDispersion.CurrentDeg, dispersion.MinDispersion);
            float serverDeg = _serverDispersionDeg.Value > 0f ? _serverDispersionDeg.Value : _ownerDispersion.CurrentDeg;
            float serverDiameter = globalDispersion.GetUiDiameter(serverDeg, dispersion.MinDispersion);
            _crosshair.SetAimingDiameters(localDiameter, serverDiameter);
        }

        private GunDispersionGlobalSettings GetGlobalDispersion()
        {
            if (IsServerInitialized)
            {
                return ServerSettings.GetGunDispersion();
            }

            return RemoteServerSettings.GunDispersion;
        }

        private Vector3 GetDispersedAimPoint(Vector3 startPos, Vector3 aimPoint, int shotId, float dispersionDeg, GunDispersionGlobalSettings globalDispersion)
        {
            globalDispersion ??= GunDispersionGlobalSettings.Default;
            if (dispersion == null || !globalDispersion.enabled || dispersionDeg <= 0f)
            {
                return aimPoint;
            }

            Vector3 direction = aimPoint - startPos;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return aimPoint;
            }

            direction /= distance;
            Vector2 unitCircle = GetDeterministicUnitCircle(shotId);
            float spreadRadius = Mathf.Tan(dispersionDeg * Mathf.Deg2Rad) * distance;

            Vector3 right = Vector3.Cross(Vector3.up, direction);
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(Vector3.forward, direction);
            }
            right.Normalize();

            Vector3 up = Vector3.Cross(direction, right).normalized;
            return aimPoint + (right * unitCircle.x + up * unitCircle.y) * spreadRadius;
        }

        private Vector2 GetDeterministicUnitCircle(int shotId)
        {
            unchecked
            {
                int objectId = NetworkObject != null ? NetworkObject.ObjectId : 0;
                int seed = shotId * 73856093 ^ objectId * 19349663;
                System.Random random = new System.Random(seed);

                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble());
                return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
        }

        private ResolvedShot ResolveShot(Vector3 startPos, Vector3 aimPoint)
        {
            ServerHitResolver.HitResult hr = ServerHitResolver.ResolveShot(
                startPos,
                aimPoint,
                hitMask,
                shellPenetrationMm,
                normalizationDeg,
                shellDamage
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
