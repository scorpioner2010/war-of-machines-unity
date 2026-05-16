using System;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Server;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Resources;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.World.Spawns;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Networking.Lobby
{
    public sealed class MatchVehicleSpawner
    {
        public async UniTask<VehicleRoot> SpawnPlayerAsync(
            ServerRoom serverRoom,
            NetworkConnection connection,
            UEScene additiveServerScene,
            float sceneValidationTimeout,
            ServerManager serverManager,
            Action<VehicleRoot> onVehicleSpawned)
        {
            float elapsedTime = 0f;

            while (!additiveServerScene.IsValid() && elapsedTime < sceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!additiveServerScene.IsValid())
            {
                return null;
            }

            Player player = serverRoom.GetPlayerByConnection(connection);
            if (player == null)
            {
                return null;
            }

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(additiveServerScene, player.team);
            if (spawnPoint == null)
            {
                return null;
            }

            PlayerProfile profile = ServerPlayerSessions.GetProfile(connection.ClientId);
            if (profile == null)
            {
                return null;
            }

            string vehicleCode = !string.IsNullOrEmpty(player.activeVehicleCode)
                ? player.activeVehicleCode
                : profile.activeVehicleCode;

            VehicleRoot vehicle = GameResourceManager.GetPrefab(vehicleCode);
            if (vehicle == null)
            {
                return null;
            }

            VehicleRoot vehicleRoot = UnityEngine.Object.Instantiate(vehicle, spawnPoint.transform.position, spawnPoint.transform.rotation);
            VehicleRuntimeStats stats = await VehicleStatsProvider.GetAsync(player.activeVehicleId, vehicleCode);
            if (stats != null)
            {
                vehicleRoot.ServerApplyRuntimeStats(stats, syncObservers: false);
            }

            serverManager.Spawn(vehicleRoot.networkObject, connection, additiveServerScene);
            if (stats != null)
            {
                vehicleRoot.ServerApplyRuntimeStats(stats, syncObservers: true);
            }

            player.playerRoot = vehicleRoot;
            if (onVehicleSpawned != null)
            {
                onVehicleSpawned(vehicleRoot);
            }

            player.playerRoot.characterInit.ServerInit(serverRoom.maxPlayers, PlayerType.Player, player.loginName, player.team, additiveServerScene);
            return vehicleRoot;
        }

        public async UniTask<VehicleRoot> SpawnBotAsync(
            ServerRoom serverRoom,
            Player player,
            UEScene additiveServerScene,
            float sceneValidationTimeout,
            ServerManager serverManager,
            Action<VehicleRoot> onVehicleSpawned)
        {
            float elapsedTime = 0f;

            while (!additiveServerScene.IsValid() && elapsedTime < sceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!additiveServerScene.IsValid())
            {
                return null;
            }

            if (serverRoom == null || player == null || !player.isBot)
            {
                return null;
            }

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(additiveServerScene, player.team);
            if (spawnPoint == null)
            {
                return null;
            }

            string vehicleCode = !string.IsNullOrEmpty(player.activeVehicleCode)
                ? player.activeVehicleCode
                : GameResourceManager.GetFirstVehicleCode();

            if (string.IsNullOrEmpty(vehicleCode))
            {
                Debug.LogWarning("Cannot spawn bot: vehicle code is empty.");
                return null;
            }

            VehicleRoot vehicle = GameResourceManager.GetPrefab(vehicleCode);
            if (vehicle == null)
            {
                Debug.LogWarning("Cannot spawn bot: vehicle prefab was not found. code=" + vehicleCode);
                return null;
            }

            VehicleRoot vehicleRoot = UnityEngine.Object.Instantiate(vehicle, spawnPoint.transform.position, spawnPoint.transform.rotation);
            VehicleRuntimeStats stats = await VehicleStatsProvider.GetAsync(player.activeVehicleId, vehicleCode);
            if (stats != null)
            {
                vehicleRoot.ServerApplyRuntimeStats(stats, syncObservers: false);
            }

            serverManager.Spawn(vehicleRoot.networkObject, null, additiveServerScene);
            if (stats != null)
            {
                vehicleRoot.ServerApplyRuntimeStats(stats, syncObservers: true);
            }

            player.playerRoot = vehicleRoot;
            if (onVehicleSpawned != null)
            {
                onVehicleSpawned(vehicleRoot);
            }

            player.playerRoot.characterInit.ServerInit(serverRoom.maxPlayers, PlayerType.Bot, player.loginName, player.team, additiveServerScene);

            VehicleBotBrain brain = vehicleRoot.GetComponent<VehicleBotBrain>();
            if (brain == null)
            {
                brain = vehicleRoot.gameObject.AddComponent<VehicleBotBrain>();
            }

            brain.StartBrain(vehicleRoot, serverRoom);
            return vehicleRoot;
        }
    }
}
