using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    [DisallowMultipleComponent]
    public sealed class CaterpillarSuspensionController : MonoBehaviour, IVehicleRootAware
    {
        [System.Serializable]
        public sealed class SuspensionTarget
        {
            public Transform transform;
            public Vector3 positionOffset;

            [System.NonSerialized] public Vector3 initialLocalPosition;
            [System.NonSerialized] public Vector3 targetLocalPosition;
            [System.NonSerialized] public float nextRaycastTime;
            [System.NonSerialized] public bool initialized;
        }

        public VehicleRoot vehicleRoot;
        public SuspensionTarget[] targets = System.Array.Empty<SuspensionTarget>();
        public float rayHeight = 1f;
        public float rayDistance = 2f;
        public LayerMask groundLayer;
        public float lerpSpeed = 10f;
        [Min(0.01f)] public float raycastInterval = 0.05f;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Start()
        {
            if (targets == null)
            {
                return;
            }

            float interval = Mathf.Max(0.01f, raycastInterval);
            for (int i = 0; i < targets.Length; i++)
            {
                SuspensionTarget target = targets[i];
                if (target == null || target.transform == null)
                {
                    continue;
                }

                target.initialLocalPosition = target.transform.localPosition;
                target.targetLocalPosition = target.initialLocalPosition;
                target.nextRaycastTime = Time.time + Random.Range(0f, interval);
                target.initialized = true;
            }
        }

        private void Update()
        {
            if (vehicleRoot == null
                || (vehicleRoot.health != null && vehicleRoot.health.IsDead)
                || targets == null)
            {
                return;
            }

            float now = Time.time;
            float interval = Mathf.Max(0.01f, raycastInterval);
            float interpolation = Time.deltaTime * Mathf.Max(0f, lerpSpeed);

            for (int i = 0; i < targets.Length; i++)
            {
                SuspensionTarget target = targets[i];
                if (target == null || target.transform == null || !target.initialized)
                {
                    continue;
                }

                if (now >= target.nextRaycastTime)
                {
                    RefreshTargetLocalPosition(target);
                    target.nextRaycastTime = now + interval;
                }

                target.transform.localPosition = Vector3.Lerp(
                    target.transform.localPosition,
                    target.targetLocalPosition,
                    interpolation);
            }
        }

        private void RefreshTargetLocalPosition(SuspensionTarget target)
        {
            Transform targetTransform = target.transform;
            Transform parent = targetTransform.parent;
            Vector3 baselineGlobalPosition = parent != null
                ? parent.TransformPoint(target.initialLocalPosition)
                : target.initialLocalPosition;
            Vector3 rayOrigin = targetTransform.position + Vector3.up * rayHeight;

            float targetGlobalY = baselineGlobalPosition.y;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer)
                && hit.point.y > baselineGlobalPosition.y)
            {
                targetGlobalY = hit.point.y;
            }

            Vector3 targetGlobalPosition = new Vector3(
                baselineGlobalPosition.x,
                targetGlobalY,
                baselineGlobalPosition.z) + target.positionOffset;
            target.targetLocalPosition = parent != null
                ? parent.InverseTransformPoint(targetGlobalPosition)
                : targetGlobalPosition;
        }
    }
}
