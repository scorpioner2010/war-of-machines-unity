using Game.Scripts.Diagnostics;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class PositionSync : MonoBehaviour
    {
        public Transform target;

        private void LateUpdate()
        {
            using (ProfileScope.Measure("Client.Interpolation.PositionSync.LateUpdate", DiagnosticsCategories.Client))
            {
                if (target == null)
                {
                    return;
                }

                transform.position = target.position;
            }
        }
    }
}
