using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class WaypointAStarPathfinder
    {
        private readonly WaypointGraphRuntime _graph;
        private readonly List<int> _open = new List<int>(128);
        private int[] _cameFrom = new int[0];
        private float[] _gScore = new float[0];
        private float[] _fScore = new float[0];
        private bool[] _closed = new bool[0];

        public WaypointAStarPathfinder(WaypointGraphRuntime graph)
        {
            _graph = graph;
        }

        public List<int> FindPath(int startNodeId, int goalNodeId)
        {
            List<int> path = new List<int>(32);
            FindPath(startNodeId, goalNodeId, path);
            return path;
        }

        public bool FindPath(int startNodeId, int goalNodeId, List<int> result)
        {
            if (result == null)
            {
                return false;
            }

            result.Clear();
            if (_graph == null || !_graph.IsBuilt)
            {
                return false;
            }

            if (startNodeId < 0 || goalNodeId < 0)
            {
                return false;
            }

            if (startNodeId >= _graph.NodeCount || goalNodeId >= _graph.NodeCount)
            {
                return false;
            }

            if (startNodeId == goalNodeId)
            {
                result.Add(startNodeId);
                return true;
            }

            EnsureCapacity(_graph.NodeCount);
            ResetState(_graph.NodeCount);

            _open.Clear();
            _open.Add(startNodeId);
            _gScore[startNodeId] = 0f;
            _fScore[startNodeId] = Heuristic(startNodeId, goalNodeId);

            while (_open.Count > 0)
            {
                int current = PopLowestFScoreNode();
                if (current == goalNodeId)
                {
                    ReconstructPath(current, result);
                    return result.Count > 0;
                }

                _closed[current] = true;
                IReadOnlyList<WaypointGraphEdge> neighbors = _graph.GetNeighbors(current);
                if (neighbors == null)
                {
                    continue;
                }

                for (int i = 0; i < neighbors.Count; i++)
                {
                    WaypointGraphEdge edge = neighbors[i];
                    int neighbor = edge.To;
                    if (neighbor < 0 || neighbor >= _graph.NodeCount || _closed[neighbor])
                    {
                        continue;
                    }

                    float tentativeG = _gScore[current] + edge.Cost;
                    if (tentativeG >= _gScore[neighbor])
                    {
                        continue;
                    }

                    _cameFrom[neighbor] = current;
                    _gScore[neighbor] = tentativeG;
                    _fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNodeId);

                    if (!ContainsOpenNode(neighbor))
                    {
                        _open.Add(neighbor);
                    }
                }
            }

            return false;
        }

        private void EnsureCapacity(int nodeCount)
        {
            if (_cameFrom.Length >= nodeCount)
            {
                return;
            }

            _cameFrom = new int[nodeCount];
            _gScore = new float[nodeCount];
            _fScore = new float[nodeCount];
            _closed = new bool[nodeCount];
        }

        private void ResetState(int nodeCount)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                _cameFrom[i] = -1;
                _gScore[i] = float.PositiveInfinity;
                _fScore[i] = float.PositiveInfinity;
                _closed[i] = false;
            }
        }

        private int PopLowestFScoreNode()
        {
            int bestOpenIndex = 0;
            int bestNode = _open[0];
            float bestScore = _fScore[bestNode];

            for (int i = 1; i < _open.Count; i++)
            {
                int node = _open[i];
                float score = _fScore[node];
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                    bestOpenIndex = i;
                }
            }

            int lastIndex = _open.Count - 1;
            _open[bestOpenIndex] = _open[lastIndex];
            _open.RemoveAt(lastIndex);
            return bestNode;
        }

        private bool ContainsOpenNode(int node)
        {
            for (int i = 0; i < _open.Count; i++)
            {
                if (_open[i] == node)
                {
                    return true;
                }
            }

            return false;
        }

        private float Heuristic(int nodeId, int goalNodeId)
        {
            Vector3 from = _graph.GetNodePosition(nodeId);
            Vector3 to = _graph.GetNodePosition(goalNodeId);
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        private void ReconstructPath(int current, List<int> result)
        {
            result.Clear();
            while (current >= 0)
            {
                result.Add(current);
                current = _cameFrom[current];
            }

            int left = 0;
            int right = result.Count - 1;
            while (left < right)
            {
                int temp = result[left];
                result[left] = result[right];
                result[right] = temp;
                left++;
                right--;
            }
        }
    }
}
