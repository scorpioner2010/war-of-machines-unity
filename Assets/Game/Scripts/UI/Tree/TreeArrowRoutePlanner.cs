using System;
using System.Collections.Generic;
using RAV.Scripts.API;
using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeArrowRoute
    {
        public TreeArrowRoute(VehicleEdge edge, IReadOnlyList<Vector2> points)
        {
            Edge = edge;
            Points = points;
        }

        public VehicleEdge Edge { get; }
        public IReadOnlyList<Vector2> Points { get; }
    }

    public sealed class TreeArrowRoutePlanner
    {
        private const float CoordinateEpsilon = 0.01f;

        private readonly struct PortGroupKey : IEquatable<PortGroupKey>
        {
            public PortGroupKey(int nodeId, int direction)
            {
                NodeId = nodeId;
                Direction = direction;
            }

            public int NodeId { get; }
            public int Direction { get; }

            public bool Equals(PortGroupKey other)
            {
                return NodeId == other.NodeId && Direction == other.Direction;
            }

            public override bool Equals(object obj)
            {
                return obj is PortGroupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (NodeId * 397) ^ Direction;
                }
            }
        }

        private readonly struct GapKey : IEquatable<GapKey>
        {
            public GapKey(float sourceY, float targetY)
            {
                SourceY = Mathf.RoundToInt(sourceY * 100f);
                TargetY = Mathf.RoundToInt(targetY * 100f);
            }

            public int SourceY { get; }
            public int TargetY { get; }

            public bool Equals(GapKey other)
            {
                return SourceY == other.SourceY && TargetY == other.TargetY;
            }

            public override bool Equals(object obj)
            {
                return obj is GapKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (SourceY * 397) ^ TargetY;
                }
            }
        }

        private sealed class EdgeInfo
        {
            public VehicleEdge Edge;
            public Rect FromRect;
            public Rect ToRect;
            public Vector2 FromCenter;
            public Vector2 ToCenter;
            public Vector2 SourceDirection;
            public Vector2 TargetDirection;
            public Vector2 Start;
            public Vector2 End;
            public bool IsVertical;
        }

        public IReadOnlyList<TreeArrowRoute> Plan(
            IEnumerable<VehicleEdge> edges,
            Dictionary<int, RectTransform> nodeById,
            RectTransform layer,
            float itemOffset,
            float shaftThickness)
        {
            List<TreeArrowRoute> routes = new List<TreeArrowRoute>();
            if (edges == null || nodeById == null || layer == null)
            {
                return routes;
            }

            Dictionary<int, Rect> nodeRects = BuildNodeRects(nodeById, layer);
            List<EdgeInfo> edgeInfos = BuildEdgeInfos(edges, nodeRects);
            if (edgeInfos.Count == 0)
            {
                return routes;
            }

            float portSpacing = Mathf.Max(10f, shaftThickness * 2f + 4f);
            AssignPorts(edgeInfos, nodeRects, Mathf.Max(0f, itemOffset), portSpacing);

            Dictionary<GapKey, List<EdgeInfo>> verticalGroups = new Dictionary<GapKey, List<EdgeInfo>>();
            List<EdgeInfo> horizontalEdges = new List<EdgeInfo>();
            for (int edgeIndex = 0; edgeIndex < edgeInfos.Count; edgeIndex++)
            {
                EdgeInfo edgeInfo = edgeInfos[edgeIndex];
                if (!edgeInfo.IsVertical)
                {
                    horizontalEdges.Add(edgeInfo);
                    continue;
                }

                GapKey key = new GapKey(edgeInfo.Start.y, edgeInfo.End.y);
                if (!verticalGroups.TryGetValue(key, out List<EdgeInfo> group))
                {
                    group = new List<EdgeInfo>();
                    verticalGroups.Add(key, group);
                }

                group.Add(edgeInfo);
            }

            float laneSpacing = Mathf.Max(10f, shaftThickness + 5f);
            foreach (List<EdgeInfo> group in verticalGroups.Values)
            {
                AddVerticalRoutes(group, routes);
            }

            for (int edgeIndex = 0; edgeIndex < horizontalEdges.Count; edgeIndex++)
            {
                EdgeInfo edgeInfo = horizontalEdges[edgeIndex];
                List<Vector2> points = BuildHorizontalRoute(edgeInfo, edgeIndex, laneSpacing);
                routes.Add(new TreeArrowRoute(edgeInfo.Edge, CompressCollinearPoints(points)));
            }

            routes.Sort(CompareRoutes);
            return routes;
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

        private static List<EdgeInfo> BuildEdgeInfos(
            IEnumerable<VehicleEdge> edges,
            Dictionary<int, Rect> nodeRects)
        {
            List<EdgeInfo> edgeInfos = new List<EdgeInfo>();
            foreach (VehicleEdge edge in edges)
            {
                if (edge == null
                    || !nodeRects.TryGetValue(edge.fromId, out Rect fromRect)
                    || !nodeRects.TryGetValue(edge.toId, out Rect toRect))
                {
                    continue;
                }

                Vector2 fromCenter = fromRect.center;
                Vector2 toCenter = toRect.center;
                Vector2 delta = toCenter - fromCenter;
                bool isVertical = Mathf.Abs(delta.y) > CoordinateEpsilon;
                bool targetIsAbove = delta.y > CoordinateEpsilon;
                bool targetIsRight = delta.x >= 0f;
                Vector2 sourceDirection;
                Vector2 targetDirection;
                if (isVertical)
                {
                    sourceDirection = targetIsAbove ? Vector2.up : Vector2.down;
                    targetDirection = -sourceDirection;
                }
                else
                {
                    sourceDirection = targetIsRight ? Vector2.right : Vector2.left;
                    targetDirection = -sourceDirection;
                }

                edgeInfos.Add(new EdgeInfo
                {
                    Edge = edge,
                    FromRect = fromRect,
                    ToRect = toRect,
                    FromCenter = fromCenter,
                    ToCenter = toCenter,
                    SourceDirection = sourceDirection,
                    TargetDirection = targetDirection,
                    IsVertical = isVertical
                });
            }

            return edgeInfos;
        }

        private static void AssignPorts(
            List<EdgeInfo> edgeInfos,
            Dictionary<int, Rect> nodeRects,
            float itemOffset,
            float portSpacing)
        {
            Dictionary<PortGroupKey, List<EdgeInfo>> sourceGroups = new Dictionary<PortGroupKey, List<EdgeInfo>>();
            Dictionary<PortGroupKey, List<EdgeInfo>> targetGroups = new Dictionary<PortGroupKey, List<EdgeInfo>>();

            for (int edgeIndex = 0; edgeIndex < edgeInfos.Count; edgeIndex++)
            {
                EdgeInfo edgeInfo = edgeInfos[edgeIndex];
                AddToGroup(
                    sourceGroups,
                    new PortGroupKey(edgeInfo.Edge.fromId, DirectionToIndex(edgeInfo.SourceDirection)),
                    edgeInfo);
                AddToGroup(
                    targetGroups,
                    new PortGroupKey(edgeInfo.Edge.toId, DirectionToIndex(edgeInfo.TargetDirection)),
                    edgeInfo);
            }

            foreach (KeyValuePair<PortGroupKey, List<EdgeInfo>> group in sourceGroups)
            {
                AssignGroupPorts(group.Value, nodeRects[group.Key.NodeId], true, itemOffset, portSpacing);
            }

            foreach (KeyValuePair<PortGroupKey, List<EdgeInfo>> group in targetGroups)
            {
                AssignGroupPorts(group.Value, nodeRects[group.Key.NodeId], false, itemOffset, portSpacing);
            }
        }

        private static void AddToGroup(
            Dictionary<PortGroupKey, List<EdgeInfo>> groups,
            PortGroupKey key,
            EdgeInfo edgeInfo)
        {
            if (!groups.TryGetValue(key, out List<EdgeInfo> group))
            {
                group = new List<EdgeInfo>();
                groups.Add(key, group);
            }

            group.Add(edgeInfo);
        }

        private static void AssignGroupPorts(
            List<EdgeInfo> group,
            Rect rect,
            bool source,
            float itemOffset,
            float portSpacing)
        {
            group.Sort((left, right) =>
            {
                float leftPosition = source ? left.ToCenter.x : left.FromCenter.x;
                float rightPosition = source ? right.ToCenter.x : right.FromCenter.x;
                int positionCompare = leftPosition.CompareTo(rightPosition);
                return positionCompare != 0
                    ? positionCompare
                    : left.Edge.toId.CompareTo(right.Edge.toId);
            });

            for (int edgeIndex = 0; edgeIndex < group.Count; edgeIndex++)
            {
                EdgeInfo edgeInfo = group[edgeIndex];
                float offset = (edgeIndex - (group.Count - 1) * 0.5f) * portSpacing;
                Vector2 direction = source ? edgeInfo.SourceDirection : edgeInfo.TargetDirection;
                Vector2 point = GetPort(rect, direction, offset, itemOffset);
                if (source)
                {
                    edgeInfo.Start = point;
                }
                else
                {
                    edgeInfo.End = point;
                }
            }
        }

        private static Vector2 GetPort(Rect rect, Vector2 direction, float offset, float itemOffset)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            {
                float y = direction.y >= 0f
                    ? rect.yMax + itemOffset
                    : rect.yMin - itemOffset;
                return new Vector2(rect.center.x + offset, y);
            }

            float x = direction.x >= 0f
                ? rect.xMax + itemOffset
                : rect.xMin - itemOffset;
            return new Vector2(x, rect.center.y + offset);
        }

        private static void AddVerticalRoutes(
            List<EdgeInfo> group,
            List<TreeArrowRoute> routes)
        {
            group.Sort((left, right) =>
            {
                int sourceCompare = left.Start.x.CompareTo(right.Start.x);
                if (sourceCompare != 0)
                {
                    return sourceCompare;
                }

                int targetCompare = left.End.x.CompareTo(right.End.x);
                return targetCompare != 0
                    ? targetCompare
                    : left.Edge.toId.CompareTo(right.Edge.toId);
            });

            for (int edgeIndex = 0; edgeIndex < group.Count; edgeIndex++)
            {
                EdgeInfo edgeInfo = group[edgeIndex];
                float laneProgress = (edgeIndex + 1f) / (group.Count + 1f);
                float laneY = Mathf.Lerp(edgeInfo.Start.y, edgeInfo.End.y, laneProgress);
                List<Vector2> points = new List<Vector2>
                {
                    edgeInfo.Start,
                    new Vector2(edgeInfo.Start.x, laneY),
                    new Vector2(edgeInfo.End.x, laneY),
                    edgeInfo.End
                };
                routes.Add(new TreeArrowRoute(edgeInfo.Edge, CompressCollinearPoints(points)));
            }
        }

        private static List<Vector2> BuildHorizontalRoute(
            EdgeInfo edgeInfo,
            int routeIndex,
            float laneSpacing)
        {
            float channelY = Mathf.Min(edgeInfo.FromRect.yMin, edgeInfo.ToRect.yMin)
                - laneSpacing * (routeIndex + 1);
            return new List<Vector2>
            {
                edgeInfo.Start,
                new Vector2(edgeInfo.Start.x, channelY),
                new Vector2(edgeInfo.End.x, channelY),
                edgeInfo.End
            };
        }

        private static List<Vector2> CompressCollinearPoints(List<Vector2> points)
        {
            if (points == null || points.Count <= 2)
            {
                return points ?? new List<Vector2>();
            }

            List<Vector2> compressed = new List<Vector2> { points[0] };
            for (int pointIndex = 1; pointIndex < points.Count - 1; pointIndex++)
            {
                Vector2 previous = compressed[compressed.Count - 1];
                Vector2 current = points[pointIndex];
                Vector2 next = points[pointIndex + 1];
                bool sameHorizontal = Mathf.Abs(previous.y - current.y) <= CoordinateEpsilon
                    && Mathf.Abs(current.y - next.y) <= CoordinateEpsilon;
                bool sameVertical = Mathf.Abs(previous.x - current.x) <= CoordinateEpsilon
                    && Mathf.Abs(current.x - next.x) <= CoordinateEpsilon;
                if (!sameHorizontal && !sameVertical)
                {
                    compressed.Add(current);
                }
            }

            compressed.Add(points[points.Count - 1]);
            return compressed;
        }

        private static int CompareRoutes(TreeArrowRoute left, TreeArrowRoute right)
        {
            int fromCompare = left.Edge.fromId.CompareTo(right.Edge.fromId);
            return fromCompare != 0
                ? fromCompare
                : left.Edge.toId.CompareTo(right.Edge.toId);
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

        private static int DirectionToIndex(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            {
                return direction.y >= 0f ? 0 : 2;
            }

            return direction.x >= 0f ? 1 : 3;
        }
    }
}
