using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ObjectPuter : MonoBehaviour
{
    private const string GroundLayerName = "Ground";
    private const int DefaultGroundLayer = 3;
    private const int MaxRaycastHits = 64;
    private const int MaxSamplesPerAxis = 16;
    private const float MinNormalSqrMagnitude = 0.0001f;

    private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastHits];

    [FormerlySerializedAs("terrainMask")]
    [SerializeField] private LayerMask groundMask = 1 << DefaultGroundLayer;
    [SerializeField] private float rayStartHeight = 100f;
    [SerializeField] private float rayDistance = 250f;
    [SerializeField] private float groundOffset;
    [SerializeField] private int samplesPerAxis = 3;
    [SerializeField] private bool alignToGroundNormal = true;
    [SerializeField] private bool includeTriggerColliders;
    [SerializeField] private Collider[] boundsColliders = System.Array.Empty<Collider>();
    [SerializeField] private Renderer[] boundsRenderers = System.Array.Empty<Renderer>();
    [SerializeField, HideInInspector] private bool normalizedLegacyGroundOffset;

    private void Reset()
    {
        normalizedLegacyGroundOffset = true;
        NormalizeSettings(true);
    }

    private void OnValidate()
    {
        NormalizeSettings(false);
    }

    [Button("Put")]
    private void Put()
    {
        NormalizeSettings(false);

        if (groundMask.value == 0)
        {
            Debug.LogWarning("ObjectPuter: Ground layer mask is empty.", this);
            return;
        }

        if (!TryGetObjectBounds(out Bounds objectBounds))
        {
            Debug.LogWarning("ObjectPuter: object has no collider or renderer bounds.", this);
            return;
        }

        if (!TryGetGroundNormal(objectBounds, out Vector3 groundNormal))
        {
            Debug.LogWarning("ObjectPuter: Ground layer was not found under this object.", this);
            return;
        }

#if UNITY_EDITOR
        Undo.RecordObject(transform, "Put object on ground");
#endif

        Vector3 placementNormal = alignToGroundNormal ? groundNormal : Vector3.up;

        if (alignToGroundNormal)
        {
            transform.rotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
        }

        if (!TryGetObjectBounds(out objectBounds))
        {
            Debug.LogWarning("ObjectPuter: object has no collider or renderer bounds after rotation.", this);
            return;
        }

        if (!TryGetGroundDistance(objectBounds, placementNormal, out float groundDistance))
        {
            Debug.LogWarning("ObjectPuter: Ground layer was not found under this object after rotation.", this);
            return;
        }

        if (!TryGetLowestObjectOffset(placementNormal, out float lowestObjectOffset))
        {
            Debug.LogWarning("ObjectPuter: object has no collider or renderer points to place on ground.", this);
            return;
        }

        float currentLowestDistance = Vector3.Dot(transform.position, placementNormal) + lowestObjectOffset;
        float targetLowestDistance = groundDistance + groundOffset;
        transform.position += placementNormal * (targetLowestDistance - currentLowestDistance);

#if UNITY_EDITOR
        EditorUtility.SetDirty(transform);
        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void AssignGroundMask(bool force)
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (groundLayer < 0)
        {
            return;
        }

        int groundLayerMask = 1 << groundLayer;
        if (force || groundMask.value == 0 || groundMask.value == ~0)
        {
            groundMask = groundLayerMask;
        }
    }

    private void NormalizeSettings(bool forceGroundMask)
    {
        AssignGroundMask(forceGroundMask);

        if (!normalizedLegacyGroundOffset)
        {
            normalizedLegacyGroundOffset = true;
        }

        rayStartHeight = Mathf.Max(0.01f, rayStartHeight);
        rayDistance = Mathf.Max(rayStartHeight + 0.01f, rayDistance);
        samplesPerAxis = Mathf.Clamp(samplesPerAxis, 1, MaxSamplesPerAxis);
    }

    private bool TryGetObjectBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; boundsColliders != null && i < boundsColliders.Length; i++)
        {
            Collider targetCollider = boundsColliders[i];
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            if (!includeTriggerColliders && targetCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetCollider.bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        for (int i = 0; boundsRenderers != null && i < boundsRenderers.Length; i++)
        {
            Renderer targetRenderer = boundsRenderers[i];
            if (targetRenderer == null || !targetRenderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private bool TryGetGroundNormal(Bounds objectBounds, out Vector3 normal)
    {
        int sampleCount = Mathf.Clamp(samplesPerAxis, 1, MaxSamplesPerAxis);
        Vector3 normalSum = Vector3.zero;
        int hitCount = 0;

        for (int xIndex = 0; xIndex < sampleCount; xIndex++)
        {
            for (int zIndex = 0; zIndex < sampleCount; zIndex++)
            {
                Vector3 rayOrigin = GetSampleRayOrigin(objectBounds, sampleCount, xIndex, zIndex);

                if (TryRaycastGround(rayOrigin, out RaycastHit hit))
                {
                    normalSum += hit.normal;
                    hitCount++;
                }
            }
        }

        if (hitCount == 0)
        {
            normal = Vector3.up;
            return false;
        }

        normal = normalSum.normalized;
        if (normal.sqrMagnitude < MinNormalSqrMagnitude)
        {
            normal = Vector3.up;
        }

        return true;
    }

    private bool TryGetGroundDistance(Bounds objectBounds, Vector3 placementNormal, out float groundDistance)
    {
        int sampleCount = Mathf.Clamp(samplesPerAxis, 1, MaxSamplesPerAxis);
        bool found = false;
        groundDistance = float.NegativeInfinity;

        for (int xIndex = 0; xIndex < sampleCount; xIndex++)
        {
            for (int zIndex = 0; zIndex < sampleCount; zIndex++)
            {
                Vector3 rayOrigin = GetSampleRayOrigin(objectBounds, sampleCount, xIndex, zIndex);

                if (!TryRaycastGround(rayOrigin, out RaycastHit hit))
                {
                    continue;
                }

                float hitDistance = Vector3.Dot(hit.point, placementNormal);
                if (!found || hitDistance > groundDistance)
                {
                    groundDistance = hitDistance;
                    found = true;
                }
            }
        }

        return found;
    }

    private Vector3 GetSampleRayOrigin(Bounds objectBounds, int sampleCount, int xIndex, int zIndex)
    {
        float xLerp = sampleCount == 1 ? 0.5f : (float)xIndex / (sampleCount - 1);
        float zLerp = sampleCount == 1 ? 0.5f : (float)zIndex / (sampleCount - 1);
        float x = Mathf.Lerp(objectBounds.min.x, objectBounds.max.x, xLerp);
        float z = Mathf.Lerp(objectBounds.min.z, objectBounds.max.z, zLerp);
        return new Vector3(x, objectBounds.max.y + rayStartHeight, z);
    }

    private bool TryRaycastGround(Vector3 origin, out RaycastHit bestHit)
    {
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, RaycastHits, rayDistance, groundMask, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        bestHit = default;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = RaycastHits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (RaycastHits[i].distance >= bestDistance)
            {
                continue;
            }

            bestDistance = RaycastHits[i].distance;
            bestHit = RaycastHits[i];
            found = true;
        }

        return found;
    }

    private bool TryGetLowestObjectOffset(Vector3 normal, out float lowestOffset)
    {
        bool hasSupportPoint = false;
        lowestOffset = float.MaxValue;

        for (int i = 0; boundsColliders != null && i < boundsColliders.Length; i++)
        {
            Collider targetCollider = boundsColliders[i];
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            if (!includeTriggerColliders && targetCollider.isTrigger)
            {
                continue;
            }

            CheckColliderSupportPoints(targetCollider, normal, ref lowestOffset);
            hasSupportPoint = true;
        }

        if (hasSupportPoint)
        {
            return true;
        }

        for (int i = 0; boundsRenderers != null && i < boundsRenderers.Length; i++)
        {
            Renderer targetRenderer = boundsRenderers[i];
            if (targetRenderer == null || !targetRenderer.enabled)
            {
                continue;
            }

            CheckBoundsCorners(targetRenderer.bounds, normal, ref lowestOffset);
            hasSupportPoint = true;
        }

        return hasSupportPoint;
    }

    private void CheckColliderSupportPoints(Collider targetCollider, Vector3 normal, ref float lowestOffset)
    {
        BoxCollider boxCollider = targetCollider as BoxCollider;
        if (boxCollider != null)
        {
            CheckBoxColliderSupportPoints(boxCollider, normal, ref lowestOffset);
            return;
        }

        SphereCollider sphereCollider = targetCollider as SphereCollider;
        if (sphereCollider != null)
        {
            CheckSphereColliderSupportPoint(sphereCollider, normal, ref lowestOffset);
            return;
        }

        CapsuleCollider capsuleCollider = targetCollider as CapsuleCollider;
        if (capsuleCollider != null)
        {
            CheckCapsuleColliderSupportPoints(capsuleCollider, normal, ref lowestOffset);
            return;
        }

        MeshCollider meshCollider = targetCollider as MeshCollider;
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            CheckMeshColliderSupportPoints(meshCollider, normal, ref lowestOffset);
            return;
        }

        CheckBoundsCorners(targetCollider.bounds, normal, ref lowestOffset);
    }

    private void CheckBoxColliderSupportPoints(BoxCollider boxCollider, Vector3 normal, ref float lowestOffset)
    {
        Vector3 center = boxCollider.center;
        Vector3 extents = boxCollider.size * 0.5f;
        Transform colliderTransform = boxCollider.transform;

        CheckLocalColliderPoint(colliderTransform, center + new Vector3(-extents.x, -extents.y, -extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(-extents.x, -extents.y, extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(-extents.x, extents.y, -extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(-extents.x, extents.y, extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(extents.x, -extents.y, -extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(extents.x, -extents.y, extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(extents.x, extents.y, -extents.z), normal, ref lowestOffset);
        CheckLocalColliderPoint(colliderTransform, center + new Vector3(extents.x, extents.y, extents.z), normal, ref lowestOffset);
    }

    private void CheckSphereColliderSupportPoint(SphereCollider sphereCollider, Vector3 normal, ref float lowestOffset)
    {
        Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
        float radius = sphereCollider.radius * GetLargestAbsScale(sphereCollider.transform.lossyScale);
        CheckWorldPoint(center - normal * radius, normal, ref lowestOffset);
    }

    private void CheckCapsuleColliderSupportPoints(CapsuleCollider capsuleCollider, Vector3 normal, ref float lowestOffset)
    {
        Transform capsuleTransform = capsuleCollider.transform;
        Vector3 lossyScale = capsuleTransform.lossyScale;
        Vector3 localAxis = GetCapsuleLocalAxis(capsuleCollider.direction);
        Vector3 worldAxis = capsuleTransform.TransformDirection(localAxis).normalized;
        float radiusScale = GetCapsuleRadiusScale(capsuleCollider.direction, lossyScale);
        float heightScale = GetCapsuleHeightScale(capsuleCollider.direction, lossyScale);
        float radius = capsuleCollider.radius * radiusScale;
        float height = Mathf.Max(capsuleCollider.height * heightScale, radius * 2f);
        float segmentHalfLength = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 center = capsuleTransform.TransformPoint(capsuleCollider.center);

        CheckWorldPoint(center + worldAxis * segmentHalfLength - normal * radius, normal, ref lowestOffset);
        CheckWorldPoint(center - worldAxis * segmentHalfLength - normal * radius, normal, ref lowestOffset);
    }

    private void CheckMeshColliderSupportPoints(MeshCollider meshCollider, Vector3 normal, ref float lowestOffset)
    {
        Vector3[] vertices = meshCollider.sharedMesh.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            CheckBoundsCorners(meshCollider.bounds, normal, ref lowestOffset);
            return;
        }

        Transform meshTransform = meshCollider.transform;
        for (int i = 0; i < vertices.Length; i++)
        {
            CheckLocalColliderPoint(meshTransform, vertices[i], normal, ref lowestOffset);
        }
    }

    private Vector3 GetCapsuleLocalAxis(int direction)
    {
        if (direction == 0)
        {
            return Vector3.right;
        }

        if (direction == 1)
        {
            return Vector3.up;
        }

        return Vector3.forward;
    }

    private float GetCapsuleRadiusScale(int direction, Vector3 lossyScale)
    {
        float x = Mathf.Abs(lossyScale.x);
        float y = Mathf.Abs(lossyScale.y);
        float z = Mathf.Abs(lossyScale.z);

        if (direction == 0)
        {
            return Mathf.Max(y, z);
        }

        if (direction == 1)
        {
            return Mathf.Max(x, z);
        }

        return Mathf.Max(x, y);
    }

    private float GetCapsuleHeightScale(int direction, Vector3 lossyScale)
    {
        if (direction == 0)
        {
            return Mathf.Abs(lossyScale.x);
        }

        if (direction == 1)
        {
            return Mathf.Abs(lossyScale.y);
        }

        return Mathf.Abs(lossyScale.z);
    }

    private float GetLargestAbsScale(Vector3 scale)
    {
        float x = Mathf.Abs(scale.x);
        float y = Mathf.Abs(scale.y);
        float z = Mathf.Abs(scale.z);
        return Mathf.Max(x, Mathf.Max(y, z));
    }

    private void CheckBoundsCorners(Bounds bounds, Vector3 normal, ref float lowestOffset)
    {
        CheckWorldPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), normal, ref lowestOffset);
        CheckWorldPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z), normal, ref lowestOffset);
    }

    private void CheckLocalColliderPoint(Transform sourceTransform, Vector3 localPoint, Vector3 normal, ref float lowestOffset)
    {
        CheckWorldPoint(sourceTransform.TransformPoint(localPoint), normal, ref lowestOffset);
    }

    private void CheckWorldPoint(Vector3 worldPoint, Vector3 normal, ref float lowestOffset)
    {
        Vector3 offset = worldPoint - transform.position;
        float distance = Vector3.Dot(offset, normal);
        if (distance < lowestOffset)
        {
            lowestOffset = distance;
        }
    }
}
