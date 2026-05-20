using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.World.Spawns;
using UnityEngine;
using UnityEngine.SceneManagement;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;
using SceneLoadData = FishNet.Managing.Scened.SceneLoadData;

namespace Game.Scripts.Testing
{
    public enum VehicleAutoSpawnNetworkRole
    {
        Server = 0,
        Client = 1
    }

    public sealed class VehicleAutoSpawnNetworkTestController : MonoBehaviour
    {
        private const string TestRoomId = "VehicleAutoSpawnTest";

        [SerializeField] private VehicleAutoSpawnNetworkRole role = VehicleAutoSpawnNetworkRole.Client;
        [SerializeField] private RobotRegistry registry;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Vector3 spawnRotationEuler;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Camera testCamera;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private ushort port = 7780;
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool autoSpawnOnClientConnected = true;
        [SerializeField] private bool loadGameplayScene = true;
        [SerializeField] private string gameplaySceneName = "MapTest";
        [SerializeField] private float gameplaySceneLoadTimeout = 10f;
        [SerializeField] private string loginName = "VehicleAutoSpawnTest";
        [SerializeField] private MatchTeam team = MatchTeam.TeamA;
        [SerializeField] private bool showStatusOverlay = true;

        private readonly Dictionary<int, VehicleRoot> _spawnedByClientId = new Dictionary<int, VehicleRoot>();

        private ServerRoom _testRoom;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _subscribed;
        private bool _serverStartedByThis;
        private bool _clientStartedByThis;
        private string _status = "Idle.";

        private void Awake()
        {
            ResolveNetworkManager();
            ResolveCameraSync();
            SubscribeNetworkEvents();
        }

        private void Start()
        {
            if (!autoStart)
            {
                return;
            }

            if (role == VehicleAutoSpawnNetworkRole.Server)
            {
                StartServer();
            }
            else
            {
                StartClient();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeNetworkEvents();

            if (networkManager == null)
            {
                return;
            }

            if (_clientStartedByThis && networkManager.ClientManager != null && networkManager.IsClientStarted)
            {
                networkManager.ClientManager.StopConnection();
            }

            if (_serverStartedByThis && networkManager.ServerManager != null && networkManager.IsServerStarted)
            {
                networkManager.ServerManager.StopConnection(true);
            }
        }

        private void StartServer()
        {
            if (networkManager == null || networkManager.ServerManager == null)
            {
                _status = "Server start failed: NetworkManager is missing.";
                Debug.LogWarning(_status);
                return;
            }

            if (networkManager.IsServerStarted)
            {
                _status = "Server is already started.";
                EnsureTestRoom(gameObject.scene);
                return;
            }

            bool started = networkManager.ServerManager.StartConnection(port);
            if (!started)
            {
                _status = "Server start failed on port " + port + ".";
                Debug.LogError(_status);
                return;
            }

            _serverStartedByThis = true;
            EnsureTestRoom(gameObject.scene);
            _status = "Server started on port " + port + ".";
            Debug.Log(_status);
        }

        private void StartClient()
        {
            if (networkManager == null || networkManager.ClientManager == null)
            {
                _status = "Client start failed: NetworkManager is missing.";
                Debug.LogWarning(_status);
                return;
            }

            if (networkManager.IsClientStarted)
            {
                _status = "Client is already connected.";
                return;
            }

            bool started = networkManager.ClientManager.StartConnection(serverAddress, port);
            if (!started)
            {
                _status = "Client connect failed to " + serverAddress + ":" + port + ".";
                Debug.LogError(_status);
                return;
            }

            _clientStartedByThis = true;
            _status = "Client connecting to " + serverAddress + ":" + port + ".";
            Debug.Log(_status);
        }

        private void ResolveNetworkManager()
        {
            if (networkManager == null)
            {
                networkManager = FindAnyObjectByType<NetworkManager>();
            }

            if (networkManager == null)
            {
                _status = "Scene setup error: NetworkManager is missing.";
                Debug.LogWarning(_status);
                return;
            }

            _serverState = networkManager.IsServerStarted ? LocalConnectionState.Started : LocalConnectionState.Stopped;
            _clientState = networkManager.IsClientStarted ? LocalConnectionState.Started : LocalConnectionState.Stopped;
        }

        private void ResolveCameraSync()
        {
            if (role != VehicleAutoSpawnNetworkRole.Client)
            {
                return;
            }

            if (testCamera == null)
            {
                testCamera = Camera.main;
            }

            if (CameraSync.In != null && testCamera != null)
            {
                CameraSync.In.gameplayCamera = testCamera;
            }
        }

        private void SubscribeNetworkEvents()
        {
            if (_subscribed || networkManager == null)
            {
                return;
            }

            if (networkManager.ServerManager != null)
            {
                networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
                networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            }

            if (networkManager.ClientManager != null)
            {
                networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            }

            _subscribed = true;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (!_subscribed || networkManager == null)
            {
                return;
            }

            if (networkManager.ServerManager != null)
            {
                networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }

            if (networkManager.ClientManager != null)
            {
                networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            }

            _subscribed = false;
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverState = args.ConnectionState;

            if (role == VehicleAutoSpawnNetworkRole.Server && args.ConnectionState == LocalConnectionState.Started)
            {
                EnsureTestRoom(gameObject.scene);
                _status = "Server started on port " + port + ".";
            }
            else if (role == VehicleAutoSpawnNetworkRole.Server && args.ConnectionState == LocalConnectionState.Stopped)
            {
                _status = "Server stopped.";
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientState = args.ConnectionState;

            if (role != VehicleAutoSpawnNetworkRole.Client)
            {
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _status = "Client connected to " + serverAddress + ":" + port + ".";
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _status = "Client disconnected.";
            }
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (role != VehicleAutoSpawnNetworkRole.Server)
            {
                return;
            }

            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                EnsureTestRoom(gameObject.scene);

                if (autoSpawnOnClientConnected)
                {
                    SpawnForConnectionAsync(connection).Forget();
                }
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                RemoveConnection(connection);
            }
        }

        private async UniTaskVoid SpawnForConnectionAsync(NetworkConnection connection)
        {
            await UniTask.DelayFrame(1);

            if (!IsConnectionReady(connection))
            {
                _status = "Spawn skipped: client connection is not ready.";
                return;
            }

            if (_spawnedByClientId.ContainsKey(connection.ClientId))
            {
                return;
            }

            if (!await WaitForConnectionReadyForSceneLoadAsync(connection))
            {
                _status = "Spawn failed: client connection is not ready for scene load.";
                Debug.LogError(_status);
                return;
            }

            Scene spawnScene = await EnsureGameplaySceneAsync(connection);
            if (!spawnScene.IsValid() || !spawnScene.isLoaded)
            {
                _status = "Spawn failed: gameplay scene " + gameplaySceneName + " is not loaded.";
                Debug.LogError(_status);
                return;
            }

            VehicleRoot prefab = GetFirstVehiclePrefab(out string vehicleCode);
            if (prefab == null)
            {
                _status = "Spawn failed: registry has no valid vehicle prefab.";
                Debug.LogError(_status);
                return;
            }

            SpawnPoint mapSpawnPoint = SpawnPoint.GetFreePoint(spawnScene, team);
            Vector3 position = mapSpawnPoint != null
                ? mapSpawnPoint.transform.position
                : spawnPoint != null
                    ? spawnPoint.position
                    : spawnPosition;
            Quaternion rotation = mapSpawnPoint != null
                ? mapSpawnPoint.transform.rotation
                : spawnPoint != null
                    ? spawnPoint.rotation
                    : Quaternion.Euler(spawnRotationEuler);
            VehicleRoot vehicleRoot = Instantiate(prefab, position, rotation);
            vehicleRoot.gameObject.SetActive(true);

            NetworkObject networkObject = vehicleRoot.networkObject != null
                ? vehicleRoot.networkObject
                : vehicleRoot.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Destroy(vehicleRoot.gameObject);
                _status = "Spawn failed: vehicle has no NetworkObject.";
                Debug.LogError(_status);
                return;
            }

            EnsurePlayer(connection, vehicleCode, vehicleRoot, spawnScene);
            networkManager.ServerManager.Spawn(networkObject, connection, spawnScene);

            if (vehicleRoot.characterInit != null)
            {
                vehicleRoot.characterInit.ServerInit(1, PlayerType.Player, loginName, team, spawnScene);
            }

            _spawnedByClientId[connection.ClientId] = vehicleRoot;
            _status = "Spawned " + vehicleRoot.name + " for client " + connection.ClientId + ".";
            Debug.Log(_status);
        }

        private VehicleRoot GetFirstVehiclePrefab(out string vehicleCode)
        {
            vehicleCode = string.Empty;
            if (registry == null)
            {
                return null;
            }

            vehicleCode = registry.GetFirstCode();
            if (string.IsNullOrEmpty(vehicleCode))
            {
                return null;
            }

            return registry.GetPrefab(vehicleCode);
        }

        private async UniTask<bool> WaitForConnectionReadyForSceneLoadAsync(NetworkConnection connection)
        {
            float timeout = Mathf.Max(0.1f, gameplaySceneLoadTimeout);
            float endTime = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (IsConnectionReadyForSceneLoad(connection))
                {
                    return true;
                }

                await UniTask.Yield();
            }

            return false;
        }

        private async UniTask<Scene> EnsureGameplaySceneAsync(NetworkConnection connection)
        {
            if (!loadGameplayScene || string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return gameObject.scene;
            }

            if (TryGetLoadedGameplayScene(connection, out Scene loadedScene))
            {
                EnsureTestRoom(loadedScene);
                return loadedScene;
            }

            if (networkManager == null || networkManager.SceneManager == null || !IsConnectionReady(connection))
            {
                return default;
            }

            _status = "Loading gameplay scene " + gameplaySceneName + "...";
            EnsureTestRoom(gameObject.scene);

            SceneLoadData sceneLoadData = new SceneLoadData(gameplaySceneName)
            {
                Options =
                {
                    AllowStacking = true,
                    AutomaticallyUnload = true,
                },
                Params =
                {
                    ServerParams = new object[]
                    {
                        _testRoom
                    },
                    ClientParams = System.BitConverter.GetBytes(0)
                }
            };

            NetworkConnection[] connections = { connection };
            networkManager.SceneManager.LoadConnectionScenes(connections, sceneLoadData);

            Scene scene = await WaitForGameplaySceneLoadedAsync(connection);
            if (scene.IsValid())
            {
                EnsureTestRoom(scene);
            }

            return scene;
        }

        private async UniTask<Scene> WaitForGameplaySceneLoadedAsync(NetworkConnection connection)
        {
            float timeout = Mathf.Max(0.1f, gameplaySceneLoadTimeout);
            float endTime = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (TryGetLoadedGameplayScene(connection, out Scene loadedScene))
                {
                    return loadedScene;
                }

                await UniTask.Yield();
            }

            return default;
        }

        private static bool IsConnectionInScene(NetworkConnection connection, Scene scene)
        {
            if (!IsConnectionReady(connection) || !scene.IsValid())
            {
                return false;
            }

            return connection.Scenes != null && connection.Scenes.Contains(scene);
        }

        private static bool IsConnectionReadyForSceneLoad(NetworkConnection connection)
        {
            return IsConnectionReady(connection)
                   && connection.IsAuthenticated
                   && connection.LoadedStartScenes(true);
        }

        private bool TryGetLoadedGameplayScene(NetworkConnection connection, out Scene loadedScene)
        {
            loadedScene = default;
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid()
                    && scene.isLoaded
                    && scene.name == gameplaySceneName
                    && HasSpawnPoint(scene)
                    && IsConnectionInScene(connection, scene))
                {
                    loadedScene = scene;
                    return true;
                }
            }

            return false;
        }

        private static bool HasSpawnPoint(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null && root.GetComponentInChildren<SpawnPoint>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureTestRoom(Scene roomScene)
        {
            _testRoom = LobbyRooms.GetRoomById(TestRoomId);
            if (_testRoom == null)
            {
                GameObject roomObject = new GameObject("VehicleAutoSpawnTest_ServerRoom");
                SceneManager.MoveGameObjectToScene(roomObject, gameObject.scene);
                _testRoom = roomObject.AddComponent<ServerRoom>();
                _testRoom.roomId = TestRoomId;
                LobbyRooms.AddRoom(_testRoom);
            }

            _testRoom.roomId = TestRoomId;
            _testRoom.roomName = TestRoomId;
            _testRoom.maxPlayers = 1;
            _testRoom.selectedLocation = !string.IsNullOrWhiteSpace(gameplaySceneName) ? gameplaySceneName : roomScene.name;
            _testRoom.isInGame = true;
            _testRoom.loadedSceneName = roomScene.IsValid() ? roomScene.name : string.Empty;
            _testRoom.handle = roomScene.IsValid() ? roomScene.handle : 0;
            _testRoom.sceneSlotIndex = ServerRoom.NoSceneSlot;
            _testRoom.sceneOffsetX = 0;
        }

        private void EnsurePlayer(NetworkConnection connection, string vehicleCode, VehicleRoot vehicleRoot, Scene spawnScene)
        {
            EnsureTestRoom(spawnScene);

            if (_testRoom == null || !IsConnectionReady(connection))
            {
                return;
            }

            LobbyPlayer player = _testRoom.GetPlayerByConnection(connection);
            if (player == null)
            {
                player = new LobbyPlayer
                {
                    loginName = loginName,
                    Connection = connection,
                    userId = 0,
                    mmr = 1000,
                    activeVehicleId = 0,
                    activeVehicleCode = vehicleCode,
                    team = team,
                    randomPlayerConnected = true
                };

                _testRoom.AddPlayer(player);
            }

            player.playerRoot = vehicleRoot;
            player.activeVehicleCode = vehicleCode;
            player.team = team;
        }

        private void RemoveConnection(NetworkConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            if (_spawnedByClientId.TryGetValue(connection.ClientId, out VehicleRoot vehicleRoot))
            {
                _spawnedByClientId.Remove(connection.ClientId);

                if (vehicleRoot != null && vehicleRoot.networkObject != null && vehicleRoot.networkObject.IsSpawned)
                {
                    networkManager.ServerManager.Despawn(vehicleRoot.networkObject);
                }
                else if (vehicleRoot != null)
                {
                    Destroy(vehicleRoot.gameObject);
                }
            }

            if (_testRoom == null)
            {
                return;
            }

            for (int i = _testRoom.players.Count - 1; i >= 0; i--)
            {
                LobbyPlayer player = _testRoom.players[i];
                if (player != null && player.Connection == connection)
                {
                    _testRoom.players.RemoveAt(i);
                }
            }
        }

        private static bool IsConnectionReady(NetworkConnection connection)
        {
            return connection != null && connection.IsActive;
        }

        private void OnGUI()
        {
            if (!showStatusOverlay)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 112f), GUI.skin.box);
            GUILayout.Label("Vehicle Auto Spawn Test");
            GUILayout.Label("Role: " + role);
            GUILayout.Label("Server: " + _serverState + "  Client: " + _clientState);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }
    }
}
