using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Server
{
    [System.Serializable]
    public class RobotMovementGlobalSettings
    {
        private static readonly RobotMovementGlobalSettings DefaultSettings = new RobotMovementGlobalSettings();

        [Header("Запасні характеристики")]
        [Tooltip("Запасна максимальна швидкість машини, якщо runtime-статистика не передала коректне значення.")]
        public float fallbackMaxSpeed = 10f;
        [Tooltip("Запасне прискорення машини, якщо runtime-статистика не передала коректне значення.")]
        public float fallbackAcceleration = 30f;
        [Tooltip("Запасна швидкість повороту корпусу в градусах за секунду. 0 означає використовувати локальне значення компонента.")]
        public float fallbackTraverseSpeedDegPerSecond = 0f;

        [Header("Інерція")]
        [Tooltip("Множник гальмування, коли машина має повністю зупинитися.")]
        public float stoppingAccelerationMultiplier = 3f;
        [Tooltip("Множник прискорення для звичайних/гусеничних машин.")]
        public float standardAccelerationMultiplier = 1f;
        [Tooltip("Множник гальмування для звичайних/гусеничних машин.")]
        public float standardBrakingMultiplier = 1f;
        [Tooltip("Множник прискорення для крокуючих машин.")]
        public float leggedAccelerationMultiplier = 2.25f;
        [Tooltip("Множник гальмування для крокуючих машин.")]
        public float leggedBrakingMultiplier = 2.75f;

        [Header("Анімація ніг")]
        [Tooltip("Швидкість руху, відносно якої рахується базова швидкість анімації ніг.")]
        public float leggedAnimationReferenceSpeed = 10f;
        [Tooltip("Мінімальний множник швидкості анімації ніг.")]
        public float leggedAnimationMinSpeedMultiplier = 0.45f;
        [Tooltip("Максимальний множник швидкості анімації ніг.")]
        public float leggedAnimationMaxSpeedMultiplier = 2.2f;
        [Tooltip("Крива реакції швидкості анімації ніг на швидкість руху. Більше значення робить зміну різкішою.")]
        public float leggedAnimationSpeedExponent = 0.8f;
        [Tooltip("Множник довжини кроку для крокуючих машин.")]
        public float leggedStepDistanceMultiplier = 1f;
        [Tooltip("Множник висоти кроку для крокуючих машин.")]
        public float leggedStepHeightMultiplier = 1f;
        [Tooltip("Множник тривалості кроку під час повороту.")]
        public float leggedTurnStepDurationMultiplier = 1f;
        [Tooltip("Множник швидкості переходів між фазами кроку.")]
        public float leggedTransitionSpeedMultiplier = 1f;

        [Header("Притискання до землі")]
        [Tooltip("Сила гравітації для руху машин.")]
        public float gravity = 25f;
        [Tooltip("Дистанція притискання до землі, щоб машина стабільно трималася поверхні.")]
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
    public class BotWanderSettings
    {
        private static readonly BotWanderSettings DefaultSettings = new BotWanderSettings();

        [Header("Рух")]
        [Tooltip("Як часто бот переоцінює свій рух і ціль.")]
        public float thinkInterval = 0.25f;
        [Tooltip("Мінімальна тривалість одного рішення руху бота.")]
        public float minMoveDuration = 1.2f;
        [Tooltip("Максимальна тривалість одного рішення руху бота.")]
        public float maxMoveDuration = 3.2f;
        [Tooltip("Базовий ввід руху вперед для бота від -1 до 1.")]
        public float forwardInput = 1f;
        [Tooltip("Максимальна сила м'якого повороту бота від 0 до 1.")]
        public float maxGentleTurnInput = 0.35f;
        [Tooltip("Шанс, що бот вибере різкий поворот замість м'якого.")]
        public float strongTurnChance = 0.18f;
        [Tooltip("Сила різкого повороту бота від 0 до 1.")]
        public float strongTurnInput = 0.85f;
        [Tooltip("Шанс, що бот тимчасово стоятиме без руху.")]
        public float idleChance = 0f;

        [Header("Маршрут по точках")]
        [Tooltip("Дистанція, на якій waypoint вважається досягнутим.")]
        public float waypointReachDistance = 2.4f;
        [Tooltip("Мінімальна дистанція до нової випадкової цілі маршруту.")]
        public float minDestinationDistance = 12f;
        [Tooltip("Кількість спроб знайти підходящу ціль маршруту.")]
        public int destinationPickAttempts = 8;
        [Tooltip("Мінімальний час між перерахунками маршруту.")]
        public float repathCooldown = 0.75f;
        [Tooltip("Як далеко ціль має зміститися, щоб бот перерахував маршрут.")]
        public float targetRepathDistance = 5f;
        [Tooltip("Кут до цілі, при якому бот дає повний ввід повороту.")]
        public float turnFullInputAngle = 90f;
        [Tooltip("Кут до цілі, після якого бот починає зменшувати рух вперед під час повороту.")]
        public float slowTurnAngle = 55f;
        [Tooltip("Кут до цілі, після якого бот майже зупиняється для розвороту.")]
        public float stopTurnAngle = 115f;
        [Tooltip("Ввід руху вперед під час повільного повороту.")]
        public float slowForwardInput = 0.35f;

        [Header("Вихід із застрягання")]
        [Tooltip("Як часто бот перевіряє, чи застряг.")]
        public float stuckCheckInterval = 1.25f;
        [Tooltip("Мінімальна пройдена дистанція за інтервал перевірки. Якщо менше, бот вважається застряглим.")]
        public float stuckDistance = 0.45f;
        [Tooltip("Скільки часу бот виконує маневр вибирання із застрягання.")]
        public float unstickDuration = 0.8f;
        [Tooltip("Ввід назад під час вибирання із застрягання.")]
        public float unstickReverseInput = -0.55f;
        [Tooltip("Сила повороту під час вибирання із застрягання.")]
        public float unstickTurnInput = 1f;

        [Header("Динамічний обхід")]
        [Tooltip("Радіус, у якому бот враховує динамічні перешкоди й інших юнітів.")]
        public float dynamicAvoidanceRadius = 4f;
        [Tooltip("Сила впливу динамічного обходу перешкод на напрямок руху бота.")]
        public float dynamicAvoidanceWeight = 0.65f;

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
            slowTurnAngle = ClampFinite(slowTurnAngle, 0f, Default.slowTurnAngle);
            stopTurnAngle = ClampFinite(stopTurnAngle, slowTurnAngle, Default.stopTurnAngle);
            slowForwardInput = ClampInput(slowForwardInput, Default.slowForwardInput);
            stuckCheckInterval = ClampFinite(stuckCheckInterval, 0.25f, Default.stuckCheckInterval);
            stuckDistance = ClampFinite(stuckDistance, 0.05f, Default.stuckDistance);
            unstickDuration = ClampFinite(unstickDuration, 0.1f, Default.unstickDuration);
            unstickReverseInput = ClampInput(unstickReverseInput, Default.unstickReverseInput);
            unstickTurnInput = ClampInput(Mathf.Abs(unstickTurnInput), Default.unstickTurnInput);
            dynamicAvoidanceRadius = ClampFinite(dynamicAvoidanceRadius, 0f, Default.dynamicAvoidanceRadius);
            dynamicAvoidanceWeight = ClampFinite(dynamicAvoidanceWeight, 0f, Default.dynamicAvoidanceWeight);
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
            slowTurnAngle = source.slowTurnAngle;
            stopTurnAngle = source.stopTurnAngle;
            slowForwardInput = source.slowForwardInput;
            stuckCheckInterval = source.stuckCheckInterval;
            stuckDistance = source.stuckDistance;
            unstickDuration = source.unstickDuration;
            unstickReverseInput = source.unstickReverseInput;
            unstickTurnInput = source.unstickTurnInput;
            dynamicAvoidanceRadius = source.dynamicAvoidanceRadius;
            dynamicAvoidanceWeight = source.dynamicAvoidanceWeight;
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
    public class ProjectileBallisticsGlobalSettings
    {
        private static readonly ProjectileBallisticsGlobalSettings DefaultSettings = new ProjectileBallisticsGlobalSettings();

        [Header("Траєкторія")]
        [Tooltip("Гравітація снаряда. 0 = снаряд летить по прямій без падіння.")]
        [Min(0f)] public float projectileGravity = 6f;
        [Tooltip("Компенсувати падіння снаряда так, щоб траєкторія намагалася влучити в точку прицілу.")]
        public bool useBallisticCompensation = true;
        [Tooltip("Якщо можливо, використовувати вищу дугу балістичної траєкторії.")]
        public bool preferHighArc;

        [Header("Дебаг")]
        [Tooltip("Показувати debug-візуалізацію балістичної траєкторії снаряда.")]
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
        
        [Tooltip("Максимальна кількість гравців у кімнаті пошуку матчу.")]
        public int maxPlayersForFindRoom = 1;
        [Tooltip("Скільки секунд кімната чекає перед стартом матчу, якщо вона не заповнилася.")]
        public int findRoomSeconds = 60;
        [Header("Боти")]
        [Tooltip("Додавати ботів у матч.")]
        public bool botsEnabled = true;
        [Tooltip("Кількість ботів, які додаються в матч.")]
        [Min(0)] public int botsPerMatch = 6;
        [Tooltip("Код машини, яку отримують боти за замовчуванням.")]
        public string defaultBotVehicleCode = "ia_l1_starter";
        [Tooltip("Налаштування поведінки ботів під час руху по карті.")]
        public BotWanderSettings botWander = new BotWanderSettings();
        [Tooltip("Глобальні серверні налаштування руху машин.")]
        public RobotMovementGlobalSettings robotMovement = new RobotMovementGlobalSettings();
        [Tooltip("Глобальні серверні налаштування розкиду, зведення і UI-кола прицілу.")]
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        [Tooltip("Глобальні серверні налаштування балістики снарядів.")]
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

        public static bool AreBotsEnabled()
        {
            return In != null && In.botsEnabled;
        }

        public static int GetBotsPerMatch()
        {
            if (In == null || In.botsPerMatch <= 0)
            {
                return 0;
            }

            return In.botsPerMatch;
        }

        public static string GetDefaultBotVehicleCode()
        {
            if (In == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(In.defaultBotVehicleCode) ? string.Empty : In.defaultBotVehicleCode;
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

            if (botsPerMatch < 0)
            {
                botsPerMatch = 0;
            }

            if (botWander != null)
            {
                botWander.Validate();
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
