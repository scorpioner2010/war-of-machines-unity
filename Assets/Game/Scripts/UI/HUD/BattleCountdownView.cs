using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class BattleCountdownView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField, Min(0.05f)] private float tickDuration = 1f;
        [SerializeField, Min(0.01f)] private float entryDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float hiddenScale = 0.72f;
        [SerializeField, Min(0.1f)] private float visibleScale = 1f;

        private bool _isVisible;
        private float _displayStartedAt;

        private void Awake()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            animatedRoot ??= transform as RectTransform;
            Hide();
        }

        private void Update()
        {
            if (!_isVisible)
            {
                return;
            }

            float elapsed = Time.unscaledTime - _displayStartedAt;
            if (elapsed >= tickDuration)
            {
                Hide();
                return;
            }

            float entryProgress = Mathf.Clamp01(elapsed / entryDuration);
            float exitStart = Mathf.Max(entryDuration, tickDuration - exitDuration);
            float exitProgress = Mathf.Clamp01((elapsed - exitStart) / exitDuration);
            float scale = Mathf.Lerp(hiddenScale, visibleScale, EaseOutBack(entryProgress));
            scale *= Mathf.Lerp(1f, 0.96f, exitProgress);

            if (animatedRoot != null)
            {
                animatedRoot.localScale = Vector3.one * scale;
            }

            if (canvasGroup != null)
            {
                float alpha = 1f - exitProgress;
                canvasGroup.alpha = alpha;
            }
        }

        public void ShowNumber(int number)
        {
            if (countdownText != null)
            {
                countdownText.SetText("{0}", number);
            }

            _displayStartedAt = Time.unscaledTime;
            _isVisible = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (animatedRoot != null)
            {
                animatedRoot.localScale = Vector3.one * hiddenScale;
            }
        }

        public void Hide()
        {
            _isVisible = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (animatedRoot != null)
            {
                animatedRoot.localScale = Vector3.one * hiddenScale;
            }
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float inverse = value - 1f;
            return inverse * inverse * ((overshoot + 1f) * inverse + overshoot) + 1f;
        }
    }
}
