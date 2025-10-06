using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class SimpleCarPitch : MonoBehaviour
    {
        public TankRoot tankRoot;
        
        public float maxPitchAngle = 10f;
        public float accelerationMultiplier = 0.1f;
    
        public float springConstant = 50f;
        public float damping = 0.95f;
    
        public float accelerationTrigger = 1f;
        public float accelerationThreshold = 0.1f;
        
        private float _currentPitch = 0f;
        private float _pitchVelocity = 0f;
        private float _lastSpeed = 0f;

        private void Update()
        {
            if (tankRoot.IsServer == false)
            {
                return;
            }
            
            
            /*
            
            float dt = Time.deltaTime;
            
            float speed = tankRoot.objectMover.rb.velocity.magnitude;
            float acceleration = (speed - _lastSpeed) / dt;
            
            _lastSpeed = speed;

            if (Mathf.Abs(acceleration) < accelerationThreshold)
            {
                acceleration = 0f;
            }

            float targetPitch = 0f;
            
            if (Mathf.Abs(acceleration) > accelerationTrigger)
            {
                if (acceleration > 0)
                {
                    targetPitch = -Mathf.Clamp(acceleration * accelerationMultiplier, 0, maxPitchAngle);
                }
                else
                {
                    targetPitch = Mathf.Clamp(-acceleration * accelerationMultiplier, 0, maxPitchAngle);
                }
            }
            else
            {
                targetPitch = 0f;
            }
            
            float force = (targetPitch - _currentPitch) * springConstant;
            _pitchVelocity += force * dt;
            _pitchVelocity *= damping;
            _currentPitch += _pitchVelocity * dt;
            
            if (Mathf.Abs(_currentPitch) < 0.01f && Mathf.Abs(_pitchVelocity) < 0.01f)
            {
                _currentPitch = 0f;
                _pitchVelocity = 0f;
            }
            
            Vector3 euler = transform.localEulerAngles;
            euler.x = _currentPitch;
            transform.localEulerAngles = euler;
            
            */
        }
    }
}
