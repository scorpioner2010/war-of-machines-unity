using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal enum BotCombatEngagementPhase
    {
        None,
        Approach,
        Settle,
        ReadyToFire,
        Reloading,
        Repositioning
    }

    internal struct BotCombatEngagementDecision
    {
        public BotCombatEngagementPhase Phase;
        public bool HoldPosition;
        public bool AllowFire;
    }

    internal sealed class BotCombatEngagementController
    {
        private const float SettleDurationSeconds = 0.22f;
        private const float StationaryForwardSpeed = 0.15f;
        private const float StationaryTraverseSpeed = 5f;

        private VehicleRoot _targetRoot;
        private BotCombatEngagementPhase _phase;
        private float _settleStartedAt = -1f;

        public void Reset()
        {
            _targetRoot = null;
            _phase = BotCombatEngagementPhase.None;
            _settleStartedAt = -1f;
        }

        public BotCombatEngagementDecision Tick(
            VehicleRoot vehicleRoot,
            VehicleRoot targetRoot,
            bool targetIsDirectlySpotted,
            bool hasLineOfFire,
            bool hasAimSolution,
            bool tacticAllowsFire,
            bool weaponCanFire,
            float now)
        {
            if (targetRoot == null)
            {
                Reset();
                return BuildDecision();
            }

            if (_targetRoot != targetRoot)
            {
                _targetRoot = targetRoot;
                _phase = BotCombatEngagementPhase.None;
                _settleStartedAt = -1f;
            }

            if (!weaponCanFire)
            {
                _phase = BotCombatEngagementPhase.Reloading;
                _settleStartedAt = -1f;
                return BuildDecision();
            }

            bool canSettle = targetIsDirectlySpotted
                             && hasLineOfFire
                             && hasAimSolution
                             && tacticAllowsFire;
            if (!canSettle)
            {
                _phase = targetIsDirectlySpotted
                    ? BotCombatEngagementPhase.Repositioning
                    : BotCombatEngagementPhase.Approach;
                _settleStartedAt = -1f;
                return BuildDecision();
            }

            if (_phase != BotCombatEngagementPhase.Settle
                && _phase != BotCombatEngagementPhase.ReadyToFire)
            {
                _phase = BotCombatEngagementPhase.Settle;
                _settleStartedAt = now;
            }

            if (_phase == BotCombatEngagementPhase.Settle
                && HasSettled(vehicleRoot, now, _settleStartedAt))
            {
                _phase = BotCombatEngagementPhase.ReadyToFire;
            }

            return BuildDecision();
        }

        public void MarkShot()
        {
            _phase = BotCombatEngagementPhase.Reloading;
            _settleStartedAt = -1f;
        }

        internal static bool HasSettled(VehicleRoot vehicleRoot, float now, float settleStartedAt)
        {
            if (settleStartedAt < 0f || now - settleStartedAt < SettleDurationSeconds)
            {
                return false;
            }

            if (vehicleRoot == null || vehicleRoot.objectMover == null)
            {
                return true;
            }

            return IsStationary(
                vehicleRoot.objectMover.CurrentForwardSpeed,
                vehicleRoot.objectMover.CurrentTraverseSpeedDegPerSecond);
        }

        internal static bool IsStationary(float forwardSpeed, float traverseSpeedDegPerSecond)
        {
            return Mathf.Abs(forwardSpeed) <= StationaryForwardSpeed
                   && Mathf.Abs(traverseSpeedDegPerSecond) <= StationaryTraverseSpeed;
        }

        private BotCombatEngagementDecision BuildDecision()
        {
            bool holdPosition = _phase == BotCombatEngagementPhase.Settle
                                || _phase == BotCombatEngagementPhase.ReadyToFire;
            return new BotCombatEngagementDecision
            {
                Phase = _phase,
                HoldPosition = holdPosition,
                AllowFire = _phase == BotCombatEngagementPhase.ReadyToFire
            };
        }
    }
}
