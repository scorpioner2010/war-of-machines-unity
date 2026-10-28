using System.Collections.Generic;
using Game.Scripts.API;
using Game.Scripts.Client;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public sealed class ArrowDrawer : MonoBehaviour
    {
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private Image arrowHeadPrefab;

        private readonly TreeArrowRouteBuilder _routeBuilder = new TreeArrowRouteBuilder();
        private readonly List<DrawRequest> _drawRequests = new List<DrawRequest>();
        private bool _hasRuntimeAppearance;
        private float _lastShaftThickness;
        private Color _lastArrowColor;
        private Color _lastArrowHeadColor;
        private float _lastArrowHeadHeight;
        private float _lastArrowHeadScale;
        private float _lastArrowHeadOffset;
        private float _lastItemOffset;

        private sealed class DrawRequest
        {
            public readonly List<VehicleEdge> Edges = new List<VehicleEdge>();
            public Dictionary<int, RectTransform> NodeById;
            public RectTransform Layer;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || _drawRequests.Count == 0)
            {
                return;
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            if (!HasRuntimeAppearanceChanged(settings))
            {
                return;
            }

            for (int requestIndex = _drawRequests.Count - 1; requestIndex >= 0; requestIndex--)
            {
                DrawRequest request = _drawRequests[requestIndex];
                if (request.Layer == null)
                {
                    _drawRequests.RemoveAt(requestIndex);
                    continue;
                }

                DrawInternal(request.Edges, request.NodeById, request.Layer, settings);
            }
        }

        public void Draw(IEnumerable<VehicleEdge> edges, Dictionary<int, RectTransform> nodeById, RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            DrawRequest request = GetOrCreateRequest(layer);
            request.Edges.Clear();
            if (edges != null)
            {
                request.Edges.AddRange(edges);
            }

            request.NodeById = nodeById;
            DrawInternal(request.Edges, request.NodeById, request.Layer, GameplayRuntimeSettingsProvider.Get());
        }

        private void DrawInternal(
            IEnumerable<VehicleEdge> edges,
            Dictionary<int, RectTransform> nodeById,
            RectTransform layer,
            GameplayRuntimeSettings settings)
        {
            ClearLayer(layer);

            if (segmentPrefab == null || arrowHeadPrefab == null || edges == null || nodeById == null)
            {
                return;
            }

            foreach (VehicleEdge edge in edges)
            {
                if (edge == null
                    || !nodeById.TryGetValue(edge.fromId, out RectTransform from)
                    || !nodeById.TryGetValue(edge.toId, out RectTransform to))
                {
                    continue;
                }

                DrawRoute(
                    _routeBuilder.Build(from, to, layer, settings.treeArrowItemOffset),
                    layer,
                    settings.treeArrowShaftThickness,
                    settings.treeArrowColor,
                    settings.treeArrowHeadColor,
                    settings.treeArrowHeadHeight,
                    settings.treeArrowHeadScale,
                    settings.treeArrowHeadOffset);
            }

            StoreRuntimeAppearance(settings);
        }

        private void DrawRoute(
            IReadOnlyList<Vector2> points,
            RectTransform layer,
            float currentShaftThickness,
            Color currentArrowColor,
            Color currentArrowHeadColor,
            float currentArrowHeadHeight,
            float currentArrowHeadScale,
            float currentArrowHeadOffset)
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
                    segmentIndex,
                    currentShaftThickness,
                    currentArrowColor);
            }

            Vector2 direction = points[points.Count - 1] - points[points.Count - 2];
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                CreateArrowHead(
                    points[points.Count - 1],
                    direction.normalized,
                    layer,
                    currentArrowHeadColor,
                    currentArrowHeadHeight,
                    currentArrowHeadScale,
                    currentArrowHeadOffset);
            }
        }

        private void CreateSegment(
            Vector2 start,
            Vector2 end,
            RectTransform layer,
            int segmentIndex,
            float currentShaftThickness,
            Color currentArrowColor)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length <= Mathf.Epsilon)
            {
                return;
            }

            Image image = Instantiate(segmentPrefab, layer);
            image.name = $"TreeArrowSegment_{segmentIndex}";
            image.raycastTarget = false;
            image.color = currentArrowColor;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = (start + end) * 0.5f;
            rectTransform.sizeDelta = new Vector2(length, currentShaftThickness);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void CreateArrowHead(
            Vector2 endpoint,
            Vector2 direction,
            RectTransform layer,
            Color currentArrowColor,
            float currentArrowHeadHeight,
            float currentArrowHeadScale,
            float currentArrowHeadOffset)
        {
            Image image = Instantiate(arrowHeadPrefab, layer);
            image.name = "TreeArrowHead";
            image.raycastTarget = false;
            image.color = currentArrowColor;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.localScale = Vector3.one * currentArrowHeadScale;
            rectTransform.anchoredPosition = endpoint + direction * currentArrowHeadOffset;

            float aspect = image.sprite == null || image.sprite.rect.height <= Mathf.Epsilon
                ? 1f
                : image.sprite.rect.width / image.sprite.rect.height;
            rectTransform.sizeDelta = new Vector2(currentArrowHeadHeight * aspect, currentArrowHeadHeight);
            rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            rectTransform.SetAsLastSibling();
        }

        private DrawRequest GetOrCreateRequest(RectTransform layer)
        {
            for (int requestIndex = 0; requestIndex < _drawRequests.Count; requestIndex++)
            {
                if (_drawRequests[requestIndex].Layer == layer)
                {
                    return _drawRequests[requestIndex];
                }
            }

            DrawRequest request = new DrawRequest { Layer = layer };
            _drawRequests.Add(request);
            return request;
        }

        private bool HasRuntimeAppearanceChanged(GameplayRuntimeSettings settings)
        {
            return !_hasRuntimeAppearance
                || !Mathf.Approximately(_lastShaftThickness, settings.treeArrowShaftThickness)
                || !ColorsApproximatelyEqual(_lastArrowColor, settings.treeArrowColor)
                || !ColorsApproximatelyEqual(_lastArrowHeadColor, settings.treeArrowHeadColor)
                || !Mathf.Approximately(_lastArrowHeadHeight, settings.treeArrowHeadHeight)
                || !Mathf.Approximately(_lastArrowHeadScale, settings.treeArrowHeadScale)
                || !Mathf.Approximately(_lastArrowHeadOffset, settings.treeArrowHeadOffset)
                || !Mathf.Approximately(_lastItemOffset, settings.treeArrowItemOffset);
        }

        private void StoreRuntimeAppearance(GameplayRuntimeSettings settings)
        {
            _hasRuntimeAppearance = true;
            _lastShaftThickness = settings.treeArrowShaftThickness;
            _lastArrowColor = settings.treeArrowColor;
            _lastArrowHeadColor = settings.treeArrowHeadColor;
            _lastArrowHeadHeight = settings.treeArrowHeadHeight;
            _lastArrowHeadScale = settings.treeArrowHeadScale;
            _lastArrowHeadOffset = settings.treeArrowHeadOffset;
            _lastItemOffset = settings.treeArrowItemOffset;
        }

        private static bool ColorsApproximatelyEqual(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r)
                && Mathf.Approximately(left.g, right.g)
                && Mathf.Approximately(left.b, right.b)
                && Mathf.Approximately(left.a, right.a);
        }

        private static void ClearLayer(RectTransform layer)
        {
            for (int childIndex = layer.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = layer.GetChild(childIndex);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
