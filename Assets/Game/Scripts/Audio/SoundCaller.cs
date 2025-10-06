using System.Collections;
using FishNet;
using Game.Scripts.Core.Helpers;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class SoundCaller : MonoBehaviour
    {
        public float volumeDefaultParameter = 1;
        public AudioResources resources;
        private static SoundCaller _in;
        public AudioPreset prefab;
        public AudioManager Manager;
        
        private GameObject _gmObj;
        private bool StopPlay => InstanceFinder.IsServer && InstanceFinder.IsClient == false;
        
        private void Awake()
        {
            _in = this;
            Manager = new AudioManager();
        }
        
        public static AudioManager GetAudioManager() 
        {  
            if (_in == null)
            {
                return null;
            }

            return _in.Manager;
        }


        public static AudioPreset GetAudioPreset()
        {
            if (_in == null)
            {
                return null;
            }
            
            return Instantiate(_in.prefab);
        }

        public static Transform GetTransform() => _in.transform;
        private void Start() => StartCoroutine(SetParametersVolume());
        
        public static AudioPreset PlayOneShot(string eventName, Transform parent)
        {
            if (_in.StopPlay)
            {
                return null;
            }
            
            if (_in == null)
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName);
            return _in.Manager.Play(element, SoundType.Sfx, Vector3.zero, parent);
        }

        public static void StopMusic()
        {
            if (_in.StopPlay)
            {
                return;
            }
            
            if (_in == null)
            {
                return;
            }

            _in.Manager.Stop(SoundType.Music);
        }
        
        public static AudioPreset PlayOneShot(string eventName, Vector3 position)
        {
            if (_in.StopPlay)
            {
                return null;
            }
            
            if (_in == null)
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName);
            return _in.Manager.Play(element, SoundType.Sfx, position);
        }

        public static AudioPreset PlayOneShot(string eventName)
        {
            if (_in.StopPlay)
            {
                return null;
            }
            
            if (_in == null)
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName);
            return _in.Manager.Play(element, SoundType.Ui);
        }
        
        public static void PlayMusic(string eventName)
        {
            if (_in.StopPlay)
            {
                return;
            }
            
            StopMusic();
            
            AudionElement element = _in.resources.GetResource(eventName);
            AudioPreset info = _in.Manager.Play(element, SoundType.Music);
            
            if (info == null)
            {
                return;
            }
            
            info.SetLoop(true);
        }
        
        private IEnumerator SetParametersVolume()
        {
            yield return new WaitForSeconds(0.3f);

            float music = PlayerPrefs.GetFloat(AudioManager.MusicVol, volumeDefaultParameter);
            float ui = PlayerPrefs.GetFloat(AudioManager.UIVol, volumeDefaultParameter);
            float sfx = PlayerPrefs.GetFloat(AudioManager.SfxVol, volumeDefaultParameter);

            ChangeVolume(sfx, SoundType.Sfx);
            ChangeVolume(music, SoundType.Music);
            ChangeVolume(ui, SoundType.Ui);
        }

        public void ChangeVolume(float volume, SoundType type)
        {
            if (type == SoundType.Music)
            {
                PlayerPrefs.SetFloat(AudioManager.MusicVol, volume);
            }
            
            if(type == SoundType.Ui)
            {
                PlayerPrefs.SetFloat(AudioManager.UIVol, volume);
            }
            
            if (type == SoundType.Sfx)
            {
                PlayerPrefs.SetFloat(AudioManager.SfxVol, volume);
                
            }
            
            Manager.Library.RemoveAllNull();
            
            foreach (AudioPreset preset in Manager.Library)
            {
                if (preset.Type == type)
                {
                    preset.SetVolume(volume);
                }
            }
        }
    }
}
