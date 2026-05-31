using Cysharp.Threading.Tasks;
using Game.Scripts.Core.Helpers;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioPreset : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        private AudionElement _element;
        public SoundType Type { get; private set; }
        public bool IsPlaying => source != null && source.isPlaying;

        public void Init()
        {
        }

        public void SetPitch(float value)
        {
            if (source != null)
            {
                source.pitch = value;
            }
        }

        public void SetLoop(bool value)
        {
            if (source != null)
            {
                source.loop = value;
            }
        }

        public void SetType(SoundType type)
        {
            Type = type;

            if (Type == SoundType.Sfx)
            {
                if (source != null)
                {
                    source.spatialBlend = 1f;
                }
            }
            else if (Type == SoundType.Music || Type == SoundType.Ui)
            {
                if (source != null)
                {
                    source.spatialBlend = 0;
                }
            }
        }

        public void Play(AudionElement element)
        {
            PlayAsync(element).Forget();
        }

        private async UniTask PlayAsync(AudionElement element)
        {
            _element = element;
            if (_element == null || _element.clips == null || _element.clips.Length == 0)
            {
                return;
            }

            if (source == null)
            {
                return;
            }

            AudioClip clip = _element.clips.RandomElement();
            if (clip == null)
            {
                return;
            }

            source.clip = clip;
            source.time = 0;
            source.Stop();
            source.enabled = true; //bug with turn off AudioSource
            source.loop = false;
            SetPitch(1);
            int delay = (int)_element.delay * 1000;
            await UniTask.Delay(delay);
            source.Play();
        }
        
        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        public void SetVolume(float volume)
        {
            if (source != null && _element != null)
            {
                source.volume = volume * _element.volume;
            }
        }
    }
}
