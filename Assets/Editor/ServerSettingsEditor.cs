using Game.Scripts.Server;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    [CustomEditor(typeof(ServerSettings))]
    public class ServerSettingsEditor : UnityEditor.Editor
    {
        private bool _showBots = true;
        private bool _showRobotMovement = true;
        private bool _showGunDispersion = true;
        private bool _showProjectileBallistics = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Наведіть курсор на назву параметра, щоб побачити український опис його впливу.",
                MessageType.Info
            );

            EditorGUILayout.Space(4f);
            DrawMatchmaking();
            EditorGUILayout.Space(6f);
            DrawBots();
            EditorGUILayout.Space(6f);
            DrawProjectileBallistics();
            EditorGUILayout.Space(6f);
            DrawRobotMovement();
            EditorGUILayout.Space(6f);
            DrawGunDispersion();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMatchmaking()
        {
            EditorGUILayout.LabelField("Matchmaking", EditorStyles.boldLabel);
            DrawProperty(
                "maxPlayersForFindRoom",
                "Скільки гравців потрібно для створення або запуску кімнати під час пошуку матчу. Якщо значення менше або дорівнює нулю, код використовує 1."
            );
            DrawProperty(
                "findRoomSeconds",
                "Скільки секунд триває пошук кімнати або очікування матчмейкінгу. Якщо значення менше або дорівнює нулю, код використовує 60 секунд."
            );
        }

        private void DrawBots()
        {
            EditorGUILayout.LabelField("Bots", EditorStyles.boldLabel);
            DrawProperty(
                "botsEnabled",
                "Enables server-side match bots. Bots are spawned by the server and replicated to clients."
            );
            DrawProperty(
                "botsPerMatch",
                "How many bots the server adds to each match after real players are assigned to the room."
            );
            DrawProperty(
                "defaultBotVehicleCode",
                "Vehicle registry code used for bots. If empty, the first valid registry item is used."
            );

            SerializedProperty botWander = serializedObject.FindProperty("botWander");
            _showBots = EditorGUILayout.Foldout(
                _showBots,
                new GUIContent("Bot Wander", "Very light server-only input wandering for bots."),
                true
            );

            if (!_showBots)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
                DrawChildProperty(botWander, "thinkInterval", "How often a bot checks whether it should change input.");
                DrawChildProperty(botWander, "minMoveDuration", "Minimum time a bot keeps one movement input.");
                DrawChildProperty(botWander, "maxMoveDuration", "Maximum time a bot keeps one movement input.");
                DrawChildProperty(botWander, "forwardInput", "Forward input used while wandering.");
                DrawChildProperty(botWander, "maxGentleTurnInput", "Maximum random gentle turn input while moving forward.");
                DrawChildProperty(botWander, "strongTurnChance", "Chance to pick a stronger turn for the next movement segment.");
                DrawChildProperty(botWander, "strongTurnInput", "Turn input used for strong turns.");
                DrawChildProperty(botWander, "idleChance", "Chance to choose a short idle segment instead of moving.");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Waypoint Path", EditorStyles.miniBoldLabel);
                DrawChildProperty(botWander, "waypointReachDistance", "Distance at which the bot advances to the next waypoint.");
                DrawChildProperty(botWander, "minDestinationDistance", "Minimum distance from bot to a random destination waypoint.");
                DrawChildProperty(botWander, "destinationPickAttempts", "How many destination nodes are tried before accepting a closer one.");
                DrawChildProperty(botWander, "repathCooldown", "Minimum delay between graph path searches for one bot.");
                DrawChildProperty(botWander, "targetRepathDistance", "Moving target distance change required before rebuilding the path.");
                DrawChildProperty(botWander, "turnFullInputAngle", "Angle mapped to full left or right input.");
                DrawChildProperty(botWander, "slowTurnAngle", "Bot slows down when target angle is above this value.");
                DrawChildProperty(botWander, "stopTurnAngle", "Bot stops forward movement when target angle is above this value.");
                DrawChildProperty(botWander, "slowForwardInput", "Forward input used during a large turn.");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Stuck Recovery", EditorStyles.miniBoldLabel);
                DrawChildProperty(botWander, "stuckCheckInterval", "How often the bot checks if it stopped making progress.");
                DrawChildProperty(botWander, "stuckDistance", "Minimum movement counted as progress between stuck checks.");
                DrawChildProperty(botWander, "unstickDuration", "How long the bot reverses and turns when stuck.");
                DrawChildProperty(botWander, "unstickReverseInput", "Reverse input used when stuck.");
                DrawChildProperty(botWander, "unstickTurnInput", "Turn input used when stuck.");

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Dynamic Avoidance", EditorStyles.miniBoldLabel);
                DrawChildProperty(botWander, "dynamicAvoidanceRadius", "Radius used for lightweight avoidance of other robots.");
                DrawChildProperty(botWander, "dynamicAvoidanceWeight", "How strongly nearby robots bend the desired movement direction.");
            }
        }

        private void DrawRobotMovement()
        {
            SerializedProperty robotMovement = serializedObject.FindProperty("robotMovement");
            _showRobotMovement = EditorGUILayout.Foldout(
                _showRobotMovement,
                new GUIContent("Robot Movement", "Глобальні серверні параметри руху роботів. Вони впливають на пересування, інерцію, гравітацію та анімацію ніг."),
                true
            );

            if (!_showRobotMovement)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Fallback Stats", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    robotMovement,
                    "fallbackMaxSpeed",
                    "Запасна максимальна швидкість робота, якщо у конкретної машини немає валідної швидкості зі статів."
                );
                DrawChildProperty(
                    robotMovement,
                    "fallbackAcceleration",
                    "Запасне прискорення робота, якщо у конкретної машини немає валідного прискорення зі статів."
                );
                DrawChildProperty(
                    robotMovement,
                    "fallbackTraverseSpeedDegPerSecond",
                    "Запасна швидкість повороту корпусу в градусах за секунду, якщо у робота немає валідного значення зі статів."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Inertia", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    robotMovement,
                    "stoppingAccelerationMultiplier",
                    "Множник гальмування, коли немає активного вводу руху. Більше значення швидше зупиняє робота."
                );
                DrawChildProperty(
                    robotMovement,
                    "standardAccelerationMultiplier",
                    "Множник прискорення для стандартних роботів без ніг. Більше значення робить розгін різкішим."
                );
                DrawChildProperty(
                    robotMovement,
                    "standardBrakingMultiplier",
                    "Множник гальмування для стандартних роботів без ніг. Більше значення швидше прибирає швидкість при зміні або відпусканні руху."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedAccelerationMultiplier",
                    "Множник прискорення для роботів із ногами. Дозволяє окремо налаштовувати відчуття руху мехів."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedBrakingMultiplier",
                    "Множник гальмування для роботів із ногами. Більше значення робить зупинку меха швидшою."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Leg Animation", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    robotMovement,
                    "leggedAnimationReferenceSpeed",
                    "Застарілий параметр старої схеми темпу ходьби. Нова ходьба рахує фазу кроку від фактичної швидкості корпуса і довжини кроку."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedAnimationMinSpeedMultiplier",
                    "Застарілий мінімальний множник старої схеми. Для звичайної ходьби більше не примушує ноги рухатись швидше за корпус, щоб стопи не просковзували."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedAnimationMaxSpeedMultiplier",
                    "Верхній ліміт частоти кроку. Якщо поставити занадто низько для максимальної швидкості робота і довжини кроку, стопи знову можуть просковзувати."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedAnimationSpeedExponent",
                    "Застаріла крива старої схеми темпу ходьби. Нова ходьба напряму прив'язана до фактичної швидкості корпуса."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedStepDistanceMultiplier",
                    "Множник довжини кроку меха. Більше значення сильніше розносить ноги вперед і назад."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedStepHeightMultiplier",
                    "Множник висоти підняття ноги під час кроку. Більше значення робить крок вищим."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedTurnStepDurationMultiplier",
                    "Множник тривалості кроку під час повороту на місці. Більше значення робить такі кроки повільнішими."
                );
                DrawChildProperty(
                    robotMovement,
                    "leggedTransitionSpeedMultiplier",
                    "Множник швидкості переходу між idle, ходьбою та поворотом для анімації ніг."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Grounding", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    robotMovement,
                    "gravity",
                    "Сила гравітації, яку використовує контролер руху робота. Більше значення швидше притискає робота вниз."
                );
                DrawChildProperty(
                    robotMovement,
                    "groundedSnap",
                    "Додаткове притискання до землі, коли робот стоїть або рухається по поверхні. Допомагає не відриватися від схилів і нерівностей."
                );
            }
        }

        private void DrawProjectileBallistics()
        {
            SerializedProperty projectileBallistics = serializedObject.FindProperty("projectileBallistics");
            _showProjectileBallistics = EditorGUILayout.Foldout(
                _showProjectileBallistics,
                new GUIContent("Projectile Ballistics", "Server authoritative projectile arc settings. Clients receive these values for prediction."),
                true
            );

            if (!_showProjectileBallistics)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Trajectory", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    projectileBallistics,
                    "projectileGravity",
                    "Downward acceleration used by projectile simulation. Higher values make a bigger arc; 0 disables drop."
                );
                DrawChildProperty(
                    projectileBallistics,
                    "useBallisticCompensation",
                    "Aims the initial velocity upward so the ballistic path passes near the aim point at the configured speed."
                );
                DrawChildProperty(
                    projectileBallistics,
                    "preferHighArc",
                    "Uses the high-angle ballistic solution when available. Keep this off for regular tank shells."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Debug", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    projectileBallistics,
                    "debugBallisticTrajectory",
                    "Enables optional trajectory, aim point, gravity, and sweep debug visualization."
                );
            }
        }

        private void DrawGunDispersion()
        {
            SerializedProperty gunDispersion = serializedObject.FindProperty("gunDispersion");
            _showGunDispersion = EditorGUILayout.Foldout(
                _showGunDispersion,
                new GUIContent("Gun Dispersion", "Глобальні серверні параметри розкиду зброї, синхронізації та розміру прицілу."),
                true
            );

            if (!_showGunDispersion)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                DrawChildProperty(
                    gunDispersion,
                    "enabled",
                    "Вмикає або вимикає глобальну систему розкиду. Якщо вимкнено, стрільба використовує мінімальний розкид."
                );
                DrawChildProperty(
                    gunDispersion,
                    "expandTime",
                    "Час у секундах, за який розкид розширюється до нового значення після руху, повороту або пострілу."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Reference Speeds", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    gunDispersion,
                    "referenceHullTraverseDegPerSec",
                    "Опорна швидкість повороту корпусу в градусах за секунду, при якій штраф розкиду від повороту корпусу вважається повним."
                );
                DrawChildProperty(
                    gunDispersion,
                    "referenceTurretTraverseDegPerSec",
                    "Опорна швидкість повороту башти в градусах за секунду, при якій штраф розкиду від повороту башти вважається повним."
                );
                DrawChildProperty(
                    gunDispersion,
                    "referenceGunTraverseDegPerSec",
                    "Опорна швидкість руху гармати в градусах за секунду, при якій штраф розкиду від наведення гармати вважається повним."
                );
                DrawChildProperty(
                    gunDispersion,
                    "referenceCameraAimDegPerSec",
                    "Опорна швидкість руху камери або прицілу в градусах за секунду, при якій клієнтський штраф розкиду від наведення вважається повним."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Database Accuracy", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    gunDispersion,
                    "accuracyReferenceDistanceMeters",
                    "Reference distance for database accuracy. Accuracy is interpreted as meters of spread at this distance."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("UI", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    gunDispersion,
                    "uiMinDiameter",
                    "Мінімальний діаметр кола прицілу в пікселях, коли розкид мінімальний."
                );
                DrawChildProperty(
                    gunDispersion,
                    "uiMaxDiameter",
                    "Максимальний діаметр кола прицілу в пікселях. Обмежує візуальний розмір при великому розкиді."
                );
                DrawChildProperty(
                    gunDispersion,
                    "uiFullyAimedPixelsPerDegreeAtMaxZoom",
                    "Pixels per degree for fully aimed accuracy at maximum zoom/sniper mode."
                );

                DrawChildProperty(
                    gunDispersion,
                    "uiFullyAimedPixelsPerDegreeAtMaxDistance",
                    "Pixels per degree for fully aimed accuracy at maximum third-person camera distance."
                );
                DrawChildProperty(
                    gunDispersion,
                    "uiBloomPixelsPerDegreeAtMaxZoom",
                    "Pixels per degree for movement/rotation/shot bloom at maximum zoom/sniper mode."
                );
                DrawChildProperty(
                    gunDispersion,
                    "uiBloomPixelsPerDegreeAtMaxDistance",
                    "Pixels per degree for movement/rotation/shot bloom at maximum third-person camera distance."
                );

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Networking", EditorStyles.miniBoldLabel);
                DrawChildProperty(
                    gunDispersion,
                    "serverSyncInterval",
                    "Інтервал у секундах між серверними синхронізаціями поточного розкиду. Менше значення точніше, але частіше надсилає дані."
                );
                DrawChildProperty(
                    gunDispersion,
                    "serverSyncDeadZoneDeg",
                    "Мінімальна зміна розкиду в градусах, після якої сервер надсилає оновлення. Більше значення зменшує дрібні мережеві оновлення."
                );
            }
        }

        private void DrawProperty(string propertyName, string tooltip)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            DrawSerializedProperty(property, tooltip);
        }

        private static void DrawChildProperty(SerializedProperty parent, string relativePropertyName, string tooltip)
        {
            if (parent == null)
            {
                return;
            }

            SerializedProperty property = parent.FindPropertyRelative(relativePropertyName);
            DrawSerializedProperty(property, tooltip);
        }

        private static void DrawSerializedProperty(SerializedProperty property, string tooltip)
        {
            if (property == null)
            {
                return;
            }

            GUIContent label = new GUIContent(property.displayName, tooltip);
            EditorGUILayout.PropertyField(property, label, true);
        }
    }
}
