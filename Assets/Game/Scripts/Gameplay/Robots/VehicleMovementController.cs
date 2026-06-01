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

        private float _forwardSpeed;
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

                float desiredSpeed = mi.y * speedLimit;
                float accelerationRate = baseAcceleration;
                if (IsStoppingOrBraking(desiredSpeed))
                {
                    accelerationRate *= Mathf.Max(1f, settings.stoppingAccelerationMultiplier);
                    accelerationRate *= settings.GetBrakingMultiplier(isLegged);
                }

                _forwardSpeed = Mathf.MoveTowards(_forwardSpeed, desiredSpeed, accelerationRate * dt);
                _forwardSpeed = Mathf.Clamp(_forwardSpeed, -speedLimit, speedLimit);

                bool grounded = controller.isGrounded;
                _vVel = grounded ? -GetGroundedSnap(settings) : _vVel - GetGravity(settings) * dt;

                Vector3 horizontalVelocity = transform.forward * _forwardSpeed;
                horizontalVelocity.y = 0f;

                Vector3 positionBeforeMove = controller.transform.position;
                Vector3 move = new Vector3(horizontalVelocity.x, _vVel, horizontalVelocity.z) * dt;
                CollisionFlags collisionFlags = controller.Move(move);
                if ((collisionFlags & CollisionFlags.Sides) != 0)
                {
                    ReconcileForwardSpeedAfterSideCollision(positionBeforeMove, horizontalVelocity, dt);
                }
            }
        }

        private bool CanMoveController()
        {
            return controller != null && controller.enabled && controller.gameObject.activeInHierarchy;
        }

        private void ResetMotionState()
        {
            _forwardSpeed = 0f;
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

        private void ReconcileForwardSpeedAfterSideCollision(
            Vector3 positionBeforeMove,
            Vector3 horizontalVelocity,
            float dt)
        {
            float intendedHorizontalSpeed = horizontalVelocity.magnitude;
            if (intendedHorizontalSpeed <= 0.0001f || dt <= 0.0001f)
            {
                return;
            }

            Vector3 horizontalDisplacement = controller.transform.position - positionBeforeMove;
            horizontalDisplacement.y = 0f;

            Vector3 movementDirection = horizontalVelocity / intendedHorizontalSpeed;
            float actualHorizontalSpeed = Vector3.Dot(horizontalDisplacement, movementDirection) / dt;
            if (actualHorizontalSpeed >= intendedHorizontalSpeed)
            {
                return;
            }

            float retainedSpeedRatio = Mathf.Clamp01(actualHorizontalSpeed / intendedHorizontalSpeed);
            _forwardSpeed *= retainedSpeedRatio;
        }

        private bool IsStoppingOrBraking(float desiredSpeed)
        {
            if (Mathf.Abs(_forwardSpeed) <= 0.0001f)
            {
                return false;
            }

            if (Mathf.Abs(desiredSpeed) <= 0.0001f)
            {
                return true;
            }

            return _forwardSpeed * desiredSpeed <= 0f || Mathf.Abs(desiredSpeed) < Mathf.Abs(_forwardSpeed);
        }
    }
}
