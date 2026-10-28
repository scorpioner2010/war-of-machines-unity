using Game.Scripts.Networking.Lobby;

namespace Game.Scripts.Gameplay.Robots
{
    public static class SpectatorTeamContext
    {
        private static MatchTeam _team = MatchTeam.None;

        public static void Capture(MatchTeam team)
        {
            _team = MatchTeamUtility.IsAssigned(team) ? team : MatchTeam.None;
        }

        public static bool TryGetTeam(out MatchTeam team)
        {
            team = _team;
            return MatchTeamUtility.IsAssigned(team);
        }

        public static void Clear()
        {
            _team = MatchTeam.None;
        }
    }
}
