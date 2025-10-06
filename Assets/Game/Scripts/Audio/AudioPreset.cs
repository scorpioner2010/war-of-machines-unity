using Cysharp.Threading.Tasks;
using Game.Scripts.Core.Helpers;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioPreset : MonoBehaviour
    {
        private AudioSource _source;
        private AudionElement _element;
        public SoundType Type { get; private set; }
        public bool IsPlaying => _source.isPlaying;

        public void Init() => _source = gameObject.GetComponent<AudioSource>();
        public void SetPitch(float value) => _source.pitch = value;
        public void SetLoop(bool value) => _source.loop = value;

        public void SetType(SoundType type)
        {
            Type = type;

            if (Type == SoundType.Sfx)
            {
                _source.spatialBlend = 1f;
            }
            else if (Type == SoundType.Music || Type == SoundType.Ui)
            {
                _source.spatialBlend = 0;
            }
        }

        public async void Play(AudionElement element)
        {
            _element = element;
            AudioClip clip = _element.clips.RandomElement();
            _source.clip = clip;
            _source.time = 0;
            _source.Stop();
            _source.enabled = true; //bug with turn off AudioSource
            _source.loop = false;
            SetPitch(1);
            int delay = (int)_element.delay * 1000;
            await UniTask.Delay(delay);
            _source.Play();
        }
        
        public void Stop() => _source.Stop();
        public void SetVolume(float volume)
        {
            _source.volume = volume * _element.volume;
        }
    }
}
