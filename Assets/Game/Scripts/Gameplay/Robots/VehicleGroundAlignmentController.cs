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
            public float raycastLength = 2f;
            public float surfaceCheckRadius = 1f;
            public int raysCount = 8;
            public float alignmentPercentage = 50f;
            public LayerMask surfaceLayer;
            public float lerpSpeed = 10f;
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
            int validHits = 0;
            for (int i = 0; i < target.raysCount; i++)
            {
                float angle = i / (float)target.raysCount * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                                 * target.surfaceCheckRadius;
                Vector3 rayOrigin = targetTransform.position + offset;
                if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit hit,
                        target.raycastLength,
                        target.surfaceLayer))
                {
                    averageNormal += hit.normal;
                    validHits++;
                }
            }

            if (validHits == 0)
            {
                return;
            }

            averageNormal /= validHits;
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
            targetTransform.rotation = Quaternion.Lerp(
                targetTransform.rotation,
                targetRotation,
                Time.deltaTime * target.lerpSpeed);
        }
    }
}
