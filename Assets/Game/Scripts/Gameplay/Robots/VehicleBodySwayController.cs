using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleBodySwayController : MonoBehaviour, IVehicleRootAware
    {
        private const float MinimumSimulationStep = 0.0001f;
        private const float MinimumInputResponse = 0.01f;
        private const float MinimumPitchSpeed = 0.01f;

        public VehicleRoot vehicleRoot;

        [Header("Input response")]
        [Range(0f, 0.5f)] public float inputDeadZone = 0.01f;
        [Min(0.01f)] public float accelerationInputResponse = 4f;
        [Min(0.01f)] public float brakingInputResponse = 8f;

        [Header("Pitch")]
        [Min(0f)] public float accelerationPitchDegrees = 2f;
        [Min(0f)] public float brakingPitchDegrees = 3f;
        [Min(0f)] public float maxPitchDegrees = 4f;

        [Header("Terrain response")]
        [Min(0f)] public float terrainAngularSpeedDeadZone = 5f;
        [Min(0f)] public float terrainPitchDegreesPerAngularSpeed = 0.025f;
        [Min(0f)] public float terrainRollDegreesPerAngularSpeed = 0.025f;
        [Min(0f)] public float terrainPitchDegreesPerVerticalSpeed = 0.75f;
        [Min(0f)] public float maxTerrainPitchDegrees = 2f;
        [Min(0f)] public float maxTerrainRollDegrees = 2f;
        [Min(0.01f)] public float maxRollSpeedDegrees = 90f;

        [Header("Spring")]
        [Min(0f)] public float springStrength = 60f;
        [Min(0f)] public float damping = 12f;
        [Min(0.01f)] public float maxPitchSpeedDegrees = 90f;
        [Min(0.0001f)] public float maximumSimulationStep = 0.05f;

        private bool _hasInputSample;
        private bool _hasTerrainSample;
        private float _simulatedForwardInput;
        private float _pitchDegrees;
        private float _pitchSpeedDegrees;
        private float _rollDegrees;
        private float _rollSpeedDegrees;
        private Vector3 _previousGroundNormal;
        private float _previousGroundDistance;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public Quaternion GetGroundRelativeRotationOffset(
            Vector3 groundNormal,
            Quaternion groundRotation,
            float averageGroundDistance,
            float deltaTime)
        {
            if (!isActiveAndEnabled
                || !ShouldSimulateLocalPresentation()
                || vehicleRoot.inputManager == null)
            {
                ResetState();
                return Quaternion.identity;
            }

            float desiredForwardInput = Mathf.Clamp(vehicleRoot.inputManager.AnimMove.y, -1f, 1f);
            if (!_hasInputSample)
            {
                _hasInputSample = true;
                _simulatedForwardInput = desiredForwardInput;
                return Quaternion.identity;
            }

            float simulationStep = Mathf.Min(
                Mathf.Max(deltaTime, 0f),
                Mathf.Max(maximumSimulationStep, MinimumSimulationStep));
            if (simulationStep <= 0f)
            {
                return Quaternion.Euler(_pitchDegrees, 0f, _rollDegrees);
            }

            float inputDelta = desiredForwardInput - _simulatedForwardInput;
            float currentInputMagnitude = Mathf.Abs(_simulatedForwardInput);
            float desiredInputMagnitude = Mathf.Abs(desiredForwardInput);
            bool changesDirection = _simulatedForwardInput * desiredForwardInput < -inputDeadZone;
            bool reducesSpeed = desiredInputMagnitude + inputDeadZone < currentInputMagnitude;
            bool isBraking = changesDirection || reducesSpeed;
            float inputResponse = isBraking ? brakingInputResponse : accelerationInputResponse;

            _simulatedForwardInput = Mathf.MoveTowards(
                _simulatedForwardInput,
                desiredForwardInput,
                Mathf.Max(inputResponse, MinimumInputResponse) * simulationStep);

            float targetPitchDegrees = 0f;
            if (Mathf.Abs(inputDelta) > inputDeadZone)
            {
                float pitchMagnitude = isBraking ? brakingPitchDegrees : accelerationPitchDegrees;
                targetPitchDegrees = -Mathf.Sign(inputDelta)
                                     * pitchMagnitude
                                     * Mathf.Min(Mathf.Abs(inputDelta), 1f);
            }

            ResolveTerrainTargets(
                desiredForwardInput,
                groundNormal,
                groundRotation,
                averageGroundDistance,
                Mathf.Max(deltaTime, MinimumSimulationStep),
                out float terrainPitchDegrees,
                out float terrainRollDegrees);

            SimulateAxis(
                ref _pitchDegrees,
                ref _pitchSpeedDegrees,
                targetPitchDegrees + terrainPitchDegrees,
                maxPitchDegrees,
                maxPitchSpeedDegrees,
                simulationStep);
            SimulateAxis(
                ref _rollDegrees,
                ref _rollSpeedDegrees,
                terrainRollDegrees,
                maxTerrainRollDegrees,
                maxRollSpeedDegrees,
                simulationStep);
            return Quaternion.Euler(_pitchDegrees, 0f, _rollDegrees);
        }

        public void ClearTerrainContact()
        {
            if (!ShouldSimulateLocalPresentation())
            {
                ResetState();
                return;
            }

            ClearTerrainContactState();
        }

        private void ClearTerrainContactState()
        {
            _hasTerrainSample = false;
            _previousGroundNormal = Vector3.zero;
            _previousGroundDistance = 0f;
        }

        private void OnDisable()
        {
            ResetState();
        }

        private void OnValidate()
        {
            inputDeadZone = Mathf.Clamp(inputDeadZone, 0f, 0.5f);
            accelerationInputResponse = Mathf.Max(accelerationInputResponse, MinimumInputResponse);
            brakingInputResponse = Mathf.Max(brakingInputResponse, MinimumInputResponse);
            accelerationPitchDegrees = Mathf.Max(accelerationPitchDegrees, 0f);
            brakingPitchDegrees = Mathf.Max(brakingPitchDegrees, 0f);
            maxPitchDegrees = Mathf.Max(maxPitchDegrees, 0f);
            terrainAngularSpeedDeadZone = Mathf.Max(terrainAngularSpeedDeadZone, 0f);
            terrainPitchDegreesPerAngularSpeed = Mathf.Max(terrainPitchDegreesPerAngularSpeed, 0f);
            terrainRollDegreesPerAngularSpeed = Mathf.Max(terrainRollDegreesPerAngularSpeed, 0f);
            terrainPitchDegreesPerVerticalSpeed = Mathf.Max(terrainPitchDegreesPerVerticalSpeed, 0f);
            maxTerrainPitchDegrees = Mathf.Max(maxTerrainPitchDegrees, 0f);
            maxTerrainRollDegrees = Mathf.Max(maxTerrainRollDegrees, 0f);
            maxRollSpeedDegrees = Mathf.Max(maxRollSpeedDegrees, MinimumPitchSpeed);
            springStrength = Mathf.Max(springStrength, 0f);
            damping = Mathf.Max(damping, 0f);
            maxPitchSpeedDegrees = Mathf.Max(maxPitchSpeedDegrees, MinimumPitchSpeed);
            maximumSimulationStep = Mathf.Max(maximumSimulationStep, MinimumSimulationStep);
        }

        private bool ShouldSimulateLocalPresentation()
        {
            return vehicleRoot != null && vehicleRoot.IsOwner && !vehicleRoot.IsMenu;
        }

        private void ResolveTerrainTargets(
            float desiredForwardInput,
            Vector3 groundNormal,
            Quaternion groundRotation,
            float averageGroundDistance,
            float deltaTime,
            out float terrainPitchDegrees,
            out float terrainRollDegrees)
        {
            terrainPitchDegrees = 0f;
            terrainRollDegrees = 0f;
            Vector3 normalizedGroundNormal = groundNormal.normalized;
            if (!_hasTerrainSample)
            {
                _hasTerrainSample = true;
                _previousGroundNormal = normalizedGroundNormal;
                _previousGroundDistance = averageGroundDistance;
                return;
            }

            Vector3 normalRotationAxis = Vector3.Cross(_previousGroundNormal, normalizedGroundNormal);
            float normalDot = Mathf.Clamp(Vector3.Dot(_previousGroundNormal, normalizedGroundNormal), -1f, 1f);
            float normalRotationAxisMagnitude = normalRotationAxis.magnitude;
            if (normalRotationAxisMagnitude > 0.000001f)
            {
                float normalRotationDegrees = Mathf.Atan2(normalRotationAxisMagnitude, normalDot) * Mathf.Rad2Deg;
                Vector3 normalAngularSpeed = normalRotationAxis
                                             * (normalRotationDegrees / (normalRotationAxisMagnitude * deltaTime));
                float pitchAngularSpeed = Vector3.Dot(normalAngularSpeed, groundRotation * Vector3.right);
                float rollAngularSpeed = Vector3.Dot(normalAngularSpeed, groundRotation * Vector3.forward);
                terrainPitchDegrees -= ResolveAngularTerrainResponse(
                    pitchAngularSpeed,
                    terrainPitchDegreesPerAngularSpeed,
                    maxTerrainPitchDegrees);
                terrainRollDegrees -= ResolveAngularTerrainResponse(
                    rollAngularSpeed,
                    terrainRollDegreesPerAngularSpeed,
                    maxTerrainRollDegrees);
            }

            if (Mathf.Abs(desiredForwardInput) > inputDeadZone)
            {
                float groundDistanceSpeed = (averageGroundDistance - _previousGroundDistance) / deltaTime;
                terrainPitchDegrees -= Mathf.Sign(desiredForwardInput)
                                       * groundDistanceSpeed
                                       * terrainPitchDegreesPerVerticalSpeed;
            }

            terrainPitchDegrees = Mathf.Clamp(
                terrainPitchDegrees,
                -maxTerrainPitchDegrees,
                maxTerrainPitchDegrees);
            terrainRollDegrees = Mathf.Clamp(
                terrainRollDegrees,
                -maxTerrainRollDegrees,
                maxTerrainRollDegrees);
            _previousGroundNormal = normalizedGroundNormal;
            _previousGroundDistance = averageGroundDistance;
        }

        private float ResolveAngularTerrainResponse(float angularSpeed, float sensitivity, float maximumDegrees)
        {
            float responseSpeed = Mathf.Max(Mathf.Abs(angularSpeed) - terrainAngularSpeedDeadZone, 0f);
            return Mathf.Sign(angularSpeed) * Mathf.Min(responseSpeed * sensitivity, maximumDegrees);
        }

        private void SimulateAxis(
            ref float angleDegrees,
            ref float speedDegrees,
            float targetAngleDegrees,
            float maximumAngleDegrees,
            float maximumSpeedDegrees,
            float deltaTime)
        {
            float springAcceleration = (targetAngleDegrees - angleDegrees) * springStrength;
            float dampingAcceleration = -speedDegrees * damping;
            speedDegrees += (springAcceleration + dampingAcceleration) * deltaTime;
            speedDegrees = Mathf.Clamp(speedDegrees, -maximumSpeedDegrees, maximumSpeedDegrees);

            angleDegrees += speedDegrees * deltaTime;
            float clampedAngle = Mathf.Clamp(angleDegrees, -maximumAngleDegrees, maximumAngleDegrees);
            if (clampedAngle != angleDegrees)
            {
                angleDegrees = clampedAngle;
                speedDegrees = 0f;
            }
        }

        private void ResetState()
        {
            _hasInputSample = false;
            _simulatedForwardInput = 0f;
            _pitchDegrees = 0f;
            _pitchSpeedDegrees = 0f;
            _rollDegrees = 0f;
            _rollSpeedDegrees = 0f;
            ClearTerrainContactState();
        }
    }
}
