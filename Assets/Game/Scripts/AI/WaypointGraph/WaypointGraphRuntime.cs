using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class WaypointGraphRuntime : MonoBehaviour
    {
        private static readonly Dictionary<int, WaypointGraphRuntime> GraphBySceneHandle = new Dictionary<int, WaypointGraphRuntime>(8);

        [SerializeField] private WaypointPointSpawner source;
        [SerializeField] private bool buildOnAwake = true;

        private readonly List<Vector3> _positions = new List<Vector3>(256);
        private readonly List<List<WaypointGraphEdge>> _neighbors = new List<List<WaypointGraphEdge>>(256);
        private readonly Dictionary<Transform, int> _nodeByTransform = new Dictionary<Transform, int>(256);
        private int _edgeCount;
        private bool _isBuilt;

        public int NodeCount => _positions.Count;
        public bool IsBuilt => _isBuilt && _positions.Count > 0 && _edgeCount > 0;

        private void Awake()
        {
            if (buildOnAwake)
            {
                Build();
            }
        }

        public static WaypointGraphRuntime FindOrCreateForScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            int sceneHandle = scene.handle;
            if (GraphBySceneHandle.TryGetValue(sceneHandle, out WaypointGraphRuntime cachedGraph) && cachedGraph != null)
            {
                cachedGraph.Build();
                return cachedGraph;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                WaypointGraphRuntime graph = roots[i].GetComponentInChildren<WaypointGraphRuntime>(true);
                if (graph != null)
                {
                    graph.Build();
                    GraphBySceneHandle[sceneHandle] = graph;
                    return graph;
                }
            }

            for (int i = 0; i < roots.Length; i++)
            {
                WaypointPointSpawner spawner = roots[i].GetComponentInChildren<WaypointPointSpawner>(true);
                if (spawner == null)
                {
                    continue;
                }

                WaypointGraphRuntime graph = spawner.GetComponent<WaypointGraphRuntime>();
                if (graph == null)
                {
                    graph = spawner.gameObject.AddComponent<WaypointGraphRuntime>();
                }

                graph.source = spawner;
                graph.Build();
                GraphBySceneHandle[sceneHandle] = graph;
                return graph;
            }

            return null;
        }

        private void OnDestroy()
        {
            int sceneHandle = gameObject.scene.handle;
            if (GraphBySceneHandle.TryGetValue(sceneHandle, out WaypointGraphRuntime graph) && graph == this)
            {
                GraphBySceneHandle.Remove(sceneHandle);
            }
        }

        public void Build()
        {
            _positions.Clear();
            _neighbors.Clear();
            _nodeByTransform.Clear();
            _edgeCount = 0;
            _isBuilt = false;

            if (source == null)
            {
                source = GetComponent<WaypointPointSpawner>();
            }

            if (source == null || source.pointsParent == null)
            {
                return;
            }

            Transform pointsParent = source.pointsParent;
            for (int i = 0; i < pointsParent.childCount; i++)
            {
                Transform point = pointsParent.GetChild(i);
                if (point == null)
                {
                    continue;
                }

                int nodeId = _positions.Count;
                _positions.Add(point.position);
                _neighbors.Add(new List<WaypointGraphEdge>(6));
                _nodeByTransform[point] = nodeId;
            }

            IReadOnlyList<WaypointPointSpawner.WaypointConnection> connections = source.Connections;
            if (connections != null)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    WaypointPointSpawner.WaypointConnection connection = connections[i];
                    if (connection.from == null || connection.to == null)
                    {
                        continue;
                    }

                    if (!_nodeByTransform.TryGetValue(connection.from, out int fromNodeId))
                    {
                        continue;
                    }

                    if (!_nodeByTransform.TryGetValue(connection.to, out int toNodeId))
                    {
                        continue;
                    }

                    float distance = connection.distance;
                    if (distance <= 0f)
                    {
                        distance = Vector3.Distance(connection.from.position, connection.to.position);
                    }

                    AddBidirectionalEdge(fromNodeId, toNodeId, distance);
                }
            }

            _isBuilt = _positions.Count > 0;
        }

        public int FindNearestNode(Vector3 position)
        {
            if (!IsBuilt)
            {
                return -1;
            }

            int nearestNodeId = -1;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < _positions.Count; i++)
            {
                Vector3 delta = _positions[i] - position;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestNodeId = i;
                }
            }

            return nearestNodeId;
        }

        public int GetRandomNodeId()
        {
            if (!IsBuilt)
            {
                return -1;
            }

            return Random.Range(0, _positions.Count);
        }

        public IReadOnlyList<WaypointGraphEdge> GetNeighbors(int nodeId)
        {
            if (nodeId < 0 || nodeId >= _neighbors.Count)
            {
                return null;
            }

            return _neighbors[nodeId];
        }

        public Vector3 GetNodePosition(int nodeId)
        {
            if (nodeId < 0 || nodeId >= _positions.Count)
            {
                return transform.position;
            }

            return _positions[nodeId];
        }

        private void AddBidirectionalEdge(int fromNodeId, int toNodeId, float distance)
        {
            if (fromNodeId == toNodeId)
            {
                return;
            }

            AddEdge(fromNodeId, toNodeId, distance);
            AddEdge(toNodeId, fromNodeId, distance);
        }

        private void AddEdge(int fromNodeId, int toNodeId, float distance)
        {
            List<WaypointGraphEdge> edges = _neighbors[fromNodeId];
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].To == toNodeId)
                {
                    return;
                }
            }

            edges.Add(new WaypointGraphEdge(toNodeId, Mathf.Max(0.01f, distance)));
            _edgeCount++;
        }
    }
}
