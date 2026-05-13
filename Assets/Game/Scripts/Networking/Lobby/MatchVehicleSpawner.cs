using System;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Server;
using Game.Scripts.API.Models;
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

            VehicleRoot vehicle = ResourceManager.GetPrefab(vehicleCode);
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
    }
}
