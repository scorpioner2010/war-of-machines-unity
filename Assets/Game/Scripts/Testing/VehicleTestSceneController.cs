using System.Net;
using System.Net.Sockets;
using System.Text;
using Cysharp.Threading.Tasks;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public bool autoStartHost = true;
        public ushort localTestPort = 7780;
        public bool autoSelectAvailablePort = true;
        public int maxPortSearchAttempts = 20;
        public bool alignVisualBoundsToGround = true;
        public LayerMask groundMask = ~0;
        public float groundRayStartHeight = 25f;
        public float groundRayDistance = 80f;
        public float groundClearance = 0.02f;

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

        private void Awake()
        {
            ResolveSceneReferences();
            ResolveNetworkManager();
        }

        private void Start()
        {
            if (autoStartHost)
            {
                StartHostAsync().Forget();
            }

            LoadVehiclesAsync().Forget();
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

        private void OnGUI()
        {
            Rect area = new Rect(12f, 12f, 390f, Screen.height - 24f);
            GUILayout.BeginArea(area, GUI.skin.box);

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

            VehicleRuntimeStats selected = GetSelected();
            VehicleRoot prefab = GetSelectedPrefab(selected);
            GUI.enabled = !_loading && IsNetworkReady() && selected != null && prefab != null;
            if (GUILayout.Button("Spawn selected robot", GUILayout.Height(34f)))
            {
                SpawnSelected();
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

        private async UniTaskVoid LoadVehiclesAsync()
        {
            if (_loading)
            {
                return;
            }

            _loading = true;
            _status = "Loading vehicles from API...";

            VehicleRuntimeStats[] result = await VehicleStatsProvider.GetAllAsync();
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

        private string BuildStatsText(VehicleRuntimeStats stats)
        {
            _builder.Length = 0;
            _builder.Append("Name: ").Append(stats.Name).Append('\n');
            _builder.Append("Code: ").Append(stats.Code).Append('\n');
            _builder.Append("Level: ").Append(stats.Level).Append('\n');
            _builder.Append("HP: ").Append(stats.MaxHealth).Append('\n');
            _builder.Append("Damage: ").Append(stats.Damage).Append('\n');
            _builder.Append("Penetration: ").Append(stats.Penetration).Append('\n');
            _builder.Append("Reload: ").Append(stats.ReloadTime).Append(" s\n");
            _builder.Append("Accuracy: ").Append(stats.Accuracy).Append('\n');
            _builder.Append("Aim time: ").Append(stats.AimTime).Append(" s\n");
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

        private void SpawnSelected()
        {
            if (!IsNetworkReady())
            {
                _status = "Network is not ready yet.";
                if (autoStartHost)
                {
                    StartHostAsync().Forget();
                }
                return;
            }

            VehicleRuntimeStats stats = GetSelected();
            VehicleRoot prefab = GetSelectedPrefab(stats);
            if (stats == null || prefab == null)
            {
                _status = "Cannot spawn selected vehicle: prefab missing.";
                return;
            }

            DespawnCurrent();

            Vector3 position = GetSpawnPosition();
            Quaternion rotation = GetSpawnRotation();
            bool hasGround = TryGetGroundHeight(position, out float groundY);

            _spawnedVehicle = Instantiate(prefab, position, rotation);
            _spawnedVehicle.gameObject.SetActive(true);

            if (alignVisualBoundsToGround && hasGround)
            {
                AlignVisualBoundsToGround(_spawnedVehicle, groundY);
            }

            _spawnedVehicle.ServerApplyRuntimeStats(stats, syncObservers: false);

            NetworkObject networkObject = _spawnedVehicle.networkObject != null
                ? _spawnedVehicle.networkObject
                : _spawnedVehicle.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Destroy(_spawnedVehicle.gameObject);
                _spawnedVehicle = null;
                _status = "Cannot spawn selected vehicle: NetworkObject missing.";
                return;
            }

            networkManager.ServerManager.Spawn(networkObject, networkManager.ClientManager.Connection, gameObject.scene);
            _spawnedVehicle.ServerApplyRuntimeStats(stats, syncObservers: true);

            if (_spawnedVehicle.characterInit != null)
            {
                _spawnedVehicle.characterInit.ServerInit(1, PlayerType.Player, "VehicleTest", MatchTeam.None, gameObject.scene);
            }

            _status = "Spawned " + stats.Name + ".";
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

            _startedNetwork = IsNetworkReady();
            _networkStartInProgress = false;

            if (_startedNetwork)
            {
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
                   && _clientState == LocalConnectionState.Started;
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

        private bool TryGetGroundHeight(Vector3 position, out float groundY)
        {
            Vector3 rayStart = position + Vector3.up * Mathf.Max(0f, groundRayStartHeight);
            float rayDistance = Mathf.Max(0.01f, groundRayStartHeight + groundRayDistance);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
                return true;
            }

            groundY = position.y;
            return false;
        }

        private void AlignVisualBoundsToGround(VehicleRoot vehicle, float groundY)
        {
            if (vehicle == null)
            {
                return;
            }

            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            float targetMinY = groundY + Mathf.Max(0f, groundClearance);
            float deltaY = targetMinY - bounds.min.y;

            CharacterController controller = vehicle.objectMover != null ? vehicle.objectMover.controller : null;
            if (controller != null)
            {
                Vector3 controllerCenter = controller.transform.TransformPoint(controller.center);
                float halfHeight = controller.height * Mathf.Abs(controller.transform.lossyScale.y) * 0.5f;
                float controllerBottomY = controllerCenter.y - halfHeight;
                float controllerDeltaY = targetMinY - controllerBottomY;
                if (controllerDeltaY > deltaY)
                {
                    deltaY = controllerDeltaY;
                }
            }

            if (Mathf.Abs(deltaY) <= 0.001f)
            {
                return;
            }

            Transform vehicleTransform = vehicle.transform;
            vehicleTransform.position = new Vector3(
                vehicleTransform.position.x,
                vehicleTransform.position.y + deltaY,
                vehicleTransform.position.z
            );
        }
    }
}
