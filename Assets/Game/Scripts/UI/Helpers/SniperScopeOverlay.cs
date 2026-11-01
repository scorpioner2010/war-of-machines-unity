using UnityEngine;
using Game.Scripts.Client;
using Game.Scripts.Gameplay.Battle;
using Game.Scripts.UI.Screens;

namespace Game.Scripts.UI.Helpers
{
    public class SniperScopeOverlay : UIScreenBase<SniperScopeOverlay>
    {
        [SerializeField] private CanvasGroup canvasGroup;

        private static SniperScopeOverlay _instance;
        private Coroutine _fadeRoutine;
        private bool _isShown;
        private bool _battleCountdownHidden;
        private const float BattleCountdownRevealDuration = 2f;

        protected override void Awake()
        {
            _instance = this;
            EnsureCanvasGroup();
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            BattleCountdownGate.ClientCountdownStateChanged += OnBattleCountdownStateChanged;
            if (BattleCountdownGate.IsClientCountdownActive)
            {
                ApplyBattleCountdownState(true);
            }
            else
            {
                _battleCountdownHidden = false;
                SetAlpha(_isShown ? 1f : 0f);
            }
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
            BattleCountdownGate.ClientCountdownStateChanged -= OnBattleCountdownStateChanged;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
        }

        private void OnBattleCountdownStateChanged(bool isActive)
        {
            ApplyBattleCountdownState(isActive);
        }

        private void ApplyBattleCountdownState(bool isActive)
        {
            _battleCountdownHidden = isActive;
            if (isActive)
            {
                if (_fadeRoutine != null)
                {
                    StopCoroutine(_fadeRoutine);
                    _fadeRoutine = null;
                }

                SetAlpha(0f);
                return;
            }

            if (_isShown && isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                if (_fadeRoutine != null)
                {
                    StopCoroutine(_fadeRoutine);
                }

                _fadeRoutine = StartCoroutine(FadeTo(1f, BattleCountdownRevealDuration));
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

            float targetAlpha = shown && !_battleCountdownHidden ? 1f : 0f;
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float fadeDuration = _battleCountdownHidden
                ? 0f
                : settings.sniperScopeOverlayFadeDuration;
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

            _fadeRoutine = StartCoroutine(FadeTo(targetAlpha, fadeDuration));
        }

        private System.Collections.IEnumerator FadeTo(float targetAlpha, float fadeDuration)
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
