namespace Game.Scripts.Networking.Lobby
{
    public enum MatchTeam
    {
        None = 0,
        TeamA = 1,
        TeamB = 2
    }

    public static class MatchTeamUtility
    {
        public static bool IsAssigned(MatchTeam team)
        {
            return team != MatchTeam.None;
        }

        public static bool AreSameAssignedTeam(MatchTeam left, MatchTeam right)
        {
            return IsAssigned(left) && left == right;
        }

        public static bool AreOpposingAssignedTeams(MatchTeam left, MatchTeam right)
        {
            return IsAssigned(left) && IsAssigned(right) && left != right;
        }
    }
}
