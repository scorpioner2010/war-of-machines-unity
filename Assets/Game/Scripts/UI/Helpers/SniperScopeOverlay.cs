using UnityEngine;
using Game.Scripts.Client;
using Game.Scripts.UI.Screens;

namespace Game.Scripts.UI.Helpers
{
    public class SniperScopeOverlay : UIScreenBase<SniperScopeOverlay>
    {
        [SerializeField] private CanvasGroup canvasGroup;

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

            if (isActive && !_instance.gameObject.activeSelf)
            {
                _instance.gameObject.SetActive(true);
            }

            _instance.SetShown(isActive);
        }

        private void OnDisable()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
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
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float fadeDuration = settings.sniperScopeOverlayFadeDuration;
            if (fadeDuration <= 0f)
            {
                SetAlpha(targetAlpha);
                return;
            }

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                SetAlpha(targetAlpha);
                return;
            }

            _fadeRoutine = StartCoroutine(FadeTo(targetAlpha));
        }

        private System.Collections.IEnumerator FadeTo(float targetAlpha)
        {
            float fadeDuration = GameplayRuntimeSettingsProvider.Get().sniperScopeOverlayFadeDuration;
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
                return;
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
