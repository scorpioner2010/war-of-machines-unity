using FishNet.Managing;
using FishNet.Transporting;
using Game.Scripts.Core.Services;
using Game.Scripts.Player.Data;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
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
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private Button connect;
        [SerializeField] private Button server;
        [SerializeField] private GameObject panel;
        [SerializeField] private NetworkAutoStartMode autoStartMode = NetworkAutoStartMode.None;
        [SerializeField] private bool hideControlsWhenAutoStarting = true;
        [SerializeField] private float autoStartDelaySeconds = 0.25f;
        [SerializeField] private float clientRetryDelaySeconds = 1f;
        [SerializeField] private int clientMaxStartAttempts = 30;

        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private bool _clientInfoRegistered;

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
                return;
            }

            if (_serverState != LocalConnectionState.Stopped)
            {
                return;
            }

            if (server != null)
            {
                server.interactable = false;
            }

            if (IsServerPortAvailable() == false)
            {
                Debug.LogWarning($"Cannot start server because UDP port {GetServerPort()} is already in use. Use Connect in this window, or stop the other server first.");
                if (server != null)
                {
                    server.interactable = true;
                }
                return;
            }

            if (networkManager.ServerManager.StartConnection() == false)
            {
                if (server != null)
                {
                    server.interactable = true;
                }
            }
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
            }

            RefreshControls();
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverState = args.ConnectionState;

            if (_serverState == LocalConnectionState.Started)
            {
                if (server != null)
                {
                    server.interactable = false;
                }
            }
            else if (_serverState == LocalConnectionState.Stopped)
            {
                if (server != null)
                {
                    server.interactable = true;
                }
            }

            RefreshControls();
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
    }
}
