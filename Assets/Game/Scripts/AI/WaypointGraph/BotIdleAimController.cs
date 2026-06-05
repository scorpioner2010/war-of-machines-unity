using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotIdleAimController
    {
        private readonly BotAimController _aimController;
        private readonly BotCombatInputWriter _inputWriter;

        public BotIdleAimController(BotAimController aimController, BotCombatInputWriter inputWriter)
        {
            _aimController = aimController;
            _inputWriter = inputWriter;
        }

        public void ApplyNoTargetTravelAim(VehicleRoot vehicleRoot, BotNavigator navigator, BotCombatSettings settings)
        {
            if (!settings.aimAlongTravelDirectionWhenNoTarget)
            {
                _inputWriter.ClearCombatInput(vehicleRoot);
                return;
            }

            if (!TryResolveNoTargetAimDirection(vehicleRoot, navigator, settings, out Vector3 aimDirection))
            {
                _inputWriter.ClearCombatInput(vehicleRoot);
                return;
            }

            Vector3 aimPoint = BotCombatUtility.GetAimOrigin(vehicleRoot) + aimDirection * settings.noTargetTravelAimDistance;
            VehicleAimInputResult aimResult = _aimController.SolveAim(vehicleRoot, aimPoint, aimDirection);
            if (!aimResult.HasState)
            {
                _inputWriter.ClearCombatInput(vehicleRoot);
                return;
            }

            _inputWriter.ApplyCombatInput(vehicleRoot, aimResult, false, vehicleRoot.inputManager.Move);
        }

        private static bool TryResolveNoTargetAimDirection(
            VehicleRoot vehicleRoot,
            BotNavigator navigator,
            BotCombatSettings settings,
            out Vector3 direction)
        {
            direction = Vector3.zero;
            if (navigator != null
                && navigator.TryGetDesiredTravelDirection(out direction, settings.noTargetTravelDirectionMaxAgeSeconds))
            {
                return true;
            }

            if (vehicleRoot == null || vehicleRoot.inputManager == null)
            {
                return false;
            }

            Transform moveTransform = BotCombatUtility.GetMoveTransform(vehicleRoot);
            if (moveTransform == null)
            {
                return false;
            }

            Vector2 move = vehicleRoot.inputManager.Move;
            if (Mathf.Abs(move.y) > 0.025f)
            {
                direction += moveTransform.forward * Mathf.Sign(move.y);
            }

            if (Mathf.Abs(move.x) > 0.025f)
            {
                direction += moveTransform.right * Mathf.Sign(move.x) * 0.35f;
            }

            if (direction.sqrMagnitude <= 0.0001f && settings.aimForwardWhenNoTargetIdle)
            {
                direction = moveTransform.forward;
            }

            direction.y = 0f;
            if (!BotCombatUtility.IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }
    }
}
