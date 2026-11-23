using System;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Server;
using RAV.Scripts.API.Models;
using RAV.Scripts.Core.Resources;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Gameplay.Robots;
using RAV.Scripts.Networking.Sessions;
using RAV.Scripts.World.Spawns;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace RAV.Scripts.Networking.Lobby
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
            int clientId = connection != null ? connection.ClientId : -1;
            GameplayRuntimeDiagnostics.Record(
                "player_spawn_requested",
                "client_id", clientId,
                "scene", additiveServerScene.IsValid() ? additiveServerScene.name : "invalid");

            if (connection == null)
            {
                GameplayRuntimeDiagnostics.Record("player_spawn_failed", "reason", "connection_missing", "client_id", clientId);
                return null;
            }

            float elapsedTime = 0f;

            while (!additiveServerScene.IsValid() && elapsedTime < sceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!additiveServerScene.IsValid())
            {
                GameplayRuntimeDiagnostics.Record(
                    "player_spawn_failed",
                    "reason", "scene_invalid_or_timeout",
                    "client_id", clientId,
                    "wait_seconds", elapsedTime);
                return null;
            }

            Player player = serverRoom.GetPlayerByConnection(connection);
            if (player == null)
            {
                GameplayRuntimeDiagnostics.Record("player_spawn_failed", "reason", "room_player_missing", "client_id", clientId);
                return null;
            }

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(additiveServerScene, player.team);
            if (spawnPoint == null)
            {
                GameplayRuntimeDiagnostics.Record(
                    "player_spawn_failed",
                    "reason", "spawn_point_missing",
                    "client_id", clientId,
                    "username", player.loginName,
                    "team", player.team);
                return null;
            }

            PlayerProfile profile = ServerPlayerSessions.GetProfile(connection.ClientId);
            if (profile == null)
            {
                GameplayRuntimeDiagnostics.Record(
                    "player_spawn_failed",
                    "reason", "server_profile_missing",
                    "client_id", clientId,
                    "username", player.loginName,
                    "team", player.team);
                return null;
            }

            string vehicleCode = !string.IsNullOrEmpty(player.activeVehicleCode)
                ? player.activeVehicleCode
                : profile.activeVehicleCode;

            VehicleRoot vehicle = GameResourceManager.GetPrefab(vehicleCode);
            if (vehicle == null)
            {
                GameplayRuntimeDiagnostics.Record(
                    "player_spawn_failed",
                    "reason", "vehicle_prefab_missing",
                    "client_id", clientId,
                    "username", player.loginName,
                    "vehicle_code", vehicleCode);
                return null;
            }

            GameplayRuntimeDiagnostics.Record(
                "player_spawn_instantiating",
                "client_id", clientId,
                "user_id", player.userId,
                "username", player.loginName,
                "team", player.team,
                "vehicle_code", vehicleCode,
                "scene", additiveServerScene.name,
                "position", spawnPoint.transform.position);
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
            GameplayRuntimeDiagnostics.Record(
                "player_spawn_completed",
                "client_id", clientId,
                "user_id", player.userId,
                "username", player.loginName,
                "team", player.team,
                "vehicle_code", vehicleCode,
                "object_id", vehicleRoot.networkObject != null ? vehicleRoot.networkObject.ObjectId : -1,
                "position", vehicleRoot.transform.position,
                "scene", additiveServerScene.name);
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
            GameplayRuntimeDiagnostics.Record(
                "bot_spawn_requested",
                "username", player != null ? player.loginName : string.Empty,
                "team", player != null ? player.team : MatchTeam.None,
                "scene", additiveServerScene.IsValid() ? additiveServerScene.name : "invalid");

            float elapsedTime = 0f;

            while (!additiveServerScene.IsValid() && elapsedTime < sceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!additiveServerScene.IsValid())
            {
                GameplayRuntimeDiagnostics.Record(
                    "bot_spawn_failed",
                    "reason", "scene_invalid_or_timeout",
                    "username", player != null ? player.loginName : string.Empty,
                    "wait_seconds", elapsedTime);
                return null;
            }

            if (serverRoom == null || player == null || !player.isBot)
            {
                GameplayRuntimeDiagnostics.Record("bot_spawn_failed", "reason", "invalid_bot_player");
                return null;
            }

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(additiveServerScene, player.team);
            if (spawnPoint == null)
            {
                GameplayRuntimeDiagnostics.Record(
                    "bot_spawn_failed",
                    "reason", "spawn_point_missing",
                    "username", player.loginName,
                    "team", player.team);
                return null;
            }

            string vehicleCode = !string.IsNullOrEmpty(player.activeVehicleCode)
                ? player.activeVehicleCode
                : GameResourceManager.GetFirstVehicleCode();

            if (string.IsNullOrEmpty(vehicleCode))
            {
                GameplayRuntimeDiagnostics.Record("bot_spawn_failed", "reason", "vehicle_code_missing", "username", player.loginName);
                Debug.LogWarning("Cannot spawn bot: vehicle code is empty.");
                return null;
            }

            VehicleRoot vehicle = GameResourceManager.GetPrefab(vehicleCode);
            if (vehicle == null)
            {
                GameplayRuntimeDiagnostics.Record(
                    "bot_spawn_failed",
                    "reason", "vehicle_prefab_missing",
                    "username", player.loginName,
                    "vehicle_code", vehicleCode);
                Debug.LogWarning("Cannot spawn bot: vehicle prefab was not found. code=" + vehicleCode);
                return null;
            }

            GameplayRuntimeDiagnostics.Record(
                "bot_spawn_instantiating",
                "username", player.loginName,
                "team", player.team,
                "vehicle_code", vehicleCode,
                "scene", additiveServerScene.name,
                "position", spawnPoint.transform.position);
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

            VehicleBotBrain brain = vehicleRoot.botBrain;
            if (brain == null)
            {
                Debug.LogError("Cannot start bot brain: VehicleBotBrain reference is not assigned on vehicle prefab.");
                return vehicleRoot;
            }

            brain.StartBrain(vehicleRoot, serverRoom);
            GameplayRuntimeDiagnostics.Record(
                "bot_spawn_completed",
                "username", player.loginName,
                "team", player.team,
                "vehicle_code", vehicleCode,
                "object_id", vehicleRoot.networkObject != null ? vehicleRoot.networkObject.ObjectId : -1,
                "brain_running", brain.IsRunning,
                "position", vehicleRoot.transform.position,
                "scene", additiveServerScene.name);
            return vehicleRoot;
        }
    }
}
