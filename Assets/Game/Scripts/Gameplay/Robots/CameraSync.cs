using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class CameraSync : MonoBehaviour
    {
        public static CameraSync In;
        public Transform target;
        public Camera gameplayCamera;
        
        private void Awake()
        {
            In = this;
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position;
                transform.rotation = target.rotation;
            }
        }
    }
}
