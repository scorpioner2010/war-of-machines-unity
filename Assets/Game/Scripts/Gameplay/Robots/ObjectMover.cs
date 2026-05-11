using UnityEngine;
using Game.Scripts.Server;

namespace Game.Scripts.Gameplay.Robots
{
    public class ObjectMover : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        public VehicleRoot vehicleRoot;
        public CharacterController controller;

        public float rotateSpeed = 2f;
        public float acceleration = 30f;
        public float maxSpeed = 10f;

        private Vector3 _hVel;
        private float _vVel;
        private bool _useRuntimeTraverseSpeed;
        private float _runtimeTraverseSpeedDegPerSecond;

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

            if (stats.Speed > 0f)
            {
                maxSpeed = stats.Speed;
            }

            if (stats.Acceleration > 0f)
            {
                acceleration = stats.Acceleration;
            }

            if (stats.TraverseSpeed > 0f)
            {
                _runtimeTraverseSpeedDegPerSecond = stats.TraverseSpeed;
                _useRuntimeTraverseSpeed = true;
            }
        }

        private void FixedUpdate()
        {
            if (vehicleRoot == null || !vehicleRoot.IsServerInitialized)
            {
                return;
            }

            Vector2 mi = vehicleRoot.inputManager.Move;
            RobotMovementGlobalSettings settings = ServerSettings.GetRobotMovement();
            Rotate(mi, settings);

            bool isLegged = vehicleRoot.footAnimator != null;
            float speedLimit = GetMaxSpeed(settings);
            float baseAcceleration = GetAcceleration(settings) * settings.GetAccelerationMultiplier(isLegged);

            Vector3 desired = transform.forward * (mi.y * speedLimit);

            float dt = Time.fixedDeltaTime;
            Vector3 delta = desired - _hVel;
            float accelerationRate = baseAcceleration;
            if (IsStoppingOrBraking(desired))
            {
                accelerationRate *= Mathf.Max(1f, settings.stoppingAccelerationMultiplier);
                accelerationRate *= settings.GetBrakingMultiplier(isLegged);
            }

            Vector3 step = Vector3.ClampMagnitude(delta, accelerationRate * dt);
            _hVel += step;

            if (_hVel.magnitude > speedLimit)
            {
                _hVel = _hVel.normalized * speedLimit;
            }

            bool grounded = controller.isGrounded;
            _vVel = grounded ? -GetGroundedSnap(settings) : _vVel - GetGravity(settings) * dt;

            Vector3 move = new Vector3(_hVel.x, _vVel, _hVel.z) * dt;
            controller.Move(move);
        }

        private void Rotate(Vector2 mi, RobotMovementGlobalSettings settings)
        {
            if (mi.x != 0f)
            {
                float rotationStep = _useRuntimeTraverseSpeed
                    ? _runtimeTraverseSpeedDegPerSecond * Time.fixedDeltaTime
                    : GetFallbackTraverseSpeed(settings) * Time.fixedDeltaTime;

                transform.Rotate(Vector3.up * mi.x * rotationStep);
            }
        }

        private float GetMaxSpeed(RobotMovementGlobalSettings settings)
        {
            if (maxSpeed > 0f)
            {
                return maxSpeed;
            }

            return Mathf.Max(0f, settings.fallbackMaxSpeed);
        }

        private float GetAcceleration(RobotMovementGlobalSettings settings)
        {
            if (acceleration > 0f)
            {
                return acceleration;
            }

            return Mathf.Max(0.01f, settings.fallbackAcceleration);
        }

        private float GetFallbackTraverseSpeed(RobotMovementGlobalSettings settings)
        {
            if (settings.fallbackTraverseSpeedDegPerSecond > 0f)
            {
                return settings.fallbackTraverseSpeedDegPerSecond;
            }

            return rotateSpeed / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        }

        private float GetGravity(RobotMovementGlobalSettings settings)
        {
            return settings.gravity > 0f ? settings.gravity : RobotMovementGlobalSettings.Default.gravity;
        }

        private float GetGroundedSnap(RobotMovementGlobalSettings settings)
        {
            return settings.groundedSnap > 0f ? settings.groundedSnap : RobotMovementGlobalSettings.Default.groundedSnap;
        }

        private bool IsStoppingOrBraking(Vector3 desired)
        {
            if (_hVel.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            if (desired.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            return Vector3.Dot(_hVel, desired) <= 0f || desired.sqrMagnitude < _hVel.sqrMagnitude;
        }
    }
}
