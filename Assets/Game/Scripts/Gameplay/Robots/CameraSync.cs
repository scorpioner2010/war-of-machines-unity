using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-90)]
    public class CameraSync : MonoBehaviour
    {
        public static CameraSync In;
        public Transform target;
        public Camera gameplayCamera;
        
        private void Awake()
        {
            In = this;
        }

        public Transform GetAimTransform()
        {
            if (target != null)
            {
                return target;
            }

            return transform;
        }

        public Quaternion GetAimRotation()
        {
            Transform aimTransform = GetAimTransform();
            return aimTransform != null ? aimTransform.rotation : transform.rotation;
        }

        public Vector3 GetAimForward()
        {
            return GetAimRotation() * Vector3.forward;
        }

        public void SyncToTarget()
        {
            if (target != null)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
            }
        }

        private void LateUpdate()
        {
            SyncToTarget();
        }
    }
}
