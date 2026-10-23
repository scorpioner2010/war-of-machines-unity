using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotLineOfFireChecker
    {
        private const int RaycastBufferSize = 64;
        private const float AimProbeRadius = 0.65f;
        private const float AimProbeHeight = 0.45f;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];

        public bool HasLineOfFire(VehicleRoot shooterRoot, Vector3 targetPoint, VehicleRoot expectedTarget, BotCombatSettings settings)
        {
            return TryGetBestAimPoint(shooterRoot, targetPoint, expectedTarget, settings, out _);
        }

        public bool TryGetBestAimPoint(
            VehicleRoot shooterRoot,
            Vector3 targetPoint,
            VehicleRoot expectedTarget,
            BotCombatSettings settings,
            out Vector3 bestAimPoint)
        {
            bestAimPoint = targetPoint;
            if (shooterRoot == null)
            {
                return false;
            }

            if (HasClearShot(shooterRoot, targetPoint, expectedTarget, settings))
            {
                return true;
            }

            if (expectedTarget == null)
            {
                return false;
            }

            Transform targetTransform = BotCombatUtility.GetMoveTransform(expectedTarget);
            if (targetTransform == null)
            {
                return false;
            }

            Vector3 right = targetTransform.right;
            right.y = 0f;
            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 candidate = targetPoint;
                if (i == 0)
                {
                    candidate += right * AimProbeRadius;
                }
                else if (i == 1)
                {
                    candidate -= right * AimProbeRadius;
                }
                else if (i == 2)
                {
                    candidate += Vector3.up * AimProbeHeight;
                }
                else
                {
                    candidate -= Vector3.up * AimProbeHeight;
                }

                if (!HasClearShot(shooterRoot, candidate, expectedTarget, settings))
                {
                    continue;
                }

                bestAimPoint = candidate;
                return true;
            }

            return false;
        }

        private bool HasClearShot(VehicleRoot shooterRoot, Vector3 targetPoint, VehicleRoot expectedTarget, BotCombatSettings settings)
        {
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
