using FishNet.Managing;
using FishNet.Transporting;
using RAV.Scripts.API;
using RAV.Scripts.API.Endpoints;
using RAV.Scripts.Core.Services;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Networking.Lobby;
using RAV.Scripts.Player.Data;
using RAV.Scripts.Server;
using RAV.Scripts.Testing;
using RAV.Scripts.UI.Loading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RAV.Scripts.UI.Helpers
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
        private const string UnityServerRegisterEndpoint = "/unity-server/register";
        private const string UnityServerHeartbeatEndpoint = "/unity-server/{0}/heartbeat";
        private const string UnityServerUnregisterEndpoint = "/unity-server/{0}/unregister";
        private const string ServerIdArgument = "-serverId";
        private const string ServerAddressArgument = "-serverAddress";
        private const string AdvertiseAddressArgument = "-advertiseAddress";
        private const string ServerPortArgument = "-serverPort";
        private const string GamePortArgument = "-gamePort";
        private const string PortArgument = "-port";

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
        [SerializeField] private bool applyRuntimeServerConfig = true;
        [SerializeField] private ClientMenuTestMode menuTestMode;

        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private bool _clientInfoRegistered;
        private bool _clientStartRequested;
        private bool _isStoppingConnections;
        private Coroutine _clientConnectCoroutine;
        private Coroutine _serverHeartbeatCoroutine;
        private bool _serverRegistered;

        private void Awake()
        {
            if (IsOfflineMenuTestEnabled() || IsEditorVehicleTestModeEnabled())
            {
                LastServerStatus = IsOfflineMenuTestEnabled()
                    ? "Offline menu test mode is active. Networking was not started."
                    : "Client vehicle test mode is active. Normal networking startup was skipped.";
                return;
            }

            ApplyRuntimeServerOverrides();

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
            if (IsOfflineMenuTestEnabled() || IsEditorVehicleTestModeEnabled())
            {
                return;
            }

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
            if (IsOfflineMenuTestEnabled() || IsEditorVehicleTestModeEnabled())
            {
                return;
            }

            StopServerHeartbeat();
            StopNetworkConnections("application quit");
        }

        private IEnumerator AutoStart()
        {
            yield return null;

            if (IsEditorVehicleTestModeEnabled())
            {
                yield break;
            }

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
            WaitForSeconds authenticationWait = new WaitForSeconds(Mathf.Max(0.1f, clientRetryDelaySeconds));
            while (ClientAuthenticationState.HasAuthenticationSession == false)
            {
                MenuLoadingScreenManager.SetConnectionStatus("Login required before multiplayer connection");
                yield return authenticationWait;
            }

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
            if (IsOfflineMenuTestEnabled() || IsEditorVehicleTestModeEnabled())
            {
                return;
            }

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

            if (ClientAuthenticationState.HasAuthenticationSession == false)
            {
                LastServerStatus = "Client connection requires an authenticated HTTPS session.";
                Popup.ShowText("Log in before connecting to the battle server.", Color.red);
                return;
            }

            _clientConnectCoroutine = StartCoroutine(StartClientConnection());
        }

        private IEnumerator StartClientConnection()
        {
            GameplayRuntimeDiagnostics.Record(
                "client_connection_start_requested",
                "api_discovery", requestClientServerAddressFromApi,
                "auto_start_mode", autoStartMode);
            if (connect != null)
            {
                connect.interactable = false;
            }

            _clientStartRequested = true;
            ClientAuthenticationState.ClearConnectionTicket();
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
                    GameplayRuntimeDiagnostics.Record("client_connection_failed", "reason", lookupError);
                    FailClientStart("Connection failed. " + lookupError);
                    _clientConnectCoroutine = null;
                    yield break;
                }

                MenuLoadingScreenManager.SetConnectionStatus("API: server address received " + connectionInfo.Address + ":" + connectionInfo.Port);
                bool ticketIssued = false;
                string ticketError = string.Empty;
                yield return RequestConnectionTicket(connectionInfo.ServerId, (success, message) =>
                {
                    ticketIssued = success;
                    ticketError = message;
                });

                if (ticketIssued == false)
                {
                    GameplayRuntimeDiagnostics.Record("client_connection_failed", "reason", "ticket_request", "message", ticketError);
                    FailClientStart("Connection ticket request failed. " + ticketError);
                    _clientConnectCoroutine = null;
                    yield break;
                }

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
                GameplayRuntimeDiagnostics.Record("client_connection_failed", "reason", "fishnet_start_returned_false");
                FailClientStart("Connection failed. Client could not start");
            }
            else
            {
                GameplayRuntimeDiagnostics.Record("client_connection_start_called", "started", true);
            }

            _clientConnectCoroutine = null;
        }

        private IEnumerator RequestConnectionTicket(string serverId, System.Action<bool, string> onComplete)
        {
            if (string.IsNullOrWhiteSpace(serverId) || ClientAuthenticationState.HasAuthenticationSession == false)
            {
                onComplete(false, "Authenticated session or server ID is missing.");
                yield break;
            }

            bool accessTokenReady = false;
            string accessTokenError = string.Empty;
            yield return ClientAuthenticationState.EnsureValidAccessToken((success, message) =>
            {
                accessTokenReady = success;
                accessTokenError = message;
            });
            if (!accessTokenReady)
            {
                onComplete(false, accessTokenError);
                yield break;
            }

            var payload = new ConnectionTicketRequest { serverId = serverId };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            string lastError = "No valid API endpoint is configured for the selected environment.";
            string[] apiBases = HttpLink.GetBaseCandidates();
            bool retriedAfterUnauthorized = false;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                using (UnityWebRequest request = new UnityWebRequest(apiBase + "/connection-tickets", UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + ClientAuthenticationState.AccessToken);

                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        ConnectionTicketResponse response = JsonUtility.FromJson<ConnectionTicketResponse>(request.downloadHandler.text);
                        if (response == null || string.IsNullOrWhiteSpace(response.ticket))
                        {
                            lastError = "Backend returned an invalid connection ticket.";
                            continue;
                        }

                        ClientAuthenticationState.SetConnectionTicket(response.serverId, response.ticket);
                        HttpLink.SetResolvedBase(apiBase);
                        onComplete(true, string.Empty);
                        yield break;
                    }

                    lastError = FormatApiRequestError(apiBase, request);
                    if (request.responseCode == 401 && !retriedAfterUnauthorized)
                    {
                        retriedAfterUnauthorized = true;
                        accessTokenReady = false;
                        accessTokenError = string.Empty;
                        yield return ClientAuthenticationState.EnsureValidAccessToken((success, message) =>
                        {
                            accessTokenReady = success;
                            accessTokenError = message;
                        }, forceRefresh: true);

                        if (!accessTokenReady)
                        {
                            lastError = accessTokenError;
                            break;
                        }

                        i--;
                    }
                }
            }

            onComplete(false, lastError);
        }

        private IEnumerator RequestActiveServerConnectionInfo(System.Action<bool, ServerConnectionInfo, string> onComplete)
        {
            string[] apiBases = GetServerLookupApiBases();
            string lastError = "No valid API endpoint is configured for the selected environment.";

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + UnityServerStatusEndpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
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

                    if (TryReadServerConnectionInfo(responseText, apiBase, out ServerConnectionInfo connectionInfo, out string error) == false)
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

        private static string[] GetServerLookupApiBases()
        {
            return HttpLink.GetBaseCandidates();
        }

        private static void AddUniqueApiBase(List<string> apiBases, string apiBase)
        {
            string normalizedBase = HttpLink.NormalizeBase(apiBase);
            if (string.IsNullOrWhiteSpace(normalizedBase))
            {
                return;
            }

            for (int i = 0; i < apiBases.Count; i++)
            {
                if (string.Equals(apiBases[i], normalizedBase, System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            apiBases.Add(normalizedBase);
        }

        private bool TryReadServerConnectionInfo(string responseText, string apiBase, out ServerConnectionInfo connectionInfo, out string error)
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
            if (response.servers != null && response.servers.Length > 0)
            {
                bool expectsLocalServer = HttpLink.IsLocalBase(apiBase);
                for (int i = 0; i < response.servers.Length; i++)
                {
                    UnityServerStatusItem server = response.servers[i];
                    if (server == null
                        || server.isOnline == false
                        || IsLoopbackServerAddress(server.address) != expectsLocalServer)
                    {
                        continue;
                    }

                    return TryGetServerConnectionInfo(server, fallbackPort, out connectionInfo, out error);
                }

                connectionInfo = default;
                error = expectsLocalServer
                    ? "No online Local Dedicated Server is registered. Start the local server and refresh its heartbeat."
                    : "No online Netcup Dedicated Server is registered. Check its deployment and heartbeat.";
                return false;
            }

            return TryGetServerConnectionInfo(response, fallbackPort, out connectionInfo, out error);
        }

        private static bool IsLoopbackServerAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            string value = address.Trim();
            if (string.Equals(value, "localhost", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IPAddress.TryParse(value, out IPAddress ipAddress) && IPAddress.IsLoopback(ipAddress);
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
            return HttpLink.FormatApiFailure(request);
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

            if (IsServerOnline(response) == false)
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

        private static bool TryGetServerConnectionInfo(UnityServerStatusItem response, ushort fallbackPort, out ServerConnectionInfo connectionInfo, out string error)
        {
            connectionInfo = default;
            error = string.Empty;

            if (response == null || response.isOnline == false)
            {
                error = "Battle server is offline";
                return false;
            }

            string address = NormalizeConnectionAddress(response.address);
            int port = response.port > 0 ? response.port : fallbackPort;
            if (string.IsNullOrWhiteSpace(response.serverId)
                || string.IsNullOrWhiteSpace(address)
                || port <= 0
                || port > ushort.MaxValue)
            {
                error = "Server API returned invalid battle server connection data";
                return false;
            }

            connectionInfo = new ServerConnectionInfo
            {
                ServerId = response.serverId,
                Address = address,
                Port = (ushort)port
            };
            return true;
        }

        private static bool IsServerOnline(UnityServerStatusResponse response)
        {
            if (response.isOnline)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(response.status))
            {
                return false;
            }

            return string.Equals(response.status, "ready", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(response.status, "online", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(response.status, "running", System.StringComparison.OrdinalIgnoreCase);
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
            if (IsOfflineMenuTestEnabled() || IsEditorVehicleTestModeEnabled())
            {
                LastServerStatus = IsOfflineMenuTestEnabled()
                    ? "Offline menu test mode is active. Networking was not started."
                    : "Client vehicle test mode is active. Normal networking startup was skipped.";
                return;
            }

            if (networkManager == null)
            {
                LastServerStatus = "Cannot start server: NetworkManager is not assigned.";
                return;
            }

            ApplyRuntimeServerOverrides();

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
            GameplayRuntimeDiagnostics.Record(
                "client_connection_state",
                "state", args.ConnectionState,
                "authenticated", ClientAuthenticationState.IsAuthenticated,
                "fishnet_client_started", networkManager != null && networkManager.IsClientStarted);

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
                if (_serverRegistered)
                {
                    StartCoroutine(UnregisterDedicatedServer());
                }
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
                if (_serverRegistered == false)
                {
                    yield return RegisterDedicatedServer();
                }

                if (_serverRegistered)
                {
                    yield return SendServerHeartbeat();
                }

                yield return wait;
            }

            _serverHeartbeatCoroutine = null;
        }

        private IEnumerator SendServerHeartbeat()
        {
            if (TryGetDedicatedServerCredentials(out string serverId, out string credential) == false)
            {
                yield break;
            }

            string address = GetAdvertisedServerAddress();
            ushort port = GetServerPort();
            UnityServerStatusPayload payload = new UnityServerStatusPayload
            {
                status = "ready",
                playersOnline = GetPlayersOnline(),
                maxPlayers = GetMaxPlayers(),
                activeMatches = GetActiveMatches(),
                address = address,
                port = port,
                message = "Unity server running"
            };

            string json = JsonUtility.ToJson(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastError = string.Empty;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + string.Format(UnityServerHeartbeatEndpoint, UnityWebRequest.EscapeURL(serverId));

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);
                    request.SetRequestHeader("Content-Type", "application/json");
                    SetDedicatedServerHeaders(request, serverId, credential);

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

        private IEnumerator UnregisterDedicatedServer()
        {
            if (TryGetDedicatedServerCredentials(out string serverId, out string credential) == false)
            {
                _serverRegistered = false;
                yield break;
            }

            string[] apiBases = HttpLink.GetBaseCandidates();
            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + string.Format(UnityServerUnregisterEndpoint, UnityWebRequest.EscapeURL(serverId));

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);
                    SetDedicatedServerHeaders(request, serverId, credential);

                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success || request.responseCode == 404)
                    {
                        HttpLink.SetResolvedBase(apiBase);
                        break;
                    }
                }
            }

            _serverRegistered = false;
        }

        private IEnumerator RegisterDedicatedServer()
        {
            if (TryGetDedicatedServerCredentials(out string serverId, out string credential) == false)
            {
                LastServerStatus = "Dedicated server registration requires RAV_SERVER_ID and RAV_SERVER_CREDENTIAL.";
                yield break;
            }

            string address = GetAdvertisedServerAddress();
            ushort port = GetServerPort();
            if (string.IsNullOrWhiteSpace(address) || port == 0)
            {
                LastServerStatus = "Dedicated server registration requires a valid advertised address and port.";
                yield break;
            }

            var payload = new UnityServerRegistrationPayload
            {
                region = GetServerRegion(),
                address = address,
                port = port,
                maxPlayers = GetMaxPlayers()
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastError = string.Empty;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                using (UnityWebRequest request = new UnityWebRequest(apiBase + UnityServerRegisterEndpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = GetApiRequestTimeoutSeconds(apiBase);
                    request.SetRequestHeader("Content-Type", "application/json");
                    SetDedicatedServerHeaders(request, serverId, credential);

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _serverRegistered = true;
                        LastServerStatus = "Dedicated server registered.";
                        HttpLink.SetResolvedBase(apiBase);
                        yield break;
                    }

                    lastError = FormatApiRequestError(apiBase, request);
                }
            }

            _serverRegistered = false;
            Debug.LogWarning("Dedicated server registration failed: " + lastError);
        }

        private static void SetDedicatedServerHeaders(UnityWebRequest request, string serverId, string credential)
        {
            request.SetRequestHeader("X-Server-Id", serverId);
            request.SetRequestHeader("X-Server-Credential", credential);
        }

        private static bool TryGetDedicatedServerCredentials(out string serverId, out string credential)
        {
            serverId = GetCommandLineValue(ServerIdArgument);
            if (string.IsNullOrWhiteSpace(serverId))
            {
                serverId = GetEnvironmentValue("RAV_SERVER_ID", "WOM_SERVER_ID");
            }

            credential = GetEnvironmentValue("RAV_SERVER_CREDENTIAL", "WOM_SERVER_CREDENTIAL");
            return string.IsNullOrWhiteSpace(serverId) == false
                   && string.IsNullOrWhiteSpace(credential) == false;
        }

        private static string GetServerRegion()
        {
            string region = GetEnvironmentValue("RAV_SERVER_REGION", "WOM_SERVER_REGION");
            return string.IsNullOrWhiteSpace(region) ? "default" : region.Trim();
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

        private bool IsOfflineMenuTestEnabled()
        {
            return menuTestMode != null && menuTestMode.IsOfflineMenuTestEnabled;
        }

        private bool IsEditorVehicleTestModeEnabled()
        {
#if UNITY_EDITOR
            return ClientVehicleTestMode.IsEnabled;
#else
            return false;
#endif
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
            string runtimeAddress = GetRuntimeServerAddress();
            if (string.IsNullOrWhiteSpace(runtimeAddress) == false)
            {
                return NormalizeConnectionAddress(runtimeAddress);
            }

            if (string.IsNullOrWhiteSpace(advertisedServerAddress) == false)
            {
                return NormalizeConnectionAddress(advertisedServerAddress);
            }

            if (HttpLink.IsLocal)
            {
                return "127.0.0.1";
            }

            return string.Empty;
        }

        private void ApplyRuntimeServerOverrides()
        {
            if (applyRuntimeServerConfig == false)
            {
                return;
            }

            if (autoStartMode != NetworkAutoStartMode.Server && server == null)
            {
                return;
            }

            if (networkManager == null || networkManager.TransportManager == null || networkManager.TransportManager.Transport == null)
            {
                return;
            }

            ushort port = GetRuntimeServerPort();
            if (port == 0)
            {
                return;
            }

            networkManager.TransportManager.Transport.SetPort(port);
        }

        private static string GetRuntimeServerAddress()
        {
            string value = GetCommandLineValue(ServerAddressArgument);
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }

            value = GetCommandLineValue(AdvertiseAddressArgument);
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }

            value = GetEnvironmentValue("RAV_SERVER_ADDRESS", "WOM_SERVER_ADDRESS");
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }

            value = GetEnvironmentValue("SERVER_ADDRESS");
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }

            value = GetEnvironmentValue("PUBLIC_IP");
            if (string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }

            return GetEnvironmentValue("HOST_IP");
        }

        private static ushort GetRuntimeServerPort()
        {
            string value = GetCommandLineValue(ServerPortArgument);
            if (TryParsePort(value, out ushort port))
            {
                return port;
            }

            value = GetCommandLineValue(GamePortArgument);
            if (TryParsePort(value, out port))
            {
                return port;
            }

            value = GetCommandLineValue(PortArgument);
            if (TryParsePort(value, out port))
            {
                return port;
            }

            value = GetEnvironmentValue("RAV_SERVER_PORT", "WOM_SERVER_PORT");
            if (TryParsePort(value, out port))
            {
                return port;
            }

            value = GetEnvironmentValue("GAME_PORT");
            if (TryParsePort(value, out port))
            {
                return port;
            }

            value = GetEnvironmentValue("SERVER_PORT");
            if (TryParsePort(value, out port))
            {
                return port;
            }

#if !UNITY_EDITOR
            value = GetEnvironmentValue("PORT");
            if (TryParsePort(value, out port))
            {
                return port;
            }
#endif

            return 0;
        }

        private static string GetEnvironmentValue(string name, string legacyName)
        {
            string value = GetEnvironmentValue(name);
            if (!string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(legacyName))
            {
                return value;
            }

            return GetEnvironmentValue(legacyName);
        }

        private static string GetEnvironmentValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return System.Environment.GetEnvironmentVariable(name);
        }

        private static string GetCommandLineValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string[] args = System.Environment.GetCommandLineArgs();
            string prefix = key + "=";

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (string.Equals(arg, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }

                    return string.Empty;
                }

                if (arg.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private static bool TryParsePort(string value, out ushort port)
        {
            port = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (ushort.TryParse(value.Trim(), out port) == false)
            {
                Debug.LogWarning("Ignored invalid server port value: " + value);
                port = 0;
                return false;
            }

            if (port == 0)
            {
                Debug.LogWarning("Ignored invalid server port value: " + value);
                return false;
            }

            return true;
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
        private class UnityServerRegistrationPayload
        {
            public string region;
            public string address;
            public int port;
            public int maxPlayers;
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
            public UnityServerStatusItem[] servers;
        }

        [System.Serializable]
        private class UnityServerStatusItem
        {
            public string serverId;
            public string region;
            public bool isOnline;
            public string status;
            public string address;
            public int port;
            public int playersOnline;
            public int maxPlayers;
            public int activeMatches;
            public string lastHeartbeatUtc;
            public string message;
        }

        [System.Serializable]
        private class ConnectionTicketRequest
        {
            public string serverId;
        }

        [System.Serializable]
        private class ConnectionTicketResponse
        {
            public string ticket;
            public string serverId;
            public string expiresAtUtc;
        }

        private struct ServerConnectionInfo
        {
            public string ServerId;
            public string Address;
            public ushort Port;
        }
    }
}
