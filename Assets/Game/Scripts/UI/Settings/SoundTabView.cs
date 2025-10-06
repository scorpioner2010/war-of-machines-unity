using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class SoundTabView : MonoBehaviour
    {
        public Slider MasterVolumeSlider;
        public Slider MusicVolumeSlider;
        public Slider SfxVolumeSlider;

        private SettingsController _controller;

        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            // Subscribe to UI events
            MasterVolumeSlider.onValueChanged.AddListener(OnUiVolumeChanged);
            MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            SfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        public void SetData(SettingsModel model)
        {
            MasterVolumeSlider.value = model.UiVolume;
            MusicVolumeSlider.value = model.MusicVolume;
            SfxVolumeSlider.value = model.SfxVolume;
        }

        private void OnUiVolumeChanged(float value)
        {
            _controller.HandleUiVolumeChanged(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _controller.HandleMusicVolumeChanged(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            _controller.HandleSfxVolumeChanged(value);
        }
    }
}