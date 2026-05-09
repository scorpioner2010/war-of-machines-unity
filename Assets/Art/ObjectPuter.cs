using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ObjectPuter : MonoBehaviour
{
    [SerializeField] private LayerMask terrainMask = ~0;
    [SerializeField] private float rayStartHeight = 100f;
    [SerializeField] private float rayDistance = 250f;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private int samplesPerAxis = 3;

    [Button("Put")]
    private void Put()
    {
        if (!TryGetObjectBounds(out Bounds objectBounds))
        {
            Debug.LogWarning("ObjectPuter: object has no collider or renderer bounds.", this);
            return;
        }

        if (!TryGetTerrainPlane(objectBounds, out Vector3 groundNormal, out float groundDistance))
        {
            Debug.LogWarning("ObjectPuter: terrain was not found under this object.", this);
            return;
        }

#if UNITY_EDITOR
        Undo.RecordObject(transform, "Put object on terrain");
#endif

        transform.rotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;

        if (!TryGetLowestColliderOffset(groundNormal, out float lowestColliderOffset))
        {
            Debug.LogWarning("ObjectPuter: object has no colliders to place on terrain.", this);
            return;
        }

        float currentLowestDistance = Vector3.Dot(transform.position, groundNormal) + lowestColliderOffset;
        float targetLowestDistance = groundDistance + groundOffset;
        transform.position += groundNormal * (targetLowestDistance - currentLowestDistance);

#if UNITY_EDITOR
        EditorUtility.SetDirty(transform);
        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private bool TryGetObjectBounds(out Bounds bounds)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds;
    }

    private bool TryGetTerrainPlane(Bounds objectBounds, out Vector3 normal, out float planeDistance)
    {
        int sampleCount = Mathf.Max(2, samplesPerAxis);
        Vector3 normalSum = Vector3.zero;
        Vector3 pointSum = Vector3.zero;
        int hitCount = 0;

        for (int xIndex = 0; xIndex < sampleCount; xIndex++)
        {
            float xLerp = sampleCount == 1 ? 0.5f : (float)xIndex / (sampleCount - 1);
            float x = Mathf.Lerp(objectBounds.min.x, objectBounds.max.x, xLerp);

            for (int zIndex = 0; zIndex < sampleCount; zIndex++)
            {
                float zLerp = sampleCount == 1 ? 0.5f : (float)zIndex / (sampleCount - 1);
                float z = Mathf.Lerp(objectBounds.min.z, objectBounds.max.z, zLerp);
                Vector3 rayOrigin = new Vector3(x, objectBounds.max.y + rayStartHeight, z);

                if (TryRaycastTerrain(rayOrigin, out RaycastHit hit))
                {
                    normalSum += hit.normal;
                    pointSum += hit.point;
                    hitCount++;
                }
            }
        }

        if (hitCount == 0)
        {
            normal = Vector3.up;
            planeDistance = 0f;
            return false;
        }

        normal = normalSum.normalized;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector3.up;
        }

        Vector3 averagePoint = pointSum / hitCount;
        planeDistance = Vector3.Dot(averagePoint, normal);
        return true;
    }

    private bool TryRaycastTerrain(Vector3 origin, out RaycastHit bestHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayDistance, terrainMask, QueryTriggerInteraction.Ignore);
        bool found = false;
        float bestDistance = float.MaxValue;
        bestHit = default;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!(hits[i].collider is TerrainCollider))
            {
                continue;
            }

            if (hits[i].distance >= bestDistance)
            {
                continue;
            }

            bestDistance = hits[i].distance;
            bestHit = hits[i];
            found = true;
        }

        return found;
    }

    private bool TryGetLowestColliderOffset(Vector3 normal, out float lowestOffset)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasCollider = false;
        lowestOffset = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
            {
                continue;
            }

            Bounds bounds = colliders[i].bounds;
            CheckBoundsCorner(bounds.min.x, bounds.min.y, bounds.min.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.min.x, bounds.min.y, bounds.max.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.min.x, bounds.max.y, bounds.min.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.min.x, bounds.max.y, bounds.max.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.max.x, bounds.min.y, bounds.min.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.max.x, bounds.min.y, bounds.max.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.max.x, bounds.max.y, bounds.min.z, normal, ref lowestOffset);
            CheckBoundsCorner(bounds.max.x, bounds.max.y, bounds.max.z, normal, ref lowestOffset);
            hasCollider = true;
        }

        return hasCollider;
    }

    private void CheckBoundsCorner(float x, float y, float z, Vector3 normal, ref float lowestOffset)
    {
        Vector3 offset = new Vector3(x, y, z) - transform.position;
        float distance = Vector3.Dot(offset, normal);
        if (distance < lowestOffset)
        {
            lowestOffset = distance;
        }
    }
}
