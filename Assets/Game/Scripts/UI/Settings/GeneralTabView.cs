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
        public Slider ReticleOpacitySlider;

        private SettingsController _controller;
        private bool _suppressServerCrosshairEvent;
        private bool _suppressCameraShakeEvent;
        private bool _suppressReticleOpacityEvent;
        
        public void Initialize(SettingsController controller)
        {
            _controller = controller;
            EnsureReticleOpacitySlider();

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

            if (ReticleOpacitySlider != null)
            {
                ReticleOpacitySlider.minValue = 0f;
                ReticleOpacitySlider.maxValue = 1f;
                ReticleOpacitySlider.wholeNumbers = false;
                ReticleOpacitySlider.onValueChanged.RemoveListener(OnReticleOpacityChanged);
                ReticleOpacitySlider.onValueChanged.AddListener(OnReticleOpacityChanged);
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

            if (ReticleOpacitySlider != null)
            {
                _suppressReticleOpacityEvent = true;
                ReticleOpacitySlider.SetValueWithoutNotify(ClientGameplaySettings.ReticleOpacity);
                _suppressReticleOpacityEvent = false;
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

        private void OnReticleOpacityChanged(float value)
        {
            if (_suppressReticleOpacityEvent || _controller == null)
            {
                return;
            }

            _controller.HandleReticleOpacityChanged(value);
        }

        private void EnsureReticleOpacitySlider()
        {
            if (ReticleOpacitySlider != null)
            {
                return;
            }

            GameObject sliderObject = new GameObject("ReticleOpacitySlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(transform, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0f, 1f);
            sliderRect.anchoredPosition = new Vector2(-82.335f, 230f);
            sliderRect.sizeDelta = new Vector2(322f, 32f);

            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.22f, 1f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.targetGraphic = background;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = ClientGameplaySettings.ReticleOpacity;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.25f);
            fillRect.anchorMax = new Vector2(1f, 0.75f);
            fillRect.offsetMin = new Vector2(5f, 0f);
            fillRect.offsetMax = new Vector2(-5f, 0f);
            fillObject.GetComponent<Image>().color = new Color(0.72f, 0.78f, 0.92f, 1f);
            slider.fillRect = fillRect;

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(sliderObject.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.sizeDelta = new Vector2(22f, 32f);
            handleObject.GetComponent<Image>().color = Color.white;
            slider.handleRect = handleRect;

            GameObject labelObject = new GameObject("ReticleOpacitySlider_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(-82.335f, 262f);
            labelRect.sizeDelta = new Vector2(322f, 30f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Reticle Opacity";
            label.fontSize = 18f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;

            ReticleOpacitySlider = slider;
        }

        private int GetLanguageIndex(string language)
        {
            return 0;
        }
    }
}
