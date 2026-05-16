using System;
using System.Collections.Generic;
using FishNet.Connection;
using Game.Scripts.Gameplay.Robots;
using Object = UnityEngine.Object;

namespace Game.Scripts.Networking.Lobby
{
    public static class LobbyRooms //only server
    {
        public static readonly Dictionary<string, ServerRoom> Rooms = new Dictionary<string, ServerRoom>();

        public static void AddRoom(ServerRoom serverRoom)
        {
            serverRoom.CreatedTime = DateTime.Now;
            serverRoom.isInGame = false;
            Rooms[serverRoom.roomId] = serverRoom;
        }

        public static ServerRoom GetNotFullAutoRoom()
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room != null && room.isAutoRoom && room.IsFull() == false && room.isInGame == false)
                {
                    return room;
                }
            }

            return null;
        }
        
        public static ServerRoom GetRoomByHandle(int handle)
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room != null && room.handle == handle)
                {
                    return room;
                }
            }

            return null;
        }

        public static ServerRoom GetRoomById(string roomId)
        {
            if (Rooms.TryGetValue(roomId, out ServerRoom room))
            {
                return room;
            }
            
            return null;
        }
        
        public static void UpdateRoomStatusInGame(string roomId)
        {
            ServerRoom serverRoom = GetRoomById(roomId);
            if (serverRoom == null)
            {
                return;
            }

            serverRoom.isInGame = true;
        }

        public static List<ServerRoom> GetRoomsByState(bool isInGame)
        {
            List<ServerRoom> rooms = new List<ServerRoom>();
            
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room != null && room.isInGame == isInGame)
                {
                    rooms.Add(room);
                }
            }
            return rooms;
        }

        public static void AddPlayerToRoom(string roomId, Player player)
        {
            ServerRoom serverRoom = GetRoomById(roomId);
            if (serverRoom == null)
            {
                return;
            }

            if (serverRoom.PlayersCount() >= serverRoom.maxPlayers)
            {
                return;
            }
            
            serverRoom.AddPlayer(player);
        }

        public static void RemovePlayerFromRoom(string roomId, string loginName)
        {
            ServerRoom serverRoom = GetRoomById(roomId);
            if (serverRoom == null)
            {
                return;
            }
            
            Player player = serverRoom.GetPlayerByName(loginName);
            
            if (player != null)
            {
                serverRoom.RemovePlayer(player);
            }

            if (serverRoom.GetPlayers().Count == 0)
            {
                ReleaseRoomSceneSlot(serverRoom);
                Rooms.Remove(roomId);
                Object.Destroy(serverRoom.gameObject);
            }
        }

        public static void RemoveRoom(ServerRoom serverRoom)
        {
            if (serverRoom == null)
            {
                return;
            }

            Rooms.Remove(serverRoom.roomId);
            ReleaseRoomSceneSlot(serverRoom);
            Object.Destroy(serverRoom.gameObject);
        }

        private static void ReleaseRoomSceneSlot(ServerRoom serverRoom)
        {
            if (GameplaySpawner.In != null)
            {
                GameplaySpawner.In.ReleaseRoomSceneSlot(serverRoom);
            }
        }
        
        public static ServerRoom GetRoomByConnection(NetworkConnection conn)
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                bool isHas = room.HasPlayer(conn);
                
                if (isHas)
                {
                    return room;
                }
            }
            return null;
        }

        public static List<ServerRoom> GetRoomsByConnection(NetworkConnection conn)
        {
            List<ServerRoom> rooms = new List<ServerRoom>();

            foreach (ServerRoom room in Rooms.Values)
            {
                if (room != null && room.HasPlayer(conn))
                {
                    rooms.Add(room);
                }
            }

            return rooms;
        }
        
        public static ServerRoom GetRoomByClientId(int clientId)
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                bool isHas = room.HasPlayer(clientId);
                
                if (isHas)
                {
                    return room;
                }
            }
            return null;
        }

        public static ServerRoom GetRoomByVehicle(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null)
            {
                return null;
            }

            foreach (ServerRoom room in Rooms.Values)
            {
                if (room != null && room.GetPlayerByVehicle(vehicleRoot) != null)
                {
                    return room;
                }
            }

            return null;
        }
    }
}
