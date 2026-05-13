using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class GameplayGUI : MonoBehaviour
    {
        public static GameplayGUI In;
        public Transform ShotResultTextTransform => isPenetrationText != null ? isPenetrationText.transform : null;
        
        [SerializeField] private TMP_Text isPenetrationText;
        [SerializeField] private float shotResultVisibleTime = 4f;

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
            yield return new WaitForSeconds(shotResultVisibleTime);
            isPenetrationText.text = string.Empty;
            _shotResultRoutine = null;
        }

        public void OnDestroy()
        {
            In = null;
        }
    }
}
