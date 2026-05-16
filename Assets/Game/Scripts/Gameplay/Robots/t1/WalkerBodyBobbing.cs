using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class WalkerBodyBobbing : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot playerRoot;
        
        [FormerlySerializedAs("bobbingAmplitude")]
        public float walkingBobbingAmplitude = 0.05f;
        [FormerlySerializedAs("bobbingFrequency")]
        public float walkingBobbingFrequency = 1.0f;
        public float turningBobbingAmplitude = 0.05f;
        public float turningBobbingFrequency = 3.0f;
        public float inputThreshold = 0.01f;
        public float footSpreadReference = 1.1f;
        public float smoothingSpeed = 8f;

        private Vector3 _initialLocalPosition;

        public void SetVehicleRoot(VehicleRoot root)
        {
            playerRoot = root;
        }

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (playerRoot == null || playerRoot.inputManager == null)
            {
                return;
            }

            bool isWalking = Mathf.Abs(playerRoot.inputManager.AnimMove.y) > inputThreshold;
            bool isRotating = Mathf.Abs(playerRoot.inputManager.AnimMove.x) > inputThreshold;

            float targetY;
            if (!TryGetFootDrivenTargetY(isWalking, isRotating, out targetY))
            {
                targetY = GetFallbackTargetY(isWalking, isRotating);
            }

            ApplyTargetY(targetY);
        }

        private bool TryGetFootDrivenTargetY(bool isWalking, bool isRotating, out float targetY)
        {
            targetY = _initialLocalPosition.y;

            RobotFootAnimator footAnimator = playerRoot.footAnimator;
            if (footAnimator == null || footAnimator.leftFoot == null || footAnimator.rightFoot == null)
            {
                return false;
            }

            FootPlacer leftFoot = footAnimator.leftFoot;
            FootPlacer rightFoot = footAnimator.rightFoot;
            if (!leftFoot.IsInitialized || !rightFoot.IsInitialized)
            {
                return false;
            }

            if (isWalking)
            {
                targetY = GetWalkingFootTargetY(footAnimator, leftFoot, rightFoot);
                return true;
            }

            if (isRotating)
            {
                targetY = GetTurningFootTargetY(footAnimator, leftFoot, rightFoot);
                return true;
            }

            return true;
        }

        private float GetWalkingFootTargetY(RobotFootAnimator footAnimator, FootPlacer leftFoot, FootPlacer rightFoot)
        {
            float leftOffsetZ = leftFoot.CurrentLocalPosition.z - leftFoot.NeutralLocalPosition.z;
            float rightOffsetZ = rightFoot.CurrentLocalPosition.z - rightFoot.NeutralLocalPosition.z;
            float spread = Mathf.Abs(leftOffsetZ - rightOffsetZ);
            float reference = Mathf.Max(0.0001f, footSpreadReference);
            if (footAnimator.stepDistance > 0f)
            {
                reference = Mathf.Max(reference, footAnimator.stepDistance);
            }

            float spread01 = Mathf.Clamp01(spread / reference);
            return _initialLocalPosition.y + Mathf.Lerp(walkingBobbingAmplitude, -walkingBobbingAmplitude, spread01);
        }

        private float GetTurningFootTargetY(RobotFootAnimator footAnimator, FootPlacer leftFoot, FootPlacer rightFoot)
        {
            float leftLift = Mathf.Max(0f, leftFoot.CurrentLocalPosition.y - leftFoot.NeutralLocalPosition.y);
            float rightLift = Mathf.Max(0f, rightFoot.CurrentLocalPosition.y - rightFoot.NeutralLocalPosition.y);
            float lift = Mathf.Max(leftLift, rightLift);
            float reference = Mathf.Max(0.0001f, footAnimator.turnLiftHeight);
            float lift01 = Mathf.Clamp01(lift / reference);
            return _initialLocalPosition.y + Mathf.Lerp(-turningBobbingAmplitude, turningBobbingAmplitude, lift01);
        }

        private float GetFallbackTargetY(bool isWalking, bool isRotating)
        {
            if (isWalking)
            {
                return _initialLocalPosition.y + Mathf.Sin(Time.time * walkingBobbingFrequency * 2f * Mathf.PI) * walkingBobbingAmplitude;
            }

            if (isRotating)
            {
                return _initialLocalPosition.y + Mathf.Sin(Time.time * turningBobbingFrequency * 2f * Mathf.PI) * turningBobbingAmplitude;
            }

            return _initialLocalPosition.y;
        }

        private void ApplyTargetY(float targetY)
        {
            float speed = Mathf.Max(0.01f, smoothingSpeed);
            Vector3 targetPosition = new Vector3(_initialLocalPosition.x, targetY, _initialLocalPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * speed);
        }
    }
}
