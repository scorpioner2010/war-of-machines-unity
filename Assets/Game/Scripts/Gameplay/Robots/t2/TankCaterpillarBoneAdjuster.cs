using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class CaterpillarBoneAdjuster : MonoBehaviour
    {
        public float rayHeight = 1f;
        public float rayDistance = 2f;
        public LayerMask groundLayer;
        public float lerpSpeed = 10f;
        [Min(0.01f)] public float raycastInterval = 0.05f;
        public Vector3 positionOffset = Vector3.zero;

        private Vector3 _initialLocalPos;
        private Vector3 _targetLocalPos;
        private RaycastHit _lastHit;
        private float _nextRaycastTime;
        private bool _didHit;
        private bool _hasTargetLocalPos;

        private void Start()
        {
            _initialLocalPos = transform.localPosition;
            _targetLocalPos = _initialLocalPos;
            _hasTargetLocalPos = true;
            _nextRaycastTime = Time.time + Random.Range(0f, Mathf.Max(0.01f, raycastInterval));
        }

        private void Update()
        {
            if (!_hasTargetLocalPos || Time.time >= _nextRaycastTime)
            {
                RefreshTargetLocalPosition();
                _nextRaycastTime = Time.time + Mathf.Max(0.01f, raycastInterval);
            }

            transform.localPosition = Vector3.Lerp(transform.localPosition, _targetLocalPos, Time.deltaTime * lerpSpeed);
        }

        private void RefreshTargetLocalPosition()
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
                {
                    targetGlobalY = hitY;
                }
            }

            Vector3 targetGlobalPos = new Vector3(baselineGlobalPos.x, targetGlobalY, baselineGlobalPos.z) + positionOffset;

            _targetLocalPos = transform.parent
                ? transform.parent.InverseTransformPoint(targetGlobalPos)
                : targetGlobalPos;
            _hasTargetLocalPos = true;
        }

    }
}
