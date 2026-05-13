using UnityEngine;
using TMPro;
using FishNet.Managing;
using FishNet.Transporting;
using Game.Scripts.UI.Helpers;

namespace Game.Scripts.UI.Loading
{
    public class LoadingSpinner : MonoBehaviour
    {
        public float rotationSpeed = -600;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private string sceneLoadingLabel = "Loading";
        [SerializeField] private string connectingLabel = "Connecting to battle server";
        [SerializeField] private string connectedLabel = "Connected";
        [SerializeField] private string offlineLabel = "Server offline. Reconnecting";
        [SerializeField] private float pulseAmplitude = 0f;
        [SerializeField] private float pulseSpeed = 2.5f;

        private RectTransform _rectTransform;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _subscribed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
            }

            if (statusText == null)
            {
                statusText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            _rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            _rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            UpdateStatusText();
        }

        private void Subscribe()
        {
            if (_subscribed || networkManager == null)
            {
                return;
            }

            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || networkManager == null)
            {
                return;
            }

            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            _subscribed = false;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientState = args.ConnectionState;
        }

        private void UpdateStatusText()
        {
            if (statusText == null)
            {
                return;
            }

            string dots = BuildDots();
            if (LoadingScreenManager.CurrentMode == LoadingScreenMode.SceneLoading)
            {
                statusText.text = sceneLoadingLabel + dots;
                return;
            }

            if (LoadingScreenManager.TryGetConnectionStatus(out string explicitStatus))
            {
                statusText.text = explicitStatus + dots;
                return;
            }

            if (_clientState == LocalConnectionState.Starting)
            {
                statusText.text = connectingLabel + dots;
                return;
            }

            if (_clientState == LocalConnectionState.Started)
            {
                statusText.text = connectedLabel + dots;
                return;
            }

            statusText.text = offlineLabel + dots;
        }

        private static string BuildDots()
        {
            int dots = 1 + Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 3;
            if (dots == 1)
            {
                return ".";
            }
            if (dots == 2)
            {
                return "..";
            }
            return "...";
        }
    }
}
