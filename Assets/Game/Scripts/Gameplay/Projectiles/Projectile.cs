using System;
using Game.Scripts.Gameplay.Robots;
using UnityEngine;

public interface IDamageable
{
    void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
}

public class Projectile : MonoBehaviour
{
    private const int CollisionBufferSize = 128;
    private const int TravelDistanceSamples = 12;
    private const float CollisionCastPadding = 0.01f;
    private const float MinLifetime = 0.01f;

    private static readonly RaycastHit[] CollisionBuffer = new RaycastHit[CollisionBufferSize];

    public LayerMask hitMask = ~0;
    public float hitRadius = 0.05f;
    public int damage = 40;
    public GameObject explosionFX;

    [Header("Debug")]
    public bool debugDrawTrajectory;
    public bool debugDrawSweepSegment;
    public bool debugDrawCollisionRadius;
    public bool debugDrawHitPoint;
    public bool debugDrawVelocityDirection;
    public bool debugBallisticTrajectory;
    [Min(0.1f)] public float debugTrajectorySeconds = 3f;
    [Range(4, 96)] public int debugTrajectorySteps = 32;
    [Min(0f)] public float debugDrawDuration = 0.05f;
    [Min(0f)] public float debugHitDrawDuration = 2f;

    private Vector3 _origin;
    private Vector3 _initialVelocity;
    private Vector3 _gravity;
    private Vector3 _previousPosition;
    private Vector3 _currentVelocity;
    private Vector3 _lastHitPoint;
    private Vector3 _lastHitNormal = Vector3.up;
    private Vector3 _debugAimPoint;
    private Vector3 _debugInitialDirection;

    private float _elapsedTime;
    private float _maxLifetime;
    private float _maxDistance;
    private float _travelledDistance;
    private float _collisionRadius;
    private float _pendingAuthoritativeCatchupTime;
    private float _debugGravityValue;
    private float _debugEstimatedDrop;

    private bool _initialized;
    private bool _authoritative;
    private bool _visualsEnabled = true;
    private bool _liveCollisionEnabled;
    private bool _resolvedTargetHandled;
    private bool _hasLastHitPoint;
    private bool _hasDebugAimPoint;
    private bool _debugUsedBallisticCompensation;
    private bool _debugBallisticSolutionFound;

    private Transform _ignoredRoot;
    private Action<RaycastHit, Vector3> _onAuthoritativeLiveHit;
    private Action _onAuthoritativeLiveMiss;

    public Vector3 Origin => _origin;
    public Vector3 InitialVelocity => _initialVelocity;
    public Vector3 Gravity => _gravity;
    public float ElapsedTime => _elapsedTime;
    public float TravelledDistance => _travelledDistance;
    public bool IsAuthoritative => _authoritative;

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
        Vector3 origin,
        Vector3 initialVelocity,
        Vector3 gravity,
        float maxLifetime,
        float maxDistance,
        float collisionRadius,
        float passedTime = 0f,
        bool authoritative = false)
    {
        _origin = BallisticProjectileMath.IsFinite(origin) ? origin : transform.position;
        _initialVelocity = BallisticProjectileMath.IsFinite(initialVelocity) ? initialVelocity : Vector3.zero;
        _gravity = BallisticProjectileMath.IsFinite(gravity) ? gravity : Vector3.zero;
        _maxLifetime = Mathf.Max(MinLifetime, maxLifetime);
        _maxDistance = Mathf.Max(0f, maxDistance);
        _collisionRadius = Mathf.Max(0f, collisionRadius);
        hitRadius = _collisionRadius;

        _authoritative = authoritative;
        _liveCollisionEnabled = false;
        _resolvedTargetHandled = false;
        _hasLastHitPoint = false;

        _elapsedTime = authoritative ? 0f : Mathf.Max(0f, passedTime);
        _pendingAuthoritativeCatchupTime = authoritative ? Mathf.Max(0f, passedTime) : 0f;
        _travelledDistance = EstimateTravelledDistance(0f, _elapsedTime);

        Vector3 currentPosition = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, _elapsedTime);
        transform.position = currentPosition;
        _previousPosition = currentPosition;
        _currentVelocity = BallisticProjectileMath.GetVelocity(_initialVelocity, _gravity, _elapsedTime);
        ApplyRotation(_currentVelocity);

        _initialized = true;
        ConfigureScriptedPhysics();
    }

    public void ReconfigureBallistic(Vector3 initialVelocity, Vector3 gravity)
    {
        if (!BallisticProjectileMath.IsFinite(initialVelocity) || !BallisticProjectileMath.IsFinite(gravity))
        {
            return;
        }

        _initialVelocity = initialVelocity;
        _gravity = gravity;

        Vector3 correctedPosition = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, _elapsedTime);
        _currentVelocity = BallisticProjectileMath.GetVelocity(_initialVelocity, _gravity, _elapsedTime);
        _previousPosition = correctedPosition;
        transform.position = correctedPosition;
        ApplyRotation(_currentVelocity);
    }

    public void ConfigureBallisticDebug(
        Vector3 aimPoint,
        Vector3 initialVelocity,
        Vector3 gravity,
        float estimatedDirectDrop,
        bool usedBallisticCompensation,
        bool ballisticSolutionFound,
        bool enableDebug)
    {
        _hasDebugAimPoint = BallisticProjectileMath.IsFinite(aimPoint);
        _debugAimPoint = aimPoint;
        _debugInitialDirection = initialVelocity.sqrMagnitude > 0.000001f
            ? initialVelocity.normalized
            : Vector3.forward;
        _debugGravityValue = gravity.magnitude;
        _debugEstimatedDrop = Mathf.Max(0f, estimatedDirectDrop);
        _debugUsedBallisticCompensation = usedBallisticCompensation;
        _debugBallisticSolutionFound = ballisticSolutionFound;

        if (enableDebug)
        {
            debugBallisticTrajectory = true;
            debugDrawVelocityDirection = true;
        }
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
    }

    public void ResolveImpactNow(Vector3 impactPoint, Vector3 impactNormal)
    {
        _hasLastHitPoint = true;
        _lastHitPoint = impactPoint;
        _lastHitNormal = impactNormal.sqrMagnitude > 0.000001f ? impactNormal.normalized : Vector3.up;

        transform.position = impactPoint;
        DrawDebugHit(impactPoint, _lastHitNormal);
        Explode(impactPoint, _lastHitNormal);
        Destroy(gameObject);
    }

    public void ConfigureResolvedMiss(Vector3 targetPoint, Action onAuthoritativeMiss = null)
    {
        ConfigureResolvedMiss(targetPoint, 0f, onAuthoritativeMiss);
    }

    public void ConfigureResolvedMiss(Vector3 targetPoint, float missContinuationMaxDistance, Action onAuthoritativeMiss = null)
    {
        SetMissContinuationMaxDistance(missContinuationMaxDistance);

        if (onAuthoritativeMiss != null)
        {
            _onAuthoritativeLiveMiss = onAuthoritativeMiss;
        }
    }

    public void SetMissContinuationMaxDistance(float maxDistance)
    {
        SetMaxDistance(maxDistance);
    }

    public void SetMaxDistance(float maxDistance)
    {
        if (maxDistance > 0f)
        {
            _maxDistance = Mathf.Max(_maxDistance, maxDistance);
        }
    }

    public void ConfigureLiveCollision(
        Transform ignoredRoot,
        Action<RaycastHit, Vector3> onAuthoritativeHit,
        Action onAuthoritativeMiss,
        float missContinuationMaxDistance = 0f)
    {
        _ignoredRoot = ignoredRoot;
        _onAuthoritativeLiveHit = onAuthoritativeHit;
        _onAuthoritativeLiveMiss = onAuthoritativeMiss;
        _liveCollisionEnabled = _authoritative;
        _resolvedTargetHandled = false;
        SetMissContinuationMaxDistance(missContinuationMaxDistance);
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        float dt = Time.deltaTime + _pendingAuthoritativeCatchupTime;
        _pendingAuthoritativeCatchupTime = 0f;
        if (dt <= 0f)
        {
            return;
        }

        Vector3 previousPosition = _previousPosition;
        float previousTime = _elapsedTime;
        _elapsedTime += dt;

        BallisticProjectileMath.TravelSegment segment = BallisticProjectileMath.GetTravelSegment(
            _origin,
            _initialVelocity,
            _gravity,
            previousTime,
            _elapsedTime);

        Vector3 newPosition = segment.CurrentPosition;
        _currentVelocity = segment.CurrentVelocity;

        Vector3 travel = newPosition - previousPosition;
        float segmentDistance = travel.magnitude;
        Vector3 travelDirection = segmentDistance > 0.000001f
            ? travel / segmentDistance
            : GetSafeVelocityDirection();

        if (segmentDistance > 0f)
        {
            _travelledDistance += segmentDistance;
        }

        DrawDebugStep(previousPosition, newPosition, _currentVelocity);

        if (_liveCollisionEnabled && segmentDistance > 0.000001f)
        {
            float castDistance = segmentDistance + GetCollisionCastPadding();
            if (TryCastCollision(previousPosition, travelDirection, castDistance, out RaycastHit hit))
            {
                HandleLiveHit(hit, travelDirection);
                return;
            }
        }

        transform.position = newPosition;
        _previousPosition = newPosition;
        ApplyRotation(_currentVelocity);

        if (HasExceededLimits())
        {
            CompleteLiveMiss();
            DestroyWithoutExplosion();
        }
    }

    private void HandleLiveHit(RaycastHit hit, Vector3 travelDirection)
    {
        _hasLastHitPoint = true;
        _lastHitPoint = hit.point;
        _lastHitNormal = hit.normal.sqrMagnitude > 0.000001f ? hit.normal.normalized : Vector3.up;
        transform.position = hit.point;
        _previousPosition = hit.point;

        if (_authoritative && !_resolvedTargetHandled)
        {
            _resolvedTargetHandled = true;
            _onAuthoritativeLiveHit?.Invoke(hit, travelDirection);
        }

        DrawDebugHit(hit.point, _lastHitNormal);
        Explode(hit.point, _lastHitNormal);
        Destroy(gameObject);
    }

    private bool HasExceededLimits()
    {
        if (_elapsedTime >= _maxLifetime)
        {
            return true;
        }

        if (_maxDistance > 0f && _travelledDistance >= _maxDistance)
        {
            return true;
        }

        return false;
    }

    private float EstimateTravelledDistance(float startTime, float endTime)
    {
        if (endTime <= startTime)
        {
            return 0f;
        }

        int samples = Mathf.Max(1, TravelDistanceSamples);
        float distance = 0f;
        Vector3 previous = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, startTime);
        for (int i = 1; i <= samples; i++)
        {
            float t = Mathf.Lerp(startTime, endTime, i / (float)samples);
            Vector3 current = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, t);
            distance += Vector3.Distance(previous, current);
            previous = current;
        }

        return distance;
    }

    private float GetCollisionCastPadding()
    {
        return Mathf.Max(CollisionCastPadding, _collisionRadius * 0.5f);
    }

    private Vector3 GetSafeVelocityDirection()
    {
        if (_currentVelocity.sqrMagnitude > 0.000001f)
        {
            return _currentVelocity.normalized;
        }

        if (_initialVelocity.sqrMagnitude > 0.000001f)
        {
            return _initialVelocity.normalized;
        }

        return transform.forward.sqrMagnitude > 0.000001f ? transform.forward : Vector3.forward;
    }

    private bool TryCastCollision(Vector3 origin, Vector3 direction, float distance, out RaycastHit bestHit)
    {
        int count;
        if (_collisionRadius > 0f)
        {
            count = Physics.SphereCastNonAlloc(
                origin,
                _collisionRadius,
                direction,
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
                direction,
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

    private void ApplyRotation(Vector3 velocity)
    {
        if (velocity.sqrMagnitude > 0.000001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
    }

    private void Explode(Vector3 position, Vector3 normal)
    {
        if (_visualsEnabled && explosionFX != null)
        {
            Vector3 safeNormal = normal.sqrMagnitude > 0.000001f ? normal.normalized : Vector3.up;
            Instantiate(explosionFX, position, Quaternion.LookRotation(safeNormal));
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

    private void DrawDebugStep(Vector3 previousPosition, Vector3 newPosition, Vector3 velocity)
    {
        if (debugDrawSweepSegment)
        {
            Debug.DrawLine(previousPosition, newPosition, Color.yellow, debugDrawDuration);
        }

        if (debugDrawVelocityDirection && velocity.sqrMagnitude > 0.000001f)
        {
            Debug.DrawRay(newPosition, velocity.normalized * 2f, Color.blue, debugDrawDuration);
        }

        if (debugDrawCollisionRadius && _collisionRadius > 0f)
        {
            DrawDebugSphere(newPosition, _collisionRadius, Color.green, debugDrawDuration);
        }

        if (debugDrawTrajectory || debugBallisticTrajectory)
        {
            DrawDebugTrajectory(_elapsedTime, debugTrajectorySeconds, debugTrajectorySteps, debugDrawDuration);
        }
    }

    private void DrawDebugHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!debugDrawHitPoint)
        {
            return;
        }

        DrawDebugSphere(hitPoint, Mathf.Max(0.05f, _collisionRadius), Color.red, debugHitDrawDuration);
        Debug.DrawRay(hitPoint, hitNormal.normalized * 1.5f, Color.red, debugHitDrawDuration);
    }

    private void DrawDebugTrajectory(float startTime, float seconds, int steps, float duration)
    {
        int safeSteps = Mathf.Clamp(steps, 4, 96);
        float safeSeconds = Mathf.Max(0.1f, seconds);
        Vector3 previous = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, startTime);
        for (int i = 1; i <= safeSteps; i++)
        {
            float t = startTime + safeSeconds * (i / (float)safeSteps);
            Vector3 current = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, t);
            Debug.DrawLine(previous, current, Color.cyan, duration);
            previous = current;
        }
    }

    private static void DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
    {
        float r = Mathf.Max(0.001f, radius);
        Debug.DrawLine(center + Vector3.right * r, center - Vector3.right * r, color, duration);
        Debug.DrawLine(center + Vector3.up * r, center - Vector3.up * r, color, duration);
        Debug.DrawLine(center + Vector3.forward * r, center - Vector3.forward * r, color, duration);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_initialized)
        {
            return;
        }

        if (debugDrawTrajectory || debugBallisticTrajectory)
        {
            Gizmos.color = Color.cyan;
            int safeSteps = Mathf.Clamp(debugTrajectorySteps, 4, 96);
            float safeSeconds = Mathf.Max(0.1f, debugTrajectorySeconds);
            Vector3 previous = transform.position;
            for (int i = 1; i <= safeSteps; i++)
            {
                float t = _elapsedTime + safeSeconds * (i / (float)safeSteps);
                Vector3 current = BallisticProjectileMath.GetPosition(_origin, _initialVelocity, _gravity, t);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }

        if (debugBallisticTrajectory && _hasDebugAimPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_debugAimPoint, 0.25f);
            Gizmos.DrawLine(_origin, _origin + _debugInitialDirection * 4f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(_debugAimPoint, _debugAimPoint + Vector3.down * Mathf.Max(0.25f, _debugEstimatedDrop));

            Gizmos.color = _debugUsedBallisticCompensation && _debugBallisticSolutionFound
                ? Color.green
                : Color.white;
            Gizmos.DrawLine(_origin, _origin + _gravity.normalized * Mathf.Clamp(_debugGravityValue, 0.25f, 4f));
        }

        if (debugDrawSweepSegment)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_previousPosition, transform.position);
        }

        if (debugDrawCollisionRadius && _collisionRadius > 0f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _collisionRadius);
        }

        if (debugDrawVelocityDirection && _currentVelocity.sqrMagnitude > 0.000001f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _currentVelocity.normalized * 2f);
        }

        if (debugDrawHitPoint && _hasLastHitPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_lastHitPoint, Mathf.Max(0.05f, _collisionRadius));
            Gizmos.DrawLine(_lastHitPoint, _lastHitPoint + _lastHitNormal.normalized * 1.5f);
        }
    }
}
