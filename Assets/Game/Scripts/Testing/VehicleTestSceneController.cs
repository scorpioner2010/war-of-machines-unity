using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Resources;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
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
        private enum TestPanelTab
        {
            Vehicle = 0,
            Bots = 1,
            Runtime = 2
        }

        private const string TestPlayerName = "VehicleTest";
        private const float ExpandedPanelMaxWidth = 900f;
        private const float ExpandedPanelMaxHeight = 760f;
        private const float ReferenceScreenWidth = 1600f;
        private const float ReferenceScreenHeight = 900f;
        private const float PanelScreenPadding = 12f;

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
        public ushort localTestTickRate = 120;
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
        private VehicleRuntimeStats _spawnedPlayerStats;
        private int _selectedIndex;
        private bool _loading;
        private string _status = "Press Reload API vehicles.";
        private Vector2 _vehicleScroll;
        private Vector2 _statsScroll;
        private Vector2 _botsScroll;
        private readonly string[] _testPanelTabs =
        {
            "Vehicle",
            "Bots",
            "Runtime"
        };
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _startedNetwork;
        private bool _startedTestServer;
        private bool _startedTestClient;
        private bool _networkStartInProgress;
        private bool _testCursorMode = true;
        private bool _testPanelExpanded = true;
        private bool _useCompactLayout;
        private float _uiScale = 1f;
        private float _styleScale = -1f;
        private TestPanelTab _activeTab = TestPanelTab.Vehicle;
        private Rect _testGuiArea;
        private GUIStyle _panelStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _vehicleRowStyle;
        private GUIStyle _selectedVehicleRowStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _detailLabelStyle;
        private GUIStyle _detailValueStyle;
        private GUIStyle _hintStyle;
        private GameObject _spawnedGameplayHud;
        private bool _gameplayHudHiddenForTest;
        private bool _gameplayHudOpenedForTest;
        private bool _spawnInProgress;
        private bool _botSpawnInProgress;
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
                if (networkManager.ServerManager != null)
                {
                    networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                }

                if (networkManager.ClientManager != null)
                {
                    networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                }
            }

            StopStartedTestNetwork();
        }

        private void OnApplicationQuit()
        {
            StopStartedTestNetwork();
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
            _uiScale = GetUiScale();
            EnsureGuiStyles();

            if (!ShouldDrawTestGui())
            {
                RefreshGameplayHudVisibilityForTest();
                _testGuiArea = Rect.zero;
                return;
            }

            RefreshGameplayHudVisibilityForTest();

            using (ProfileScope.Measure("OnGUI.VehicleTestSceneController", DiagnosticsCategories.Editor))
            {
                if (!_testPanelExpanded)
                {
                    DrawCollapsedTestPanel();
                    return;
                }

                _testGuiArea = GetExpandedTestPanelRect();
                _useCompactLayout = _testGuiArea.width < 720f;
                GUILayout.BeginArea(_testGuiArea, _panelStyle);

                DrawTestPanelHeader();
                GUILayout.Space(6f * _uiScale);
                DrawTestPanelTabs();
                GUILayout.Space(8f * _uiScale);

                if (_activeTab == TestPanelTab.Vehicle)
                {
                    DrawVehicleTab();
                }
                else if (_activeTab == TestPanelTab.Bots)
                {
                    DrawBotsTab();
                }
                else
                {
                    DrawRuntimeTab();
                }

                GUILayout.EndArea();
            }
        }

        private void DrawCollapsedTestPanel()
        {
            float padding = PanelScreenPadding * _uiScale;
            _testGuiArea = new Rect(padding, padding, 190f * _uiScale, 46f * _uiScale);
            GUILayout.BeginArea(_testGuiArea, _panelStyle);

            if (GUILayout.Button("Open Vehicle Test", _primaryButtonStyle, GUILayout.Height(30f * _uiScale)))
            {
                _testPanelExpanded = true;
                SetTestCursorMode(true);
                RefreshGameplayHudVisibilityForTest();
            }

            GUILayout.EndArea();
        }

        private void DrawTestPanelHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vehicle Test", _titleStyle);

            GUILayout.FlexibleSpace();
            GUI.enabled = !_loading;
            if (GUILayout.Button("Reload", _secondaryButtonStyle, GUILayout.Width(82f * _uiScale), GUILayout.Height(28f * _uiScale)))
            {
                LoadVehiclesAsync().Forget();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Collapse", _secondaryButtonStyle, GUILayout.Width(92f * _uiScale), GUILayout.Height(28f * _uiScale)))
            {
                _testPanelExpanded = false;
                RefreshGameplayHudVisibilityForTest();
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(_status, _statusStyle);
        }

        private void DrawTestPanelTabs()
        {
            GUI.enabled = true;
            int selectedTab = GUILayout.Toolbar((int)_activeTab, _testPanelTabs, _tabStyle, GUILayout.Height(30f * _uiScale));
            _activeTab = (TestPanelTab)Mathf.Clamp(selectedTab, 0, _testPanelTabs.Length - 1);
        }

        private void DrawVehicleTab()
        {
            float contentHeight = GetVehicleContentHeight();
            if (_useCompactLayout)
            {
                float listHeight = Mathf.Max(110f * _uiScale, contentHeight * 0.4f);
                DrawVehicleList(listHeight);
                GUILayout.Space(8f * _uiScale);
                DrawSelectedStats(Mathf.Max(120f * _uiScale, contentHeight - listHeight - 8f * _uiScale));
            }
            else
            {
                float contentWidth = Mathf.Max(1f, _testGuiArea.width - _panelStyle.padding.horizontal);
                float catalogWidth = Mathf.Max(270f * _uiScale, contentWidth * 0.4f);

                GUILayout.BeginHorizontal();
                DrawVehicleList(contentHeight, catalogWidth);
                GUILayout.Space(8f * _uiScale);
                DrawSelectedStats(contentHeight);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f * _uiScale);
            DrawVehicleActions();
        }

        private void DrawVehicleActions()
        {
            VehicleRuntimeStats selected = GetSelected();
            VehicleRoot prefab = GetSelectedPrefab(selected);
            bool canSpawn = !_loading && !_spawnInProgress && IsNetworkReady() && selected != null && prefab != null;

            GUILayout.BeginHorizontal();
            GUI.enabled = canSpawn;
            string spawnLabel = _useCompactLayout ? "Spawn robot" : "Spawn selected robot";
            if (GUILayout.Button(spawnLabel, _primaryButtonStyle, GUILayout.Height(38f * _uiScale)))
            {
                SpawnSelectedAsync().Forget();
            }

            GUI.enabled = _spawnedVehicle != null;
            if (GUILayout.Button("Despawn", _secondaryButtonStyle, GUILayout.Width(110f * _uiScale), GUILayout.Height(38f * _uiScale)))
            {
                DespawnCurrent();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(5f * _uiScale);
            GUILayout.Label("WASD move  •  mouse aim  •  LMB fire  •  Space action", _hintStyle);
        }

        private void DrawBotsTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Test bots", _sectionTitleStyle);
            GUILayout.Label("Player vehicle: " + (_spawnedVehicle != null ? _spawnedVehicle.name : "not spawned"), _detailLabelStyle);
            GUILayout.Label("Bots in room: " + CountSpawnedBots(), _detailLabelStyle);

            Scene playerScene = _spawnedVehicle != null ? _spawnedVehicle.gameObject.scene : default;
            string sceneName = playerScene.IsValid() ? playerScene.name : "none";
            GUILayout.Label("Bot spawn scene: " + sceneName, _detailLabelStyle);

            bool canSpawnBot = !_loading
                               && !_spawnInProgress
                               && !_botSpawnInProgress
                               && IsNetworkReady()
                               && _spawnedVehicle != null
                               && playerScene.IsValid()
                               && playerScene.isLoaded
                               && HasSpawnPoint(playerScene);

            GUILayout.Space(6f * _uiScale);
            GUILayout.BeginHorizontal();
            GUI.enabled = canSpawnBot;
            if (GUILayout.Button("Add enemy bot", _primaryButtonStyle, GUILayout.Height(36f * _uiScale)))
            {
                SpawnRandomBotAsync(false).Forget();
            }

            if (GUILayout.Button("Add ally bot", _secondaryButtonStyle, GUILayout.Height(36f * _uiScale)))
            {
                SpawnRandomBotAsync(true).Forget();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (!canSpawnBot)
            {
                GUILayout.Space(6f * _uiScale);
                GUILayout.Label(BuildBotSpawnBlockReason(playerScene), _statusStyle);
            }

            GUILayout.EndVertical();

            GUILayout.Space(8f * _uiScale);
            DrawBotList(GetScrollableListHeight(250f));
        }

        private void DrawRuntimeTab()
        {
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Runtime status", _sectionTitleStyle);
            DrawStatRow("Network", IsNetworkReady() ? "Ready" : "Not ready");
            DrawStatRow("Server", _serverState.ToString());
            DrawStatRow("Client", _clientState.ToString());
            DrawStatRow("Input mode", _testCursorMode ? "Test UI" : "Vehicle control");
            GUILayout.Space(8f * _uiScale);
            DrawTestSettingsSummary();
            GUILayout.EndVertical();
        }

        private string BuildBotSpawnBlockReason(Scene playerScene)
        {
            if (_loading)
            {
                return "Bot spawn blocked: vehicles are still loading.";
            }

            if (_spawnInProgress)
            {
                return "Bot spawn blocked: player vehicle spawn is in progress.";
            }

            if (_botSpawnInProgress)
            {
                return "Bot spawn blocked: bot spawn is in progress.";
            }

            if (!IsNetworkReady())
            {
                return "Bot spawn blocked: local FishNet host is not ready.";
            }

            if (_spawnedVehicle == null)
            {
                return "Bot spawn blocked: spawn the player vehicle first.";
            }

            if (!playerScene.IsValid() || !playerScene.isLoaded)
            {
                return "Bot spawn blocked: player vehicle scene is not loaded.";
            }

            if (!HasSpawnPoint(playerScene))
            {
                return "Bot spawn blocked: current player scene has no SpawnPoint.";
            }

            return "Bot spawn blocked.";
        }

        private void DrawBotList(float height)
        {
            GUILayout.BeginVertical(_cardStyle, GUILayout.Height(height));
            GUILayout.Label("Room bots", _sectionTitleStyle);
            _botsScroll = GUILayout.BeginScrollView(_botsScroll, GUILayout.Height(Mathf.Max(60f * _uiScale, height - 48f * _uiScale)));

            if (_testRoom == null || _testRoom.players == null)
            {
                GUILayout.Label("No room yet.", _hintStyle);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            bool hasBots = false;
            for (int i = 0; i < _testRoom.players.Count; i++)
            {
                LobbyPlayer player = _testRoom.players[i];
                if (player == null || !player.isBot)
                {
                    continue;
                }

                hasBots = true;
                string rootState = player.playerRoot != null ? "spawned" : "missing root";
                if (player.playerRoot != null && player.playerRoot.health != null && player.playerRoot.health.IsDead)
                {
                    rootState = "dead";
                }

                GUILayout.Label(player.loginName
                                + "  •  "
                                + player.team
                                + "  •  "
                                + player.activeVehicleCode
                                + "  •  "
                                + rootState,
                    _detailLabelStyle);
            }

            if (!hasBots)
            {
                GUILayout.Label("No bots spawned.", _hintStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private bool ShouldDrawTestGui()
        {
            return showTestGui;
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

        private void DrawVehicleList(float height, float width = 0f)
        {
            if (width > 0f)
            {
                GUILayout.BeginVertical(_cardStyle, GUILayout.Width(width), GUILayout.Height(height));
            }
            else
            {
                GUILayout.BeginVertical(_cardStyle, GUILayout.Height(height));
            }

            GUILayout.Label("Vehicle catalog", _sectionTitleStyle);
            GUILayout.Label(_vehicles != null ? _vehicles.Length + " vehicles loaded" : "No vehicles loaded", _hintStyle);
            _vehicleScroll = GUILayout.BeginScrollView(_vehicleScroll, GUILayout.Height(Mathf.Max(50f * _uiScale, height - 72f * _uiScale)));

            if (_vehicles == null || _vehicles.Length == 0)
            {
                GUILayout.Label("No vehicles. Use Reload after the API is ready.", _hintStyle);
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
                    string label = GetVehicleDisplayName(stats) + "\n" + stats.Code + "  •  Level " + stats.Level;
                    if (prefab == null)
                    {
                        label += "  •  no prefab";
                    }

                    bool selected = i == _selectedIndex;
                    if (GUILayout.Button(label, selected ? _selectedVehicleRowStyle : _vehicleRowStyle, GUILayout.Height(46f * _uiScale)))
                    {
                        _selectedIndex = i;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawSelectedStats(float height)
        {
            VehicleRuntimeStats stats = GetSelected();
            GUILayout.BeginVertical(_cardStyle, GUILayout.Height(height));
            GUILayout.Label("Selected vehicle", _sectionTitleStyle);

            if (stats == null)
            {
                GUILayout.Label("Choose a vehicle from the catalog.", _hintStyle);
            }
            else
            {
                GUILayout.Label(GetVehicleDisplayName(stats) + "  •  " + stats.Code, _titleStyle);
                GUILayout.Label("Level " + stats.Level + (GetSelectedPrefab(stats) != null ? "  •  ready to spawn" : "  •  prefab missing"), _hintStyle);
                GUILayout.Space(4f * _uiScale);
                _statsScroll = GUILayout.BeginScrollView(_statsScroll, GUILayout.Height(Mathf.Max(50f * _uiScale, height - 105f * _uiScale)));
                DrawVehicleStats(stats);
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
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
            GUILayout.Label(status, _sectionTitleStyle);
            if (testRuntimeSettings.HasActiveTestParameters)
            {
                DrawStatRow("Reload", testRuntimeSettings.reloadTime.ToString("0.###") + " s");
                DrawStatRow("Shells", testRuntimeSettings.shellsCount.ToString());
            }

            DrawStatRow("Hit markers", testRuntimeSettings.createHitMarkerSphere ? "On" : "Off");
            DrawStatRow("Forced full aim", testRuntimeSettings.forceFullyAimedAccuracyOnly ? "On" : "Off");
        }

        private void DrawVehicleStats(VehicleRuntimeStats stats)
        {
            DrawStatSection("Combat");
            DrawStatRow("Durability", stats.MaxHealth.ToString("0"));
            DrawStatRow("Damage", stats.DamageMin.ToString("0.#") + " – " + stats.DamageMax.ToString("0.#"));
            DrawStatRow("Penetration", stats.Penetration.ToString("0.#"));
            DrawStatRow("Reload", stats.ReloadTime.ToString("0.###") + " s");
            DrawStatRow("Ammo", stats.ShellsCount.ToString());
            DrawStatRow("Shell speed", stats.ShellSpeed.ToString("0.#") + " m/s");

            DrawStatSection("Aiming");
            DrawStatRow("Accuracy @100m", stats.Accuracy.ToString("0.###") + " m");
            DrawStatRow("Aim time", stats.AimTime.ToString("0.###") + " s");
            DrawStatRow("View range", stats.ViewRange.ToString("0.#") + " m");

            GunDispersionGlobalSettings dispersionSettings = ServerSettings.GetGunDispersion();
            float dispersionDeg = dispersionSettings.GetAccuracyDispersionDeg(stats.Accuracy, 0f);
            float farRingDiameter = dispersionSettings.GetUiDiameter(dispersionDeg, dispersionDeg, 0f);
            float zoomRingDiameter = dispersionSettings.GetUiDiameter(dispersionDeg, dispersionDeg, 1f);
            DrawStatRow("Full aim spread", dispersionDeg.ToString("0.###") + "°");
            DrawStatRow("Aim ring", farRingDiameter.ToString("0.#") + " / " + zoomRingDiameter.ToString("0.#") + " px");

            DrawStatSection("Mobility");
            DrawStatRow("Speed", stats.Speed.ToString("0.#"));
            DrawStatRow("Acceleration", stats.Acceleration.ToString("0.#"));
            DrawStatRow("Hull traverse", stats.TraverseSpeed.ToString("0.#"));
            DrawStatRow("Turret traverse", stats.TurretTraverseSpeed.ToString("0.#"));

            DrawStatSection("Armor  front / side / rear");
            DrawStatRow("Hull", stats.HullArmor.Front + " / " + stats.HullArmor.Side + " / " + stats.HullArmor.Rear);
            DrawStatRow("Turret", stats.TurretArmor.Front + " / " + stats.TurretArmor.Side + " / " + stats.TurretArmor.Rear);
        }

        private void DrawStatSection(string title)
        {
            GUILayout.Space(6f * _uiScale);
            GUILayout.Label(title, _sectionTitleStyle);
        }

        private void DrawStatRow(string label, string value)
        {
            float availableWidth = Mathf.Max(1f, _testGuiArea.width - _panelStyle.padding.horizontal);
            float labelWidth = Mathf.Clamp(availableWidth * 0.44f, 105f * _uiScale, 245f * _uiScale);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _detailLabelStyle, GUILayout.Width(labelWidth));
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, _detailValueStyle);
            GUILayout.EndHorizontal();
        }

        private static string GetVehicleDisplayName(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                return "Unknown vehicle";
            }

            return string.IsNullOrWhiteSpace(stats.Name) ? stats.Code : stats.Name;
        }

        private float GetVehicleContentHeight()
        {
            float reservedHeight = _useCompactLayout ? 222f : 176f;
            return Mathf.Max(150f * _uiScale, _testGuiArea.height - reservedHeight * _uiScale);
        }

        private float GetScrollableListHeight(float reservedHeight)
        {
            return Mathf.Max(110f * _uiScale, _testGuiArea.height - reservedHeight * _uiScale);
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

            if (!await WaitForConnectionReadyForSceneLoadAsync(ownerConnection))
            {
                _status = "Cannot spawn selected vehicle: local owner is not ready for scene load.";
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
            _spawnedPlayerStats = stats.Clone();
            if (testRuntimeSettings != null)
            {
                testRuntimeSettings.ApplyToVehicle(_spawnedVehicle);
            }

            ConfigureTestCameraForPlayerVehicle(_spawnedVehicle);
            ConfigureWorldHudForTestVehicle(_spawnedVehicle, TestPlayerName);
            SetTestCursorMode(false);

            _status = "Spawned " + runtimeStats.Name + ".";
            if (testRuntimeSettings != null && testRuntimeSettings.HasActiveTestParameters)
            {
                _status += " Test combat overrides applied.";
            }

            _spawnInProgress = false;
        }

        private async UniTaskVoid SpawnRandomBotAsync(bool ally)
        {
            if (_botSpawnInProgress)
            {
                return;
            }

            if (!IsNetworkReady())
            {
                _status = "Cannot spawn bot: local FishNet host is not ready.";
                if (autoStartHost)
                {
                    StartHostAsync().Forget();
                }

                return;
            }

            if (_spawnedVehicle == null)
            {
                _status = "Cannot spawn bot: spawn the player vehicle first.";
                return;
            }

            Scene spawnScene = _spawnedVehicle.gameObject.scene;
            if (!spawnScene.IsValid() || !spawnScene.isLoaded)
            {
                _status = "Cannot spawn bot: player vehicle scene is not loaded.";
                return;
            }

            if (!HasSpawnPoint(spawnScene))
            {
                _status = "Cannot spawn bot: current player scene has no SpawnPoint.";
                return;
            }

            NetworkConnection ownerConnection = GetLocalOwnerConnection();
            if (!IsConnectionReady(ownerConnection))
            {
                _status = "Cannot spawn bot: local owner connection missing.";
                return;
            }

            _botSpawnInProgress = true;
            try
            {
                EnsureGameResourceManager();

                VehicleRuntimeStats playerStats = _spawnedPlayerStats != null ? _spawnedPlayerStats : GetSelected();
                ServerRoom room = PrepareTestRoom(ownerConnection, playerStats, spawnScene);
                LobbyPlayer testPlayer = room.GetPlayerByConnection(ownerConnection);
                if (testPlayer != null)
                {
                    testPlayer.playerRoot = _spawnedVehicle;
                }

                string vehicleCode = PickRandomBotVehicleCode();
                if (string.IsNullOrEmpty(vehicleCode))
                {
                    _status = "Cannot spawn bot: no valid vehicle prefab was found.";
                    return;
                }

                MatchTeam playerTeam = GetSpawnedPlayerTeam();
                LobbyPlayer bot = CreateTestBotPlayer(room, ally, playerTeam, vehicleCode);
                room.AddPlayer(bot);
                UpdateTestRoomMaxPlayers(room);

                VehicleRoot botRoot = await _matchVehicleSpawner.SpawnBotAsync(
                    room,
                    bot,
                    spawnScene,
                    Mathf.Max(0.1f, gameplaySceneLoadTimeout),
                    networkManager.ServerManager,
                    null);

                if (botRoot == null)
                {
                    room.RemovePlayer(bot);
                    UpdateTestRoomMaxPlayers(room);
                    _status = "Cannot spawn bot: MatchVehicleSpawner failed.";
                    return;
                }

                ConfigureWorldHudForTestVehicle(botRoot, bot.loginName);
                _status = "Spawned " + (ally ? "ally" : "enemy") + " bot "
                          + bot.loginName
                          + " ["
                          + vehicleCode
                          + "].";
            }
            finally
            {
                _botSpawnInProgress = false;
            }
        }

        private void ConfigureWorldHudForTestVehicle(VehicleRoot vehicleRoot, string nickname)
        {
            if (vehicleRoot == null || vehicleRoot.vehicleHUD == null)
            {
                return;
            }

            vehicleRoot.vehicleHUD.SetVehicleRoot(vehicleRoot);

            Camera camera = ResolveGameplayCameraForWorldHud();
            if (camera != null)
            {
                vehicleRoot.vehicleHUD.SetCamera(camera);
            }

            if (string.IsNullOrEmpty(nickname) && vehicleRoot.characterInit != null)
            {
                nickname = vehicleRoot.characterInit.LoginName.Value;
            }

            if (!string.IsNullOrEmpty(nickname))
            {
                vehicleRoot.vehicleHUD.SetNick(nickname);
            }
        }

        private void ConfigureTestCameraForPlayerVehicle(VehicleRoot vehicleRoot)
        {
            CameraSync cameraSync = RefreshTestCameraSync();
            if (cameraSync == null || vehicleRoot == null || vehicleRoot.cameraController == null)
            {
                return;
            }

            vehicleRoot.cameraController.Init();
            cameraSync.target = vehicleRoot.cameraController.transform;
            cameraSync.SyncToTarget();
        }

        private Camera ResolveGameplayCameraForWorldHud()
        {
            RefreshTestCameraSync();

            if (testCamera != null)
            {
                return testCamera;
            }

            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                return CameraSync.In.gameplayCamera;
            }

            return null;
        }

        private string PickRandomBotVehicleCode()
        {
            List<string> candidates = new List<string>(16);

            if (_vehicles != null)
            {
                for (int i = 0; i < _vehicles.Length; i++)
                {
                    VehicleRuntimeStats stats = _vehicles[i];
                    if (stats != null)
                    {
                        AddValidVehicleCode(candidates, stats.Code);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                AddValidVehicleCode(candidates, ServerSettings.GetDefaultBotVehicleCode());
            }

            if (candidates.Count == 0)
            {
                List<string> registryCodes = new List<string>(16);
                GameResourceManager.FillVehicleCodes(registryCodes);
                for (int i = 0; i < registryCodes.Count; i++)
                {
                    AddValidVehicleCode(candidates, registryCodes[i]);
                }
            }

            if (candidates.Count == 0)
            {
                AddValidVehicleCode(candidates, GameResourceManager.GetFirstVehicleCode());
            }

            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void AddValidVehicleCode(List<string> candidates, string vehicleCode)
        {
            if (candidates == null || string.IsNullOrEmpty(vehicleCode) || !HasVehiclePrefab(vehicleCode))
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == vehicleCode)
                {
                    return;
                }
            }

            candidates.Add(vehicleCode);
        }

        private bool HasVehiclePrefab(string vehicleCode)
        {
            if (string.IsNullOrEmpty(vehicleCode))
            {
                return false;
            }

            if (registry != null && registry.GetPrefab(vehicleCode) != null)
            {
                return true;
            }

            return GameResourceManager.GetPrefab(vehicleCode) != null;
        }

        private LobbyPlayer CreateTestBotPlayer(ServerRoom room, bool ally, MatchTeam playerTeam, string vehicleCode)
        {
            MatchTeam botTeam = ally ? playerTeam : GetOpposingTeam(playerTeam);
            return new LobbyPlayer
            {
                loginName = BuildTestBotName(room, ally),
                Connection = null,
                token = string.Empty,
                userId = 0,
                mmr = ServerSettings.GetBotMmr(),
                activeVehicleId = 0,
                activeVehicleCode = vehicleCode,
                team = botTeam,
                isBot = true,
                randomPlayerConnected = true
            };
        }

        private string BuildTestBotName(ServerRoom room, bool ally)
        {
            string basePrefix = ally ? "Ally " : "Enemy ";
            basePrefix += ServerSettings.GetBotNamePrefix();

            int index = 1;
            while (index < 10000)
            {
                string candidate = basePrefix + index;
                if (room == null || room.GetPlayerByName(candidate) == null)
                {
                    return candidate;
                }

                index++;
            }

            return basePrefix + Random.Range(10000, 99999);
        }

        private MatchTeam GetSpawnedPlayerTeam()
        {
            if (_spawnedVehicle != null && _spawnedVehicle.characterInit != null)
            {
                MatchTeam team = _spawnedVehicle.characterInit.Team.Value;
                if (MatchTeamUtility.IsAssigned(team))
                {
                    return team;
                }
            }

            return MatchTeam.TeamA;
        }

        private static MatchTeam GetOpposingTeam(MatchTeam team)
        {
            return team == MatchTeam.TeamB ? MatchTeam.TeamA : MatchTeam.TeamB;
        }

        private int CountSpawnedBots()
        {
            if (_testRoom == null || _testRoom.players == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _testRoom.players.Count; i++)
            {
                LobbyPlayer player = _testRoom.players[i];
                if (player != null && player.isBot && player.playerRoot != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void UpdateTestRoomMaxPlayers(ServerRoom room)
        {
            if (room == null || room.players == null)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < room.players.Count; i++)
            {
                if (room.players[i] != null)
                {
                    count++;
                }
            }

            room.maxPlayers = Mathf.Max(1, count);
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
                _spawnedPlayerStats = runtimeStats.Clone();
            }
            else
            {
                _spawnedPlayerStats = null;
            }

            if (testRuntimeSettings != null)
            {
                testRuntimeSettings.ApplyToVehicle(_spawnedVehicle);
            }

            if (_spawnedVehicle.characterInit != null)
            {
                _spawnedVehicle.characterInit.ServerInit(1, PlayerType.Player, TestPlayerName, MatchTeam.TeamA, spawnScene);
            }

            ConfigureTestCameraForPlayerVehicle(_spawnedVehicle);
            ConfigureWorldHudForTestVehicle(_spawnedVehicle, TestPlayerName);
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
                EnsureConnectionInScene(ownerConnection, gameObject.scene);
                return gameObject.scene;
            }

            if (TryGetLoadedGameplayScene(ownerConnection, out Scene loadedScene))
            {
                _gameplayScene = loadedScene;
                return _gameplayScene;
            }

            if (_gameplaySceneLoadInProgress)
            {
                return await WaitForGameplaySceneLoadedAsync(ownerConnection);
            }

            if (networkManager == null || networkManager.SceneManager == null || string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return default;
            }

            _gameplaySceneLoadInProgress = true;
            _status = "Loading gameplay scene " + gameplaySceneName + "...";

            RegisterTestProfile(ownerConnection, stats);
            ServerRoom roomForLoad = PrepareTestRoom(ownerConnection, stats, gameObject.scene);
            roomForLoad.selectedLocation = gameplaySceneName;
            roomForLoad.loadedSceneName = string.Empty;
            roomForLoad.handle = 0;

            SceneLoadData sceneLoadData = TryGetLoadedGameplayScene(out Scene loadedSceneWithoutConnection)
                ? new SceneLoadData(loadedSceneWithoutConnection)
                : new SceneLoadData(gameplaySceneName);

            sceneLoadData.Options.AllowStacking = true;
            sceneLoadData.Options.AutomaticallyUnload = false;
            sceneLoadData.Params.ServerParams = new object[]
            {
                roomForLoad
            };
            sceneLoadData.Params.ClientParams = System.BitConverter.GetBytes(0);

            NetworkConnection[] connections = { ownerConnection };
            networkManager.SceneManager.LoadConnectionScenes(connections, sceneLoadData);

            _gameplayScene = await WaitForGameplaySceneLoadedAsync(ownerConnection);
            _gameplaySceneLoadInProgress = false;
            return _gameplayScene;
        }

        private async UniTask<Scene> WaitForGameplaySceneLoadedAsync(NetworkConnection ownerConnection)
        {
            float timeout = Mathf.Max(0.1f, gameplaySceneLoadTimeout);
            float endTime = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < endTime)
            {
                if (TryGetLoadedGameplayScene(ownerConnection, out Scene loadedScene))
                {
                    return loadedScene;
                }

                await UniTask.Yield();
            }

            return default;
        }

        private bool TryGetLoadedGameplayScene(NetworkConnection ownerConnection, out Scene loadedScene)
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
                    && IsConnectionInScene(ownerConnection, scene))
                {
                    loadedScene = scene;
                    return true;
                }
            }

            return false;
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
                if (scene.IsValid()
                    && scene.isLoaded
                    && scene.name == gameplaySceneName
                    && HasSpawnPoint(scene))
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
            _testRoom.selectedLocation = spawnScene.IsValid() ? spawnScene.name : gameplaySceneName;
            _testRoom.isInGame = true;
            _testRoom.loadedSceneName = spawnScene.IsValid() ? spawnScene.name : string.Empty;
            _testRoom.handle = spawnScene.IsValid() ? spawnScene.handle : 0;
            _testRoom.sceneSlotIndex = ServerRoom.NoSceneSlot;
            _testRoom.sceneOffsetX = 0;
            UpsertTestPlayer(ownerConnection, stats);
            UpdateTestRoomMaxPlayers(_testRoom);

            return _testRoom;
        }

        private void UpsertTestPlayer(NetworkConnection ownerConnection, VehicleRuntimeStats stats)
        {
            if (_testRoom == null)
            {
                return;
            }

            LobbyPlayer player = _testRoom.GetPlayerByConnection(ownerConnection);
            if (player == null)
            {
                player = new LobbyPlayer();
                _testRoom.AddPlayer(player);
            }

            player.loginName = TestPlayerName;
            player.Connection = ownerConnection;
            player.userId = 0;
            player.mmr = 1000;
            player.activeVehicleId = stats != null ? stats.VehicleId : 0;
            player.activeVehicleCode = stats != null ? stats.Code : string.Empty;
            player.team = MatchTeam.TeamA;
            player.isBot = false;
            player.randomPlayerConnected = true;
            player.leftBattle = false;
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
                    username = TestPlayerName,
                    mmr = 1000
                };
            }

            profile.activeVehicleId = stats.VehicleId;
            profile.activeVehicleCode = stats.Code;
            profile.activeVehicleName = stats.Name;
            ServerPlayerSessions.SetProfile(ownerConnection, profile);
        }

        private void ClearTestPlayerVehicle()
        {
            if (_testRoom == null || _testRoom.players == null)
            {
                return;
            }

            for (int i = 0; i < _testRoom.players.Count; i++)
            {
                LobbyPlayer player = _testRoom.players[i];
                if (player != null && !player.isBot)
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
            _spawnedPlayerStats = null;
            ClearTestPlayerVehicle();
            SetTestCursorMode(true);
        }

        private void SetTestCursorMode(bool enabled)
        {
            _testCursorMode = enabled;
            if (!enabled)
            {
                _testPanelExpanded = false;
            }

            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;

            if (_spawnedVehicle != null && _spawnedVehicle.inputManager != null)
            {
                _spawnedVehicle.inputManager.SetControlsBlocked(enabled);
            }

            RefreshGameplayHudVisibilityForTest();
        }

        private void RefreshGameplayHudVisibilityForTest()
        {
            if (_spawnedGameplayHud == null)
            {
                return;
            }

            if (ShouldHideGameplayHudForTest())
            {
                HideGameplayHudForTest(_spawnedGameplayHud);
                _gameplayHudHiddenForTest = true;
                _gameplayHudOpenedForTest = false;
                return;
            }

            if (!_gameplayHudOpenedForTest || _gameplayHudHiddenForTest)
            {
                OpenGameplayHudForTest(_spawnedGameplayHud);
                _gameplayHudOpenedForTest = true;
            }

            _gameplayHudHiddenForTest = false;
        }

        private bool ShouldHideGameplayHudForTest()
        {
            return showTestGui && _testCursorMode && _testPanelExpanded;
        }

        private float GetUiScale()
        {
            float widthScale = Screen.width / ReferenceScreenWidth;
            float heightScale = Screen.height / ReferenceScreenHeight;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.72f, 1.25f);
        }

        private void EnsureGuiStyles()
        {
            if (_panelStyle != null && Mathf.Abs(_styleScale - _uiScale) < 0.01f)
            {
                return;
            }

            _styleScale = _uiScale;
            int bodyFontSize = Mathf.Max(11, Mathf.RoundToInt(14f * _uiScale));
            int smallFontSize = Mathf.Max(10, Mathf.RoundToInt(12f * _uiScale));
            int sectionFontSize = Mathf.Max(12, Mathf.RoundToInt(15f * _uiScale));
            int titleFontSize = Mathf.Max(15, Mathf.RoundToInt(22f * _uiScale));
            int panelPadding = Mathf.Max(8, Mathf.RoundToInt(14f * _uiScale));
            int cardPadding = Mathf.Max(6, Mathf.RoundToInt(9f * _uiScale));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(panelPadding, panelPadding, panelPadding, panelPadding)
            };

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(cardPadding, cardPadding, cardPadding, cardPadding),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = titleFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = smallFontSize,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _sectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = sectionFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _vehicleRowStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFontSize,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(cardPadding, cardPadding, cardPadding / 2, cardPadding / 2)
            };

            _selectedVehicleRowStyle = new GUIStyle(_vehicleRowStyle)
            {
                fontStyle = FontStyle.Bold
            };
            _selectedVehicleRowStyle.normal.background = GUI.skin.button.active.background;
            _selectedVehicleRowStyle.normal.textColor = new Color(0.88f, 0.96f, 1f);

            _primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = bodyFontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _secondaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = smallFontSize,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _detailLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = bodyFontSize,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _detailValueStyle = new GUIStyle(_detailLabelStyle)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = smallFontSize,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
        }

        private Rect GetExpandedTestPanelRect()
        {
            float padding = PanelScreenPadding * _uiScale;
            float availableWidth = Mathf.Max(1f, Screen.width - padding * 2f);
            float availableHeight = Mathf.Max(1f, Screen.height - padding * 2f);
            float width = Mathf.Min(ExpandedPanelMaxWidth * _uiScale, availableWidth);
            float height = Mathf.Min(ExpandedPanelMaxHeight * _uiScale, availableHeight);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            return new Rect(x, y, width, height);
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

            RefreshTestCameraSync();
        }

        private CameraSync RefreshTestCameraSync()
        {
            if (testCamera == null)
            {
                return null;
            }

            CameraSync cameraSync = testCamera.GetComponent<CameraSync>();
            if (cameraSync == null)
            {
                return null;
            }

            CameraSync.In = cameraSync;
            cameraSync.gameplayCamera = testCamera;
            ConfigureTestCameraAsPrimary(cameraSync);
            return cameraSync;
        }

        private void ConfigureTestCameraAsPrimary(CameraSync testCameraSync)
        {
            if (testCamera == null)
            {
                return;
            }

            testCamera.gameObject.SetActive(true);
            testCamera.enabled = true;
            testCamera.depth = 100f;

            CameraSync[] cameraSyncs = FindObjectsByType<CameraSync>(FindObjectsSortMode.None);
            for (int i = 0; i < cameraSyncs.Length; i++)
            {
                CameraSync candidate = cameraSyncs[i];
                if (candidate == null || candidate == testCameraSync)
                {
                    continue;
                }

                if (candidate.gameplayCamera != null && candidate.gameplayCamera != testCamera)
                {
                    candidate.gameplayCamera.enabled = false;
                }

                candidate.enabled = false;
            }
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
            RefreshGameplayHudVisibilityForTest();

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

        private static void OpenGameplayHudForTest(GameObject hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            hudRoot.SetActive(true);

            Menu hudMenu = hudRoot.GetComponent<Menu>();
            if (hudMenu != null)
            {
                if (MenuManager.IsReady && MenuManager.RegisterMenu(MenuType.GameplayHUD, hudMenu))
                {
                    MenuManager.OpenMenu(MenuType.GameplayHUD);
                }
                else
                {
                    hudMenu.Open();
                }

                return;
            }

            CanvasGroup canvasGroup = hudRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private static void HideGameplayHudForTest(GameObject hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            Menu hudMenu = hudRoot.GetComponent<Menu>();
            if (hudMenu != null)
            {
                hudMenu.CloseImmediate();
                return;
            }

            CanvasGroup canvasGroup = hudRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                return;
            }

            hudRoot.SetActive(false);
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
            ConfigureLocalTestTickRate();

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
            if (!CanStopNetworkManager())
            {
                _startedTestClient = false;
                _startedTestServer = false;
                return;
            }

            if (_startedTestClient && networkManager.ClientManager != null && networkManager.IsClientStarted)
            {
                networkManager.ClientManager.StopConnection();
            }

            if (_startedTestServer && networkManager.ServerManager != null && networkManager.IsServerStarted)
            {
                networkManager.ServerManager.StopConnection(true);
            }

            _startedTestClient = false;
            _startedTestServer = false;
        }

        private bool CanStopNetworkManager()
        {
            return networkManager != null
                   && networkManager.gameObject != null
                   && networkManager.gameObject.activeInHierarchy
                   && networkManager.enabled;
        }

        private void ConfigureLocalTestTickRate()
        {
            if (networkManager == null || networkManager.TimeManager == null)
            {
                return;
            }

            ushort tickRate = localTestTickRate > 0 ? localTestTickRate : (ushort)60;
            networkManager.TimeManager.SetTickRate(tickRate);
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

            return IsConnectionReadyForSceneLoad(connection);
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
            return EnsureConnectionInScene(connection, gameObject.scene);
        }

        private bool EnsureConnectionInScene(NetworkConnection connection, Scene scene)
        {
            if (!IsConnectionReady(connection) || networkManager == null || networkManager.SceneManager == null)
            {
                return false;
            }

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

        private static bool IsConnectionReady(NetworkConnection connection)
        {
            return connection != null && connection.IsActive;
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
