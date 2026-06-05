using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.Loading;
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
