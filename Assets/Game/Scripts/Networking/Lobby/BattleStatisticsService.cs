using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public static class BattleStatisticsService
    {
        public static void RecordHit(VehicleRoot attackerRoot, VehicleRoot targetRoot, float damage, bool killed)
        {
            if (attackerRoot == null || damage <= 0f)
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetRoomByVehicle(attackerRoot);
            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return;
            }

            Player attacker = serverRoom.GetPlayerByVehicle(attackerRoot);
            if (attacker == null)
            {
                return;
            }

            attacker.damage = Mathf.Clamp(attacker.damage + Mathf.RoundToInt(damage), 0, 20000);

            if (!killed || targetRoot == null || targetRoot == attackerRoot)
            {
                return;
            }

            Player target = serverRoom.GetPlayerByVehicle(targetRoot);
            if (target != null)
            {
                attacker.kills = Mathf.Clamp(attacker.kills + 1, 0, 20);
            }
        }
    }
}
