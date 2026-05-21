using Game.Scripts.Diagnostics;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class VehicleMovementController : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        private const float MovementSettingsRefreshInterval = 0.5f;

        private static RobotMovementGlobalSettings _sharedMovementSettings;
        private static float _nextMovementSettingsRefreshTime;

        public VehicleRoot vehicleRoot;
        public RobotMovementMotor motor;

        public float MotorYaw => motor != null ? motor.MotorYaw : transform.eulerAngles.y;
        public float CurrentForwardSpeed => motor != null ? motor.currentForwardSpeed : 0f;
        public float CurrentTurnSpeed => motor != null ? motor.currentTurnSpeed : 0f;
        public Vector3 Velocity => motor != null ? motor.velocity : Vector3.zero;
        public bool IsGrounded => motor != null && motor.isGrounded;
        public bool IsSlidingOnSteepSlope => motor != null && motor.isSlidingOnSteepSlope;
        public Vector3 GroundPoint => motor != null ? motor.groundPoint : transform.position;
        public Vector3 GroundNormal => motor != null ? motor.groundNormal : Vector3.up;
        public float SlopeAngle => motor != null ? motor.slopeAngle : 0f;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            EnsureMotor();
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            EnsureMotor();
            if (motor != null)
            {
                motor.ApplyVehicleStats(stats);
            }
        }

        private void Awake()
        {
            EnsureMotor();
        }

        private void FixedUpdate()
        {
            if (vehicleRoot == null || !vehicleRoot.IsServerInitialized)
            {
                return;
            }

            EnsureMotor();
            if (motor == null)
            {
                return;
            }

            using (ProfileScope.Measure("Server.VehicleMovement.FixedUpdate", DiagnosticsCategories.Physics))
            {
                Vector2 moveInput = vehicleRoot.inputManager != null ? vehicleRoot.inputManager.Move : Vector2.zero;
                RobotMovementGlobalSettings settings = GetMovementSettings();
                bool isLegged = vehicleRoot.footAnimator != null;
                motor.Tick(moveInput, settings, Time.fixedDeltaTime, isLegged);
            }
        }

        private static RobotMovementGlobalSettings GetMovementSettings()
        {
            if (_sharedMovementSettings == null || Time.unscaledTime >= _nextMovementSettingsRefreshTime)
            {
                _sharedMovementSettings = ServerSettings.GetRobotMovement();
                _nextMovementSettingsRefreshTime = Time.unscaledTime + MovementSettingsRefreshInterval;
            }

            return _sharedMovementSettings;
        }

        private void EnsureMotor()
        {
            if (motor != null)
            {
                return;
            }

            motor = GetComponent<RobotMovementMotor>();
            if (motor == null)
            {
                motor = gameObject.AddComponent<RobotMovementMotor>();
            }
        }
    }
}
