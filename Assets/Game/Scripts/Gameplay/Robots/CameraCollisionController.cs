using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public sealed class CameraCollisionController : MonoBehaviour
    {
        private const int HitBufferSize = 32;
        private const string DeathDebrisLayerName = "Chassis";
        private static int _deathDebrisLayer = int.MinValue;

        public VehicleRoot ignoredVehicle;
        public LayerMask collisionMask = ~(1 << 2);
        [Min(0.01f)] public float collisionRadius = 0.3f;
        [Min(0f)] public float surfacePadding = 0.08f;
        [Min(0.01f)] public float recoverySpeed = 24f;

        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
        private bool _hasResolvedDistance;
        private float _resolvedDistance;

        public void Resolve(Vector3 pivotPosition)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Vector3 desiredOffset = transform.position - pivotPosition;
            float desiredDistance = desiredOffset.magnitude;
            if (desiredDistance <= 0.0001f)
            {
                _hasResolvedDistance = false;
                return;
            }

            Vector3 direction = desiredOffset / desiredDistance;
            float safeDistance = FindSafeDistance(pivotPosition, direction, desiredDistance);
            if (!_hasResolvedDistance || safeDistance <= _resolvedDistance || desiredDistance < _resolvedDistance)
            {
                _resolvedDistance = safeDistance;
                _hasResolvedDistance = true;
            }
            else
            {
                _resolvedDistance = Mathf.MoveTowards(
                    _resolvedDistance,
                    safeDistance,
                    Mathf.Max(0.01f, recoverySpeed) * Time.deltaTime);
            }

            _resolvedDistance = Mathf.Min(_resolvedDistance, desiredDistance);
            transform.position = pivotPosition + direction * _resolvedDistance;
        }

        private float FindSafeDistance(Vector3 pivotPosition, Vector3 direction, float desiredDistance)
        {
            int hitCount = Physics.SphereCastNonAlloc(
                pivotPosition,
                Mathf.Max(0.01f, collisionRadius),
                direction,
                _hits,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            float safeDistance = desiredDistance;
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit hit = _hits[hitIndex];
                Collider hitCollider = hit.collider;
                if (hitCollider == null || IsIgnoredCollider(hitCollider))
                {
                    continue;
                }

                float hitDistance = Mathf.Max(0f, hit.distance - Mathf.Max(0f, surfacePadding));
                if (hitDistance < safeDistance)
                {
                    safeDistance = hitDistance;
                }
            }

            return safeDistance;
        }

        private bool IsIgnoredCollider(Collider hitCollider)
        {
            if (IsDetachedDeathDebris(hitCollider))
            {
                return true;
            }

            if (ignoredVehicle == null)
            {
                return false;
            }

            Transform ignoredTransform = ignoredVehicle.transform;
            Transform hitTransform = hitCollider.transform;
            return hitTransform == ignoredTransform || hitTransform.IsChildOf(ignoredTransform);
        }

        private static bool IsDetachedDeathDebris(Collider hitCollider)
        {
            if (hitCollider == null || hitCollider.attachedRigidbody == null)
            {
                return false;
            }

            if (_deathDebrisLayer == int.MinValue)
            {
                _deathDebrisLayer = LayerMask.NameToLayer(DeathDebrisLayerName);
            }

            return _deathDebrisLayer >= 0
                   && hitCollider.gameObject.layer == _deathDebrisLayer
                   && hitCollider.attachedRigidbody.transform.parent == null;
        }

        private void OnDisable()
        {
            _hasResolvedDistance = false;
        }
    }
}
