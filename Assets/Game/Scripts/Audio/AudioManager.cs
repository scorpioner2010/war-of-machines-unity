using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioManager
    {
        public readonly List<AudioPreset> Library = new();

        private readonly AudioPreset _presetPrefab;
        private readonly Transform _root;
        private readonly float _defaultVolume;
        private float _sfxMinDistance = DefaultSfxMinDistance;
        private float _sfxMaxDistance = DefaultSfxMaxDistance;
        private AudioRolloffMode _sfxRolloffMode = AudioRolloffMode.Logarithmic;

        public const string MusicVol = "MusicVol";
        public const string SfxVol = "SFXVol";
        public const string UIVol = "UIVol";
        public const float DefaultSfxMinDistance = 2f;
        public const float DefaultSfxMaxDistance = 50f;

        public const float MusicVolParameter = 1;
        public const float SfxVolParameter = 1;
        public const float UIVolParameter = 1;

        public AudioManager(AudioPreset presetPrefab, Transform root, float defaultVolume = 1f)
        {
            _presetPrefab = presetPrefab;
            _root = root;
            _defaultVolume = Mathf.Clamp01(defaultVolume);
        }

        public AudioPreset Play(AudionElement element, SoundType type)
        {
            return Play(element, type, Vector3.zero);
        }

        public AudioPreset Play(
            AudionElement element,
            SoundType type,
            Vector3 position,
            Transform parent = null,
            bool loop = false)
        {
            if (element == null || element.clips == null || element.clips.Length == 0)
            {
                return null;
            }

            if (type == SoundType.Music)
            {
                AudioPreset playingMusic = GetPlayingPreset(element.name, type);
                if (playingMusic != null)
                {
                    return playingMusic;
                }

                if (type == SoundType.Music)
                {
                    Stop(SoundType.Music);
                }
            }

            AudioPreset audioPreset = GetAvailablePreset(type);
            if (audioPreset == null)
            {
                audioPreset = CreateNewAudio(element.name);
            }

            if (audioPreset == null)
            {
                return null;
            }

            SetPosition(position, audioPreset, parent, type);
            audioPreset.SetType(type);
            if (type == SoundType.Sfx)
            {
                audioPreset.SetSpatialSettings(_sfxMinDistance, _sfxMaxDistance, _sfxRolloffMode);
            }
            audioPreset.Play(element, loop || type == SoundType.Music);
            audioPreset.SetVolume(GetVolume(type));
            audioPreset.gameObject.name = element.name;

            return audioPreset;
        }

        public AudioPreset PlayUi(AudionElement element)
        {
            return Play(element, SoundType.Ui);
        }

        public AudioPreset PlaySfx(
            AudionElement element,
            Vector3 position,
            Transform parent = null,
            bool loop = false)
        {
            return Play(element, SoundType.Sfx, position, parent, loop);
        }

        public AudioPreset PlayMusic(AudionElement element)
        {
            return Play(element, SoundType.Music);
        }

        public void UpdateVolume()
        {
            RemoveNullEntries();

            for (int i = 0; i < Library.Count; i++)
            {
                AudioPreset preset = Library[i];
                preset.SetVolume(GetVolume(preset.Type));
            }
        }

        public void SetVolume(float volume, SoundType type)
        {
            PlayerPrefs.SetFloat(GetVolumeKey(type), Mathf.Clamp01(volume));
            UpdateVolume();
            PlayerPrefs.Save();
        }

        public void SetSfxSpatialSettings(
            float minDistance,
            float maxDistance,
            AudioRolloffMode rolloffMode)
        {
            _sfxMinDistance = Mathf.Max(0.01f, minDistance);
            _sfxMaxDistance = Mathf.Max(_sfxMinDistance, maxDistance);
            _sfxRolloffMode = rolloffMode;

            RemoveNullEntries();
            for (int i = 0; i < Library.Count; i++)
            {
                AudioPreset preset = Library[i];
                if (preset.Type == SoundType.Sfx)
                {
                    preset.SetSpatialSettings(_sfxMinDistance, _sfxMaxDistance, _sfxRolloffMode);
                }
            }
        }

        public void Stop(SoundType type)
        {
            RemoveNullEntries();

            foreach (AudioPreset preset in Library)
            {
                if (preset.Type == type)
                {
                    preset.Stop();
                }
            }
        }

        private AudioPreset CreateNewAudio(string eventName)
        {
            if (_presetPrefab == null)
            {
                Debug.LogError("AudioManager cannot create a sound because the AudioPreset prefab is missing.");
                return null;
            }

            AudioPreset audioPreset = _root == null
                ? Object.Instantiate(_presetPrefab)
                : Object.Instantiate(_presetPrefab, _root);

            if (audioPreset == null)
            {
                return null;
            }

            audioPreset.gameObject.name = eventName;
            audioPreset.Init();
            Library.Add(audioPreset);
            return audioPreset;
        }

        private void SetPosition(Vector3 position, AudioPreset audioPreset, Transform parent, SoundType type)
        {
            if (type != SoundType.Sfx || parent == null)
            {
                audioPreset.transform.SetParent(_root, false);
                audioPreset.transform.position = position;
                return;
            }

            audioPreset.transform.SetParent(parent, false);
            audioPreset.transform.localPosition = position;
        }

        private AudioPreset GetAvailablePreset(SoundType type)
        {
            RemoveNullEntries();

            foreach (AudioPreset audio in Library)
            {
                if (audio.Type == type && audio.IsAvailable)
                {
                    return audio;
                }
            }

            return null;
        }

        private AudioPreset GetPlayingPreset(string eventName, SoundType type)
        {
            RemoveNullEntries();

            foreach (AudioPreset audio in Library)
            {
                if (audio.Type == type
                    && audio.EventName == eventName
                    && audio.IsBusy)
                {
                    return audio;
                }
            }

            return null;
        }

        private float GetVolume(SoundType type)
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(GetVolumeKey(type), _defaultVolume));
        }

        private static string GetVolumeKey(SoundType type)
        {
            return type switch
            {
                SoundType.Music => MusicVol,
                SoundType.Sfx => SfxVol,
                SoundType.Ui => UIVol,
                _ => UIVol
            };
        }

        private void RemoveNullEntries()
        {
            for (int i = Library.Count - 1; i >= 0; i--)
            {
                if (Library[i] == null)
                {
                    Library.RemoveAt(i);
                }
            }
        }
    }

    public enum SoundType
    {
        Music,
        Sfx,
        Ui,
    }
}
