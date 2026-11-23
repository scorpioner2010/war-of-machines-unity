using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public class TreeGrid : MonoBehaviour
    {
        public VehicleClass vehicleType;

        public void Init(VehicleClass vehicle)
        {
            vehicleType = vehicle;
        }
    }
}
