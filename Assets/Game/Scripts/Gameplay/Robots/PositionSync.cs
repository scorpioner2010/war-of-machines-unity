using Game.Scripts.Diagnostics;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class PositionSync : MonoBehaviour
    {
        public Transform target;
        public VehicleRoot vehicleRoot;

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

                transform.position = target.position + target.TransformVector(_targetSpaceOffset);
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
            if (_attachedToTarget || target == null)
            {
                return;
            }

            transform.SetParent(target, true);
            transform.localPosition = _targetSpaceOffset;
            _attachedToTarget = true;
        }
    }
}
