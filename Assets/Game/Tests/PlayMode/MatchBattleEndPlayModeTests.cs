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

        [Test]
        public void NetworkedDeathReleasesDebrisOnlyForClientObservers()
        {
            Type deathLogicType = Type.GetType("DeathLogic, Assembly-CSharp");
            Assert.IsNotNull(deathLogicType);

            MethodInfo method = deathLogicType.GetMethod(
                "ShouldReleaseClientDebris",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            Assert.IsTrue(ShouldReleaseClientDebris(method, hasSpawnedNetworkObject: false, isClientInitialized: false));
            Assert.IsFalse(ShouldReleaseClientDebris(method, hasSpawnedNetworkObject: true, isClientInitialized: false));
            Assert.IsTrue(ShouldReleaseClientDebris(method, hasSpawnedNetworkObject: true, isClientInitialized: true));
        }

        [Test]
        public void AuthoritativeDeathDespawnsOnlySpawnedServerVehicles()
        {
            Type vehicleHealthType = Type.GetType(
                "Game.Scripts.Gameplay.Robots.VehicleHealth, Assembly-CSharp");
            Assert.IsNotNull(vehicleHealthType);

            MethodInfo method = vehicleHealthType.GetMethod(
                "ShouldDespawnAfterDeath",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);

            Assert.IsFalse(ShouldDespawnAfterDeath(method, isServerInitialized: false, isSpawned: true));
            Assert.IsFalse(ShouldDespawnAfterDeath(method, isServerInitialized: true, isSpawned: false));
            Assert.IsTrue(ShouldDespawnAfterDeath(method, isServerInitialized: true, isSpawned: true));
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

        private static bool ShouldReleaseClientDebris(
            MethodInfo method,
            bool hasSpawnedNetworkObject,
            bool isClientInitialized)
        {
            return (bool)method.Invoke(null, new object[] { hasSpawnedNetworkObject, isClientInitialized });
        }

        private static bool ShouldDespawnAfterDeath(
            MethodInfo method,
            bool isServerInitialized,
            bool isSpawned)
        {
            return (bool)method.Invoke(null, new object[] { isServerInitialized, isSpawned });
        }
    }
}
