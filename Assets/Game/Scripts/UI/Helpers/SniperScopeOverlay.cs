using UnityEngine;
using Game.Scripts.UI.Screens;

namespace Game.Scripts.UI.Helpers
{
    public class SniperScopeOverlay : UIScreenBase<SniperScopeOverlay>
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;

        private static SniperScopeOverlay _instance;
        private Coroutine _fadeRoutine;
        private bool _isShown;

        protected override void Awake()
        {
            _instance = this;
            EnsureCanvasGroup();
            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public new static void SetActiveScreen(bool isActive)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.SetShown(isActive);
        }

        private void SetShown(bool shown)
        {
            EnsureCanvasGroup();
            if (canvasGroup == null)
            {
                return;
            }

            if (_isShown == shown && _fadeRoutine == null)
            {
                return;
            }

            _isShown = shown;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            float targetAlpha = shown ? 1f : 0f;
            if (fadeDuration <= 0f)
            {
                SetAlpha(targetAlpha);
                return;
            }

            _fadeRoutine = StartCoroutine(FadeTo(targetAlpha));
        }

        private System.Collections.IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlpha(targetAlpha);
            _fadeRoutine = null;
        }

        private void EnsureCanvasGroup()
        {
            if (screen == null)
            {
                return;
            }

            if (!screen.activeSelf)
            {
                screen.SetActive(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = screen.GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = screen.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
}
