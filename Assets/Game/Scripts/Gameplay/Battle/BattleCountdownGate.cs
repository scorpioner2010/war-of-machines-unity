using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;

namespace Game.Scripts.Gameplay.Battle
{
    public static class BattleCountdownGate
    {
        private static readonly HashSet<string> ServerLockedRooms = new HashSet<string>();

        public static bool IsClientMovementAndFiringBlocked { get; private set; }

        public static void BeginClient()
        {
            IsClientMovementAndFiringBlocked = true;
        }

        public static void EndClient()
        {
            IsClientMovementAndFiringBlocked = false;
        }

        public static void BeginServer(string roomId)
        {
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                ServerLockedRooms.Add(roomId);
            }
        }

        public static void EndServer(string roomId)
        {
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                ServerLockedRooms.Remove(roomId);
            }
        }

        public static bool IsServerMovementAndFiringBlocked(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || !vehicleRoot.IsServerInitialized)
            {
                return false;
            }

            ServerRoom room = LobbyRooms.GetRoomByVehicle(vehicleRoot);
            return room != null && ServerLockedRooms.Contains(room.roomId);
        }

        public static void ClearAllServerLocks()
        {
            ServerLockedRooms.Clear();
        }
    }
}
