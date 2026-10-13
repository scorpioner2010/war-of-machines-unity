using Game.Scripts.Diagnostics;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class PositionSync : MonoBehaviour
    {
        public Transform target;
        public VehicleRoot vehicleRoot;
        public VehicleBodyVerticalSpringController bodyVerticalSpringController;

        private Vector3 _targetSpaceOffset;
        private bool _hasTargetSpaceOffset;
        private bool _attachedToTarget;

        private void Awake()
        {
            CacheTargetSpaceOffset();
        }

        private void OnEnable()
        {
            CacheTargetSpaceOffset();
        }

        private void LateUpdate()
        {
            using (ProfileScope.Measure("Client.Interpolation.PositionSync.LateUpdate", DiagnosticsCategories.Client))
            {
                if (target == null)
                {
                    return;
                }

                if (!_hasTargetSpaceOffset)
                {
                    CacheTargetSpaceOffset();
                }

                if (ShouldUseHierarchyFollow())
                {
                    AttachToTarget();
                    return;
                }

                transform.position = target.position + target.TransformVector(GetTargetSpaceOffset());
            }
        }

        private void CacheTargetSpaceOffset()
        {
            if (target == null || _hasTargetSpaceOffset)
            {
                return;
            }

            _targetSpaceOffset = target.InverseTransformVector(transform.position - target.position);
            _hasTargetSpaceOffset = true;
        }

        private bool ShouldUseHierarchyFollow()
        {
            return vehicleRoot != null && vehicleRoot.IsHostInitialized;
        }

    private void AttachToTarget()
    {
        if (target == null)
        {
            return;
        }

        if (!_attachedToTarget)
        {
            transform.SetParent(target, true);
            _attachedToTarget = true;
        }

        transform.localPosition = GetTargetSpaceOffset();
    }

        private Vector3 GetTargetSpaceOffset()
        {
            Vector3 targetSpaceOffset = _targetSpaceOffset;
            if (bodyVerticalSpringController != null)
            {
                targetSpaceOffset.y += bodyVerticalSpringController.VerticalOffset;
            }

            return targetSpaceOffset;
        }
    }
}
