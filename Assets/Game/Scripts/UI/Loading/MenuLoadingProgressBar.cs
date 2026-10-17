using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Loading
{
    /// <summary>
    /// Displays map-loading progress with a fixed set of UI segments.
    /// </summary>
    public class MenuLoadingProgressBar : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float progress;
        [SerializeField] private Image[] segments;
        [SerializeField] private Sprite fullSprite;
        [SerializeField] private Sprite afterFullSprite;

        public float Progress => progress;

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnValidate()
        {
            progress = Mathf.Clamp01(progress);
            Refresh();
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            Refresh();
        }

        private void Refresh()
        {
            if (segments == null || segments.Length == 0)
            {
                return;
            }

            int fullSegmentCount = Mathf.FloorToInt(progress * segments.Length);

            for (int index = 0; index < segments.Length; index++)
            {
                Image segment = segments[index];
                if (segment == null)
                {
                    continue;
                }

                bool isFull = index < fullSegmentCount;
                bool isAfterFull = index == fullSegmentCount && fullSegmentCount < segments.Length;
                segment.gameObject.SetActive(isFull || isAfterFull);

                if (isFull)
                {
                    segment.sprite = fullSprite;
                    continue;
                }

                if (isAfterFull)
                {
                    segment.sprite = afterFullSprite;
                }
            }
        }
    }
}
