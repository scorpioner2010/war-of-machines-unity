using UnityEngine;
using TMPro;
using FishNet.Managing;
using FishNet.Transporting;
using Game.Scripts.UI.Helpers;
using UnityEngine.UI;

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
        [SerializeField] private bool hideGraphicDuringConnection = true;
        [SerializeField] private Graphic spinnerGraphic;
        [SerializeField] private TMP_Text connectionStatusText;

        private RectTransform _rectTransform;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private bool _subscribed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialRotation = _rectTransform.localRotation;
            _initialScale = _rectTransform.localScale;

            if (spinnerGraphic == null)
            {
                spinnerGraphic = GetComponent<Graphic>();
            }

            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
            }

            if (statusText == null)
            {
                statusText = GetComponentInChildren<TMP_Text>(true);
            }

            EnsureConnectionStatusText();
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
            bool isConnectionMode = LoadingScreenManager.CurrentMode == LoadingScreenMode.Connection;
            SetSpinnerGraphicVisible(isConnectionMode == false || hideGraphicDuringConnection == false);
            SetConnectionStatusTextVisible(isConnectionMode);

            if (isConnectionMode)
            {
                _rectTransform.localRotation = _initialRotation;
                _rectTransform.localScale = _initialScale;
                UpdateStatusText();
                return;
            }

            _rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            _rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            UpdateStatusText();
        }

        private void EnsureConnectionStatusText()
        {
            if (connectionStatusText != null)
            {
                return;
            }

            Transform parent = transform.parent != null ? transform.parent : transform;
            GameObject textObject = new GameObject("Connection Status Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            textObject.transform.SetAsLastSibling();

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.08f, 0.38f);
            rectTransform.anchorMax = new Vector2(0.92f, 0.62f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 27f;
            text.fontSize = 21f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            if (statusText != null)
            {
                text.font = statusText.font;
                text.fontSharedMaterial = statusText.fontSharedMaterial;
            }

            connectionStatusText = text;
            SetConnectionStatusTextVisible(false);
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
            EnsureConnectionStatusText();

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
            TMP_Text targetText = LoadingScreenManager.CurrentMode == LoadingScreenMode.Connection
                ? connectionStatusText
                : statusText;

            if (targetText == null)
            {
                return;
            }

            string dots = BuildDots();
            if (LoadingScreenManager.CurrentMode == LoadingScreenMode.SceneLoading)
            {
                targetText.text = sceneLoadingLabel + dots;
                return;
            }

            if (LoadingScreenManager.TryGetConnectionStatus(out string explicitStatus))
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
