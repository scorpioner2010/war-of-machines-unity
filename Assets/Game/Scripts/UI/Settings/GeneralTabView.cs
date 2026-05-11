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

        private SettingsController _controller;
        private bool _suppressServerCrosshairEvent;
        
        public void Initialize(SettingsController controller)
        {
            _controller = controller;
            EnsureServerCrosshairToggle();

            if (ServerCrosshairToggle != null)
            {
                ServerCrosshairToggle.onValueChanged.RemoveListener(OnServerCrosshairToggleChanged);
                ServerCrosshairToggle.onValueChanged.AddListener(OnServerCrosshairToggleChanged);
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
            EnsureServerCrosshairToggle();
            if (ServerCrosshairToggle == null)
            {
                return;
            }

            _suppressServerCrosshairEvent = true;
            ServerCrosshairToggle.isOn = model != null && model.ServerCrosshairEnabled;
            _suppressServerCrosshairEvent = false;
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

        private void EnsureServerCrosshairToggle()
        {
            if (ServerCrosshairToggle != null)
            {
                return;
            }

            Transform existing = transform.Find("ServerCrosshairToggle");
            if (existing != null)
            {
                ServerCrosshairToggle = existing.GetComponent<Toggle>();
                if (ServerCrosshairToggle != null)
                {
                    return;
                }
            }

            GameObject root = new GameObject("ServerCrosshairToggle", typeof(RectTransform), typeof(Toggle), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(transform, false);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(20f, -90f);
            rootRect.sizeDelta = new Vector2(360f, 32f);

            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.minHeight = 32f;
            layoutElement.preferredHeight = 32f;

            HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 10f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Toggle toggle = root.GetComponent<Toggle>();
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.SetParent(rootRect, false);
            backgroundRect.sizeDelta = new Vector2(22f, 22f);
            LayoutElement backgroundLayout = background.GetComponent<LayoutElement>();
            backgroundLayout.minWidth = 22f;
            backgroundLayout.preferredWidth = 22f;
            backgroundLayout.minHeight = 22f;
            backgroundLayout.preferredHeight = 22f;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.12f, 0.14f, 0.17f, 1f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.SetParent(backgroundRect, false);
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            Image checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = new Color(0.25f, 0.62f, 1f, 1f);

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.SetParent(rootRect, false);
            labelRect.sizeDelta = new Vector2(300f, 28f);
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.minWidth = 280f;
            labelLayout.preferredWidth = 300f;

            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.text = "Server crosshair";
            labelText.fontSize = 20f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = ClientGameplaySettings.ServerCrosshairEnabled;
            ServerCrosshairToggle = toggle;
        }

        private int GetLanguageIndex(string language)
        {
            return 0;
        }
    }
}
