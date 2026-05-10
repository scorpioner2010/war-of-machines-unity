using Game.Scripts.UI.Robots;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class UISenerd : MonoBehaviour
    {
        public VehicleRoot vehicleRoot;
        public bool isActive;

        private Vector3 _prevPos;

        public void Init()
        {
            isActive = true;
            _prevPos = vehicleRoot.objectMover.transform.position;
        }

        private void Update()
        {
            if (!isActive || !vehicleRoot.IsOwner)
            {
                return;
            }

            Transform t = vehicleRoot.objectMover.transform;
            Vector3 delta = t.position - _prevPos;
            float speed = new Vector2(delta.x, delta.z).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            _prevPos = t.position;

            int kmh = Mathf.RoundToInt(speed * 3.6f);
            SpeedHud.SetText(kmh.ToString());
        }
    }
}
