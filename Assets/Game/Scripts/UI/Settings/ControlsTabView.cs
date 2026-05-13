using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            EnsureSensitivitySliders();

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
            EnsureSensitivitySliders();
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

        private void EnsureSensitivitySliders()
        {
            if (GameplayMouseSensitivitySlider == null && MouseSensitivitySlider != null)
            {
                GameplayMouseSensitivitySlider = MouseSensitivitySlider;
            }

            if (GameplayMouseSensitivitySlider == null)
            {
                GameplayMouseSensitivitySlider = FindSlider("GameplayMouseSensitivitySlider");
            }

            if (SniperMouseSensitivitySlider == null)
            {
                SniperMouseSensitivitySlider = FindSlider("SniperMouseSensitivitySlider");
            }

            if (GameplayMouseSensitivitySlider == null)
            {
                GameplayMouseSensitivitySlider = CreateSensitivitySlider(
                    "GameplayMouseSensitivitySlider",
                    "Mouse sensitivity",
                    new Vector2(-156.124f, 226.74f),
                    ClientGameplaySettings.DefaultGameplayMouseSensitivity);
            }

            if (SniperMouseSensitivitySlider == null)
            {
                SniperMouseSensitivitySlider = CreateSensitivitySlider(
                    "SniperMouseSensitivitySlider",
                    "Sniper sensitivity",
                    new Vector2(-156.124f, 164.71f),
                    ClientGameplaySettings.DefaultSniperMouseSensitivity);
            }

            ConfigureSensitivitySlider(GameplayMouseSensitivitySlider);
            ConfigureSensitivitySlider(SniperMouseSensitivitySlider);
        }

        private Slider FindSlider(string objectName)
        {
            Transform child = transform.Find(objectName);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent<Slider>();
        }

        private Slider CreateSensitivitySlider(string objectName, string labelText, Vector2 sliderPosition, float defaultValue)
        {
            CreateLabel(objectName + "_Label", labelText, new Vector2(-433.65f, sliderPosition.y));

            GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.SetParent(transform, false);
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = sliderPosition;
            sliderRect.sizeDelta = new Vector2(436.017f, 23.164f);

            Image background = CreateSliderImage("Background", sliderRect, new Color(0.16f, 0.18f, 0.22f, 1f));
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            Image fill = CreateSliderImage("Fill", fillAreaRect, new Color(0.35f, 0.67f, 1f, 1f));
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            Image handle = CreateSliderImage("Handle", handleAreaRect, new Color(0.92f, 0.96f, 1f, 1f));
            RectTransform handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(18f, 24f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.value = defaultValue;
            ConfigureSensitivitySlider(slider);

            return slider;
        }

        private void CreateLabel(string objectName, string labelText, Vector2 position)
        {
            if (transform.Find(objectName) != null)
            {
                return;
            }

            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(transform, false);
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = position;
            labelRect.sizeDelta = new Vector2(220f, 46f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
        }

        private static Image CreateSliderImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void ConfigureSensitivitySlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = ClientGameplaySettings.MinMouseSensitivity;
            slider.maxValue = ClientGameplaySettings.MaxMouseSensitivity;
            slider.wholeNumbers = false;
        }
    }
}
