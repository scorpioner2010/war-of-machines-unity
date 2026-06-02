using Game.Scripts.AI.WaypointGraph;
using Game.Scripts.Networking.Lobby;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleBotBrain : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        public BotNavigator navigator;

        private readonly BotCombatController _combatController = new BotCombatController();
        private bool _isRunning;

        public bool IsRunning => _isRunning;

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

            if (navigator == null)
            {
                Debug.LogError("[VehicleBotBrain] BotNavigator reference is not assigned on vehicle prefab.", this);
                return;
            }

            navigator.Initialize(root, room, graph);
            _combatController.Initialize(root, room, navigator);
            _isRunning = true;
        }

        private void FixedUpdate()
        {
            if (!_isRunning)
            {
                return;
            }

            _combatController.Tick(Time.time);
        }

        private void OnDisable()
        {
            _combatController.Stop();
            _isRunning = false;

            if (navigator != null)
            {
                navigator.Stop();
            }
        }
    }
}
