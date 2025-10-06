using UnityEngine;

namespace Game.Scripts.Player.Camera
{
    public class CameraCollision : MonoBehaviour
    {
        public UnityEngine.Camera mainCamera;
        public GameObject focusPoint;
        [Range(0.1f, 3.0f)]
        public float cushionOffset = 1.0f;
        public LayerMask maskedLayers;

        private void Start()
        {
            if (mainCamera == null)
            {
                UnityEngine.Debug.LogError("Основна камера не призначена!");
                return;
            }
            if (focusPoint == null)
            {
                UnityEngine.Debug.LogError("Точка фокусування не призначена!");
                return;
            }
            mainCamera.nearClipPlane = 0.01f;
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }
            if (focusPoint == null)
            {
                return;
            }
            
            Vector3 focusPos = focusPoint.transform.position;
            Vector3 camPos = mainCamera.transform.position;
            Vector3 direction = camPos - focusPos;
            float distance = direction.magnitude;
            if (distance > 0)
                direction /= distance;

            if (Physics.Raycast(focusPos, direction, out RaycastHit hit, distance + cushionOffset, maskedLayers))
            {
                float hitDistance = Vector3.Distance(hit.point, focusPos);
                Vector3 newPos = (hitDistance < cushionOffset)
                    ? focusPos
                    : hit.point - direction * cushionOffset;

                mainCamera.transform.position = newPos;
            }
        }
    }
}