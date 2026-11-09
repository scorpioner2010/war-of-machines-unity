using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Audio
{
    [DefaultExecutionOrder(-10010)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public sealed class AudioListenerRoot : MonoBehaviour
    {
        private const string GameplayCameraName = "GameplayCamera";
        private static AudioListenerRoot _instance;

        [SerializeField] private Camera initialTarget;

        private AudioListener _listener;
        private Camera _targetCamera;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _listener = GetComponent<AudioListener>();
            _targetCamera = initialTarget;
            EnsureSingleListener();
            SyncToTarget();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            if (_instance == this)
            {
                EnsureSingleListener();
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void LateUpdate()
        {
            SyncToTarget();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingleListener();
        }

        public static void SetTarget(Camera target)
        {
            if (_instance == null)
            {
                return;
            }

            _instance._targetCamera = target != null ? target : _instance.initialTarget;
            _instance.SyncToTarget();
        }

        public static bool TrySetGameplayTarget(Scene scene)
        {
            if (_instance == null || !scene.IsValid())
            {
                return false;
            }

            Camera gameplayCamera = FindCamera(scene, GameplayCameraName);
            if (gameplayCamera == null)
            {
                return false;
            }

            SetTarget(gameplayCamera);
            return true;
        }

        private static Camera FindCamera(Scene scene, string cameraName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform candidate = transforms[j];
                    if (candidate.name == cameraName
                        && candidate.TryGetComponent(out Camera camera))
                    {
                        return camera;
                    }
                }
            }

            return null;
        }

        private void EnsureSingleListener()
        {
            if (_listener == null)
            {
                return;
            }

            _listener.enabled = true;
            AudioListener[] listeners = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null && listener != _listener)
                {
                    listener.enabled = false;
                }
            }
        }

        private void SyncToTarget()
        {
            if (_targetCamera == null)
            {
                _targetCamera = initialTarget;
            }

            if (_targetCamera != null)
            {
                transform.SetPositionAndRotation(
                    _targetCamera.transform.position,
                    _targetCamera.transform.rotation);
            }
        }
    }
}
