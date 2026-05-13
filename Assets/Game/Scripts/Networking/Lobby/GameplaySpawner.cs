using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.UI.Lobby;
using Game.Scripts.UI.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UEScene = UnityEngine.SceneManagement.Scene;
using UESceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Game.Scripts.Networking.Lobby
{
    public enum GameMaps
    {
        Map = 0,
    }
    
    public class GameplaySpawner : NetworkBehaviour
    {
        public static GameplaySpawner In;
        public GameMaps[] scenes;
        public GameplayTimer gameplayTimerPrefab;
        
        private UEScene _additiveServerScene;
        private readonly MatchSceneOffsetService _sceneOffsetService = new MatchSceneOffsetService();
        private readonly MatchVehicleSpawner _vehicleSpawner = new MatchVehicleSpawner();
        public int sceneOffsetX;
        private const float SceneValidationTimeout = 10f;
        private const int EndGameDelayMilliseconds = 2000;

        private void Awake()
        {
            In = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            UESceneManager.sceneLoaded += HandleServerSceneLoaded;
            SceneManager.OnLoadEnd += HandleServerLoadEnd;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            UESceneManager.sceneLoaded -= HandleServerSceneLoaded;
            SceneManager.OnLoadEnd -= HandleServerLoadEnd;
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            PendingBattleResults.Clear();
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                List<ServerRoom> rooms = LobbyRooms.GetRoomsByConnection(conn);
                for (int i = 0; i < rooms.Count; i++)
                {
                    ServerRoom serverRoom = rooms[i];
                    if (serverRoom == null)
                    {
                        continue;
                    }
                
                    Player player = serverRoom.GetPlayerByConnection(conn);
                    if (player == null)
                    {
                        continue;
                    }

                    if (serverRoom.IsActiveMatch)
                    {
                        AbandonPlayer(serverRoom, player);
                        continue;
                    }

                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
                }
            }
        }

        private void HandleServerSceneLoaded(UEScene scene, LoadSceneMode mode)
        {
            if (!IsValidScene(scene))
            {
                return;
            }

            int usedOffset = sceneOffsetX;
            
            _sceneOffsetService.ApplyOffset(scene, usedOffset);

            sceneOffsetX += 500;
            _additiveServerScene = scene;

            ApplySceneOffsetClientRpc(scene.handle, usedOffset);
        }

        private void HandleServerLoadEnd(SceneLoadEndEventArgs args)
        {
            foreach (object param in args.QueueData.SceneLoadData.Params.ServerParams)
            {
                if (param is ServerRoom info)
                {
                    ServerRoom serverRoom = LobbyRooms.GetRoomById(info.roomId);
                    
                    foreach (Scene sc in args.LoadedScenes)
                    {
                        if (sc.name == serverRoom.loadedSceneName)
                        {
                            serverRoom.handle = sc.handle;
                        }
                    }
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UESceneManager.sceneLoaded += HandleClientSceneLoaded;
            SceneManager.OnLoadEnd += HandleClientLoadEnd;
            SceneManager.OnUnloadEnd += SceneManagerOnUnloadEnd;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            UESceneManager.sceneLoaded -= HandleClientSceneLoaded;
            SceneManager.OnLoadEnd -= HandleClientLoadEnd;
            SceneManager.OnUnloadEnd -= SceneManagerOnUnloadEnd;
        }

        private void HandleClientSceneLoaded(UEScene scene, LoadSceneMode mode)
        {
            if (!IsValidScene(scene))
            {
                return;
            }
            
            NotifyServerSceneLoaded(ClientManager.Connection.ClientId);
        }
        
        private void HandleClientLoadEnd(SceneLoadEndEventArgs args)
        {
            byte[] cp = args.QueueData.SceneLoadData.Params.ClientParams;
            int offset = (cp != null && cp.Length >= 4) ? BitConverter.ToInt32(cp, 0) : 0;

            foreach (Scene scene in args.LoadedScenes)
            {
                if (!IsValidScene(scene))
                {
                    continue;
                }
                
                _sceneOffsetService.ApplyOffset(scene, offset);
            }
        }

        private void SceneManagerOnUnloadEnd(SceneUnloadEndEventArgs obj)
        {
        }
        
        public void ReturnToMainMenu()
        {
            RobotView.GenerateIcons();

            if (MainMenu.In != null)
            {
                MainMenu.In.SetActive(true);
            }

            MenuManager.CloseMenu(MenuType.GameplayHUD);
            MenuManager.CloseMenu(MenuType.GameplayPause);
            
            RequestPlayerDisconnectServerRpc(ClientManager.Connection.ClientId);
            EnsureMainMenuAfterDisconnect().Forget();
        }

        private async UniTask EnsureMainMenuAfterDisconnect()
        {
            await UniTask.Delay(1200);

            if (MenuManager.CurrentType == MenuType.LoadScreen)
            {
                MenuManager.OpenMenu(MenuType.MainMenu);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerDisconnectServerRpc(int clientId)
        {
            if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn) == false)
            {
                return;
            }
            
            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);

            if (serverRoom != null)
            {
                Player player = serverRoom.GetPlayerByConnection(conn);
            
                if (player != null)
                {
                    if (serverRoom.IsActiveMatch)
                    {
                        AbandonPlayer(serverRoom, player);
                        SceneManager.UnloadConnectionScenes(conn, new SceneUnloadData(serverRoom.loadedSceneName));
                        return;
                    }

                    SceneManager.UnloadConnectionScenes(conn, new SceneUnloadData(serverRoom.loadedSceneName));

                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);

                    if (player.playerRoot != null && player.playerRoot.networkObject != null)
                    {
                        Despawn(player.playerRoot.networkObject);
                    }
                }
            }
        }

        [Server]
        private void AbandonPlayer(ServerRoom serverRoom, Player player)
        {
            if (serverRoom == null || player == null || player.leftBattle || serverRoom.isGameFinished)
            {
                return;
            }

            player.leftBattle = true;
            player.battleResult = "lose";

            if (player.playerRoot != null && player.playerRoot.inputManager != null)
            {
                player.playerRoot.inputManager.SetControlsBlocked(true);
            }

            if (player.playerRoot != null && player.playerRoot.health != null && !player.playerRoot.health.IsDead)
            {
                player.playerRoot.health.ServerKill();
            }

            player.Connection = null;
            EvaluateBattleEnd(serverRoom);
        }

        private bool IsValidScene(UEScene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scene.name.Contains(scenes[i].ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyServerSceneLoaded(int clientId)
        {
            if (!ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
            {
                return;
            }
            
            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);
            if (serverRoom == null)
            {
                return;
            }

            Player playerByConnection = serverRoom.GetPlayerByConnection(conn);
            if (playerByConnection == null)
            {
                return;
            }

            playerByConnection.randomPlayerConnected = true;
            bool allLoaded = serverRoom.AreAllRealPlayersLoaded();
            
            if (allLoaded)
            {
                SpawnBattleVehiclesAsync(serverRoom).Forget();
            }
        }

        private async UniTask SpawnBattleVehiclesAsync(ServerRoom serverRoom)
        {
            if (serverRoom == null || serverRoom.spawnStarted)
            {
                return;
            }

            serverRoom.spawnStarted = true;
            await VehicleStatsProvider.PreloadAsync();

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null)
                {
                    continue;
                }

                if (player.isBot)
                {
                    SpawnBot(serverRoom, player);
                }
                else
                {
                    await SpawnPlayerAsync(serverRoom, player.Connection);
                }
            }
                
            LobbyRooms.UpdateRoomStatusInGame(serverRoom.roomId);
            SpawnTimer(serverRoom);
        }

        private void SpawnTimer(ServerRoom serverRoom)
        {
            GameplayTimer timer = Instantiate(gameplayTimerPrefab, Vector3.zero, Quaternion.identity);
            timer.serverRoom = serverRoom;
            ServerManager.Spawn(timer.networkObject, LocalConnection, _additiveServerScene);
            serverRoom.gameplayTimer =  timer;
        }
        
        private void SpawnBot(ServerRoom serverRoom, Player player)
        {
            return;
        }
        
        private async UniTask SpawnPlayerAsync(ServerRoom serverRoom, NetworkConnection connection)
        {
            await _vehicleSpawner.SpawnPlayerAsync(
                serverRoom,
                connection,
                _additiveServerScene,
                SceneValidationTimeout,
                ServerManager,
                vehicleRoot =>
                {
                    vehicleRoot.health.OnServerDeath += _ => HandleRobotDeath(serverRoom);
                });
        }

        [Server]
        private void HandleRobotDeath(ServerRoom serverRoom)
        {
            EvaluateBattleEnd(serverRoom);
        }

        [Server]
        private void EvaluateBattleEnd(ServerRoom serverRoom)
        {
            BattleEndState endState = BattleRules.EvaluateEnd(serverRoom);
            if (!endState.ShouldFinish)
            {
                return;
            }

            FinishBattle(serverRoom, endState.WinnerTeam, endState.IsDraw);
        }

        [Server]
        public void HandleTimeExpired(ServerRoom serverRoom)
        {
            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

            FinishBattle(serverRoom, MatchTeam.None, true);
        }

        [Server]
        public void RecordHitStats(VehicleRoot attackerRoot, VehicleRoot targetRoot, float damage, bool killed)
        {
            BattleStatisticsService.RecordHit(attackerRoot, targetRoot, damage, killed);
        }

        [Server]
        private void FinishBattle(ServerRoom serverRoom, MatchTeam winnerTeam, bool isDraw)
        {
            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

            serverRoom.isGameFinished = true;

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot)
                {
                    continue;
                }

                EndGameResult result = BattleRules.GetEndGameResult(player, winnerTeam, isDraw);
                player.battleResult = BattleRules.ToApiResult(result);

                if (BattleRules.ShouldReceiveEndGame(player))
                {
                    TargetShowEndGameRpc(player.Connection, result);
                }
            }

            SubmitBattleResults(serverRoom).Forget();
        }

        private async UniTask SubmitBattleResults(ServerRoom serverRoom)
        {
            if (serverRoom == null)
            {
                return;
            }

            List<PlayerBattleResultDelivery> deliveries = await MatchResultService.EndMatchAndBuildDeliveries(serverRoom);
            for (int i = 0; i < deliveries.Count; i++)
            {
                PlayerBattleResultDelivery delivery = deliveries[i];
                if (delivery.Connection == null)
                {
                    PendingBattleResults.Enqueue(delivery.UserId, delivery.Result);
                    continue;
                }

                SendGameResult(delivery.Connection, delivery.Result);
            }

            serverRoom.matchRewardsSent = true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ConfirmGameResultReceivedServerRpc(string roomId, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetRoomById(roomId);
            if (serverRoom == null || !serverRoom.matchRewardsSent)
            {
                return;
            }

            int userId = ServerPlayerSessions.GetUserId(sender.ClientId);
            Player player = serverRoom.GetPlayerByUserId(userId);
            if (player == null)
            {
                return;
            }

            LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPendingGameResultsServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            int userId = ServerPlayerSessions.GetUserId(sender.ClientId);
            List<PlayerBattleResult> pendingResults = new List<PlayerBattleResult>();
            if (!PendingBattleResults.TryTakeAll(userId, pendingResults))
            {
                return;
            }

            for (int i = 0; i < pendingResults.Count; i++)
            {
                SendGameResult(sender, pendingResults[i]);
            }
        }

        [TargetRpc]
        private void TargetShowEndGameRpc(NetworkConnection target, EndGameResult result)
        {
            ShowEndGameDelayed(result).Forget();
        }

        private void SendGameResult(NetworkConnection target, PlayerBattleResult result)
        {
            TargetShowGameResultRpc(
                target,
                result.RoomId,
                result.Result,
                result.Kills,
                result.Damage,
                result.XpEarned,
                result.Bolts,
                result.FreeXp,
                result.MmrDelta
            );
        }

        [TargetRpc]
        private void TargetShowGameResultRpc(
            NetworkConnection target,
            string roomId,
            string result,
            int kills,
            int damage,
            int xpEarned,
            int bolts,
            int freeXp,
            int mmrDelta)
        {
            GameResultUI.Enqueue(roomId, result, kills, damage, xpEarned, bolts, freeXp, mmrDelta);
        }

        private async UniTask ShowEndGameDelayed(EndGameResult result)
        {
            await UniTask.Delay(EndGameDelayMilliseconds, cancellationToken: this.GetCancellationTokenOnDestroy());

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MenuManager.OpenMenu(MenuType.EndGame);
            EndGameUI.Show(result);
        }

        [ObserversRpc]
        private void ApplySceneOffsetClientRpc(int sceneHandle, int offset)
        {
            UEScene scene = GetSceneByHandleLocal(sceneHandle);
            
            if (!scene.IsValid())
            {
                return;
            }

            _sceneOffsetService.ApplyOffset(scene, offset);
        }

        private UEScene GetSceneByHandleLocal(int handle)
        {
            for (int i = 0; i < UESceneManager.sceneCount; i++)
            {
                Scene s = UESceneManager.GetSceneAt(i);
                if (s.handle == handle)
                {
                    return s;
                }
            }
            return default;
        }

    }
}
