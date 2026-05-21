using FishNet;
using FishNet.Managing.Timing;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public sealed class PerformanceStatsHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Vector2 anchoredPosition = new Vector2(14f, -14f);
        [SerializeField] private Vector2 sizeDelta = new Vector2(180f, 44f);
        [SerializeField] private float fontSize = 15f;
        [SerializeField] private float updateInterval = 0.25f;

        private float _nextUpdateTime;
        private int _frameCount;
        private float _frameWindowStart;
        private int _lastFps = -1;
        private long _lastPing = -1;

        private void Awake()
        {
            EnsureText();
            _frameWindowStart = Time.unscaledTime;
            _nextUpdateTime = Time.unscaledTime;
        }

        private void Update()
        {
            _frameCount++;

            float now = Time.unscaledTime;
            if (now < _nextUpdateTime)
            {
                return;
            }

            float elapsed = Mathf.Max(0.001f, now - _frameWindowStart);
            int fps = Mathf.RoundToInt(_frameCount / elapsed);
            _frameCount = 0;
            _frameWindowStart = now;
            _nextUpdateTime = now + Mathf.Max(0.1f, updateInterval);

            long ping = GetPing();
            if (fps == _lastFps && ping == _lastPing)
            {
                return;
            }

            _lastFps = fps;
            _lastPing = ping;
            if (statsText != null)
            {
                statsText.SetText("FPS {0}\nPING {1} ms", fps, ping);
            }
        }

        private long GetPing()
        {
            TimeManager timeManager = InstanceFinder.TimeManager;
            if (timeManager == null)
            {
                return 0;
            }

            long ping = timeManager.RoundTripTime;
            long tickDeduction = (long)(timeManager.TickDelta * 2000d);
            return (long)Mathf.Max(1f, ping - tickDeduction);
        }

        private void EnsureText()
        {
            if (statsText == null)
            {
                GameObject textObject = new GameObject("PerformanceStatsText", typeof(RectTransform));
                textObject.transform.SetParent(transform, false);
                RectTransform rectTransform = textObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;

                statsText = textObject.AddComponent<TextMeshProUGUI>();
            }

            statsText.raycastTarget = false;
            statsText.fontSize = Mathf.Max(8f, fontSize);
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.textWrappingMode = TextWrappingModes.NoWrap;
            statsText.overflowMode = TextOverflowModes.Overflow;
            statsText.color = Color.white;
            statsText.text = "FPS --\nPING -- ms";
        }
    }
}
