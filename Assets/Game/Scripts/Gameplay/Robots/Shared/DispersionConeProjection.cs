using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class DispersionConeProjection
    {
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
