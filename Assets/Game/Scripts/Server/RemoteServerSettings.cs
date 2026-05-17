using Game.Scripts.Gameplay.Robots;

namespace Game.Scripts.Server
{
    public static class RemoteServerSettings
    {
        public static int MaxPlayersForFindRoom { get; private set; } = 1;
        public static int FindRoomSeconds { get; private set; } = 60;
        public static GunDispersionGlobalSettings GunDispersion { get; } = new GunDispersionGlobalSettings();
        public static RobotMovementGlobalSettings RobotMovement { get; } = new RobotMovementGlobalSettings();
        public static ProjectileBallisticsGlobalSettings ProjectileBallistics { get; } = new ProjectileBallisticsGlobalSettings();
        public static bool IsLoaded { get; private set; }

        public static void Apply(
            int maxPlayersForFindRoom,
            int findRoomSeconds,
            float robotFallbackMaxSpeed,
            float robotFallbackAcceleration,
            float robotFallbackTraverseSpeed,
            float robotStoppingAccelerationMultiplier,
            float robotStandardAccelerationMultiplier,
            float robotStandardBrakingMultiplier,
            float robotLeggedAccelerationMultiplier,
            float robotLeggedBrakingMultiplier,
            float robotLeggedAnimationReferenceSpeed,
            float robotLeggedAnimationMinSpeedMultiplier,
            float robotLeggedAnimationMaxSpeedMultiplier,
            float robotLeggedAnimationSpeedExponent,
            float robotLeggedStepDistanceMultiplier,
            float robotLeggedStepHeightMultiplier,
            float robotLeggedTurnStepDurationMultiplier,
            float robotLeggedTransitionSpeedMultiplier,
            float robotGravity,
            float robotGroundedSnap,
            bool gunDispersionEnabled,
            float gunDispersionExpandTime,
            float gunDispersionReferenceHullTraverse,
            float gunDispersionReferenceTurretTraverse,
            float gunDispersionReferenceGunTraverse,
            float gunDispersionReferenceCameraAim,
            float gunDispersionAccuracyReferenceDistanceMeters,
            float gunDispersionUiMinDiameter,
            float gunDispersionUiMaxDiameter,
            float gunDispersionUiFullyAimedPixelsPerDegreeAtMaxZoom,
            float gunDispersionUiFullyAimedPixelsPerDegreeAtMaxDistance,
            float gunDispersionUiBloomPixelsPerDegreeAtMaxZoom,
            float gunDispersionUiBloomPixelsPerDegreeAtMaxDistance,
            float gunDispersionUiDiameterLerpSpeed,
            float gunDispersionServerSyncInterval,
            float gunDispersionServerSyncDeadZoneDeg,
            float projectileGravity,
            bool projectileUseBallisticCompensation,
            bool projectilePreferHighArc,
            bool projectileDebugBallisticTrajectory)
        {
            MaxPlayersForFindRoom = maxPlayersForFindRoom > 0 ? maxPlayersForFindRoom : 1;
            FindRoomSeconds = findRoomSeconds > 0 ? findRoomSeconds : 60;

            RobotMovement.fallbackMaxSpeed = robotFallbackMaxSpeed > 0f ? robotFallbackMaxSpeed : RobotMovementGlobalSettings.Default.fallbackMaxSpeed;
            RobotMovement.fallbackAcceleration = robotFallbackAcceleration > 0f ? robotFallbackAcceleration : RobotMovementGlobalSettings.Default.fallbackAcceleration;
            RobotMovement.fallbackTraverseSpeedDegPerSecond = robotFallbackTraverseSpeed >= 0f ? robotFallbackTraverseSpeed : RobotMovementGlobalSettings.Default.fallbackTraverseSpeedDegPerSecond;
            RobotMovement.stoppingAccelerationMultiplier = robotStoppingAccelerationMultiplier > 0f ? robotStoppingAccelerationMultiplier : RobotMovementGlobalSettings.Default.stoppingAccelerationMultiplier;
            RobotMovement.standardAccelerationMultiplier = robotStandardAccelerationMultiplier > 0f ? robotStandardAccelerationMultiplier : RobotMovementGlobalSettings.Default.standardAccelerationMultiplier;
            RobotMovement.standardBrakingMultiplier = robotStandardBrakingMultiplier > 0f ? robotStandardBrakingMultiplier : RobotMovementGlobalSettings.Default.standardBrakingMultiplier;
            RobotMovement.leggedAccelerationMultiplier = robotLeggedAccelerationMultiplier > 0f ? robotLeggedAccelerationMultiplier : RobotMovementGlobalSettings.Default.leggedAccelerationMultiplier;
            RobotMovement.leggedBrakingMultiplier = robotLeggedBrakingMultiplier > 0f ? robotLeggedBrakingMultiplier : RobotMovementGlobalSettings.Default.leggedBrakingMultiplier;
            RobotMovement.leggedAnimationReferenceSpeed = robotLeggedAnimationReferenceSpeed > 0f ? robotLeggedAnimationReferenceSpeed : RobotMovementGlobalSettings.Default.leggedAnimationReferenceSpeed;
            RobotMovement.leggedAnimationMinSpeedMultiplier = robotLeggedAnimationMinSpeedMultiplier > 0f ? robotLeggedAnimationMinSpeedMultiplier : RobotMovementGlobalSettings.Default.leggedAnimationMinSpeedMultiplier;
            RobotMovement.leggedAnimationMaxSpeedMultiplier = robotLeggedAnimationMaxSpeedMultiplier > 0f ? robotLeggedAnimationMaxSpeedMultiplier : RobotMovementGlobalSettings.Default.leggedAnimationMaxSpeedMultiplier;
            RobotMovement.leggedAnimationSpeedExponent = robotLeggedAnimationSpeedExponent > 0f ? robotLeggedAnimationSpeedExponent : RobotMovementGlobalSettings.Default.leggedAnimationSpeedExponent;
            RobotMovement.leggedStepDistanceMultiplier = robotLeggedStepDistanceMultiplier > 0f ? robotLeggedStepDistanceMultiplier : RobotMovementGlobalSettings.Default.leggedStepDistanceMultiplier;
            RobotMovement.leggedStepHeightMultiplier = robotLeggedStepHeightMultiplier > 0f ? robotLeggedStepHeightMultiplier : RobotMovementGlobalSettings.Default.leggedStepHeightMultiplier;
            RobotMovement.leggedTurnStepDurationMultiplier = robotLeggedTurnStepDurationMultiplier > 0f ? robotLeggedTurnStepDurationMultiplier : RobotMovementGlobalSettings.Default.leggedTurnStepDurationMultiplier;
            RobotMovement.leggedTransitionSpeedMultiplier = robotLeggedTransitionSpeedMultiplier > 0f ? robotLeggedTransitionSpeedMultiplier : RobotMovementGlobalSettings.Default.leggedTransitionSpeedMultiplier;
            RobotMovement.gravity = robotGravity > 0f ? robotGravity : RobotMovementGlobalSettings.Default.gravity;
            RobotMovement.groundedSnap = robotGroundedSnap > 0f ? robotGroundedSnap : RobotMovementGlobalSettings.Default.groundedSnap;

            GunDispersion.enabled = gunDispersionEnabled;
            GunDispersion.expandTime = gunDispersionExpandTime > 0f ? gunDispersionExpandTime : GunDispersionGlobalSettings.Default.expandTime;
            GunDispersion.referenceHullTraverseDegPerSec = gunDispersionReferenceHullTraverse > 0f ? gunDispersionReferenceHullTraverse : GunDispersionGlobalSettings.Default.referenceHullTraverseDegPerSec;
            GunDispersion.referenceTurretTraverseDegPerSec = gunDispersionReferenceTurretTraverse > 0f ? gunDispersionReferenceTurretTraverse : GunDispersionGlobalSettings.Default.referenceTurretTraverseDegPerSec;
            GunDispersion.referenceGunTraverseDegPerSec = gunDispersionReferenceGunTraverse > 0f ? gunDispersionReferenceGunTraverse : GunDispersionGlobalSettings.Default.referenceGunTraverseDegPerSec;
            GunDispersion.referenceCameraAimDegPerSec = gunDispersionReferenceCameraAim > 0f ? gunDispersionReferenceCameraAim : GunDispersionGlobalSettings.Default.referenceCameraAimDegPerSec;
            GunDispersion.accuracyReferenceDistanceMeters = gunDispersionAccuracyReferenceDistanceMeters > 0f ? gunDispersionAccuracyReferenceDistanceMeters : GunDispersionGlobalSettings.Default.accuracyReferenceDistanceMeters;
            GunDispersion.uiMinDiameter = gunDispersionUiMinDiameter > 0f ? gunDispersionUiMinDiameter : GunDispersionGlobalSettings.Default.uiMinDiameter;
            GunDispersion.uiMaxDiameter = gunDispersionUiMaxDiameter > GunDispersion.uiMinDiameter ? gunDispersionUiMaxDiameter : GunDispersionGlobalSettings.Default.uiMaxDiameter;
            GunDispersion.uiFullyAimedPixelsPerDegreeAtMaxZoom = gunDispersionUiFullyAimedPixelsPerDegreeAtMaxZoom >= 0f ? gunDispersionUiFullyAimedPixelsPerDegreeAtMaxZoom : GunDispersionGlobalSettings.Default.uiFullyAimedPixelsPerDegreeAtMaxZoom;
            GunDispersion.uiFullyAimedPixelsPerDegreeAtMaxDistance = gunDispersionUiFullyAimedPixelsPerDegreeAtMaxDistance >= 0f ? gunDispersionUiFullyAimedPixelsPerDegreeAtMaxDistance : GunDispersionGlobalSettings.Default.uiFullyAimedPixelsPerDegreeAtMaxDistance;
            GunDispersion.uiBloomPixelsPerDegreeAtMaxZoom = gunDispersionUiBloomPixelsPerDegreeAtMaxZoom >= 0f ? gunDispersionUiBloomPixelsPerDegreeAtMaxZoom : GunDispersionGlobalSettings.Default.uiBloomPixelsPerDegreeAtMaxZoom;
            GunDispersion.uiBloomPixelsPerDegreeAtMaxDistance = gunDispersionUiBloomPixelsPerDegreeAtMaxDistance >= 0f ? gunDispersionUiBloomPixelsPerDegreeAtMaxDistance : GunDispersionGlobalSettings.Default.uiBloomPixelsPerDegreeAtMaxDistance;
            GunDispersion.uiDiameterLerpSpeed = gunDispersionUiDiameterLerpSpeed >= 0f ? gunDispersionUiDiameterLerpSpeed : GunDispersionGlobalSettings.Default.uiDiameterLerpSpeed;
            GunDispersion.serverSyncInterval = gunDispersionServerSyncInterval > 0f ? gunDispersionServerSyncInterval : GunDispersionGlobalSettings.Default.serverSyncInterval;
            GunDispersion.serverSyncDeadZoneDeg = gunDispersionServerSyncDeadZoneDeg >= 0f ? gunDispersionServerSyncDeadZoneDeg : GunDispersionGlobalSettings.Default.serverSyncDeadZoneDeg;
            GunDispersion.Validate();

            ProjectileBallistics.projectileGravity = projectileGravity >= 0f ? projectileGravity : ProjectileBallisticsGlobalSettings.Default.projectileGravity;
            ProjectileBallistics.useBallisticCompensation = projectileUseBallisticCompensation;
            ProjectileBallistics.preferHighArc = projectilePreferHighArc;
            ProjectileBallistics.debugBallisticTrajectory = projectileDebugBallisticTrajectory;
            ProjectileBallistics.Validate();

            IsLoaded = true;
        }
    }
}
