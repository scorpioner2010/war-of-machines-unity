using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    [DisallowMultipleComponent]
    public sealed class TrackedVehicleAnimator : MonoBehaviour, IVehicleRootAware
    {
        [System.Serializable]
        public sealed class Wheel
        {
            public Transform transform;
            public float rotationSpeed = 2500f;
        }

        private const float InputThreshold = 0.01f;
        private const float MovingInnerTrackMinSpeedRatio = 1f / 3f;

        public VehicleRoot vehicleRoot;
        public Renderer[] trackRenderers = System.Array.Empty<Renderer>();

        public float forwardBackwardSpeed = 1f;
        public float turnInPlaceSpeed = 0.7f;
        public float turnWhileMovingSpeed = 0.5f;

        public Wheel[] leftWheels = System.Array.Empty<Wheel>();
        public Wheel[] rightWheels = System.Array.Empty<Wheel>();

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Update()
        {
            if (vehicleRoot == null
                || vehicleRoot.inputManager == null
                || (vehicleRoot.health != null && vehicleRoot.health.IsDead))
            {
                return;
            }

            Vector2 movement = vehicleRoot.inputManager.AnimMove;
            float forwardInput = movement.y;
            float turnInput = forwardInput < 0f ? -movement.x : movement.x;

            ResolveTrackSpeeds(forwardInput, turnInput, out float leftSpeed, out float rightSpeed);
            AnimateTrackTextures(leftSpeed, rightSpeed);
            RotateWheels(leftWheels, leftSpeed);
            RotateWheels(rightWheels, rightSpeed);
        }

        private void ResolveTrackSpeeds(
            float forwardInput,
            float turnInput,
            out float leftSpeed,
            out float rightSpeed)
        {
            leftSpeed = 0f;
            rightSpeed = 0f;

            if (Mathf.Abs(forwardInput) > InputThreshold && Mathf.Abs(turnInput) < InputThreshold)
            {
                leftSpeed = forwardInput * forwardBackwardSpeed;
                rightSpeed = forwardInput * forwardBackwardSpeed;
            }
            else if (Mathf.Abs(forwardInput) < InputThreshold && Mathf.Abs(turnInput) > InputThreshold)
            {
                leftSpeed = turnInput * turnInPlaceSpeed;
                rightSpeed = -turnInput * turnInPlaceSpeed;
            }
            else if (Mathf.Abs(forwardInput) > InputThreshold && Mathf.Abs(turnInput) > InputThreshold)
            {
                leftSpeed = (forwardInput + turnInput) * turnWhileMovingSpeed;
                rightSpeed = (forwardInput - turnInput) * turnWhileMovingSpeed;
                EnsureMovingInnerTrackSpeed(forwardInput, ref leftSpeed, ref rightSpeed);
            }
        }

        private void AnimateTrackTextures(float leftSpeed, float rightSpeed)
        {
            if (trackRenderers != null && trackRenderers.Length > 0 && trackRenderers[0] != null)
            {
                Material leftMaterial = trackRenderers[0].material;
                Vector2 offset = leftMaterial.mainTextureOffset;
                offset.y -= leftSpeed * Time.deltaTime;
                leftMaterial.mainTextureOffset = offset;
            }

            if (trackRenderers != null && trackRenderers.Length > 1 && trackRenderers[1] != null)
            {
                Material rightMaterial = trackRenderers[1].material;
                Vector2 offset = rightMaterial.mainTextureOffset;
                offset.y -= rightSpeed * Time.deltaTime;
                rightMaterial.mainTextureOffset = offset;
            }
        }

        private static void RotateWheels(Wheel[] wheels, float inputSpeed)
        {
            if (wheels == null || Mathf.Abs(inputSpeed) <= 0.001f)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < wheels.Length; i++)
            {
                Wheel wheel = wheels[i];
                if (wheel == null || wheel.transform == null || wheel.transform.parent == null)
                {
                    continue;
                }

                wheel.transform.Rotate(Vector3.back * inputSpeed * deltaTime * wheel.rotationSpeed);
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

        private static float GetMovingInnerTrackSpeed(
            float forwardInput,
            float innerTrackSpeed,
            float outerTrackMagnitude)
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
