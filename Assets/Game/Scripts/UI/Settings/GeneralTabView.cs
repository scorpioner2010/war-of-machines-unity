using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class GeneralTabView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Dropdown LanguageDropdown;
        public Button FeedbackButton;
        public Button CreditsButton;
        public Toggle ServerCrosshairToggle;
        public Toggle CameraShakeToggle;

        private SettingsController _controller;
        private bool _suppressServerCrosshairEvent;
        private bool _suppressCameraShakeEvent;
        
        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            if (LanguageDropdown != null)
            {
                LanguageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
                LanguageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            }

            if (ServerCrosshairToggle != null)
            {
                ServerCrosshairToggle.onValueChanged.RemoveListener(OnServerCrosshairToggleChanged);
                ServerCrosshairToggle.onValueChanged.AddListener(OnServerCrosshairToggleChanged);
            }

            if (CameraShakeToggle != null)
            {
                CameraShakeToggle.onValueChanged.RemoveListener(OnCameraShakeToggleChanged);
                CameraShakeToggle.onValueChanged.AddListener(OnCameraShakeToggleChanged);
            }
        }

        private void OnCreditsButtonClicked()
        {
            
        }

        private void OnFeedbackButtonClicked()
        {
            
        }
        
        public void SetData(SettingsModel model)
        {
            if (ServerCrosshairToggle != null)
            {
                _suppressServerCrosshairEvent = true;
                ServerCrosshairToggle.isOn = model != null && model.ServerCrosshairEnabled;
                _suppressServerCrosshairEvent = false;
            }

            if (CameraShakeToggle != null)
            {
                _suppressCameraShakeEvent = true;
                CameraShakeToggle.isOn = model == null || model.CameraShakeEnabled;
                _suppressCameraShakeEvent = false;
            }
        }

        private void OnLanguageDropdownChanged(int index)
        {
            _controller.HandleLanguageChanged(index);
        }

        private void OnServerCrosshairToggleChanged(bool isOn)
        {
            if (_suppressServerCrosshairEvent || _controller == null)
            {
                return;
            }

            _controller.HandleServerCrosshairChanged(isOn);
        }

        private void OnCameraShakeToggleChanged(bool isOn)
        {
            if (_suppressCameraShakeEvent || _controller == null)
            {
                return;
            }

            _controller.HandleCameraShakeChanged(isOn);
        }

        private int GetLanguageIndex(string language)
        {
            return 0;
        }
    }
}
