using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaypointPointSpawner : MonoBehaviour
{
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

    [Header("Raycast")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;

    [Header("Cube Settings")]
    public Vector3 cubeSize = new Vector3(0.5f, 0.5f, 0.5f);
    public Transform pointsParent;
    public bool clearOldPointsBeforeGenerate = true;

    [Header("Generated Point Visual")]
    public Color generatedCubeColor = new Color(1f, 0f, 0f, 1f);

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

    [SerializeField]
    private List<WaypointConnection> connections = new List<WaypointConnection>();

    public IReadOnlyList<WaypointConnection> Connections => connections;

    [Serializable]
    public struct WaypointConnection
    {
        public Transform from;
        public Transform to;
        public float distance;

        public WaypointConnection(Transform from, Transform to, float distance)
        {
            this.from = from;
            this.to = to;
            this.distance = distance;
        }
    }

    [Button("Generate Waypoint Cubes")]
    public void GenerateWaypointCubes()
    {
#if UNITY_EDITOR
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

        EnsureParent();

        if (clearOldPointsBeforeGenerate)
        {
            ClearOldPoints();
            ClearConnections();
        }

        List<Vector3> spawnedPositions = new List<Vector3>();
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
                continue;

            Vector3 rayOrigin = new Vector3(randomX, spawnHeight, randomZ);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnHeight * 2f))
                continue;

            bool hitGround = IsInLayerMask(hit.collider.gameObject.layer, groundMask);
            bool hitObstacle = IsInLayerMask(hit.collider.gameObject.layer, obstacleMask);

            if (!hitGround)
                continue;

            if (hitObstacle)
                continue;

            Vector3 spawnPosition = hit.point;

            if (IsTooCloseToExistingPoint(spawnPosition, spawnedPositions))
                continue;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Waypoint_Point_{spawned:000}";
            cube.transform.position = spawnPosition + Vector3.up * (cubeSize.y * 0.5f);
            cube.transform.localScale = cubeSize;
            cube.transform.SetParent(pointsParent);

            RemoveCollider(cube);
            ApplyGeneratedCubeColor(cube);

            Undo.RegisterCreatedObjectUndo(cube, "Generate Waypoint Cube");

            spawnedPositions.Add(spawnPosition);
            spawned++;
        }

        Debug.Log($"[WaypointPointSpawner] Generated {spawned}/{pointsToSpawn} waypoint cubes. Attempts: {attempts}/{maxAttempts}");

        EditorUtility.SetDirty(this);
#else
        Debug.LogWarning("[WaypointPointSpawner] This generator is intended for Editor use only.");
#endif
    }

    [Button("Build Waypoint Connections")]
    public void BuildWaypointConnections()
    {
#if UNITY_EDITOR
        if (pointsParent == null)
        {
            Debug.LogWarning("[WaypointPointSpawner] pointsParent is null. Generate points first.");
            return;
        }

        List<Transform> points = GetGeneratedPoints();

        if (points.Count < 2)
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

        HashSet<string> usedPairs = new HashSet<string>();

        int createdConnections = 0;
        int blockedConnections = 0;

        for (int i = 0; i < points.Count; i++)
        {
            Transform current = points[i];

            List<TransformDistance> candidates = new List<TransformDistance>();

            for (int j = 0; j < points.Count; j++)
            {
                if (i == j)
                    continue;

                Transform other = points[j];

                float distance = Vector3.Distance(current.position, other.position);

                if (distance > connectionRadius)
                    continue;

                candidates.Add(new TransformDistance(other, distance));
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int addedForThisPoint = 0;

            for (int c = 0; c < candidates.Count; c++)
            {
                if (addedForThisPoint >= maxConnectionsPerPoint)
                    break;

                Transform other = candidates[c].transform;

                string pairKey = GetPairKey(current, other);

                if (usedPairs.Contains(pairKey))
                    continue;

                if (!IsConnectionClear(current.position, other.position))
                {
                    blockedConnections++;
                    continue;
                }

                float distance = Vector3.Distance(current.position, other.position);

                connections.Add(new WaypointConnection(current, other, distance));
                usedPairs.Add(pairKey);

                addedForThisPoint++;
                createdConnections++;
            }
        }

        Debug.Log($"[WaypointPointSpawner] Built {createdConnections} waypoint connections. Blocked/skipped by obstacles: {blockedConnections}");

        EditorUtility.SetDirty(this);
#else
        Debug.LogWarning("[WaypointPointSpawner] This generator is intended for Editor use only.");
#endif
    }

    [Button("Clear Generated Cubes")]
    public void ClearGeneratedCubes()
    {
#if UNITY_EDITOR
        EnsureParent();
        ClearOldPoints();
        ClearConnections();
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

    private List<Transform> GetGeneratedPoints()
    {
        List<Transform> points = new List<Transform>();

        if (pointsParent == null)
            return points;

        for (int i = 0; i < pointsParent.childCount; i++)
        {
            Transform child = pointsParent.GetChild(i);

            if (child != null)
                points.Add(child);
        }

        return points;
    }

    private bool IsConnectionClear(Vector3 from, Vector3 to)
    {
        Vector3 start = from + Vector3.up * connectionCheckHeight;
        Vector3 end = to + Vector3.up * connectionCheckHeight;

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return false;

        direction.Normalize();

        bool hitObstacle = Physics.SphereCast(
            start,
            connectionCheckRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleMask
        );

        return !hitObstacle;
    }

    private string GetPairKey(Transform a, Transform b)
    {
        int aId = a.GetInstanceID();
        int bId = b.GetInstanceID();

        if (aId < bId)
            return $"{aId}_{bId}";

        return $"{bId}_{aId}";
    }

    private void ClearConnections()
    {
        connections.Clear();
    }

    private List<Transform> GetOrderedContourPoints()
    {
        List<Transform> source = new List<Transform>();

        if (contourPoints == null)
            return source;

        foreach (Transform point in contourPoints)
        {
            if (point != null)
                source.Add(point);
        }

        if (!sortContourByNearest || source.Count <= 2)
            return source;

        List<Transform> ordered = new List<Transform>();
        HashSet<Transform> used = new HashSet<Transform>();

        Transform current = source[0];

        ordered.Add(current);
        used.Add(current);

        while (ordered.Count < source.Count)
        {
            Transform nearest = null;
            float nearestDistanceSqr = float.MaxValue;

            foreach (Transform candidate in source)
            {
                if (used.Contains(candidate))
                    continue;

                float distanceSqr = (candidate.position - current.position).sqrMagnitude;

                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearest = candidate;
                }
            }

            if (nearest == null)
                break;

            ordered.Add(nearest);
            used.Add(nearest);
            current = nearest;
        }

        return ordered;
    }

#if UNITY_EDITOR
    private void EnsureParent()
    {
        if (pointsParent != null)
            return;

        GameObject parentObject = new GameObject("Generated Waypoint Points");
        parentObject.transform.SetParent(transform);
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(parentObject, "Create Waypoint Points Parent");

        pointsParent = parentObject.transform;
        EditorUtility.SetDirty(this);
    }

    private void ClearOldPoints()
    {
        if (pointsParent == null)
            return;

        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < pointsParent.childCount; i++)
        {
            children.Add(pointsParent.GetChild(i).gameObject);
        }

        foreach (GameObject child in children)
        {
            Undo.DestroyObjectImmediate(child);
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
                ((zi > point.z) != (zj > point.z)) &&
                (point.x < (xj - xi) * (point.z - zi) / ((zj - zi) + Mathf.Epsilon) + xi);

            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private bool IsTooCloseToExistingPoint(Vector3 position, List<Vector3> existingPositions)
    {
        float minDistanceSqr = minDistanceBetweenPoints * minDistanceBetweenPoints;

        for (int i = 0; i < existingPositions.Count; i++)
        {
            if ((position - existingPositions[i]).sqrMagnitude < minDistanceSqr)
                return true;
        }

        return false;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void RemoveCollider(GameObject cube)
    {
        Collider collider = cube.GetComponent<Collider>();

        if (collider == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(collider);
        else
            Destroy(collider);
#else
        Destroy(collider);
#endif
    }

    private void ApplyGeneratedCubeColor(GameObject cube)
    {
        Renderer renderer = cube.GetComponent<Renderer>();

        if (renderer == null)
            return;

        Material material = new Material(Shader.Find("Standard"));
        material.color = generatedCubeColor;

        renderer.sharedMaterial = material;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        DrawContourGizmos();
        DrawGeneratedPointGizmos();
        DrawConnectionGizmos();
    }

    private void DrawContourGizmos()
    {
        List<Transform> orderedContour = GetOrderedContourPoints();

        if (orderedContour == null || orderedContour.Count < 2)
            return;

        Gizmos.color = contourColor;

        for (int i = 0; i < orderedContour.Count; i++)
        {
            Transform current = orderedContour[i];
            Transform next = orderedContour[(i + 1) % orderedContour.Count];

            if (current == null || next == null)
                continue;

            Gizmos.DrawLine(current.position, next.position);
            Gizmos.DrawSphere(current.position, 0.35f);
        }
    }

    private void DrawGeneratedPointGizmos()
    {
        if (pointsParent == null)
            return;

        Gizmos.color = pointColor;

        for (int i = 0; i < pointsParent.childCount; i++)
        {
            Transform child = pointsParent.GetChild(i);

            if (child == null)
                continue;

            Gizmos.DrawSphere(child.position, 0.2f);
        }
    }

    private void DrawConnectionGizmos()
    {
        if (connections == null || connections.Count == 0)
            return;

        Gizmos.color = connectionColor;

        for (int i = 0; i < connections.Count; i++)
        {
            WaypointConnection connection = connections[i];

            if (connection.from == null || connection.to == null)
                continue;

            Gizmos.DrawLine(connection.from.position, connection.to.position);
        }
    }

    private struct TransformDistance
    {
        public Transform transform;
        public float distance;

        public TransformDistance(Transform transform, float distance)
        {
            this.transform = transform;
            this.distance = distance;
        }
    }
}
