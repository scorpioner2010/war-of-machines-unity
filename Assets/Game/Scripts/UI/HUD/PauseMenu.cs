using System;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button disconnectButton;
        
        public event Action OnDisconnectPressed;
        
        public void Awake()
        {
            resumeButton.onClick.AddListener(ResumeButtonPressed);
            disconnectButton.onClick.AddListener(DisconnectButtonPressed);
        }
        
        public void DisconnectButtonPressed()
        {
            OnDisconnectPressed?.Invoke();
            pauseMenu.SetActive(false);
            
            LoadingScreenManager.ShowLoading();
            LoadingScreenManager.HideLoading(); //hide with delay 1 second
        }
        
        private void ResumeButtonPressed()
        {
            pauseMenu.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        public void OpenPause()
        {
            if (IsVehicleTestScene())
            {
                return;
            }

            pauseMenu.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void Update()
        {
            if (IsVehicleTestScene())
            {
                return;
            }

            if (InputManager.Escape)
            {
                if (!pauseMenu.activeInHierarchy)
                {
                    pauseMenu.SetActive(true);
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    pauseMenu.SetActive(false);
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

        private static bool IsVehicleTestScene()
        {
            return SceneManager.GetActiveScene().name == "VehicleTest";
        }
    }
}
