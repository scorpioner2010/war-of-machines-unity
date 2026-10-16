using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class ControlsTabView : MonoBehaviour
    {
        public Slider MouseSensitivitySlider;
        public Slider GameplayMouseSensitivitySlider;
        public Slider SniperMouseSensitivitySlider;
        public Toggle InvertXAxisToggle;
        public Toggle InvertYAxisToggle;
        public Text WalkKeyText;
        public Text AttackKeyText;
        public Button RebindWalkButton;
        public Button RebindAttackButton;

        private SettingsController _controller;
        private bool _suppressSliderEvents;

        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            if (GameplayMouseSensitivitySlider != null)
            {
                GameplayMouseSensitivitySlider.onValueChanged.RemoveListener(OnGameplayMouseSensitivityChanged);
                GameplayMouseSensitivitySlider.onValueChanged.AddListener(OnGameplayMouseSensitivityChanged);
            }

            if (SniperMouseSensitivitySlider != null)
            {
                SniperMouseSensitivitySlider.onValueChanged.RemoveListener(OnSniperMouseSensitivityChanged);
                SniperMouseSensitivitySlider.onValueChanged.AddListener(OnSniperMouseSensitivityChanged);
            }
        }

        public void SetData(SettingsModel model)
        {
            _suppressSliderEvents = true;

            if (GameplayMouseSensitivitySlider != null)
            {
                GameplayMouseSensitivitySlider.value = model != null
                    ? model.GameplayMouseSensitivity
                    : ClientGameplaySettings.DefaultGameplayMouseSensitivity;
            }

            if (SniperMouseSensitivitySlider != null)
            {
                SniperMouseSensitivitySlider.value = model != null
                    ? model.SniperMouseSensitivity
                    : ClientGameplaySettings.DefaultSniperMouseSensitivity;
            }

            _suppressSliderEvents = false;
        }

        private void OnMouseSensitivityChanged(float value)
        {
            _controller.HandleMouseSensitivityChanged(value);
        }

        private void OnGameplayMouseSensitivityChanged(float value)
        {
            if (_suppressSliderEvents || _controller == null)
            {
                return;
            }

            _controller.HandleGameplayMouseSensitivityChanged(value);
        }

        private void OnSniperMouseSensitivityChanged(float value)
        {
            if (_suppressSliderEvents || _controller == null)
            {
                return;
            }

            _controller.HandleSniperMouseSensitivityChanged(value);
        }

        private void OnInvertXAxisChanged(bool isOn)
        {
            _controller.HandleInvertXAxisChanged(isOn);
        }

        private void OnInvertYAxisChanged(bool isOn)
        {
            _controller.HandleInvertYAxisChanged(isOn);
        }

        private void OnWalkKeyBound(string newKey)
        {
            _controller.HandleWalkKeyChanged(newKey);
            WalkKeyText.text = newKey;
        }

        private void OnAttackKeyBound(string newKey)
        {
            _controller.HandleAttackKeyChanged(newKey);
            AttackKeyText.text = newKey;
        }

    }
}
