using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotAimPointResolver
    {
        public Vector3 Resolve(VehicleRoot targetRoot, BotCombatSettings settings, Vector3 aimOffset)
        {
            if (targetRoot == null)
            {
                return Vector3.zero;
            }

            Bounds bounds;
            Vector3 point;
            if (settings.preferTurretAimPoint
                && (TryGetBoundsFromColliders(targetRoot.turretColliders, out bounds)
                    || TryGetBoundsFromArmorMaps(targetRoot.armorMaps, ArmorMap.ArmorZone.Turret, out bounds)))
            {
                point = bounds.center;
            }
            else if ((targetRoot.health != null && TryGetBoundsFromColliders(targetRoot.health.colliders, out bounds))
                     || TryGetBoundsFromArmorMaps(targetRoot.armorMaps, null, out bounds))
            {
                point = bounds.center;
            }
            else if (targetRoot.robotHullRotation != null)
            {
                point = targetRoot.robotHullRotation.transform.position;
            }
            else
            {
                point = targetRoot.transform.position + Vector3.up * settings.fallbackTargetHeight;
            }

            if (aimOffset.sqrMagnitude > 0.000001f)
            {
                Transform reference = BotCombatUtility.GetMoveTransform(targetRoot);
                if (reference != null)
                {
                    point += reference.right * aimOffset.x;
                    point += Vector3.up * aimOffset.y;
                    point += reference.forward * aimOffset.z;
                }
            }

            return point;
        }

        private static bool TryGetBoundsFromColliders(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            if (colliders == null)
            {
                return false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private static bool TryGetBoundsFromArmorMaps(
            ArmorMap[] armorMaps,
            ArmorMap.ArmorZone? requiredZone,
            out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            if (armorMaps == null)
            {
                return false;
            }

            for (int i = 0; i < armorMaps.Length; i++)
            {
                ArmorMap armorMap = armorMaps[i];
                if (armorMap == null)
                {
                    continue;
                }

                if (requiredZone.HasValue && armorMap.ResolvedArmorZone != requiredZone.Value)
                {
                    continue;
                }

                Collider targetCollider = armorMap.ArmorCollider;
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private static bool IsUsableCollider(Collider targetCollider)
        {
            return targetCollider != null
                   && targetCollider.enabled
                   && targetCollider.gameObject.activeInHierarchy;
        }
    }
}
