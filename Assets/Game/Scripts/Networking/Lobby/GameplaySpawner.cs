using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.HUD;
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
        Test = 0,
    }
    
    public class GameplaySpawner : NetworkBehaviour
    {
        public static GameplaySpawner In;
        public GameMaps[] scenes;
        public GameplayTimer gameplayTimerPrefab;
        
        [SerializeField] private LobbyManager lobbyManager;

        private UEScene _additiveServerScene;
        public int sceneOffsetX;
        private const float SceneValidationTimeout = 10f;

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
                ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);
                if (serverRoom == null)
                {
                    return;
                }
                
                Player player = serverRoom.GetPlayers().Find(x => x.Connection == conn);
                
                if (player == null)
                {
                    return;
                }
                
                LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
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
            GameplayGUI.In.pauseMenu.OnDisconnectPressed += ReturnToMainMenu;
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
            MainMenu.In.SetActive(true);
            MenuManager.CloseMenu(MenuType.GameplayHUD);
         
            foreach (VehicleRoot root in FindObjectsByType<VehicleRoot>(FindObjectsSortMode.None))
            {
                if(root.OwnerId == ClientManager.Connection.ClientId)
                {
                    //Destroy from gameplay
                }
            }
            
            RequestPlayerDisconnectServerRpc(ClientManager.Connection.ClientId);
            lobbyManager.RequestGetRoomList();
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
                    SceneManager.UnloadConnectionScenes(conn, new SceneUnloadData(serverRoom.loadedSceneName));
                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
                    Despawn(player.playerRoot.networkObject);
                }
            }
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
            ServerManager.Spawn(timer.networkObject, LocalConnection, _additiveServerScene);
            serverRoom.gameplayTimer =  timer;
        }
        
        private void SpawnBot(ServerRoom serverRoom, Player player)
        {
            // Bot spawning is not implemented in this demo yet.
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

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(_additiveServerScene);
            if (spawnPoint == null)
            {
                return;
            }

            PlayerProfile profile = ProfileServer.GetProfileByClientId(connection.ClientId);
            if (profile == null)
            {
                return;
            }

            VehicleRoot vehicle = ResourceManager.GetPrefab(profile.activeVehicleCode);
            if (vehicle == null)
            {
                return;
            }

            VehicleRoot vehicleRoot = Instantiate(vehicle, spawnPoint.transform.position, Quaternion.identity);
            ServerManager.Spawn(vehicleRoot.networkObject, connection, _additiveServerScene);
            
            Player player = serverRoom.GetPlayerByConnection(connection);
            if (player == null)
            {
                return;
            }

            player.playerRoot = vehicleRoot;
            player.playerRoot.characterInit.ServerInit(serverRoom.maxPlayers, PlayerType.Player, player.loginName, _additiveServerScene);
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
