using System.Collections.Generic;
using RAV.Scripts.API;
using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeArrowRouteValidator
    {
        private const float Epsilon = 0.01f;

        public bool TryValidate(
            IReadOnlyList<TreeArrowRoute> routes,
            Dictionary<int, RectTransform> nodeById,
            RectTransform layer,
            out string error)
        {
            error = string.Empty;
            if (routes == null || nodeById == null || layer == null)
            {
                error = "Route validation received incomplete geometry.";
                return false;
            }

            Dictionary<int, Rect> nodeRects = BuildNodeRects(nodeById, layer);
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                TreeArrowRoute route = routes[routeIndex];
                if (route == null || route.Edge == null || route.Points == null || route.Points.Count < 2)
                {
                    error = $"Route {routeIndex} has no usable points.";
                    return false;
                }

                if (!nodeRects.ContainsKey(route.Edge.fromId) || !nodeRects.ContainsKey(route.Edge.toId))
                {
                    error = $"Route {route.Edge.fromId}->{route.Edge.toId} has a missing endpoint node.";
                    return false;
                }

                if (route.Points[route.Points.Count - 1].y <= route.Points[0].y + Epsilon)
                {
                    error = $"Route {route.Edge.fromId}->{route.Edge.toId} does not travel bottom-to-top.";
                    return false;
                }

                for (int segmentIndex = 0; segmentIndex < route.Points.Count - 1; segmentIndex++)
                {
                    Vector2 start = route.Points[segmentIndex];
                    Vector2 end = route.Points[segmentIndex + 1];
                    foreach (KeyValuePair<int, Rect> nodeRect in nodeRects)
                    {
                        if (nodeRect.Key == route.Edge.fromId || nodeRect.Key == route.Edge.toId)
                        {
                            continue;
                        }

                        if (SegmentIntersectsInterior(start, end, nodeRect.Value))
                        {
                            error = $"Route {route.Edge.fromId}->{route.Edge.toId} enters node {nodeRect.Key}.";
                            return false;
                        }
                    }
                }
            }

            for (int firstRouteIndex = 0; firstRouteIndex < routes.Count; firstRouteIndex++)
            {
                TreeArrowRoute firstRoute = routes[firstRouteIndex];
                for (int secondRouteIndex = firstRouteIndex + 1; secondRouteIndex < routes.Count; secondRouteIndex++)
                {
                    TreeArrowRoute secondRoute = routes[secondRouteIndex];
                    if (RoutesIntersect(firstRoute, secondRoute))
                    {
                        error = $"Routes {firstRoute.Edge.fromId}->{firstRoute.Edge.toId} and {secondRoute.Edge.fromId}->{secondRoute.Edge.toId} intersect.";
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool RoutesIntersect(TreeArrowRoute first, TreeArrowRoute second)
        {
            for (int firstSegmentIndex = 0; firstSegmentIndex < first.Points.Count - 1; firstSegmentIndex++)
            {
                for (int secondSegmentIndex = 0; secondSegmentIndex < second.Points.Count - 1; secondSegmentIndex++)
                {
                    if (SegmentsIntersect(
                        first.Points[firstSegmentIndex],
                        first.Points[firstSegmentIndex + 1],
                        second.Points[secondSegmentIndex],
                        second.Points[secondSegmentIndex + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SegmentIntersectsInterior(Vector2 start, Vector2 end, Rect rect)
        {
            if (Mathf.Abs(start.x - end.x) <= Epsilon)
            {
                return start.x > rect.xMin + Epsilon
                    && start.x < rect.xMax - Epsilon
                    && Mathf.Max(Mathf.Min(start.y, end.y), rect.yMin) < Mathf.Min(Mathf.Max(start.y, end.y), rect.yMax) - Epsilon;
            }

            if (Mathf.Abs(start.y - end.y) <= Epsilon)
            {
                return start.y > rect.yMin + Epsilon
                    && start.y < rect.yMax - Epsilon
                    && Mathf.Max(Mathf.Min(start.x, end.x), rect.xMin) < Mathf.Min(Mathf.Max(start.x, end.x), rect.xMax) - Epsilon;
            }

            return true;
        }

        private static bool SegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            bool firstHorizontal = Mathf.Abs(firstStart.y - firstEnd.y) <= Epsilon;
            bool secondHorizontal = Mathf.Abs(secondStart.y - secondEnd.y) <= Epsilon;
            if (firstHorizontal && secondHorizontal)
            {
                return Mathf.Abs(firstStart.y - secondStart.y) <= Epsilon
                    && OverlapLength(firstStart.x, firstEnd.x, secondStart.x, secondEnd.x) > Epsilon;
            }

            if (!firstHorizontal && !secondHorizontal)
            {
                return Mathf.Abs(firstStart.x - secondStart.x) <= Epsilon
                    && OverlapLength(firstStart.y, firstEnd.y, secondStart.y, secondEnd.y) > Epsilon;
            }

            Vector2 horizontalStart = firstHorizontal ? firstStart : secondStart;
            Vector2 horizontalEnd = firstHorizontal ? firstEnd : secondEnd;
            Vector2 verticalStart = firstHorizontal ? secondStart : firstStart;
            Vector2 verticalEnd = firstHorizontal ? secondEnd : firstEnd;
            float verticalX = verticalStart.x;
            float horizontalY = horizontalStart.y;
            return verticalX > Mathf.Min(horizontalStart.x, horizontalEnd.x) + Epsilon
                && verticalX < Mathf.Max(horizontalStart.x, horizontalEnd.x) - Epsilon
                && horizontalY > Mathf.Min(verticalStart.y, verticalEnd.y) + Epsilon
                && horizontalY < Mathf.Max(verticalStart.y, verticalEnd.y) - Epsilon;
        }

        private static float OverlapLength(float firstStart, float firstEnd, float secondStart, float secondEnd)
        {
            return Mathf.Min(Mathf.Max(firstStart, firstEnd), Mathf.Max(secondStart, secondEnd))
                - Mathf.Max(Mathf.Min(firstStart, firstEnd), Mathf.Min(secondStart, secondEnd));
        }

        private static Dictionary<int, Rect> BuildNodeRects(
            Dictionary<int, RectTransform> nodeById,
            RectTransform layer)
        {
            Dictionary<int, Rect> nodeRects = new Dictionary<int, Rect>();
            foreach (KeyValuePair<int, RectTransform> pair in nodeById)
            {
                if (pair.Value != null)
                {
                    nodeRects[pair.Key] = GetLocalRect(pair.Value, layer);
                }
            }

            return nodeRects;
        }

        private static Rect GetLocalRect(RectTransform rectTransform, RectTransform layer)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 bottomLeft = layer.InverseTransformPoint(corners[0]);
            Vector2 topLeft = layer.InverseTransformPoint(corners[1]);
            Vector2 topRight = layer.InverseTransformPoint(corners[2]);
            Vector2 bottomRight = layer.InverseTransformPoint(corners[3]);
            return Rect.MinMaxRect(
                Mathf.Min(bottomLeft.x, topLeft.x, topRight.x, bottomRight.x),
                Mathf.Min(bottomLeft.y, topLeft.y, topRight.y, bottomRight.y),
                Mathf.Max(bottomLeft.x, topLeft.x, topRight.x, bottomRight.x),
                Mathf.Max(bottomLeft.y, topLeft.y, topRight.y, bottomRight.y));
        }
    }
}
