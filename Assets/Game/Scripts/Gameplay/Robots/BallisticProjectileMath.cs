using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class BallisticProjectileMath
    {
        public struct TravelSegment
        {
            public Vector3 PreviousPosition;
            public Vector3 CurrentPosition;
            public Vector3 CurrentVelocity;
            public float Distance;
        }

        public static Vector3 GetPosition(Vector3 origin, Vector3 initialVelocity, Vector3 gravity, float time)
        {
            float t = Mathf.Max(0f, time);
            return origin + initialVelocity * t + 0.5f * gravity * t * t;
        }

        public static Vector3 GetVelocity(Vector3 initialVelocity, Vector3 gravity, float time)
        {
            float t = Mathf.Max(0f, time);
            return initialVelocity + gravity * t;
        }

        public static Vector3 BuildInitialVelocity(
            Vector3 muzzlePosition,
            Vector3 aimPoint,
            float shellSpeed,
            Vector3 dispersionOffset)
        {
            return BuildDirectInitialVelocity(muzzlePosition, aimPoint + dispersionOffset, shellSpeed, Quaternion.identity);
        }

        public static Vector3 BuildInitialVelocity(
            Vector3 muzzlePosition,
            Vector3 aimPoint,
            float shellSpeed,
            Quaternion dispersionRotation)
        {
            return BuildDirectInitialVelocity(muzzlePosition, aimPoint, shellSpeed, dispersionRotation);
        }

        public static Vector3 BuildDirectInitialVelocity(
            Vector3 muzzlePosition,
            Vector3 aimPoint,
            float shellSpeed,
            Quaternion dispersionRotation)
        {
            Vector3 direction = BuildDirection(muzzlePosition, aimPoint);
            direction = dispersionRotation * direction;
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                direction = Vector3.forward;
            }
            else
            {
                direction.Normalize();
            }

            return direction * Mathf.Max(0f, shellSpeed);
        }

        public static bool TryBuildBallisticInitialVelocity(
            Vector3 muzzlePosition,
            Vector3 aimPoint,
            float shellSpeed,
            Vector3 gravity,
            bool preferHighArc,
            out Vector3 initialVelocity)
        {
            initialVelocity = Vector3.zero;

            float speed = Mathf.Max(0f, shellSpeed);
            if (speed <= 0.0001f || !IsFinite(muzzlePosition) || !IsFinite(aimPoint) || !IsFinite(gravity))
            {
                return false;
            }

            float gravityMagnitude = gravity.magnitude;
            if (gravityMagnitude <= 0.0001f)
            {
                initialVelocity = BuildDirectInitialVelocity(muzzlePosition, aimPoint, speed, Quaternion.identity);
                return true;
            }

            Vector3 up = -gravity / gravityMagnitude;
            Vector3 delta = aimPoint - muzzlePosition;
            float verticalDistance = Vector3.Dot(delta, up);
            Vector3 horizontalDelta = delta - up * verticalDistance;
            float horizontalDistance = horizontalDelta.magnitude;

            if (horizontalDistance <= 0.001f)
            {
                return TryBuildVerticalInitialVelocity(
                    verticalDistance,
                    speed,
                    gravityMagnitude,
                    up,
                    out initialVelocity);
            }

            Vector3 horizontalDirection = horizontalDelta / horizontalDistance;
            float speedSquared = speed * speed;
            float rootTerm = speedSquared * speedSquared
                             - gravityMagnitude
                             * (gravityMagnitude * horizontalDistance * horizontalDistance
                                + 2f * verticalDistance * speedSquared);

            if (rootTerm < 0f && rootTerm > -0.0001f)
            {
                rootTerm = 0f;
            }

            if (rootTerm < 0f)
            {
                return false;
            }

            float root = Mathf.Sqrt(rootTerm);
            float denominator = gravityMagnitude * horizontalDistance;
            if (denominator <= 0.000001f)
            {
                return false;
            }

            float tangent = preferHighArc
                ? (speedSquared + root) / denominator
                : (speedSquared - root) / denominator;

            if (!IsFinite(tangent))
            {
                return false;
            }

            float horizontalSpeed = speed / Mathf.Sqrt(1f + tangent * tangent);
            float verticalSpeed = horizontalSpeed * tangent;
            initialVelocity = horizontalDirection * horizontalSpeed + up * verticalSpeed;

            if (!IsFinite(initialVelocity) || initialVelocity.sqrMagnitude <= 0.000001f)
            {
                initialVelocity = Vector3.zero;
                return false;
            }

            return true;
        }

        public static Vector3 BuildInitialVelocityFromDirection(Vector3 direction, float shellSpeed)
        {
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                direction = Vector3.forward;
            }
            else
            {
                direction.Normalize();
            }

            return direction * Mathf.Max(0f, shellSpeed);
        }

        public static float EstimateDirectDrop(
            Vector3 muzzlePosition,
            Vector3 aimPoint,
            float shellSpeed,
            Vector3 gravity)
        {
            float speed = Mathf.Max(0.0001f, shellSpeed);
            Vector3 direction = BuildDirection(muzzlePosition, aimPoint);
            Vector3 delta = aimPoint - muzzlePosition;
            float distanceAlongShot = Mathf.Max(0f, Vector3.Dot(delta, direction));
            float time = distanceAlongShot / speed;
            return 0.5f * gravity.magnitude * time * time;
        }

        public static TravelSegment GetTravelSegment(
            Vector3 origin,
            Vector3 initialVelocity,
            Vector3 gravity,
            float previousTime,
            float currentTime)
        {
            Vector3 previousPosition = GetPosition(origin, initialVelocity, gravity, previousTime);
            Vector3 currentPosition = GetPosition(origin, initialVelocity, gravity, currentTime);

            TravelSegment segment = new TravelSegment
            {
                PreviousPosition = previousPosition,
                CurrentPosition = currentPosition,
                CurrentVelocity = GetVelocity(initialVelocity, gravity, currentTime),
                Distance = Vector3.Distance(previousPosition, currentPosition)
            };

            return segment;
        }

        public static Vector3 BuildDirection(Vector3 muzzlePosition, Vector3 aimPoint)
        {
            Vector3 direction = aimPoint - muzzlePosition;
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        private static bool TryBuildVerticalInitialVelocity(
            float verticalDistance,
            float speed,
            float gravityMagnitude,
            Vector3 up,
            out Vector3 initialVelocity)
        {
            initialVelocity = Vector3.zero;

            if (verticalDistance >= 0f)
            {
                float maxHeight = speed * speed / (2f * gravityMagnitude);
                if (verticalDistance > maxHeight)
                {
                    return false;
                }

                initialVelocity = up * speed;
                return true;
            }

            initialVelocity = -up * speed;
            return true;
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
