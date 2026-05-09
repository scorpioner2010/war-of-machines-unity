using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.API.Models;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public class LobbyManager : NetworkBehaviour
    {
        [SerializeField] private ServerRoom serverRoomPrefab;

        public override void OnStartClient()
        {
            base.OnStartClient();
            RequestServerSettingsServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void FindMatchServerRpc(string selectedLocation, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            Player player = CreatePlayer(sender);
            if (player.Connection == null)
            {
                return;
            }

            ServerRoom room = LobbyRooms.GetNotFullAutoRoom();
            if (room == null)
            {
                room = CreateMatchmakingRoom(selectedLocation);
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
        private void RequestServerSettingsServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            TargetServerSettingsRpc(sender, ServerSettings.GetMaxPlayersForFindRoom(), ServerSettings.GetFindRoomSeconds());
        }

        [TargetRpc]
        private void TargetServerSettingsRpc(NetworkConnection target, int maxPlayersForFindRoom, int findRoomSeconds)
        {
            RemoteServerSettings.Apply(maxPlayersForFindRoom, findRoomSeconds);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelFindRoomServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            ServerRoom room = LobbyRooms.GetRoomByConnection(sender);
            if (room != null && !room.isInGame)
            {
                Player player = room.GetPlayerByConnection(sender);
                if (player != null)
                {
                    LobbyRooms.RemovePlayerFromRoom(room.roomId, player.loginName);
                }
            }

            TargetMatchmakingCancelledRpc(sender);
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
            room.AssignTeams();
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

        private Player CreatePlayer(NetworkConnection sender)
        {
            PlayerProfile profile = ServerPlayerSessions.GetProfile(sender.ClientId);

            return new Player
            {
                loginName = profile != null ? profile.username : string.Empty,
                Connection = sender,
                userId = profile != null ? profile.id : 0,
                mmr = profile != null ? profile.mmr : 1000,
                team = MatchTeam.None
            };
        }

        private ServerRoom CreateMatchmakingRoom(string selectedLocation)
        {
            ServerRoom room = Instantiate(serverRoomPrefab, transform);
            room.roomId = Guid.NewGuid().ToString();
            room.maxPlayers = ServerSettings.GetMaxPlayersForFindRoom();
            room.selectedLocation = selectedLocation;
            room.isAutoRoom = true;
            return room;
        }
    }
}
