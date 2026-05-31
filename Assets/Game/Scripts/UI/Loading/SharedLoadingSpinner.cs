using UnityEngine;
using TMPro;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine.UI;

namespace Game.Scripts.UI.Loading
{
    public class SharedLoadingSpinner : MonoBehaviour
    {
        public float rotationSpeed = -600;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private string sceneLoadingLabel = "Loading";
        [SerializeField] private string connectingLabel = "Connecting to battle server";
        [SerializeField] private string connectedLabel = "Connected";
        [SerializeField] private string offlineLabel = "Server offline. Reconnecting";
        [SerializeField] private bool hideGraphicDuringConnection = true;
        [SerializeField] private Graphic spinnerGraphic;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private RectTransform rectTransform;

        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _subscribed;

        private void Awake()
        {
            if (rectTransform == null)
            {
                enabled = false;
                return;
            }

            _initialRotation = rectTransform.localRotation;
            _initialScale = rectTransform.localScale;
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshVisualState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (rectTransform == null)
            {
                return;
            }

            bool isConnectionMode = MenuLoadingScreenManager.CurrentMode == MenuLoadingScreenMode.Connection;
            SetSpinnerGraphicVisible(isConnectionMode == false || hideGraphicDuringConnection == false);
            SetConnectionStatusTextVisible(isConnectionMode);

            if (isConnectionMode)
            {
                rectTransform.localRotation = _initialRotation;
                rectTransform.localScale = _initialScale;
                UpdateStatusText();
                return;
            }

            rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            rectTransform.localScale = _initialScale;
            UpdateStatusText();
        }

        private void SetSpinnerGraphicVisible(bool visible)
        {
            if (spinnerGraphic == null || spinnerGraphic.enabled == visible)
            {
                return;
            }

            spinnerGraphic.enabled = visible;
        }

        private void SetConnectionStatusTextVisible(bool visible)
        {
            if (connectionStatusText == null || connectionStatusText.gameObject.activeSelf == visible)
            {
                return;
            }

            connectionStatusText.gameObject.SetActive(visible);
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
            TMP_Text targetText = statusText;
            if (MenuLoadingScreenManager.CurrentMode == MenuLoadingScreenMode.Connection)
            {
                targetText = connectionStatusText != null ? connectionStatusText : statusText;
            }

            if (targetText == null)
            {
                return;
            }

            string dots = BuildDots();
            if (MenuLoadingScreenManager.CurrentMode == MenuLoadingScreenMode.SceneLoading)
            {
                targetText.text = sceneLoadingLabel + dots;
                return;
            }

            if (MenuLoadingScreenManager.TryGetConnectionStatus(out string explicitStatus))
            {
                targetText.text = explicitStatus;
                return;
            }

            if (_clientState == LocalConnectionState.Starting)
            {
                targetText.text = connectingLabel;
                return;
            }

            if (_clientState == LocalConnectionState.Started)
            {
                targetText.text = connectedLabel;
                return;
            }

            targetText.text = offlineLabel;
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
