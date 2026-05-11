using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class BodyBobbing : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot playerRoot;
        
        public float bobbingAmplitude = 0.05f;
        public float bobbingFrequency = 1.0f;
        public float inputThreshold = 0.01f;
        public bool isWalkingBobbing = true;
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

            if (TryUpdateFromFootPositions(isWalking, isRotating))
            {
                return;
            }
        
            bool applyBobbing;
            if (isWalkingBobbing)
            {
                applyBobbing = isWalking;
            }
            else
            {
                applyBobbing = isRotating && !isWalking;
            }

            if (applyBobbing)
            {
                float newY = _initialLocalPosition.y + Mathf.Sin(Time.time * bobbingFrequency * 2 * Mathf.PI) * bobbingAmplitude;
                Vector3 newPosition = new Vector3(_initialLocalPosition.x, newY, _initialLocalPosition.z);
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _initialLocalPosition, Time.deltaTime * 5f);
            }
        }

        private bool TryUpdateFromFootPositions(bool isWalking, bool isRotating)
        {
            if (!isWalkingBobbing)
            {
                return playerRoot != null && playerRoot.footAnimator != null;
            }

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

            bool applyBobbing = isWalking || isRotating;
            float targetY = _initialLocalPosition.y;
            if (applyBobbing)
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
                targetY = _initialLocalPosition.y + Mathf.Lerp(bobbingAmplitude, -bobbingAmplitude, spread01);
            }

            float speed = Mathf.Max(0.01f, smoothingSpeed);
            Vector3 targetPosition = new Vector3(_initialLocalPosition.x, targetY, _initialLocalPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * speed);
            return true;
        }
    }
}
