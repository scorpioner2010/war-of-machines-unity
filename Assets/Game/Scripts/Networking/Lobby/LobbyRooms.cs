using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using Object = UnityEngine.Object;

namespace Game.Scripts.Networking.Lobby
{
    public static class LobbyRooms //only server
    {
        public static readonly Dictionary<string, ServerRoom> Rooms = new ();

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
                if (room.isAutoRoom && room.IsFull() == false && room.isInGame == false)
                {
                    return room;
                }
            }

            return null;
        }
        
        public static ServerRoom GetRoomByHandle(int handle)
        {
            ServerRoom singleOrDefault = Rooms.Values.SingleOrDefault(room => room.handle == handle);
            
            if (singleOrDefault != null)
            {
                return Rooms[singleOrDefault.roomId];
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
            serverRoom.isInGame = true;
        }

        public static List<ServerRoom> GetRoomsByState(bool isInGame)
        {
            List<ServerRoom> rooms = new List<ServerRoom>();
            
            foreach (ServerRoom room in Rooms.Values)
            {
                if (room.isInGame == isInGame)
                {
                    rooms.Add(room);
                }
            }
            return rooms;
        }

        public static void AddPlayerToRoom(string roomId, Player player)
        {
            ServerRoom serverRoom = GetRoomById(roomId);
         
            if (serverRoom.PlayersCount() >= serverRoom.maxPlayers)
            {
                return;
            }
            
            serverRoom.AddPlayer(player);
        }

        public static void RemovePlayerFromRoom(string roomId, string loginName)
        {
            ServerRoom serverRoom = GetRoomById(roomId);
            
            Player player = serverRoom.GetPlayerBuyName(loginName);
            
            if (player != null)
            {
                serverRoom.RemovePlayer(player);
                
                if (serverRoom.PlayersCount() == 0)
                {
                    Rooms.Remove(serverRoom.roomId);
                }
            }

            if (serverRoom.GetPlayers().Count == 0)
            {
                Rooms.Remove(roomId);
                Object.Destroy(serverRoom.gameObject);
            }
        }
        
        public static ServerRoom GetRoomByConnection(NetworkConnection conn)
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                bool isHas = room.HasPlayer(conn);
                
                if (isHas)
                {
                    return room;
                }
            }
            return null;
        }
        
        public static ServerRoom GetRoomByClientId(int clientId)
        {
            foreach (ServerRoom room in Rooms.Values)
            {
                bool isHas = room.HasPlayer(clientId);
                
                if (isHas)
                {
                    return room;
                }
            }
            return null;
        }
    }
}
