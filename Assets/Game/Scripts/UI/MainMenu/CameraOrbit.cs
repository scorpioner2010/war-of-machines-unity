using UnityEngine;

namespace Game.Scripts.UI.MainMenu
{
    public class CameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 5.0f;
        [SerializeField] private float heightOffset = 2.0f;

        [SerializeField] private float xSpeed = 0.2f; // чутливість по X (пікселі → градуси)
        [SerializeField] private float ySpeed = 0.2f; // чутливість по Y
        [SerializeField] private float yMinLimit = -20f;
        [SerializeField] private float yMaxLimit = 80f;

        [Header("Drag Area")]
        [SerializeField] private UIOrbitDragArea dragArea;

        private float _x;
        private float _y;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _x = angles.y;
            _y = angles.x;
        }

        private void LateUpdate()
        {
            if (target == null) { return; }

            if (dragArea != null && dragArea.IsDragging)
            {
                // Отримуємо дельту з UI (в пікселях) і конвертуємо в кути
                Vector2 delta = dragArea.ConsumeDelta();
                _x += delta.x * xSpeed;
                _y -= delta.y * ySpeed;
                _y = Mathf.Clamp(_y, yMinLimit, yMaxLimit);
            }

            Quaternion rotation = Quaternion.Euler(_y, _x, 0f);
            Vector3 position = target.position + Vector3.up * heightOffset + rotation * new Vector3(0f, 0f, -distance);

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}