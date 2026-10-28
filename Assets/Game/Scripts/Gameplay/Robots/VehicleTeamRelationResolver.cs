using Game.Scripts.Networking.Lobby;

namespace Game.Scripts.Gameplay.Robots
{
    public enum VehicleTeamRelation
    {
        Unknown,
        Ally,
        Enemy
    }

    public static class VehicleTeamRelationResolver
    {
        public static VehicleTeamRelation Resolve(VehicleRoot target)
        {
            if (target == null || target.characterInit == null)
            {
                return VehicleTeamRelation.Unknown;
            }

            MatchTeam targetTeam = target.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(targetTeam))
            {
                return VehicleTeamRelation.Unknown;
            }

            if (TryGetLocalTeam(out MatchTeam localTeam))
            {
                return MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam)
                    ? VehicleTeamRelation.Ally
                    : VehicleTeamRelation.Enemy;
            }

            return VehicleTeamRelation.Unknown;
        }

        private static bool TryGetLocalTeam(out MatchTeam team)
        {
            VehicleRoot localPlayer = VehicleRoot.LocalPlayerVehicle;
            if (localPlayer != null
                && localPlayer.characterInit != null
                && MatchTeamUtility.IsAssigned(localPlayer.characterInit.Team.Value))
            {
                team = localPlayer.characterInit.Team.Value;
                return true;
            }

            return SpectatorTeamContext.TryGetTeam(out team);
        }
    }
}
