using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.MenuController;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public class LobbyManager : NetworkBehaviour
    {
        [SerializeField] private ServerRoom serverRoomPrefab;

        [ServerRpc(RequireOwnership = false)]
        public void FindMatchServerRpc(int maxPlayers, string selectedLocation, string loginName, int senderId)
        {
            Player player = CreatePlayer(loginName, senderId);
            if (player.Connection == null)
            {
                return;
            }

            ServerRoom room = LobbyRooms.GetNotFullAutoRoom();
            if (room == null)
            {
                room = CreateMatchmakingRoom(maxPlayers, selectedLocation);
                LobbyRooms.AddRoom(room);
                room.OnTimeIsUp += StartGame;
                room.RunTimerAsync();
            }

            room.AddPlayer(player);

            if (room.IsFull())
            {
                StartGame(room);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelFindRoomServerRpc(int clientId)
        {
            NetworkConnection connection = GetConnectionOrNull(clientId);
            if (connection == null)
            {
                return;
            }

            ServerRoom room = LobbyRooms.GetRoomByConnection(connection);
            if (room != null && !room.isInGame)
            {
                Player player = room.GetPlayerByConnection(connection);
                if (player != null)
                {
                    LobbyRooms.RemovePlayerFromRoom(room.roomId, player.loginName);
                }
            }

            TargetMatchmakingCancelledRpc(connection);
        }

        private void StartGame(ServerRoom room)
        {
            if (room == null || room.isInGame)
            {
                return;
            }

            List<NetworkConnection> connections = GetRealPlayerConnections(room);
            if (connections.Count == 0)
            {
                LobbyRooms.RemoveRoom(room);
                return;
            }

            room.isInGame = true;
            LoadScene(room, connections);
        }

        private void LoadScene(ServerRoom room, List<NetworkConnection> connections)
        {
            int offset = GameplaySpawner.In.sceneOffsetX;
            room.loadedSceneName = room.selectedLocation;

            SceneLoadData sceneLoadData = new SceneLoadData(room.selectedLocation)
            {
                Options =
                {
                    AllowStacking = true,
                    AutomaticallyUnload = true,
                },
                Params =
                {
                    ServerParams = new object[]
                    {
                        room
                    },
                    ClientParams = BitConverter.GetBytes(offset)
                }
            };

            for (int i = 0; i < connections.Count; i++)
            {
                TargetStartGameResponseRpc(connections[i]);
            }

            SceneManager.LoadConnectionScenes(connections.ToArray(), sceneLoadData);
        }

        [TargetRpc]
        private void TargetMatchmakingCancelledRpc(NetworkConnection target)
        {
            MenuManager.OpenMenu(MenuType.MainMenu);
        }

        [TargetRpc]
        private void TargetStartGameResponseRpc(NetworkConnection target)
        {
            MenuManager.CloseMenu(MenuType.FindGame);
        }

        private List<NetworkConnection> GetRealPlayerConnections(ServerRoom room)
        {
            List<NetworkConnection> connections = new List<NetworkConnection>();

            foreach (Player player in room.GetPlayers())
            {
                if (!player.isBot && player.Connection != null)
                {
                    connections.Add(player.Connection);
                }
            }

            return connections;
        }

        private NetworkConnection GetConnectionOrNull(int clientId)
        {
            if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection connection))
            {
                return connection;
            }

            return null;
        }

        private Player CreatePlayer(string loginName, int senderId)
        {
            return new Player
            {
                loginName = loginName,
                Connection = GetConnectionOrNull(senderId)
            };
        }

        private ServerRoom CreateMatchmakingRoom(int maxPlayers, string selectedLocation)
        {
            ServerRoom room = Instantiate(serverRoomPrefab, transform);
            room.roomId = Guid.NewGuid().ToString();
            room.maxPlayers = maxPlayers;
            room.selectedLocation = selectedLocation;
            room.isAutoRoom = true;
            return room;
        }
    }
}
