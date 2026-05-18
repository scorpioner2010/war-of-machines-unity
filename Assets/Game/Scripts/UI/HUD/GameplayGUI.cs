using System.Collections;
using Game.Scripts.Client;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class GameplayGUI : MonoBehaviour
    {
        public static GameplayGUI In;
        public Transform ShotResultTextTransform => isPenetrationText != null ? isPenetrationText.transform : null;
        
        [SerializeField] private TMP_Text isPenetrationText;

        private Coroutine _shotResultRoutine;

        public void Awake()
        {
            In = this;

            if (isPenetrationText != null)
            {
                isPenetrationText.text = string.Empty;
            }
        }
        
        public void UpdateHealth(float healthPercentage)
        {
           
        }

        public void ShowShotResult(string message)
        {
            if (isPenetrationText == null)
            {
                return;
            }

            if (_shotResultRoutine != null)
            {
                StopCoroutine(_shotResultRoutine);
            }

            _shotResultRoutine = StartCoroutine(ShowShotResultRoutine(message));
        }

        private IEnumerator ShowShotResultRoutine(string message)
        {
            isPenetrationText.text = message;
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            yield return new WaitForSeconds(settings.shotResultVisibleTime);
            isPenetrationText.text = string.Empty;
            _shotResultRoutine = null;
        }

        public void OnDestroy()
        {
            In = null;
        }
    }
}
