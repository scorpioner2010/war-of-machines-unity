using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Server
{
    [System.Serializable]
    public class RobotMovementGlobalSettings
    {
        private static readonly RobotMovementGlobalSettings DefaultSettings = new RobotMovementGlobalSettings();

        [Header("Fallback Stats")]
        public float fallbackMaxSpeed = 10f;
        public float fallbackAcceleration = 30f;
        public float fallbackTraverseSpeedDegPerSecond = 0f;

        [Header("Inertia")]
        public float stoppingAccelerationMultiplier = 3f;
        public float standardAccelerationMultiplier = 1f;
        public float standardBrakingMultiplier = 1f;
        public float leggedAccelerationMultiplier = 2.25f;
        public float leggedBrakingMultiplier = 2.75f;

        [Header("Leg Animation")]
        public float leggedAnimationReferenceSpeed = 10f;
        public float leggedAnimationMinSpeedMultiplier = 0.45f;
        public float leggedAnimationMaxSpeedMultiplier = 2.2f;
        public float leggedAnimationSpeedExponent = 0.8f;
        public float leggedStepDistanceMultiplier = 1f;
        public float leggedStepHeightMultiplier = 1f;
        public float leggedTurnStepDurationMultiplier = 1f;
        public float leggedTransitionSpeedMultiplier = 1f;

        [Header("Grounding")]
        public float gravity = 25f;
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

        public void Validate()
        {
            fallbackMaxSpeed = ClampFinite(fallbackMaxSpeed, 0f, Default.fallbackMaxSpeed);
            fallbackAcceleration = ClampFinite(fallbackAcceleration, 0.01f, Default.fallbackAcceleration);
            fallbackTraverseSpeedDegPerSecond = ClampFinite(fallbackTraverseSpeedDegPerSecond, 0f, Default.fallbackTraverseSpeedDegPerSecond);
            stoppingAccelerationMultiplier = ClampFinite(stoppingAccelerationMultiplier, 0.01f, Default.stoppingAccelerationMultiplier);
            standardAccelerationMultiplier = ClampFinite(standardAccelerationMultiplier, 0.01f, Default.standardAccelerationMultiplier);
            standardBrakingMultiplier = ClampFinite(standardBrakingMultiplier, 0.01f, Default.standardBrakingMultiplier);
            leggedAccelerationMultiplier = ClampFinite(leggedAccelerationMultiplier, 0.01f, Default.leggedAccelerationMultiplier);
            leggedBrakingMultiplier = ClampFinite(leggedBrakingMultiplier, 0.01f, Default.leggedBrakingMultiplier);
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
            stoppingAccelerationMultiplier = source.stoppingAccelerationMultiplier;
            standardAccelerationMultiplier = source.standardAccelerationMultiplier;
            standardBrakingMultiplier = source.standardBrakingMultiplier;
            leggedAccelerationMultiplier = source.leggedAccelerationMultiplier;
            leggedBrakingMultiplier = source.leggedBrakingMultiplier;
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
    public class ProjectileBallisticsGlobalSettings
    {
        private static readonly ProjectileBallisticsGlobalSettings DefaultSettings = new ProjectileBallisticsGlobalSettings();

        [Header("Trajectory")]
        [Min(0f)] public float projectileGravity = 6f;
        public bool useBallisticCompensation = true;
        public bool preferHighArc;

        [Header("Debug")]
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

    public class ServerSettings : MonoBehaviour
    {
        public static ServerSettings In;
        
        public int maxPlayersForFindRoom = 1;
        public int findRoomSeconds = 60;
        public RobotMovementGlobalSettings robotMovement = new RobotMovementGlobalSettings();
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        public ProjectileBallisticsGlobalSettings projectileBallistics = new ProjectileBallisticsGlobalSettings();
        
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
        }
    }
}
