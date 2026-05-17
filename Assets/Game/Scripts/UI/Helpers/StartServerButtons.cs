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
        private const string UnityServerStatusEndpoint = "/unity-server/status";

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
        [SerializeField] private bool requestClientServerAddressFromApi = true;
        [SerializeField] private int serverLookupTimeoutSeconds = 5;
        [SerializeField] private string advertisedServerAddress;

        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private bool _clientInfoRegistered;
        private bool _clientStartRequested;
        private bool _isStoppingConnections;
        private Coroutine _clientConnectCoroutine;
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
                if (_clientState == LocalConnectionState.Stopped && _clientConnectCoroutine == null && _clientStartRequested == false)
                {
                    MenuLoadingScreenManager.SetConnectionStatus("Connection attempt " + (attempts + 1) + " / " + maxAttempts);
                    OnConnectClicked();
                    attempts++;
                }

                yield return retryDelay;
            }

            if (_clientState != LocalConnectionState.Started)
            {
                MenuLoadingScreenManager.SetConnectionStatus("Connection failed. Battle server is not responding");
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

            if (_clientConnectCoroutine != null)
            {
                return;
            }

            if (_clientStartRequested)
            {
                return;
            }

            _clientConnectCoroutine = StartCoroutine(StartClientConnection());
        }

        private IEnumerator StartClientConnection()
        {
            if (connect != null)
            {
                connect.interactable = false;
            }

            _clientStartRequested = true;
            MenuLoadingScreenManager.ShowConnectionLoading("Preparing multiplayer connection");

            bool started;
            if (requestClientServerAddressFromApi)
            {
                ServerConnectionInfo connectionInfo = default;
                bool hasConnectionInfo = false;
                string lookupError = string.Empty;

                MenuLoadingScreenManager.SetConnectionStatus("API: requesting multiplayer server address");
                yield return RequestActiveServerConnectionInfo((isSuccess, result, message) =>
                {
                    hasConnectionInfo = isSuccess;
                    connectionInfo = result;
                    lookupError = message;
                });

                if (hasConnectionInfo == false)
                {
                    FailClientStart("Connection failed. " + lookupError);
                    _clientConnectCoroutine = null;
                    yield break;
                }

                MenuLoadingScreenManager.SetConnectionStatus("API: server address received " + connectionInfo.Address + ":" + connectionInfo.Port);
                MenuLoadingScreenManager.SetConnectionStatus("Dedicated server: connecting to " + connectionInfo.Address + ":" + connectionInfo.Port);
                started = networkManager.ClientManager.StartConnection(connectionInfo.Address, connectionInfo.Port);
            }
            else
            {
                MenuLoadingScreenManager.SetConnectionStatus("Dedicated server: connecting");
                started = networkManager.ClientManager.StartConnection();
            }

            if (started == false)
            {
                FailClientStart("Connection failed. Client could not start");
            }

            _clientConnectCoroutine = null;
        }

        private IEnumerator RequestActiveServerConnectionInfo(System.Action<bool, ServerConnectionInfo, string> onComplete)
        {
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastError = "Server API is not available";

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + UnityServerStatusEndpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.certificateHandler = new AcceptAllCertificates();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);

                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        lastError = FormatApiRequestError(apiBase, request);
                        continue;
                    }

                    string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        lastError = "Server API returned an empty response from " + apiBase;
                        continue;
                    }

                    if (TryReadServerConnectionInfo(responseText, out ServerConnectionInfo connectionInfo, out string error) == false)
                    {
                        lastError = error;
                        continue;
                    }

                    HttpLink.SetResolvedBase(apiBase);
                    onComplete(true, connectionInfo, string.Empty);
                    yield break;
                }
            }

            onComplete(false, default, lastError);
        }

        private bool TryReadServerConnectionInfo(string responseText, out ServerConnectionInfo connectionInfo, out string error)
        {
            UnityServerStatusResponse response;
            try
            {
                response = JsonUtility.FromJson<UnityServerStatusResponse>(responseText);
            }
            catch (System.ArgumentException)
            {
                connectionInfo = default;
                error = "Server API returned invalid JSON";
                return false;
            }

            ushort fallbackPort = GetConfiguredClientPort();
            return TryGetServerConnectionInfo(response, fallbackPort, out connectionInfo, out error);
        }

        private int GetApiRequestTimeoutSeconds(string apiBase)
        {
            int timeoutSeconds = Mathf.Max(1, serverLookupTimeoutSeconds);
            if (HttpLink.IsLocalBase(apiBase))
            {
                return Mathf.Min(timeoutSeconds, 2);
            }

            return timeoutSeconds;
        }

        private static string FormatApiRequestError(string apiBase, UnityWebRequest request)
        {
            return "Server API is not available at " + apiBase + ": " + request.responseCode + " " + request.error;
        }

        private static bool TryGetServerConnectionInfo(UnityServerStatusResponse response, ushort fallbackPort, out ServerConnectionInfo connectionInfo, out string error)
        {
            connectionInfo = default;
            error = string.Empty;

            if (response == null)
            {
                error = "Server API returned invalid server status";
                return false;
            }

            if (response.isOnline == false)
            {
                error = "Battle server is offline";
                return false;
            }

            string address = GetResponseAddress(response);
            if (string.IsNullOrWhiteSpace(address))
            {
                error = "Server API did not return a battle server address";
                return false;
            }

            int port = GetResponsePort(response);
            if (port <= 0)
            {
                port = fallbackPort;
            }

            if (port <= 0 || port > ushort.MaxValue)
            {
                error = "Server API returned an invalid battle server port";
                return false;
            }

            connectionInfo = new ServerConnectionInfo
            {
                Address = address.Trim(),
                Port = (ushort)port
            };

            return true;
        }

        private static string GetResponseAddress(UnityServerStatusResponse response)
        {
            string address = string.Empty;
            if (string.IsNullOrWhiteSpace(response.address) == false)
            {
                address = response.address;
            }
            else if (string.IsNullOrWhiteSpace(response.publicIp) == false)
            {
                address = response.publicIp;
            }
            else if (string.IsNullOrWhiteSpace(response.ipAddress) == false)
            {
                address = response.ipAddress;
            }

            return NormalizeConnectionAddress(address);
        }

        private static string NormalizeConnectionAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return string.Empty;
            }

            string value = address.Trim();
            if (HttpLink.IsLocal)
            {
                if (string.Equals(value, "localhost", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "::1", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "0:0:0:0:0:0:0:1", System.StringComparison.OrdinalIgnoreCase))
                {
                    return "127.0.0.1";
                }

                if (IPAddress.TryParse(value, out IPAddress ipAddress))
                {
                    if (IPAddress.IsLoopback(ipAddress))
                    {
                        return "127.0.0.1";
                    }
                }
            }

            return value;
        }

        private static int GetResponsePort(UnityServerStatusResponse response)
        {
            if (response.port > 0)
            {
                return response.port;
            }

            if (response.gamePort > 0)
            {
                return response.gamePort;
            }

            if (response.serverPort > 0)
            {
                return response.serverPort;
            }

            return 0;
        }

        private ushort GetConfiguredClientPort()
        {
            if (networkManager == null || networkManager.TransportManager == null || networkManager.TransportManager.Transport == null)
            {
                return 0;
            }

            return networkManager.TransportManager.Transport.GetPort();
        }

        private void FailClientStart(string statusText)
        {
            _clientStartRequested = false;
            MenuLoadingScreenManager.SetConnectionStatus(statusText);
            MenuLoadingScreenManager.HideConnectionLoading();

            if (connect != null)
            {
                connect.interactable = true;
            }

            RefreshControls();
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
                _clientStartRequested = false;
                RegisterClientInfo();
                MenuLoadingScreenManager.SetConnectionStatus("Dedicated server: connected");
                MenuLoadingScreenManager.HideConnectionLoading();
            }
            else if (_clientState == LocalConnectionState.Stopped)
            {
                if (connect != null)
                {
                    connect.interactable = true;
                }

                if (autoStartMode == NetworkAutoStartMode.Client && _isStoppingConnections == false)
                {
                    _clientStartRequested = false;
                    MenuLoadingScreenManager.ShowConnectionLoading("Connection failed. Retrying");
                }
                else
                {
                    bool failedConnect = _clientStartRequested && !_isStoppingConnections;
                    _clientStartRequested = false;
                    MenuLoadingScreenManager.SetConnectionStatus(failedConnect
                        ? "Connection failed. Battle server is not responding"
                        : "Connection stopped");
                    MenuLoadingScreenManager.HideConnectionLoading();
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
                address = GetAdvertisedServerAddress(),
                port = GetServerPort(),
                message = "Unity server running"
            };

            string json = JsonUtility.ToJson(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastError = string.Empty;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + UnityServerStatusEndpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.certificateHandler = new AcceptAllCertificates();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);
                    request.SetRequestHeader("Content-Type", "application/json");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        HttpLink.SetResolvedBase(apiBase);
                        yield break;
                    }

                    lastError = FormatApiRequestError(apiBase, request);
                }
            }

            Debug.LogWarning("Unity server heartbeat failed: " + lastError);
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

        private string GetAdvertisedServerAddress()
        {
            if (string.IsNullOrWhiteSpace(advertisedServerAddress))
            {
                if (HttpLink.IsLocal)
                {
                    return "127.0.0.1";
                }

                return string.Empty;
            }

            return NormalizeConnectionAddress(advertisedServerAddress);
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
            public string address;
            public int port;
            public string message;
        }

        [System.Serializable]
        private class UnityServerStatusResponse
        {
            public bool isOnline;
            public string status;
            public string address;
            public string publicIp;
            public string ipAddress;
            public int port;
            public int gamePort;
            public int serverPort;
            public string message;
        }

        private struct ServerConnectionInfo
        {
            public string Address;
            public ushort Port;
        }
    }
}
