using System.Collections.Generic;
using RAV.Scripts.API;
using RAV.Scripts.UI.Tree;
using NUnit.Framework;
using UnityEngine;

namespace RAV.Editor.Tests
{
    public sealed class TreeArrowRouteBuilderEditModeTests
    {
        [Test]
        public void BuildsUpLeftUpRouteThroughTheGapBetweenRows()
        {
            GameObject layerObject = new GameObject("TreeArrowRouteTestLayer", typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(1000f, 1000f);

            RectTransform from = CreateNode(layer, new Vector2(0f, -200f));
            RectTransform to = CreateNode(layer, new Vector2(-200f, 200f));
            IReadOnlyList<Vector2> route = new TreeArrowRouteBuilder().Build(from, to, layer);

            Assert.That(route.Count, Is.EqualTo(4));
            Assert.That(route[0], Is.EqualTo(new Vector2(0f, -150f)));
            Assert.That(route[1], Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(route[2], Is.EqualTo(new Vector2(-200f, 0f)));
            Assert.That(route[3], Is.EqualTo(new Vector2(-200f, 150f)));

            Object.DestroyImmediate(layerObject);
        }

        [Test]
        public void BuildsHorizontalRouteForNodesInTheSameRow()
        {
            GameObject layerObject = new GameObject("TreeArrowHorizontalRouteTestLayer", typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(1000f, 1000f);

            RectTransform from = CreateNode(layer, new Vector2(-200f, 0f));
            RectTransform to = CreateNode(layer, new Vector2(200f, 0f));
            IReadOnlyList<Vector2> route = new TreeArrowRouteBuilder().Build(from, to, layer);

            Assert.That(route.Count, Is.EqualTo(2));
            Assert.That(route[0], Is.EqualTo(new Vector2(-150f, 0f)));
            Assert.That(route[1], Is.EqualTo(new Vector2(150f, 0f)));

            Object.DestroyImmediate(layerObject);
        }

        [Test]
        public void AppliesItemOffsetAwayFromBothNodeBorders()
        {
            GameObject layerObject = new GameObject("TreeArrowOffsetRouteTestLayer", typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(1000f, 1000f);

            RectTransform from = CreateNode(layer, new Vector2(0f, -200f));
            RectTransform to = CreateNode(layer, new Vector2(-200f, 200f));
            IReadOnlyList<Vector2> route = new TreeArrowRouteBuilder().Build(from, to, layer, 10f);

            Assert.That(route[0], Is.EqualTo(new Vector2(0f, -140f)));
            Assert.That(route[3], Is.EqualTo(new Vector2(-200f, 140f)));

            Object.DestroyImmediate(layerObject);
        }

        [Test]
        public void PlansServerNovaGraphWithoutRouteIntersections()
        {
            GameObject layerObject = new GameObject("TreeArrowNovaPlannerTestLayer", typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(1800f, 1000f);
            VehicleGraph graph = TreeGraphTestFixture.CreateNovaGraphSnapshot();
            TreeGraphLayout layout = new TreeGraphLayoutPlanner().Plan(
                graph,
                new Vector2(100f, 100f),
                32f,
                80f);
            Dictionary<int, RectTransform> nodeMap = new Dictionary<int, RectTransform>();
            foreach (KeyValuePair<int, TreeGraphNodePlacement> placement in layout.Placements)
            {
                nodeMap[placement.Key] = CreateNode(layer, placement.Value.Position);
            }

            IReadOnlyList<TreeArrowRoute> routes = new TreeArrowRoutePlanner().Plan(
                graph.edges,
                nodeMap,
                layer,
                0f,
                5f);

            Assert.That(routes.Count, Is.EqualTo(graph.edges.Length));
            Assert.That(
                new TreeArrowRouteValidator().TryValidate(routes, nodeMap, layer, out string validationError),
                Is.True,
                validationError);

            Object.DestroyImmediate(layerObject);
        }

        [Test]
        public void PlansServerIronGraphWithoutRouteIntersections()
        {
            GameObject layerObject = new GameObject("TreeArrowIronPlannerTestLayer", typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(1800f, 1000f);
            VehicleGraph graph = TreeGraphTestFixture.CreateIronGraphSnapshot();
            TreeGraphLayout layout = new TreeGraphLayoutPlanner().Plan(
                graph,
                new Vector2(100f, 100f),
                32f,
                80f);
            Dictionary<int, RectTransform> nodeMap = new Dictionary<int, RectTransform>();
            foreach (KeyValuePair<int, TreeGraphNodePlacement> placement in layout.Placements)
            {
                nodeMap[placement.Key] = CreateNode(layer, placement.Value.Position);
            }

            IReadOnlyList<TreeArrowRoute> routes = new TreeArrowRoutePlanner().Plan(
                graph.edges,
                nodeMap,
                layer,
                0f,
                5f);

            Assert.That(routes.Count, Is.EqualTo(graph.edges.Length));
            Assert.That(
                new TreeArrowRouteValidator().TryValidate(routes, nodeMap, layer, out string validationError),
                Is.True,
                validationError);

            Object.DestroyImmediate(layerObject);
        }

        private static RectTransform CreateNode(RectTransform layer, Vector2 position)
        {
            GameObject nodeObject = new GameObject("TreeArrowRouteTestNode", typeof(RectTransform));
            RectTransform node = nodeObject.GetComponent<RectTransform>();
            node.SetParent(layer, false);
            node.anchorMin = new Vector2(0.5f, 0.5f);
            node.anchorMax = new Vector2(0.5f, 0.5f);
            node.pivot = new Vector2(0.5f, 0.5f);
            node.anchoredPosition = position;
            node.sizeDelta = new Vector2(100f, 100f);
            return node;
        }

        private static VehicleEdge CreateEdge(int fromId, int toId)
        {
            return new VehicleEdge
            {
                fromId = fromId,
                toId = toId
            };
        }

        private static bool HasSegmentInsideDifferentNode(
            TreeArrowRoute route,
            Dictionary<int, RectTransform> nodeMap,
            RectTransform layer)
        {
            for (int nodeIdIndex = 0; nodeIdIndex < nodeMap.Count; nodeIdIndex++)
            {
                int nodeId = new List<int>(nodeMap.Keys)[nodeIdIndex];
                if (nodeId == route.Edge.fromId || nodeId == route.Edge.toId)
                {
                    continue;
                }

                Rect rect = GetLocalRect(nodeMap[nodeId], layer);
                for (int pointIndex = 0; pointIndex < route.Points.Count - 1; pointIndex++)
                {
                    if (SegmentIntersectsRect(route.Points[pointIndex], route.Points[pointIndex + 1], rect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RoutesIntersect(TreeArrowRoute first, TreeArrowRoute second)
        {
            for (int firstPointIndex = 0; firstPointIndex < first.Points.Count - 1; firstPointIndex++)
            {
                for (int secondPointIndex = 0; secondPointIndex < second.Points.Count - 1; secondPointIndex++)
                {
                    if (SegmentsIntersect(
                        first.Points[firstPointIndex],
                        first.Points[firstPointIndex + 1],
                        second.Points[secondPointIndex],
                        second.Points[secondPointIndex + 1]))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private static bool SegmentIntersectsRect(Vector2 start, Vector2 end, Rect rect)
        {
            if (Mathf.Abs(start.x - end.x) <= 0.01f)
            {
                return start.x > rect.xMin
                    && start.x < rect.xMax
                    && Mathf.Max(Mathf.Min(start.y, end.y), rect.yMin) < Mathf.Min(Mathf.Max(start.y, end.y), rect.yMax);
            }

            return start.y > rect.yMin
                && start.y < rect.yMax
                && Mathf.Max(Mathf.Min(start.x, end.x), rect.xMin) < Mathf.Min(Mathf.Max(start.x, end.x), rect.xMax);
        }

        private static bool SegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            bool firstHorizontal = Mathf.Abs(firstStart.y - firstEnd.y) <= 0.01f;
            bool secondHorizontal = Mathf.Abs(secondStart.y - secondEnd.y) <= 0.01f;
            if (firstHorizontal && secondHorizontal)
            {
                return Mathf.Abs(firstStart.y - secondStart.y) <= 0.01f
                    && Mathf.Max(Mathf.Min(firstStart.x, firstEnd.x), Mathf.Min(secondStart.x, secondEnd.x))
                    < Mathf.Min(Mathf.Max(firstStart.x, firstEnd.x), Mathf.Max(secondStart.x, secondEnd.x));
            }

            if (!firstHorizontal && !secondHorizontal)
            {
                return Mathf.Abs(firstStart.x - secondStart.x) <= 0.01f
                    && Mathf.Max(Mathf.Min(firstStart.y, firstEnd.y), Mathf.Min(secondStart.y, secondEnd.y))
                    < Mathf.Min(Mathf.Max(firstStart.y, firstEnd.y), Mathf.Max(secondStart.y, secondEnd.y));
            }

            Vector2 horizontalStart = firstHorizontal ? firstStart : secondStart;
            Vector2 horizontalEnd = firstHorizontal ? firstEnd : secondEnd;
            Vector2 verticalStart = firstHorizontal ? secondStart : firstStart;
            Vector2 verticalEnd = firstHorizontal ? secondEnd : firstEnd;
            return verticalStart.x > Mathf.Min(horizontalStart.x, horizontalEnd.x)
                && verticalStart.x < Mathf.Max(horizontalStart.x, horizontalEnd.x)
                && horizontalStart.y > Mathf.Min(verticalStart.y, verticalEnd.y)
                && horizontalStart.y < Mathf.Max(verticalStart.y, verticalEnd.y);
        }
    }
}
