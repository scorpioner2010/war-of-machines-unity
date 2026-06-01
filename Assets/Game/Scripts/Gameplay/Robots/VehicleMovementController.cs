using FishNet.Managing.Timing;
using UnityEngine;
using Game.Scripts.Diagnostics;
using Game.Scripts.Server;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleMovementController : MonoBehaviour, IVehicleRootAware, IVehicleInitializable, IVehicleStatsConsumer
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
        private TimeManager _timeManager;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (!context.IsServer)
            {
                return;
            }

            SubscribeToNetworkTicks(context.Root);
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

        private void OnEnable()
        {
            if (vehicleRoot != null && vehicleRoot.IsServerInitialized)
            {
                SubscribeToNetworkTicks(vehicleRoot);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromNetworkTicks();
            ResetMotionState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkTicks();
        }

        private void SubscribeToNetworkTicks(VehicleRoot root)
        {
            TimeManager nextTimeManager = root != null && root.networkObject != null
                ? root.networkObject.TimeManager
                : null;

            if (_timeManager == nextTimeManager)
            {
                return;
            }

            UnsubscribeFromNetworkTicks();
            _timeManager = nextTimeManager;
            if (_timeManager == null)
            {
                Debug.LogError($"{nameof(VehicleMovementController)} requires a configured FishNet TimeManager.", this);
                return;
            }

            _timeManager.OnTick += TimeManager_OnTick;
        }

        private void UnsubscribeFromNetworkTicks()
        {
            if (_timeManager == null)
            {
                return;
            }

            _timeManager.OnTick -= TimeManager_OnTick;
            _timeManager = null;
        }

        private void TimeManager_OnTick()
        {
            SimulateMovement((float)_timeManager.TickDelta);
        }

        private void SimulateMovement(float dt)
        {
            if (vehicleRoot == null || !vehicleRoot.IsServerInitialized || !CanMoveController())
            {
                ResetMotionState();
                return;
            }

            using (ProfileScope.Measure("Server.VehicleMovement.Tick", DiagnosticsCategories.Physics))
            {
                Vector2 mi = vehicleRoot.inputManager.Move;
                RobotMovementGlobalSettings settings = ServerSettings.GetRobotMovement();
                Rotate(mi, settings, dt);

                bool isLegged = vehicleRoot.footAnimator != null;
                float speedLimit = GetMaxSpeed(settings);
                float baseAcceleration = GetAcceleration(settings) * settings.GetAccelerationMultiplier(isLegged);

                Vector3 desired = transform.forward * (mi.y * speedLimit);

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
        }

        private bool CanMoveController()
        {
            return controller != null && controller.enabled && controller.gameObject.activeInHierarchy;
        }

        private void ResetMotionState()
        {
            _hVel = Vector3.zero;
            _vVel = 0f;
        }

        private void Rotate(Vector2 mi, RobotMovementGlobalSettings settings, float dt)
        {
            if (mi.x != 0f)
            {
                float rotationStep = _useRuntimeTraverseSpeed
                    ? _runtimeTraverseSpeedDegPerSecond * dt
                    : GetFallbackTraverseSpeed(settings) * dt;

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
