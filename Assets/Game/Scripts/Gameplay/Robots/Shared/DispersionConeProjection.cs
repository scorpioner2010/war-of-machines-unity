using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class DispersionConeProjection
    {
        private const int ReticleBoundarySampleCount = 32;
        private const float ReticleSafetyMultiplier = 1.02f;
        private const float MinimumDirectionMagnitude = 0.000001f;

        public static Vector3 BuildDispersedDirection(Vector3 baseDirection, Vector2 unitCircle, float dispersionDeg)
        {
            if (!TryBuildBasis(baseDirection, out Vector3 normalizedDirection, out Vector3 right, out Vector3 up))
            {
                return Vector3.forward;
            }

            float spreadTangent = Mathf.Tan(Mathf.Clamp(dispersionDeg, 0f, 89.9f) * Mathf.Deg2Rad);
            Vector3 dispersedDirection = normalizedDirection
                + right * (unitCircle.x * spreadTangent)
                + up * (unitCircle.y * spreadTangent);
            if (dispersedDirection.sqrMagnitude <= MinimumDirectionMagnitude)
            {
                return normalizedDirection;
            }

            return dispersedDirection.normalized;
        }

        public static bool TryGetScreenDiameter(
            Camera camera,
            Vector3 origin,
            Vector3 aimPoint,
            float dispersionDeg,
            out float diameterPixels)
        {
            diameterPixels = 0f;
            if (camera == null || !IsFinite(origin) || !IsFinite(aimPoint))
            {
                return false;
            }

            Vector3 aimVector = aimPoint - origin;
            float targetDistance = aimVector.magnitude;
            if (targetDistance <= 0.1f || !TryBuildBasis(aimVector, out Vector3 baseDirection, out Vector3 right, out Vector3 up))
            {
                return false;
            }

            Vector3 screenCenter = camera.WorldToScreenPoint(aimPoint);
            if (!IsFinite(screenCenter) || screenCenter.z <= 0f)
            {
                return false;
            }

            float spreadTangent = Mathf.Tan(Mathf.Clamp(dispersionDeg, 0f, 89.9f) * Mathf.Deg2Rad);
            if (spreadTangent <= 0f)
            {
                return true;
            }

            float maxRadiusPixels = 0f;
            for (int i = 0; i < ReticleBoundarySampleCount; i++)
            {
                float angle = 2f * Mathf.PI * i / ReticleBoundarySampleCount;
                Vector2 unitCircle = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 dispersedDirection = baseDirection
                    + right * (unitCircle.x * spreadTangent)
                    + up * (unitCircle.y * spreadTangent);
                if (dispersedDirection.sqrMagnitude <= MinimumDirectionMagnitude)
                {
                    continue;
                }

                Vector3 boundaryPoint = origin + dispersedDirection.normalized * targetDistance;
                Vector3 screenBoundary = camera.WorldToScreenPoint(boundaryPoint);
                if (!IsFinite(screenBoundary) || screenBoundary.z <= 0f)
                {
                    return false;
                }

                float radiusPixels = Vector2.Distance(
                    new Vector2(screenCenter.x, screenCenter.y),
                    new Vector2(screenBoundary.x, screenBoundary.y));
                maxRadiusPixels = Mathf.Max(maxRadiusPixels, radiusPixels);
            }

            diameterPixels = maxRadiusPixels * 2f * ReticleSafetyMultiplier;
            return IsFinite(diameterPixels);
        }

        public static float GetFovScale(float referenceFov, float currentFov)
        {
            float safeReferenceFov = Mathf.Clamp(referenceFov, 1f, 179f);
            float safeCurrentFov = Mathf.Clamp(currentFov, 1f, 179f);
            float referenceTangent = Mathf.Tan(safeReferenceFov * 0.5f * Mathf.Deg2Rad);
            float currentTangent = Mathf.Tan(safeCurrentFov * 0.5f * Mathf.Deg2Rad);
            if (currentTangent <= 0.000001f)
            {
                return 1f;
            }

            return referenceTangent / currentTangent;
        }

        private static bool TryBuildBasis(
            Vector3 direction,
            out Vector3 normalizedDirection,
            out Vector3 right,
            out Vector3 up)
        {
            normalizedDirection = direction;
            right = default;
            up = default;
            if (!IsFinite(direction) || direction.sqrMagnitude <= MinimumDirectionMagnitude)
            {
                return false;
            }

            normalizedDirection.Normalize();
            right = Vector3.Cross(Vector3.up, normalizedDirection);
            if (right.sqrMagnitude <= MinimumDirectionMagnitude)
            {
                right = Vector3.Cross(Vector3.forward, normalizedDirection);
            }

            if (right.sqrMagnitude <= MinimumDirectionMagnitude)
            {
                return false;
            }

            right.Normalize();
            up = Vector3.Cross(normalizedDirection, right);
            if (up.sqrMagnitude <= MinimumDirectionMagnitude)
            {
                return false;
            }

            up.Normalize();
            return true;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
