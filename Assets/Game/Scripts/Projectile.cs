using System;
using UnityEngine;

public interface IDamageable
{
    void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
}

public class Projectile : MonoBehaviour
{
    private const float FlightLifetimePadding = 0.25f;
    private const float MaxFlightLifetime = 5f;
    private const int FlightLifetimeSamples = 32;
    private const int CollisionBufferSize = 128;
    private const float CollisionCastPadding = 0.15f;

    private static readonly RaycastHit[] CollisionBuffer = new RaycastHit[CollisionBufferSize];

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
    private Vector3 _originalStartPoint;
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
    private bool _hasResolvedTarget;
    private bool _explodeAtResolvedTarget;
    private Vector3 _resolvedImpactNormal = Vector3.up;
    private Action _onAuthoritativeResolvedTarget;
    private bool _resolvedTargetHandled;
    private bool _visualsEnabled = true;
    private bool _liveCollisionEnabled;
    private Transform _ignoredRoot;
    private Action<RaycastHit, Vector3> _onAuthoritativeLiveHit;
    private Action _onAuthoritativeLiveMiss;
    private float _missContinuationMaxDistance;
    private bool _missContinuationUsed;
    private Vector3 _lastTravelDirection;
    private float _continuationSpeedMultiplier = -1f;

    public GameObject explosionFX;

    public void SetVisualsEnabled(bool enabled)
    {
        _visualsEnabled = enabled;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = enabled;
            }
        }

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
            {
                continue;
            }

            if (enabled)
            {
                particle.Play();
            }
            else
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

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
        _originalStartPoint = _startPoint;
        _targetPoint = targetPoint;

        _initialSpeed = Mathf.Max(0.0001f, initialSpeed);
        _lifeTime = ClampLifetime(lifeTime);
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
        _minSpeedMultiplier = Mathf.Clamp(minSpeedMultiplier, 0.01f, 1f);

        _distanceTraveled = 0f;
        _continuationSpeedMultiplier = -1f;
        RecomputeTrajectory();
        ExtendLifetimeToReachTarget();

        _passedTimeCatchup = Mathf.Max(0f, passedTime);
        _initialized = true;
        ConfigureScriptedPhysics();

        Vector3 toTarget = (_targetPoint - _startPoint);
        if (toTarget.sqrMagnitude > 1e-6f)
        {
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        _prevPos = transform.position;
        _authoritative = authoritative;
        _lastTravelDirection = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : transform.forward;
    }

    private void ConfigureScriptedPhysics()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    public void ConfigureResolvedImpact(Vector3 targetPoint, Vector3 impactNormal, Action onAuthoritativeImpact = null)
    {
        ConfigureResolvedTarget(targetPoint, impactNormal, true, onAuthoritativeImpact);
    }

    public void ResolveImpactNow(Vector3 impactPoint, Vector3 impactNormal)
    {
        transform.position = impactPoint;
        Explode(impactPoint, impactNormal);
        Destroy(gameObject);
    }

    public void ConfigureResolvedMiss(Vector3 targetPoint, Action onAuthoritativeMiss = null)
    {
        ConfigureResolvedMiss(targetPoint, 0f, onAuthoritativeMiss);
    }

    public void ConfigureResolvedMiss(Vector3 targetPoint, float missContinuationMaxDistance, Action onAuthoritativeMiss = null)
    {
        ConfigureResolvedTarget(targetPoint, Vector3.up, false, onAuthoritativeMiss);
        SetMissContinuationMaxDistance(missContinuationMaxDistance);
    }

    public void SetMissContinuationMaxDistance(float maxDistance)
    {
        _missContinuationMaxDistance = Mathf.Max(0f, maxDistance);
    }

    private void ConfigureResolvedTarget(Vector3 targetPoint, Vector3 impactNormal, bool explodeAtTarget, Action onAuthoritativeArrival)
    {
        _targetPoint = targetPoint;
        _resolvedImpactNormal = impactNormal.sqrMagnitude > 1e-6f ? impactNormal.normalized : Vector3.up;
        _hasResolvedTarget = true;
        _explodeAtResolvedTarget = explodeAtTarget;
        _resolvedTargetHandled = false;
        _liveCollisionEnabled = false;

        _onAuthoritativeResolvedTarget = onAuthoritativeArrival;

        RecomputeTrajectory();
        ExtendLifetimeToReachTarget();
    }

    public void ConfigureLiveCollision(
        Transform ignoredRoot,
        Action<RaycastHit, Vector3> onAuthoritativeHit,
        Action onAuthoritativeMiss,
        float missContinuationMaxDistance = 0f)
    {
        _hasResolvedTarget = false;
        _explodeAtResolvedTarget = false;
        _resolvedTargetHandled = false;
        _liveCollisionEnabled = true;
        _ignoredRoot = ignoredRoot;
        _onAuthoritativeLiveHit = onAuthoritativeHit;
        _onAuthoritativeLiveMiss = onAuthoritativeMiss;
        SetMissContinuationMaxDistance(missContinuationMaxDistance);
    }

    private void RecomputeTrajectory()
    {
        _totalDistance = Vector3.Distance(_startPoint, _targetPoint);

        float scaled = Mathf.Pow(_totalDistance, _arcExponent) * _arcScale;
        _arcHeightComputed = Mathf.Clamp(scaled, _arcMin, _arcMax);
        _arcUp = _arcAlongWorldUp ? Vector3.up : ComputeArcUp(_startPoint, _targetPoint);
    }

    private void ExtendLifetimeToReachTarget()
    {
        float estimatedFlightTime = EstimateFlightTime();
        if (!IsFinite(estimatedFlightTime) || estimatedFlightTime <= 0f)
        {
            return;
        }

        _lifeTime = ClampLifetime(Mathf.Max(_lifeTime, estimatedFlightTime + FlightLifetimePadding));
    }

    private float EstimateFlightTime()
    {
        if (_totalDistance <= 0f)
        {
            return 0f;
        }

        float result = 0f;
        float lastFraction = 0f;
        for (int i = 1; i <= FlightLifetimeSamples; i++)
        {
            float fraction = i / (float)FlightLifetimeSamples;
            float midFraction = (lastFraction + fraction) * 0.5f;
            float segmentDistance = _totalDistance * (fraction - lastFraction);
            float speed = _initialSpeed * GetSpeedMultiplier(midFraction);
            if (speed <= 0.001f)
            {
                return _lifeTime;
            }

            result += segmentDistance / speed;
            lastFraction = fraction;
        }

        return result;
    }

    private float GetSpeedMultiplier(float fraction)
    {
        float slowdownEval = _useSlowdown ? _slowdownCurve.Evaluate(fraction) : 1f;
        float speedMul = 1f - (_useSlowdown ? (_slowdownAmount * Mathf.Pow(fraction, _slowdownExponent) * slowdownEval) : 0f);
        return Mathf.Clamp(speedMul, _minSpeedMultiplier, 1f);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float ClampLifetime(float lifeTime)
    {
        return Mathf.Clamp(lifeTime, 0.01f, MaxFlightLifetime);
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
        if (_visualsEnabled && explosionFX != null)
        {
            Instantiate(explosionFX, pos, Quaternion.LookRotation(normal != Vector3.zero ? normal : Vector3.up));
        }
    }

    private void DestroyWithoutExplosion()
    {
        Destroy(gameObject);
    }

    private void CompleteLiveMiss()
    {
        if (_liveCollisionEnabled && _authoritative && !_resolvedTargetHandled)
        {
            _resolvedTargetHandled = true;
            _onAuthoritativeLiveMiss?.Invoke();
        }
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        if (Time.time - _spawnTime >= _lifeTime)
        {
            CompleteLiveMiss();
            DestroyWithoutExplosion();
            return;
        }

        float fraction = (_totalDistance <= 0f) ? 1f : Mathf.Clamp01(_distanceTraveled / _totalDistance);

        float speedMultiplier = _continuationSpeedMultiplier > 0f
            ? _continuationSpeedMultiplier
            : GetSpeedMultiplier(fraction);
        float currentSpeed = _initialSpeed * speedMultiplier;

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
            _lastTravelDirection = travel / dist;
        }

        if (_liveCollisionEnabled && dist > 1e-6f)
        {
            Vector3 dir = _lastTravelDirection;
            _lastTravelDirection = dir;
            if (TryCastCollision(_prevPos, dir, dist + GetCollisionCastPadding(), out RaycastHit hit))
            {
                if (_authoritative)
                {
                    if (!_resolvedTargetHandled)
                    {
                        _resolvedTargetHandled = true;
                        _onAuthoritativeLiveHit?.Invoke(hit, dir);
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

    private float GetCollisionCastPadding()
    {
        return Mathf.Max(CollisionCastPadding, hitRadius, _arriveThreshold);
    }

    private bool TryCastCollision(Vector3 origin, Vector3 dir, float distance, out RaycastHit bestHit)
    {
        int count;
        if (hitRadius > 0f)
        {
            count = Physics.SphereCastNonAlloc(
                origin,
                hitRadius,
                dir,
                CollisionBuffer,
                distance,
                hitMask,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            count = Physics.RaycastNonAlloc(
                origin,
                dir,
                CollisionBuffer,
                distance,
                hitMask,
                QueryTriggerInteraction.Ignore
            );
        }

        int bestIndex = -1;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = CollisionBuffer[i].collider;
            if (hitCollider == null || IsUnderRoot(hitCollider.transform, _ignoredRoot))
            {
                continue;
            }

            float hitDistance = CollisionBuffer[i].distance;
            if (hitDistance < bestDistance)
            {
                bestDistance = hitDistance;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            bestHit = CollisionBuffer[bestIndex];
            return true;
        }

        bestHit = default;
        return false;
    }

    private static bool IsUnderRoot(Transform transform, Transform root)
    {
        if (root == null)
        {
            return false;
        }

        Transform current = transform;
        while (current != null)
        {
            if (current == root)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void OnArrived()
    {
        if (_hasResolvedTarget)
        {
            if (!_explodeAtResolvedTarget && TryContinueMissFlight())
            {
                return;
            }

            transform.position = _targetPoint;

            if (_authoritative && !_resolvedTargetHandled)
            {
                _resolvedTargetHandled = true;
                _onAuthoritativeResolvedTarget?.Invoke();
            }

            if (_explodeAtResolvedTarget)
            {
                Explode(_targetPoint, _resolvedImpactNormal);
            }

            Destroy(gameObject);
            return;
        }

        if (TryContinueMissFlight())
        {
            return;
        }

        CompleteLiveMiss();
        DestroyWithoutExplosion();
    }

    private bool TryContinueMissFlight()
    {
        if (_missContinuationUsed || _missContinuationMaxDistance <= 0f)
        {
            return false;
        }

        float traveledFromOriginal = Vector3.Distance(_originalStartPoint, transform.position);
        float remainingDistance = _missContinuationMaxDistance - traveledFromOriginal;
        if (remainingDistance <= Mathf.Max(_arriveThreshold, 0.1f))
        {
            return false;
        }

        Vector3 direction = GetMissContinuationDirection();

        if (direction.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        direction.Normalize();
        _missContinuationUsed = true;
        _startPoint = transform.position;
        _targetPoint = _startPoint + direction * remainingDistance;
        _distanceTraveled = 0f;
        _prevPos = _startPoint;
        _hasResolvedTarget = false;
        _explodeAtResolvedTarget = false;
        _useArc = false;
        _continuationSpeedMultiplier = GetSpeedMultiplier(1f);

        RecomputeTrajectory();
        _spawnTime = Time.time;
        _lifeTime = ClampLifetime(EstimateFlightTime() + FlightLifetimePadding);

        return true;
    }

    private Vector3 GetMissContinuationDirection()
    {
        Vector3 direction = _targetPoint - _startPoint;
        if (direction.sqrMagnitude > 0.000001f)
        {
            return direction;
        }

        if (_lastTravelDirection.sqrMagnitude > 0.000001f)
        {
            return _lastTravelDirection;
        }

        return transform.forward;
    }
}
