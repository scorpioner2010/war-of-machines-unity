using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Services;
using Game.Scripts.Server;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class NetworkWeaponShooter : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable, IVehicleStatsConsumer
    {
        public static event System.Action<Vector3, Vector3> AuthoritativeProjectileHit;

        public VehicleRoot vehicleRoot;

        public Projectile projectilePrefab;
        public Transform muzzleTransform;

        public GunDispersionSettings dispersion = new GunDispersionSettings();

        public float projectileSpeed = VehicleRuntimeStats.DefaultShellSpeed;
        public float projectileLifeTime = 8f;
        public float maxShotDistance = 2000f;
        [Tooltip("Fallback value only. Runtime shots use ServerSettings projectile ballistics.")]
        [Min(0f)] public float projectileGravity = 6f;
        [Min(0f)] public float projectileCollisionRadius = 0.05f;
        [Tooltip("Fallback value only. Runtime shots use ServerSettings projectile ballistics.")]
        public bool useBallisticCompensation = true;
        [Tooltip("Fallback value only. Runtime shots use ServerSettings projectile ballistics.")]
        public bool preferHighArc;
        [Tooltip("Fallback value only. Runtime shots use ServerSettings projectile ballistics.")]
        public bool debugBallisticTrajectory;

        [Tooltip("Legacy artificial arc flag; ignored by ballistic projectile simulation. Use projectileGravity instead.")]
        public bool projectileUseArc = true;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public float projectileArcScale = 0.02f;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public float projectileArcMin = 0f;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public float projectileArcMax = 50f;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public float projectileArcExponent = 1f;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public AnimationCurve projectileArcCurve;
        [Tooltip("Legacy artificial arc field; ignored by ballistic projectile simulation.")]
        public bool projectileArcAlongWorldUp = true;

        [Tooltip("Legacy slowdown field; ignored by ballistic projectile simulation.")]
        public bool projectileUseSlowdown = true;
        [Tooltip("Legacy slowdown field; ignored by ballistic projectile simulation.")]
        [Range(0f, 1f)] public float projectileSlowdownAmount = 0.5f;
        [Tooltip("Legacy slowdown field; ignored by ballistic projectile simulation.")]
        public float projectileSlowdownExponent = 1f;
        [Tooltip("Legacy slowdown field; ignored by ballistic projectile simulation.")]
        public AnimationCurve projectileSlowdownCurve;
        [Tooltip("Legacy slowdown field; ignored by ballistic projectile simulation.")]
        [Range(0f, 1f)] public float projectileMinSpeedMultiplier = 0.1f;

        public LayerMask hitMask = ~0;
        public float damageMin = VehicleRuntimeStats.DefaultDamageMin;
        public float damageMax = VehicleRuntimeStats.DefaultDamageMax;
        public float shellPenetrationMm = 200f;
        public float normalizationDeg = 0f;

        private const float MAX_PASSED_TIME = 0.30f;

        private int _shotSeq;
        private float _nextServerDispersionSyncTime;
        private GunCrosshair _crosshair;
        private bool _ownerDispersionInitialized;
        private bool _serverDispersionInitialized;

        private readonly HashSet<int> _processedShots = new HashSet<int>();
        private readonly HashSet<int> _observedResolvedHitShots = new HashSet<int>();
        private readonly Dictionary<int, Projectile> _predictedProjectiles = new Dictionary<int, Projectile>();
        private readonly Dictionary<int, Projectile> _observedProjectiles = new Dictionary<int, Projectile>();
        private readonly GunDispersionModel _ownerDispersion = new GunDispersionModel();
        private readonly GunDispersionModel _serverDispersion = new GunDispersionModel();
        private readonly SyncVar<float> _serverDispersionDeg = new();
        private bool _testAccuracyDebugMode;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void SetTestAccuracyDebugMode(bool enabled)
        {
            _testAccuracyDebugMode = enabled;
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                return;
            }

            projectileSpeed = VehicleRuntimeStats.ResolveShellSpeed(stats.ShellSpeed);
            VehicleRuntimeStats.ResolveDamageRange(stats.DamageMin, stats.DamageMax, out damageMin, out damageMax);

            if (stats.Penetration > 0f)
            {
                shellPenetrationMm = stats.Penetration;
            }

            ApplyAccuracyStats(stats.Accuracy);

            if (stats.AimTime > 0f)
            {
                dispersion.aimTime = stats.AimTime;
            }

            if (_serverDispersionInitialized)
            {
                InitServerDispersion();
            }

            if (_ownerDispersionInitialized)
            {
                InitOwnerDispersion();
            }
        }

        private void ApplyAccuracyStats(float databaseAccuracy)
        {
            if (dispersion == null || databaseAccuracy <= 0f)
            {
                return;
            }

            GunDispersionGlobalSettings globalDispersion = GetGlobalDispersion();
            float minDispersionDeg = globalDispersion.GetAccuracyDispersionDeg(databaseAccuracy, dispersion.MinDispersion);
            dispersion.minDispersionDeg = minDispersionDeg;
            if (dispersion.maxDispersionDeg < minDispersionDeg)
            {
                dispersion.maxDispersionDeg = minDispersionDeg;
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
                float serverDeg;
                if (_testAccuracyDebugMode)
                {
                    _serverDispersion.ForceFullyAimed(vehicleRoot, dispersion, includeCameraAimMotion: false);
                    serverDeg = _serverDispersion.CurrentDeg;
                }
                else
                {
                    serverDeg = _serverDispersion.Tick(vehicleRoot, dispersion, globalDispersion, Time.deltaTime, includeCameraAimMotion: false);
                }
                SyncServerDispersion(serverDeg, globalDispersion, force: false);
            }

            if (IsOwner && vehicleRoot != null && !vehicleRoot.IsMenu)
            {
                if (!_ownerDispersionInitialized)
                {
                    InitOwnerDispersion();
                }

                if (_testAccuracyDebugMode)
                {
                    _ownerDispersion.ForceFullyAimed(vehicleRoot, dispersion, includeCameraAimMotion: true);
                }
                else
                {
                    _ownerDispersion.Tick(vehicleRoot, dispersion, GetGlobalDispersion(), Time.deltaTime, includeCameraAimMotion: true);
                }
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
            public bool Hit;
            public Vector3 Point;
            public Vector3 Normal;
            public bool TargetIsRobot;
            public float Damage;
            public VehicleHealth TargetHealth;
            public VehicleRoot TargetRoot;
            public ShotHudStatus HudStatus;
        }

        private struct DispersedShotRay
        {
            public Vector3 Direction;
            public Vector3 TargetPoint;
        }

        public void PredictAndRequest()
        {
            if (!vehicleRoot.IsOwner)
            {
                return;
            }

            Vector3 startPos = muzzleTransform.position;
            Vector3 baseAimPoint = GetShotAimPoint(startPos);
            int shotId = ++_shotSeq;
            if (!_ownerDispersionInitialized)
            {
                InitOwnerDispersion();
            }

            float predictedDispersionDeg;
            if (_testAccuracyDebugMode)
            {
                _ownerDispersion.ForceFullyAimed(vehicleRoot, dispersion, includeCameraAimMotion: true);
                predictedDispersionDeg = dispersion != null ? dispersion.MinDispersion : 0f;
            }
            else
            {
                predictedDispersionDeg = _serverDispersionDeg.Value > 0f
                    ? _serverDispersionDeg.Value
                    : _ownerDispersion.CurrentDeg;
            }
            DispersedShotRay predictedRay = BuildDispersedShotRay(startPos, baseAimPoint, shotId, predictedDispersionDeg, GetGlobalDispersion());

            Projectile predicted = SpawnLocal(startPos, predictedRay.TargetPoint, 0f, false, false, Vector3.up, true);
            _predictedProjectiles[shotId] = predicted;
            AddOwnerShotBloom();

            if (!IsSpawned)
            {
                return;
            }

            FireRequestServerRpc(shotId, startPos, baseAimPoint, base.TimeManager.Tick);
        }

        private Vector3 GetShotAimPoint(Vector3 startPos)
        {
            WeaponAimController weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim != null)
            {
                Vector3 currentGunAimPoint = weaponAim.GetCurrentAimPointForOrigin(startPos);
                if (IsFinite(currentGunAimPoint) && (currentGunAimPoint - startPos).sqrMagnitude > 0.01f)
                {
                    return currentGunAimPoint;
                }

                Vector3 gunForward = GetGunShotForward(weaponAim);
                if (IsFinite(gunForward) && gunForward.sqrMagnitude > 0.000001f)
                {
                    gunForward.Normalize();
                    return startPos + gunForward * GetMaxShotDistance();
                }
            }

            Vector3 forward = muzzleTransform != null ? muzzleTransform.forward : transform.forward;
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return startPos + forward * GetMaxShotDistance();
        }

        private Vector3 GetGunShotForward(WeaponAimController weaponAim)
        {
            if (weaponAim == null || weaponAim.gun == null)
            {
                return muzzleTransform != null ? muzzleTransform.forward : transform.forward;
            }

            return WeaponAimController.ToWorldAxis(weaponAim.gun, weaponAim.localForwardAxis);
        }

        private Projectile SpawnLocal(
            Vector3 startPos,
            Vector3 aimPoint,
            float passedTime,
            bool authoritative,
            bool explodeOnArrival,
            Vector3 impactNormal,
            bool visible = true,
            System.Action onAuthoritativeImpact = null,
            bool configureResolvedTarget = true)
        {
            ProjectileVisualSpawnParams spawnParams = CreateProjectileSpawnParams(
                startPos,
                aimPoint,
                passedTime,
                authoritative,
                explodeOnArrival,
                impactNormal,
                visible,
                onAuthoritativeImpact,
                configureResolvedTarget);
            return ProjectileVisualSpawner.Spawn(spawnParams);
        }

        private ProjectileVisualSpawnParams CreateProjectileSpawnParams(
            Vector3 startPos,
            Vector3 aimPoint,
            float passedTime,
            bool authoritative,
            bool explodeOnArrival,
            Vector3 impactNormal,
            bool visible,
            System.Action onAuthoritativeImpact,
            bool configureResolvedTarget)
        {
            ProjectileVisualSpawnParams spawnParams = new ProjectileVisualSpawnParams
            {
                ProjectilePrefab = projectilePrefab,
                HitMask = hitMask,
                Damage = Mathf.RoundToInt(Mathf.Max(0f, damageMax)),
                StartPosition = startPos,
                AimPoint = aimPoint,
                InitialSpeed = projectileSpeed,
                Gravity = GetProjectileGravity(),
                LifeTime = projectileLifeTime,
                CollisionRadius = GetProjectileCollisionRadius(),
                UseBallisticCompensation = ShouldUseBallisticCompensation(),
                PreferHighArc = ShouldPreferHighArc(),
                DebugBallisticTrajectory = ShouldDebugBallisticTrajectory(),
                PassedTime = passedTime,
                Authoritative = authoritative,
                ExplodeOnArrival = explodeOnArrival,
                ImpactNormal = impactNormal,
                Visible = visible,
                OnAuthoritativeImpact = onAuthoritativeImpact,
                ConfigureResolvedTarget = configureResolvedTarget,
                MaxShotDistance = GetMaxShotDistance()
            };

            return spawnParams;
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
            float shotDispersionDeg = _serverDispersion.CurrentDeg;
            if (_testAccuracyDebugMode)
            {
                _serverDispersion.ForceFullyAimed(vehicleRoot, dispersion, includeCameraAimMotion: false);
                shotDispersionDeg = dispersion != null ? dispersion.MinDispersion : 0f;
            }

            DispersedShotRay dispersedRay = BuildDispersedShotRay(startPos, aimPoint, shotId, shotDispersionDeg, globalDispersion);
            AddServerShotBloom();
            ConfigureOwnerProjectileTrajectoryTargetRpc(sender, shotId, dispersedRay.TargetPoint);

            bool serverVisualVisible = !(sender.IsLocalClient && IsClientInitialized);
            Projectile serverProjectile = null;
            serverProjectile = SpawnLocal(
                startPos,
                dispersedRay.TargetPoint,
                passed,
                true,
                false,
                Vector3.up,
                serverVisualVisible,
                null,
                configureResolvedTarget: false
            );

            if (serverProjectile != null)
            {
                serverProjectile.ConfigureLiveCollision(
                    vehicleRoot != null ? vehicleRoot.transform : null,
                    (hit, direction) => HandleAuthoritativeProjectileHit(sender, shotId, hit, direction),
                    () => HandleAuthoritativeProjectileMiss(
                        sender,
                        shotId,
                        serverProjectile != null ? serverProjectile.transform.position : dispersedRay.TargetPoint),
                    GetMaxShotDistance()
                );
            }

            FireObserversRpc(shotId, startPos, dispersedRay.TargetPoint, clientTick);
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
            if (_testAccuracyDebugMode)
            {
                return;
            }

            if (!_ownerDispersionInitialized)
            {
                InitOwnerDispersion();
            }

            _ownerDispersion.AddShotBloom(dispersion, GetGlobalDispersion());
            ApplyCrosshairDispersion();
        }

        private void AddServerShotBloom()
        {
            if (_testAccuracyDebugMode)
            {
                return;
            }

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
            float uiZoom01 = GetCrosshairUiZoom01();
            float localDeg = _testAccuracyDebugMode ? dispersion.MinDispersion : _ownerDispersion.CurrentDeg;
            float localDiameter = globalDispersion.GetUiDiameter(localDeg, dispersion.MinDispersion, uiZoom01);
            float serverDeg = _testAccuracyDebugMode
                ? localDeg
                : _serverDispersionDeg.Value > 0f ? _serverDispersionDeg.Value : _ownerDispersion.CurrentDeg;
            float serverDiameter = globalDispersion.GetUiDiameter(serverDeg, dispersion.MinDispersion, uiZoom01);
            _crosshair.SetAimingDiameters(localDiameter, serverDiameter, globalDispersion.uiDiameterLerpSpeed);
            _crosshair.SetAimStatus(localDeg, dispersion.MinDispersion, dispersion.MaxDispersion);
        }

        private float GetCrosshairUiZoom01()
        {
            if (vehicleRoot == null || vehicleRoot.cameraController == null)
            {
                return 1f;
            }

            return vehicleRoot.cameraController.GetAimUiZoom01();
        }

        private GunDispersionGlobalSettings GetGlobalDispersion()
        {
            if (IsServerInitialized)
            {
                return ServerSettings.GetGunDispersion();
            }

            return RemoteServerSettings.GunDispersion;
        }

        private ProjectileBallisticsGlobalSettings GetProjectileBallistics()
        {
            if (IsServerInitialized)
            {
                return ServerSettings.GetProjectileBallistics();
            }

            return RemoteServerSettings.ProjectileBallistics;
        }

        private DispersedShotRay BuildDispersedShotRay(Vector3 startPos, Vector3 aimPoint, int shotId, float dispersionDeg, GunDispersionGlobalSettings globalDispersion)
        {
            globalDispersion ??= GunDispersionGlobalSettings.Default;
            float maxDistance = GetMaxShotDistance();
            Vector3 baseDirection = aimPoint - startPos;
            float distance = baseDirection.magnitude;
            if (!IsFinite(baseDirection) || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.001f)
            {
                baseDirection = muzzleTransform != null ? muzzleTransform.forward : transform.forward;
                distance = maxDistance;
            }
            else
            {
                baseDirection /= distance;
            }

            float targetDistance = Mathf.Clamp(distance, 0.001f, maxDistance);

            if (dispersion == null || !globalDispersion.enabled || dispersionDeg <= 0f)
            {
                return new DispersedShotRay
                {
                    Direction = baseDirection,
                    TargetPoint = startPos + baseDirection * targetDistance
                };
            }

            Vector2 unitCircle = GetDeterministicUnitCircle(shotId);

            Vector3 right = Vector3.Cross(Vector3.up, baseDirection);
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(Vector3.forward, baseDirection);
            }
            right.Normalize();

            Vector3 up = Vector3.Cross(baseDirection, right).normalized;
            float spreadTan = Mathf.Tan(Mathf.Max(0f, dispersionDeg) * Mathf.Deg2Rad);
            Vector3 dispersedDirection = baseDirection
                                         + right * (unitCircle.x * spreadTan)
                                         + up * (unitCircle.y * spreadTan);
            if (dispersedDirection.sqrMagnitude <= 0.000001f)
            {
                dispersedDirection = baseDirection;
            }
            else
            {
                dispersedDirection.Normalize();
            }

            return new DispersedShotRay
            {
                Direction = dispersedDirection,
                TargetPoint = startPos + dispersedDirection * targetDistance
            };
        }

        private float GetMaxShotDistance()
        {
            return Mathf.Max(1f, maxShotDistance);
        }

        private Vector3 GetProjectileGravity()
        {
            ProjectileBallisticsGlobalSettings ballistics = GetProjectileBallistics();
            float gravity = ballistics != null
                ? ballistics.projectileGravity
                : projectileGravity;

            if (float.IsNaN(gravity) || float.IsInfinity(gravity))
            {
                gravity = ProjectileBallisticsGlobalSettings.Default.projectileGravity;
            }

            if (gravity <= 0f)
            {
                return Vector3.zero;
            }

            return Vector3.down * gravity;
        }

        private bool ShouldUseBallisticCompensation()
        {
            ProjectileBallisticsGlobalSettings ballistics = GetProjectileBallistics();
            if (ballistics == null)
            {
                return useBallisticCompensation;
            }

            return ballistics.useBallisticCompensation;
        }

        private bool ShouldPreferHighArc()
        {
            ProjectileBallisticsGlobalSettings ballistics = GetProjectileBallistics();
            if (ballistics == null)
            {
                return preferHighArc;
            }

            return ballistics.preferHighArc;
        }

        private bool ShouldDebugBallisticTrajectory()
        {
            ProjectileBallisticsGlobalSettings ballistics = GetProjectileBallistics();
            if (ballistics == null)
            {
                return debugBallisticTrajectory;
            }

            return ballistics.debugBallisticTrajectory;
        }

        private float GetProjectileCollisionRadius()
        {
            if (projectileCollisionRadius > 0f)
            {
                return projectileCollisionRadius;
            }

            return projectilePrefab != null ? Mathf.Max(0f, projectilePrefab.hitRadius) : 0f;
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

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                   && !float.IsNaN(value.y)
                   && !float.IsNaN(value.z)
                   && !float.IsInfinity(value.x)
                   && !float.IsInfinity(value.y)
                   && !float.IsInfinity(value.z);
        }

        private void HandleAuthoritativeProjectileHit(NetworkConnection shooterConnection, int shotId, RaycastHit hit, Vector3 shotDirection)
        {
            if (!IsServerInitialized)
            {
                return;
            }

            ResolvedShot shot = ResolveProjectileHit(hit, shotDirection);
            AuthoritativeProjectileHit?.Invoke(hit.point, hit.normal);
            ResolveOwnerProjectileTargetRpc(shooterConnection, shotId, shot.Point, shot.Normal, shot.Hit);
            ResolveObserversProjectileTargetRpc(shotId, shot.Point, shot.Normal, shot.Hit);
            ApplyResolvedShotDamage(shooterConnection, shot);
        }

        private void HandleAuthoritativeProjectileMiss(NetworkConnection shooterConnection, int shotId, Vector3 missPoint)
        {
            if (!IsServerInitialized)
            {
                return;
            }

            ResolveOwnerProjectileTargetRpc(shooterConnection, shotId, missPoint, Vector3.up, false);
            ResolveObserversProjectileTargetRpc(shotId, missPoint, Vector3.up, false);
            ShowShotResultTargetRpc(shooterConnection, (byte)ShotHudStatus.Miss);
        }

        private ResolvedShot ResolveProjectileHit(RaycastHit hit, Vector3 shotDirection)
        {
            float damage = RollDamage();
            ServerHitResolver.HitResult hr = ServerHitResolver.ResolveHit(
                hit,
                shotDirection,
                shellPenetrationMm,
                normalizationDeg,
                damage
            );

            return BuildResolvedShot(hr, hit.point, hit.normal);
        }

        private float RollDamage()
        {
            VehicleRuntimeStats.ResolveDamageRange(damageMin, damageMax, out float min, out float max);
            if (Mathf.Approximately(min, max))
            {
                return min;
            }

            return Random.Range(min, max);
        }

        private ResolvedShot BuildResolvedShot(ServerHitResolver.HitResult hr, Vector3 missPoint, Vector3 missNormal)
        {
            VehicleHealth targetHealth = null;
            VehicleRoot targetRoot = null;

            if (hr.hit && hr.collider != null)
            {
                targetHealth = hr.collider.GetComponentInParent<VehicleHealth>();
                if (targetHealth != null)
                {
                    targetRoot = targetHealth.GetComponentInParent<VehicleRoot>();
                    if (targetRoot == null)
                    {
                        targetRoot = hr.collider.GetComponentInParent<VehicleRoot>();
                    }
                }
                else
                {
                    targetRoot = hr.collider.GetComponentInParent<VehicleRoot>();
                    if (targetRoot != null)
                    {
                        targetHealth = targetRoot.health != null
                            ? targetRoot.health
                            : targetRoot.GetComponentInChildren<VehicleHealth>(true);
                    }
                }
            }

            bool targetIsRobot = targetHealth != null && targetRoot != null;
            ShotHudStatus status = GetHudStatus(targetIsRobot, hr.penetrated, hr.damage);

            return new ResolvedShot
            {
                Hit = hr.hit,
                Point = hr.hit ? hr.point : missPoint,
                Normal = hr.hit && hr.normal.sqrMagnitude > 1e-6f ? hr.normal : missNormal,
                TargetIsRobot = targetIsRobot,
                Damage = targetIsRobot ? hr.damage : 0f,
                TargetHealth = targetHealth,
                TargetRoot = targetRoot,
                HudStatus = status
            };
        }

        private ShotHudStatus GetHudStatus(bool targetIsRobot, bool penetrated, float damage)
        {
            if (!targetIsRobot)
            {
                return ShotHudStatus.Miss;
            }

            if (penetrated && damage > 0f)
            {
                return ShotHudStatus.Penetrated;
            }

            return ShotHudStatus.NotPenetrated;
        }

        private void ApplyResolvedShotDamage(NetworkConnection shooterConnection, ResolvedShot shot)
        {
            if (!IsServerInitialized)
            {
                return;
            }

            if (shot.TargetIsRobot && shot.TargetHealth != null && shot.Damage > 0f)
            {
                DamageService.ApplyVehicleShotDamage(vehicleRoot, shot.TargetRoot, shot.TargetHealth, shot.Damage);
            }

            ShowShotResultTargetRpc(shooterConnection, (byte)shot.HudStatus);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void FireObserversRpc(int shotId, Vector3 startPos, Vector3 targetPoint, uint clientTick)
        {
            if (IsOwner)
            {
                return;
            }

            if (_observedResolvedHitShots.Remove(shotId))
            {
                return;
            }

            float passed = (float)base.TimeManager.TimePassed(clientTick, allowNegative: false);
            passed = Mathf.Min(MAX_PASSED_TIME, passed);

            Projectile projectile = SpawnLocal(
                startPos,
                targetPoint,
                passed,
                false,
                false,
                Vector3.up
            );
            _observedProjectiles[shotId] = projectile;
        }

        [TargetRpc]
        private void ConfigureOwnerProjectileTrajectoryTargetRpc(NetworkConnection conn, int shotId, Vector3 serverAimPoint)
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

            CorrectPredictedProjectileTrajectory(projectile, serverAimPoint);
            projectile.SetMaxDistance(GetMaxShotDistance());
        }

        private void CorrectPredictedProjectileTrajectory(Projectile projectile, Vector3 serverAimPoint)
        {
            if (projectile == null)
            {
                return;
            }

            ProjectileVisualSpawnParams spawnParams = CreateProjectileSpawnParams(
                projectile.Origin,
                serverAimPoint,
                0f,
                false,
                false,
                Vector3.up,
                true,
                null,
                false);

            Vector3 initialVelocity = ProjectileVisualSpawner.BuildInitialVelocity(
                spawnParams,
                out bool usedBallisticCompensation,
                out bool ballisticSolutionFound);

            projectile.ReconfigureBallistic(initialVelocity, spawnParams.Gravity);
            projectile.ConfigureBallisticDebug(
                serverAimPoint,
                initialVelocity,
                spawnParams.Gravity,
                BallisticProjectileMath.EstimateDirectDrop(
                    spawnParams.StartPosition,
                    spawnParams.AimPoint,
                    spawnParams.InitialSpeed,
                    spawnParams.Gravity),
                usedBallisticCompensation,
                ballisticSolutionFound,
                spawnParams.DebugBallisticTrajectory);
        }

        [TargetRpc]
        private void ResolveOwnerProjectileTargetRpc(NetworkConnection conn, int shotId, Vector3 impactPoint, Vector3 impactNormal, bool hit)
        {
            if (!_predictedProjectiles.TryGetValue(shotId, out Projectile projectile))
            {
                if (hit)
                {
                    SpawnImpactFx(impactPoint, impactNormal);
                }
                return;
            }

            if (projectile == null)
            {
                _predictedProjectiles.Remove(shotId);
                if (hit)
                {
                    SpawnImpactFx(impactPoint, impactNormal);
                }
                return;
            }

            if (hit)
            {
                projectile.ResolveImpactNow(impactPoint, impactNormal);
            }
            else
            {
                projectile.SetMaxDistance(GetMaxShotDistance());
            }

            _predictedProjectiles.Remove(shotId);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ResolveObserversProjectileTargetRpc(int shotId, Vector3 impactPoint, Vector3 impactNormal, bool hit)
        {
            if (!_observedProjectiles.TryGetValue(shotId, out Projectile projectile))
            {
                if (hit)
                {
                    _observedResolvedHitShots.Add(shotId);
                    SpawnImpactFx(impactPoint, impactNormal);
                }
                return;
            }

            if (projectile == null)
            {
                _observedProjectiles.Remove(shotId);
                if (hit)
                {
                    _observedResolvedHitShots.Add(shotId);
                    SpawnImpactFx(impactPoint, impactNormal);
                }
                return;
            }

            if (hit)
            {
                projectile.ResolveImpactNow(impactPoint, impactNormal);
            }
            else
            {
                projectile.SetMaxDistance(GetMaxShotDistance());
            }

            _observedProjectiles.Remove(shotId);
        }

        private void SpawnImpactFx(Vector3 impactPoint, Vector3 impactNormal)
        {
            ProjectileVisualSpawner.SpawnImpactFx(projectilePrefab, impactPoint, impactNormal);
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
