using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class ArmorMap : MonoBehaviour
    {
        public TankRoot tankRoot;
        public Texture2D thicknessMap;
        [Min(0f)] public float minMm = 0;
        [Min(0f)] public float maxMm = 500;

        public float SampleMm(Vector2 uv)
        {
            float u = Mathf.Clamp01(uv.x);
            float v = Mathf.Clamp01(uv.y);
            Color c = thicknessMap.GetPixelBilinear(u, v);
            float t = Mathf.Clamp01(c.r); // 0=чорний => maxMm, 1=білий => minMm
            return Mathf.Lerp(maxMm, minMm, t);
        }

        public bool TryGetArmorLoS(RaycastHit hit, Vector3 shotDir, float normDeg, out float baseMm, out float losMm)
        {
            baseMm = SampleMm(hit.textureCoord);

            Vector3 dir = shotDir.normalized;
            Vector3 n = hit.normal.normalized;
            float cosTheta = Mathf.Clamp(Vector3.Dot(-dir, n), -1f, 1f);
            float thetaDeg = Mathf.Acos(cosTheta) * Mathf.Rad2Deg;
            float thetaPrime = Mathf.Max(0f, thetaDeg - Mathf.Max(0f, normDeg));
            float cosThetaPrime = Mathf.Cos(thetaPrime * Mathf.Deg2Rad);
            if (cosThetaPrime <= 0.0001f) cosThetaPrime = 0.0001f;

            losMm = baseMm / cosThetaPrime;
            return true;
        }
    }
}