using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class BodyBobbing : MonoBehaviour
    {
        public TankRoot playerRoot;
        
        public float bobbingAmplitude = 0.05f;
        public float bobbingFrequency = 1.0f;
        public float inputThreshold = 0.01f;
        public bool isWalkingBobbing = true;

        private Vector3 _initialLocalPosition;

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            bool isWalking = Mathf.Abs(playerRoot.inputManager.AnimMove.y) > inputThreshold;
            bool isRotating = Mathf.Abs(playerRoot.inputManager.AnimMove.x) > inputThreshold;
        
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
    }
}