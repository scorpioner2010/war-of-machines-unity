using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotFireDecision
    {
        private readonly BotAimController _aimController;

        public BotFireDecision(BotAimController aimController)
        {
            _aimController = aimController;
        }

        public bool CanShootAtTarget(
            VehicleRoot vehicleRoot,
            BotCombatState state,
            VehicleAimInputResult aimResult,
            Vector3 aimPoint,
            BotCombatSettings settings,
            float now)
        {
            if (now - state.TargetAcquiredTime < settings.minTargetHoldBeforeFire || now < state.FireAllowedTime)
            {
                return false;
            }

            if (vehicleRoot.weaponReloadController == null || !vehicleRoot.weaponReloadController.ServerCanFire)
            {
                return false;
            }

            if (vehicleRoot.shooterNet == null)
            {
                return false;
            }

            if (!_aimController.IsAimAligned(vehicleRoot, aimResult, aimPoint, settings))
            {
                return false;
            }

            return IsDispersionReady(vehicleRoot, settings);
        }

        private static bool IsDispersionReady(VehicleRoot vehicleRoot, BotCombatSettings settings)
        {
            if (vehicleRoot == null || vehicleRoot.shooterNet == null)
            {
                return false;
            }

            float minimumDispersion = Mathf.Max(0f, vehicleRoot.shooterNet.MinDispersionDeg);
            float currentDispersion = Mathf.Max(minimumDispersion, vehicleRoot.shooterNet.ServerCurrentDispersionDeg);
            float allowedDispersion = Mathf.Max(
                minimumDispersion * Mathf.Max(1f, settings.maxFireDispersionMultiplier),
                minimumDispersion + Mathf.Max(0f, settings.maxFireDispersionAddDeg));
            return currentDispersion <= allowedDispersion + 0.001f;
        }
    }
}
