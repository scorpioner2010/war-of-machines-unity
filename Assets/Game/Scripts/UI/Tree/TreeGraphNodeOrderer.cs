using System.Collections.Generic;
using RAV.Scripts.API;
using UnityEngine;

namespace RAV.Scripts.UI.Tree
{
    public sealed class TreeGraphNodeOrderer
    {
        public Dictionary<int, List<VehicleNode>> GroupAndOrder(VehicleNode[] nodes, VehicleEdge[] edges)
        {
            TreeGraphLayout layout = new TreeGraphLayoutPlanner().Plan(
                nodes,
                edges,
                new Vector2(284f, 100f),
                32f,
                80f);
            Dictionary<int, List<VehicleNode>> result = new Dictionary<int, List<VehicleNode>>();

            foreach (KeyValuePair<int, List<int>> layer in layout.NodeIdsByDepth)
            {
                List<VehicleNode> orderedNodes = new List<VehicleNode>();
                for (int nodeIndex = 0; nodeIndex < layer.Value.Count; nodeIndex++)
                {
                    orderedNodes.Add(layout.Placements[layer.Value[nodeIndex]].Node);
                }

                result[layer.Key] = orderedNodes;
            }

            return result;
        }
    }
}
