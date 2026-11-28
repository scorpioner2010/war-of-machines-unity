using System;
using System.Collections.Generic;
using RAV.Scripts.API;
using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeGraphNodePlacement
    {
        public TreeGraphNodePlacement(VehicleNode node, int depth, Vector2 position)
        {
            Node = node;
            Depth = depth;
            Position = position;
        }

        public VehicleNode Node { get; }
        public int Depth { get; }
        public Vector2 Position { get; }
    }

    public sealed class TreeGraphLayout
    {
        private readonly Dictionary<int, TreeGraphNodePlacement> _placements;
        private readonly Dictionary<int, List<int>> _nodeIdsByDepth;

        public TreeGraphLayout(
            Dictionary<int, TreeGraphNodePlacement> placements,
            Dictionary<int, List<int>> nodeIdsByDepth,
            int maxDepth)
        {
            _placements = placements;
            _nodeIdsByDepth = nodeIdsByDepth;
            MaxDepth = maxDepth;
        }

        public int MaxDepth { get; }
        public IReadOnlyDictionary<int, TreeGraphNodePlacement> Placements => _placements;
        public IReadOnlyDictionary<int, List<int>> NodeIdsByDepth => _nodeIdsByDepth;
    }

    public sealed class TreeGraphLayoutPlanner
    {
        private sealed class GraphState
        {
            public readonly Dictionary<int, VehicleNode> NodesById = new Dictionary<int, VehicleNode>();
            public readonly Dictionary<int, List<int>> ChildrenById = new Dictionary<int, List<int>>();
            public readonly Dictionary<int, List<int>> ParentsById = new Dictionary<int, List<int>>();
            public readonly Dictionary<int, int> DiscoveryOrder = new Dictionary<int, int>();
        }

        private sealed class NeighborBlock
        {
            public readonly List<int> NodeIds = new List<int>();
            public float Barycenter;
            public int FirstDiscoveryOrder;
        }

        public TreeGraphLayout Plan(
            VehicleGraph graph,
            Vector2 nodeSize,
            float horizontalGap,
            float verticalGap)
        {
            return Plan(
                graph != null ? graph.nodes : null,
                graph != null ? graph.edges : null,
                nodeSize,
                horizontalGap,
                verticalGap);
        }

        public TreeGraphLayout Plan(
            VehicleNode[] nodes,
            VehicleEdge[] edges,
            Vector2 nodeSize,
            float horizontalGap,
            float verticalGap)
        {
            GraphState state = BuildState(nodes, edges);
            if (state.NodesById.Count == 0)
            {
                return new TreeGraphLayout(
                    new Dictionary<int, TreeGraphNodePlacement>(),
                    new Dictionary<int, List<int>>(),
                    0);
            }

            Dictionary<int, int> depths = ComputeDepths(state);
            Dictionary<int, List<int>> nodeIdsByDepth = GroupByDepth(state, depths);
            RefineLayerOrder(state, nodeIdsByDepth);

            float rowStep = Mathf.Max(1f, nodeSize.y + Mathf.Max(0f, verticalGap));
            float bottomY = -(nodeIdsByDepth.Count - 1) * rowStep * 0.5f;
            float columnStep = Mathf.Max(1f, nodeSize.x + Mathf.Max(0f, horizontalGap));
            Dictionary<int, TreeGraphNodePlacement> placements = new Dictionary<int, TreeGraphNodePlacement>();

            foreach (KeyValuePair<int, List<int>> layer in nodeIdsByDepth)
            {
                List<int> nodeIds = layer.Value;
                float layerStartX = -(nodeIds.Count - 1) * columnStep * 0.5f;
                float y = bottomY + layer.Key * rowStep;
                for (int nodeIndex = 0; nodeIndex < nodeIds.Count; nodeIndex++)
                {
                    int nodeId = nodeIds[nodeIndex];
                    placements[nodeId] = new TreeGraphNodePlacement(
                        state.NodesById[nodeId],
                        layer.Key,
                        new Vector2(layerStartX + nodeIndex * columnStep, y));
                }
            }

            return new TreeGraphLayout(placements, nodeIdsByDepth, GetMaxDepth(depths));
        }

        private static GraphState BuildState(VehicleNode[] nodes, VehicleEdge[] edges)
        {
            GraphState state = new GraphState();
            if (nodes != null)
            {
                for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
                {
                    VehicleNode node = nodes[nodeIndex];
                    if (node == null || node.id <= 0 || state.NodesById.ContainsKey(node.id))
                    {
                        continue;
                    }

                    state.NodesById.Add(node.id, node);
                    state.DiscoveryOrder.Add(node.id, state.DiscoveryOrder.Count);
                }
            }

            if (edges == null)
            {
                return state;
            }

            for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
            {
                VehicleEdge edge = edges[edgeIndex];
                if (edge == null
                    || edge.fromId == edge.toId
                    || !state.NodesById.ContainsKey(edge.fromId)
                    || !state.NodesById.ContainsKey(edge.toId))
                {
                    continue;
                }

                AddUnique(state.ChildrenById, edge.fromId, edge.toId);
                AddUnique(state.ParentsById, edge.toId, edge.fromId);
            }

            foreach (List<int> children in state.ChildrenById.Values)
            {
                children.Sort((left, right) => CompareByDiscovery(state, left, right));
            }

            foreach (List<int> parents in state.ParentsById.Values)
            {
                parents.Sort((left, right) => CompareByDiscovery(state, left, right));
            }

            return state;
        }

        private static Dictionary<int, int> ComputeDepths(GraphState state)
        {
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Dictionary<int, int> incomingCount = new Dictionary<int, int>();
            List<int> roots = new List<int>();

            foreach (int nodeId in state.NodesById.Keys)
            {
                int parentCount = state.ParentsById.TryGetValue(nodeId, out List<int> parents)
                    ? parents.Count
                    : 0;
                incomingCount[nodeId] = parentCount;
                depths[nodeId] = 0;
                if (parentCount == 0)
                {
                    roots.Add(nodeId);
                }
            }

            roots.Sort((left, right) => CompareByDiscovery(state, left, right));
            Queue<int> pending = new Queue<int>(roots);
            while (pending.Count > 0)
            {
                int parentId = pending.Dequeue();
                if (!state.ChildrenById.TryGetValue(parentId, out List<int> children))
                {
                    continue;
                }

                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    int childId = children[childIndex];
                    depths[childId] = Mathf.Max(depths[childId], depths[parentId] + 1);
                    incomingCount[childId]--;
                    if (incomingCount[childId] == 0)
                    {
                        pending.Enqueue(childId);
                    }
                }
            }

            return depths;
        }

        private static Dictionary<int, List<int>> GroupByDepth(
            GraphState state,
            Dictionary<int, int> depths)
        {
            Dictionary<int, List<int>> layers = new Dictionary<int, List<int>>();
            foreach (KeyValuePair<int, int> depth in depths)
            {
                if (!layers.TryGetValue(depth.Value, out List<int> nodeIds))
                {
                    nodeIds = new List<int>();
                    layers.Add(depth.Value, nodeIds);
                }

                nodeIds.Add(depth.Key);
            }

            foreach (List<int> nodeIds in layers.Values)
            {
                nodeIds.Sort((left, right) => CompareByDiscovery(state, left, right));
            }

            return layers;
        }

        private static void RefineLayerOrder(GraphState state, Dictionary<int, List<int>> layers)
        {
            int maxDepth = GetMaxDepthFromLayers(layers);
            Dictionary<int, int> positions = BuildPositions(layers);
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                if (layers.TryGetValue(depth, out List<int> layer))
                {
                    SortByNeighbors(layer, state.ParentsById, positions, state);
                    positions = BuildPositions(layers);
                }
            }
        }

        private static void SortByNeighbors(
            List<int> layer,
            Dictionary<int, List<int>> neighborsById,
            Dictionary<int, int> positions,
            GraphState state)
        {
            List<NeighborBlock> blocks = BuildNeighborBlocks(layer, neighborsById, positions, state);
            blocks.Sort((left, right) =>
            {
                int barycenterCompare = left.Barycenter.CompareTo(right.Barycenter);
                return barycenterCompare != 0
                    ? barycenterCompare
                    : left.FirstDiscoveryOrder.CompareTo(right.FirstDiscoveryOrder);
            });

            layer.Clear();
            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                NeighborBlock block = blocks[blockIndex];
                block.NodeIds.Sort((left, right) =>
                {
                    float leftBarycenter = GetBarycenter(left, neighborsById, positions);
                    float rightBarycenter = GetBarycenter(right, neighborsById, positions);
                    int barycenterCompare = leftBarycenter.CompareTo(rightBarycenter);
                    return barycenterCompare != 0
                        ? barycenterCompare
                        : CompareByDiscovery(state, left, right);
                });
                layer.AddRange(block.NodeIds);
            }
        }

        private static List<NeighborBlock> BuildNeighborBlocks(
            List<int> layer,
            Dictionary<int, List<int>> neighborsById,
            Dictionary<int, int> positions,
            GraphState state)
        {
            List<NeighborBlock> blocks = new List<NeighborBlock>();
            Dictionary<int, NeighborBlock> blockByNeighbor = new Dictionary<int, NeighborBlock>();

            for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
            {
                int nodeId = layer[nodeIndex];
                NeighborBlock block = null;
                if (neighborsById.TryGetValue(nodeId, out List<int> neighbors))
                {
                    for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        if (blockByNeighbor.TryGetValue(neighbors[neighborIndex], out NeighborBlock existingBlock))
                        {
                            block = existingBlock;
                            break;
                        }
                    }
                }

                if (block == null)
                {
                    block = new NeighborBlock();
                    blocks.Add(block);
                }
                else if (neighbors != null)
                {
                    for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        int neighborId = neighbors[neighborIndex];
                        if (blockByNeighbor.TryGetValue(neighborId, out NeighborBlock existingBlock)
                            && existingBlock != block)
                        {
                            MergeBlocks(block, existingBlock, blocks, blockByNeighbor, neighborsById);
                        }
                    }
                }

                block.NodeIds.Add(nodeId);
                if (neighbors != null)
                {
                    for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        blockByNeighbor[neighbors[neighborIndex]] = block;
                    }
                }
            }

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                NeighborBlock block = blocks[blockIndex];
                block.Barycenter = GetBlockBarycenter(block, neighborsById, positions);
                block.FirstDiscoveryOrder = GetFirstDiscoveryOrder(block, state);
            }

            return blocks;
        }

        private static void MergeBlocks(
            NeighborBlock target,
            NeighborBlock source,
            List<NeighborBlock> blocks,
            Dictionary<int, NeighborBlock> blockByNeighbor,
            Dictionary<int, List<int>> neighborsById)
        {
            if (target == source)
            {
                return;
            }

            target.NodeIds.AddRange(source.NodeIds);
            blocks.Remove(source);
            for (int nodeIndex = 0; nodeIndex < source.NodeIds.Count; nodeIndex++)
            {
                int nodeId = source.NodeIds[nodeIndex];
                if (!neighborsById.TryGetValue(nodeId, out List<int> neighbors))
                {
                    continue;
                }

                for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                {
                    blockByNeighbor[neighbors[neighborIndex]] = target;
                }
            }
        }

        private static float GetBlockBarycenter(
            NeighborBlock block,
            Dictionary<int, List<int>> neighborsById,
            Dictionary<int, int> positions)
        {
            float total = 0f;
            int count = 0;
            for (int nodeIndex = 0; nodeIndex < block.NodeIds.Count; nodeIndex++)
            {
                float barycenter = GetBarycenter(block.NodeIds[nodeIndex], neighborsById, positions);
                if (float.IsPositiveInfinity(barycenter))
                {
                    continue;
                }

                total += barycenter;
                count++;
            }

            return count == 0 ? float.PositiveInfinity : total / count;
        }

        private static int GetFirstDiscoveryOrder(NeighborBlock block, GraphState state)
        {
            int firstDiscoveryOrder = int.MaxValue;
            for (int nodeIndex = 0; nodeIndex < block.NodeIds.Count; nodeIndex++)
            {
                int nodeId = block.NodeIds[nodeIndex];
                if (state.DiscoveryOrder.TryGetValue(nodeId, out int discoveryOrder))
                {
                    firstDiscoveryOrder = Mathf.Min(firstDiscoveryOrder, discoveryOrder);
                }
            }

            return firstDiscoveryOrder;
        }

        private static float GetBarycenter(
            int nodeId,
            Dictionary<int, List<int>> neighborsById,
            Dictionary<int, int> positions)
        {
            if (!neighborsById.TryGetValue(nodeId, out List<int> neighbors) || neighbors.Count == 0)
            {
                return float.PositiveInfinity;
            }

            float total = 0f;
            int count = 0;
            for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
            {
                if (positions.TryGetValue(neighbors[neighborIndex], out int position))
                {
                    total += position;
                    count++;
                }
            }

            return count == 0 ? float.PositiveInfinity : total / count;
        }

        private static Dictionary<int, int> BuildPositions(Dictionary<int, List<int>> layers)
        {
            Dictionary<int, int> positions = new Dictionary<int, int>();
            foreach (List<int> layer in layers.Values)
            {
                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    positions[layer[nodeIndex]] = nodeIndex;
                }
            }

            return positions;
        }

        private static void AddUnique(Dictionary<int, List<int>> map, int key, int value)
        {
            if (!map.TryGetValue(key, out List<int> values))
            {
                values = new List<int>();
                map.Add(key, values);
            }

            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }

        private static int CompareByDiscovery(GraphState state, int left, int right)
        {
            int leftOrder = state.DiscoveryOrder.TryGetValue(left, out int resolvedLeft)
                ? resolvedLeft
                : int.MaxValue;
            int rightOrder = state.DiscoveryOrder.TryGetValue(right, out int resolvedRight)
                ? resolvedRight
                : int.MaxValue;
            int orderCompare = leftOrder.CompareTo(rightOrder);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            return left.CompareTo(right);
        }

        private static int GetMaxDepth(Dictionary<int, int> depths)
        {
            int maxDepth = 0;
            foreach (int depth in depths.Values)
            {
                maxDepth = Mathf.Max(maxDepth, depth);
            }

            return maxDepth;
        }

        private static int GetMaxDepthFromLayers(Dictionary<int, List<int>> layers)
        {
            int maxDepth = 0;
            foreach (int depth in layers.Keys)
            {
                maxDepth = Mathf.Max(maxDepth, depth);
            }

            return maxDepth;
        }
    }
}
