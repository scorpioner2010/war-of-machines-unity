using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Server
{
    [System.Serializable]
    public class RobotMovementGlobalSettings
    {
        private static readonly RobotMovementGlobalSettings DefaultSettings = new RobotMovementGlobalSettings();

        [Header("Fallback Statistics")]
        [Tooltip("Fallback maximum vehicle speed when runtime statistics do not provide a valid value.")]
        public float fallbackMaxSpeed = 10f;
        [Tooltip("Fallback vehicle acceleration when runtime statistics do not provide a valid value.")]
        public float fallbackAcceleration = 30f;
        [Tooltip("Fallback hull turn speed in degrees per second. 0 uses the component's local value.")]
        public float fallbackTraverseSpeedDegPerSecond = 0f;

        [Header("Stopping After Releasing Movement")]
        [Tooltip("NV braking multiplier after movement input is released.")]
        public float nvStoppingAccelerationMultiplier = 3f;
        [Tooltip("IA braking multiplier after movement input is released.")]
        public float iaStoppingAccelerationMultiplier = 2.75f;
        [Tooltip("Acceleration multiplier for standard and tracked vehicles.")]
        public float standardAccelerationMultiplier = 1f;
        [Tooltip("Braking multiplier for standard and tracked vehicles.")]
        public float standardBrakingMultiplier = 1f;
        [Tooltip("Acceleration multiplier for walking vehicles.")]
        public float leggedAccelerationMultiplier = 2.25f;
        [Tooltip("Braking multiplier for walking vehicles.")]
        public float leggedBrakingMultiplier = 2.75f;
        [Tooltip("Multiplier for braking when switching to the opposite direction.")]
        public float directionReversalBrakingMultiplier = 2f;

        [Header("Leg Animation")]
        [Tooltip("Movement speed used as the reference for the base leg animation speed.")]
        public float leggedAnimationReferenceSpeed = 10f;
        [Tooltip("Minimum leg animation speed multiplier.")]
        public float leggedAnimationMinSpeedMultiplier = 0.45f;
        [Tooltip("Maximum leg animation speed multiplier.")]
        public float leggedAnimationMaxSpeedMultiplier = 2.2f;
        [Tooltip("Response curve from movement speed to leg animation speed. Higher values make the change sharper.")]
        public float leggedAnimationSpeedExponent = 0.8f;
        [Tooltip("Step-length multiplier for walking vehicles.")]
        public float leggedStepDistanceMultiplier = 1f;
        [Tooltip("Step-height multiplier for walking vehicles.")]
        public float leggedStepHeightMultiplier = 1f;
        [Tooltip("Step-duration multiplier while turning.")]
        public float leggedTurnStepDurationMultiplier = 1f;
        [Tooltip("Transition-speed multiplier between step phases.")]
        public float leggedTransitionSpeedMultiplier = 1f;

        [Header("Ground Adhesion")]
        [Tooltip("Gravity strength for vehicle movement.")]
        public float gravity = 25f;
        [Tooltip("Ground-adhesion distance that keeps the vehicle stable on the surface.")]
        public float groundedSnap = 2f;

        public static RobotMovementGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public float GetAccelerationMultiplier(bool isLegged)
        {
            float value = isLegged ? leggedAccelerationMultiplier : standardAccelerationMultiplier;
            return Mathf.Max(0.01f, value);
        }

        public float GetBrakingMultiplier(bool isLegged)
        {
            float value = isLegged ? leggedBrakingMultiplier : standardBrakingMultiplier;
            return Mathf.Max(0.01f, value);
        }

        public float GetDirectionReversalBrakingMultiplier()
        {
            return Mathf.Max(0.01f, directionReversalBrakingMultiplier);
        }

        public void Validate()
        {
            fallbackMaxSpeed = ClampFinite(fallbackMaxSpeed, 0f, Default.fallbackMaxSpeed);
            fallbackAcceleration = ClampFinite(fallbackAcceleration, 0.01f, Default.fallbackAcceleration);
            fallbackTraverseSpeedDegPerSecond = ClampFinite(fallbackTraverseSpeedDegPerSecond, 0f, Default.fallbackTraverseSpeedDegPerSecond);
            nvStoppingAccelerationMultiplier = ClampFinite(nvStoppingAccelerationMultiplier, 0.01f, Default.nvStoppingAccelerationMultiplier);
            iaStoppingAccelerationMultiplier = ClampFinite(iaStoppingAccelerationMultiplier, 0.01f, Default.iaStoppingAccelerationMultiplier);
            standardAccelerationMultiplier = ClampFinite(standardAccelerationMultiplier, 0.01f, Default.standardAccelerationMultiplier);
            standardBrakingMultiplier = ClampFinite(standardBrakingMultiplier, 0.01f, Default.standardBrakingMultiplier);
            leggedAccelerationMultiplier = ClampFinite(leggedAccelerationMultiplier, 0.01f, Default.leggedAccelerationMultiplier);
            leggedBrakingMultiplier = ClampFinite(leggedBrakingMultiplier, 0.01f, Default.leggedBrakingMultiplier);
            directionReversalBrakingMultiplier = ClampFinite(directionReversalBrakingMultiplier, 0.01f, Default.directionReversalBrakingMultiplier);
            leggedAnimationReferenceSpeed = ClampFinite(leggedAnimationReferenceSpeed, 0.01f, Default.leggedAnimationReferenceSpeed);
            leggedAnimationMinSpeedMultiplier = ClampFinite(leggedAnimationMinSpeedMultiplier, 0.01f, Default.leggedAnimationMinSpeedMultiplier);
            leggedAnimationMaxSpeedMultiplier = ClampFinite(leggedAnimationMaxSpeedMultiplier, 0.01f, Default.leggedAnimationMaxSpeedMultiplier);
            if (leggedAnimationMaxSpeedMultiplier < leggedAnimationMinSpeedMultiplier)
            {
                leggedAnimationMaxSpeedMultiplier = leggedAnimationMinSpeedMultiplier;
            }

            leggedAnimationSpeedExponent = ClampFinite(leggedAnimationSpeedExponent, 0.01f, Default.leggedAnimationSpeedExponent);
            leggedStepDistanceMultiplier = ClampFinite(leggedStepDistanceMultiplier, 0.01f, Default.leggedStepDistanceMultiplier);
            leggedStepHeightMultiplier = ClampFinite(leggedStepHeightMultiplier, 0.01f, Default.leggedStepHeightMultiplier);
            leggedTurnStepDurationMultiplier = ClampFinite(leggedTurnStepDurationMultiplier, 0.01f, Default.leggedTurnStepDurationMultiplier);
            leggedTransitionSpeedMultiplier = ClampFinite(leggedTransitionSpeedMultiplier, 0.01f, Default.leggedTransitionSpeedMultiplier);
            gravity = ClampFinite(gravity, 0.01f, Default.gravity);
            groundedSnap = ClampFinite(groundedSnap, 0.01f, Default.groundedSnap);
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }

        public void CopyFrom(RobotMovementGlobalSettings source)
        {
            if (source == null)
            {
                return;
            }

            fallbackMaxSpeed = source.fallbackMaxSpeed;
            fallbackAcceleration = source.fallbackAcceleration;
            fallbackTraverseSpeedDegPerSecond = source.fallbackTraverseSpeedDegPerSecond;
            nvStoppingAccelerationMultiplier = source.nvStoppingAccelerationMultiplier;
            iaStoppingAccelerationMultiplier = source.iaStoppingAccelerationMultiplier;
            standardAccelerationMultiplier = source.standardAccelerationMultiplier;
            standardBrakingMultiplier = source.standardBrakingMultiplier;
            leggedAccelerationMultiplier = source.leggedAccelerationMultiplier;
            leggedBrakingMultiplier = source.leggedBrakingMultiplier;
            directionReversalBrakingMultiplier = source.directionReversalBrakingMultiplier;
            leggedAnimationReferenceSpeed = source.leggedAnimationReferenceSpeed;
            leggedAnimationMinSpeedMultiplier = source.leggedAnimationMinSpeedMultiplier;
            leggedAnimationMaxSpeedMultiplier = source.leggedAnimationMaxSpeedMultiplier;
            leggedAnimationSpeedExponent = source.leggedAnimationSpeedExponent;
            leggedStepDistanceMultiplier = source.leggedStepDistanceMultiplier;
            leggedStepHeightMultiplier = source.leggedStepHeightMultiplier;
            leggedTurnStepDurationMultiplier = source.leggedTurnStepDurationMultiplier;
            leggedTransitionSpeedMultiplier = source.leggedTransitionSpeedMultiplier;
            gravity = source.gravity;
            groundedSnap = source.groundedSnap;
        }
    }

    [System.Serializable]
    public class BotWanderSettings
    {
        private static readonly BotWanderSettings DefaultSettings = new BotWanderSettings();

        [Header("Movement")]
        [Tooltip("How often the bot reevaluates its movement and target.")]
        public float thinkInterval = 0.25f;
        [Tooltip("Minimum duration of one bot movement decision.")]
        public float minMoveDuration = 1.2f;
        [Tooltip("Maximum duration of one bot movement decision.")]
        public float maxMoveDuration = 3.2f;
        [Tooltip("Base forward movement input for the bot from -1 to 1.")]
        public float forwardInput = 1f;
        [Tooltip("Maximum soft-turn strength for the bot from 0 to 1.")]
        public float maxGentleTurnInput = 0.35f;
        [Tooltip("Chance that the bot chooses a sharp turn instead of a soft turn.")]
        public float strongTurnChance = 0.18f;
        [Tooltip("Sharp-turn strength for the bot from 0 to 1.")]
        public float strongTurnInput = 0.85f;
        [Tooltip("Chance that the bot temporarily remains stationary.")]
        public float idleChance = 0f;

        [Header("Waypoint Routing")]
        [Tooltip("Distance at which a waypoint is considered reached.")]
        public float waypointReachDistance = 2.4f;
        [Tooltip("Minimum distance to a new random route target.")]
        public float minDestinationDistance = 12f;
        [Tooltip("Number of attempts to find a suitable route target.")]
        public int destinationPickAttempts = 8;
        [Tooltip("Minimum time between route recalculations.")]
        public float repathCooldown = 0.75f;
        [Tooltip("How far the target must move before the bot recalculates its route.")]
        public float targetRepathDistance = 5f;
        [Tooltip("Angle to the target at which the bot applies full turn input.")]
        public float turnFullInputAngle = 90f;
        [Tooltip("Angle after which the bot stops moving forward and turns in place toward the waypoint.")]
        public float turnInPlaceEnterAngle = 48f;
        [Tooltip("Angle below which the bot finishes turning in place and moves forward again.")]
        public float turnInPlaceExitAngle = 18f;
        [Tooltip("Angle to the target after which the bot starts reducing forward movement while turning.")]
        public float slowTurnAngle = 55f;
        [Tooltip("Angle to the target after which the bot nearly stops to turn around.")]
        public float stopTurnAngle = 115f;
        public float turnDeadZoneAngle = 4f;
        public float turnInputSmoothing = 7f;
        [Tooltip("Forward movement input during a slow turn.")]
        public float slowForwardInput = 0.35f;
        [Tooltip("Distance from a waypoint at which the bot starts slowing for a more accurate arrival.")]
        public float waypointApproachSlowDistance = 8f;
        [Tooltip("A waypoint is accepted when the bot passes it within this distance, even without entering the exact arrival radius.")]
        public float waypointPassDistance = 5f;
        [Tooltip("A waypoint within the pass distance is accepted when it is behind the hull by this angle.")]
        public float waypointPassedAngle = 125f;

        [Header("Stuck Recovery")]
        [Tooltip("How often the bot checks whether it is stuck.")]
        public float stuckCheckInterval = 1.25f;
        [Tooltip("Minimum distance traveled during the check interval. Below this value, the bot is considered stuck.")]
        public float stuckDistance = 0.45f;
        [Tooltip("How long the bot performs a stuck-recovery maneuver.")]
        public float unstickDuration = 0.8f;
        [Tooltip("Reverse input during stuck recovery.")]
        public float unstickReverseInput = -0.55f;
        [Tooltip("Turn strength during stuck recovery.")]
        public float unstickTurnInput = 1f;

        [Header("Dynamic Avoidance")]
        [Tooltip("Radius in which the bot considers dynamic obstacles and other units.")]
        public float dynamicAvoidanceRadius = 4f;
        [Tooltip("Influence strength of dynamic obstacle avoidance on the bot's movement direction.")]
        public float dynamicAvoidanceWeight = 0.65f;

        [Header("Exploration")]
        public bool exploreUnvisitedNodes = true;
        [Tooltip("Additional score applied to waypoint nodes already visited by this bot.")]
        public float explorationVisitedPenalty = 80f;

        [Header("Robot proximity sensing")]
        [Tooltip("How often the bot probes the eight horizontal directions for nearby robots.")]
        public float proximityScanInterval = 0.5f;
        [Tooltip("Maximum distance of each proximity ray in meters.")]
        public float proximityRayDistance = 8f;
        [Tooltip("Vertical offset of the middle proximity ray from the movement root.")]
        public float proximityRayHeight = 1.1f;
        [Tooltip("Vertical distance between the three rays used to cover different robot heights.")]
        public float proximityRayVerticalSpacing = 0.75f;
        [Tooltip("Strength of steering around robots detected by proximity rays.")]
        public float proximityAvoidanceWeight = 1.4f;
        public LayerMask proximityMask = ~0;

        public static BotWanderSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            thinkInterval = ClampFinite(thinkInterval, 0.05f, Default.thinkInterval);
            minMoveDuration = ClampFinite(minMoveDuration, 0.1f, Default.minMoveDuration);
            maxMoveDuration = ClampFinite(maxMoveDuration, minMoveDuration, Default.maxMoveDuration);
            forwardInput = ClampInput(forwardInput, Default.forwardInput);
            maxGentleTurnInput = ClampInput(Mathf.Abs(maxGentleTurnInput), Default.maxGentleTurnInput);
            strongTurnChance = Mathf.Clamp01(ClampFinite(strongTurnChance, 0f, Default.strongTurnChance));
            strongTurnInput = ClampInput(Mathf.Abs(strongTurnInput), Default.strongTurnInput);
            idleChance = Mathf.Clamp01(ClampFinite(idleChance, 0f, Default.idleChance));
            waypointReachDistance = ClampFinite(waypointReachDistance, 0.1f, Default.waypointReachDistance);
            minDestinationDistance = ClampFinite(minDestinationDistance, 0f, Default.minDestinationDistance);
            destinationPickAttempts = Mathf.Max(1, destinationPickAttempts);
            repathCooldown = ClampFinite(repathCooldown, 0.1f, Default.repathCooldown);
            targetRepathDistance = ClampFinite(targetRepathDistance, 0.1f, Default.targetRepathDistance);
            turnFullInputAngle = ClampFinite(turnFullInputAngle, 1f, Default.turnFullInputAngle);
            turnInPlaceEnterAngle = ClampFinite(turnInPlaceEnterAngle, 1f, Default.turnInPlaceEnterAngle);
            turnInPlaceExitAngle = ClampFinite(turnInPlaceExitAngle, 0f, Default.turnInPlaceExitAngle);
            if (turnInPlaceExitAngle > turnInPlaceEnterAngle)
            {
                turnInPlaceExitAngle = turnInPlaceEnterAngle;
            }

            slowTurnAngle = ClampFinite(slowTurnAngle, 0f, Default.slowTurnAngle);
            stopTurnAngle = ClampFinite(stopTurnAngle, slowTurnAngle, Default.stopTurnAngle);
            turnDeadZoneAngle = ClampFinite(turnDeadZoneAngle, 0f, Default.turnDeadZoneAngle);
            turnInputSmoothing = ClampFinite(turnInputSmoothing, 0.1f, Default.turnInputSmoothing);
            slowForwardInput = ClampInput(slowForwardInput, Default.slowForwardInput);
            waypointApproachSlowDistance = ClampFinite(waypointApproachSlowDistance, waypointReachDistance, Default.waypointApproachSlowDistance);
            waypointPassDistance = ClampFinite(waypointPassDistance, waypointReachDistance, Default.waypointPassDistance);
            waypointPassedAngle = Mathf.Clamp(ClampFinite(waypointPassedAngle, 1f, Default.waypointPassedAngle), 1f, 179f);
            stuckCheckInterval = ClampFinite(stuckCheckInterval, 0.25f, Default.stuckCheckInterval);
            stuckDistance = ClampFinite(stuckDistance, 0.05f, Default.stuckDistance);
            unstickDuration = ClampFinite(unstickDuration, 0.1f, Default.unstickDuration);
            unstickReverseInput = ClampInput(unstickReverseInput, Default.unstickReverseInput);
            unstickTurnInput = ClampInput(Mathf.Abs(unstickTurnInput), Default.unstickTurnInput);
            dynamicAvoidanceRadius = ClampFinite(dynamicAvoidanceRadius, 0f, Default.dynamicAvoidanceRadius);
            dynamicAvoidanceWeight = ClampFinite(dynamicAvoidanceWeight, 0f, Default.dynamicAvoidanceWeight);
            explorationVisitedPenalty = ClampFinite(explorationVisitedPenalty, 0f, Default.explorationVisitedPenalty);
            proximityScanInterval = ClampFinite(proximityScanInterval, 0.1f, Default.proximityScanInterval);
            proximityRayDistance = ClampFinite(proximityRayDistance, 0.5f, Default.proximityRayDistance);
            proximityRayHeight = ClampFinite(proximityRayHeight, 0f, Default.proximityRayHeight);
            proximityRayVerticalSpacing = ClampFinite(proximityRayVerticalSpacing, 0f, Default.proximityRayVerticalSpacing);
            proximityAvoidanceWeight = ClampFinite(proximityAvoidanceWeight, 0f, Default.proximityAvoidanceWeight);
        }

        public void CopyFrom(BotWanderSettings source)
        {
            if (source == null)
            {
                return;
            }

            thinkInterval = source.thinkInterval;
            minMoveDuration = source.minMoveDuration;
            maxMoveDuration = source.maxMoveDuration;
            forwardInput = source.forwardInput;
            maxGentleTurnInput = source.maxGentleTurnInput;
            strongTurnChance = source.strongTurnChance;
            strongTurnInput = source.strongTurnInput;
            idleChance = source.idleChance;
            waypointReachDistance = source.waypointReachDistance;
            minDestinationDistance = source.minDestinationDistance;
            destinationPickAttempts = source.destinationPickAttempts;
            repathCooldown = source.repathCooldown;
            targetRepathDistance = source.targetRepathDistance;
            turnFullInputAngle = source.turnFullInputAngle;
            turnInPlaceEnterAngle = source.turnInPlaceEnterAngle;
            turnInPlaceExitAngle = source.turnInPlaceExitAngle;
            slowTurnAngle = source.slowTurnAngle;
            stopTurnAngle = source.stopTurnAngle;
            turnDeadZoneAngle = source.turnDeadZoneAngle;
            turnInputSmoothing = source.turnInputSmoothing;
            slowForwardInput = source.slowForwardInput;
            waypointApproachSlowDistance = source.waypointApproachSlowDistance;
            waypointPassDistance = source.waypointPassDistance;
            waypointPassedAngle = source.waypointPassedAngle;
            stuckCheckInterval = source.stuckCheckInterval;
            stuckDistance = source.stuckDistance;
            unstickDuration = source.unstickDuration;
            unstickReverseInput = source.unstickReverseInput;
            unstickTurnInput = source.unstickTurnInput;
            dynamicAvoidanceRadius = source.dynamicAvoidanceRadius;
            dynamicAvoidanceWeight = source.dynamicAvoidanceWeight;
            exploreUnvisitedNodes = source.exploreUnvisitedNodes;
            explorationVisitedPenalty = source.explorationVisitedPenalty;
            proximityScanInterval = source.proximityScanInterval;
            proximityRayDistance = source.proximityRayDistance;
            proximityRayHeight = source.proximityRayHeight;
            proximityRayVerticalSpacing = source.proximityRayVerticalSpacing;
            proximityAvoidanceWeight = source.proximityAvoidanceWeight;
            proximityMask = source.proximityMask;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }

        private static float ClampInput(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return Mathf.Clamp(fallback, -1f, 1f);
            }

            return Mathf.Clamp(value, -1f, 1f);
        }
    }

    [System.Serializable]
    public class BotCombatSettings
    {
        private static readonly BotCombatSettings DefaultSettings = new BotCombatSettings();

        [Header("Target Acquisition")]
        [Tooltip("Enable server-side bot target search, aiming, and firing.")]
        public bool enabled = true;
        [Tooltip("How often the bot updates combat decisions.")]
        public float thinkInterval = 0.12f;
        [Tooltip("How often the bot scans the room for a better enemy target.")]
        public float targetScanInterval = 0.35f;
        [Tooltip("Hard upper limit for target acquisition distance.")]
        public float maxAcquireDistance = 800f;
        [Tooltip("Runtime view-distance multiplier before applying maxAcquireDistance.")]
        public float viewRangeMultiplier = 6f;
        [Tooltip("The current target is forgotten after exceeding the acquisition distance multiplied by this value.")]
        public float forgetTargetDistanceMultiplier = 1.5f;
        [Tooltip("How many seconds the bot remembers a target after losing direct line of sight.")]
        public float lostSightForgetSeconds = 2.5f;
        [Tooltip("Require a clear line of fire to acquire a new target.")]
        public bool requireLineOfSightToAcquire = true;
        [Tooltip("Layers checked by bot visibility and line-of-fire raycasts.")]
        public LayerMask lineOfSightMask = ~0;
        [Tooltip("Redirect the navigator toward the selected enemy instead of normal wandering.")]
        public bool moveTowardTarget = true;
        [Tooltip("Stop and fire from the current position when the selected target is visible, shootable, and within the bot's tactical long range.")]
        public bool holdPositionWithLineOfFire = true;

        [Header("Aiming")]
        [Tooltip("Use the turret bounds as the aiming point when available.")]
        public bool preferTurretAimPoint = true;
        [Tooltip("Fallback vertical target offset when collider bounds are unavailable.")]
        public float fallbackTargetHeight = 1.2f;
        [Tooltip("Small random aiming offset so bots do not hit the exact center every time.")]
        public float randomAimRadius = 0.25f;
        [Tooltip("Predict a moving target's position using projectile speed.")]
        public bool leadMovingTargets = true;
        [Tooltip("Moving-target lead multiplier. 1 means full physical leading.")]
        public float leadPredictionMultiplier = 1f;
        [Tooltip("Maximum target movement prediction time in seconds.")]
        public float maxLeadSeconds = 1.5f;
        [Tooltip("How often target velocity is measured.")]
        public float targetVelocitySampleInterval = 0.08f;
        [Tooltip("How strongly new target velocity samples replace the previous estimate.")]
        public float targetVelocitySmoothing = 0.75f;
        [Tooltip("Number of short intercept iterations used to solve target lead time.")]
        public int leadPredictionIterations = 3;
        [Tooltip("Aim the turret along the bot's movement direction while no enemy is visible.")]
        public bool aimAlongTravelDirectionWhenNoTarget = true;
        [Tooltip("Fallback aiming-point distance when the bot looks along its movement direction.")]
        public float noTargetTravelAimDistance = 220f;
        [Tooltip("Maximum navigation-direction age for turret aiming without a target.")]
        public float noTargetTravelDirectionMaxAgeSeconds = 0.8f;
        [Tooltip("Aim the turret forward when no enemy is visible and no recent movement direction exists.")]
        public bool aimForwardWhenNoTargetIdle = true;

        [Header("Fire Permission")]
        [Tooltip("Maximum yaw error between the current and desired turret rotation before firing.")]
        public float maxAimYawErrorDeg = 6f;
        [Tooltip("Maximum pitch error between the current and desired gun elevation before firing.")]
        public float maxAimPitchErrorDeg = 6f;
        [Tooltip("Maximum angle between the barrel direction and selected aiming point before firing.")]
        public float maxMuzzleAimErrorDeg = 7f;
        [Tooltip("The bot may fire when current dispersion does not exceed minimum dispersion multiplied by this value.")]
        public float maxFireDispersionMultiplier = 8f;
        [Tooltip("Additional absolute allowed-dispersion margin in degrees.")]
        public float maxFireDispersionAddDeg = 2.5f;
        [Tooltip("Minimum time to keep the same target before firing is allowed.")]
        public float minTargetHoldBeforeFire = 0.08f;
        [Tooltip("Minimum human-like reaction delay after acquiring a target.")]
        public float reactionDelayMin = 0.08f;
        [Tooltip("Maximum human-like reaction delay after acquiring a target.")]
        public float reactionDelayMax = 0.18f;

        public static BotCombatSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            thinkInterval = ClampFinite(thinkInterval, 0.03f, Default.thinkInterval);
            targetScanInterval = ClampFinite(targetScanInterval, thinkInterval, Default.targetScanInterval);
            maxAcquireDistance = ClampFinite(maxAcquireDistance, 1f, Default.maxAcquireDistance);
            viewRangeMultiplier = ClampFinite(viewRangeMultiplier, 0.1f, Default.viewRangeMultiplier);
            forgetTargetDistanceMultiplier = ClampFinite(forgetTargetDistanceMultiplier, 1f, Default.forgetTargetDistanceMultiplier);
            lostSightForgetSeconds = ClampFinite(lostSightForgetSeconds, 0f, Default.lostSightForgetSeconds);
            fallbackTargetHeight = ClampFinite(fallbackTargetHeight, 0f, Default.fallbackTargetHeight);
            randomAimRadius = ClampFinite(randomAimRadius, 0f, Default.randomAimRadius);
            leadPredictionMultiplier = ClampFinite(leadPredictionMultiplier, 0f, Default.leadPredictionMultiplier);
            maxLeadSeconds = ClampFinite(maxLeadSeconds, 0f, Default.maxLeadSeconds);
            targetVelocitySampleInterval = ClampFinite(targetVelocitySampleInterval, 0.03f, Default.targetVelocitySampleInterval);
            targetVelocitySmoothing = Mathf.Clamp01(ClampFinite(targetVelocitySmoothing, 0f, Default.targetVelocitySmoothing));
            leadPredictionIterations = Mathf.Clamp(leadPredictionIterations, 1, 8);
            noTargetTravelAimDistance = ClampFinite(noTargetTravelAimDistance, 1f, Default.noTargetTravelAimDistance);
            noTargetTravelDirectionMaxAgeSeconds = ClampFinite(noTargetTravelDirectionMaxAgeSeconds, 0.03f, Default.noTargetTravelDirectionMaxAgeSeconds);
            maxAimYawErrorDeg = ClampFinite(maxAimYawErrorDeg, 0f, Default.maxAimYawErrorDeg);
            maxAimPitchErrorDeg = ClampFinite(maxAimPitchErrorDeg, 0f, Default.maxAimPitchErrorDeg);
            maxMuzzleAimErrorDeg = ClampFinite(maxMuzzleAimErrorDeg, 0f, Default.maxMuzzleAimErrorDeg);
            maxFireDispersionMultiplier = ClampFinite(maxFireDispersionMultiplier, 1f, Default.maxFireDispersionMultiplier);
            maxFireDispersionAddDeg = ClampFinite(maxFireDispersionAddDeg, 0f, Default.maxFireDispersionAddDeg);
            minTargetHoldBeforeFire = ClampFinite(minTargetHoldBeforeFire, 0f, Default.minTargetHoldBeforeFire);
            reactionDelayMin = ClampFinite(reactionDelayMin, 0f, Default.reactionDelayMin);
            reactionDelayMax = ClampFinite(reactionDelayMax, reactionDelayMin, Default.reactionDelayMax);
        }

        public void CopyFrom(BotCombatSettings source)
        {
            if (source == null)
            {
                return;
            }

            enabled = source.enabled;
            thinkInterval = source.thinkInterval;
            targetScanInterval = source.targetScanInterval;
            maxAcquireDistance = source.maxAcquireDistance;
            viewRangeMultiplier = source.viewRangeMultiplier;
            forgetTargetDistanceMultiplier = source.forgetTargetDistanceMultiplier;
            lostSightForgetSeconds = source.lostSightForgetSeconds;
            requireLineOfSightToAcquire = source.requireLineOfSightToAcquire;
            lineOfSightMask = source.lineOfSightMask;
            moveTowardTarget = source.moveTowardTarget;
            holdPositionWithLineOfFire = source.holdPositionWithLineOfFire;
            preferTurretAimPoint = source.preferTurretAimPoint;
            fallbackTargetHeight = source.fallbackTargetHeight;
            randomAimRadius = source.randomAimRadius;
            leadMovingTargets = source.leadMovingTargets;
            leadPredictionMultiplier = source.leadPredictionMultiplier;
            maxLeadSeconds = source.maxLeadSeconds;
            targetVelocitySampleInterval = source.targetVelocitySampleInterval;
            targetVelocitySmoothing = source.targetVelocitySmoothing;
            leadPredictionIterations = source.leadPredictionIterations;
            aimAlongTravelDirectionWhenNoTarget = source.aimAlongTravelDirectionWhenNoTarget;
            noTargetTravelAimDistance = source.noTargetTravelAimDistance;
            noTargetTravelDirectionMaxAgeSeconds = source.noTargetTravelDirectionMaxAgeSeconds;
            aimForwardWhenNoTargetIdle = source.aimForwardWhenNoTargetIdle;
            maxAimYawErrorDeg = source.maxAimYawErrorDeg;
            maxAimPitchErrorDeg = source.maxAimPitchErrorDeg;
            maxMuzzleAimErrorDeg = source.maxMuzzleAimErrorDeg;
            maxFireDispersionMultiplier = source.maxFireDispersionMultiplier;
            maxFireDispersionAddDeg = source.maxFireDispersionAddDeg;
            minTargetHoldBeforeFire = source.minTargetHoldBeforeFire;
            reactionDelayMin = source.reactionDelayMin;
            reactionDelayMax = source.reactionDelayMax;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

    [System.Serializable]
    public class ProjectileBallisticsGlobalSettings
    {
        private static readonly ProjectileBallisticsGlobalSettings DefaultSettings = new ProjectileBallisticsGlobalSettings();

        [Header("Trajectory")]
        [Tooltip("Projectile gravity. 0 = the projectile travels in a straight line without dropping.")]
        [Min(0f)] public float projectileGravity = 6f;
        [Tooltip("Compensate for projectile drop so the trajectory attempts to hit the aiming point.")]
        public bool useBallisticCompensation = true;
        [Tooltip("Use a higher ballistic trajectory arc when possible.")]
        public bool preferHighArc;

        [Header("Debug")]
        [Tooltip("Show debug visualization of the projectile ballistic trajectory.")]
        public bool debugBallisticTrajectory;

        public static ProjectileBallisticsGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            projectileGravity = ClampFinite(projectileGravity, 0f, Default.projectileGravity);
        }

        public void CopyFrom(ProjectileBallisticsGlobalSettings source)
        {
            if (source == null)
            {
                return;
            }

            projectileGravity = source.projectileGravity;
            useBallisticCompensation = source.useBallisticCompensation;
            preferHighArc = source.preferHighArc;
            debugBallisticTrajectory = source.debugBallisticTrajectory;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

    [System.Serializable]
    public class VehicleInputSyncSettings
    {
        private static readonly VehicleInputSyncSettings DefaultSettings = new VehicleInputSyncSettings();

        [Header("Input Sending")]
        [Tooltip("Maximum pause between owner-to-server control packets when input does not change.")]
        public float sendInterval = 0.05f;
        [Tooltip("Minimum yaw/pitch change in degrees before the client sends a new aiming direction.")]
        public float yawPitchSendDeadzoneDeg = 0.03f;
        [Tooltip("Minimum aiming-point change in meters before the client sends a new point.")]
        public float aimPointSendDeadzoneMeters = 0.02f;

        public static VehicleInputSyncSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public float GetAimPointSendDeadzoneSqr()
        {
            float value = Mathf.Max(0f, aimPointSendDeadzoneMeters);
            return value * value;
        }

        public void Validate()
        {
            sendInterval = ClampFinite(sendInterval, 0.001f, Default.sendInterval);
            yawPitchSendDeadzoneDeg = ClampFinite(yawPitchSendDeadzoneDeg, 0f, Default.yawPitchSendDeadzoneDeg);
            aimPointSendDeadzoneMeters = ClampFinite(aimPointSendDeadzoneMeters, 0f, Default.aimPointSendDeadzoneMeters);
        }

        public void CopyFrom(VehicleInputSyncSettings source)
        {
            if (source == null)
            {
                return;
            }

            sendInterval = source.sendInterval;
            yawPitchSendDeadzoneDeg = source.yawPitchSendDeadzoneDeg;
            aimPointSendDeadzoneMeters = source.aimPointSendDeadzoneMeters;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

    [System.Serializable]
    public class MatchVisibilityGlobalSettings
    {
        private const int DefaultLayer = 0;
        private const int TransparentFxLayer = 1;
        private const int IgnoreRaycastLayer = 2;
        private const int GroundLayer = 3;
        private const int WaterLayer = 4;
        private const int UiLayer = 5;
        private const int ArmorLayer = 6;
        private const int ChassisLayer = 7;
        private const int ObstacleLayer = 8;
        private const int DefaultLineOfSightMaskBits = (1 << DefaultLayer) | (1 << GroundLayer) | (1 << ObstacleLayer);
        private const int ExcludedLineOfSightMaskBits =
            (1 << TransparentFxLayer)
            | (1 << IgnoreRaycastLayer)
            | (1 << WaterLayer)
            | (1 << UiLayer)
            | (1 << ArmorLayer)
            | (1 << ChassisLayer);

        private static readonly MatchVisibilityGlobalSettings DefaultSettings = new MatchVisibilityGlobalSettings();

        [Header("Match Visibility")]
        [Tooltip("Enable server-side team spotting and map-visibility snapshots.")]
        public bool enabled = true;
        [Tooltip("How often the server recalculates spotting and sends map snapshots.")]
        public float tickInterval = 0.5f;
        [Tooltip("Fallback view range for vehicles without runtime statistics.")]
        public float fallbackViewRange = 120f;
        [Tooltip("Maximum effective view range. Set to 0 to disable the limit.")]
        public float maxViewRange = 450f;
        [Tooltip("Enemies within this distance ignore the view-range stat, but walls still block detection when requireLineOfSight is enabled.")]
        public float guaranteedDetectionRange = 35f;
        [Tooltip("How many seconds an enemy remains visible on the team map after the entire team loses direct spotting.")]
        public float spottedMemorySeconds = 3f;
        [Tooltip("When enabled, terrain and static obstacles can block spotting rays.")]
        public bool requireLineOfSight = true;
        [Tooltip("Layers that can block spotting rays. Do not add vehicle layers to this mask.")]
        public LayerMask lineOfSightMask = DefaultLineOfSightMaskBits;
        [Tooltip("Maximum number of visibility raycast checks per tick for one match room.")]
        public int maxLineOfSightChecksPerTick = 24;
        [Tooltip("How many seconds a watcher-target visibility result can be reused before another raycast.")]
        public float lineOfSightRecheckSeconds = 0.5f;
        [Tooltip("Vertical offset of the ray origin from the observer's ground position.")]
        public float spotterEyeHeight = 2f;
        [Tooltip("Vertical offset of the ray endpoint from the target's ground position.")]
        public float targetProbeHeight = 1.4f;
        [Tooltip("Horizontal radius of extra spotting points around the target for partial peeks from cover.")]
        public float targetProbeRadius = 0.75f;

        public static MatchVisibilityGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            tickInterval = ClampFinite(tickInterval, 0.05f, Default.tickInterval);
            fallbackViewRange = ClampFinite(fallbackViewRange, 0f, Default.fallbackViewRange);
            maxViewRange = ClampFinite(maxViewRange, 0f, Default.maxViewRange);
            guaranteedDetectionRange = ClampFinite(guaranteedDetectionRange, 0f, Default.guaranteedDetectionRange);
            spottedMemorySeconds = ClampFinite(spottedMemorySeconds, 0f, Default.spottedMemorySeconds);
            lineOfSightMask = NormalizeLineOfSightMask(lineOfSightMask);
            maxLineOfSightChecksPerTick = Mathf.Max(1, maxLineOfSightChecksPerTick);
            lineOfSightRecheckSeconds = ClampFinite(lineOfSightRecheckSeconds, 0.05f, Default.lineOfSightRecheckSeconds);
            spotterEyeHeight = ClampFinite(spotterEyeHeight, 0f, Default.spotterEyeHeight);
            targetProbeHeight = ClampFinite(targetProbeHeight, 0f, Default.targetProbeHeight);
            targetProbeRadius = ClampFinite(targetProbeRadius, 0f, Default.targetProbeRadius);
        }

        public void CopyFrom(MatchVisibilityGlobalSettings source)
        {
            if (source == null)
            {
                return;
            }

            enabled = source.enabled;
            tickInterval = source.tickInterval;
            fallbackViewRange = source.fallbackViewRange;
            maxViewRange = source.maxViewRange;
            guaranteedDetectionRange = source.guaranteedDetectionRange;
            spottedMemorySeconds = source.spottedMemorySeconds;
            requireLineOfSight = source.requireLineOfSight;
            lineOfSightMask = source.lineOfSightMask;
            maxLineOfSightChecksPerTick = source.maxLineOfSightChecksPerTick;
            lineOfSightRecheckSeconds = source.lineOfSightRecheckSeconds;
            spotterEyeHeight = source.spotterEyeHeight;
            targetProbeHeight = source.targetProbeHeight;
            targetProbeRadius = source.targetProbeRadius;
        }

        private static LayerMask NormalizeLineOfSightMask(LayerMask source)
        {
            int mask = source.value;
            if (mask == 0 || mask == ~0)
            {
                mask = DefaultLineOfSightMaskBits;
            }

            mask &= ~ExcludedLineOfSightMaskBits;

            return mask;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

    [System.Serializable]
    public class MatchSceneGlobalSettings
    {
        private static readonly MatchSceneGlobalSettings DefaultSettings = new MatchSceneGlobalSettings();

        [Header("Match Scenes")]
        [Tooltip("X-axis distance between simultaneously loaded battle-scene instances on the server.")]
        public int sceneSlotSpacingX = 500;
        [Tooltip("How many seconds the server waits for a valid battle scene before canceling vehicle spawn.")]
        public float sceneValidationTimeout = 10f;
        [Tooltip("Delay before showing the result screen after battle completion, in milliseconds.")]
        public int endGameDelayMilliseconds = 2000;
        [Tooltip("Match duration in seconds before automatic draw completion.")]
        public float matchDurationSeconds = 300f;

        public static MatchSceneGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            sceneSlotSpacingX = Mathf.Max(1, sceneSlotSpacingX);
            sceneValidationTimeout = ClampFinite(sceneValidationTimeout, 0.1f, Default.sceneValidationTimeout);
            endGameDelayMilliseconds = Mathf.Max(0, endGameDelayMilliseconds);
            matchDurationSeconds = ClampFinite(matchDurationSeconds, 1f, Default.matchDurationSeconds);
        }

        public void CopyFrom(MatchSceneGlobalSettings source)
        {
            if (source == null)
            {
                return;
            }

            sceneSlotSpacingX = source.sceneSlotSpacingX;
            sceneValidationTimeout = source.sceneValidationTimeout;
            endGameDelayMilliseconds = source.endGameDelayMilliseconds;
            matchDurationSeconds = source.matchDurationSeconds;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

    public class ServerSettings : MonoBehaviour
    {
        public static ServerSettings In;
        
        [Tooltip("Number of real players for an immediate start and target total participants. After matchmaking, the server fills the remaining slots with bots.")]
        public int maxPlayersForFindRoom = 1;
        [Tooltip("How many seconds the room waits before starting if it is not full.")]
        public int findRoomSeconds = 60;
        [Header("Bots")]
        [Tooltip("Add bots to fill the match to maxPlayersForFindRoom.")]
        public bool botsEnabled = true;
        [Tooltip("Vehicle code assigned to bots by default.")]
        public string defaultBotVehicleCode = "ia_l1_starter";
        [Tooltip("MMR assigned to server bots when creating a match.")]
        public int botMmr = 1000;
        [Tooltip("Name prefix for server bots in a match.")]
        public string botNamePrefix = "Bot ";
        [Tooltip("Bot behavior settings while moving around the map.")]
        public BotWanderSettings botWander = new BotWanderSettings();
        [Tooltip("Server settings for bot target acquisition, aiming, and firing.")]
        public BotCombatSettings botCombat = new BotCombatSettings();
        [Tooltip("Global server settings for vehicle movement.")]
        public RobotMovementGlobalSettings robotMovement = new RobotMovementGlobalSettings();
        [Tooltip("Global server settings for dispersion, aiming, and the UI aiming circle.")]
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        [Tooltip("Global server settings for projectile ballistics.")]
        public ProjectileBallisticsGlobalSettings projectileBallistics = new ProjectileBallisticsGlobalSettings();
        [Tooltip("Global settings for vehicle-input synchronization frequency and thresholds.")]
        public VehicleInputSyncSettings vehicleInputSync = new VehicleInputSyncSettings();
        [Tooltip("Server-side spotting and shared team-map snapshots.")]
        public MatchVisibilityGlobalSettings matchVisibility = new MatchVisibilityGlobalSettings();
        [Tooltip("Global settings for battle-scene loading and match-result display.")]
        public MatchSceneGlobalSettings matchScene = new MatchSceneGlobalSettings();
        
        private void Awake()
        {
            ValidateSettings();
            In = this;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public static int GetMaxPlayersForFindRoom()
        {
            if (In == null || In.maxPlayersForFindRoom <= 0)
            {
                return 1;
            }

            return In.maxPlayersForFindRoom;
        }

        public static int GetFindRoomSeconds()
        {
            if (In == null || In.findRoomSeconds <= 0)
            {
                return 60;
            }

            return In.findRoomSeconds;
        }

        public static bool AreBotsEnabled()
        {
            return In != null && In.botsEnabled;
        }

        public static string GetDefaultBotVehicleCode()
        {
            if (In == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(In.defaultBotVehicleCode) ? string.Empty : In.defaultBotVehicleCode;
        }

        public static int GetBotMmr()
        {
            if (In == null || In.botMmr < 0)
            {
                return 1000;
            }

            return In.botMmr;
        }

        public static string GetBotNamePrefix()
        {
            if (In == null || string.IsNullOrEmpty(In.botNamePrefix))
            {
                return "Bot ";
            }

            return In.botNamePrefix;
        }

        public static BotWanderSettings GetBotWander()
        {
            if (In == null || In.botWander == null)
            {
                return BotWanderSettings.Default;
            }

            In.botWander.Validate();
            return In.botWander;
        }

        public static BotCombatSettings GetBotCombat()
        {
            if (In == null || In.botCombat == null)
            {
                return BotCombatSettings.Default;
            }

            In.botCombat.Validate();
            return In.botCombat;
        }

        public static GunDispersionGlobalSettings GetGunDispersion()
        {
            if (In == null || In.gunDispersion == null)
            {
                return GunDispersionGlobalSettings.Default;
            }

            In.gunDispersion.Validate();
            return In.gunDispersion;
        }

        public static RobotMovementGlobalSettings GetRobotMovement()
        {
            if (In == null || In.robotMovement == null)
            {
                return RobotMovementGlobalSettings.Default;
            }

            In.robotMovement.Validate();
            return In.robotMovement;
        }

        public static ProjectileBallisticsGlobalSettings GetProjectileBallistics()
        {
            if (In == null || In.projectileBallistics == null)
            {
                return ProjectileBallisticsGlobalSettings.Default;
            }

            In.projectileBallistics.Validate();
            return In.projectileBallistics;
        }

        public static VehicleInputSyncSettings GetVehicleInputSync()
        {
            if (In == null || In.vehicleInputSync == null)
            {
                return VehicleInputSyncSettings.Default;
            }

            In.vehicleInputSync.Validate();
            return In.vehicleInputSync;
        }

        public static MatchVisibilityGlobalSettings GetMatchVisibility()
        {
            if (In == null || In.matchVisibility == null)
            {
                return MatchVisibilityGlobalSettings.Default;
            }

            In.matchVisibility.Validate();
            return In.matchVisibility;
        }

        public static MatchSceneGlobalSettings GetMatchScene()
        {
            if (In == null || In.matchScene == null)
            {
                return MatchSceneGlobalSettings.Default;
            }

            In.matchScene.Validate();
            return In.matchScene;
        }

        private void ValidateSettings()
        {
            if (maxPlayersForFindRoom < 1)
            {
                maxPlayersForFindRoom = 1;
            }

            if (findRoomSeconds < 1)
            {
                findRoomSeconds = 1;
            }

            if (botMmr < 0)
            {
                botMmr = 0;
            }

            if (string.IsNullOrEmpty(botNamePrefix))
            {
                botNamePrefix = "Bot ";
            }

            if (botWander != null)
            {
                botWander.Validate();
            }

            if (botCombat != null)
            {
                botCombat.Validate();
            }

            if (robotMovement != null)
            {
                robotMovement.Validate();
            }

            if (gunDispersion != null)
            {
                gunDispersion.Validate();
            }

            if (projectileBallistics != null)
            {
                projectileBallistics.Validate();
            }

            if (vehicleInputSync != null)
            {
                vehicleInputSync.Validate();
            }

            if (matchVisibility != null)
            {
                matchVisibility.Validate();
            }

            if (matchScene != null)
            {
                matchScene.Validate();
            }
        }
    }
}
