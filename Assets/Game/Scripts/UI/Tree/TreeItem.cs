using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class TreeItem : MonoBehaviour
    {
        public Button button;
        public TMP_Text vehicleName;
        public TMP_Text level;
        public TMP_Text priceValue;
        public GameObject priceText;
        public CanvasGroup priceValueCanvasGroup;
        public CanvasGroup priceTextCanvasGroup;
        public TMP_Text expValue;
        public GameObject expText;
        public CanvasGroup expValueCanvasGroup;
        public CanvasGroup expTextCanvasGroup;
        public Image image;
        public VehicleClass vehicleType;
        public GameObject isClose;
        public CanvasGroup isCloseCanvasGroup;
        public GameObject isHave;
        public CanvasGroup isHaveCanvasGroup;
        public GameObject isSelect;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;

        private const float StateFadeDuration = 0.4f;
        private CancellationTokenSource _stateFadeCancellation;

        private void OnDestroy()
        {
            if (_stateFadeCancellation == null)
            {
                return;
            }

            _stateFadeCancellation.Cancel();
            _stateFadeCancellation.Dispose();
            _stateFadeCancellation = null;
        }

        public void ApplyVisualState(
            int purchaseCost,
            int researchCost,
            bool owned,
            bool researched,
            bool locked,
            bool unavailable)
        {
            if (image != null)
            {
                image.color = unavailable ? Color.black : Color.white;
            }

            bool showPurchaseCost = unavailable || !owned;
            bool showResearchCost = unavailable || !researched;
            string purchaseValue = unavailable ? "~~~" : FormatResourceValue(purchaseCost);
            string researchValue = unavailable ? "~~~" : FormatResourceValue(researchCost);

            if (priceValue != null)
            {
                priceValue.text = purchaseValue;
            }

            if (expValue != null)
            {
                expValue.text = researchValue;
            }

            _stateFadeCancellation?.Cancel();
            _stateFadeCancellation?.Dispose();
            _stateFadeCancellation = new CancellationTokenSource();
            FadeStateAsync(
                showPurchaseCost,
                showResearchCost,
                owned,
                locked,
                _stateFadeCancellation.Token).Forget();
        }

        public void SetSelected(bool selected)
        {
            if (isSelect != null)
            {
                isSelect.SetActive(selected);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = selected ? 1f : 0.35f;
            }
        }

        private async UniTaskVoid FadeStateAsync(
            bool showPurchaseCost,
            bool showResearchCost,
            bool showOwned,
            bool showLocked,
            CancellationToken cancellationToken)
        {
            float priceValueStart = PrepareGroup(
                priceValueCanvasGroup,
                priceValue != null ? priceValue.gameObject : null,
                showPurchaseCost);
            float priceTextStart = PrepareGroup(
                priceTextCanvasGroup,
                priceText,
                showPurchaseCost);
            float expValueStart = PrepareGroup(
                expValueCanvasGroup,
                expValue != null ? expValue.gameObject : null,
                showResearchCost);
            float expTextStart = PrepareGroup(
                expTextCanvasGroup,
                expText,
                showResearchCost);
            float ownedStart = PrepareGroup(isHaveCanvasGroup, isHave, showOwned);
            float lockedStart = PrepareGroup(isCloseCanvasGroup, isClose, showLocked);

            try
            {
                float elapsed = 0f;
                while (elapsed < StateFadeDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float progress = Mathf.Clamp01(elapsed / StateFadeDuration);
                    float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

                    SetAlpha(priceValueCanvasGroup, priceValueStart, showPurchaseCost, easedProgress);
                    SetAlpha(priceTextCanvasGroup, priceTextStart, showPurchaseCost, easedProgress);
                    SetAlpha(expValueCanvasGroup, expValueStart, showResearchCost, easedProgress);
                    SetAlpha(expTextCanvasGroup, expTextStart, showResearchCost, easedProgress);
                    SetAlpha(isHaveCanvasGroup, ownedStart, showOwned, easedProgress);
                    SetAlpha(isCloseCanvasGroup, lockedStart, showLocked, easedProgress);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    elapsed += Time.unscaledDeltaTime;
                }

                cancellationToken.ThrowIfCancellationRequested();
                CompleteGroup(priceValueCanvasGroup, priceValue != null ? priceValue.gameObject : null, showPurchaseCost);
                CompleteGroup(priceTextCanvasGroup, priceText, showPurchaseCost);
                CompleteGroup(expValueCanvasGroup, expValue != null ? expValue.gameObject : null, showResearchCost);
                CompleteGroup(expTextCanvasGroup, expText, showResearchCost);
                CompleteGroup(isHaveCanvasGroup, isHave, showOwned);
                CompleteGroup(isCloseCanvasGroup, isClose, showLocked);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static float PrepareGroup(CanvasGroup group, GameObject fallback, bool visible)
        {
            if (group == null)
            {
                if (fallback != null)
                {
                    fallback.SetActive(visible);
                }

                return -1f;
            }

            if (visible && !group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
                group.alpha = 0f;
            }

            return group.alpha;
        }

        private static void SetAlpha(CanvasGroup group, float start, bool visible, float progress)
        {
            if (group == null || start < 0f)
            {
                return;
            }

            float target = visible ? 1f : 0f;
            group.alpha = Mathf.Lerp(start, target, progress);
        }

        private static void CompleteGroup(CanvasGroup group, GameObject fallback, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.gameObject.SetActive(visible);
        }

        private static string FormatResourceValue(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
        }
    }
}
