using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class FootPlacer : MonoBehaviour 
    { 
        public LayerMask groundLayer;
        public float groundCheckDistance = 1f;
        public float footOffset = 0.05f;

        private Vector3 _neutralLocalPos;
        private Transform _parentTransform;

        private void Start() 
        { 
            _parentTransform = transform.parent; 
            _neutralLocalPos = transform.localPosition; 
        } 

        public void SetTargetOffset(Vector3 offset, float groundBlend) 
        {
            if (_parentTransform == null)
            {
                return;
            }
            
            Vector3 targetWorldPos = _parentTransform.TransformPoint(_neutralLocalPos + offset); 
            Vector3 rayOrigin = targetWorldPos + Vector3.up * 0.5f; 
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) 
            {
                float groundY = hit.point.y;
                targetWorldPos.y = Mathf.Lerp(targetWorldPos.y, groundY + footOffset, groundBlend);
            }
            else
            {
                targetWorldPos.y = Mathf.Lerp(targetWorldPos.y, _parentTransform.position.y + footOffset, groundBlend);
            }
            transform.localPosition = _parentTransform.InverseTransformPoint(targetWorldPos);
        }
    }
}
