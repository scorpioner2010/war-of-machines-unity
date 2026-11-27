using System;
using System.Collections.Generic;
using RAV.Scripts.Gameplay.Robots;
using RAV.Scripts.Networking.Lobby;
using RAV.Scripts.Testing;

namespace RAV.Scripts.Gameplay.Battle
{
    public static class BattleCountdownGate
    {
        private static readonly HashSet<string> ServerLockedRooms = new HashSet<string>();
        private static bool _clientMovementAndFiringBlocked;

        public static bool IsClientMovementAndFiringBlocked =>
            _clientMovementAndFiringBlocked && !ClientVehicleTestMode.IsEnabled;
        public static bool IsClientCountdownActive => IsClientMovementAndFiringBlocked;
        public static event Action<bool> ClientCountdownStateChanged;

        public static void BeginClient()
        {
            if (ClientVehicleTestMode.IsEnabled || _clientMovementAndFiringBlocked)
            {
                return;
            }

            _clientMovementAndFiringBlocked = true;
            ClientCountdownStateChanged?.Invoke(true);
        }

        public static void EndClient()
        {
            if (!_clientMovementAndFiringBlocked)
            {
                return;
            }

            _clientMovementAndFiringBlocked = false;
            ClientCountdownStateChanged?.Invoke(false);
        }

        public static void BeginServer(string roomId)
        {
            if (!ClientVehicleTestMode.IsEnabled && !string.IsNullOrWhiteSpace(roomId))
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

        public static bool IsServerCountdownActive(string roomId)
        {
            return !ClientVehicleTestMode.IsEnabled
                && !string.IsNullOrWhiteSpace(roomId)
                && ServerLockedRooms.Contains(roomId);
        }

        public static bool IsServerMovementAndFiringBlocked(VehicleRoot vehicleRoot)
        {
            if (ClientVehicleTestMode.IsEnabled || vehicleRoot == null || !vehicleRoot.IsServerInitialized)
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
