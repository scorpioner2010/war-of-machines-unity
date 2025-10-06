using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class CameraController : MonoBehaviour
    {
        public Transform rig;  // Ссылка на игрока
        public float distance = 10.0f;  // Расстояние от игрока
        public float xSpeed = 120.0f;  // Скорость вращения по горизонтали
        public float ySpeed = 120.0f;  // Скорость вращения по вертикали

        private float _X;
        private float _Y;

        public void Init()
        {
            Vector3 angles = transform.eulerAngles;
            _X = angles.y;
            _Y = angles.x;

            if (CameraSync.In != null)
            {
                CameraSync.In.target = transform;
            }
        }

        private void Update()
        {
            if (rig)
            {
                _X += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                _Y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                _Y = Mathf.Clamp(_Y, -20, 80);  // Ограничение углов по вертикали

                Quaternion rotation = Quaternion.Euler(_Y, _X, 0);
                Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + rig.position;

                transform.rotation = rotation;
                transform.position = position;
            }
        }
    }
}
