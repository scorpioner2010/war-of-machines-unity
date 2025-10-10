using UnityEngine;

namespace Game.Scripts.UI.Tree
{
    public class TreeGrid : MonoBehaviour
    {
        public Vehicle vehicleType;

        public void Init(Vehicle vehicle)
        {
            vehicleType = vehicle;
        }
    }
}