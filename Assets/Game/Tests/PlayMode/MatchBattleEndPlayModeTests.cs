using System;
using System.Reflection;
using NUnit.Framework;

namespace Game.Tests.PlayMode
{
    public sealed class MatchBattleEndPlayModeTests
    {
        [Test]
        public void ConnectedDeadPlayerStillReceivesEndGameAfterVehicleDespawn()
        {
            object player = CreatePlayer(isBot: false, leftBattle: false, hasConnection: true);

            Assert.IsTrue(ShouldReceiveEndGame(player));
        }

        [Test]
        public void DisconnectedOrAbandonedPlayerDoesNotReceiveEndGame()
        {
            object disconnectedPlayer = CreatePlayer(isBot: false, leftBattle: false, hasConnection: false);
            object abandonedPlayer = CreatePlayer(isBot: false, leftBattle: true, hasConnection: true);

            Assert.IsFalse(ShouldReceiveEndGame(disconnectedPlayer));
            Assert.IsFalse(ShouldReceiveEndGame(abandonedPlayer));
        }

        private static object CreatePlayer(bool isBot, bool leftBattle, bool hasConnection)
        {
            Type playerType = Type.GetType("Game.Scripts.Networking.Lobby.Player, Assembly-CSharp");
            Assert.IsNotNull(playerType);

            object player = Activator.CreateInstance(playerType);
            playerType.GetField("isBot", BindingFlags.Public | BindingFlags.Instance).SetValue(player, isBot);
            playerType.GetField("leftBattle", BindingFlags.Public | BindingFlags.Instance).SetValue(player, leftBattle);

            if (hasConnection)
            {
                Type connectionType = Type.GetType("FishNet.Connection.NetworkConnection, Assembly-CSharp");
                Assert.IsNotNull(connectionType);
                object connection = Activator.CreateInstance(connectionType);
                connectionType.GetField("ClientId", BindingFlags.Public | BindingFlags.Instance).SetValue(connection, 7);
                playerType.GetField("Connection", BindingFlags.Public | BindingFlags.Instance).SetValue(player, connection);
            }

            return player;
        }

        private static bool ShouldReceiveEndGame(object player)
        {
            Type battleRulesType = Type.GetType("Game.Scripts.Networking.Lobby.BattleRules, Assembly-CSharp");
            Assert.IsNotNull(battleRulesType);

            MethodInfo method = battleRulesType.GetMethod(
                "ShouldReceiveEndGame",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);
            return (bool)method.Invoke(null, new[] { player });
        }
    }
}
