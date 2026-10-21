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
        public static VehicleInputSyncSettings VehicleInputSync { get; } = new VehicleInputSyncSettings();
        public static MatchSceneGlobalSettings MatchScene { get; } = new MatchSceneGlobalSettings();
        public static bool IsLoaded { get; private set; }

        public static void Apply(
            int maxPlayersForFindRoom,
            int findRoomSeconds,
            float robotFallbackMaxSpeed,
            float robotFallbackAcceleration,
            float robotFallbackTraverseSpeed,
            float robotNvStoppingAccelerationMultiplier,
            float robotIaStoppingAccelerationMultiplier,
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
            float gunDispersionAccuracyReferenceDistanceMeters,
            float gunDispersionReticleDiameterLerpSpeed,
            float gunDispersionReticleHorizontalLerpSpeed,
            float gunDispersionReticleVerticalLerpSpeed,
            float gunDispersionReticleSniperHorizontalLerpSpeed,
            float gunDispersionReticleSniperVerticalLerpSpeed,
            float gunDispersionServerSyncInterval,
            float gunDispersionServerSyncDeadZoneDeg,
            float projectileGravity,
            bool projectileUseBallisticCompensation,
            bool projectilePreferHighArc,
            bool projectileDebugBallisticTrajectory,
            float inputSendInterval,
            float inputYawPitchSendDeadzoneDeg,
            float inputAimPointSendDeadzoneMeters,
            int matchSceneSlotSpacingX,
            float matchSceneValidationTimeout,
            int matchEndGameDelayMilliseconds,
            float matchDurationSeconds)
        {
            MaxPlayersForFindRoom = maxPlayersForFindRoom > 0 ? maxPlayersForFindRoom : 1;
            FindRoomSeconds = findRoomSeconds > 0 ? findRoomSeconds : 60;

            RobotMovement.fallbackMaxSpeed = robotFallbackMaxSpeed > 0f ? robotFallbackMaxSpeed : RobotMovementGlobalSettings.Default.fallbackMaxSpeed;
            RobotMovement.fallbackAcceleration = robotFallbackAcceleration > 0f ? robotFallbackAcceleration : RobotMovementGlobalSettings.Default.fallbackAcceleration;
            RobotMovement.fallbackTraverseSpeedDegPerSecond = robotFallbackTraverseSpeed >= 0f ? robotFallbackTraverseSpeed : RobotMovementGlobalSettings.Default.fallbackTraverseSpeedDegPerSecond;
            RobotMovement.nvStoppingAccelerationMultiplier = robotNvStoppingAccelerationMultiplier > 0f ? robotNvStoppingAccelerationMultiplier : RobotMovementGlobalSettings.Default.nvStoppingAccelerationMultiplier;
            RobotMovement.iaStoppingAccelerationMultiplier = robotIaStoppingAccelerationMultiplier > 0f ? robotIaStoppingAccelerationMultiplier : RobotMovementGlobalSettings.Default.iaStoppingAccelerationMultiplier;
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
            GunDispersion.accuracyReferenceDistanceMeters = gunDispersionAccuracyReferenceDistanceMeters > 0f ? gunDispersionAccuracyReferenceDistanceMeters : GunDispersionGlobalSettings.Default.accuracyReferenceDistanceMeters;
            GunDispersion.reticleDiameterLerpSpeed = gunDispersionReticleDiameterLerpSpeed >= 0f ? gunDispersionReticleDiameterLerpSpeed : GunDispersionGlobalSettings.Default.reticleDiameterLerpSpeed;
            GunDispersion.reticleHorizontalLerpSpeed = gunDispersionReticleHorizontalLerpSpeed >= 0f ? gunDispersionReticleHorizontalLerpSpeed : GunDispersionGlobalSettings.Default.reticleHorizontalLerpSpeed;
            GunDispersion.reticleVerticalLerpSpeed = gunDispersionReticleVerticalLerpSpeed >= 0f ? gunDispersionReticleVerticalLerpSpeed : GunDispersionGlobalSettings.Default.reticleVerticalLerpSpeed;
            GunDispersion.reticleSniperHorizontalLerpSpeed = gunDispersionReticleSniperHorizontalLerpSpeed >= 0f ? gunDispersionReticleSniperHorizontalLerpSpeed : GunDispersionGlobalSettings.Default.reticleSniperHorizontalLerpSpeed;
            GunDispersion.reticleSniperVerticalLerpSpeed = gunDispersionReticleSniperVerticalLerpSpeed >= 0f ? gunDispersionReticleSniperVerticalLerpSpeed : GunDispersionGlobalSettings.Default.reticleSniperVerticalLerpSpeed;
            GunDispersion.serverSyncInterval = gunDispersionServerSyncInterval > 0f ? gunDispersionServerSyncInterval : GunDispersionGlobalSettings.Default.serverSyncInterval;
            GunDispersion.serverSyncDeadZoneDeg = gunDispersionServerSyncDeadZoneDeg >= 0f ? gunDispersionServerSyncDeadZoneDeg : GunDispersionGlobalSettings.Default.serverSyncDeadZoneDeg;
            GunDispersion.Validate();

            ProjectileBallistics.projectileGravity = projectileGravity >= 0f ? projectileGravity : ProjectileBallisticsGlobalSettings.Default.projectileGravity;
            ProjectileBallistics.useBallisticCompensation = projectileUseBallisticCompensation;
            ProjectileBallistics.preferHighArc = projectilePreferHighArc;
            ProjectileBallistics.debugBallisticTrajectory = projectileDebugBallisticTrajectory;
            ProjectileBallistics.Validate();

            VehicleInputSync.sendInterval = inputSendInterval;
            VehicleInputSync.yawPitchSendDeadzoneDeg = inputYawPitchSendDeadzoneDeg;
            VehicleInputSync.aimPointSendDeadzoneMeters = inputAimPointSendDeadzoneMeters;
            VehicleInputSync.Validate();

            MatchScene.sceneSlotSpacingX = matchSceneSlotSpacingX;
            MatchScene.sceneValidationTimeout = matchSceneValidationTimeout;
            MatchScene.endGameDelayMilliseconds = matchEndGameDelayMilliseconds;
            MatchScene.matchDurationSeconds = matchDurationSeconds;
            MatchScene.Validate();

            IsLoaded = true;
        }
    }
}
