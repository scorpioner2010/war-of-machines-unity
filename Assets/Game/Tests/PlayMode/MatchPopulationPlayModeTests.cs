using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class MatchPopulationPlayModeTests
    {
        private GameObject _roomObject;

        [TearDown]
        public void TearDown()
        {
            if (_roomObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_roomObject);
            }
        }

        [Test]
        public void MatchPopulationFillsTargetAndBalancesRealPlayersAndBots()
        {
            Type roomType = FindGameType("Game.Scripts.Networking.Lobby.ServerRoom");
            Type playerType = FindGameType("Game.Scripts.Networking.Lobby.Player");
            Type populationServiceType = FindGameType("Game.Scripts.Networking.Lobby.MatchBotPopulationService");
            Assert.IsNotNull(roomType);
            Assert.IsNotNull(playerType);
            Assert.IsNotNull(populationServiceType);

            _roomObject = new GameObject("MatchPopulationRegressionRoom");
            Component room = _roomObject.AddComponent(roomType);
            SetField(room, "maxPlayers", 20);

            IList players = GetField<IList>(room, "players");
            for (int i = 0; i < 6; i++)
            {
                players.Add(CreatePlayer(playerType, false, "Player " + i));
            }

            object populationService = Activator.CreateInstance(populationServiceType);
            MethodInfo fillToTarget = populationServiceType.GetMethod("FillToTarget", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(fillToTarget);

            int addedBots = (int)fillToTarget.Invoke(
                populationService,
                new object[] { room, 20, "ia_l1_starter" });

            Assert.That(addedBots, Is.EqualTo(14));
            Assert.That(players.Count, Is.EqualTo(20));
            Assert.That(GetField<int>(room, "maxPlayers"), Is.EqualTo(20));

            InvokeMethod(room, "AssignTeams");

            int teamAReal = 0;
            int teamABots = 0;
            int teamBReal = 0;
            int teamBBots = 0;
            foreach (object player in players)
            {
                bool isBot = GetField<bool>(player, "isBot");
                string team = GetField<object>(player, "team").ToString();
                if (team == "TeamA")
                {
                    if (isBot) teamABots++; else teamAReal++;
                }
                else if (team == "TeamB")
                {
                    if (isBot) teamBBots++; else teamBReal++;
                }
                else
                {
                    Assert.Fail("A match participant was not assigned to a team.");
                }
            }

            Assert.That(teamAReal, Is.EqualTo(3));
            Assert.That(teamABots, Is.EqualTo(7));
            Assert.That(teamBReal, Is.EqualTo(3));
            Assert.That(teamBBots, Is.EqualTo(7));
        }

        private static object CreatePlayer(Type playerType, bool isBot, string name)
        {
            object player = Activator.CreateInstance(playerType);
            SetField(player, "isBot", isBot);
            SetField(player, "loginName", name);
            SetField(player, "mmr", 1000);
            return player;
        }

        private static Type FindGameType(string typeName)
        {
            return Type.GetType(typeName + ", Assembly-CSharp");
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing field " + fieldName + " on " + target.GetType().Name + ".");
            return (T)field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing field " + fieldName + " on " + target.GetType().Name + ".");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Missing method " + methodName + " on " + target.GetType().Name + ".");
            method.Invoke(target, null);
        }
    }
}
