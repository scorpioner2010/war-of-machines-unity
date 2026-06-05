using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotLineOfFireChecker
    {
        private const int RaycastBufferSize = 64;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];

        public bool HasLineOfFire(VehicleRoot shooterRoot, Vector3 targetPoint, VehicleRoot expectedTarget, BotCombatSettings settings)
        {
            if (shooterRoot == null)
            {
                return false;
            }

            Vector3 origin = BotCombatUtility.GetAimOrigin(shooterRoot);
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (!BotCombatUtility.IsFinite(direction) || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.001f)
            {
                return false;
            }

            direction /= distance;
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                RaycastBuffer,
                distance + 0.25f,
                settings.lineOfSightMask,
                QueryTriggerInteraction.Ignore);

            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = RaycastBuffer[i].collider;
                if (hitCollider == null || BotCombatUtility.IsUnderRoot(hitCollider.transform, shooterRoot.transform))
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

            if (bestIndex < 0)
            {
                return true;
            }

            Collider bestCollider = RaycastBuffer[bestIndex].collider;
            if (VehicleColliderRegistry.TryGetRoot(bestCollider, out VehicleRoot hitRoot))
            {
                return hitRoot == expectedTarget;
            }

            return expectedTarget != null && BotCombatUtility.IsUnderRoot(bestCollider.transform, expectedTarget.transform);
        }
    }
}
