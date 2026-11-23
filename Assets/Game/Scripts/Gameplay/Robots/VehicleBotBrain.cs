using RAV.Scripts.AI.WaypointGraph;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Networking.Lobby;
using UnityEngine;

namespace RAV.Scripts.Gameplay.Robots
{
    public class VehicleBotBrain : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        public BotNavigator navigator;

        private readonly BotCombatController _combatController = new BotCombatController();
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public VehicleRoot CurrentTargetRoot => _combatController.TargetRoot;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void StartBrain(VehicleRoot root, ServerRoom room)
        {
            if (_isRunning)
            {
                GameplayRuntimeDiagnostics.Record("bot_brain_start_skipped", "reason", "already_running", "vehicle", root != null ? root.name : string.Empty);
                return;
            }

            SetVehicleRoot(root);
            GameplayRuntimeDiagnostics.Record(
                "bot_brain_start_requested",
                "vehicle", root != null ? root.name : string.Empty,
                "room_id", room != null ? room.roomId : string.Empty,
                "position", root != null ? root.transform.position : Vector3.zero);

            WaypointGraphRuntime graph = root != null
                ? WaypointGraphRuntime.FindOrCreateForScene(root.gameObject.scene)
                : null;

            if (navigator == null)
            {
                GameplayRuntimeDiagnostics.Record("bot_brain_start_failed", "reason", "navigator_missing", "vehicle", root != null ? root.name : string.Empty);
                Debug.LogError("[VehicleBotBrain] BotNavigator reference is not assigned on vehicle prefab.", this);
                return;
            }

            navigator.Initialize(root, room, graph);
            _combatController.Initialize(root, room, navigator);
            _isRunning = true;
            GameplayRuntimeDiagnostics.Record(
                "bot_brain_started",
                "vehicle", root != null ? root.name : string.Empty,
                "room_id", room != null ? room.roomId : string.Empty,
                "position", root != null ? root.transform.position : Vector3.zero);
        }

        public void StopBrain()
        {
            if (!_isRunning)
            {
                return;
            }

            GameplayRuntimeDiagnostics.Record(
                "bot_brain_stopped",
                "vehicle", vehicleRoot != null ? vehicleRoot.name : string.Empty,
                "position", vehicleRoot != null ? vehicleRoot.transform.position : Vector3.zero);
            _combatController.Stop();
            _isRunning = false;

            if (navigator != null)
            {
                navigator.Stop();
            }
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
            StopBrain();
        }
    }
}
