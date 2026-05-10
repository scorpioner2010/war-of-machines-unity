using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class WeaponAimAtCamera : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable
    {
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
        public Vector3 CurrentAimPoint { get; private set; }

        private readonly SyncVar<Vector3> _serverAimPoint = new(Vector3.zero);
        public Vector3 ServerAimPoint => _serverAimPoint.Value;

        private Transform _cam;
        private Quaternion _initialLocalRotation;

        private float _localPitch;
        private float _targetPitchServer;

        public enum Axis { X, Y, Z }

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

            Init(CameraSync.In.transform);
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
        }

        private void Awake()
        {
            if (gun != null)
            {
                _initialLocalRotation = gun.localRotation;
            }
        }

        [Server]
        public void SetTargetPitchServer(float targetPitch)
        {
            _targetPitchServer = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            if (gun == null)
            {
                return;
            }

            Vector3 gunFwdWorld = ToWorldAxis(gun, localForwardAxis).normalized;
            Ray gunRay = new Ray(gun.position, gunFwdWorld);

            Vector3 gunAimPoint = gun.position + gunFwdWorld * maxAimDistance;
            if (Physics.Raycast(gunRay, out RaycastHit gunHit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            {
                gunAimPoint = gunHit.point;
            }
            CurrentAimPoint = gunAimPoint;

            if (IsServerInitialized)
            {
                float step = smoothSpeed * Time.deltaTime;

                float delta = Mathf.DeltaAngle(_localPitch, _targetPitchServer);
                if (Mathf.Abs(delta) <= pitchDeadZoneDeg)
                {
                    _localPitch = _targetPitchServer;
                }
                else
                {
                    _localPitch = Mathf.MoveTowardsAngle(_localPitch, _targetPitchServer, step);
                }

                Quaternion localRot = _initialLocalRotation * Quaternion.AngleAxis(_localPitch, AxisToVector(localPitchAxis));
                gun.localRotation = localRot;

                _serverAimPoint.Value = CurrentAimPoint;
            }
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
