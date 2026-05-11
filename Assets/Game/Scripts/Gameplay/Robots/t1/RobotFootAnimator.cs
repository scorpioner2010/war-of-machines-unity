using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class RobotFootAnimator : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot playerRoot;
        
        public FootPlacer leftFoot;
        public FootPlacer rightFoot;
        
        public float inputThreshold = 0.01f;

        public float stepDistance = 1.1f;
        public float stepHeight = 0.5f;
        public float stepCycleDuration = 1f;

        public float turnLiftHeight = 0.2f;
        public float turnStepDuration = 0.5f;

        public float animTransitionSpeed = 5f;

        private float _walkPhase = 0f;
        private float _turnTimer = 0f;
        private bool _isLeftTurningStep = true;

        private float _currentWalkWeight = 0f;
        private float _currentTurnWeight = 0f;
        private Vector3 _lastWorldPosition;
        private bool _hasLastWorldPosition;

        public void SetVehicleRoot(VehicleRoot root)
        {
            playerRoot = root;
        }
        
        private void Update()
        {
            if (playerRoot == null || playerRoot.inputManager == null)
            {
                return;
            }

            RobotMovementGlobalSettings settings = GetMovementSettings();
            float actualSpeed = SampleHorizontalSpeed();
            Vector2 movementInput = playerRoot.inputManager.AnimMove;
        
            bool isWalking = Mathf.Abs(movementInput.y) > inputThreshold;
            bool isTurning = (!isWalking) && (Mathf.Abs(movementInput.x) > inputThreshold);
            float transitionSpeed = animTransitionSpeed * Mathf.Max(0.01f, settings.leggedTransitionSpeedMultiplier);
        
            if (isWalking)
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 1f, Time.deltaTime * transitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * transitionSpeed);
                float direction = movementInput.y > 0f ? 1f : -1f;
                float animationSpeed = GetWalkAnimationSpeed(settings, actualSpeed);
                _walkPhase += direction * Time.deltaTime * animationSpeed / Mathf.Max(0.0001f, stepCycleDuration);
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
                float currentTurnStepDuration = GetTurnStepDuration(settings);
                if (_turnTimer >= currentTurnStepDuration)
                {
                    _turnTimer -= currentTurnStepDuration;
                    _isLeftTurningStep = !_isLeftTurningStep;
                }
            }
            else
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 0f, Time.deltaTime * transitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * transitionSpeed);
            }

            Vector3 leftWalkOffset = ComputeWalkOffset(_walkPhase, settings);
            Vector3 rightWalkOffset = ComputeWalkOffset((_walkPhase + 0.5f) % 1f, settings);
            float leftWalkBlend = ComputeWalkBlend(_walkPhase);
            float rightWalkBlend = ComputeWalkBlend((_walkPhase + 0.5f) % 1f);

            Vector3 turnOffset = new Vector3(0f, turnLiftHeight, 0f);
            float turnBlend = ComputeTurnBlend(settings);
            float leftTurnBlend = _isLeftTurningStep ? turnBlend : 1f;
            float rightTurnBlend = !_isLeftTurningStep ? turnBlend : 1f;
            Vector3 leftTurnOffset = turnOffset;
            Vector3 rightTurnOffset = turnOffset;

            Vector3 neutralOffset = Vector3.zero;
            float neutralBlend = 1f;

            Vector3 leftFinalOffset = neutralOffset * (1f - (_currentWalkWeight + _currentTurnWeight))
                                      + leftWalkOffset * _currentWalkWeight
                                      + leftTurnOffset * _currentTurnWeight;
            Vector3 rightFinalOffset = neutralOffset * (1f - (_currentWalkWeight + _currentTurnWeight))
                                       + rightWalkOffset * _currentWalkWeight
                                       + rightTurnOffset * _currentTurnWeight;

            float leftFinalBlend = (neutralBlend * (1f - (_currentWalkWeight + _currentTurnWeight))
                                    + leftWalkBlend * _currentWalkWeight
                                    + leftTurnBlend * _currentTurnWeight);
            float rightFinalBlend = (neutralBlend * (1f - (_currentWalkWeight + _currentTurnWeight))
                                     + rightWalkBlend * _currentWalkWeight
                                     + rightTurnBlend * _currentTurnWeight);

            if (leftFoot != null)
            {
                leftFoot.SetTargetOffset(leftFinalOffset, leftFinalBlend);
            }
            if (rightFoot != null)
            {
                rightFoot.SetTargetOffset(rightFinalOffset, rightFinalBlend);
            }
        }

        private Vector3 ComputeWalkOffset(float phase, RobotMovementGlobalSettings settings)
        {
            float halfStep = stepDistance * Mathf.Max(0.01f, settings.leggedStepDistanceMultiplier) * 0.5f;
            float horizontal = 0f;
            float vertical = 0f;

            if (phase < 0.5f)
            {
                float t = phase / 0.5f;
                horizontal = Mathf.Lerp(halfStep, -halfStep, t);
            }
            else
            {
                float t = (phase - 0.5f) / 0.5f;
                horizontal = Mathf.Lerp(-halfStep, halfStep, t);
                vertical = Mathf.Sin(Mathf.PI * t) * stepHeight * Mathf.Max(0.01f, settings.leggedStepHeightMultiplier);
            }
            return new Vector3(0f, vertical, horizontal);
        }

        private float ComputeWalkBlend(float phase)
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
            return t < 0.5f ? Mathf.Lerp(1f, 0f, t * 2f) : Mathf.Lerp(0f, 1f, (t - 0.5f) * 2f);
        }

        private float GetWalkAnimationSpeed(RobotMovementGlobalSettings settings, float actualSpeed)
        {
            float referenceSpeed = Mathf.Max(0.01f, settings.leggedAnimationReferenceSpeed);
            float normalizedSpeed = Mathf.Clamp01(actualSpeed / referenceSpeed);
            float shapedSpeed = Mathf.Pow(normalizedSpeed, Mathf.Max(0.01f, settings.leggedAnimationSpeedExponent));
            float minSpeed = Mathf.Max(0.01f, settings.leggedAnimationMinSpeedMultiplier);
            float maxSpeed = Mathf.Max(minSpeed, settings.leggedAnimationMaxSpeedMultiplier);
            return Mathf.Lerp(minSpeed, maxSpeed, shapedSpeed);
        }

        private float GetTurnStepDuration(RobotMovementGlobalSettings settings)
        {
            return Mathf.Max(0.0001f, turnStepDuration * Mathf.Max(0.01f, settings.leggedTurnStepDurationMultiplier));
        }

        private float SampleHorizontalSpeed()
        {
            Vector3 currentPosition = playerRoot != null ? playerRoot.transform.position : transform.position;
            if (!_hasLastWorldPosition)
            {
                _lastWorldPosition = currentPosition;
                _hasLastWorldPosition = true;
                return 0f;
            }

            Vector3 delta = currentPosition - _lastWorldPosition;
            _lastWorldPosition = currentPosition;
            delta.y = 0f;
            return delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        private RobotMovementGlobalSettings GetMovementSettings()
        {
            if (playerRoot != null && playerRoot.IsServerInitialized)
            {
                return ServerSettings.GetRobotMovement();
            }

            return RemoteServerSettings.RobotMovement;
        }
    }
}
