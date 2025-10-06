using System;
using Game.Scripts.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.UI.Settings
{
    public class SettingsController
    {
        private SettingsModel _model;
        
        private Volume _postProcessingVolume;

        public SettingsController(SettingsModel model)
        {
            _model = model;
            _model.Load(); // Load from PlayerPrefs
        }
        
        public void SetPostProcessingVolume(Volume volume)
        {
            _postProcessingVolume = volume;
        }

        // ------------------ GENERAL ------------------
        public void HandleLanguageChanged(int index)
        {
            _model.Language = GetLanguageByIndex(index);
        }

        
        // ------------------ VIDEO --------------------
        public void HandleFullScreenChanged(int index)
        {
            _model.FullScreenIndex = index;
        }

        public void HandleResolutionChanged(int index)
        {
            _model.ResolutionIndex = index;
        }

        public void HandleQualityChanged(int index)
        {
            _model.QualityIndex = index;
        }

        public void HandleGammaChanged(float value)
        {
            _model.Gamma = value;
        }

        // ------------------ SOUND --------------------
        public void HandleUiVolumeChanged(float value)
        {
            _model.UiVolume = value;
            PlayerPrefs.SetFloat("UIVol", value);
        }

        public void HandleMusicVolumeChanged(float value)
        {
            _model.MusicVolume = value;
            PlayerPrefs.SetFloat("MusicVol", value);
            
        }

        public void HandleSfxVolumeChanged(float value)
        {   
            _model.SfxVolume = value;
            PlayerPrefs.SetFloat("SFXVol", value);
        }

        // ------------------ CONTROLS -----------------
        public void HandleMouseSensitivityChanged(float value)
        {
            _model.MouseSensitivity = value;
        }

        public void HandleInvertXAxisChanged(bool isOn)
        {
            _model.InvertXAxis = isOn;
        }

        public void HandleInvertYAxisChanged(bool isOn)
        {
            _model.InvertYAxis = isOn;
        }

        // Example methods for re-binding keys:
        public void HandleWalkKeyChanged(string newKey)
        {
            _model.WalkKey = newKey;
        }

        public void HandleAttackKeyChanged(string newKey)
        {
            _model.AttackKey = newKey;
        }

        // ------------------ APPLY CHANGES ------------------
        public void ApplyChanges(SettingsView.TabType tabType)
        {
            // 1) Save to PlayerPrefs
            
            switch (tabType)
            {
                case SettingsView.TabType.General:
                    _model.SaveGeneral();
                    break;
                case SettingsView.TabType.Video:
                    _model.SaveVideo();
                    // 2) Fullscreen
                    var fullScreenMode = (FullScreenMode)_model.FullScreenIndex;

                    // 3) Resolution
                    Resolution[] resolutions = Screen.resolutions;
                    int chosenIndex = Mathf.Clamp(_model.ResolutionIndex, 0, resolutions.Length - 1);
                    Resolution chosenResolution = resolutions[chosenIndex];
                    Screen.SetResolution(chosenResolution.width, chosenResolution.height, fullScreenMode);

                    // 4) Quality
                    QualitySettings.SetQualityLevel(_model.QualityIndex);

                    // 5) Gamma
                    // Option A: If using post-processing with ColorAdjustments
                    if (_postProcessingVolume != null &&
                        _postProcessingVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
                    {
                        float postExposure = Mathf.Log(_model.Gamma);
                        colorAdjustments.postExposure.Override(postExposure);
                    }
                    // Option B: Simple ambient light hack (less accurate):
                    // RenderSettings.ambientLight = Color.white * _model.Gamma;
                    
                    break;
                case SettingsView.TabType.Sounds:
                    _model.SaveAudio();
                    SoundCaller.GetAudioManager().UpdateVolume();
                    break;
                case SettingsView.TabType.Controls:
                    _model.SaveControls();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tabType), tabType, null);
            }
            
            _model.Save();

            UnityEngine.Debug.Log("Settings applied and saved to PlayerPrefs.");
        }

        // Example helper to map language index to string
        private string GetLanguageByIndex(int index)
        {
            switch (index)
            {
                case 1: return "Ukrainian";
                case 2: return "Spanish";
                default: return "English";
            }
        }
    }
}