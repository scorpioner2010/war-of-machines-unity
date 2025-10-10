using System.Collections.Generic;
using Game.Scripts.API;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class ArrowDrawer : MonoBehaviour
    {
        public Image arrowPrefab;

        // малює стрілки між вузлами
        public void Draw(IEnumerable<VehicleEdge> edges, Dictionary<int, RectTransform> nodeById, RectTransform layer)
        {
            if (arrowPrefab == null || layer == null)
                return;

            // очищаємо попередні стрілки
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

                // центри нод у світових координатах
                Vector3 wa = GetWorldCenter(from);
                Vector3 wb = GetWorldCenter(to);

                // переводимо в локальні координати шару стрілок
                Vector2 a = layer.InverseTransformPoint(wa);
                Vector2 b = layer.InverseTransformPoint(wb);

                Vector2 mid = (a + b) * 0.5f;
                Vector2 dir = (b - a);
                float len = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // створюємо нову стрілку
                Image img = Instantiate(arrowPrefab, layer);
                RectTransform rt = img.rectTransform;

                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = mid;        // центр стрілки між точками
                rt.sizeDelta = new Vector2(len, rt.sizeDelta.y); // розтягуємо по довжині
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        // обчислює центр RectTransform у world space
        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return (c[0] + c[2]) * 0.5f;
        }
    }
}
