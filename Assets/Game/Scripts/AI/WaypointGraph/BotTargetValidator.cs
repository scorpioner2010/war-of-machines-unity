using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotTargetValidator
    {
        public bool IsEnemyTarget(VehicleRoot selfRoot, VehicleRoot targetRoot)
        {
            if (targetRoot == null || targetRoot == selfRoot)
            {
                return false;
            }

            if (targetRoot.health != null && targetRoot.health.IsDead)
            {
                return false;
            }

            if (selfRoot == null || selfRoot.characterInit == null || targetRoot.characterInit == null)
            {
                return true;
            }

            MatchTeam localTeam = selfRoot.characterInit.Team.Value;
            MatchTeam targetTeam = targetRoot.characterInit.Team.Value;
            return !MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam);
        }
    }
}
