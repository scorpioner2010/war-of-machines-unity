using FishNet.Managing;
using Game.Scripts.Core.Resources;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Testing
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class ClientVehicleTestMode : MonoBehaviour
    {
#if UNITY_EDITOR
        private static ClientVehicleTestMode _instance;
        private static bool _active;

        [SerializeField] private bool enableVehicleTestMode;
        [SerializeField] private VehicleTestSceneController controller;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private RobotRegistry registry;
        [SerializeField] private GameResourceManager resourceManager;
        [SerializeField] private Camera testCamera;
        [SerializeField] private CameraSync testCameraSync;
        [SerializeField] private Camera normalClientCamera;
        [SerializeField] private GameObject sceneGameplayHud;
        [SerializeField] private Canvas sceneGameplayCanvas;
        [SerializeField] private VehicleTestRuntimeSettings testRuntimeSettings;
        [SerializeField] private ServerSettings serverSettingsPrefab;

        public static bool IsEnabled => _active;

        private ServerSettings _testServerSettings;

        private void Awake()
        {
            _instance = this;
            _active = false;

            if (!enableVehicleTestMode)
            {
                return;
            }

            if (!ValidateConfiguration())
            {
                return;
            }

            _active = true;
            ApplyConfiguration();
        }

        private void OnDestroy()
        {
            if (ServerSettings.In == _testServerSettings)
            {
                ServerSettings.In = null;
            }

            if (_testServerSettings != null)
            {
                Destroy(_testServerSettings.gameObject);
                _testServerSettings = null;
            }

            if (_instance == this)
            {
                _instance = null;
                _active = false;
            }
        }

        private bool ValidateConfiguration()
        {
            if (controller == null || networkManager == null || registry == null || resourceManager == null)
            {
                Debug.LogError("Client vehicle test mode is enabled but its runtime references are incomplete.", this);
                return false;
            }

            if (testCamera == null || testCameraSync == null || sceneGameplayHud == null || sceneGameplayCanvas == null)
            {
                Debug.LogError("Client vehicle test mode is enabled but its camera or scene HUD references are incomplete.", this);
                return false;
            }

            if (testRuntimeSettings == null)
            {
                Debug.LogError("Client vehicle test mode is enabled but VehicleTestRuntimeSettings is not assigned.", this);
                return false;
            }

            if (serverSettingsPrefab == null)
            {
                Debug.LogError("Client vehicle test mode is enabled but the ServerSettings prefab is not assigned.", this);
                return false;
            }

            return true;
        }

        private void ApplyConfiguration()
        {
            EnsureTestServerSettings();

            controller.registry = registry;
            controller.resourceManager = resourceManager;
            controller.testCamera = testCamera;
            controller.testCameraSync = testCameraSync;
            controller.networkManager = networkManager;
            controller.testRuntimeSettings = testRuntimeSettings;
            controller.sceneGameplayHud = sceneGameplayHud;
            controller.sceneGameplayCanvas = sceneGameplayCanvas;
            controller.useSceneGameplayHud = true;
            controller.clientGameplayHudPrefab = null;
            controller.replaceSceneGameplayHud = false;

            if (normalClientCamera != null)
            {
                normalClientCamera.enabled = false;
            }

            controller.gameObject.SetActive(true);
        }

        private void EnsureTestServerSettings()
        {
            if (ServerSettings.In != null || serverSettingsPrefab == null)
            {
                return;
            }

            _testServerSettings = Instantiate(serverSettingsPrefab);
            _testServerSettings.name = "ServerSettings (VehicleTest)";
        }
#else
        public static bool IsEnabled => false;
#endif
    }
}
