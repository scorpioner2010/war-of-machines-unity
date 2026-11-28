using System.Collections.Generic;
using RAV.Scripts.API;
using RAV.Scripts.Client;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.UI.Tree
{
    public sealed class ArrowDrawer : MonoBehaviour
    {
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private Image arrowHeadPrefab;

        private readonly TreeArrowRoutePlanner _routePlanner = new TreeArrowRoutePlanner();
        private readonly TreeArrowRouteRenderer _routeRenderer = new TreeArrowRouteRenderer();
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

        public void Draw(
            IEnumerable<VehicleEdge> edges,
            Dictionary<int, RectTransform> nodeById,
            RectTransform layer)
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
            if (segmentPrefab == null || arrowHeadPrefab == null || edges == null || nodeById == null)
            {
                _routeRenderer.Clear(layer);
                return;
            }

            IReadOnlyList<TreeArrowRoute> routes = _routePlanner.Plan(
                edges,
                nodeById,
                layer,
                settings.treeArrowItemOffset,
                settings.treeArrowShaftThickness);

            _routeRenderer.Draw(
                routes,
                layer,
                segmentPrefab,
                arrowHeadPrefab,
                settings.treeArrowShaftThickness,
                settings.treeArrowColor,
                settings.treeArrowHeadColor,
                settings.treeArrowHeadHeight,
                settings.treeArrowHeadScale,
                settings.treeArrowHeadOffset);
            StoreRuntimeAppearance(settings);
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
    }
}
