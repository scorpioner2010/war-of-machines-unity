using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2 
{
    public class RotateObject : MonoBehaviour
    {
        public float speed;
        public float currentSpeed;  // динамічна швидкість обертання

        private void Update() 
        {
            if (Mathf.Abs(currentSpeed) > 0.001f)
            {
                transform.Rotate(Vector3.back * currentSpeed * Time.deltaTime * speed);
            }
        }
    }
}