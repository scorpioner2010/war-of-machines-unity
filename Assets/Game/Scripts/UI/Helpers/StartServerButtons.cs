using FishNet.Managing;
using FishNet.Transporting;
using Game.Scripts.API;
using Game.Scripts.API.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using Game.Scripts.Server;
using Game.Scripts.UI.Loading;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Game.Scripts.UI.Helpers
{
    public enum NetworkAutoStartMode
    {
        None,
        Client,
        Server,
    }

    public class StartServerButtons : MonoBehaviour
    {
        public static string LastServerStatus { get; private set; } = "Server start not requested.";

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private Button connect;
        [SerializeField] private Button server;
        [SerializeField] private GameObject panel;
        [SerializeField] private NetworkAutoStartMode autoStartMode = NetworkAutoStartMode.None;
        [SerializeField] private bool hideControlsWhenAutoStarting = true;
        [SerializeField] private float autoStartDelaySeconds = 0.25f;
        [SerializeField] private float clientRetryDelaySeconds = 1f;
        [SerializeField] private int clientMaxStartAttempts = 30;
        [SerializeField] private float serverHeartbeatIntervalSeconds = 2f;

        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private bool _clientInfoRegistered;
        private bool _isStoppingConnections;
        private Coroutine _serverHeartbeatCoroutine;

        private void Awake()
        {
            if (connect != null && connect.transform.parent != null)
            {
                connect.transform.parent.gameObject.SetActive(true);
            }

            if (networkManager != null)
            {
                networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
                networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            }

            if (connect != null)
            {
                connect.onClick.AddListener(OnConnectClicked);
            }

            if (server != null)
            {
                server.onClick.AddListener(OnServerClicked);
            }

            RefreshControls();

            if (autoStartMode != NetworkAutoStartMode.None)
            {
                StartCoroutine(AutoStart());
            }
        }

        private void OnDestroy()
        {
            StopServerHeartbeat();
            StopNetworkConnections("destroy");

            if (connect != null)
            {
                connect.onClick.RemoveListener(OnConnectClicked);
            }

            if (server != null)
            {
                server.onClick.RemoveListener(OnServerClicked);
            }

            if (networkManager != null)
            {
                networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            }
        }

        private void OnApplicationQuit()
        {
            StopServerHeartbeat();
            StopNetworkConnections("application quit");
        }

        private IEnumerator AutoStart()
        {
            yield return null;

            if (autoStartDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(autoStartDelaySeconds);
            }

            if (networkManager == null)
            {
                Debug.LogWarning("Cannot auto-start networking because NetworkManager is not assigned.");
                yield break;
            }

            if (autoStartMode == NetworkAutoStartMode.Client)
            {
                yield return AutoStartClient();
            }
            else if (autoStartMode == NetworkAutoStartMode.Server)
            {
                OnServerClicked();
            }
        }

        private IEnumerator AutoStartClient()
        {
            int attempts = 0;
            int maxAttempts = Mathf.Max(1, clientMaxStartAttempts);
            WaitForSeconds retryDelay = new WaitForSeconds(Mathf.Max(0.1f, clientRetryDelaySeconds));

            RegisterClientInfo();

            while (_clientState != LocalConnectionState.Started && attempts < maxAttempts)
            {
                if (_clientState == LocalConnectionState.Stopped)
                {
                    OnConnectClicked();
                    attempts++;
                }

                yield return retryDelay;
            }
        }

        private void OnConnectClicked()
        {
            if (networkManager == null)
            {
                return;
            }

            if (_clientState != LocalConnectionState.Stopped)
            {
                return;
            }

            if (connect != null)
            {
                connect.interactable = false;
            }

            LoadingScreenManager.ShowConnectionLoading();

            if (networkManager.ClientManager.StartConnection() == false)
            {
                if (connect != null)
                {
                    connect.interactable = true;
                }
            }
        }

        private void OnServerClicked()
        {
            if (networkManager == null)
            {
                LastServerStatus = "Cannot start server: NetworkManager is not assigned.";
                return;
            }

            if (_serverState != LocalConnectionState.Stopped)
            {
                LastServerStatus = "Server start skipped: current state is " + _serverState + ".";
                return;
            }

            if (server != null)
            {
                server.interactable = false;
            }

            if (IsServerPortAvailable() == false)
            {
                LastServerStatus = "Cannot start server: UDP port " + GetServerPort() + " is already in use.";
                Debug.LogWarning(LastServerStatus + " Use Connect in this window, or stop the other server first.");
                if (server != null)
                {
                    server.interactable = true;
                }
                return;
            }

            if (networkManager.ServerManager.StartConnection() == false)
            {
                LastServerStatus = "ServerManager.StartConnection returned false.";
                if (server != null)
                {
                    server.interactable = true;
                }

                return;
            }

            LastServerStatus = "Server start requested.";
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientState = args.ConnectionState;

            if (_clientState == LocalConnectionState.Started)
            {
                RegisterClientInfo();
            }
            else if (_clientState == LocalConnectionState.Stopped)
            {
                if (connect != null)
                {
                    connect.interactable = true;
                }

                if (autoStartMode == NetworkAutoStartMode.Client)
                {
                    LoadingScreenManager.ShowConnectionLoading();
                }
            }

            RefreshControls();
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverState = args.ConnectionState;
            LastServerStatus = "Server state: " + _serverState + ".";

            if (_serverState == LocalConnectionState.Started)
            {
                LastServerStatus = "Server started.";
                StartServerHeartbeat();
                if (server != null)
                {
                    server.interactable = false;
                }
            }
            else if (_serverState == LocalConnectionState.Stopped)
            {
                LastServerStatus = "Server stopped.";
                StopServerHeartbeat();
                if (server != null)
                {
                    server.interactable = true;
                }
            }

            RefreshControls();
        }

        private void StartServerHeartbeat()
        {
            if (_serverHeartbeatCoroutine != null)
            {
                return;
            }

            _serverHeartbeatCoroutine = StartCoroutine(ServerHeartbeatLoop());
        }

        private void StopServerHeartbeat()
        {
            if (_serverHeartbeatCoroutine == null)
            {
                return;
            }

            StopCoroutine(_serverHeartbeatCoroutine);
            _serverHeartbeatCoroutine = null;
        }

        private IEnumerator ServerHeartbeatLoop()
        {
            float interval = Mathf.Max(0.1f, serverHeartbeatIntervalSeconds);
            WaitForSeconds wait = new WaitForSeconds(interval);

            while (networkManager != null && networkManager.IsServerStarted)
            {
                yield return SendServerHeartbeat();
                yield return wait;
            }

            _serverHeartbeatCoroutine = null;
        }

        private IEnumerator SendServerHeartbeat()
        {
            UnityServerStatusPayload payload = new UnityServerStatusPayload
            {
                status = "ready",
                playersOnline = GetPlayersOnline(),
                maxPlayers = GetMaxPlayers(),
                activeMatches = GetActiveMatches(),
                message = "Unity server running"
            };

            string json = JsonUtility.ToJson(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            string url = HttpLink.APIBase + "/unity-server/status";

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.certificateHandler = new AcceptAllCertificates();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Unity server heartbeat failed: " + request.responseCode + " " + request.error);
                }
            }
        }

        private int GetPlayersOnline()
        {
            if (networkManager == null || networkManager.ServerManager == null)
            {
                return 0;
            }

            return networkManager.ServerManager.Clients.Count;
        }

        private static int GetMaxPlayers()
        {
            int maxPlayers = 0;

            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room != null && room.maxPlayers > 0)
                {
                    maxPlayers += room.maxPlayers;
                }
            }

            if (maxPlayers > 0)
            {
                return maxPlayers;
            }

            return ServerSettings.GetMaxPlayersForFindRoom();
        }

        private static int GetActiveMatches()
        {
            int activeMatches = 0;

            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room != null && room.IsActiveMatch)
                {
                    activeMatches++;
                }
            }

            return activeMatches;
        }

        private void RegisterClientInfo()
        {
            if (_clientInfoRegistered)
            {
                return;
            }

            if (ServiceLocator.IsRegistered<IPlayerClientInfo>() == false)
            {
                ServiceLocator.TryRegister<IPlayerClientInfo>(new PlayerClientInfo());
            }

            _clientInfoRegistered = true;
        }

        private void RefreshControls()
        {
            if (autoStartMode != NetworkAutoStartMode.None && hideControlsWhenAutoStarting)
            {
                SetControlsActive(false, false, false);
                return;
            }

            bool roleSelected = _clientState == LocalConnectionState.Started || _serverState == LocalConnectionState.Started;
            bool showConnect = roleSelected == false;
            bool showServer = roleSelected == false;

            SetControlsActive(showConnect, showServer, showConnect || showServer);
        }

        private void SetControlsActive(bool showConnect, bool showServer, bool showPanel)
        {
            if (connect != null)
            {
                connect.gameObject.SetActive(showConnect);
            }

            if (server != null)
            {
                server.gameObject.SetActive(showServer);
            }

            if (panel != null)
            {
                panel.gameObject.SetActive(showPanel);
            }
        }

        private bool IsServerPortAvailable()
        {
            ushort port = GetServerPort();

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

        private ushort GetServerPort()
        {
            return networkManager.TransportManager.Transport.GetPort();
        }

        private void StopNetworkConnections(string reason)
        {
            if (_isStoppingConnections || networkManager == null)
            {
                return;
            }

            _isStoppingConnections = true;

            if (networkManager.IsClientStarted)
            {
                networkManager.ClientManager.StopConnection();
            }

            if (networkManager.IsServerStarted)
            {
                networkManager.ServerManager.StopConnection(true);
            }

            LastServerStatus = "Network stopped on " + reason + ".";
        }

        [System.Serializable]
        private class UnityServerStatusPayload
        {
            public string status;
            public int playersOnline;
            public int maxPlayers;
            public int activeMatches;
            public string message;
        }
    }
}
