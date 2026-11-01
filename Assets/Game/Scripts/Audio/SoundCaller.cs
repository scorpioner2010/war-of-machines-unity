using FishNet;
using Game.Scripts.MenuController;
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

        [Header("Runtime SFX Spatial Settings")]
        [Min(AudioManager.DefaultSfxMinDistance)]
        public float sfxMinDistance = AudioManager.DefaultSfxMinDistance;
        [Min(AudioManager.DefaultSfxMinDistance)]
        public float sfxMaxDistance = AudioManager.DefaultSfxMaxDistance;
        public AudioRolloffMode sfxRolloffMode = AudioRolloffMode.Logarithmic;

        private static bool _battleAudioActive;
        private float _appliedSfxMinDistance = float.NaN;
        private float _appliedSfxMaxDistance = float.NaN;
        private AudioRolloffMode _appliedSfxRolloffMode;

        private bool StopPlay => InstanceFinder.IsServerStarted && InstanceFinder.IsClientStarted == false;
        
        private void Awake()
        {
            if (_in != null && _in != this)
            {
                Destroy(gameObject);
                return;
            }

            _in = this;
            _battleAudioActive = false;
            Manager = new AudioManager(prefab, transform, volumeDefaultParameter);
            SyncRuntimeSfxSpatialSettings(true);
        }

        private void OnEnable()
        {
            MenuManager.OnEnable += HandleMenuOpened;
            if (MenuManager.IsReady)
            {
                HandleMenuOpened(MenuManager.CurrentType);
            }
        }

        private void Update()
        {
            if (!StopPlay)
            {
                SyncRuntimeSfxSpatialSettings();
                Manager?.UpdateVolume();
            }
        }

        private void OnDestroy()
        {
            MenuManager.OnEnable -= HandleMenuOpened;
            if (_in == this)
            {
                _battleAudioActive = false;
                _in = null;
            }
        }
        
        public static AudioManager GetAudioManager() => _in?.Manager;

        public static AudioPreset GetAudioPreset()
        {
            if (_in == null)
            {
                return null;
            }

            if (_in.prefab == null)
            {
                return null;
            }

            return Instantiate(_in.prefab);
        }

        public static Transform GetTransform() => _in != null ? _in.transform : null;

        private static bool CanPlay()
        {
            return _in != null
                && _in.Manager != null
                && _in.resources != null
                && !_in.StopPlay;
        }

        public static AudioPreset PlayOneShot(string eventName, Transform parent)
        {
            return PlaySfx(eventName, parent);
        }

        public static AudioPreset PlaySfx(string eventName, Transform parent, Vector3 localPosition = default)
        {
            if (!CanPlay())
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName, SoundType.Sfx);
            return _in.Manager.PlaySfx(element, localPosition, parent);
        }

        public static AudioPreset PlaySfxForRobot(
            string eventName,
            Transform parent,
            Vector3 localPosition,
            string robotCode)
        {
            if (!CanPlay())
            {
                return null;
            }

            AudionElement element = _in.resources.GetResource(
                eventName,
                SoundType.Sfx,
                robotCode);
            return _in.Manager.PlaySfx(element, localPosition, parent);
        }

        public static AudioPreset PlayLoopedSfx(
            string eventName,
            Transform parent,
            Vector3 localPosition = default)
        {
            if (!CanPlay())
            {
                return null;
            }

            AudionElement element = _in.resources.GetResource(eventName, SoundType.Sfx);
            return _in.Manager.PlaySfx(element, localPosition, parent, true);
        }

        public static AudioPreset PlayLoopedSfxForRobot(
            string eventName,
            Transform parent,
            Vector3 localPosition,
            string robotCode)
        {
            if (!CanPlay())
            {
                return null;
            }

            AudionElement element = _in.resources.GetResource(
                eventName,
                SoundType.Sfx,
                robotCode);
            return _in.Manager.PlaySfx(element, localPosition, parent, true);
        }

        public static void StopMusic()
        {
            if (!CanPlay())
            {
                return;
            }

            _in.Manager.Stop(SoundType.Music);
        }
        
        public static AudioPreset PlayOneShot(string eventName, Vector3 position)
        {
            return PlaySfx(eventName, position);
        }

        public static AudioPreset PlaySfx(string eventName, Vector3 position)
        {
            if (!CanPlay())
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName, SoundType.Sfx);
            return _in.Manager.PlaySfx(element, position);
        }

        public static AudioPreset PlaySfxForRobot(
            string eventName,
            Vector3 position,
            string robotCode)
        {
            if (!CanPlay())
            {
                return null;
            }

            AudionElement element = _in.resources.GetResource(
                eventName,
                SoundType.Sfx,
                robotCode);
            return _in.Manager.PlaySfx(element, position);
        }

        public static AudioPreset PlayOneShot(string eventName)
        {
            return PlayUi(eventName);
        }

        public static AudioPreset PlayUi(string eventName)
        {
            if (!CanPlay())
            {
                return null;
            }
            
            AudionElement element = _in.resources.GetResource(eventName, SoundType.Ui);
            return _in.Manager.PlayUi(element);
        }
        
        public static void PlayMusic(string eventName)
        {
            if (!CanPlay())
            {
                return;
            }
            
            AudionElement element = _in.resources.GetResource(eventName, SoundType.Music);
            _in.Manager.PlayMusic(element);
        }

        public static void StartBattleAudio()
        {
            if (!CanPlay())
            {
                return;
            }

            if (_battleAudioActive)
            {
                return;
            }

            _battleAudioActive = true;
            PlayMusic(AudioEventNames.BattleMusic);
        }

        public static void StopBattleAudio()
        {
            bool wasActive = _battleAudioActive;
            _battleAudioActive = false;

            if (!CanPlay())
            {
                return;
            }

            if (!wasActive)
            {
                return;
            }

            StopMusic();
            PlayUi(AudioEventNames.UiBattleFinished);
        }

        public void ChangeVolume(float volume, SoundType type)
        {
            Manager?.SetVolume(volume, type);
        }

        public void SetRuntimeSfxSpatialSettings(
            float minDistance,
            float maxDistance,
            AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic)
        {
            sfxMinDistance = minDistance;
            sfxMaxDistance = maxDistance;
            sfxRolloffMode = rolloffMode;
            SyncRuntimeSfxSpatialSettings(true);
        }

        private void SyncRuntimeSfxSpatialSettings(bool force = false)
        {
            if (Manager == null)
            {
                return;
            }

            float minDistance = Mathf.Max(AudioManager.DefaultSfxMinDistance, sfxMinDistance);
            float maxDistance = Mathf.Max(minDistance, sfxMaxDistance);
            if (!force
                && Mathf.Approximately(_appliedSfxMinDistance, minDistance)
                && Mathf.Approximately(_appliedSfxMaxDistance, maxDistance)
                && _appliedSfxRolloffMode == sfxRolloffMode)
            {
                return;
            }

            _appliedSfxMinDistance = minDistance;
            _appliedSfxMaxDistance = maxDistance;
            _appliedSfxRolloffMode = sfxRolloffMode;
            Manager.SetSfxSpatialSettings(minDistance, maxDistance, sfxRolloffMode);
        }

        private static void HandleMenuOpened(MenuType type)
        {
            if (!CanPlay())
            {
                return;
            }

            if (type == MenuType.MainMenu)
            {
                if (_battleAudioActive)
                {
                    StopBattleAudio();
                }

                PlayMusic(AudioEventNames.GarageMain);
            }
            else if (type == MenuType.DevelopmentTree)
            {
                PlayMusic(AudioEventNames.GarageDevelopmentTree);
                PlayUi(AudioEventNames.UiDevelopmentTreeOpen);
            }
            else if (type == MenuType.EndGame)
            {
                StopBattleAudio();
                PlayMusic(AudioEventNames.BattleResultMusic);
            }
            else if (type == MenuType.GameplayHUD)
            {
                StartBattleAudio();
            }
            else if (type == MenuType.GameResult)
            {
                PlayMusic(AudioEventNames.BattleResultMusic);
            }
        }
    }
}
