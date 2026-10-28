using System.Collections.Generic;
using Game.Scripts.UI.Tree;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
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
    }
}
