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
        public NetworkGameplayTimer gameplayTimerPrefab;

        [SerializeField] private int sceneSlotSpacingX = 500;

        private readonly MatchSceneOffsetService _sceneOffsetService = new MatchSceneOffsetService();
        private readonly MatchVehicleSpawner _vehicleSpawner = new MatchVehicleSpawner();
        private readonly Dictionary<int, int> _sceneSlotsByHandle = new Dictionary<int, int>();
        private readonly Dictionary<int, ServerRoom> _roomsBySceneHandle = new Dictionary<int, ServerRoom>();
        private MatchSceneSlotAllocator _sceneSlotAllocator;
        private const float SceneValidationTimeout = 10f;
        private const int EndGameDelayMilliseconds = 2000;
        private const int DefaultSceneSlotSpacingX = 500;

        private void Awake()
        {
            In = this;
            _sceneSlotAllocator = new MatchSceneSlotAllocator(GetSceneSlotSpacing());
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnLoadEnd += HandleServerLoadEnd;
            SceneManager.OnUnloadEnd += SceneManagerOnUnloadEnd;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            SceneManager.OnLoadEnd -= HandleServerLoadEnd;
            SceneManager.OnUnloadEnd -= SceneManagerOnUnloadEnd;
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            _sceneSlotsByHandle.Clear();
            _roomsBySceneHandle.Clear();
            _sceneSlotAllocator.Clear();
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

        private void HandleServerLoadEnd(SceneLoadEndEventArgs args)
        {
            ServerRoom serverRoom = GetLoadedServerRoom(args);
            if (serverRoom == null)
            {
                return;
            }

            foreach (Scene scene in args.LoadedScenes)
            {
                if (IsRoomLoadedScene(serverRoom, scene))
                {
                    RegisterRoomScene(serverRoom, scene);
                    return;
                }
            }
        }

        public int ReserveSceneSlot(ServerRoom serverRoom)
        {
            if (serverRoom == null)
            {
                return 0;
            }

            if (_sceneSlotAllocator == null)
            {
                _sceneSlotAllocator = new MatchSceneSlotAllocator(GetSceneSlotSpacing());
            }

            if (serverRoom.HasSceneSlot)
            {
                return serverRoom.sceneOffsetX;
            }

            int slot = _sceneSlotAllocator.ReserveSlot();
            int offset = _sceneSlotAllocator.GetOffset(slot);
            serverRoom.AssignSceneSlot(slot, offset);
            return offset;
        }

        public void ReleaseRoomSceneSlot(ServerRoom serverRoom)
        {
            if (serverRoom == null || !serverRoom.HasSceneSlot)
            {
                return;
            }

            if (serverRoom.HasLoadedScene && serverRoom.GetLoadedScene().IsValid())
            {
                return;
            }

            if (serverRoom.HasLoadedScene)
            {
                _sceneSlotsByHandle.Remove(serverRoom.handle);
                _roomsBySceneHandle.Remove(serverRoom.handle);
                serverRoom.ClearLoadedScene();
            }

            ReleaseSceneSlot(serverRoom.sceneSlotIndex);
            serverRoom.ClearSceneSlot();
        }

        private ServerRoom GetLoadedServerRoom(SceneLoadEndEventArgs args)
        {
            object[] serverParams = args.QueueData.SceneLoadData.Params.ServerParams;
            if (serverParams == null)
            {
                return null;
            }

            for (int i = 0; i < serverParams.Length; i++)
            {
                if (serverParams[i] is ServerRoom roomInfo)
                {
                    return LobbyRooms.GetRoomById(roomInfo.roomId);
                }
            }

            return null;
        }

        private bool IsRoomLoadedScene(ServerRoom serverRoom, Scene scene)
        {
            if (serverRoom == null || !IsValidScene(scene))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(serverRoom.loadedSceneName) && scene.name == serverRoom.loadedSceneName)
            {
                return true;
            }

            return scene.name == serverRoom.selectedLocation;
        }

        private void RegisterRoomScene(ServerRoom serverRoom, Scene scene)
        {
            if (serverRoom == null || !scene.IsValid())
            {
                return;
            }

            int offset = ReserveSceneSlot(serverRoom);
            serverRoom.AssignLoadedScene(scene);

            if (!_sceneSlotsByHandle.ContainsKey(scene.handle))
            {
                _sceneSlotsByHandle[scene.handle] = serverRoom.sceneSlotIndex;
                _roomsBySceneHandle[scene.handle] = serverRoom;
                _sceneOffsetService.ApplyOffset(scene, offset);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UESceneManager.sceneLoaded += HandleClientSceneLoaded;
            SceneManager.OnLoadEnd += HandleClientLoadEnd;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            UESceneManager.sceneLoaded -= HandleClientSceneLoaded;
            SceneManager.OnLoadEnd -= HandleClientLoadEnd;
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
            if (IsServerInitialized)
            {
                return;
            }

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
            if (!IsServerInitialized || obj.UnloadedScenesV2 == null)
            {
                return;
            }

            for (int i = 0; i < obj.UnloadedScenesV2.Count; i++)
            {
                ReleaseSceneSlotForHandle(obj.UnloadedScenesV2[i].Handle);
            }
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
                        UnloadRoomSceneForConnection(conn, serverRoom);
                        return;
                    }

                    UnloadRoomSceneForConnection(conn, serverRoom);

                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);

                    if (player.playerRoot != null && player.playerRoot.networkObject != null)
                    {
                        Despawn(player.playerRoot.networkObject);
                    }
                }
            }
        }

        private void UnloadRoomSceneForConnection(NetworkConnection connection, ServerRoom serverRoom)
        {
            if (connection == null || serverRoom == null)
            {
                return;
            }

            SceneUnloadData sceneUnloadData = CreateRoomSceneUnloadData(serverRoom);
            if (sceneUnloadData == null)
            {
                return;
            }

            SceneManager.UnloadConnectionScenes(connection, sceneUnloadData);
        }

        private SceneUnloadData CreateRoomSceneUnloadData(ServerRoom serverRoom)
        {
            if (serverRoom == null)
            {
                return null;
            }

            UEScene scene = serverRoom.GetLoadedScene();
            if (scene.IsValid())
            {
                return new SceneUnloadData(scene);
            }

            if (!string.IsNullOrEmpty(serverRoom.loadedSceneName))
            {
                return new SceneUnloadData(serverRoom.loadedSceneName);
            }

            return null;
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
                    await SpawnBotAsync(serverRoom, player);
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
            UEScene roomScene = serverRoom.GetLoadedScene();
            if (!roomScene.IsValid())
            {
                return;
            }

            NetworkGameplayTimer timer = Instantiate(gameplayTimerPrefab, Vector3.zero, Quaternion.identity);
            timer.serverRoom = serverRoom;
            ServerManager.Spawn(timer.networkObject, LocalConnection, roomScene);
            serverRoom.gameplayTimer =  timer;
        }
        
        private async UniTask SpawnBotAsync(ServerRoom serverRoom, Player player)
        {
            UEScene roomScene = serverRoom.GetLoadedScene();
            if (!roomScene.IsValid())
            {
                return;
            }

            await _vehicleSpawner.SpawnBotAsync(
                serverRoom,
                player,
                roomScene,
                SceneValidationTimeout,
                ServerManager,
                vehicleRoot =>
                {
                    vehicleRoot.health.OnServerDeath += _ => HandleRobotDeath(serverRoom);
                });
        }
        
        private async UniTask SpawnPlayerAsync(ServerRoom serverRoom, NetworkConnection connection)
        {
            UEScene roomScene = serverRoom.GetLoadedScene();
            if (!roomScene.IsValid())
            {
                return;
            }

            await _vehicleSpawner.SpawnPlayerAsync(
                serverRoom,
                connection,
                roomScene,
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

        private void ReleaseSceneSlotForHandle(int sceneHandle)
        {
            if (!_sceneSlotsByHandle.TryGetValue(sceneHandle, out int slot))
            {
                return;
            }

            _sceneSlotsByHandle.Remove(sceneHandle);

            if (_roomsBySceneHandle.TryGetValue(sceneHandle, out ServerRoom serverRoom))
            {
                _roomsBySceneHandle.Remove(sceneHandle);
                if (serverRoom != null)
                {
                    serverRoom.ClearLoadedScene();
                    serverRoom.ClearSceneSlot();
                }
            }

            ReleaseSceneSlot(slot);
        }

        private void ReleaseSceneSlot(int slot)
        {
            if (_sceneSlotAllocator == null)
            {
                return;
            }

            _sceneSlotAllocator.ReleaseSlot(slot);
        }

        private int GetSceneSlotSpacing()
        {
            if (sceneSlotSpacingX > 0)
            {
                return sceneSlotSpacingX;
            }

            return DefaultSceneSlotSpacingX;
        }

    }
}
