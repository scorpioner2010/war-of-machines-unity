using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class VideoTabView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Dropdown FullScreenDropdown;
        public TMP_Dropdown ResolutionDropdown;
        public TMP_Dropdown QualityDropdown;
        public Slider GammaSlider;

        private SettingsController _controller;

        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            List<string> fullScreenType = new List<string>
            {
                "FullScreen",// = 1
                "Windowed", // = 3
            };
            FullScreenDropdown.AddOptions(fullScreenType);
            
            Resolution[] resolutions = Screen.resolutions;
            List<string> screenResolution = new List<string>();

            for (int i = 0; i < resolutions.Length; i++)
            {
                screenResolution.Add(resolutions[i].ToString());
            }
            
            ResolutionDropdown.AddOptions(screenResolution);
            
            List<string> quality = new List<string>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                quality.Add(QualitySettings.names[i]);
            }
            
            QualityDropdown.AddOptions(quality);
            
            // Subscribe to UI events
            FullScreenDropdown.onValueChanged.AddListener(OnFullScreenChanged);
            ResolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            QualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            GammaSlider.onValueChanged.AddListener(OnGammaChanged);
        }

        public void SetData(SettingsModel model)
        {
            if (model.FullScreenIndex == 1)
            {
                FullScreenDropdown.value = 0;
            }

            if (model.FullScreenIndex == 3)
            {
                FullScreenDropdown.value = 1;
            }
            
            ResolutionDropdown.value = model.ResolutionIndex;
            QualityDropdown.value = model.QualityIndex;
            GammaSlider.value = model.Gamma;
        }
        
        private void OnFullScreenChanged(int index)
        {
            if (index == 0)
            {
                _controller.HandleFullScreenChanged(1);
            }

            if (index == 1)
            {
                _controller.HandleFullScreenChanged(3);
            }
        }

        private void OnResolutionChanged(int index)
        {
            _controller.HandleResolutionChanged(index);
        }

        private void OnQualityChanged(int index)
        {
            _controller.HandleQualityChanged(index);
        }

        private void OnGammaChanged(float value)
        {
            _controller.HandleGammaChanged(value);
        }
    }
}