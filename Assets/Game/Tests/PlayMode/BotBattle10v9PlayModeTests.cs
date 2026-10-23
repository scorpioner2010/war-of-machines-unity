using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public sealed class BotBattle10v9PlayModeTests
    {
        private const int EnemyBotCount = 10;
        private const int AllyBotCount = 9;
        private const float SetupTimeoutSeconds = 45f;
        private const float BattleTimeoutSeconds = 300f;

        [UnityTest]
        [Category("Integration")]
        [Timeout(360000)]
        public IEnumerator TenEnemyBotsAndNineAllyBotsRunUntilOneTeamFalls()
        {
            SceneManager.LoadScene("Client", LoadSceneMode.Single);
            yield return null;

            Component controller = null;
            yield return WaitUntil(
                () =>
                {
                    Type controllerType = Type.GetType("Game.Scripts.Testing.VehicleTestSceneController, Assembly-CSharp");
                    controller = controllerType != null
                        ? UnityEngine.Object.FindFirstObjectByType(controllerType, FindObjectsInactive.Include) as Component
                        : null;
                    return controller != null;
                },
                SetupTimeoutSeconds,
                "VehicleTestSceneController was not activated in Client scene.");

            yield return WaitUntil(
                () => InvokePrivate<bool>(controller, "IsNetworkReady"),
                SetupTimeoutSeconds,
                "Vehicle test host/client did not become ready.");

            yield return WaitUntil(
                () => GetPrivateArrayLength(controller, "_vehicles") > 0,
                SetupTimeoutSeconds,
                "Vehicle catalog did not load. status=" + GetPrivateField<string>(controller, "_status"));

            SetPrivateField(controller, "_selectedIndex", 0);
            InvokePrivate(controller, "SpawnSelectedAsync");
            yield return WaitUntil(
                () => GetPrivateField<Component>(controller, "_spawnedVehicle") != null,
                SetupTimeoutSeconds,
                "Vehicle test player did not spawn. status=" + GetPrivateField<string>(controller, "_status"));

            for (int enemyIndex = 0; enemyIndex < EnemyBotCount; enemyIndex++)
            {
                yield return SpawnBot(controller, ally: false, enemyIndex + 1);
            }

            for (int allyIndex = 0; allyIndex < AllyBotCount; allyIndex++)
            {
                yield return SpawnBot(controller, ally: true, allyIndex + 1);
            }

            object room = GetPrivateField<object>(controller, "_testRoom");
            Assert.IsNotNull(room);
            Assert.That(CountBots(room), Is.EqualTo(EnemyBotCount + AllyBotCount));

            Dictionary<int, float> previousHullYaw = new Dictionary<int, float>();
            Dictionary<int, int> previousHullDirections = new Dictionary<int, int>();
            Dictionary<int, int> hullDirectionFlips = new Dictionary<int, int>();
            Dictionary<int, float> previousTurretYaw = new Dictionary<int, float>();
            Dictionary<int, int> previousTurretDirections = new Dictionary<int, int>();
            Dictionary<int, int> turretDirectionFlips = new Dictionary<int, int>();
            float nextTelemetryTime = Time.realtimeSinceStartup;
            float nextDetailedTelemetryTime = nextTelemetryTime + 5f;
            float battleEndTime = Time.realtimeSinceStartup + BattleTimeoutSeconds;
            int maximumHullDirectionFlips = 0;
            int maximumTurretDirectionFlips = 0;

            while (Time.realtimeSinceStartup < battleEndTime && HasLiveBotsOnBothTeams(room))
            {
                if (Time.realtimeSinceStartup >= nextTelemetryTime)
                {
                    nextTelemetryTime += 1f;
                    CollectTelemetry(
                        room,
                        previousHullYaw,
                        previousHullDirections,
                        hullDirectionFlips,
                        previousTurretYaw,
                        previousTurretDirections,
                        turretDirectionFlips,
                        ref maximumHullDirectionFlips,
                        ref maximumTurretDirectionFlips);
                    Debug.Log(
                        "[BotBattle10v9] alive enemy=" + CountAliveBots(room, false)
                        + " ally=" + CountAliveBots(room, true)
                        + " maxHullDirectionFlips=" + maximumHullDirectionFlips
                        + " maxTurretDirectionFlips=" + maximumTurretDirectionFlips);

                    if (Time.realtimeSinceStartup >= nextDetailedTelemetryTime)
                    {
                        nextDetailedTelemetryTime += 5f;
                        Debug.Log(BuildDetailedBotTelemetry(room));
                    }
                }

                yield return null;
            }

            Assert.IsFalse(
                HasLiveBotsOnBothTeams(room),
                "10v9 battle did not finish before timeout. alive enemy="
                + CountAliveBots(room, false)
                + ", alive ally="
                + CountAliveBots(room, true)
                + ", maxHullDirectionFlips="
                + maximumHullDirectionFlips
                + ", maxTurretDirectionFlips="
                + maximumTurretDirectionFlips);
        }

        private static IEnumerator SpawnBot(Component controller, bool ally, int index)
        {
            InvokePrivate(controller, "SpawnRandomBotAsync", ally);
            yield return WaitUntil(
                () =>
                {
                    object room = GetPrivateField<object>(controller, "_testRoom");
                    return CountBots(room) >= index;
                },
                SetupTimeoutSeconds,
                "Timed out spawning " + (ally ? "ally" : "enemy") + " bot " + index + ".");

            yield return WaitUntil(
                () => !GetPrivateField<bool>(controller, "_botSpawnInProgress"),
                SetupTimeoutSeconds,
                "Bot spawn flag did not clear for " + (ally ? "ally" : "enemy") + " bot " + index + ".");
        }

        private static void CollectTelemetry(
            object room,
            Dictionary<int, float> previousHullYaw,
            Dictionary<int, int> previousHullDirections,
            Dictionary<int, int> hullDirectionFlips,
            Dictionary<int, float> previousTurretYaw,
            Dictionary<int, int> previousTurretDirections,
            Dictionary<int, int> turretDirectionFlips,
            ref int maximumHullDirectionFlips,
            ref int maximumTurretDirectionFlips)
        {
            IList players = GetPlayers(room);
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                object player = players[playerIndex];
                Component root = GetPlayerField<Component>(player, "playerRoot");
                if (player == null || !GetPlayerField<bool>(player, "isBot") || root == null || IsDead(root))
                {
                    continue;
                }

                Component objectMover = GetComponentField(root, "objectMover");
                Component turret = GetComponentField(root, "robotHullRotation");
                float hullYaw = objectMover != null ? objectMover.transform.eulerAngles.y : 0f;
                float turretYaw = GetFloatProperty(turret, "CurrentLocalYaw");
                int playerId = playerIndex;
                UpdateDirectionFlip(
                    previousHullYaw,
                    previousHullDirections,
                    hullDirectionFlips,
                    playerId,
                    hullYaw,
                    out int hullFlips);
                UpdateDirectionFlip(
                    previousTurretYaw,
                    previousTurretDirections,
                    turretDirectionFlips,
                    playerId,
                    turretYaw,
                    out int turretFlips);
                maximumHullDirectionFlips = Mathf.Max(maximumHullDirectionFlips, hullFlips);
                maximumTurretDirectionFlips = Mathf.Max(maximumTurretDirectionFlips, turretFlips);
            }
        }

        private static void UpdateDirectionFlip(
            Dictionary<int, float> previousAngles,
            Dictionary<int, int> previousDirections,
            Dictionary<int, int> directionFlips,
            int playerId,
            float currentAngle,
            out int flipCount)
        {
            flipCount = directionFlips.TryGetValue(playerId, out int existingFlips) ? existingFlips : 0;
            if (previousAngles.TryGetValue(playerId, out float previousAngle))
            {
                float delta = Mathf.DeltaAngle(previousAngle, currentAngle);
                if (Mathf.Abs(delta) > 0.5f)
                {
                    int direction = delta > 0f ? 1 : -1;
                    if (previousDirections.TryGetValue(playerId, out int previousDirection)
                        && previousDirection != direction)
                    {
                        flipCount++;
                    }

                    directionFlips[playerId] = flipCount;
                    previousDirections[playerId] = direction;
                }
            }

            previousAngles[playerId] = currentAngle;
        }

        private static string BuildDetailedBotTelemetry(object room)
        {
            IList players = GetPlayers(room);
            StringBuilder telemetry = new StringBuilder("[BotBattle10v9Detail]");
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                object player = players[playerIndex];
                Component root = GetPlayerField<Component>(player, "playerRoot");
                if (player == null || !GetPlayerField<bool>(player, "isBot") || root == null || IsDead(root))
                {
                    continue;
                }

                Component objectMover = GetComponentField(root, "objectMover");
                Component botBrain = GetComponentField(root, "botBrain");
                Component navigator = GetComponentField(botBrain, "navigator");
                Component health = GetComponentField(root, "health");
                Component weapon = GetComponentField(root, "weaponReloadController");
                Component targetRoot = GetCurrentTargetRoot(botBrain);
                bool hasDesiredDirection = GetDesiredTravelDirection(navigator, out Vector3 desiredDirection);
                telemetry.Append(" ")
                    .Append(GetPlayerField<string>(player, "loginName"))
                    .Append(" pos=")
                    .Append(objectMover != null ? objectMover.transform.position.ToString("F1") : "none")
                    .Append(" target=")
                    .Append(FindPlayerName(players, targetRoot))
                    .Append(" desired=")
                    .Append(hasDesiredDirection ? desiredDirection.ToString("F1") : "none")
                    .Append(" hp=")
                    .Append(GetFloatProperty(health, "Current").ToString("F0"))
                    .Append(" ammo=")
                    .Append(GetIntProperty(weapon, "ServerAmmoLeft"));
            }

            return telemetry.ToString();
        }

        private static Component GetCurrentTargetRoot(Component botBrain)
        {
            if (botBrain == null)
            {
                return null;
            }

            PropertyInfo property = botBrain.GetType().GetProperty(
                "CurrentTargetRoot",
                BindingFlags.Public | BindingFlags.Instance);
            return property != null ? property.GetValue(botBrain) as Component : null;
        }

        private static bool GetDesiredTravelDirection(
            Component navigator,
            out Vector3 desiredDirection)
        {
            desiredDirection = Vector3.zero;
            if (navigator == null)
            {
                return false;
            }

            MethodInfo method = navigator.GetType().GetMethod(
                "TryGetDesiredTravelDirection",
                BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return false;
            }

            object[] arguments = { Vector3.zero, 10f };
            bool hasDirection = (bool)method.Invoke(navigator, arguments);
            if (hasDirection && arguments[0] is Vector3 direction)
            {
                desiredDirection = direction;
            }

            return hasDirection;
        }

        private static string FindPlayerName(IList players, Component targetRoot)
        {
            if (targetRoot == null)
            {
                return "none";
            }

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                object player = players[playerIndex];
                if (GetPlayerField<Component>(player, "playerRoot") == targetRoot)
                {
                    return GetPlayerField<string>(player, "loginName") ?? "unnamed";
                }
            }

            return "external";
        }

        private static bool HasLiveBotsOnBothTeams(object room)
        {
            return CountAliveBots(room, false) > 0 && CountAliveBots(room, true) > 0;
        }

        private static int CountBots(object room)
        {
            IList players = GetPlayers(room);
            int count = 0;
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                object player = players[playerIndex];
                if (player != null && GetPlayerField<bool>(player, "isBot") && GetPlayerField<Component>(player, "playerRoot") != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAliveBots(object room, bool ally)
        {
            IList players = GetPlayers(room);
            int count = 0;
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                object player = players[playerIndex];
                if (player == null || !GetPlayerField<bool>(player, "isBot"))
                {
                    continue;
                }

                Component root = GetPlayerField<Component>(player, "playerRoot");
                if (root == null || IsDead(root))
                {
                    continue;
                }

                string playerName = GetPlayerField<string>(player, "loginName");
                if ((playerName != null && playerName.StartsWith("Ally ")) == ally)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsDead(Component root)
        {
            Component health = GetComponentField(root, "health");
            return GetBoolProperty(health, "IsDead");
        }

        private static Component GetComponentField(Component target, string fieldName)
        {
            if (target == null)
            {
                return null;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            return field != null ? field.GetValue(target) as Component : null;
        }

        private static float GetFloatProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return 0f;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.GetValue(target) is float value ? value : 0f;
        }

        private static int GetIntProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return 0;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.GetValue(target) is int value ? value : 0;
        }

        private static bool GetBoolProperty(object target, string propertyName)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property != null && property.GetValue(target) is bool value && value;
        }

        private static IList GetPlayers(object room)
        {
            if (room == null)
            {
                return Array.Empty<object>();
            }

            FieldInfo playersField = room.GetType().GetField("players", BindingFlags.Public | BindingFlags.Instance);
            return playersField != null && playersField.GetValue(room) is IList players
                ? players
                : Array.Empty<object>();
        }

        private static T GetPlayerField<T>(object player, string fieldName)
        {
            if (player == null)
            {
                return default;
            }

            FieldInfo field = player.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            return field != null && field.GetValue(player) is T value ? value : default;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            if (target == null)
            {
                return default;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null && field.GetValue(target) is T value ? value : default;
        }

        private static int GetPrivateArrayLength(object target, string fieldName)
        {
            object value = GetPrivateField<object>(target, fieldName);
            return value is Array array ? array.Length : 0;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing private field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private static T InvokePrivate<T>(object target, string methodName, params object[] arguments)
        {
            object result = InvokePrivate(target, methodName, arguments);
            return result is T value ? value : default;
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Missing private test method " + methodName + ".");
            return method.Invoke(target, arguments);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string failureMessage)
        {
            float endTime = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < endTime && !condition())
            {
                yield return null;
            }

            Assert.IsTrue(condition(), failureMessage);
        }
    }
}
