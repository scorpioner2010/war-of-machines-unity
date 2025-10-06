using System.Collections.Generic;
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

        private SettingsController _controller;
        
        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            // Subscribe to UI events
            //LanguageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            //FeedbackButton.onClick.AddListener(OnFeedbackButtonClicked);
            //CreditsButton.onClick.AddListener(OnCreditsButtonClicked);
            
            //List<string> languageList = new List<string>();
            //languageList.Add("English");
            //LanguageDropdown.AddOptions(languageList);
        }

        private void OnCreditsButtonClicked()
        {
            
        }

        private void OnFeedbackButtonClicked()
        {
            
        }
        
        public void SetData(SettingsModel model)
        {
            //LanguageDropdown.value = GetLanguageIndex(model.Language);
        }

        private void OnLanguageDropdownChanged(int index)
        {
            _controller.HandleLanguageChanged(index);
        }
        private int GetLanguageIndex(string language)
        {
            switch (language)
            {
                case "Ukrainian": return 1;
                case "Spanish":   return 2;
                default:          return 0; // English
            }
        }
    }
}