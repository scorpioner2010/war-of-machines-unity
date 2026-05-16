namespace Game.Scripts.AI.WaypointGraph
{
    public readonly struct WaypointGraphEdge
    {
        public readonly int To;
        public readonly float Cost;

        public WaypointGraphEdge(int to, float cost)
        {
            To = to;
            Cost = cost;
        }
    }
}
