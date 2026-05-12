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

        public float projectileSpeed = VehicleRuntimeStats.DefaultShellSpeed;
        public float projectileLifeTime = 8f;
        public float maxShotDistance = 2000f;

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

            projectileSpeed = VehicleRuntimeStats.ResolveShellSpeed(stats.ShellSpeed);
            VehicleRuntimeStats.ResolveDamageRange(stats.DamageMin, stats.DamageMax, out damageMin, out damageMax);

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
            public bool Hit;
            public Vector3 Point;
            public Vector3 Normal;
            public bool TargetIsRobot;
            public float Damage;
            public Health TargetHealth;
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

            DispersedShotRay predictedRay = BuildDispersedShotRay(startPos, baseAimPoint, shotId, _ownerDispersion.CurrentDeg, GetGlobalDispersion());

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
            WeaponAimAtCamera weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim != null)
            {
                if (CameraSync.In != null)
                {
                    weaponAim.ResolveCameraAim(CameraSync.In.transform, out Vector3 cameraAimPoint, out _);
                    if (IsFinite(cameraAimPoint) && (cameraAimPoint - startPos).sqrMagnitude > 0.01f)
                    {
                        return cameraAimPoint;
                    }
                }

                Vector3 desiredAimPoint = weaponAim.DesiredAimPoint;
                if (IsFinite(desiredAimPoint) && (desiredAimPoint - startPos).sqrMagnitude > 0.01f)
                {
                    return desiredAimPoint;
                }

                Vector3 currentAimPoint = weaponAim.GetCurrentAimPointForOrigin(startPos);
                if (IsFinite(currentAimPoint) && (currentAimPoint - startPos).sqrMagnitude > 0.01f)
                {
                    return currentAimPoint;
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
            Projectile proj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            proj.hitMask = hitMask;
            proj.damage = Mathf.RoundToInt(Mathf.Max(0f, damageMax));
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
            if (configureResolvedTarget)
            {
                if (explodeOnArrival)
                {
                    proj.ConfigureResolvedImpact(aimPoint, impactNormal, onAuthoritativeImpact);
                }
                else
                {
                    proj.ConfigureResolvedMiss(aimPoint, GetMaxShotDistance(), onAuthoritativeImpact);
                }
            }

            if (!visible)
            {
                proj.SetVisualsEnabled(false);
            }
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
            DispersedShotRay dispersedRay = BuildDispersedShotRay(startPos, aimPoint, shotId, _serverDispersion.CurrentDeg, globalDispersion);
            AddServerShotBloom();
            ConfigureOwnerProjectileTrajectoryTargetRpc(sender, shotId, dispersedRay.TargetPoint);

            bool serverVisualVisible = !(sender.IsLocalClient && IsClientInitialized);
            Projectile serverProjectile = SpawnLocal(
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
                    () => HandleAuthoritativeProjectileMiss(sender, shotId, dispersedRay.TargetPoint),
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
            Health targetHealth = null;
            VehicleRoot targetRoot = null;

            if (hr.hit && hr.collider != null)
            {
                targetHealth = hr.collider.GetComponentInParent<Health>();
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
                            : targetRoot.GetComponentInChildren<Health>(true);
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
        private void ConfigureOwnerProjectileTrajectoryTargetRpc(NetworkConnection conn, int shotId, Vector3 targetPoint)
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

            projectile.ConfigureResolvedMiss(targetPoint, GetMaxShotDistance());
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
                projectile.SetMissContinuationMaxDistance(GetMaxShotDistance());
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
                projectile.SetMissContinuationMaxDistance(GetMaxShotDistance());
            }

            _observedProjectiles.Remove(shotId);
        }

        private void SpawnImpactFx(Vector3 impactPoint, Vector3 impactNormal)
        {
            if (projectilePrefab == null || projectilePrefab.explosionFX == null)
            {
                return;
            }

            Vector3 normal = impactNormal.sqrMagnitude > 0.000001f ? impactNormal : Vector3.up;
            Instantiate(projectilePrefab.explosionFX, impactPoint, Quaternion.LookRotation(normal));
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
