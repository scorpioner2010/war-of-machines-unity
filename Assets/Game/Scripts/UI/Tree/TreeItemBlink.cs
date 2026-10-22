using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class TreeItemBlink : MonoBehaviour
    {
        [Header("Blink")]
        [SerializeField] private Image[] images;
        [SerializeField, Range(0f, 1f)] private float alphaA = 1f;
        [SerializeField, Range(0f, 1f)] private float alphaB = 1f;
        [SerializeField] private float scaleYA = 1f;
        [SerializeField] private float scaleYB = 1f;
        [SerializeField, Min(0f)] private float blinkSpeed = 1f;

        private float cycleProgress;

        private void OnEnable()
        {
            cycleProgress = 0f;
            ApplyValue(0f);
        }

        private void Update()
        {
            if (images == null || images.Length == 0 || blinkSpeed <= 0f)
            {
                return;
            }

            cycleProgress = Mathf.Repeat(cycleProgress + Time.unscaledDeltaTime * blinkSpeed, 1f);
            float pingPong = Mathf.PingPong(cycleProgress * 2f, 1f);
            ApplyValue(Mathf.SmoothStep(0f, 1f, pingPong));
        }

        private void ApplyValue(float normalizedValue)
        {
            if (images == null || images.Length == 0)
            {
                return;
            }

            float alpha = Mathf.Lerp(alphaA, alphaB, normalizedValue);
            float scaleY = Mathf.Lerp(scaleYA, scaleYB, normalizedValue);

            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                {
                    continue;
                }

                Color color = image.color;
                color.a = alpha;
                image.color = color;

                Vector3 localScale = image.transform.localScale;
                localScale.y = scaleY;
                image.transform.localScale = localScale;
            }
        }
    }
}
