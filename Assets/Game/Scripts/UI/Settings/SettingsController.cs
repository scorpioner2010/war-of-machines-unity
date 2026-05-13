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
            _model.Load();
            ClientGameplaySettings.SetServerCrosshairEnabled(_model.ServerCrosshairEnabled, false);
            ClientGameplaySettings.SetMouseSensitivities(
                _model.GameplayMouseSensitivity,
                _model.SniperMouseSensitivity,
                true);
        }
        
        public void SetPostProcessingVolume(Volume volume)
        {
            _postProcessingVolume = volume;
        }

        public void HandleLanguageChanged(int index)
        {
            _model.Language = GetLanguageByIndex(index);
        }

        
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

        public void HandleMouseSensitivityChanged(float value)
        {
            HandleGameplayMouseSensitivityChanged(value);
        }

        public void HandleGameplayMouseSensitivityChanged(float value)
        {
            _model.GameplayMouseSensitivity = ClientGameplaySettings.ClampMouseSensitivity(value);
            ClientGameplaySettings.SetGameplayMouseSensitivity(_model.GameplayMouseSensitivity);
        }

        public void HandleSniperMouseSensitivityChanged(float value)
        {
            _model.SniperMouseSensitivity = ClientGameplaySettings.ClampMouseSensitivity(value);
            ClientGameplaySettings.SetSniperMouseSensitivity(_model.SniperMouseSensitivity);
        }

        public void HandleInvertXAxisChanged(bool isOn)
        {
            _model.InvertXAxis = isOn;
        }

        public void HandleInvertYAxisChanged(bool isOn)
        {
            _model.InvertYAxis = isOn;
        }

        public void HandleServerCrosshairChanged(bool isOn)
        {
            _model.ServerCrosshairEnabled = isOn;
            ClientGameplaySettings.SetServerCrosshairEnabled(isOn);
        }

        public void HandleWalkKeyChanged(string newKey)
        {
            _model.WalkKey = newKey;
        }

        public void HandleAttackKeyChanged(string newKey)
        {
            _model.AttackKey = newKey;
        }

        public void ApplyChanges(SettingsView.TabType tabType)
        {
            switch (tabType)
            {
                case SettingsView.TabType.General:
                    _model.SaveGeneral();
                    break;
                case SettingsView.TabType.Video:
                    _model.SaveVideo();
                    var fullScreenMode = (FullScreenMode)_model.FullScreenIndex;

                    Resolution[] resolutions = Screen.resolutions;
                    int chosenIndex = Mathf.Clamp(_model.ResolutionIndex, 0, resolutions.Length - 1);
                    Resolution chosenResolution = resolutions[chosenIndex];
                    Screen.SetResolution(chosenResolution.width, chosenResolution.height, fullScreenMode);

                    QualitySettings.SetQualityLevel(_model.QualityIndex);

                    if (_postProcessingVolume != null &&
                        _postProcessingVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
                    {
                        float postExposure = Mathf.Log(_model.Gamma);
                        colorAdjustments.postExposure.Override(postExposure);
                    }
                    
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

        }

        private string GetLanguageByIndex(int index)
        {
            return "English";
        }
    }
}
