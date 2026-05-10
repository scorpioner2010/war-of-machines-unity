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

        }

        private void OnCreditsButtonClicked()
        {
            
        }

        private void OnFeedbackButtonClicked()
        {
            
        }
        
        public void SetData(SettingsModel model)
        {
        }

        private void OnLanguageDropdownChanged(int index)
        {
            _controller.HandleLanguageChanged(index);
        }
        private int GetLanguageIndex(string language)
        {
            return 0;
        }
    }
}
