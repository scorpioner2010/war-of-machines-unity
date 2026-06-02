using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class CaterpillarTrack : MonoBehaviour, IVehicleRootAware
    {
        private const float InputThreshold = 0.01f;
        private const float MovingInnerTrackMinSpeedRatio = 1f / 3f;

        public VehicleRoot vehicleRoot;
        public Renderer[] mesh;

        public float forwardBackwardSpeed = 1.0f;
        public float turnInPlaceSpeed = 0.7f;
        public float turnWhileMovingSpeed = 0.5f;

        public WheelSpinAnimator[] rightWheels;
        public WheelSpinAnimator[] leftWheels;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Update()
        {
            if (vehicleRoot == null || vehicleRoot.inputManager == null)
            {
                return;
            }

            Vector2 mv = vehicleRoot.inputManager.AnimMove;
            float forwardInput = mv.y;
            float turnInput = mv.x;

            float leftInputSpeed = 0f;
            float rightInputSpeed = 0f;

            if (Mathf.Abs(forwardInput) > InputThreshold && Mathf.Abs(turnInput) < InputThreshold)
            {
                leftInputSpeed = forwardInput * forwardBackwardSpeed;
                rightInputSpeed = forwardInput * forwardBackwardSpeed;
            }
            else if (Mathf.Abs(forwardInput) < InputThreshold && Mathf.Abs(turnInput) > InputThreshold)
            {
                leftInputSpeed = turnInput * turnInPlaceSpeed;
                rightInputSpeed = -turnInput * turnInPlaceSpeed;
            }
            else if (Mathf.Abs(forwardInput) > InputThreshold && Mathf.Abs(turnInput) > InputThreshold)
            {
                leftInputSpeed = (forwardInput + turnInput) * turnWhileMovingSpeed;
                rightInputSpeed = (forwardInput - turnInput) * turnWhileMovingSpeed;
                EnsureMovingInnerTrackSpeed(forwardInput, ref leftInputSpeed, ref rightInputSpeed);
            }

            float leftTrackSpeed = leftInputSpeed * -Time.deltaTime;
            float rightTrackSpeed = rightInputSpeed * -Time.deltaTime;

            if (mesh != null && mesh.Length > 0 && mesh[0] != null)
            {
                Material leftMaterial = mesh[0].material;
                Vector2 leftOffset = leftMaterial.mainTextureOffset;
                leftOffset.y += leftTrackSpeed;
                leftMaterial.mainTextureOffset = leftOffset;
            }

            if (mesh != null && mesh.Length > 1 && mesh[1] != null)
            {
                Material rightMaterial = mesh[1].material;
                Vector2 rightOffset = rightMaterial.mainTextureOffset;
                rightOffset.y += rightTrackSpeed;
                rightMaterial.mainTextureOffset = rightOffset;
            }

            if (leftWheels != null)
            {
                for (int i = 0; i < leftWheels.Length; i++)
                {
                    WheelSpinAnimator wheel = leftWheels[i];
                    if (wheel != null)
                    {
                        wheel.currentSpeed = leftInputSpeed;
                    }
                }
            }

            if (rightWheels != null)
            {
                for (int i = 0; i < rightWheels.Length; i++)
                {
                    WheelSpinAnimator wheel = rightWheels[i];
                    if (wheel != null)
                    {
                        wheel.currentSpeed = rightInputSpeed;
                    }
                }
            }
        }

        private void EnsureMovingInnerTrackSpeed(
            float forwardInput,
            ref float leftInputSpeed,
            ref float rightInputSpeed)
        {
            float leftMagnitude = Mathf.Abs(leftInputSpeed);
            float rightMagnitude = Mathf.Abs(rightInputSpeed);

            if (leftMagnitude >= rightMagnitude)
            {
                rightInputSpeed = GetMovingInnerTrackSpeed(forwardInput, rightInputSpeed, leftMagnitude);
                return;
            }

            leftInputSpeed = GetMovingInnerTrackSpeed(forwardInput, leftInputSpeed, rightMagnitude);
        }

        private float GetMovingInnerTrackSpeed(float forwardInput, float innerTrackSpeed, float outerTrackMagnitude)
        {
            float minInnerTrackMagnitude = outerTrackMagnitude * MovingInnerTrackMinSpeedRatio;
            if (Mathf.Abs(innerTrackSpeed) >= minInnerTrackMagnitude)
            {
                return innerTrackSpeed;
            }

            return Mathf.Sign(forwardInput) * minInnerTrackMagnitude;
        }
    }
}
