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
        private int _edgeCount;
        private bool _isBuilt;

        public int NodeCount => _positions.Count;
        public bool IsBuilt => _isBuilt && _positions.Count > 0 && _edgeCount > 0;

        private void Awake()
        {
            RegisterForScene();
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
                if (!cachedGraph.IsBuilt)
                {
                    cachedGraph.Build();
                }

                return cachedGraph;
            }

            return null;
        }

        private void RegisterForScene()
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid())
            {
                return;
            }

            GraphBySceneHandle[scene.handle] = this;
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
            _edgeCount = 0;
            _isBuilt = false;

            if (source == null || source.WaypointPointCount <= 0)
            {
                return;
            }

            int pointCount = source.WaypointPointCount;
            for (int i = 0; i < pointCount; i++)
            {
                if (!source.TryGetWaypointPoint(i, out Vector3 pointPosition))
                {
                    continue;
                }

                _positions.Add(pointPosition);
                _neighbors.Add(new List<WaypointGraphEdge>(6));
            }

            IReadOnlyList<WaypointPointSpawner.WaypointConnection> connections = source.Connections;
            if (connections != null)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    WaypointPointSpawner.WaypointConnection connection = connections[i];
                    if (!connection.IsValid(_positions.Count))
                    {
                        continue;
                    }

                    int fromNodeId = connection.fromIndex;
                    int toNodeId = connection.toIndex;
                    float distance = connection.distance;
                    if (distance <= 0f)
                    {
                        distance = Vector3.Distance(_positions[fromNodeId], _positions[toNodeId]);
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
