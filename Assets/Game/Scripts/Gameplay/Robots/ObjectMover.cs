using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class ObjectMover : MonoBehaviour
    {
        public TankRoot tankRoot;
        public CharacterController controller;

        public float rotateSpeed = 2f;
        public float acceleration = 30f;
        public float maxSpeed = 10f;
        public float gravity = 25f;
        public float groundedSnap = 2f;

        private Vector3 _hVel;
        private float _vVel;

        private void FixedUpdate()
        {
            // Тільки сервер рухає; клієнти отримують NetworkTransform
            if (!tankRoot.IsServer)
            {
                return;
            }

            Vector2 mi = tankRoot.inputManager.Move;
            Rotate(mi);

            Vector3 desired = transform.forward * (mi.y * maxSpeed);

            float dt = Time.fixedDeltaTime;
            Vector3 delta = desired - _hVel;
            Vector3 step = Vector3.ClampMagnitude(delta, acceleration * dt);
            _hVel += step;

            if (_hVel.magnitude > maxSpeed)
                _hVel = _hVel.normalized * maxSpeed;

            bool grounded = controller.isGrounded;
            _vVel = grounded ? -groundedSnap : _vVel - gravity * dt;

            Vector3 move = new Vector3(_hVel.x, _vVel, _hVel.z) * dt;
            controller.Move(move);
        }

        private void Rotate(Vector2 mi)
        {
            if (mi.x != 0f)
                transform.Rotate(Vector3.up * mi.x * rotateSpeed);
        }
    }
}