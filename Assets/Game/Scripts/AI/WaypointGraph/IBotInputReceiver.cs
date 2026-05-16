namespace Game.Scripts.AI.WaypointGraph
{
    public interface IBotInputReceiver
    {
        void ApplyBotInput(float forward, float turn);
    }
}
