using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleGroundAlignmentController : MonoBehaviour, IVehicleRootAware
    {
        [System.Serializable]
        public sealed class Target
        {
            public Transform transform;
            public VehicleBodySwayController bodySwayController;
            public float raycastLength = 2f;
            public float surfaceCheckRadius = 1f;
            public int raysCount = 8;
        public float alignmentPercentage = 50f;
            public LayerMask surfaceLayer;
            public float lerpSpeed = 10f;

            [System.NonSerialized] private Vector3[] _rayOffsets = System.Array.Empty<Vector3>();
            [System.NonSerialized] private int _cachedRaysCount = -1;
            [System.NonSerialized] private float _cachedSurfaceCheckRadius = float.NaN;

            public void EnsureRayOffsets()
            {
                if (_cachedRaysCount == raysCount
                    && _cachedSurfaceCheckRadius == surfaceCheckRadius
                    && _rayOffsets.Length == raysCount)
                {
                    return;
                }

                _cachedRaysCount = raysCount;
                _cachedSurfaceCheckRadius = surfaceCheckRadius;
                _rayOffsets = new Vector3[raysCount];
                for (int i = 0; i < raysCount; i++)
                {
                    float angle = i / (float)raysCount * Mathf.PI * 2f;
                    _rayOffsets[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                                     * surfaceCheckRadius;
                }
            }

            public Vector3 GetRayOffset(int index)
            {
                return _rayOffsets[index];
            }
        }

        public VehicleRoot vehicleRoot;
        public Target[] targets = System.Array.Empty<Target>();

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Update()
        {
            if (vehicleRoot == null
                || (vehicleRoot.health != null && vehicleRoot.health.IsDead)
                || targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                AlignWithGround(targets[i]);
            }
        }

        private static void AlignWithGround(Target target)
        {
            if (target == null || target.transform == null || target.raysCount <= 0)
            {
                return;
            }

            Transform targetTransform = target.transform;
            Vector3 averageNormal = Vector3.zero;
            float averageGroundDistance = 0f;
            int validHits = 0;
            target.EnsureRayOffsets();
            for (int i = 0; i < target.raysCount; i++)
            {
                Vector3 rayOrigin = targetTransform.position + target.GetRayOffset(i);
                if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit hit,
                        target.raycastLength,
                        target.surfaceLayer))
                {
                    averageNormal += hit.normal;
                    averageGroundDistance += hit.distance;
                    validHits++;
                }
            }

            if (validHits == 0)
            {
                if (target.bodySwayController != null)
                {
                    target.bodySwayController.ClearTerrainContact();
                }

                return;
            }

            averageNormal /= validHits;
            averageGroundDistance /= validHits;
            Vector3 finalNormal = Vector3.Lerp(
                Vector3.up,
                averageNormal,
                target.alignmentPercentage / 100f).normalized;
            Vector3 forwardDirection = targetTransform.parent != null
                ? targetTransform.parent.forward
                : targetTransform.forward;
            forwardDirection = Vector3.ProjectOnPlane(forwardDirection, finalNormal).normalized;
            if (forwardDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, finalNormal);
            if (target.bodySwayController != null)
            {
                targetRotation *= target.bodySwayController.GetGroundRelativeRotationOffset(
                    finalNormal,
                    targetRotation,
                    averageGroundDistance,
                    Time.deltaTime);
            }

            targetTransform.rotation = Quaternion.Lerp(
                targetTransform.rotation,
                targetRotation,
                Time.deltaTime * target.lerpSpeed);
        }
    }
}
