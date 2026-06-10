using Game.Scripts.Networking.Lobby;
using Game.Scripts.Client;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleAutoAimController : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int RaycastBufferSize = 256;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];
        public VehicleRoot vehicleRoot;
        public LayerMask acquireMask = ~0;

        private VehicleRoot _targetRoot;
        private Collider[] _targetArmorColliders;
        private Collider[] _targetTurretColliders;
        private ArmorMap[] _targetArmorMaps;
        private Collider[] _targetArmorMapColliders;
        private Transform _targetTurretTransform;
        private Vector3 _lastTargetPoint;
        private bool _controlsLocalAutoAim;

        public bool IsActive => _targetRoot != null;
        public VehicleRoot TargetRoot => _targetRoot;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            _controlsLocalAutoAim = context.IsOwner && !context.IsMenu;
            if (!_controlsLocalAutoAim)
            {
                ClearTarget();
            }
        }

        public bool ToggleFromCurrentView()
        {
            if (!_controlsLocalAutoAim)
            {
                return false;
            }

            if (IsActive)
            {
                ClearTarget();
                return false;
            }

            return TryAcquireFromCurrentView();
        }

        public void ClearTarget()
        {
            _targetRoot = null;
            _targetArmorColliders = null;
            _targetTurretColliders = null;
            _targetArmorMaps = null;
            _targetArmorMapColliders = null;
            _targetTurretTransform = null;
            _lastTargetPoint = Vector3.zero;
        }

        public bool TryGetAimTarget(out Vector3 aimPoint, out Vector3 aimForward)
        {
            aimPoint = default;
            aimForward = default;

            if (!_controlsLocalAutoAim || _targetRoot == null)
            {
                return false;
            }

            if (!IsValidEnemyTarget(_targetRoot))
            {
                ClearTarget();
                return false;
            }

            if (!TryGetTargetPoint(out aimPoint))
            {
                ClearTarget();
                return false;
            }

            Vector3 origin = GetAimForwardOrigin();
            aimForward = aimPoint - origin;
            if (!IsFinite(aimForward) || aimForward.sqrMagnitude <= 0.000001f)
            {
                aimForward = _targetRoot.transform.forward;
            }

            if (!IsFinite(aimForward) || aimForward.sqrMagnitude <= 0.000001f)
            {
                aimForward = Vector3.forward;
            }

            aimForward.Normalize();
            _lastTargetPoint = aimPoint;
            return true;
        }

        private bool TryAcquireFromCurrentView()
        {
            Camera cam = GetGameplayCamera();
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return TryAcquire(ray);
        }

        private bool TryAcquire(Ray ray)
        {
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float maxDistance = Mathf.Max(0.1f, settings.autoAimMaxAcquireDistance);
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                RaycastBuffer,
                maxDistance,
                acquireMask,
                QueryTriggerInteraction.Ignore
            );

            ArmorMap bestArmor = null;
            VehicleRoot bestRoot = null;
            RaycastHit bestHit = default;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = RaycastBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (!VehicleColliderRegistry.TryGetArmor(hitCollider, out ArmorMap armor, out VehicleRoot targetRoot))
                {
                    continue;
                }

                if (!IsValidEnemyTarget(targetRoot))
                {
                    continue;
                }

                float hitDistance = RaycastBuffer[i].distance;
                if (hitDistance < bestDistance)
                {
                    bestDistance = hitDistance;
                    bestArmor = armor;
                    bestRoot = targetRoot;
                    bestHit = RaycastBuffer[i];
                }
            }

            if (bestArmor == null || bestRoot == null)
            {
                return false;
            }

            SetTarget(bestRoot, bestHit.point);
            return true;
        }

        private void SetTarget(VehicleRoot targetRoot, Vector3 hitPoint)
        {
            _targetRoot = targetRoot;
            _targetArmorColliders = targetRoot.health != null ? targetRoot.health.colliders : null;
            _targetTurretTransform = targetRoot.robotHullRotation != null ? targetRoot.robotHullRotation.transform : null;
            _targetTurretColliders = targetRoot.turretColliders;
            _targetArmorMaps = targetRoot.armorMaps;
            CacheArmorMapColliders();
            _lastTargetPoint = IsFinite(hitPoint) ? hitPoint : targetRoot.transform.position;
        }

        private void CacheArmorMapColliders()
        {
            if (_targetArmorMaps == null)
            {
                _targetArmorMapColliders = null;
                return;
            }

            _targetArmorMapColliders = new Collider[_targetArmorMaps.Length];
            for (int i = 0; i < _targetArmorMaps.Length; i++)
            {
                ArmorMap armorMap = _targetArmorMaps[i];
                _targetArmorMapColliders[i] = armorMap != null ? armorMap.ArmorCollider : null;
            }
        }

        private bool TryGetTargetPoint(out Vector3 point)
        {
            point = default;

            if (_targetRoot == null)
            {
                return false;
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            if (settings.autoAimPreferTurretTarget
                && (TryGetBoundsFromColliders(_targetTurretColliders, out Bounds bounds)
                    || TryGetBoundsFromArmorMaps(_targetArmorMaps, ArmorMap.ArmorZone.Turret, out bounds)))
            {
                point = bounds.center;
                return IsFinite(point);
            }

            if (settings.autoAimPreferTurretTarget && _targetTurretTransform != null)
            {
                point = _targetTurretTransform.position;
                return IsFinite(point);
            }

            if (TryGetBoundsFromColliders(_targetArmorColliders, out Bounds fallbackBounds)
                || TryGetBoundsFromArmorMaps(_targetArmorMaps, ArmorMap.ArmorZone.Auto, out fallbackBounds))
            {
                point = fallbackBounds.center;
                return IsFinite(point);
            }

            if (IsFinite(_lastTargetPoint) && _lastTargetPoint != Vector3.zero)
            {
                point = _lastTargetPoint;
                return true;
            }

            point = _targetRoot.transform.position + Vector3.up * Mathf.Max(0f, settings.autoAimFallbackTargetHeight);
            return IsFinite(point);
        }

        private bool TryGetBoundsFromColliders(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            if (colliders == null)
            {
                return false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private bool TryGetBoundsFromArmorMaps(ArmorMap[] armorMaps, ArmorMap.ArmorZone requiredZone, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            if (armorMaps == null)
            {
                return false;
            }

            for (int i = 0; i < armorMaps.Length; i++)
            {
                ArmorMap armorMap = armorMaps[i];
                if (armorMap == null)
                {
                    continue;
                }

                if (requiredZone != ArmorMap.ArmorZone.Auto && armorMap.ResolvedArmorZone != requiredZone)
                {
                    continue;
                }

                Collider targetCollider = _targetArmorMapColliders != null && i < _targetArmorMapColliders.Length
                    ? _targetArmorMapColliders[i]
                    : null;
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private bool IsUsableCollider(Collider targetCollider)
        {
            return targetCollider != null
                   && targetCollider.enabled
                   && targetCollider.gameObject.activeInHierarchy;
        }

        private bool IsValidEnemyTarget(VehicleRoot targetRoot)
        {
            if (targetRoot == null || targetRoot == vehicleRoot)
            {
                return false;
            }

            if (targetRoot.health != null && targetRoot.health.IsDead)
            {
                return false;
            }

            if (targetRoot.clientVisibility != null && !targetRoot.clientVisibility.IsVisible)
            {
                return false;
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            if (!settings.autoAimRejectSameTeam)
            {
                return true;
            }

            if (vehicleRoot == null || vehicleRoot.characterInit == null || targetRoot.characterInit == null)
            {
                return true;
            }

            MatchTeam localTeam = vehicleRoot.characterInit.Team.Value;
            MatchTeam targetTeam = targetRoot.characterInit.Team.Value;
            return !MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam);
        }

        private Vector3 GetAimForwardOrigin()
        {
            Camera cam = GetGameplayCamera();
            if (cam != null)
            {
                return cam.transform.position;
            }

            if (vehicleRoot != null && vehicleRoot.weaponAimAtCamera != null && vehicleRoot.weaponAimAtCamera.gun != null)
            {
                return vehicleRoot.weaponAimAtCamera.gun.position;
            }

            return vehicleRoot != null ? vehicleRoot.transform.position : transform.position;
        }

        private static Camera GetGameplayCamera()
        {
            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                return CameraSync.In.gameplayCamera;
            }

            return null;
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
    }
}
