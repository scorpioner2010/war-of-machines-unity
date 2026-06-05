using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotCombatNavigationController
    {
        private const float TargetPositionUpdateDistance = 1f;

        private BotNavigator _navigator;
        private VehicleRoot _navigationTargetRoot;
        private Vector3 _navigationTargetPosition;
        private bool _hasNavigationTarget;

        public void Initialize(BotNavigator navigator)
        {
            _navigator = navigator;
            _navigationTargetRoot = null;
            _navigationTargetPosition = Vector3.zero;
            _hasNavigationTarget = false;
        }

        public void UpdateForTarget(BotCombatSettings settings, VehicleRoot targetRoot, Vector3 mapPosition, bool holdPosition)
        {
            if (_navigator == null)
            {
                return;
            }

            _navigator.SetMovementSuppressed(holdPosition);
            if (holdPosition)
            {
                ClearNavigationTarget();
                return;
            }

            if (!settings.moveTowardTarget || targetRoot == null)
            {
                ClearNavigationTarget();
                return;
            }

            if (_hasNavigationTarget
                && _navigationTargetRoot == targetRoot
                && HasSameNavigationPosition(mapPosition))
            {
                return;
            }

            _navigationTargetRoot = targetRoot;
            _navigationTargetPosition = mapPosition;
            _hasNavigationTarget = true;
            _navigator.SetTargetPosition(mapPosition);
        }

        public void ReleaseControl()
        {
            if (_navigator != null)
            {
                _navigator.SetMovementSuppressed(false);
            }

            ClearNavigationTarget();
        }

        private bool HasSameNavigationPosition(Vector3 mapPosition)
        {
            Vector3 delta = mapPosition - _navigationTargetPosition;
            delta.y = 0f;
            return delta.sqrMagnitude < TargetPositionUpdateDistance * TargetPositionUpdateDistance;
        }

        private void ClearNavigationTarget()
        {
            if (_navigator != null && _hasNavigationTarget)
            {
                _navigator.SetTarget(null);
            }

            _navigationTargetRoot = null;
            _navigationTargetPosition = Vector3.zero;
            _hasNavigationTarget = false;
        }
    }
}
