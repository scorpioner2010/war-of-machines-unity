using RAV.Scripts.API;

namespace RAV.Scripts.UI.Tree
{
    public static class TreeGraphTestFixture
    {
        public static VehicleGraph CreateIronGraphSnapshot()
        {
            return new VehicleGraph
            {
                nodes = new[]
                {
                    CreateNode(1, "ia_l1_starter", "TZ-18", "Scout", 1, true, 126f, 700f, 87, 52.8f, 79.2f, 105f),
                    CreateNode(2, "ia_l2_scout", "TZ-23 101F", "Scout", 2, true, 144f, 860f, 149, 25.2f, 37.8f, 125f),
                    CreateNode(3, "ia_l2_guardian", "TZ-42", "Guardian", 2, true, 111f, 820f, 65, 86.4f, 129.6f, 100f),
                    CreateNode(4, "ia_l2_colossus", "YUG Titan", "Colossus", 2, true, 90f, 940f, 50, 115.2f, 172.8f, 85f),
                    CreateNode(5, "ia_l3_scout_wraith", "R30-Halberd", "Scout", 3, false, 150f, 930f, 170, 34.8f, 52.2f, 135f),
                    CreateNode(6, "ia_l3_guardian_warder", "BR-174 Deadlock", "Guardian", 3, false, 120f, 1060f, 90, 100.8f, 151.2f, 108f),
                    CreateNode(7, "ia_l3_guardian_rampart", "BR-160", "Guardian", 3, false, 114f, 1020f, 80, 108f, 162f, 102f),
                    CreateNode(8, "ia_l3_colossus_mastodon", "R42-Glaive 7B", "Colossus", 3, false, 96f, 1180f, 58, 145.2f, 217.8f, 88f),
                    CreateNode(9, "ia_l4_guardian_citadel", "Blackline-09 Mk. II", "Guardian", 4, false, 126f, 1160f, 95, 120f, 180f, 110f),
                    CreateNode(10, "ia_l4_colossus_dreadnought", "V-88/R Northstar", "Colossus", 4, false, 92f, 1320f, 52, 172.8f, 259.2f, 90f)
                },
                edges = new[]
                {
                    CreateEdge(1, 2, 400),
                    CreateEdge(2, 3, 700),
                    CreateEdge(3, 4, 1000),
                    CreateEdge(1, 7, 2600),
                    CreateEdge(7, 6, 2200),
                    CreateEdge(6, 5, 1800),
                    CreateEdge(7, 8, 3200),
                    CreateEdge(8, 10, 6200),
                    CreateEdge(10, 9, 5000)
                }
            };
        }

        public static VehicleGraph CreateNovaGraphSnapshot()
        {
            return new VehicleGraph
            {
                nodes = new[]
                {
                    CreateNode(11, "nv_l1_starter", "N-16 Whitecap", "Scout", 1, true, 135f, 1360f, 118, 38.4f, 57.6f, 110f),
                    CreateNode(12, "nv_l2_scout", "N-17", "Scout", 2, true, 150f, 1420f, 185, 28.8f, 43.2f, 130f),
                    CreateNode(13, "nv_l2_guardian", "ARGUS-7B", "Guardian", 2, true, 120f, 1460f, 75, 74.4f, 112.8f, 105f),
                    CreateNode(14, "nv_l2_colossus", "Cormorant 8C", "Colossus", 2, true, 99f, 1520f, 57, 100.8f, 151.2f, 90f),
                    CreateNode(15, "nv_l3_scout_rift", "VX-44/2", "Scout", 3, false, 150f, 1500f, 205, 33.6f, 50.4f, 140f),
                    CreateNode(16, "nv_l3_scout_specter", "ZR-22 Knifepoint", "Scout", 3, false, 150f, 1510f, 220, 30f, 45f, 145f),
                    CreateNode(17, "nv_l4_scout_horizon", "77-V Seeker", "Scout", 4, false, 150f, 1560f, 235, 36f, 54f, 155f),
                    CreateNode(18, "nv_l3_guardian_prism", "P-8 Null Compass", "Guardian", 3, false, 132f, 1540f, 95, 90f, 135f, 115f),
                    CreateNode(19, "nv_l3_colossus_harrow", "S-404 Firebreak", "Colossus", 3, false, 105f, 1600f, 62, 115.2f, 172.8f, 94f),
                    CreateNode(20, "nv_l3_colossus_oblivion", "A-93 Graveward", "Colossus", 3, false, 99f, 1630f, 55, 129.6f, 194.4f, 88f),
                    CreateNode(21, "nv_l4_colossus_eclipse", "M-03 Sundown", "Colossus", 4, false, 93f, 1700f, 50, 151.2f, 226.8f, 92f)
                },
                edges = new[]
                {
                    CreateEdge(11, 12, 400),
                    CreateEdge(12, 13, 700),
                    CreateEdge(13, 14, 1000),
                    CreateEdge(14, 16, 2200),
                    CreateEdge(11, 18, 2400),
                    CreateEdge(18, 15, 1700),
                    CreateEdge(15, 21, 6500),
                    CreateEdge(15, 17, 4800),
                    CreateEdge(11, 20, 3600),
                    CreateEdge(20, 19, 3000),
                    CreateEdge(19, 21, 6500)
                }
            };
        }

        private static VehicleNode CreateNode(
            int id,
            string code,
            string name,
            string vehicleClass,
            int level,
            bool isVisible,
            float shellSpeed,
            float enginePower,
            int shellsCount,
            float damageMin,
            float damageMax,
            float viewRange)
        {
            return new VehicleNode
            {
                id = id,
                code = code,
                name = name,
                @class = vehicleClass,
                level = level,
                branch = "biped",
                factionCode = "nova_syndicate",
                isVisible = isVisible,
                shellSpeed = shellSpeed,
                enginePower = enginePower,
                shellsCount = shellsCount,
                damageMin = damageMin,
                damageMax = damageMax,
                viewRange = viewRange
            };
        }

        private static VehicleEdge CreateEdge(int fromId, int toId, int requiredXp)
        {
            return new VehicleEdge
            {
                fromId = fromId,
                toId = toId,
                requiredXp = requiredXp
            };
        }
    }
}
