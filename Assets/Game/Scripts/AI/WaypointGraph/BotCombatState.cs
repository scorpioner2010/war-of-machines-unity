using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotCombatState
    {
        public VehicleRoot TargetRoot { get; private set; }
        public Vector3 TargetMapPosition { get; private set; }
        public Vector3 TargetAimOffset { get; private set; }
        public bool TargetIsDirectlySpotted { get; private set; }
        public float NextThinkTime { get; private set; }
        public float NextScanTime { get; private set; }
        public float TargetAcquiredTime { get; private set; }
        public float FireAllowedTime { get; private set; }

        public void InitializeTimings(BotCombatSettings settings, float now)
        {
            NextThinkTime = now + Random.Range(0f, settings.thinkInterval);
            NextScanTime = now + Random.Range(0f, settings.targetScanInterval);
        }

        public void ScheduleNextThink(BotCombatSettings settings, float now)
        {
            NextThinkTime = now + settings.thinkInterval;
        }

        public void ScheduleNextScan(BotCombatSettings settings, float now)
        {
            NextScanTime = now + settings.targetScanInterval;
        }

        public void SetTarget(
            VehicleRoot targetRoot,
            Vector3 mapPosition,
            bool isDirectlySpotted,
            BotCombatSettings settings,
            float now)
        {
            TargetRoot = targetRoot;
            TargetMapPosition = mapPosition;
            TargetIsDirectlySpotted = isDirectlySpotted;
            TargetAimOffset = BotCombatUtility.BuildAimOffset(settings);
            TargetAcquiredTime = now;
            FireAllowedTime = now + Random.Range(settings.reactionDelayMin, settings.reactionDelayMax);
        }

        public void RefreshTargetMapVisibility(Vector3 mapPosition, bool isDirectlySpotted)
        {
            TargetMapPosition = mapPosition;
            TargetIsDirectlySpotted = isDirectlySpotted;
        }

        public void ClearTarget()
        {
            TargetRoot = null;
            TargetMapPosition = Vector3.zero;
            TargetAimOffset = Vector3.zero;
            TargetIsDirectlySpotted = false;
            TargetAcquiredTime = 0f;
            FireAllowedTime = 0f;
        }
    }
}
