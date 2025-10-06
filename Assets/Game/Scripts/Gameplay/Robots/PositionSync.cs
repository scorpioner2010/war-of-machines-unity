using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class PositionSync : MonoBehaviour
    {
        public Transform target;

        private void LateUpdate()
        {
            transform.position = target.position;
        }
    }
}
