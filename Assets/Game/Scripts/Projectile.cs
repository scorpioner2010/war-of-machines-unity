using UnityEngine;

public interface IDamageable
{
    void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
}

public class Projectile : MonoBehaviour
{
    public LayerMask hitMask = ~0;
    public float hitRadius = 0.05f;
    public int damage = 40;

    private float _initialSpeed;
    private float _lifeTime;
    private float _arriveThreshold = 0.1f;

    private bool _useArc;
    private float _arcScale;
    private float _arcMin;
    private float _arcMax;
    private float _arcExponent;
    private AnimationCurve _arcCurve;
    private bool _arcAlongWorldUp;

    private bool _useSlowdown;
    private float _slowdownAmount;
    private float _slowdownExponent;
    private AnimationCurve _slowdownCurve;
    private float _minSpeedMultiplier;

    private Vector3 _startPoint;
    private Vector3 _targetPoint;
    private float _spawnTime;
    private float _totalDistance;
    private float _distanceTraveled;
    private bool _initialized;

    private float _arcHeightComputed;
    private Vector3 _arcUp;

    private float _passedTimeCatchup;

    private Vector3 _prevPos;
    private bool _authoritative;

    public GameObject explosionFX;

    public void Init(
        Vector3 targetPoint,
        float initialSpeed,
        float lifeTime,
        bool useArc,
        float arcScale,
        float arcMin,
        float arcMax,
        float arcExponent,
        AnimationCurve arcCurve,
        bool arcAlongWorldUp,
        bool useSlowdown,
        float slowdownAmount,
        float slowdownExponent,
        AnimationCurve slowdownCurve,
        float minSpeedMultiplier = 0.05f,
        float passedTime = 0f,
        bool authoritative = false
    )
    {
        _startPoint = transform.position;
        _targetPoint = targetPoint;

        _initialSpeed = Mathf.Max(0.0001f, initialSpeed);
        _lifeTime = lifeTime;
        _spawnTime = Time.time;

        _useArc = useArc;
        _arcScale = arcScale;
        _arcMin = arcMin;
        _arcMax = arcMax;
        _arcExponent = Mathf.Max(0.0001f, arcExponent);
        _arcCurve = (arcCurve != null && arcCurve.length > 0) ? arcCurve : DefaultArcCurve();
        _arcAlongWorldUp = arcAlongWorldUp;

        _useSlowdown = useSlowdown;
        _slowdownAmount = Mathf.Clamp01(slowdownAmount);
        _slowdownExponent = Mathf.Max(0.0001f, slowdownExponent);
        _slowdownCurve = (slowdownCurve != null && slowdownCurve.length > 0) ? slowdownCurve : DefaultSlowdownCurve();
        _minSpeedMultiplier = Mathf.Clamp(minSpeedMultiplier, 0f, 1f);

        _totalDistance = Vector3.Distance(_startPoint, _targetPoint);
        _distanceTraveled = 0f;

        float scaled = Mathf.Pow(_totalDistance, _arcExponent) * _arcScale;
        _arcHeightComputed = Mathf.Clamp(scaled, _arcMin, _arcMax);
        _arcUp = _arcAlongWorldUp ? Vector3.up : ComputeArcUp(_startPoint, _targetPoint);

        _passedTimeCatchup = Mathf.Max(0f, passedTime);
        _initialized = true;

        Vector3 toTarget = (_targetPoint - _startPoint);
        if (toTarget.sqrMagnitude > 1e-6f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        _prevPos = transform.position;
        _authoritative = authoritative;
    }

    private AnimationCurve DefaultArcCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, 4f, 0f)
        );
    }

    private AnimationCurve DefaultSlowdownCurve()
    {
        return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
    }

    private Vector3 ComputeArcUp(Vector3 start, Vector3 target)
    {
        Vector3 dir = (target - start).normalized;
        Vector3 any = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 up = Vector3.ProjectOnPlane(any, dir).normalized;
        if (up.sqrMagnitude < 1e-6f)
        {
            return Vector3.up;
        }
        return up;
    }

    private void Explode(Vector3 pos, Vector3 normal)
    {
        if (explosionFX != null)
        {
            Instantiate(explosionFX, pos, Quaternion.LookRotation(normal != Vector3.zero ? normal : Vector3.up));
        }
    }

    private void ExplodeAndDestroy()
    {
        Explode(transform.position, Vector3.up);
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        if (Time.time - _spawnTime >= _lifeTime)
        {
            ExplodeAndDestroy();
            return;
        }

        float fraction = (_totalDistance <= 0f) ? 1f : Mathf.Clamp01(_distanceTraveled / _totalDistance);

        float slowdownEval = _useSlowdown ? _slowdownCurve.Evaluate(fraction) : 1f;
        float speedMul = 1f - (_useSlowdown ? (_slowdownAmount * Mathf.Pow(fraction, _slowdownExponent) * slowdownEval) : 0f);
        speedMul = Mathf.Clamp(speedMul, _minSpeedMultiplier, 1f);

        float currentSpeed = _initialSpeed * speedMul;

        float catchupDelta = 0f;
        if (_passedTimeCatchup > 0f)
        {
            float stepCatch = _passedTimeCatchup * 0.08f;
            _passedTimeCatchup -= stepCatch;
            if (_passedTimeCatchup <= (Time.deltaTime * 0.5f))
            {
                stepCatch += _passedTimeCatchup;
                _passedTimeCatchup = 0f;
            }
            catchupDelta = stepCatch;
        }

        float dt = Time.deltaTime + catchupDelta;

        float stepLen = currentSpeed * dt;
        _distanceTraveled += stepLen;

        float newFraction = (_totalDistance <= 0f) ? 1f : Mathf.Clamp01(_distanceTraveled / _totalDistance);
        Vector3 basePos = Vector3.Lerp(_startPoint, _targetPoint, newFraction);
        float heightMul = _useArc ? _arcCurve.Evaluate(newFraction) : 0f;
        Vector3 newPos = basePos + _arcUp * (_arcHeightComputed * heightMul);

        Vector3 travel = newPos - _prevPos;
        float dist = travel.magnitude;
        if (dist > 1e-6f)
        {
            Vector3 dir = travel / dist;
            bool hitSomething = false;
            RaycastHit hit;

            if (hitRadius > 0f)
            {
                hitSomething = Physics.SphereCast(_prevPos, hitRadius, dir, out hit, dist, hitMask, QueryTriggerInteraction.Ignore);
            }
            else
            {
                hitSomething = Physics.Raycast(_prevPos, dir, out hit, dist, hitMask, QueryTriggerInteraction.Ignore);
            }

            if (hitSomething)
            {
                if (_authoritative)
                {
                    Transform t = hit.collider.transform;
                    IDamageable dmg = t.GetComponentInParent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.ApplyDamage(damage, hit.point, hit.normal);
                    }
                }

                Explode(hit.point, hit.normal);
                Destroy(gameObject);
                return;
            }
        }

        float lookAheadDistance = Mathf.Max(0.01f, _initialSpeed * 0.02f);
        float aheadDist = Mathf.Min(_distanceTraveled + lookAheadDistance, _totalDistance);
        float aheadFrac = (_totalDistance <= 0f) ? 1f : Mathf.Clamp01(aheadDist / _totalDistance);
        Vector3 aheadBase = Vector3.Lerp(_startPoint, _targetPoint, aheadFrac);
        float aheadHMul = _useArc ? _arcCurve.Evaluate(aheadFrac) : 0f;
        Vector3 aheadPos = aheadBase + _arcUp * (_arcHeightComputed * aheadHMul);

        Vector3 vdir = (aheadPos - newPos);
        if (vdir.sqrMagnitude > 1e-8f)
        {
            transform.rotation = Quaternion.LookRotation(vdir.normalized, Vector3.up);
        }

        _prevPos = newPos;
        transform.position = newPos;

        if (newFraction >= 1f || Vector3.Distance(transform.position, _targetPoint) <= _arriveThreshold)
        {
            OnArrived();
        }
    }

    private void OnArrived()
    {
        ExplodeAndDestroy();
    }
}

