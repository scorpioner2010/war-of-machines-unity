using System;
using System.Collections.Generic;
using Game.Scripts.Diagnostics;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaypointPointSpawner : MonoBehaviour
{
    private const int MaxObstacleOverlapResults = 128;
    private const float LegacyImportDuplicateDistance = 0.05f;

    private static readonly Collider[] ObstacleOverlapResults = new Collider[MaxObstacleOverlapResults];

    [Header("Contour")]
    [Tooltip("Contour points. If sortContourByNearest is enabled, order in this array does not matter much.")]
    public Transform[] contourPoints;

    [Tooltip("If enabled, contour points will be ordered by nearest-neighbor distance before drawing/generation.")]
    public bool sortContourByNearest = true;

    [Header("Generation")]
    public int pointsToSpawn = 100;
    public float spawnHeight = 100f;
    public int maxAttempts = 5000;
    public float minDistanceBetweenPoints = 3f;

    [Tooltip("Minimum horizontal distance from a generated waypoint to any collider in obstacleMask.")]
    [Min(0f)] public float minDistanceFromObstacles = 2f;

    [Header("Raycast")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;

    [Header("Logical Points")]
    public bool clearOldPointsBeforeGenerate = true;
    [SerializeField, HideInInspector] private Transform pointsParent;

    [Header("Connection Settings")]
    public float connectionRadius = 8f;
    public int maxConnectionsPerPoint = 4;

    [Tooltip("SphereCast radius used to check if robot can pass between two points.")]
    public float connectionCheckRadius = 0.5f;

    [Tooltip("Height offset for connection ray/sphere cast. Usually robot center height or a little above ground.")]
    public float connectionCheckHeight = 0.5f;

    [Tooltip("If true, old connections will be cleared before building new ones.")]
    public bool clearOldConnectionsBeforeBuild = true;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color contourColor = Color.yellow;
    public Color pointColor = Color.cyan;
    public Color connectionColor = Color.green;
    public Color blockedConnectionColor = Color.red;

    [SerializeField] private List<Vector3> waypointPoints = new List<Vector3>();
    [SerializeField] private List<WaypointConnection> connections = new List<WaypointConnection>();

    public IReadOnlyList<Vector3> WaypointPoints => waypointPoints;
    public IReadOnlyList<WaypointConnection> Connections => connections;
    public int WaypointPointCount => waypointPoints != null ? waypointPoints.Count : 0;

    [Serializable]
    public struct WaypointConnection
    {
        public int fromIndex;
        public int toIndex;
        public float distance;

        public WaypointConnection(int fromIndex, int toIndex, float distance)
        {
            this.fromIndex = fromIndex;
            this.toIndex = toIndex;
            this.distance = distance;
        }

        public bool IsValid(int pointCount)
        {
            return fromIndex >= 0
                   && toIndex >= 0
                   && fromIndex < pointCount
                   && toIndex < pointCount
                   && fromIndex != toIndex;
        }
    }

    private void OnValidate()
    {
        EnsureLists();

        pointsToSpawn = Mathf.Max(0, pointsToSpawn);
        spawnHeight = Mathf.Max(0.01f, spawnHeight);
        maxAttempts = Mathf.Max(0, maxAttempts);
        minDistanceBetweenPoints = Mathf.Max(0f, minDistanceBetweenPoints);
        minDistanceFromObstacles = Mathf.Max(0f, minDistanceFromObstacles);
        connectionRadius = Mathf.Max(0f, connectionRadius);
        maxConnectionsPerPoint = Mathf.Max(0, maxConnectionsPerPoint);
        connectionCheckRadius = Mathf.Max(0.01f, connectionCheckRadius);
        connectionCheckHeight = Mathf.Max(0f, connectionCheckHeight);
        RemoveInvalidConnections();
    }

    [Button("Generate Waypoint Points")]
    public void GenerateWaypointPoints()
    {
#if UNITY_EDITOR
        EnsureLists();

        List<Transform> orderedContour = GetOrderedContourPoints();
        if (orderedContour.Count < 3)
        {
            Debug.LogWarning("[WaypointPointSpawner] Need at least 3 contour points.");
            return;
        }

        if (pointsToSpawn <= 0)
        {
            Debug.LogWarning("[WaypointPointSpawner] pointsToSpawn must be greater than 0.");
            return;
        }

        if (maxAttempts <= 0)
        {
            Debug.LogWarning("[WaypointPointSpawner] maxAttempts must be greater than 0.");
            return;
        }

        if (clearOldPointsBeforeGenerate)
        {
            ClearWaypointPoints();
            ClearLegacyPointObjects();
            ClearConnections();
        }

        List<Vector3> spawnedPositions = new List<Vector3>(waypointPoints.Count + pointsToSpawn);
        for (int i = 0; i < waypointPoints.Count; i++)
        {
            spawnedPositions.Add(waypointPoints[i]);
        }

        Bounds bounds = CalculateContourBounds(orderedContour);

        int spawned = 0;
        int attempts = 0;

        while (spawned < pointsToSpawn && attempts < maxAttempts)
        {
            attempts++;

            float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
            Vector3 xzPoint = new Vector3(randomX, 0f, randomZ);

            if (!IsPointInsidePolygonXZ(xzPoint, orderedContour))
            {
                continue;
            }

            if (!TryFindGroundPoint(randomX, randomZ, out Vector3 spawnPosition))
            {
                continue;
            }

            if (IsTooCloseToObstacle(spawnPosition))
            {
                continue;
            }

            if (IsTooCloseToExistingPoint(spawnPosition, spawnedPositions))
            {
                continue;
            }

            waypointPoints.Add(spawnPosition);
            spawnedPositions.Add(spawnPosition);
            spawned++;
        }

        RemoveInvalidConnections();
        Debug.Log("[WaypointPointSpawner] Generated "
                  + spawned
                  + "/"
                  + pointsToSpawn
                  + " logical waypoint points. Total: "
                  + waypointPoints.Count
                  + ". Attempts: "
                  + attempts
                  + "/"
                  + maxAttempts);

        EditorUtility.SetDirty(this);
#else
        Debug.LogWarning("[WaypointPointSpawner] This generator is intended for Editor use only.");
#endif
    }

    [Button("Build Waypoint Connections")]
    public void BuildWaypointConnections()
    {
#if UNITY_EDITOR
        EnsureLists();
        ImportLegacyPointObjects(false);

        if (waypointPoints.Count < 2)
        {
            Debug.LogWarning("[WaypointPointSpawner] Need at least 2 generated points to build connections.");
            return;
        }

        if (connectionRadius <= 0f)
        {
            Debug.LogWarning("[WaypointPointSpawner] connectionRadius must be greater than 0.");
            return;
        }

        if (maxConnectionsPerPoint <= 0)
        {
            Debug.LogWarning("[WaypointPointSpawner] maxConnectionsPerPoint must be greater than 0.");
            return;
        }

        if (clearOldConnectionsBeforeBuild)
        {
            ClearConnections();
        }
        else
        {
            RemoveInvalidConnections();
        }

        HashSet<ulong> usedPairs = new HashSet<ulong>();
        for (int i = 0; i < connections.Count; i++)
        {
            WaypointConnection existingConnection = connections[i];
            if (existingConnection.IsValid(waypointPoints.Count))
            {
                usedPairs.Add(GetPairKey(existingConnection.fromIndex, existingConnection.toIndex));
            }
        }

        int createdConnections = 0;
        int blockedConnections = 0;

        for (int i = 0; i < waypointPoints.Count; i++)
        {
            Vector3 current = waypointPoints[i];
            List<PointDistance> candidates = new List<PointDistance>();

            for (int j = 0; j < waypointPoints.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Vector3 other = waypointPoints[j];
                float distance = Vector3.Distance(current, other);
                if (distance > connectionRadius)
                {
                    continue;
                }

                candidates.Add(new PointDistance(j, distance));
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int addedForThisPoint = 0;
            for (int c = 0; c < candidates.Count; c++)
            {
                if (addedForThisPoint >= maxConnectionsPerPoint)
                {
                    break;
                }

                int otherIndex = candidates[c].pointIndex;
                ulong pairKey = GetPairKey(i, otherIndex);
                if (usedPairs.Contains(pairKey))
                {
                    continue;
                }

                Vector3 other = waypointPoints[otherIndex];
                if (!IsConnectionClear(current, other))
                {
                    blockedConnections++;
                    continue;
                }

                connections.Add(new WaypointConnection(i, otherIndex, candidates[c].distance));
                usedPairs.Add(pairKey);
                addedForThisPoint++;
                createdConnections++;
            }
        }

        Debug.Log("[WaypointPointSpawner] Built "
                  + createdConnections
                  + " waypoint connections. Blocked/skipped by obstacles: "
                  + blockedConnections);

        EditorUtility.SetDirty(this);
#else
        Debug.LogWarning("[WaypointPointSpawner] This generator is intended for Editor use only.");
#endif
    }

    [Button("Clear Generated Points")]
    public void ClearGeneratedPoints()
    {
#if UNITY_EDITOR
        ClearWaypointPoints();
        ClearLegacyPointObjects();
        ClearConnections();
        EditorUtility.SetDirty(this);
#endif
    }

    [Button("Clear Connections")]
    public void ClearConnectionsButton()
    {
#if UNITY_EDITOR
        ClearConnections();
        EditorUtility.SetDirty(this);
#endif
    }

    public Vector3 GetWaypointPoint(int index)
    {
        if (waypointPoints == null || index < 0 || index >= waypointPoints.Count)
        {
            return transform.position;
        }

        return waypointPoints[index];
    }

    public bool TryGetWaypointPoint(int index, out Vector3 position)
    {
        if (waypointPoints == null || index < 0 || index >= waypointPoints.Count)
        {
            position = transform.position;
            return false;
        }

        position = waypointPoints[index];
        return true;
    }

    private void EnsureLists()
    {
        if (waypointPoints == null)
        {
            waypointPoints = new List<Vector3>();
        }

        if (connections == null)
        {
            connections = new List<WaypointConnection>();
        }
    }

    private bool TryFindGroundPoint(float x, float z, out Vector3 position)
    {
        Vector3 rayOrigin = new Vector3(x, spawnHeight, z);
        int raycastMask = groundMask.value | obstacleMask.value;
        if (raycastMask == 0)
        {
            raycastMask = ~0;
        }

        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                spawnHeight * 2f,
                raycastMask,
                QueryTriggerInteraction.Ignore))
        {
            position = Vector3.zero;
            return false;
        }

        bool hitGround = IsInLayerMask(hit.collider.gameObject.layer, groundMask);
        bool hitObstacle = IsInLayerMask(hit.collider.gameObject.layer, obstacleMask);

        if (!hitGround || hitObstacle)
        {
            position = Vector3.zero;
            return false;
        }

        position = hit.point;
        return true;
    }

    private bool IsConnectionClear(Vector3 from, Vector3 to)
    {
        Vector3 start = from + Vector3.up * connectionCheckHeight;
        Vector3 end = to + Vector3.up * connectionCheckHeight;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return false;
        }

        direction.Normalize();

        bool hitObstacle = Physics.SphereCast(
            start,
            connectionCheckRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        return !hitObstacle;
    }

    private bool IsTooCloseToObstacle(Vector3 position)
    {
        float clearance = Mathf.Max(0f, minDistanceFromObstacles);
        if (clearance <= 0f || obstacleMask.value == 0)
        {
            return false;
        }

        Collider[] overlaps = ObstacleOverlapResults;
        int overlapCount = Physics.OverlapSphereNonAlloc(
            position,
            clearance,
            overlaps,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        if (overlapCount >= ObstacleOverlapResults.Length)
        {
            overlaps = Physics.OverlapSphere(
                position,
                clearance,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );
            overlapCount = overlaps.Length;
        }

        float clearanceSqr = clearance * clearance;
        for (int i = 0; i < overlapCount; i++)
        {
            Collider obstacle = overlaps[i];
            if (obstacle == null)
            {
                continue;
            }

            Vector3 closestPoint = obstacle.ClosestPoint(position);
            Vector2 horizontalOffset = new Vector2(
                closestPoint.x - position.x,
                closestPoint.z - position.z
            );

            if (horizontalOffset.sqrMagnitude <= clearanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private ulong GetPairKey(int a, int b)
    {
        int min = a < b ? a : b;
        int max = a < b ? b : a;
        return ((ulong)(uint)min << 32) | (uint)max;
    }

    private void ClearWaypointPoints()
    {
        EnsureLists();
        waypointPoints.Clear();
    }

    private void ClearConnections()
    {
        EnsureLists();
        connections.Clear();
    }

    private void RemoveInvalidConnections()
    {
        EnsureLists();

        for (int i = connections.Count - 1; i >= 0; i--)
        {
            if (!connections[i].IsValid(waypointPoints.Count))
            {
                connections.RemoveAt(i);
            }
        }
    }

    private List<Transform> GetOrderedContourPoints()
    {
        List<Transform> source = new List<Transform>();

        if (contourPoints == null)
        {
            return source;
        }

        for (int i = 0; i < contourPoints.Length; i++)
        {
            Transform point = contourPoints[i];
            if (point != null)
            {
                source.Add(point);
            }
        }

        if (!sortContourByNearest || source.Count <= 2)
        {
            return source;
        }

        List<Transform> ordered = new List<Transform>();
        HashSet<Transform> used = new HashSet<Transform>();
        Transform current = source[0];

        ordered.Add(current);
        used.Add(current);

        while (ordered.Count < source.Count)
        {
            Transform nearest = null;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < source.Count; i++)
            {
                Transform candidate = source[i];
                if (used.Contains(candidate))
                {
                    continue;
                }

                float distanceSqr = (candidate.position - current.position).sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = candidate;
                }
            }

            if (nearest == null)
            {
                break;
            }

            ordered.Add(nearest);
            used.Add(nearest);
            current = nearest;
        }

        return ordered;
    }

#if UNITY_EDITOR
    private int ImportLegacyPointObjects(bool clearLegacyObjects)
    {
        if (pointsParent == null)
        {
            return 0;
        }

        EnsureLists();

        int imported = 0;
        for (int i = 0; i < pointsParent.childCount; i++)
        {
            Transform child = pointsParent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            Vector3 pointPosition = GetLegacyPointPosition(child);
            if (ContainsPointWithinDistance(pointPosition, waypointPoints, LegacyImportDuplicateDistance))
            {
                continue;
            }

            waypointPoints.Add(pointPosition);
            imported++;
        }

        if (clearLegacyObjects)
        {
            ClearLegacyPointObjects();
        }

        return imported;
    }

    private Vector3 GetLegacyPointPosition(Transform pointTransform)
    {
        return pointTransform.position;
    }

    private void ClearLegacyPointObjects()
    {
        if (pointsParent == null)
        {
            return;
        }

        Transform legacyParent = pointsParent;
        List<GameObject> children = new List<GameObject>(legacyParent.childCount);
        for (int i = 0; i < legacyParent.childCount; i++)
        {
            Transform child = legacyParent.GetChild(i);
            if (child != null)
            {
                children.Add(child.gameObject);
            }
        }

        for (int i = 0; i < children.Count; i++)
        {
            Undo.DestroyObjectImmediate(children[i]);
        }

        if (legacyParent.parent == transform)
        {
            GameObject parentObject = legacyParent.gameObject;
            pointsParent = null;
            Undo.DestroyObjectImmediate(parentObject);
        }
    }
#endif

    private Bounds CalculateContourBounds(List<Transform> orderedContour)
    {
        Bounds bounds = new Bounds(orderedContour[0].position, Vector3.zero);

        for (int i = 1; i < orderedContour.Count; i++)
        {
            bounds.Encapsulate(orderedContour[i].position);
        }

        return bounds;
    }

    private bool IsPointInsidePolygonXZ(Vector3 point, List<Transform> orderedContour)
    {
        bool inside = false;
        int count = orderedContour.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector3 pi = orderedContour[i].position;
            Vector3 pj = orderedContour[j].position;

            float xi = pi.x;
            float zi = pi.z;
            float xj = pj.x;
            float zj = pj.z;

            bool intersects =
                ((zi > point.z) != (zj > point.z))
                && (point.x < (xj - xi) * (point.z - zi) / ((zj - zi) + Mathf.Epsilon) + xi);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private bool IsTooCloseToExistingPoint(Vector3 position, List<Vector3> existingPositions)
    {
        return ContainsPointWithinDistance(position, existingPositions, minDistanceBetweenPoints);
    }

    private bool ContainsPointWithinDistance(Vector3 position, List<Vector3> existingPositions, float distance)
    {
        float minDistanceSqr = distance * distance;
        for (int i = 0; i < existingPositions.Count; i++)
        {
            Vector3 delta = position - existingPositions[i];
            delta.y = 0f;
            if (delta.sqrMagnitude < minDistanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        using (ProfileScope.Measure("Gizmos.WaypointPointSpawner.OnDrawGizmos", DiagnosticsCategories.Editor))
        {
            if (!drawGizmos)
            {
                return;
            }

            DrawContourGizmos();
            DrawGeneratedPointGizmos();
            DrawConnectionGizmos();
        }
    }

    private void DrawContourGizmos()
    {
        List<Transform> orderedContour = GetOrderedContourPoints();

        if (orderedContour == null || orderedContour.Count < 2)
        {
            return;
        }

        Gizmos.color = contourColor;

        for (int i = 0; i < orderedContour.Count; i++)
        {
            Transform current = orderedContour[i];
            Transform next = orderedContour[(i + 1) % orderedContour.Count];

            if (current == null || next == null)
            {
                continue;
            }

            Gizmos.DrawLine(current.position, next.position);
            Gizmos.DrawSphere(current.position, 0.35f);
        }
    }

    private void DrawGeneratedPointGizmos()
    {
        if (waypointPoints == null || waypointPoints.Count == 0)
        {
            return;
        }

        Gizmos.color = pointColor;
        for (int i = 0; i < waypointPoints.Count; i++)
        {
            Gizmos.DrawSphere(waypointPoints[i], 0.2f);
        }
    }

    private void DrawConnectionGizmos()
    {
        if (connections == null || connections.Count == 0 || waypointPoints == null)
        {
            return;
        }

        Gizmos.color = connectionColor;

        for (int i = 0; i < connections.Count; i++)
        {
            WaypointConnection connection = connections[i];
            if (!connection.IsValid(waypointPoints.Count))
            {
                continue;
            }

            Gizmos.DrawLine(waypointPoints[connection.fromIndex], waypointPoints[connection.toIndex]);
        }
    }

    private struct PointDistance
    {
        public int pointIndex;
        public float distance;

        public PointDistance(int pointIndex, float distance)
        {
            this.pointIndex = pointIndex;
            this.distance = distance;
        }
    }
}
