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
                return;

            foreach (Transform c in layer)
            {
                if (Application.isPlaying)
                    Destroy(c.gameObject);
                else
                    DestroyImmediate(c.gameObject);
            }

            foreach (VehicleEdge e in edges)
            {
                if (!nodeById.TryGetValue(e.fromId, out var from) || !nodeById.TryGetValue(e.toId, out var to))
                    continue;

                Vector3 wa = GetWorldCenter(from);
                Vector3 wb = GetWorldCenter(to);

                Vector2 a = layer.InverseTransformPoint(wa);
                Vector2 b = layer.InverseTransformPoint(wb);

                Vector2 mid = (a + b) * 0.5f;
                Vector2 dir = (b - a);
                float len = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                Image img = Instantiate(arrowPrefab, layer);
                RectTransform rt = img.rectTransform;

                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = mid;
                rt.sizeDelta = new Vector2(len, rt.sizeDelta.y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return (c[0] + c[2]) * 0.5f;
        }
    }
}
