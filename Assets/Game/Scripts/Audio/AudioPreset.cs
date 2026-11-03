using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioPreset : MonoBehaviour
    {
        [SerializeField] private AudioSource source;

        private AudionElement _element;
        private AudioClipEntry _clipEntry;
        private double _scheduledDspTime = -1d;
        private float _playbackVolume = 1f;
        private float _categoryVolume = 1f;

        public SoundType Type { get; private set; }
        public bool IsPlaying => source != null && source.isPlaying;
        public bool IsBusy => source != null
            && (source.isPlaying || (_scheduledDspTime >= 0d && AudioSettings.dspTime < _scheduledDspTime));
        public bool IsAvailable => source != null && !IsBusy;
        public string EventName => _element != null ? _element.name : string.Empty;
        public float PlaybackVolume => _playbackVolume;
        public float Pitch => source != null ? source.pitch : 1f;

        private void Awake()
        {
            EnsureSource();
        }

        public void Init()
        {
            EnsureSource();
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            _element = null;
            _clipEntry = null;
            _scheduledDspTime = -1d;
            _playbackVolume = 1f;
            _categoryVolume = 1f;
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

            if (source != null)
            {
                source.spatialBlend = type == SoundType.Sfx ? 1f : 0f;
            }
        }

        public void SetSpatialSettings(float minDistance, float maxDistance, AudioRolloffMode rolloffMode)
        {
            EnsureSource();
            if (source == null)
            {
                return;
            }

            float safeMinDistance = Mathf.Max(0.01f, minDistance);
            source.minDistance = safeMinDistance;
            source.maxDistance = Mathf.Max(safeMinDistance, maxDistance);
            source.rolloffMode = rolloffMode;
        }

        public void Play(AudionElement element)
        {
            Play(element, false);
        }

        public void Play(AudionElement element, bool loop)
        {
            EnsureSource();
            Stop();
            _element = element;

            if (_element == null || _element.clips == null || _element.clips.Length == 0 || source == null)
            {
                return;
            }

            AudioClipEntry clipEntry = GetRandomClip(_element.clips);
            if (clipEntry == null || clipEntry.clip == null)
            {
                return;
            }

            source.clip = clipEntry.clip;
            source.time = 0f;
            source.enabled = true;
            source.loop = loop;
            source.pitch = 1f;
            _clipEntry = clipEntry;
            _playbackVolume = 1f;

            float delay = Mathf.Max(0f, _element.delay);
            if (delay > 0f)
            {
                _scheduledDspTime = AudioSettings.dspTime + delay;
                source.PlayScheduled(_scheduledDspTime);
            }
            else
            {
                source.Play();
            }
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
                source.loop = false;
            }

            _scheduledDspTime = -1d;
        }

        public void SetVolume(float volume)
        {
            _categoryVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        public void SetPlaybackVolume(float volume)
        {
            _playbackVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (source != null && _element != null)
            {
                float clipVolume = _clipEntry != null ? _clipEntry.volume : 1f;
                source.volume = _categoryVolume
                    * Mathf.Clamp01(_element.volume)
                    * Mathf.Clamp01(clipVolume)
                    * _playbackVolume;
            }
        }

        private void EnsureSource()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }

        private static AudioClipEntry GetRandomClip(AudioClipEntry[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClipEntry clip = clips[(startIndex + offset) % clips.Length];
                if (clip != null && clip.clip != null)
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
