using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotCombatInputWriter
    {
        public void ApplyCombatInput(VehicleRoot vehicleRoot, VehicleAimInputResult aimResult, bool shoot, Vector2 move)
        {
            if (vehicleRoot == null || vehicleRoot.inputManager == null)
            {
                return;
            }

            VehicleServerInput input = VehicleServerInput.Combat(
                move,
                shoot,
                false,
                aimResult.YawDeg,
                aimResult.PitchDeg,
                aimResult.CameraAimPoint,
                aimResult.CameraAimForward);

            vehicleRoot.inputManager.ServerSetExternalInput(input, true);
        }

        public void ClearCombatInput(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || vehicleRoot.inputManager == null || !vehicleRoot.inputManager.IsServerInitialized)
            {
                return;
            }

            VehicleServerInput input = VehicleServerInput.Movement(vehicleRoot.inputManager.Move);
            vehicleRoot.inputManager.ServerSetExternalInput(input, true);
        }
    }
}
