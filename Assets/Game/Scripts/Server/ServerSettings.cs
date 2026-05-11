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

    public class ServerSettings : MonoBehaviour
    {
        public static ServerSettings In;
        
        public int maxPlayersForFindRoom = 1;
        public int findRoomSeconds = 60;
        public RobotMovementGlobalSettings robotMovement = new RobotMovementGlobalSettings();
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        
        private void Awake()
        {
            In = this;
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

            return In.gunDispersion;
        }

        public static RobotMovementGlobalSettings GetRobotMovement()
        {
            if (In == null || In.robotMovement == null)
            {
                return RobotMovementGlobalSettings.Default;
            }

            return In.robotMovement;
        }
    }
}
