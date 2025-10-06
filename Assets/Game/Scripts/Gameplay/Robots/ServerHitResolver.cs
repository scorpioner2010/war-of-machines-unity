using System;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class ServerHitResolver
    {
        public static bool DebugShots = true;

        [Serializable]
        public struct HitResult
        {
            public bool hit;
            public Vector3 point;
            public Vector3 normal;
            public Collider collider;
            public ArmorMap armor;
            public float baseMm;
            public float losMm;
            public bool penetrated;
            public float damage;
        }

        public static HitResult ResolveShot(
            Vector3 startPos,
            Vector3 aimPoint,
            LayerMask hitMask,
            float shellPenMm = 200f,
            float normDeg = 0f,
            float maxDistanceFallback = 2000f)
        {
            HitResult hr = default;

            Vector3 dir = (aimPoint - startPos);
            float dist = dir.magnitude;

            if (float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z))
            {
                dir = Vector3.forward;
                dist = maxDistanceFallback;
            }
            else if (dist < 0.001f)
            {
                dir = Vector3.forward;
                dist = maxDistanceFallback;
            }
            else
            {
                dir /= Mathf.Max(dist, 1e-6f);
            }

            float maxCastDist = Mathf.Max(dist, 0.1f);

            bool didHit = Physics.Raycast(
                startPos,
                dir,
                out RaycastHit hit,
                maxCastDist,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            // Другий шанс: якщо не влучили в межах dist, спробувати довший рей до maxDistanceFallback
            if (!didHit && maxCastDist < maxDistanceFallback)
            {
                didHit = Physics.Raycast(
                    startPos,
                    dir,
                    out hit,
                    maxDistanceFallback,
                    hitMask,
                    QueryTriggerInteraction.Ignore
                );
                if (didHit)
                {
                    maxCastDist = maxDistanceFallback;
                }
            }

            if (didHit)
            {
                hr.hit = true;
                hr.point = hit.point;
                hr.normal = hit.normal;
                hr.collider = hit.collider;

                ArmorMap armor = hit.collider != null ? hit.collider.GetComponentInParent<ArmorMap>() : null;

                if (armor != null && armor.TryGetArmorLoS(hit, dir, normDeg, out float baseMm, out float losMm))
                {
                    hr.armor = armor;
                    hr.baseMm = baseMm;
                    hr.losMm = losMm;
                    hr.penetrated = shellPenMm >= losMm;
                    hr.damage = hr.penetrated ? 100f : 0f;
                }
                else
                {
                    // Нема ArmorMap — рахуємо як “тонку” поверхню (повне пробиття)
                    hr.penetrated = true;
                    hr.damage = 100f;
                }
            }

            if (DebugShots)
            {
                Color lineColor = !hr.hit ? Color.yellow : (hr.penetrated ? Color.green : Color.red);
                Vector3 end = hr.hit ? hr.point : startPos + dir * maxCastDist;
                Debug.DrawLine(startPos, end, lineColor, 2f);

                if (hr.hit)
                {
                    string targetName = (hr.collider != null) ? hr.collider.gameObject.name : "null";
                    string armorRoot = (hr.armor != null && hr.armor.tankRoot != null) ? hr.armor.tankRoot.name : "n/a";
                    string layerName = (hr.collider != null) ? LayerMask.LayerToName(hr.collider.gameObject.layer) : "n/a";

                    Debug.Log(
                        $"[HitResolver] hit={hr.hit} pen={hr.penetrated} dmg={hr.damage} penReq={shellPenMm:0} base={hr.baseMm:0.0}mm los={hr.losMm:0.0}mm " +
                        $"targetGO={targetName} tankRoot={armorRoot} layer={layerName} point={hr.point}"
                    );
                    Debug.DrawRay(hr.point, hr.normal * 0.8f, Color.cyan, 2f);
                }
                else
                {
                    Debug.Log($"[HitResolver] MISS mask={hitMask.value} from={startPos} to={aimPoint}");
                }
            }

            return hr;
        }
    }
}
