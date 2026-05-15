using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class TankPitchCompatibilityComponent : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Update()
        {
            // Intentionally left blank. This component is kept only for prefab compatibility.
        }
    }
}
