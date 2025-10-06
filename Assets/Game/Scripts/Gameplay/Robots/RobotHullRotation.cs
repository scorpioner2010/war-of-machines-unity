using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class RobotHullRotation : NetworkBehaviour
    {
        public TankRoot tankRoot;

        public float rotationSpeed = 30f;
        public float maxLocalYaw = 0f;
        public bool startAlignedToChassis = true;

        private float _localYaw;
        private float _targetYawServer;
        private Transform _chassisTransform;

        public void Init()
        {
            _chassisTransform = tankRoot.objectMover.transform;

            if (startAlignedToChassis)
            {
                _localYaw = 0f;
            }
            else
            {
                Vector3 chassisFwd = _chassisTransform.forward;
                chassisFwd.y = 0f; if (chassisFwd.sqrMagnitude > 1e-6f) chassisFwd.Normalize();

                Vector3 turretFwd = transform.forward;
                turretFwd.y = 0f; if (turretFwd.sqrMagnitude > 1e-6f) turretFwd.Normalize();

                _localYaw = Vector3.SignedAngle(chassisFwd, turretFwd, Vector3.up);
            }

            _targetYawServer = _localYaw;
            transform.rotation = _chassisTransform.rotation * Quaternion.Euler(0f, _localYaw, 0f);
        }

        [Server]
        public void SetTargetYawServer(float targetLocalYaw)
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
            if (!IsServer || _chassisTransform == null)
            {
                return;
            }

            float step = rotationSpeed * Time.deltaTime;
            _localYaw = Mathf.MoveTowardsAngle(_localYaw, _targetYawServer, step);
            transform.rotation = _chassisTransform.rotation * Quaternion.Euler(0f, _localYaw, 0f);
        }
    }
}