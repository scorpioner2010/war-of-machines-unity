using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class CaterpillarBoneAdjuster : MonoBehaviour
    {
        public float rayHeight = 1f;
        public float rayDistance = 2f;
        public LayerMask groundLayer;
        public float lerpSpeed = 10f;
        public Vector3 positionOffset = Vector3.zero;

        private Vector3 _initialLocalPos;
        private RaycastHit _lastHit;
        private bool _didHit;

        private void Start()
        {
            _initialLocalPos = transform.localPosition;
        }

        private void Update()
        {
            Vector3 baselineGlobalPos = transform.parent
                ? transform.parent.TransformPoint(_initialLocalPos)
                : _initialLocalPos;

            Vector3 rayOrigin = transform.position + Vector3.up * rayHeight;
            Ray ray = new Ray(rayOrigin, Vector3.down);

            float targetGlobalY = baselineGlobalPos.y;
            _didHit = Physics.Raycast(ray, out _lastHit, rayDistance, groundLayer);
            
            if (_didHit)
            {
                float hitY = _lastHit.point.y;
                if (hitY > baselineGlobalPos.y)
                    targetGlobalY = hitY;
            }

            Vector3 targetGlobalPos = new Vector3(baselineGlobalPos.x, targetGlobalY, baselineGlobalPos.z) + positionOffset;

            Vector3 targetLocalPos = transform.parent
                ? transform.parent.InverseTransformPoint(targetGlobalPos)
                : targetGlobalPos;

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * lerpSpeed);
        }

        private void OnDrawGizmos()
        {
            return;
            Vector3 origin = transform.position + Vector3.up * rayHeight;
            Vector3 direction = Vector3.down * rayDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + direction);

            if (_didHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_lastHit.point, 0.05f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(origin + direction, 0.05f);
            }
        }
    }
}
