using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Lobby;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.World.Spawns;
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
        public int sceneOffsetX;
        private const float SceneValidationTimeout = 10f;
        private const int EndGameDelayMilliseconds = 2000;
        private static readonly Dictionary<int, Queue<PendingGameResult>> PendingResultsByUserId = new Dictionary<int, Queue<PendingGameResult>>();

        private struct PendingGameResult
        {
            public string RoomId;
            public string Result;
            public int Kills;
            public int Damage;
            public int XpEarned;
            public int Bolts;
            public int FreeXp;
            public int MmrDelta;
        }

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
                
                    Player player = serverRoom.GetPlayers().Find(x => x.Connection == conn);
                    if (player == null)
                    {
                        continue;
                    }

                    if (serverRoom.isInGame && !serverRoom.matchRewardsSent)
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
            
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                go.transform.position += Vector3.right * usedOffset;
            }

            sceneOffsetX += 500;
            _additiveServerScene = scene;

            // повідомити клієнтів про зсув
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

            if (GameplayGUI.In != null && GameplayGUI.In.pauseMenu != null)
            {
                GameplayGUI.In.pauseMenu.OnDisconnectPressed += ReturnToMainMenu;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            UESceneManager.sceneLoaded -= HandleClientSceneLoaded;
            SceneManager.OnLoadEnd -= HandleClientLoadEnd;
            SceneManager.OnUnloadEnd -= SceneManagerOnUnloadEnd;

            if (GameplayGUI.In != null && GameplayGUI.In.pauseMenu != null)
            {
                GameplayGUI.In.pauseMenu.OnDisconnectPressed -= ReturnToMainMenu;
            }
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
                
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    go.transform.position += Vector3.right * offset;
                }
            }
        }

        private void SceneManagerOnUnloadEnd(SceneUnloadEndEventArgs obj) { }
        
        public void ReturnToMainMenu()
        {
            RobotView.GenerateIcons();

            if (MainMenu.In != null)
            {
                MainMenu.In.SetActive(true);
            }

            MenuManager.CloseMenu(MenuType.GameplayHUD);
         
            foreach (VehicleRoot root in FindObjectsByType<VehicleRoot>(FindObjectsSortMode.None))
            {
                if(root.OwnerId == ClientManager.Connection.ClientId)
                {
                    //Destroy from gameplay
                }
            }
            
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
                    if (serverRoom.isInGame && !serverRoom.matchRewardsSent)
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
            if (serverRoom == null || player == null || player.leftBattle)
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

        private bool IsValidScene(UEScene scene) => scene.IsValid() && scenes.Any(k => scene.name.Contains(k.ToString()));

        private List<Player> GetRealPlayers(ServerRoom serverRoom)
        {
            List<Player> realPlayers = new List<Player>();

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (!player.isBot)
                {
                    realPlayers.Add(player);
                }
            }

            return realPlayers;
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
            List<Player> realPlayers = GetRealPlayers(serverRoom);
            bool allLoaded = realPlayers.All(p => p.randomPlayerConnected);
            
            if (allLoaded) //виконується тільки тоді коли всі гравці загрузилися
            {
                foreach (Player player in serverRoom.GetPlayers())
                {
                    if (player.isBot)
                    {
                        SpawnBot(serverRoom, player);
                    }
                    else
                    {
                        SpawnPlayer(serverRoom, player.Connection);
                    }
                }
                
                LobbyRooms.UpdateRoomStatusInGame(serverRoom.roomId);
                SpawnTimer(serverRoom);
                //ScoreBoard timer = Instantiate(scoreBoard, Vector3.zero, Quaternion.identity);
                //timer.serverRoom = serverRoom;
                //timer.GetComponent<RoomConditionRebuilder>().SetupRoomID(serverRoom.roomId);
                //ServerManager.Spawn(timer.gameObject, scene: _additiveServerScene);
            }
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
        
        private async void SpawnPlayer(ServerRoom serverRoom, NetworkConnection connection)
        {
            float elapsedTime = 0f;
            
            while (!_additiveServerScene.IsValid() && elapsedTime < SceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!_additiveServerScene.IsValid())
            {
                return;
            }

            Player player = serverRoom.GetPlayerByConnection(connection);
            if (player == null)
            {
                return;
            }

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(_additiveServerScene, player.team);
            if (spawnPoint == null)
            {
                return;
            }
            
            PlayerProfile profile = ServerPlayerSessions.GetProfile(connection.ClientId);
            if (profile == null)
            {
                return;
            }

            VehicleRoot vehicle = ResourceManager.GetPrefab(profile.activeVehicleCode);
            if (vehicle == null)
            {
                return;
            }

            VehicleRoot vehicleRoot = Instantiate(vehicle, spawnPoint.transform.position, spawnPoint.transform.rotation);
            ServerManager.Spawn(vehicleRoot.networkObject, connection, _additiveServerScene);

            player.playerRoot = vehicleRoot;
            player.playerRoot.health.OnServerDeath += _ => HandleRobotDeath(serverRoom);
            player.playerRoot.characterInit.ServerInit(serverRoom.maxPlayers, PlayerType.Player, player.loginName, player.team, _additiveServerScene);
        }

        [Server]
        private void HandleRobotDeath(ServerRoom serverRoom)
        {
            EvaluateBattleEnd(serverRoom);
        }

        [Server]
        private void EvaluateBattleEnd(ServerRoom serverRoom)
        {
            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

            int redAlive = 0;
            int blueAlive = 0;
            int unassignedAlive = 0;
            int aliveRobots = 0;

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.playerRoot == null || player.playerRoot.health == null)
                {
                    continue;
                }

                if (!player.leftBattle && !player.playerRoot.health.IsDead)
                {
                    aliveRobots++;
                    if (player.team == MatchTeam.Red)
                    {
                        redAlive++;
                    }
                    else if (player.team == MatchTeam.Blue)
                    {
                        blueAlive++;
                    }
                    else
                    {
                        unassignedAlive++;
                    }
                }
            }

            MatchTeam winnerTeam = MatchTeam.None;
            bool isDraw = aliveRobots == 0;

            if (!isDraw)
            {
                if (unassignedAlive == 0 && redAlive > 0 && blueAlive == 0)
                {
                    winnerTeam = MatchTeam.Red;
                }
                else if (unassignedAlive == 0 && blueAlive > 0 && redAlive == 0)
                {
                    winnerTeam = MatchTeam.Blue;
                }
            }

            if (!isDraw && winnerTeam == MatchTeam.None)
            {
                return;
            }

            FinishBattle(serverRoom, winnerTeam, isDraw);
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
            if (attackerRoot == null || damage <= 0f)
            {
                return;
            }

            ServerRoom serverRoom = GetRoomByVehicle(attackerRoot);
            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

            Player attacker = serverRoom.GetPlayers().Find(p => p != null && p.playerRoot == attackerRoot);
            if (attacker == null)
            {
                return;
            }

            attacker.damage = Mathf.Clamp(attacker.damage + Mathf.RoundToInt(damage), 0, 20000);

            if (!killed || targetRoot == null || targetRoot == attackerRoot)
            {
                return;
            }

            Player target = serverRoom.GetPlayers().Find(p => p != null && p.playerRoot == targetRoot);
            if (target != null)
            {
                attacker.kills = Mathf.Clamp(attacker.kills + 1, 0, 20);
            }
        }

        private static ServerRoom GetRoomByVehicle(VehicleRoot root)
        {
            if (root == null)
            {
                return null;
            }

            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                foreach (Player player in room.GetPlayers())
                {
                    if (player != null && player.playerRoot == root)
                    {
                        return room;
                    }
                }
            }

            return null;
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

                EndGameResult result = GetEndGameResult(player, winnerTeam, isDraw);
                player.battleResult = ToApiResult(result);

                if (ShouldReceiveEndGame(player))
                {
                    TargetShowEndGameRpc(player.Connection, result);
                }
            }

            SubmitBattleResults(serverRoom).Forget();
        }

        private static bool ShouldReceiveEndGame(Player player)
        {
            return player != null
                   && !player.isBot
                   && player.Connection != null
                   && !player.leftBattle
                   && player.playerRoot != null;
        }

        private static EndGameResult GetEndGameResult(Player player, MatchTeam winnerTeam, bool isDraw)
        {
            if (player != null && player.leftBattle)
            {
                return EndGameResult.Lose;
            }

            if (isDraw)
            {
                return EndGameResult.Draw;
            }

            return player.team == winnerTeam ? EndGameResult.Win : EndGameResult.Lose;
        }

        private static string ToApiResult(EndGameResult result)
        {
            if (result == EndGameResult.Win)
            {
                return "win";
            }

            if (result == EndGameResult.Draw)
            {
                return "draw";
            }

            return "lose";
        }

        private async UniTask SubmitBattleResults(ServerRoom serverRoom)
        {
            if (serverRoom == null || serverRoom.matchEndSubmitted)
            {
                return;
            }

            serverRoom.matchEndSubmitted = true;

            ParticipantInput[] participants = BuildParticipantInputs(serverRoom);
            string token = serverRoom.GetApiToken();
            MatchParticipantView[] rewardItems = Array.Empty<MatchParticipantView>();

            if (serverRoom.matchId <= 0 && !string.IsNullOrEmpty(token))
            {
                (bool started, _, int matchId) = await MatchesManager.StartMatch(serverRoom.selectedLocation, token);
                if (started)
                {
                    serverRoom.matchId = matchId;
                }
            }

            if (serverRoom.matchId > 0 && !string.IsNullOrEmpty(token) && participants.Length > 0)
            {
                (bool isSuccess, _, EndMatchResponse response) = await MatchesManager.EndMatch(serverRoom.matchId, participants, token);

                if (isSuccess && response != null && response.participants != null)
                {
                    rewardItems = response.participants;
                }
                else if (isSuccess)
                {
                    (bool loaded, _, MatchParticipantView[] items) = await MatchesManager.GetParticipants(serverRoom.matchId, token);
                    if (loaded && items != null)
                    {
                        rewardItems = items;
                    }
                }
            }

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot)
                {
                    continue;
                }

                NetworkConnection resultConnection = player.Connection;
                if (resultConnection == null)
                {
                    resultConnection = ServerPlayerSessions.GetConnectionByUserId(player.userId);
                }

                MatchParticipantView reward = FindReward(player.userId, rewardItems);
                int xpEarned = reward != null ? reward.xpEarned : 0;
                int bolts = reward != null ? reward.bolts : 0;
                int freeXp = reward != null ? reward.freeXp : 0;
                int mmrDelta = reward != null ? reward.mmrDelta : 0;

                if (resultConnection == null)
                {
                    StorePendingResult(
                        player.userId,
                        serverRoom.roomId,
                        player.battleResult,
                        player.kills,
                        player.damage,
                        xpEarned,
                        bolts,
                        freeXp,
                        mmrDelta
                    );
                    continue;
                }

                TargetShowGameResultRpc(
                    resultConnection,
                    serverRoom.roomId,
                    player.battleResult,
                    player.kills,
                    player.damage,
                    xpEarned,
                    bolts,
                    freeXp,
                    mmrDelta
                );
            }

            serverRoom.matchRewardsSent = true;
        }

        private static void StorePendingResult(
            int userId,
            string roomId,
            string result,
            int kills,
            int damage,
            int xpEarned,
            int bolts,
            int freeXp,
            int mmrDelta)
        {
            if (userId <= 0)
            {
                return;
            }

            if (!PendingResultsByUserId.TryGetValue(userId, out Queue<PendingGameResult> queue))
            {
                queue = new Queue<PendingGameResult>();
                PendingResultsByUserId[userId] = queue;
            }

            queue.Enqueue(new PendingGameResult
            {
                RoomId = roomId,
                Result = result,
                Kills = kills,
                Damage = damage,
                XpEarned = xpEarned,
                Bolts = bolts,
                FreeXp = freeXp,
                MmrDelta = mmrDelta
            });
        }

        private static ParticipantInput[] BuildParticipantInputs(ServerRoom serverRoom)
        {
            List<ParticipantInput> participants = new List<ParticipantInput>();

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot || player.userId <= 0)
                {
                    continue;
                }

                ParticipantInput participant = new ParticipantInput
                {
                    userId = player.userId,
                    vehicleId = player.activeVehicleId,
                    team = (int)player.team,
                    result = string.IsNullOrEmpty(player.battleResult) ? "lose" : player.battleResult,
                    kills = Mathf.Clamp(player.kills, 0, 20),
                    damage = Mathf.Clamp(player.damage, 0, 20000)
                };

                participants.Add(participant);
            }

            return participants.ToArray();
        }

        private static MatchParticipantView FindReward(int userId, MatchParticipantView[] rewards)
        {
            if (rewards == null)
            {
                return null;
            }

            for (int i = 0; i < rewards.Length; i++)
            {
                MatchParticipantView reward = rewards[i];
                if (reward != null && reward.userId == userId)
                {
                    return reward;
                }
            }

            return null;
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
            if (userId <= 0 || !PendingResultsByUserId.TryGetValue(userId, out Queue<PendingGameResult> queue))
            {
                return;
            }

            while (queue.Count > 0)
            {
                PendingGameResult result = queue.Dequeue();
                TargetShowGameResultRpc(
                    sender,
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

            PendingResultsByUserId.Remove(userId);
        }

        [TargetRpc]
        private void TargetShowEndGameRpc(NetworkConnection target, EndGameResult result)
        {
            ShowEndGameDelayed(result).Forget();
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

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                go.transform.position += Vector3.right * offset;
            }
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

        public static List<T> FindObjectsInScene<T>(UEScene scene, bool includeInactive = true) where T : Component
        {
            List<T> results = new List<T>();
            
            if (!scene.IsValid())
            {
                return results;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<T>(includeInactive));
            }
            
            return results;
        }

        public static List<Component> FindObjectsInScene(GameObject root, Type componentType, bool includeInactive = true)
        {
            if (root == null)
            {
                return new List<Component>();
            }
            
            return root.GetComponentsInChildren(componentType, includeInactive).Cast<Component>().ToList();
        }

        public static List<T> FindObjectsInScene<T>(GameObject root, bool includeInactive = true) where T : Component
        {
            if (root == null)
            {
                return new List<T>();
            }
            
            return root.GetComponentsInChildren<T>(includeInactive).ToList();
        }
    }
}
