using System.Collections.Generic;
using Game.Scripts.Core.Helpers;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioManager
    {
        public List<AudioPreset> Library = new();
        
        public const string MusicVol = "MusicVol";
        public const string SfxVol = "SFXVol";
        public const string UIVol = "UIVol";
        
        public const float MusicVolParameter = 1;
        public const float SfxVolParameter = 1;
        public const float UIVolParameter = 1;
        
        public AudioPreset Play(AudionElement element, SoundType type)
        {
            return Play(element, type, Vector3.zero);
        }
        
        public AudioPreset Play(AudionElement element, SoundType type, Vector3 position, Transform parent = null)
        {
            if(type == SoundType.Music && IsPlaying(element.name))
            {
                return GetPlayingPreset(element.name);
            }
            
            AudioPreset audioPreset = GetResource(element);

            if (audioPreset == null)
            {
                audioPreset = CreateNewAudio(element); //Create new audio
            }
            
            if (audioPreset == null)
            {
                return null;
            }
            
            SetPosition(position, audioPreset, parent); //Set position or parent
            
            audioPreset.transform.parent = SoundCaller.GetTransform();
            
            float volume = type switch
            {
                SoundType.Music => PlayerPrefs.GetFloat(MusicVol, MusicVolParameter),
                SoundType.Sfx => PlayerPrefs.GetFloat(SfxVol, SfxVolParameter),
                SoundType.Ui => PlayerPrefs.GetFloat(UIVol, UIVolParameter),
                _ => 0
            };
            
            audioPreset.SetType(type);
            audioPreset.Play(element);
            audioPreset.SetVolume(volume);
            
            return audioPreset;
        }

        public void UpdateVolume()
        {
            for (var i = 0; i < Library.Count; i++)
            {
                    
                float volume = Library[i].Type switch
                {
                    SoundType.Music => PlayerPrefs.GetFloat(MusicVol, MusicVolParameter),
                    SoundType.Sfx => PlayerPrefs.GetFloat(SfxVol, SfxVolParameter),
                    SoundType.Ui => PlayerPrefs.GetFloat(UIVol, UIVolParameter),
                    _ => 0
                };

                Library[i].SetVolume(volume);
            }
        }
        
        public void Stop(SoundType type)
        {
            Library.RemoveAllNull();
            
            foreach (AudioPreset lib in Library)
            {
                if (lib.Type == type)
                {
                    lib.Stop();
                }
            }
        }
        
        private AudioPreset CreateNewAudio(AudionElement element)
        {
            AudioPreset audioPreset = SoundCaller.GetAudioPreset();
            
            try
            {
                audioPreset.gameObject.name = element.name;
                audioPreset.Init();
                Library.Add(audioPreset);
            }
            catch
            {
                Debug.LogError(audioPreset);
                Debug.LogError(element);
                Debug.LogError(element.name);
            }
            
            return audioPreset;
        }
        
        private void SetPosition(Vector3 position, AudioPreset audioPreset, Transform parent)
        {
            if (parent == null)
            {
                audioPreset.transform.position = position;
            }
            else
            {
                audioPreset.transform.parent = parent;
                audioPreset.transform.localPosition = position;
            }
        }
        
        private AudioPreset GetResource(AudionElement element)
        {
            Library.RemoveAllNull();
                
            foreach (AudioPreset audio in Library)
            {
                if (audio.IsPlaying == false)
                {
                    audio.gameObject.name = element.name;
                    return audio;
                }
            }
            return null;
        }

        private bool IsPlaying(string nameAudio)
        {
            Library.RemoveAllNull();
            
            foreach (AudioPreset audio in Library)
            {
                if (audio.name == nameAudio && audio.IsPlaying)
                {
                    return true;
                }
            }

            return false;
        }

        private AudioPreset GetPlayingPreset(string nameAudio)
        {
            foreach (AudioPreset audio in Library)
            {
                if (audio.name == nameAudio && audio.IsPlaying)
                {
                    return audio;
                }
            }

            return null;
        }
    }

    public enum SoundType
    {
        Music,
        Sfx,
        Ui,
    }
}
