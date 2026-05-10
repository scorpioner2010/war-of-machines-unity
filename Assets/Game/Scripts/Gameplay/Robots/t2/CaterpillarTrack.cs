using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class CaterpillarTrack : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        public Renderer[] mesh;

        public float forwardBackwardSpeed = 1.0f;
        public float turnInPlaceSpeed = 0.7f;
        public float turnWhileMovingSpeed = 0.5f;

        public RotateObject[] rightWheels;
        public RotateObject[] leftWheels;

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

            if (Mathf.Abs(forwardInput) > 0.01f && Mathf.Abs(turnInput) < 0.01f)
            {
                leftInputSpeed = forwardInput * forwardBackwardSpeed;
                rightInputSpeed = forwardInput * forwardBackwardSpeed;
            }
            else if (Mathf.Abs(forwardInput) < 0.01f && Mathf.Abs(turnInput) > 0.01f)
            {
                leftInputSpeed = turnInput * turnInPlaceSpeed;
                rightInputSpeed = -turnInput * turnInPlaceSpeed;
            }
            else if (Mathf.Abs(forwardInput) > 0.01f && Mathf.Abs(turnInput) > 0.01f)
            {
                leftInputSpeed = (forwardInput + turnInput) * turnWhileMovingSpeed;
                rightInputSpeed = (forwardInput - turnInput) * turnWhileMovingSpeed;
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
                    RotateObject wheel = leftWheels[i];
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
                    RotateObject wheel = rightWheels[i];
                    if (wheel != null)
                    {
                        wheel.currentSpeed = rightInputSpeed;
                    }
                }
            }
        }
    }
}
