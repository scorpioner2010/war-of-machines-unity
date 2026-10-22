using UnityEngine;

namespace Game.Scripts.UI.MainMenu
{
    public class VehicleBlink : MonoBehaviour
    {
        [Header("Blink")]
        [SerializeField] private CanvasGroup blackScreen;
        [SerializeField, Range(0f, 1f)] private float alphaA = 0.5f;
        [SerializeField, Range(0f, 1f)] private float alphaB = 1f;
        [SerializeField, Min(0f)] private float blinkSpeed = 1f;

        private float cycleProgress;
        private float authoredAlpha = 1f;
        private bool isBlinking;

        private void Awake()
        {
            if (blackScreen != null)
            {
                authoredAlpha = blackScreen.alpha;
            }
        }

        private void OnEnable()
        {
            cycleProgress = 0f;
            isBlinking = false;
            RestoreAuthoredAlpha();
            SetBlackScreenActive(false);
        }

        private void OnDisable()
        {
            isBlinking = false;
            RestoreAuthoredAlpha();
            SetBlackScreenActive(false);
        }

        public void SetBlinking(bool blinking)
        {
            if (isBlinking == blinking)
            {
                SetBlackScreenActive(blinking);
                return;
            }

            isBlinking = blinking;
            cycleProgress = 0f;

            if (isBlinking)
            {
                SetBlackScreenActive(true);
                ApplyValue(0f);
                return;
            }

            RestoreAuthoredAlpha();
            SetBlackScreenActive(false);
        }

        private void Update()
        {
            if (!isBlinking || blackScreen == null || blinkSpeed <= 0f)
            {
                return;
            }

            cycleProgress = Mathf.Repeat(cycleProgress + Time.unscaledDeltaTime * blinkSpeed, 1f);
            float pingPong = Mathf.PingPong(cycleProgress * 2f, 1f);
            ApplyValue(Mathf.SmoothStep(0f, 1f, pingPong));
        }

        private void ApplyValue(float normalizedValue)
        {
            if (blackScreen == null)
            {
                return;
            }

            blackScreen.alpha = Mathf.Lerp(alphaA, alphaB, normalizedValue);
        }

        private void RestoreAuthoredAlpha()
        {
            if (blackScreen == null)
            {
                return;
            }

            blackScreen.alpha = authoredAlpha;
        }

        private void SetBlackScreenActive(bool active)
        {
            if (blackScreen == null || blackScreen.gameObject.activeSelf == active)
            {
                return;
            }

            blackScreen.gameObject.SetActive(active);
        }
    }
}
