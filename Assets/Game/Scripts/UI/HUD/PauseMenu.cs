using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Menu menu;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button autonomousControlButton;
        [SerializeField] private TMP_Text autonomousControlButtonLabel;

        private bool _registered;

        public void Awake()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeButtonPressed);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(SettingsButtonPressed);
            }
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(DisconnectButtonPressed);
            }
            if (autonomousControlButton != null)
            {
                autonomousControlButton.onClick.AddListener(AutonomousControlButtonPressed);
            }

            RefreshAutonomousControlButton();
        }

        private void Start()
        {
            TryRegister();
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeButtonPressed);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(SettingsButtonPressed);
            }
            if (disconnectButton != null)
            {
                disconnectButton.onClick.RemoveListener(DisconnectButtonPressed);
            }
            if (autonomousControlButton != null)
            {
                autonomousControlButton.onClick.RemoveListener(AutonomousControlButtonPressed);
            }
        }
        
        public void DisconnectButtonPressed()
        {
            if (IsVehicleTestLoaded())
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (GameplaySpawner.In != null)
            {
                GameplaySpawner.In.ReturnToMainMenu();
            }
            else
            {
                MenuManager.OpenMenu(MenuType.MainMenu);
            }

            MenuLoadingScreenManager.ShowLoading();
            MenuLoadingScreenManager.HideLoading();
        }
        
        private void ResumeButtonPressed()
        {
            if (IsVehicleTestLoaded())
            {
                return;
            }

            MenuManager.OpenMenu(MenuType.GameplayHUD);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void SettingsButtonPressed()
        {
            if (IsVehicleTestLoaded())
            {
                return;
            }

            MenuManager.OpenMenu(MenuType.Settings);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void AutonomousControlButtonPressed()
        {
            VehicleRoot localVehicle = VehicleRoot.LocalPlayerVehicle;
            VehicleInputController inputController = localVehicle != null ? localVehicle.inputManager : null;
            if (inputController == null)
            {
                return;
            }

            inputController.RequestAutonomousControl(!inputController.IsAutonomous);
            ResumeButtonPressed();
        }
        
        public void OpenPause()
        {
            if (IsVehicleTestLoaded() || !TryRegister())
            {
                return;
            }

            MenuManager.OpenMenu(MenuType.GameplayPause);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void Update()
        {
            RefreshAutonomousControlButton();

            if (IsVehicleTestLoaded())
            {
                return;
            }

            if (VehicleInputController.Escape)
            {
                if (!TryRegister())
                {
                    return;
                }

                if (MenuManager.CurrentType == MenuType.GameplayHUD)
                {
                    OpenPause();
                }
                else if (MenuManager.CurrentType == MenuType.GameplayPause)
                {
                    ResumeButtonPressed();
                }
                else if (MenuManager.CurrentType == MenuType.Settings && MenuManager.PreviousType == MenuType.GameplayPause)
                {
                    OpenPause();
                }
            }
        }

        private void RefreshAutonomousControlButton()
        {
            if (autonomousControlButton == null)
            {
                return;
            }

            VehicleRoot localVehicle = VehicleRoot.LocalPlayerVehicle;
            VehicleInputController inputController = localVehicle != null ? localVehicle.inputManager : null;
            bool hasAliveVehicle = inputController != null
                                   && (localVehicle.health == null || !localVehicle.health.IsDead);
            autonomousControlButton.interactable = hasAliveVehicle;

            if (autonomousControlButtonLabel != null)
            {
                autonomousControlButtonLabel.text = inputController != null && inputController.IsAutonomous
                    ? "DISABLE AUTOPILOT"
                    : "ENABLE AUTOPILOT";
            }
        }

        private bool TryRegister()
        {
            if (_registered)
            {
                return true;
            }

            if (IsVehicleTestLoaded())
            {
                return false;
            }

            _registered = MenuManager.RegisterMenu(MenuType.GameplayPause, menu);
            return _registered;
        }

        private static bool IsVehicleTestLoaded()
        {
            Scene vehicleTestScene = SceneManager.GetSceneByName("VehicleTest");
            return vehicleTestScene.IsValid() && vehicleTestScene.isLoaded;
        }
    }
}
