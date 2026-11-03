using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Testing
{
    [DefaultExecutionOrder(-9990)]
    [DisallowMultipleComponent]
    public sealed class ClientVehicleTestAudioRouting : MonoBehaviour
    {
        private readonly HashSet<AudioListener> _disabledListeners = new HashSet<AudioListener>();
        private AudioListener _testListener;

        private void Awake()
        {
            _testListener = GetComponent<AudioListener>();
            if (_testListener != null)
            {
                _testListener.enabled = false;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            SynchronizeListeners();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreDisabledListeners();

            if (_testListener != null)
            {
                _testListener.enabled = false;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            RestoreDisabledListeners();

            if (_testListener != null)
            {
                _testListener.enabled = false;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SynchronizeListeners();
        }

        private void SynchronizeListeners()
        {
            if (_testListener == null)
            {
                return;
            }

            if (!ClientVehicleTestMode.IsEnabled)
            {
                _testListener.enabled = false;
                return;
            }

            _testListener.enabled = true;
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null || listener == _testListener || !listener.enabled)
                {
                    continue;
                }

                listener.enabled = false;
                _disabledListeners.Add(listener);
            }
        }

        private void RestoreDisabledListeners()
        {
            foreach (AudioListener listener in _disabledListeners)
            {
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }

            _disabledListeners.Clear();
        }
    }
}
