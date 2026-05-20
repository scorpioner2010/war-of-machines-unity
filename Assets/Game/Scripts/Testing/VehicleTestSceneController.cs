using System.Net;
using System.Net.Sockets;
using System.Text;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Resources;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.Server;
using Game.Scripts.UI.HUD;
using Game.Scripts.World.Spawns;
using UnityEngine;
using UnityEngine.SceneManagement;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;
using SceneLoadData = FishNet.Managing.Scened.SceneLoadData;

namespace Game.Scripts.Testing
{
    public class VehicleTestSceneController : MonoBehaviour
    {
        public RobotRegistry registry;
        public Vector3 spawnPosition = new Vector3(0f, 1.2f, 0f);
        public Vector3 spawnRotationEuler = Vector3.zero;
        public Transform spawnPoint;
        public Camera testCamera;
        public NetworkManager networkManager;
        public GameObject clientGameplayHudPrefab;
        public VehicleTestRuntimeSettings testRuntimeSettings;
        public bool replaceSceneGameplayHud = true;
        public bool autoStartHost = true;
        public ushort localTestPort = 7780;
        public bool autoSelectAvailablePort = true;
        public int maxPortSearchAttempts = 20;
        public bool showTestGui = true;
        public bool loadVehiclesFromApi = true;
        public bool autoSpawnFirstVehicle;
        public bool useDirectLocalSpawn;
        public float autoSpawnTimeout = 10f;
        public bool loadGameplaySceneForSpawns = true;
        public string gameplaySceneName = "Map";
        public float gameplaySceneLoadTimeout = 10f;

        private VehicleRuntimeStats[] _vehicles = new VehicleRuntimeStats[0];
        private VehicleRoot _spawnedVehicle;
        private int _selectedIndex;
        private bool _loading;
        private string _status = "Press Reload API vehicles.";
        private Vector2 _vehicleScroll;
        private Vector2 _statsScroll;
        private readonly StringBuilder _builder = new StringBuilder(512);
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _startedNetwork;
        private bool _startedTestServer;
        private bool _startedTestClient;
        private bool _networkStartInProgress;
        private bool _testCursorMode = true;
        private Rect _testGuiArea;
        private GameObject _spawnedGameplayHud;
        private bool _spawnInProgress;
        private bool _gameplaySceneLoadInProgress;
        private Scene _gameplayScene;
        private ServerRoom _testRoom;
        private readonly MatchVehicleSpawner _matchVehicleSpawner = new MatchVehicleSpawner();

        private void Awake()
        {
            ResolveSceneReferences();
            ResolveTestRuntimeSettings();
            EnsureGameplayHud();
            ResolveNetworkManager();
        }

        private void Start()
        {
            SetTestCursorMode(true);

            if (autoStartHost)
            {
                StartHostAsync().Forget();
            }

            if (autoSpawnFirstVehicle)
            {
                AutoSpawnFirstVehicleAsync().Forget();
            }

            if (loadVehiclesFromApi)
            {
                LoadVehiclesAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            if (_spawnedVehicle != null)
            {
                DespawnCurrent();
            }

            if (networkManager != null)
            {
                networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;

                if (_startedTestClient && networkManager.IsClientStarted)
                {
                    networkManager.ClientManager.StopConnection();
                }

                if (_startedTestServer && networkManager.IsServerStarted)
                {
                    networkManager.ServerManager.StopConnection(true);
                }
            }
        }

        private void Update()
        {
            if (VehicleInputController.Escape)
            {
                SetTestCursorMode(!_testCursorMode);
            }

            if (_testCursorMode && _spawnedVehicle != null && Input.GetMouseButtonDown(0) && !IsMouseOverTestGui())
            {
                SetTestCursorMode(false);
            }

            if (_spawnedVehicle != null && testRuntimeSettings != null)
            {
                testRuntimeSettings.ApplyToVehicle(_spawnedVehicle);
            }
        }

        private void OnGUI()
        {
            if (!showTestGui)
            {
                return;
            }

            using (ProfileScope.Measure("OnGUI.VehicleTestSceneController", DiagnosticsCategories.Editor))
            {
                _testGuiArea = new Rect(12f, 12f, 390f, Screen.height - 24f);
                GUILayout.BeginArea(_testGuiArea, GUI.skin.box);

                GUILayout.Label("Vehicle Parameter Test");
                GUILayout.Label(_status);

                GUI.enabled = !_loading;
                if (GUILayout.Button("Reload API vehicles", GUILayout.Height(30f)))
                {
                    LoadVehiclesAsync().Forget();
                }

                GUILayout.Space(8f);
                DrawVehicleList();
                GUILayout.Space(8f);
                DrawSelectedStats();
                GUILayout.Space(8f);
                DrawTestSettingsSummary();
                GUILayout.Space(8f);

                VehicleRuntimeStats selected = GetSelected();
                VehicleRoot prefab = GetSelectedPrefab(selected);
                GUI.enabled = !_loading && !_spawnInProgress && IsNetworkReady() && selected != null && prefab != null;
                if (GUILayout.Button("Spawn selected robot", GUILayout.Height(34f)))
                {
                    SpawnSelectedAsync().Forget();
                }

                GUI.enabled = _spawnedVehicle != null;
                if (GUILayout.Button("Despawn robot", GUILayout.Height(28f)))
                {
                    DespawnCurrent();
                }

                GUI.enabled = true;
                GUILayout.Space(8f);
                GUILayout.Label("Controls: WASD move, mouse aim, LMB fire, Space action.");

                GUILayout.EndArea();
            }
        }

        private bool IsMouseOverTestGui()
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector2 guiMousePosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            return _testGuiArea.Contains(guiMousePosition);
        }

        private async UniTaskVoid LoadVehiclesAsync()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            _status = "Loading vehicles from API...";

            VehicleRuntimeStats[] result = await VehicleStatsProvider.GetAllAsync(forceReload: true);
            _vehicles = result != null ? result : new VehicleRuntimeStats[0];
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _vehicles.Length - 1));
            _status = _vehicles.Length > 0
                ? "Loaded " + _vehicles.Length + " vehicles."
                : "No vehicles loaded. Check API server.";
            _loading = false;
        }

        private void DrawVehicleList()
        {
            GUILayout.Label("Vehicles");
            _vehicleScroll = GUILayout.BeginScrollView(_vehicleScroll, GUILayout.Height(210f));

            if (_vehicles == null || _vehicles.Length == 0)
            {
                GUILayout.Label("No vehicles.");
            }
            else
            {
                for (int i = 0; i < _vehicles.Length; i++)
                {
                    VehicleRuntimeStats stats = _vehicles[i];
                    if (stats == null)
                    {
                        continue;
                    }

                    VehicleRoot prefab = GetSelectedPrefab(stats);
                    string label = stats.Name + "  [" + stats.Code + "]";
                    if (prefab == null)
                    {
                        label += "  no prefab";
                    }

                    bool selected = i == _selectedIndex;
                    if (GUILayout.Toggle(selected, label, GUI.skin.button, GUILayout.Height(26f)) && !selected)
                    {
                        _selectedIndex = i;
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawSelectedStats()
        {
            VehicleRuntimeStats stats = GetSelected();
            GUILayout.Label("Selected stats");
            _statsScroll = GUILayout.BeginScrollView(_statsScroll, GUILayout.Height(190f));

            if (stats == null)
            {
                GUILayout.Label("Select a vehicle.");
            }
            else
            {
                GUILayout.TextArea(BuildStatsText(stats), GUILayout.ExpandHeight(true));
            }

            GUILayout.EndScrollView();
        }

        private void DrawTestSettingsSummary()
        {
            if (testRuntimeSettings == null)
            {
                GUILayout.Label("Test runtime settings: missing.");
                return;
            }

            string status = testRuntimeSettings.HasActiveTestParameters
                ? "Test overrides ON"
                : "Test overrides OFF";
            GUILayout.Label(status);
            if (testRuntimeSettings.HasActiveTestParameters)
            {
                GUILayout.Label("Reload: " + testRuntimeSettings.reloadTime.ToString("0.###") + " s");
                GUILayout.Label("Shells: " + testRuntimeSettings.shellsCount);
            }

            GUILayout.Label(testRuntimeSettings.createHitMarkerSphere
                ? "Hit markers ON"
                : "Hit markers OFF");
            GUILayout.Label(testRuntimeSettings.forceFullyAimedAccuracyOnly
                ? "Accuracy debug: fully aimed accuracy only ON"
                : "Accuracy debug OFF");
        }

        private string BuildStatsText(VehicleRuntimeStats stats)
        {
            _builder.Length = 0;
            _builder.Append("Name: ").Append(stats.Name).Append('\n');
            _builder.Append("Code: ").Append(stats.Code).Append('\n');
            _builder.Append("Level: ").Append(stats.Level).Append('\n');
            _builder.Append("HP: ").Append(stats.MaxHealth).Append('\n');
            _builder.Append("Damage: ").Append(stats.DamageMin).Append('-').Append(stats.DamageMax).Append('\n');
            _builder.Append("Penetration: ").Append(stats.Penetration).Append('\n');
            _builder.Append("Shell speed: ").Append(stats.ShellSpeed).Append('\n');
            _builder.Append("Ammo: ").Append(stats.ShellsCount).Append('\n');
            _builder.Append("Reload: ").Append(stats.ReloadTime).Append(" s\n");
            _builder.Append("Accuracy @100m: ").Append(stats.Accuracy).Append(" m\n");
            AppendResolvedAccuracyStats(stats);
            _builder.Append("Aim time: ").Append(stats.AimTime).Append(" s\n");
            _builder.Append("View range: ").Append(stats.ViewRange).Append(" m\n");
            _builder.Append("Speed: ").Append(stats.Speed).Append('\n');
            _builder.Append("Acceleration: ").Append(stats.Acceleration).Append('\n');
            _builder.Append("Traverse: ").Append(stats.TraverseSpeed).Append('\n');
            _builder.Append("Turret traverse: ").Append(stats.TurretTraverseSpeed).Append('\n');
            _builder.Append("Hull armor: ").Append(stats.HullArmor.Front).Append('/')
                .Append(stats.HullArmor.Side).Append('/').Append(stats.HullArmor.Rear).Append('\n');
            _builder.Append("Turret armor: ").Append(stats.TurretArmor.Front).Append('/')
                .Append(stats.TurretArmor.Side).Append('/').Append(stats.TurretArmor.Rear);
            return _builder.ToString();
        }

        private void AppendResolvedAccuracyStats(VehicleRuntimeStats stats)
        {
            GunDispersionGlobalSettings dispersionSettings = ServerSettings.GetGunDispersion();
            float dispersionDeg = dispersionSettings.GetAccuracyDispersionDeg(stats.Accuracy, 0f);
            float farRingDiameter = dispersionSettings.GetUiDiameter(dispersionDeg, dispersionDeg, 0f);
            float zoomRingDiameter = dispersionSettings.GetUiDiameter(dispersionDeg, dispersionDeg, 1f);
            _builder.Append("Fully aimed dispersion: ").Append(dispersionDeg.ToString("0.###")).Append(" deg\n");
            _builder.Append("Fully aimed ring far: ").Append(farRingDiameter.ToString("0.#")).Append(" px\n");
            _builder.Append("Fully aimed ring zoom: ").Append(zoomRingDiameter.ToString("0.#")).Append(" px\n");
        }

        private VehicleRuntimeStats GetSelected()
        {
            if (_vehicles == null || _vehicles.Length == 0)
            {
                return null;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _vehicles.Length)
            {
                return null;
            }

            return _vehicles[_selectedIndex];
        }

        private VehicleRoot GetSelectedPrefab(VehicleRuntimeStats stats)
        {
            if (registry == null || stats == null || string.IsNullOrEmpty(stats.Code))
            {
                return null;
            }

            return registry.GetPrefab(stats.Code);
        }

        private async UniTaskVoid AutoSpawnFirstVehicleAsync()
        {
            if (_spawnInProgress)
            {
                return;
            }

            bool networkReady = await WaitForNetworkReadyAsync(Mathf.Max(0.1f, autoSpawnTimeout));
            if (!networkReady)
            {
                _status = "Auto spawn failed: local FishNet host is not ready.";
                return;
            }

            NetworkConnection ownerConnection = GetLocalOwnerConnection();
            if (!IsConnectionReady(ownerConnection))
            {
                _status = "Auto spawn failed: local owner connection missing.";
                return;
            }

            if (!EnsureLocalConnectionInTestScene(ownerConnection))
            {
                _status = "Auto spawn failed: local owner is not in the test scene.";
                return;
            }

            VehicleRoot prefab = GetFirstRegistryPrefab(out string vehicleCode);
            if (prefab == null)
            {
                _status = "Auto spawn failed: RobotRegistry has no vehicle prefab.";
                return;
            }

            _spawnInProgress = true;
            bool spawned = SpawnVehicleDirect(prefab, null, ownerConnection, gameObject.scene, vehicleCode);
            _spawnInProgress = false;

            if (!spawned)
            {
                _status = "Auto spawn failed: direct spawn could not create vehicle.";
            }
        }

        private VehicleRoot GetFirstRegistryPrefab(out string vehicleCode)
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

        private async UniTaskVoid SpawnSelectedAsync()
        {
            if (_spawnInProgress)
            {
                return;
            }

            if (!IsNetworkReady())
            {
                _status = "Network is not ready yet.";
                if (autoStartHost)
                {
                    StartHostAsync().Forget();
                }
                return;
            }

            _spawnInProgress = true;

            VehicleRuntimeStats stats = GetSelected();
            VehicleRoot prefab = GetSelectedPrefab(stats);
            if (stats == null || prefab == null)
            {
                _status = "Cannot spawn selected vehicle: prefab missing.";
                _spawnInProgress = false;
                return;
            }

            VehicleRuntimeStats runtimeStats = BuildRuntimeStatsForSpawn(stats);
            if (runtimeStats == null)
            {
                _status = "Cannot spawn selected vehicle: runtime stats missing.";
                _spawnInProgress = false;
                return;
            }

            NetworkConnection ownerConnection = GetLocalOwnerConnection();
            if (!IsConnectionReady(ownerConnection))
            {
                _status = "Cannot spawn selected vehicle: local owner connection missing.";
                _spawnInProgress = false;
                return;
            }

            if (!EnsureLocalConnectionInTestScene(ownerConnection))
            {
                _status = "Cannot spawn selected vehicle: local owner is not in the test scene.";
                _spawnInProgress = false;
                return;
            }

            if (useDirectLocalSpawn)
            {
                bool spawned = SpawnVehicleDirect(prefab, runtimeStats, ownerConnection, gameObject.scene, runtimeStats.Name);
                _spawnInProgress = false;

                if (!spawned)
                {
                    _status = "Cannot spawn selected vehicle: direct spawn failed.";
                }

                return;
            }

            Scene spawnScene = await EnsureGameplaySpawnSceneAsync(ownerConnection, stats);
            if (!spawnScene.IsValid() || !spawnScene.isLoaded)
            {
                _status = "Cannot spawn selected vehicle: gameplay scene is not loaded.";
                _spawnInProgress = false;
                return;
            }

            if (!HasSpawnPoint(spawnScene))
            {
                _status = "Cannot spawn selected vehicle: no spawn points in " + spawnScene.name + ".";
                _spawnInProgress = false;
                return;
            }

            DespawnCurrent();

            EnsureGameResourceManager();
            ServerRoom room = PrepareTestRoom(ownerConnection, stats, spawnScene);
            _spawnedVehicle = await _matchVehicleSpawner.SpawnPlayerAsync(
                room,
                ownerConnection,
                spawnScene,
                Mathf.Max(0.1f, gameplaySceneLoadTimeout),
                networkManager.ServerManager,
                null);

            if (_spawnedVehicle == null)
            {
                _status = "Cannot spawn selected vehicle: MatchVehicleSpawner failed.";
                _spawnInProgress = false;
                return;
            }

            _spawnedVehicle.ServerApplyRuntimeStats(runtimeStats, syncObservers: true);
            if (testRuntimeSettings != null)
            {
                testRuntimeSettings.ApplyToVehicle(_spawnedVehicle);
            }

            SetTestCursorMode(false);

            _status = "Spawned " + runtimeStats.Name + ".";
            if (testRuntimeSettings != null && testRuntimeSettings.HasActiveTestParameters)
            {
                _status += " Test combat overrides applied.";
            }

            _spawnInProgress = false;
        }

        private bool SpawnVehicleDirect(
            VehicleRoot prefab,
            VehicleRuntimeStats runtimeStats,
            NetworkConnection ownerConnection,
            Scene spawnScene,
            string vehicleName)
        {
            if (prefab == null || !IsConnectionReady(ownerConnection) || networkManager == null || networkManager.ServerManager == null)
            {
                return false;
            }

            if (!spawnScene.IsValid() || !spawnScene.isLoaded)
            {
                return false;
            }

            DespawnCurrent();

            _spawnedVehicle = Instantiate(prefab, GetSpawnPosition(), GetSpawnRotation());
            if (_spawnedVehicle == null)
            {
                return false;
            }

            _spawnedVehicle.gameObject.SetActive(true);

            if (runtimeStats != null)
            {
                _spawnedVehicle.ServerApplyRuntimeStats(runtimeStats, syncObservers: false);
            }

            NetworkObject networkObject = _spawnedVehicle.networkObject != null
                ? _spawnedVehicle.networkObject
                : _spawnedVehicle.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Destroy(_spawnedVehicle.gameObject);
                _spawnedVehicle = null;
                return false;
            }

            networkManager.ServerManager.Spawn(networkObject, ownerConnection, spawnScene);

            if (runtimeStats != null)
            {
                _spawnedVehicle.ServerApplyRuntimeStats(runtimeStats, syncObservers: true);
            }

            if (testRuntimeSettings != null)
            {
                testRuntimeSettings.ApplyToVehicle(_spawnedVehicle);
            }

            if (_spawnedVehicle.characterInit != null)
            {
                _spawnedVehicle.characterInit.ServerInit(1, PlayerType.Player, "VehicleTest", MatchTeam.TeamA, spawnScene);
            }

            SetTestCursorMode(false);

            string displayName = !string.IsNullOrEmpty(vehicleName)
                ? vehicleName
                : _spawnedVehicle.name;
            _status = "Spawned " + displayName + ".";
            if (testRuntimeSettings != null && testRuntimeSettings.HasActiveTestParameters)
            {
                _status += " Test combat overrides applied.";
            }

            return true;
        }

        private VehicleRuntimeStats BuildRuntimeStatsForSpawn(VehicleRuntimeStats source)
        {
            if (testRuntimeSettings != null)
            {
                return testRuntimeSettings.BuildRuntimeStats(source);
            }

            return source != null ? source.Clone() : null;
        }

        private void EnsureGameResourceManager()
        {
            GameResourceManager resourceManager = FindAnyObjectByType<GameResourceManager>();
            if (resourceManager == null)
            {
                GameObject resourceObject = new GameObject("VehicleTest_GameResourceManager");
                SceneManager.MoveGameObjectToScene(resourceObject, gameObject.scene);
                resourceManager = resourceObject.AddComponent<GameResourceManager>();
            }

            if (resourceManager.registry == null && registry != null)
            {
                resourceManager.registry = registry;
            }
        }

        private async UniTask<Scene> EnsureGameplaySpawnSceneAsync(NetworkConnection ownerConnection, VehicleRuntimeStats stats)
        {
            if (!loadGameplaySceneForSpawns)
            {
                return gameObject.scene;
            }

            if (TryGetLoadedGameplayScene(out Scene loadedScene))
            {
                _gameplayScene = loadedScene;
                return _gameplayScene;
            }

            if (_gameplaySceneLoadInProgress)
            {
                return await WaitForGameplaySceneLoadedAsync();
            }

            if (networkManager == null || networkManager.SceneManager == null || string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return default;
            }

            _gameplaySceneLoadInProgress = true;
            _status = "Loading gameplay scene " + gameplaySceneName + "...";

            RegisterTestProfile(ownerConnection, stats);

            SceneLoadData sceneLoadData = new SceneLoadData(gameplaySceneName)
            {
                Options =
                {
                    AllowStacking = true,
                    AutomaticallyUnload = true,
                },
                Params =
                {
                    ClientParams = System.BitConverter.GetBytes(0)
                }
            };

            NetworkConnection[] connections = { ownerConnection };
            networkManager.SceneManager.LoadConnectionScenes(connections, sceneLoadData);

            _gameplayScene = await WaitForGameplaySceneLoadedAsync();
            _gameplaySceneLoadInProgress = false;
            return _gameplayScene;
        }

        private async UniTask<Scene> WaitForGameplaySceneLoadedAsync()
        {
            float timeout = Mathf.Max(0.1f, gameplaySceneLoadTimeout);
            float endTime = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (TryGetLoadedGameplayScene(out Scene loadedScene))
                {
                    return loadedScene;
                }

                await UniTask.Yield();
            }

            return default;
        }

        private bool TryGetLoadedGameplayScene(out Scene loadedScene)
        {
            loadedScene = default;
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.name == gameplaySceneName && HasSpawnPoint(scene))
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

        private ServerRoom PrepareTestRoom(NetworkConnection ownerConnection, VehicleRuntimeStats stats, Scene spawnScene)
        {
            if (_testRoom == null)
            {
                GameObject roomObject = new GameObject("VehicleTest_ServerRoom");
                SceneManager.MoveGameObjectToScene(roomObject, gameObject.scene);
                _testRoom = roomObject.AddComponent<ServerRoom>();
            }

            RegisterTestProfile(ownerConnection, stats);

            _testRoom.roomId = "VehicleTest";
            _testRoom.roomName = "VehicleTest";
            _testRoom.maxPlayers = 1;
            _testRoom.selectedLocation = spawnScene.IsValid() ? spawnScene.name : gameplaySceneName;
            _testRoom.isInGame = true;
            _testRoom.loadedSceneName = spawnScene.IsValid() ? spawnScene.name : string.Empty;
            _testRoom.handle = spawnScene.IsValid() ? spawnScene.handle : 0;
            _testRoom.sceneSlotIndex = ServerRoom.NoSceneSlot;
            _testRoom.sceneOffsetX = 0;
            _testRoom.players.Clear();
            _testRoom.AddPlayer(new LobbyPlayer
            {
                loginName = "VehicleTest",
                Connection = ownerConnection,
                userId = 0,
                mmr = 1000,
                activeVehicleId = stats != null ? stats.VehicleId : 0,
                activeVehicleCode = stats != null ? stats.Code : string.Empty,
                team = MatchTeam.TeamA,
                randomPlayerConnected = true
            });

            return _testRoom;
        }

        private static void RegisterTestProfile(NetworkConnection ownerConnection, VehicleRuntimeStats stats)
        {
            if (!IsConnectionReady(ownerConnection) || stats == null)
            {
                return;
            }

            PlayerProfile profile = ServerPlayerSessions.GetProfile(ownerConnection.ClientId);
            if (profile == null)
            {
                profile = new PlayerProfile
                {
                    id = 0,
                    username = "VehicleTest",
                    mmr = 1000
                };
            }

            profile.activeVehicleId = stats.VehicleId;
            profile.activeVehicleCode = stats.Code;
            profile.activeVehicleName = stats.Name;
            ServerPlayerSessions.SetProfile(ownerConnection, profile);
        }

        private void ClearTestRoomVehicle()
        {
            if (_testRoom == null || _testRoom.players == null)
            {
                return;
            }

            for (int i = 0; i < _testRoom.players.Count; i++)
            {
                LobbyPlayer player = _testRoom.players[i];
                if (player != null)
                {
                    player.playerRoot = null;
                }
            }
        }

        private void DespawnCurrent()
        {
            if (_spawnedVehicle == null)
            {
                return;
            }

            NetworkObject networkObject = _spawnedVehicle.networkObject != null
                ? _spawnedVehicle.networkObject
                : _spawnedVehicle.GetComponent<NetworkObject>();

            if (networkManager != null && networkManager.IsServerStarted && networkObject != null && networkObject.IsSpawned)
            {
                networkManager.ServerManager.Despawn(networkObject);
            }
            else
            {
                Destroy(_spawnedVehicle.gameObject);
            }

            _spawnedVehicle = null;
            ClearTestRoomVehicle();
            SetTestCursorMode(true);
        }

        private void SetTestCursorMode(bool enabled)
        {
            _testCursorMode = enabled;
            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;

            if (_spawnedVehicle != null && _spawnedVehicle.inputManager != null)
            {
                _spawnedVehicle.inputManager.SetControlsBlocked(enabled);
            }
        }

        private void ResolveSceneReferences()
        {
            if (testCamera == null)
            {
                testCamera = Camera.main;
            }

            if (testCamera == null)
            {
                _status = "Scene setup error: MainCamera is missing.";
                return;
            }

            CameraSync cameraSync = testCamera.GetComponent<CameraSync>();
            if (cameraSync == null)
            {
                _status = "Scene setup error: MainCamera needs CameraSync.";
                return;
            }

            cameraSync.gameplayCamera = testCamera;
        }

        private void ResolveTestRuntimeSettings()
        {
            if (testRuntimeSettings != null)
            {
                return;
            }

            VehicleTestRuntimeSettings[] settings = FindObjectsByType<VehicleTestRuntimeSettings>(FindObjectsSortMode.None);
            for (int i = 0; i < settings.Length; i++)
            {
                VehicleTestRuntimeSettings candidate = settings[i];
                if (candidate != null && candidate.gameObject.scene == gameObject.scene)
                {
                    testRuntimeSettings = candidate;
                    return;
                }
            }
        }

        private void EnsureGameplayHud()
        {
            if (clientGameplayHudPrefab == null || _spawnedGameplayHud != null)
            {
                return;
            }

            Canvas canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                _status = "Scene setup error: gameplay Canvas is missing.";
                return;
            }

            GameObject sceneHud = FindDirectChild(canvas.transform, "GameplayHUD");
            GameObject sceneUiRoot = FindRootObjectInScene("UI");
            int siblingIndex = sceneHud != null ? sceneHud.transform.GetSiblingIndex() : canvas.transform.childCount;

            _spawnedGameplayHud = Instantiate(clientGameplayHudPrefab, canvas.transform, false);
            _spawnedGameplayHud.name = clientGameplayHudPrefab.name;
            _spawnedGameplayHud.SetActive(true);
            _spawnedGameplayHud.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, canvas.transform.childCount - 1));

            ConfigureGameplayHud(_spawnedGameplayHud, canvas);

            if (replaceSceneGameplayHud && sceneHud != null && sceneHud != _spawnedGameplayHud)
            {
                sceneHud.SetActive(false);
                Destroy(sceneHud);
            }

            if (replaceSceneGameplayHud && sceneUiRoot != null && sceneUiRoot != _spawnedGameplayHud)
            {
                sceneUiRoot.SetActive(false);
                Destroy(sceneUiRoot);
            }
        }

        private Canvas FindGameplayCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas fallback = null;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.gameObject.scene != gameObject.scene)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = canvas;
                }

                if (canvas.name == "Canvas")
                {
                    return canvas;
                }
            }

            return fallback;
        }

        private GameObject FindRootObjectInScene(string objectName)
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root != null && root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == childName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void ConfigureGameplayHud(GameObject hudRoot, Canvas canvas)
        {
            if (hudRoot == null || canvas == null)
            {
                return;
            }

            GunCrosshair[] crosshairs = hudRoot.GetComponentsInChildren<GunCrosshair>(true);
            for (int i = 0; i < crosshairs.Length; i++)
            {
                GunCrosshair crosshair = crosshairs[i];
                if (crosshair == null)
                {
                    continue;
                }

                crosshair.ResolveCanvasReference(canvas);
            }
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
                return;
            }

            _serverState = networkManager.IsServerStarted ? LocalConnectionState.Started : LocalConnectionState.Stopped;
            _clientState = networkManager.IsClientStarted ? LocalConnectionState.Started : LocalConnectionState.Stopped;
            networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        private async UniTaskVoid StartHostAsync()
        {
            if (_networkStartInProgress || IsNetworkReady())
            {
                return;
            }

            if (networkManager == null)
            {
                ResolveNetworkManager();
                if (networkManager == null)
                {
                    return;
                }
            }

            _networkStartInProgress = true;
            _status = "Starting local FishNet host...";

            if (!networkManager.IsServerStarted)
            {
                if (!TryConfigureAvailablePort(out ushort port))
                {
                    _status = "FishNet host failed: no free UDP port near " + localTestPort + ".";
                    _networkStartInProgress = false;
                    return;
                }

                if (!networkManager.ServerManager.StartConnection(port))
                {
                    _status = "FishNet host failed: server could not bind UDP port " + port + ".";
                    _networkStartInProgress = false;
                    return;
                }

                _startedTestServer = true;
            }

            await WaitForServerStartedAsync();

            if (!networkManager.IsServerStarted)
            {
                _status = "FishNet host failed: server did not start.";
                StopStartedTestNetwork();
                _networkStartInProgress = false;
                return;
            }

            if (!networkManager.IsClientStarted)
            {
                ushort port = networkManager.TransportManager.Transport.GetPort();
                if (!networkManager.ClientManager.StartConnection("127.0.0.1", port))
                {
                    _status = "FishNet host failed: client could not connect to UDP port " + port + ".";
                    StopStartedTestNetwork();
                    _networkStartInProgress = false;
                    return;
                }

                _startedTestClient = true;
            }

            await WaitForClientStartedAsync();
            await WaitForLocalOwnerConnectionAsync();

            _startedNetwork = IsNetworkReady();
            _networkStartInProgress = false;

            if (_startedNetwork)
            {
                NetworkConnection ownerConnection = GetLocalOwnerConnection();
                if (!EnsureLocalConnectionInTestScene(ownerConnection))
                {
                    StopStartedTestNetwork();
                    _startedNetwork = false;
                    _status = "FishNet host failed: local owner did not enter the test scene.";
                    return;
                }

                _status = "Local FishNet host ready on UDP " + networkManager.TransportManager.Transport.GetPort() + ".";
            }
            else
            {
                StopStartedTestNetwork();
                _status = "FishNet host failed to start.";
            }
        }

        private bool TryConfigureAvailablePort(out ushort port)
        {
            port = 0;

            if (networkManager == null || networkManager.TransportManager == null || networkManager.TransportManager.Transport == null)
            {
                return false;
            }

            ushort startPort = localTestPort != 0
                ? localTestPort
                : networkManager.TransportManager.Transport.GetPort();

            int attempts = Mathf.Max(1, maxPortSearchAttempts);
            if (!autoSelectAvailablePort)
            {
                if (!IsUdpPortAvailable(startPort))
                {
                    return false;
                }

                port = startPort;
                networkManager.TransportManager.Transport.SetPort(port);
                return true;
            }

            for (int i = 0; i < attempts; i++)
            {
                int candidate = startPort + i;
                if (candidate > ushort.MaxValue)
                {
                    break;
                }

                ushort candidatePort = (ushort)candidate;
                if (!IsUdpPortAvailable(candidatePort))
                {
                    continue;
                }

                port = candidatePort;
                networkManager.TransportManager.Transport.SetPort(port);
                return true;
            }

            return false;
        }

        private static bool IsUdpPortAvailable(ushort port)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork))
                {
                    udpClient.ExclusiveAddressUse = true;
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                }

                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private void StopStartedTestNetwork()
        {
            if (networkManager == null)
            {
                return;
            }

            if (_startedTestClient && networkManager.IsClientStarted)
            {
                networkManager.ClientManager.StopConnection();
            }

            if (_startedTestServer && networkManager.IsServerStarted)
            {
                networkManager.ServerManager.StopConnection(true);
            }

            _startedTestClient = false;
            _startedTestServer = false;
        }

        private async UniTask WaitForServerStartedAsync()
        {
            float endTime = Time.realtimeSinceStartup + 5f;
            while (!networkManager.IsServerStarted && Time.realtimeSinceStartup < endTime)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask WaitForClientStartedAsync()
        {
            float endTime = Time.realtimeSinceStartup + 5f;
            while (!networkManager.IsClientStarted && Time.realtimeSinceStartup < endTime)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask WaitForLocalOwnerConnectionAsync()
        {
            float endTime = Time.realtimeSinceStartup + 5f;
            while (!IsConnectionReady(GetLocalOwnerConnection()) && Time.realtimeSinceStartup < endTime)
            {
                await UniTask.Yield();
            }
        }

        private async UniTask<bool> WaitForNetworkReadyAsync(float timeout)
        {
            float endTime = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (IsNetworkReady())
                {
                    return true;
                }

                await UniTask.Yield();
            }

            return IsNetworkReady();
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverState = args.ConnectionState;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientState = args.ConnectionState;
        }

        private bool IsNetworkReady()
        {
            if (networkManager == null)
            {
                return false;
            }

            return networkManager.IsServerStarted
                   && networkManager.IsClientStarted
                   && _serverState == LocalConnectionState.Started
                   && _clientState == LocalConnectionState.Started
                   && IsConnectionReady(GetLocalOwnerConnection());
        }

        private NetworkConnection GetLocalOwnerConnection()
        {
            if (networkManager == null || networkManager.ClientManager == null || networkManager.ServerManager == null)
            {
                return null;
            }

            NetworkConnection clientConnection = networkManager.ClientManager.Connection;
            if (!IsConnectionReady(clientConnection))
            {
                return null;
            }

            if (networkManager.ServerManager.Clients == null)
            {
                return null;
            }

            if (networkManager.ServerManager.Clients.TryGetValue(clientConnection.ClientId, out NetworkConnection serverConnection)
                && IsConnectionReady(serverConnection))
            {
                return serverConnection;
            }

            return null;
        }

        private bool EnsureLocalConnectionInTestScene(NetworkConnection connection)
        {
            if (!IsConnectionReady(connection) || networkManager == null || networkManager.SceneManager == null)
            {
                return false;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (connection.Scenes != null && connection.Scenes.Contains(scene))
            {
                return true;
            }

            networkManager.SceneManager.AddConnectionToScene(connection, scene);
            return connection.Scenes != null && connection.Scenes.Contains(scene);
        }

        private static bool IsConnectionReady(NetworkConnection connection)
        {
            return connection != null && connection.IsValid;
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            return spawnPosition;
        }

        private Quaternion GetSpawnRotation()
        {
            if (spawnPoint != null)
            {
                return spawnPoint.rotation;
            }

            return Quaternion.Euler(spawnRotationEuler);
        }
    }
}
