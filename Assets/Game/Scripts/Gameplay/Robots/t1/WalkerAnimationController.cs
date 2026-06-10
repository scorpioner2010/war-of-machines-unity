using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    [DisallowMultipleComponent]
    public sealed class WalkerAnimationController : MonoBehaviour, IVehicleRootAware
    {
        [System.Serializable]
        public sealed class Foot
        {
            public Transform transform;
            [System.NonSerialized] public Transform parent;
            [System.NonSerialized] public Vector3 neutralLocalPosition;
            [System.NonSerialized] public bool initialized;
        }

        public VehicleRoot vehicleRoot;
        public Foot leftFoot = new Foot();
        public Foot rightFoot = new Foot();

        [Header("Ground placement")]
        public LayerMask groundLayer;
        public float groundCheckDistance = 6f;
        public float footOffset;

        [Header("Step")]
        public float inputThreshold = 0.01f;
        public float stepDistance = 1.1f;
        public float stepHeight = 0.5f;
        public float stepCycleDuration = 1f;
        public float turnLiftHeight = 0.2f;
        public float turnStepDuration = 0.5f;
        public float animTransitionSpeed = 5f;
        public float strideSpeedSmoothing = 12f;
        public float minStrideSpeed = 0.05f;

        [Header("Body bobbing")]
        public Transform bodyTransform;
        public float walkingBobbingAmplitude = 0.05f;
        public float walkingBobbingFrequency = 1f;
        public float turningBobbingAmplitude = 0.05f;
        public float turningBobbingFrequency = 3f;
        public float footSpreadReference = 1.1f;
        public float bodySmoothingSpeed = 8f;

        private float _walkPhase;
        private float _turnTimer;
        private bool _isLeftTurningStep = true;
        private float _currentWalkWeight;
        private float _currentTurnWeight;
        private Vector3 _lastWorldPosition;
        private bool _hasLastWorldPosition;
        private float _smoothedHorizontalSpeed;
        private Vector3 _initialBodyLocalPosition;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Start()
        {
            InitializeFoot(leftFoot);
            InitializeFoot(rightFoot);
            if (bodyTransform != null)
            {
                _initialBodyLocalPosition = bodyTransform.localPosition;
            }
        }

        private void Update()
        {
            if (vehicleRoot == null
                || vehicleRoot.inputManager == null
                || (vehicleRoot.health != null && vehicleRoot.health.IsDead))
            {
                return;
            }

            RobotMovementGlobalSettings settings = GetMovementSettings();
            float actualSpeed = SampleHorizontalSpeed();
            Vector2 movementInput = vehicleRoot.inputManager.AnimMove;
            bool isWalking = Mathf.Abs(movementInput.y) > inputThreshold;
            bool isTurning = !isWalking && Mathf.Abs(movementInput.x) > inputThreshold;
            UpdateAnimationWeights(settings, movementInput, actualSpeed, isWalking, isTurning);
            ApplyFootAnimation(settings);
            ApplyBodyBobbing(isWalking, isTurning);
        }

        private void UpdateAnimationWeights(
            RobotMovementGlobalSettings settings,
            Vector2 movementInput,
            float actualSpeed,
            bool isWalking,
            bool isTurning)
        {
            float transitionSpeed = animTransitionSpeed * Mathf.Max(0.01f, settings.leggedTransitionSpeedMultiplier);
            if (isWalking)
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 1f, Time.deltaTime * transitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * transitionSpeed);
                float direction = movementInput.y > 0f ? 1f : -1f;
                _walkPhase += direction * Time.deltaTime * GetWalkPhaseSpeed(settings, actualSpeed);
                _walkPhase %= 1f;
                if (_walkPhase < 0f)
                {
                    _walkPhase += 1f;
                }
            }
            else if (isTurning)
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 0f, Time.deltaTime * transitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 1f, Time.deltaTime * transitionSpeed);
                _turnTimer += Time.deltaTime;
                float duration = GetTurnStepDuration(settings);
                if (_turnTimer >= duration)
                {
                    _turnTimer -= duration;
                    _isLeftTurningStep = !_isLeftTurningStep;
                }
            }
            else
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 0f, Time.deltaTime * transitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * transitionSpeed);
            }
        }

        private void ApplyFootAnimation(RobotMovementGlobalSettings settings)
        {
            Vector3 leftWalkOffset = ComputeWalkOffset(_walkPhase, settings);
            Vector3 rightWalkOffset = ComputeWalkOffset((_walkPhase + 0.5f) % 1f, settings);
            float leftWalkBlend = ComputeWalkBlend(_walkPhase);
            float rightWalkBlend = ComputeWalkBlend((_walkPhase + 0.5f) % 1f);
            Vector3 turnOffset = new Vector3(0f, turnLiftHeight, 0f);
            float turnBlend = ComputeTurnBlend(settings);
            float neutralWeight = 1f - (_currentWalkWeight + _currentTurnWeight);

            SetFootTarget(
                leftFoot,
                leftWalkOffset * _currentWalkWeight + turnOffset * _currentTurnWeight,
                neutralWeight + leftWalkBlend * _currentWalkWeight
                              + (_isLeftTurningStep ? turnBlend : 1f) * _currentTurnWeight);
            SetFootTarget(
                rightFoot,
                rightWalkOffset * _currentWalkWeight + turnOffset * _currentTurnWeight,
                neutralWeight + rightWalkBlend * _currentWalkWeight
                              + (!_isLeftTurningStep ? turnBlend : 1f) * _currentTurnWeight);
        }

        private void ApplyBodyBobbing(bool isWalking, bool isTurning)
        {
            if (bodyTransform == null)
            {
                return;
            }

            float targetY = _initialBodyLocalPosition.y;
            if (leftFoot.initialized && rightFoot.initialized)
            {
                if (isWalking)
                {
                    float leftOffsetZ = leftFoot.transform.localPosition.z - leftFoot.neutralLocalPosition.z;
                    float rightOffsetZ = rightFoot.transform.localPosition.z - rightFoot.neutralLocalPosition.z;
                    float reference = Mathf.Max(0.0001f, Mathf.Max(footSpreadReference, stepDistance));
                    float spread01 = Mathf.Clamp01(Mathf.Abs(leftOffsetZ - rightOffsetZ) / reference);
                    targetY += Mathf.Lerp(walkingBobbingAmplitude, -walkingBobbingAmplitude, spread01);
                }
                else if (isTurning)
                {
                    float leftLift = Mathf.Max(0f, leftFoot.transform.localPosition.y - leftFoot.neutralLocalPosition.y);
                    float rightLift = Mathf.Max(0f, rightFoot.transform.localPosition.y - rightFoot.neutralLocalPosition.y);
                    float lift01 = Mathf.Clamp01(Mathf.Max(leftLift, rightLift) / Mathf.Max(0.0001f, turnLiftHeight));
                    targetY += Mathf.Lerp(-turningBobbingAmplitude, turningBobbingAmplitude, lift01);
                }
            }
            else if (isWalking)
            {
                targetY += Mathf.Sin(Time.time * walkingBobbingFrequency * 2f * Mathf.PI) * walkingBobbingAmplitude;
            }
            else if (isTurning)
            {
                targetY += Mathf.Sin(Time.time * turningBobbingFrequency * 2f * Mathf.PI) * turningBobbingAmplitude;
            }

            Vector3 targetPosition = new Vector3(
                _initialBodyLocalPosition.x,
                targetY,
                _initialBodyLocalPosition.z);
            bodyTransform.localPosition = Vector3.Lerp(
                bodyTransform.localPosition,
                targetPosition,
                Time.deltaTime * Mathf.Max(0.01f, bodySmoothingSpeed));
        }

        private void SetFootTarget(Foot foot, Vector3 offset, float groundBlend)
        {
            if (foot == null || !foot.initialized)
            {
                return;
            }

            Vector3 targetWorldPosition = foot.parent.TransformPoint(foot.neutralLocalPosition + offset);
            Vector3 rayOrigin = targetWorldPosition + Vector3.up * 0.5f;
            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    groundCheckDistance,
                    groundLayer))
            {
                targetWorldPosition.y = Mathf.Lerp(
                    targetWorldPosition.y,
                    hit.point.y + footOffset,
                    groundBlend);
            }
            else
            {
                targetWorldPosition.y = Mathf.Lerp(
                    targetWorldPosition.y,
                    foot.parent.position.y + footOffset,
                    groundBlend);
            }

            foot.transform.localPosition = foot.parent.InverseTransformPoint(targetWorldPosition);
        }

        private static void InitializeFoot(Foot foot)
        {
            if (foot == null || foot.transform == null || foot.transform.parent == null)
            {
                return;
            }

            foot.parent = foot.transform.parent;
            foot.neutralLocalPosition = foot.transform.localPosition;
            foot.initialized = true;
        }

        private Vector3 ComputeWalkOffset(float phase, RobotMovementGlobalSettings settings)
        {
            float halfStep = stepDistance * Mathf.Max(0.01f, settings.leggedStepDistanceMultiplier) * 0.5f;
            float horizontal;
            float vertical = 0f;

            if (phase < 0.5f)
            {
                horizontal = Mathf.Lerp(halfStep, -halfStep, phase / 0.5f);
            }
            else
            {
                float t = (phase - 0.5f) / 0.5f;
                horizontal = Mathf.Lerp(-halfStep, halfStep, t);
                vertical = Mathf.Sin(Mathf.PI * t)
                           * stepHeight
                           * Mathf.Max(0.01f, settings.leggedStepHeightMultiplier);
            }

            return new Vector3(0f, vertical, horizontal);
        }

        private static float ComputeWalkBlend(float phase)
        {
            if (phase < 0.45f)
            {
                return 1f;
            }

            if (phase < 0.55f)
            {
                return Mathf.Lerp(1f, 0f, (phase - 0.45f) / 0.1f);
            }

            if (phase < 0.85f)
            {
                return 0f;
            }

            return Mathf.Lerp(0f, 1f, (phase - 0.85f) / 0.15f);
        }

        private float ComputeTurnBlend(RobotMovementGlobalSettings settings)
        {
            float t = _turnTimer / GetTurnStepDuration(settings);
            return t < 0.5f
                ? Mathf.Lerp(1f, 0f, t * 2f)
                : Mathf.Lerp(0f, 1f, (t - 0.5f) * 2f);
        }

        private float GetWalkPhaseSpeed(RobotMovementGlobalSettings settings, float actualSpeed)
        {
            float strideDistance = stepDistance * Mathf.Max(0.01f, settings.leggedStepDistanceMultiplier);
            if (actualSpeed < Mathf.Max(0f, minStrideSpeed))
            {
                return 0f;
            }

            float phaseSpeed = actualSpeed / Mathf.Max(0.0001f, strideDistance * 2f);
            float maxPhaseSpeed = Mathf.Max(0.01f, settings.leggedAnimationMaxSpeedMultiplier)
                                  / Mathf.Max(0.0001f, stepCycleDuration);
            return Mathf.Min(phaseSpeed, maxPhaseSpeed);
        }

        private float GetTurnStepDuration(RobotMovementGlobalSettings settings)
        {
            return Mathf.Max(
                0.0001f,
                turnStepDuration * Mathf.Max(0.01f, settings.leggedTurnStepDurationMultiplier));
        }

        private float SampleHorizontalSpeed()
        {
            Transform speedTransform = vehicleRoot.objectMover != null
                ? vehicleRoot.objectMover.transform
                : transform;
            Vector3 currentPosition = speedTransform.position;
            if (!_hasLastWorldPosition)
            {
                _lastWorldPosition = currentPosition;
                _hasLastWorldPosition = true;
                return 0f;
            }

            Vector3 delta = currentPosition - _lastWorldPosition;
            _lastWorldPosition = currentPosition;
            delta.y = 0f;
            float rawSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, strideSpeedSmoothing) * Time.deltaTime);
            _smoothedHorizontalSpeed = Mathf.Lerp(_smoothedHorizontalSpeed, rawSpeed, t);
            return _smoothedHorizontalSpeed;
        }

        private RobotMovementGlobalSettings GetMovementSettings()
        {
            return vehicleRoot.IsServerInitialized
                ? ServerSettings.GetRobotMovement()
                : RemoteServerSettings.RobotMovement;
        }
    }
}
