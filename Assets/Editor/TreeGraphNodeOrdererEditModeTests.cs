using System.Collections.Generic;
using NUnit.Framework;
using RAV.Scripts.UI.Tree;

namespace RAV.Editor.Tests
{
    public sealed class TreeGraphNodeOrdererEditModeTests
    {
        [Test]
        public void OrdersNovaSnapshotByGraphDepthInsteadOfServerLevel()
        {
            var graph = TreeGraphTestFixture.CreateNovaGraphSnapshot();
            Dictionary<int, List<RAV.Scripts.API.VehicleNode>> layers = new TreeGraphNodeOrderer().GroupAndOrder(graph.nodes, graph.edges);

            Assert.That(layers[0][0].code, Is.EqualTo("nv_l1_starter"));
            Assert.That(layers[1].Count, Is.EqualTo(3));
            Assert.That(layers[2].Count, Is.EqualTo(3));
            Assert.That(layers[3].Count, Is.EqualTo(3));
            Assert.That(layers[4][0].code, Is.EqualTo("nv_l3_scout_specter"));
        }

        [Test]
        public void OrdersIronBranchesByTheirActualParentPositions()
        {
            var graph = TreeGraphTestFixture.CreateIronGraphSnapshot();
            TreeGraphLayout layout = new TreeGraphLayoutPlanner().Plan(
                graph,
                new UnityEngine.Vector2(100f, 100f),
                32f,
                80f);

            List<string> depthThreeCodes = new List<string>();
            foreach (int nodeId in layout.NodeIdsByDepth[3])
            {
                depthThreeCodes.Add(layout.Placements[nodeId].Node.code);
            }

            Assert.That(
                depthThreeCodes,
                Is.EqualTo(new[] { "ia_l2_colossus", "ia_l3_scout_wraith", "ia_l4_colossus_dreadnought" }));
        }
    }
}
