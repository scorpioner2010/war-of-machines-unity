using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class VehicleBodyVerticalSpringController : MonoBehaviour, IVehicleRootAware
    {
        private const float MinimumDeltaTime = 0.0001f;
        private const float MinimumVerticalSpeed = 0.01f;
        private const float MinimumChassisVerticalSpeed = 0.01f;

        public VehicleRoot vehicleRoot;
        public Transform chassis;

        [Header("Chassis vertical response")]
        [Min(0f)] public float verticalSpeedDeadZone = 0.05f;
        [Min(0f)] public float verticalSpeedChangeDeadZone = 0.1f;
        [Min(MinimumChassisVerticalSpeed)] public float maximumChassisVerticalSpeed = 8f;
        [Min(MinimumChassisVerticalSpeed)] public float maximumChassisVerticalSpeedChange = 3f;
        [Min(0f)] public float chassisVerticalSpeedTransfer = 1.2f;

        [Header("Spring")]
        [FormerlySerializedAs("maxOffset")]
        [Min(0f)] public float maxUpwardOffset = 0.02f;
        [Min(0f)] public float maxDownwardOffset = 0.02f;

        [Min(0f)] public float springStrength = 45f;
        [Min(0f)] public float damping = 6f;
        [Min(0.01f)] public float maxVerticalSpeed = 4f;
        [Range(0f, 1f)] public float endStopRebound = 0.35f;
        [Min(0.0001f)] public float maximumSimulationStep = 0.05f;

        private bool _hasChassisSample;
        private bool _ignoreNextChassisSpeedChange;
        private float _previousChassisY;
        private float _previousChassisVerticalSpeed;
        private float _verticalOffset;
        private float _verticalSpeed;

        public float VerticalOffset => _verticalOffset;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void OnEnable()
        {
            ResetMotion();
        }

        private void LateUpdate()
        {
            if (vehicleRoot == null
                || !vehicleRoot.IsOwner
                || vehicleRoot.IsMenu
                || (vehicleRoot.health != null && vehicleRoot.health.IsDead)
                || chassis == null)
            {
                ResetMotion();
                return;
            }

            float sampleDeltaTime = Mathf.Max(Time.deltaTime, MinimumDeltaTime);
            float simulationStep = Mathf.Min(
                sampleDeltaTime,
                Mathf.Max(maximumSimulationStep, MinimumDeltaTime));
            ApplyChassisMotionImpulse(sampleDeltaTime);
            SimulateOffset(simulationStep);
        }

        private void OnDisable()
        {
            ResetMotion();
        }

        private void OnValidate()
        {
            verticalSpeedDeadZone = Mathf.Max(verticalSpeedDeadZone, 0f);
            verticalSpeedChangeDeadZone = Mathf.Max(verticalSpeedChangeDeadZone, 0f);
            maximumChassisVerticalSpeed = Mathf.Max(
                maximumChassisVerticalSpeed,
                MinimumChassisVerticalSpeed);
            maximumChassisVerticalSpeedChange = Mathf.Max(
                maximumChassisVerticalSpeedChange,
                MinimumChassisVerticalSpeed);
            chassisVerticalSpeedTransfer = Mathf.Max(chassisVerticalSpeedTransfer, 0f);
            maxUpwardOffset = Mathf.Max(maxUpwardOffset, 0f);
            maxDownwardOffset = Mathf.Max(maxDownwardOffset, 0f);
            springStrength = Mathf.Max(springStrength, 0f);
            damping = Mathf.Max(damping, 0f);
            maxVerticalSpeed = Mathf.Max(maxVerticalSpeed, MinimumVerticalSpeed);
            endStopRebound = Mathf.Clamp01(endStopRebound);
            maximumSimulationStep = Mathf.Max(maximumSimulationStep, MinimumDeltaTime);
        }

        private void ApplyChassisMotionImpulse(float deltaTime)
        {
            float chassisY = chassis.position.y;
            if (!_hasChassisSample)
            {
                _hasChassisSample = true;
                _previousChassisY = chassisY;
                _previousChassisVerticalSpeed = 0f;
                return;
            }

            float chassisVerticalSpeed = (chassisY - _previousChassisY) / deltaTime;
            _previousChassisY = chassisY;
            if (Mathf.Abs(chassisVerticalSpeed) > maximumChassisVerticalSpeed)
            {
                _hasChassisSample = false;
                _ignoreNextChassisSpeedChange = true;
                return;
            }

            if (Mathf.Abs(chassisVerticalSpeed) <= verticalSpeedDeadZone)
            {
                chassisVerticalSpeed = 0f;
            }

            if (_ignoreNextChassisSpeedChange)
            {
                _ignoreNextChassisSpeedChange = false;
                _previousChassisVerticalSpeed = chassisVerticalSpeed;
                return;
            }

            float chassisVerticalSpeedChange = chassisVerticalSpeed - _previousChassisVerticalSpeed;
            _previousChassisVerticalSpeed = chassisVerticalSpeed;
            if (Mathf.Abs(chassisVerticalSpeedChange) <= verticalSpeedChangeDeadZone)
            {
                return;
            }

            float clampedSpeedChange = Mathf.Clamp(
                chassisVerticalSpeedChange,
                -maximumChassisVerticalSpeedChange,
                maximumChassisVerticalSpeedChange);
            _verticalSpeed -= clampedSpeedChange * chassisVerticalSpeedTransfer;
            _verticalSpeed = Mathf.Clamp(_verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);
        }

        private void SimulateOffset(float deltaTime)
        {
            float springAcceleration = -_verticalOffset * springStrength;
            float dampingAcceleration = -_verticalSpeed * damping;
            _verticalSpeed += (springAcceleration + dampingAcceleration) * deltaTime;
            _verticalSpeed = Mathf.Clamp(_verticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);
            _verticalOffset += _verticalSpeed * deltaTime;
            if (_verticalOffset > maxUpwardOffset)
            {
                _verticalOffset = maxUpwardOffset;
                if (_verticalSpeed > 0f)
                {
                    _verticalSpeed = -_verticalSpeed * endStopRebound;
                }

                return;
            }

            if (_verticalOffset < -maxDownwardOffset)
            {
                _verticalOffset = -maxDownwardOffset;
                if (_verticalSpeed < 0f)
                {
                    _verticalSpeed = -_verticalSpeed * endStopRebound;
                }
            }
        }

        private void ResetMotion()
        {
            _hasChassisSample = false;
            _ignoreNextChassisSpeedChange = false;
            _previousChassisY = 0f;
            _previousChassisVerticalSpeed = 0f;
            _verticalOffset = 0f;
            _verticalSpeed = 0f;
        }
    }
}
