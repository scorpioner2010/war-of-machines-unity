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
        [Tooltip("Кут, після якого бот зупиняє рух вперед і розвертається на місці до waypoint.")]
        public float turnInPlaceEnterAngle = 48f;
        [Tooltip("Кут, нижче якого бот завершує розворот на місці та знову рухається вперед.")]
        public float turnInPlaceExitAngle = 18f;
        [Tooltip("Кут до цілі, після якого бот починає зменшувати рух вперед під час повороту.")]
        public float slowTurnAngle = 55f;
        [Tooltip("Кут до цілі, після якого бот майже зупиняється для розвороту.")]
        public float stopTurnAngle = 115f;
        [Tooltip("Ввід руху вперед під час повільного повороту.")]
        public float slowForwardInput = 0.35f;
        [Tooltip("Дистанція до waypoint, з якої бот починає сповільнюватися для точнішого прибуття.")]
        public float waypointApproachSlowDistance = 8f;
        [Tooltip("Waypoint приймається, якщо бот пройшов повз нього в межах цієї дистанції, навіть не потрапивши в точний радіус досягнення.")]
        public float waypointPassDistance = 5f;
        [Tooltip("Waypoint у межах дистанції проходу приймається, якщо він залишився позаду корпусу на такий кут.")]
        public float waypointPassedAngle = 125f;

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
            turnInPlaceEnterAngle = ClampFinite(turnInPlaceEnterAngle, 1f, Default.turnInPlaceEnterAngle);
            turnInPlaceExitAngle = ClampFinite(turnInPlaceExitAngle, 0f, Default.turnInPlaceExitAngle);
            if (turnInPlaceExitAngle > turnInPlaceEnterAngle)
            {
                turnInPlaceExitAngle = turnInPlaceEnterAngle;
            }

            slowTurnAngle = ClampFinite(slowTurnAngle, 0f, Default.slowTurnAngle);
            stopTurnAngle = ClampFinite(stopTurnAngle, slowTurnAngle, Default.stopTurnAngle);
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

        [Header("Пошук цілей")]
        [Tooltip("Увімкнути серверний пошук цілей, наведення і стрільбу ботів.")]
        public bool enabled = true;
        [Tooltip("Як часто бот оновлює бойові рішення.")]
        public float thinkInterval = 0.12f;
        [Tooltip("Як часто бот перевіряє кімнату в пошуку кращої ворожої цілі.")]
        public float targetScanInterval = 0.45f;
        [Tooltip("Жорстка верхня межа дистанції захоплення цілі.")]
        public float maxAcquireDistance = 800f;
        [Tooltip("Множник runtime-дальності огляду перед застосуванням maxAcquireDistance.")]
        public float viewRangeMultiplier = 6f;
        [Tooltip("Поточна ціль забувається після перевищення дистанції захоплення, помноженої на це значення.")]
        public float forgetTargetDistanceMultiplier = 1.5f;
        [Tooltip("Скільки секунд бот пам'ятає ціль після втрати прямої видимості.")]
        public float lostSightForgetSeconds = 2.5f;
        [Tooltip("Вимагати чисту лінію вогню для захоплення нової цілі.")]
        public bool requireLineOfSightToAcquire = true;
        [Tooltip("Шари, які перевіряють raycast-промені видимості та лінії вогню бота.")]
        public LayerMask lineOfSightMask = ~0;
        [Tooltip("Перенаправляти навігатор до вибраного ворога замість звичайного wander-руху.")]
        public bool moveTowardTarget = true;
        [Tooltip("Зупиняти рух і стріляти з поточної позиції, коли вибрана ціль видима, доступна для пострілу і не далі за тактичну дальню дистанцію бота.")]
        public bool holdPositionWithLineOfFire = true;

        [Header("Наведення")]
        [Tooltip("За можливості використовувати bounds башти як точку наведення.")]
        public bool preferTurretAimPoint = true;
        [Tooltip("Запасне вертикальне зміщення цілі, якщо bounds колайдерів недоступні.")]
        public float fallbackTargetHeight = 1.2f;
        [Tooltip("Невелике випадкове зміщення наведення, щоб боти не били щоразу точно в центр.")]
        public float randomAimRadius = 0.25f;
        [Tooltip("Прогнозувати позицію рухомої цілі з урахуванням швидкості снаряда.")]
        public bool leadMovingTargets = true;
        [Tooltip("Множник випередження рухомої цілі. 1 означає повне фізичне випередження.")]
        public float leadPredictionMultiplier = 0.75f;
        [Tooltip("Максимальний час прогнозування руху цілі в секундах.")]
        public float maxLeadSeconds = 1.25f;
        [Tooltip("Як часто вимірюється швидкість цілі.")]
        public float targetVelocitySampleInterval = 0.2f;
        [Tooltip("Наводити башту вздовж напрямку руху бота, поки ворога не видно.")]
        public bool aimAlongTravelDirectionWhenNoTarget = true;
        [Tooltip("Запасна дистанція точки наведення, коли бот дивиться в напрямку руху.")]
        public float noTargetTravelAimDistance = 220f;
        [Tooltip("Максимальний вік напрямку навігації для наведення башти без цілі.")]
        public float noTargetTravelDirectionMaxAgeSeconds = 0.8f;
        [Tooltip("Наводити башту вперед, якщо ворога не видно і свіжий напрямок руху відсутній.")]
        public bool aimForwardWhenNoTargetIdle = true;

        [Header("Дозвіл пострілу")]
        [Tooltip("Максимальна помилка yaw між поточним і бажаним поворотом башти перед пострілом.")]
        public float maxAimYawErrorDeg = 4f;
        [Tooltip("Максимальна помилка pitch між поточним і бажаним нахилом гармати перед пострілом.")]
        public float maxAimPitchErrorDeg = 4f;
        [Tooltip("Максимальний кут між напрямком дула і вибраною точкою наведення перед пострілом.")]
        public float maxMuzzleAimErrorDeg = 5f;
        [Tooltip("Бот може стріляти, якщо поточний розкид не перевищує мінімальний розкид, помножений на це значення.")]
        public float maxFireDispersionMultiplier = 6f;
        [Tooltip("Додатковий абсолютний запас допустимого розкиду в градусах.")]
        public float maxFireDispersionAddDeg = 1.75f;
        [Tooltip("Мінімальний час утримання тієї самої цілі перед дозволом пострілу.")]
        public float minTargetHoldBeforeFire = 0.2f;
        [Tooltip("Мінімальна людоподібна затримка реакції після захоплення цілі.")]
        public float reactionDelayMin = 0.15f;
        [Tooltip("Максимальна людоподібна затримка реакції після захоплення цілі.")]
        public float reactionDelayMax = 0.35f;

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

    [System.Serializable]
    public class VehicleInputSyncSettings
    {
        private static readonly VehicleInputSyncSettings DefaultSettings = new VehicleInputSyncSettings();

        [Header("Відправка інпуту")]
        [Tooltip("Максимальна пауза між пакетами керування від власника машини до сервера, якщо інпут не змінюється.")]
        public float sendInterval = 0.05f;
        [Tooltip("Мінімальна зміна yaw/pitch у градусах, після якої клієнт відправляє новий напрямок прицілювання.")]
        public float yawPitchSendDeadzoneDeg = 0.03f;
        [Tooltip("Мінімальна зміна точки прицілювання в метрах, після якої клієнт відправляє нову точку.")]
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

        [Header("Видимість матчу")]
        [Tooltip("Увімкнути серверний командний spotting і знімки видимості карти.")]
        public bool enabled = true;
        [Tooltip("Як часто сервер перераховує spotting і надсилає знімки карти.")]
        public float tickInterval = 0.5f;
        [Tooltip("Запасна дальність огляду для машин без runtime-статистики.")]
        public float fallbackViewRange = 120f;
        [Tooltip("Максимальна ефективна дальність огляду. Встановіть 0, щоб вимкнути обмеження.")]
        public float maxViewRange = 450f;
        [Tooltip("Вороги в межах цієї дистанції виявляються навіть без прямої видимості.")]
        public float guaranteedDetectionRange = 35f;
        [Tooltip("Скільки секунд ворог залишається видимим на командній карті після втрати прямого spotting усією командою.")]
        public float spottedMemorySeconds = 3f;
        [Tooltip("Якщо увімкнено, рельєф і статичні перешкоди можуть блокувати spotting-промені.")]
        public bool requireLineOfSight = true;
        [Tooltip("Шари, які можуть блокувати spotting-промені. Не додавайте до цієї маски шари машин.")]
        public LayerMask lineOfSightMask = DefaultLineOfSightMaskBits;
        [Tooltip("Максимальна кількість raycast-перевірок видимості за один tick однієї кімнати матчу.")]
        public int maxLineOfSightChecksPerTick = 24;
        [Tooltip("Скільки секунд можна повторно використовувати результат видимості пари спостерігач-ціль перед новим raycast.")]
        public float lineOfSightRecheckSeconds = 0.5f;
        [Tooltip("Вертикальне зміщення початку променя від наземної позиції спостерігача.")]
        public float spotterEyeHeight = 2f;
        [Tooltip("Вертикальне зміщення кінця променя від наземної позиції цілі.")]
        public float targetProbeHeight = 1.4f;

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

        [Header("Сцени матчів")]
        [Tooltip("Відстань по X між одночасно завантаженими інстансами бойових сцен на сервері.")]
        public int sceneSlotSpacingX = 500;
        [Tooltip("Скільки секунд сервер чекає валідну бойову сцену перед скасуванням спавну машини.")]
        public float sceneValidationTimeout = 10f;
        [Tooltip("Затримка перед показом екрана результату після завершення бою, у мілісекундах.")]
        public int endGameDelayMilliseconds = 2000;
        [Tooltip("Тривалість матчу в секундах до автоматичного завершення нічиєю.")]
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
        [Tooltip("MMR, який отримують серверні боти під час створення матчу.")]
        public int botMmr = 1000;
        [Tooltip("Префікс імені для серверних ботів у матчі.")]
        public string botNamePrefix = "Bot ";
        [Tooltip("Налаштування поведінки ботів під час руху по карті.")]
        public BotWanderSettings botWander = new BotWanderSettings();
        [Tooltip("Серверні налаштування пошуку цілей, наведення і стрільби ботів.")]
        public BotCombatSettings botCombat = new BotCombatSettings();
        [Tooltip("Глобальні серверні налаштування руху машин.")]
        public RobotMovementGlobalSettings robotMovement = new RobotMovementGlobalSettings();
        [Tooltip("Глобальні серверні налаштування розкиду, зведення і UI-кола прицілу.")]
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        [Tooltip("Глобальні серверні налаштування балістики снарядів.")]
        public ProjectileBallisticsGlobalSettings projectileBallistics = new ProjectileBallisticsGlobalSettings();
        [Tooltip("Глобальні налаштування частоти й порогів синхронізації інпуту техніки.")]
        public VehicleInputSyncSettings vehicleInputSync = new VehicleInputSyncSettings();
        [Tooltip("Серверний spotting і спільні знімки командної карти.")]
        public MatchVisibilityGlobalSettings matchVisibility = new MatchVisibilityGlobalSettings();
        [Tooltip("Глобальні налаштування завантаження бойових сцен і показу результатів матчу.")]
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

            if (botsPerMatch < 0)
            {
                botsPerMatch = 0;
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
