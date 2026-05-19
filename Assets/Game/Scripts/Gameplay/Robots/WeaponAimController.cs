using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Diagnostics;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-10)]
    public class WeaponAimController : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int AimRaycastBufferSize = 64;
        private static readonly RaycastHit[] AimRaycastBuffer = new RaycastHit[AimRaycastBufferSize];

        public VehicleRoot vehicleRoot;

        public Transform gun;
        public float smoothSpeed = 10f;
        public float minPitch = -45f;
        public float maxPitch = 45f;
        public float pitchOffset = 0f;
        public float maxAimDistance = 500f;
        public float forwardZClamp = 0.25f;
        public LayerMask aimMask = ~0;

        public Axis localForwardAxis = Axis.Z;
        public Axis localPitchAxis = Axis.X;

        public float pitchDeadZoneDeg = 0.25f;

        public Quaternion InitialLocalRotation => _initialLocalRotation;
        public Vector3 DesiredAimPoint { get; private set; }
        public Vector3 CurrentAimPoint { get; private set; }

        private readonly SyncVar<Vector3> _serverAimPoint = new(Vector3.zero);
        public Vector3 ServerAimPoint => _serverAimPoint.Value;

        private Transform _cam;
        private Quaternion _initialLocalRotation;

        private float _localPitch;
        private float _targetPitchServer;
        private bool _hasDesiredAimPoint;
        private bool _hasAimPlaneForward;
        private Vector3 _aimPlaneForward;
        private int _externalDesiredAimFrame = -1;

        private const float MinAimDistance = 0.25f;
        private const float AimPointClampPadding = 50f;

        public enum Axis { X, Y, Z }

        public float CurrentLocalPitch => _localPitch;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (!context.IsOwner || context.IsMenu || CameraSync.In == null)
            {
                return;
            }

            Init(CameraSync.In.GetAimTransform());
        }

        public void Init(Transform cam)
        {
            if (gun == null)
            {
                return;
            }

            _cam = cam;
            _initialLocalRotation = gun.localRotation;
            _localPitch = 0f;
            _targetPitchServer = 0f;
            RefreshDesiredAimPointFromCamera();
            UpdateCurrentAimPoint();
        }

        private void Awake()
        {
            if (gun != null)
            {
                _initialLocalRotation = gun.localRotation;
                SetFallbackAimPoint();
            }
        }

        [Server]
        public void SetTargetPitchServer(float targetPitch)
        {
            SetTargetPitch(targetPitch);
        }

        public void SetTargetPitch(float targetPitch)
        {
            _targetPitchServer = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        [Server]
        public void SetDesiredAimPointServer(Vector3 aimPoint, Vector3 aimPlaneForward)
        {
            SetDesiredAimPoint(aimPoint, aimPlaneForward);
        }

        public void SetDesiredAimPoint(Vector3 aimPoint)
        {
            Vector3 aimPlaneForward = _cam != null ? _cam.forward : _aimPlaneForward;
            SetDesiredAimPoint(aimPoint, aimPlaneForward);
        }

        public void SetDesiredAimPoint(Vector3 aimPoint, Vector3 aimPlaneForward)
        {
            if (!IsFinite(aimPoint))
            {
                return;
            }

            DesiredAimPoint = ClampAimPoint(aimPoint);
            _hasDesiredAimPoint = true;
            SetAimPlaneForward(aimPlaneForward);
            _externalDesiredAimFrame = Time.frameCount;
        }

        public void ResolveCameraAim(Transform cameraTransform, out Vector3 aimPoint, out Vector3 aimForward)
        {
            if (cameraTransform == null)
            {
                EnsureDesiredAimPoint();
                aimPoint = DesiredAimPoint;
                aimForward = GetAimPlaneForward();
                if (aimForward.sqrMagnitude <= 0.000001f)
                {
                    aimForward = transform.forward;
                }
                return;
            }

            aimForward = cameraTransform.forward;
            float maxDistance = Mathf.Max(MinAimDistance, maxAimDistance);
            Ray cameraRay = new Ray(cameraTransform.position, aimForward);
            aimPoint = cameraTransform.position + aimForward * maxDistance;

            Transform ignoredRoot = vehicleRoot != null ? vehicleRoot.transform : transform.root;
            if (TryRaycastAim(cameraRay, maxDistance, ignoredRoot, out RaycastHit cameraHit))
            {
                aimPoint = cameraHit.point;
            }
        }

        public Vector3 GetCurrentAimPointForOrigin(Vector3 origin)
        {
            if (gun == null)
            {
                return DesiredAimPoint;
            }

            EnsureDesiredAimPoint();

            Vector3 gunFwdWorld = GetLogicalAimForwardWorld();
            if (gunFwdWorld.sqrMagnitude <= 0.000001f)
            {
                gunFwdWorld = transform.forward;
            }

            gunFwdWorld.Normalize();

            Transform ignoredRoot = vehicleRoot != null ? vehicleRoot.transform : transform.root;
            float castDistance = GetAimCastDistance(origin);
            Ray gunRay = new Ray(origin, gunFwdWorld);
            if (TryRaycastAim(gunRay, castDistance, ignoredRoot, out RaycastHit gunHit))
            {
                return gunHit.point;
            }

            float fallbackDistance = GetLogicalFallbackAimDistance(origin, castDistance);
            return origin + gunFwdWorld * fallbackDistance;
        }

        public Vector3 GetLogicalAimForwardWorld()
        {
            if (gun == null)
            {
                return transform.forward;
            }

            Quaternion logicalLocalRotation = _initialLocalRotation
                                              * Quaternion.AngleAxis(_targetPitchServer, AxisToVector(localPitchAxis));
            Quaternion parentRotation = gun.parent != null ? gun.parent.rotation : transform.rotation;
            Quaternion logicalWorldRotation = parentRotation * logicalLocalRotation;
            Vector3 forward = logicalWorldRotation * AxisToVector(localForwardAxis);
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                forward = ToWorldAxis(gun, localForwardAxis);
            }

            return forward;
        }

        private void LateUpdate()
        {
            using (ProfileScope.Measure(IsServerInitialized ? "Server.WeaponAim.LateUpdate" : "Client.WeaponAim.LateUpdate", IsServerInitialized ? DiagnosticsCategories.Server : DiagnosticsCategories.Client))
            {
                if (gun == null)
                {
                    return;
                }

                RefreshDesiredAimPointFromCamera();

                if (ShouldDriveGunPitch())
                {
                    _localPitch = _targetPitchServer;
                    Quaternion localRot = _initialLocalRotation * Quaternion.AngleAxis(_localPitch, AxisToVector(localPitchAxis));
                    gun.localRotation = localRot;
                }

                UpdateCurrentAimPoint();

                if (IsServerInitialized)
                {
                    _serverAimPoint.Value = CurrentAimPoint;
                }
            }
        }

        private void RefreshDesiredAimPointFromCamera()
        {
            if (_externalDesiredAimFrame == Time.frameCount)
            {
                EnsureDesiredAimPoint();
                return;
            }

            if (_cam == null)
            {
                EnsureDesiredAimPoint();
                return;
            }

            ResolveCameraAim(_cam, out Vector3 cameraAimPoint, out Vector3 cameraAimForward);
            SetDesiredAimPoint(cameraAimPoint, cameraAimForward);
        }

        private void UpdateCurrentAimPoint()
        {
            CurrentAimPoint = GetCurrentAimPointForOrigin(gun.position);
        }

        private void EnsureDesiredAimPoint()
        {
            if (_hasDesiredAimPoint && IsFinite(DesiredAimPoint))
            {
                return;
            }

            SetFallbackAimPoint();
        }

        private void SetFallbackAimPoint()
        {
            if (gun == null)
            {
                SetAimPlaneForward(transform.forward);
                DesiredAimPoint = transform.position + transform.forward * Mathf.Max(MinAimDistance, maxAimDistance);
                CurrentAimPoint = DesiredAimPoint;
                _hasDesiredAimPoint = true;
                return;
            }

            Vector3 gunFwdWorld = ToWorldAxis(gun, localForwardAxis);
            if (gunFwdWorld.sqrMagnitude <= 0.000001f)
            {
                gunFwdWorld = transform.forward;
            }

            gunFwdWorld.Normalize();
            SetAimPlaneForward(gunFwdWorld);
            DesiredAimPoint = gun.position + gunFwdWorld * Mathf.Max(MinAimDistance, maxAimDistance);
            CurrentAimPoint = DesiredAimPoint;
            _hasDesiredAimPoint = true;
        }

        private Vector3 ClampAimPoint(Vector3 aimPoint)
        {
            if (gun == null)
            {
                return aimPoint;
            }

            Vector3 fromGun = aimPoint - gun.position;
            float distance = fromGun.magnitude;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= MinAimDistance)
            {
                Vector3 gunFwdWorld = ToWorldAxis(gun, localForwardAxis);
                if (gunFwdWorld.sqrMagnitude <= 0.000001f)
                {
                    gunFwdWorld = transform.forward;
                }

                gunFwdWorld.Normalize();
                return gun.position + gunFwdWorld * MinAimDistance;
            }

            float maxDistance = Mathf.Max(MinAimDistance, maxAimDistance + AimPointClampPadding);
            if (distance > maxDistance)
            {
                return gun.position + fromGun / distance * maxDistance;
            }

            return aimPoint;
        }

        private float GetLogicalFallbackAimDistance(Vector3 origin, float maxDistance)
        {
            if (_hasDesiredAimPoint && IsFinite(DesiredAimPoint))
            {
                float desiredDistance = (DesiredAimPoint - origin).magnitude;
                if (!float.IsNaN(desiredDistance)
                    && !float.IsInfinity(desiredDistance)
                    && desiredDistance > MinAimDistance)
                {
                    return Mathf.Clamp(desiredDistance, MinAimDistance, Mathf.Max(MinAimDistance, maxDistance));
                }
            }

            return Mathf.Max(MinAimDistance, maxDistance);
        }

        private float GetAimCastDistance(Vector3 origin)
        {
            float maxDistance = Mathf.Max(MinAimDistance, maxAimDistance + AimPointClampPadding);
            if (_hasDesiredAimPoint && IsFinite(DesiredAimPoint))
            {
                float desiredDistance = (DesiredAimPoint - origin).magnitude;
                if (!float.IsNaN(desiredDistance) && !float.IsInfinity(desiredDistance))
                {
                    maxDistance = Mathf.Max(maxDistance, desiredDistance);
                }
            }

            return maxDistance;
        }

        private bool ShouldDriveGunPitch()
        {
            if (IsServerInitialized)
            {
                return true;
            }

            return IsOwner && vehicleRoot != null && !vehicleRoot.IsMenu;
        }

        private Vector3 GetAimPlaneForward()
        {
            if (_cam != null)
            {
                return _cam.forward.normalized;
            }

            if (_hasAimPlaneForward && _aimPlaneForward.sqrMagnitude > 0.000001f)
            {
                return _aimPlaneForward;
            }

            return Vector3.zero;
        }

        private void SetAimPlaneForward(Vector3 aimPlaneForward)
        {
            if (!IsFinite(aimPlaneForward) || aimPlaneForward.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            _aimPlaneForward = aimPlaneForward.normalized;
            _hasAimPlaneForward = true;
        }

        private bool TryRaycastAim(Ray ray, float distance, Transform ignoredRoot, out RaycastHit bestHit)
        {
            int count = Physics.RaycastNonAlloc(
                ray,
                AimRaycastBuffer,
                distance,
                aimMask,
                QueryTriggerInteraction.Ignore
            );

            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = AimRaycastBuffer[i].collider;
                if (hitCollider == null || IsUnderRoot(hitCollider.transform, ignoredRoot))
                {
                    continue;
                }

                float hitDistance = AimRaycastBuffer[i].distance;
                if (hitDistance < bestDistance)
                {
                    bestDistance = hitDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                bestHit = AimRaycastBuffer[bestIndex];
                return true;
            }

            bestHit = default;
            return false;
        }

        private static bool IsUnderRoot(Transform transform, Transform root)
        {
            if (root == null)
            {
                return false;
            }

            Transform current = transform;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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

        public static Vector3 AxisToVector(Axis a)
        {
            if (a == Axis.X)
            {
                return Vector3.right;
            }
            if (a == Axis.Y)
            {
                return Vector3.up;
            }
            return Vector3.forward;
        }

        public static Vector3 ToWorldAxis(Transform t, Axis a)
        {
            if (a == Axis.X)
            {
                return t.right;
            }
            if (a == Axis.Y)
            {
                return t.up;
            }
            return t.forward;
        }
    }
}
