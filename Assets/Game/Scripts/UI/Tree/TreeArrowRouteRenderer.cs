using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeArrowRouteRenderer
    {
        public void Draw(
            IReadOnlyList<TreeArrowRoute> routes,
            RectTransform layer,
            Image segmentPrefab,
            Image arrowHeadPrefab,
            float shaftThickness,
            Color arrowColor,
            Color arrowHeadColor,
            float arrowHeadHeight,
            float arrowHeadScale,
            float arrowHeadOffset)
        {
            Clear(layer);
            if (routes == null || layer == null || segmentPrefab == null || arrowHeadPrefab == null)
            {
                return;
            }

            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                DrawRoute(
                    routes[routeIndex].Points,
                    layer,
                    segmentPrefab,
                    arrowHeadPrefab,
                    shaftThickness,
                    arrowColor,
                    arrowHeadColor,
                    arrowHeadHeight,
                    arrowHeadScale,
                    arrowHeadOffset,
                    routeIndex);
            }
        }

        public void Clear(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            for (int childIndex = layer.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = layer.GetChild(childIndex);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DrawRoute(
            IReadOnlyList<Vector2> points,
            RectTransform layer,
            Image segmentPrefab,
            Image arrowHeadPrefab,
            float shaftThickness,
            Color arrowColor,
            Color arrowHeadColor,
            float arrowHeadHeight,
            float arrowHeadScale,
            float arrowHeadOffset,
            int routeIndex)
        {
            if (points == null || points.Count < 2)
            {
                return;
            }

            for (int segmentIndex = 0; segmentIndex < points.Count - 1; segmentIndex++)
            {
                CreateSegment(
                    points[segmentIndex],
                    points[segmentIndex + 1],
                    layer,
                    segmentPrefab,
                    shaftThickness,
                    arrowColor,
                    routeIndex,
                    segmentIndex);
            }

            Vector2 direction = points[points.Count - 1] - points[points.Count - 2];
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            CreateArrowHead(
                points[points.Count - 1],
                direction.normalized,
                layer,
                arrowHeadPrefab,
                arrowHeadColor,
                arrowHeadHeight,
                arrowHeadScale,
                arrowHeadOffset,
                routeIndex);
        }

        private static void CreateSegment(
            Vector2 start,
            Vector2 end,
            RectTransform layer,
            Image segmentPrefab,
            float shaftThickness,
            Color arrowColor,
            int routeIndex,
            int segmentIndex)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length <= Mathf.Epsilon)
            {
                return;
            }

            Image image = Object.Instantiate(segmentPrefab, layer);
            image.name = $"TreeArrowSegment_{routeIndex}_{segmentIndex}";
            image.raycastTarget = false;
            image.color = arrowColor;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = (start + end) * 0.5f;
            rectTransform.sizeDelta = new Vector2(length, shaftThickness);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private static void CreateArrowHead(
            Vector2 endpoint,
            Vector2 direction,
            RectTransform layer,
            Image arrowHeadPrefab,
            Color arrowHeadColor,
            float arrowHeadHeight,
            float arrowHeadScale,
            float arrowHeadOffset,
            int routeIndex)
        {
            Image image = Object.Instantiate(arrowHeadPrefab, layer);
            image.name = $"TreeArrowHead_{routeIndex}";
            image.raycastTarget = false;
            image.color = arrowHeadColor;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.localScale = Vector3.one * arrowHeadScale;
            rectTransform.anchoredPosition = endpoint + direction * arrowHeadOffset;

            float aspect = image.sprite == null || image.sprite.rect.height <= Mathf.Epsilon
                ? 1f
                : image.sprite.rect.width / image.sprite.rect.height;
            rectTransform.sizeDelta = new Vector2(arrowHeadHeight * aspect, arrowHeadHeight);
            rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            rectTransform.SetAsLastSibling();
        }
    }
}
