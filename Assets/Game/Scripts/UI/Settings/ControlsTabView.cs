using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class ControlsTabView : MonoBehaviour
    {
        public Slider MouseSensitivitySlider;
        public Toggle InvertXAxisToggle;
        public Toggle InvertYAxisToggle;
        public Text WalkKeyText;
        public Text AttackKeyText;
        public Button RebindWalkButton;
        public Button RebindAttackButton;

        private SettingsController _controller;

        public void Initialize(SettingsController controller)
        {
            _controller = controller;
        }

        public void SetData(SettingsModel model)
        {
        }

        private void OnMouseSensitivityChanged(float value)
        {
            _controller.HandleMouseSensitivityChanged(value);
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