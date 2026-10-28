using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.UI.Tree
{
    public sealed class TreeArrowRouteBuilder
    {
        public IReadOnlyList<Vector2> Build(RectTransform from, RectTransform to, RectTransform layer)
        {
            return Build(from, to, layer, 0f);
        }

        public IReadOnlyList<Vector2> Build(
            RectTransform from,
            RectTransform to,
            RectTransform layer,
            float itemOffset)
        {
            if (from == null || to == null || layer == null)
            {
                return Array.Empty<Vector2>();
            }

            float safeItemOffset = Mathf.Max(0f, itemOffset);

            Vector2 fromCenter = GetCenter(from, layer);
            Vector2 toCenter = GetCenter(to, layer);

            if (Mathf.Abs(toCenter.y - fromCenter.y) <= 1f)
            {
                bool targetIsRight = toCenter.x >= fromCenter.x;
                Vector2 horizontalSourceDirection = targetIsRight ? Vector2.right : Vector2.left;
                Vector2 horizontalTargetDirection = -horizontalSourceDirection;
                Vector2 sourcePoint = GetEdgePoint(from, layer, horizontalSourceDirection)
                    + horizontalSourceDirection * safeItemOffset;
                Vector2 targetPoint = GetEdgePoint(to, layer, horizontalTargetDirection)
                    + horizontalTargetDirection * safeItemOffset;
                return new[] { sourcePoint, targetPoint };
            }

            bool targetIsAbove = toCenter.y > fromCenter.y;
            Vector2 verticalSourceDirection = targetIsAbove ? Vector2.up : Vector2.down;
            Vector2 verticalTargetDirection = -verticalSourceDirection;
            Vector2 sourceEdge = GetEdgePoint(from, layer, verticalSourceDirection)
                + verticalSourceDirection * safeItemOffset;
            Vector2 targetEdge = GetEdgePoint(to, layer, verticalTargetDirection)
                + verticalTargetDirection * safeItemOffset;
            float channelY = (sourceEdge.y + targetEdge.y) * 0.5f;

            return new[]
            {
                sourceEdge,
                new Vector2(sourceEdge.x, channelY),
                new Vector2(targetEdge.x, channelY),
                targetEdge
            };
        }

        private static Vector2 GetCenter(RectTransform rectTransform, RectTransform layer)
        {
            return layer.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.center));
        }

        private static Vector2 GetEdgePoint(RectTransform rectTransform, RectTransform layer, Vector2 direction)
        {
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            Vector2 bottomLeft = layer.InverseTransformPoint(worldCorners[0]);
            Vector2 topLeft = layer.InverseTransformPoint(worldCorners[1]);
            Vector2 topRight = layer.InverseTransformPoint(worldCorners[2]);
            Vector2 bottomRight = layer.InverseTransformPoint(worldCorners[3]);

            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            {
                return direction.y >= 0f
                    ? (topLeft + topRight) * 0.5f
                    : (bottomLeft + bottomRight) * 0.5f;
            }

            return direction.x >= 0f
                ? (topRight + bottomRight) * 0.5f
                : (bottomLeft + topLeft) * 0.5f;
        }
    }
}
