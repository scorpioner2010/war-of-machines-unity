using Game.Scripts.AI.WaypointGraph;
using Game.Scripts.Networking.Lobby;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleBotBrain : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;

        private BotNavigator _navigator;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void StartBrain(VehicleRoot root, ServerRoom room)
        {
            SetVehicleRoot(root);

            WaypointGraphRuntime graph = root != null
                ? WaypointGraphRuntime.FindOrCreateForScene(root.gameObject.scene)
                : null;

            _navigator = GetComponent<BotNavigator>();
            if (_navigator == null)
            {
                _navigator = gameObject.AddComponent<BotNavigator>();
            }

            _navigator.Initialize(root, room, graph);
        }

        private void OnDisable()
        {
            if (_navigator != null)
            {
                _navigator.Stop();
            }
        }
    }
}
