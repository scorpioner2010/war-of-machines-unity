using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleTurretRotationController : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable, IVehicleStatsConsumer
    {
        public VehicleRoot vehicleRoot;

        public float rotationSpeed = 30f;
        public float maxLocalYaw = 0f;
        public bool startAlignedToChassis = true;

        private float _localYaw;
        private float _targetYawServer;
        private Transform _chassisTransform;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats != null && stats.TurretTraverseSpeed > 0f)
            {
                rotationSpeed = stats.TurretTraverseSpeed;
            }
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsServer || (context.IsOwner && !context.IsMenu))
            {
                Init();
            }
        }

        public void Init()
        {
            if (vehicleRoot == null || vehicleRoot.objectMover == null)
            {
                return;
            }

            _chassisTransform = vehicleRoot.objectMover.transform;

            if (startAlignedToChassis)
            {
                _localYaw = 0f;
            }
            else
            {
                Vector3 chassisFwd = _chassisTransform.forward;
                chassisFwd.y = 0f;
                if (chassisFwd.sqrMagnitude > 1e-6f)
                {
                    chassisFwd.Normalize();
                }

                Vector3 turretFwd = transform.forward;
                turretFwd.y = 0f;
                if (turretFwd.sqrMagnitude > 1e-6f)
                {
                    turretFwd.Normalize();
                }

                _localYaw = Vector3.SignedAngle(chassisFwd, turretFwd, Vector3.up);
            }

            _targetYawServer = _localYaw;
            transform.rotation = _chassisTransform.rotation * Quaternion.Euler(0f, _localYaw, 0f);
        }

        [Server]
        public void SetTargetYawServer(float targetLocalYaw)
        {
            SetTargetYaw(targetLocalYaw);
        }

        public void SetTargetYaw(float targetLocalYaw)
        {
            if (maxLocalYaw > 0f)
            {
                float half = Mathf.Abs(maxLocalYaw);
                targetLocalYaw = Mathf.Clamp(targetLocalYaw, -half, half);
            }

            _targetYawServer = targetLocalYaw;
        }

        private void LateUpdate()
        {
            if (!ShouldDriveRotation() || _chassisTransform == null)
            {
                return;
            }

            float step = rotationSpeed * Time.deltaTime;
            _localYaw = Mathf.MoveTowardsAngle(_localYaw, _targetYawServer, step);
            transform.rotation = _chassisTransform.rotation * Quaternion.Euler(0f, _localYaw, 0f);
        }

        private bool ShouldDriveRotation()
        {
            if (IsServerInitialized)
            {
                return true;
            }

            return IsOwner && vehicleRoot != null && !vehicleRoot.IsMenu;
        }
    }
}
