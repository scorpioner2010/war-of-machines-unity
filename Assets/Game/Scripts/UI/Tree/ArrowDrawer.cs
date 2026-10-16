using System.Collections.Generic;
using Game.Scripts.API;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class ArrowDrawer : MonoBehaviour
    {
        public Image arrowPrefab;

        public void Draw(IEnumerable<VehicleEdge> edges, Dictionary<int, RectTransform> nodeById, RectTransform layer)
        {
            if (arrowPrefab == null || layer == null)
            {
                return;
            }

            foreach (Transform child in layer)
            {
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

            if (edges == null || nodeById == null)
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

                Vector3 wa = GetWorldCenter(from);
                Vector3 wb = GetWorldCenter(to);

                Vector3 worldDirection = (wb - wa).normalized;
                wa += worldDirection * GetEdgeOffset(from, worldDirection);
                wb -= worldDirection * GetEdgeOffset(to, worldDirection);

                Vector2 a = layer.InverseTransformPoint(wa);
                Vector2 b = layer.InverseTransformPoint(wb);

                Vector2 mid = (a + b) * 0.5f;
                Vector2 dir = b - a;
                float len = dir.magnitude;
                if (len <= Mathf.Epsilon)
                {
                    continue;
                }

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                Image img = Instantiate(arrowPrefab, layer);
                img.raycastTarget = false;
                RectTransform rt = img.rectTransform;

                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = mid;
                rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            return rt.TransformPoint(rt.rect.center);
        }

        private static float GetEdgeOffset(RectTransform rectTransform, Vector3 worldDirection)
        {
            float halfWidth = rectTransform.rect.width * rectTransform.lossyScale.x * 0.5f;
            float halfHeight = rectTransform.rect.height * rectTransform.lossyScale.y * 0.5f;
            float horizontalDirection = Mathf.Abs(worldDirection.x);
            float verticalDirection = Mathf.Abs(worldDirection.y);

            return horizontalDirection > verticalDirection ? halfWidth : halfHeight;
        }
    }
}
