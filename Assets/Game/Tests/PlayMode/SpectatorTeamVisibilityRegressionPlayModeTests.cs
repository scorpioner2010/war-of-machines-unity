using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class SpectatorTeamVisibilityRegressionPlayModeTests
    {
        [Test]
        public void SpectatorTeamContextKeepsBothTeamRelationsAfterLocalVehicleDespawn()
        {
            GameObject targetObject = new GameObject("SpectatorRelationTarget");
            try
            {
                Type rootType = GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Type initializerType = GetType(
                    "Game.Scripts.Gameplay.Robots.VehicleNetworkInitializer, Assembly-CSharp");
                Type contextType = GetType("Game.Scripts.Gameplay.Robots.SpectatorTeamContext, Assembly-CSharp");
                Type resolverType = GetType(
                    "Game.Scripts.Gameplay.Robots.VehicleTeamRelationResolver, Assembly-CSharp");
                Type teamType = GetType("Game.Scripts.Networking.Lobby.MatchTeam, Assembly-CSharp");
                Component targetRoot = targetObject.AddComponent(rootType);
                Component initializer = targetObject.AddComponent(initializerType);
                SetField(targetRoot, "characterInit", initializer);
                MethodInfo capture = contextType.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static);
                MethodInfo resolve = resolverType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
                object teamA = Enum.Parse(teamType, "TeamA");
                object teamB = Enum.Parse(teamType, "TeamB");

                capture.Invoke(null, new[] { teamA });
                SetTeam(initializer, teamA);
                Assert.That(resolve.Invoke(null, new[] { targetRoot }).ToString(), Is.EqualTo("Ally"));

                SetTeam(initializer, teamB);
                Assert.That(resolve.Invoke(null, new[] { targetRoot }).ToString(), Is.EqualTo("Enemy"));

                capture.Invoke(null, new[] { teamB });
                SetTeam(initializer, teamB);
                Assert.That(resolve.Invoke(null, new[] { targetRoot }).ToString(), Is.EqualTo("Ally"));
            }
            finally
            {
                Type contextType = Type.GetType(
                    "Game.Scripts.Gameplay.Robots.SpectatorTeamContext, Assembly-CSharp");
                contextType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void SnapshotRecipientsKeepDeadPlayerTeamWhenRootWasDespawned()
        {
            Type recipientsType = GetType(
                "Game.Scripts.Networking.Lobby.MatchVisibilitySnapshotRecipients, Assembly-CSharp");
            Type playerType = GetType("Game.Scripts.Networking.Lobby.Player, Assembly-CSharp");
            Type snapshotType = GetType(
                "Game.Scripts.Networking.Lobby.MatchVisibilitySnapshot, Assembly-CSharp");
            Type teamType = GetType("Game.Scripts.Networking.Lobby.MatchTeam, Assembly-CSharp");
            object recipients = Activator.CreateInstance(recipientsType);
            object deadPlayer = Activator.CreateInstance(playerType);
            object teamA = Enum.Parse(teamType, "TeamA");
            object teamB = Enum.Parse(teamType, "TeamB");
            SetField(deadPlayer, "team", teamA);
            SetField(deadPlayer, "isBot", false);
            SetField(deadPlayer, "leftBattle", false);
            SetField(deadPlayer, "playerRoot", null);
            object teamASnapshot = Activator.CreateInstance(snapshotType);
            object teamBSnapshot = Activator.CreateInstance(snapshotType);
            SetField(teamASnapshot, "Version", 7);
            SetField(teamBSnapshot, "Version", 9);
            MethodInfo method = recipientsType.GetMethod("TryGetSnapshot");
            object[] arguments = { deadPlayer, teamASnapshot, teamBSnapshot, null };

            bool resolved = (bool)method.Invoke(recipients, arguments);
            object selectedSnapshot = arguments[3];

            Assert.IsTrue(resolved);
            Assert.AreEqual(7, GetField(selectedSnapshot, "Version"));

            SetField(deadPlayer, "team", teamB);
            resolved = (bool)method.Invoke(recipients, arguments);
            selectedSnapshot = arguments[3];

            Assert.IsTrue(resolved);
            Assert.AreEqual(9, GetField(selectedSnapshot, "Version"));
        }

        private static Type GetType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, assemblyQualifiedName);
            return type;
        }

        private static void SetTeam(Component initializer, object team)
        {
            FieldInfo teamField = initializer.GetType().GetField("Team", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(teamField);
            object teamSyncVar = teamField.GetValue(initializer);
            SetField(teamSyncVar, "_initialValue", team);
            SetField(teamSyncVar, "_value", team);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(target);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
