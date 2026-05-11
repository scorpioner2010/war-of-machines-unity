using System;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class ServerHitResolver
    {
        private const int RaycastBufferSize = 128;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];

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
            float shellDamage = 100f,
            float maxDistanceFallback = 2000f,
            Transform ignoredRoot = null)
        {
            Vector3 dir = aimPoint - startPos;
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

            return ResolveShotDirection(
                startPos,
                dir,
                hitMask,
                shellPenMm,
                normDeg,
                shellDamage,
                Mathf.Max(dist, 0.1f),
                maxDistanceFallback,
                ignoredRoot
            );
        }

        public static HitResult ResolveShotDirection(
            Vector3 startPos,
            Vector3 direction,
            LayerMask hitMask,
            float shellPenMm = 200f,
            float normDeg = 0f,
            float shellDamage = 100f,
            float initialDistance = 2000f,
            float maxDistanceFallback = 2000f,
            Transform ignoredRoot = null)
        {
            HitResult hr = default;

            Vector3 dir = direction;
            if (float.IsNaN(dir.x) || float.IsNaN(dir.y) || float.IsNaN(dir.z) || dir.sqrMagnitude < 0.000001f)
            {
                dir = Vector3.forward;
            }
            else
            {
                dir.Normalize();
            }

            float dist = Mathf.Max(initialDistance, 0.1f);
            float maxCastDist = Mathf.Max(dist, 0.1f);

            bool didHit = TryRaycast(
                startPos,
                dir,
                out RaycastHit hit,
                maxCastDist,
                hitMask,
                ignoredRoot
            );

            // Fallback: if we missed within dist, try a longer ray to maxDistanceFallback.
            if (!didHit && maxCastDist < maxDistanceFallback)
            {
                didHit = TryRaycast(
                    startPos,
                    dir,
                    out hit,
                    maxDistanceFallback,
                    hitMask,
                    ignoredRoot
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
                    hr.damage = hr.penetrated ? Mathf.Max(0f, shellDamage) : 0f;
                }
                else
                {
                    // No ArmorMap: treat it as a thin surface with full penetration.
                    hr.penetrated = true;
                    hr.damage = Mathf.Max(0f, shellDamage);
                }
            }

            return hr;
        }

        private static bool TryRaycast(
            Vector3 startPos,
            Vector3 dir,
            out RaycastHit hit,
            float distance,
            LayerMask hitMask,
            Transform ignoredRoot)
        {
            if (ignoredRoot == null)
            {
                return Physics.Raycast(
                    startPos,
                    dir,
                    out hit,
                    distance,
                    hitMask,
                    QueryTriggerInteraction.Ignore
                );
            }

            int count = Physics.RaycastNonAlloc(
                startPos,
                dir,
                RaycastBuffer,
                distance,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider collider = RaycastBuffer[i].collider;
                if (collider == null || IsUnderRoot(collider.transform, ignoredRoot))
                {
                    continue;
                }

                float hitDistance = RaycastBuffer[i].distance;
                if (hitDistance < bestDistance)
                {
                    bestDistance = hitDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                hit = RaycastBuffer[bestIndex];
                return true;
            }

            hit = default;
            return false;
        }

        private static bool IsUnderRoot(Transform transform, Transform root)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
