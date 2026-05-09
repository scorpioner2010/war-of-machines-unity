using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.MenuController;
using Game.Scripts.UI.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Networking.Lobby
{
    public class LobbyManager : NetworkBehaviour
    {
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button backCreateButton;

        [SerializeField] private LobbyUI lobbyUI;
        [SerializeField] private Button backLobbyButton;
        [SerializeField] private RoomUI roomUI;

        [SerializeField] private ServerRoom serverRoomPrefab;

        private void Awake()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            }

            if (backCreateButton != null)
            {
                backCreateButton.onClick.AddListener(OnBackCreateClicked);
            }

            if (backLobbyButton != null)
            {
                backLobbyButton.onClick.AddListener(OnBackLobbyClicked);
            }
        }

        private void OnBackLobbyClicked()
        {
            MenuManager.OpenMenu(MenuType.MainMenu);
        }

        private void OnBackCreateClicked()
        {
            MenuManager.CloseMenu(MenuType.CreateRoom);
        }

        private void OnCreateRoomClicked()
        {
            MenuManager.OpenMenu(MenuType.CreateRoom);
        }

        public override void OnStartClient()
        {
            RequestGetRoomList();
        }

        public void RequestGetRoomList()
        {
            RequestLoginServerRpc(ClientManager.Connection.ClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestLoginServerRpc(int clientId)
        {
            NetworkConnection senderConnection = GetConnectionOrNull(clientId);
            if (senderConnection == null)
            {
                return;
            }

            UpdateLobbyRoomsTargetRpc(senderConnection, GetOpenRoomInfos());
        }

        [TargetRpc]
        private void UpdateLobbyRoomsTargetRpc(NetworkConnection target, List<ClientRoom> roomList)
        {
            if (lobbyUI != null)
            {
                lobbyUI.UpdateLobbyRooms(roomList);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateRoomServerRpc(string roomName, int maxPlayers, string selectedLocation, string loginName, int senderId)
        {
            ServerRoom room = CreateServerRoom(maxPlayers, selectedLocation, false);
            Player creator = CreatePlayer(loginName, senderId);

            if (creator.Connection == null)
            {
                Destroy(room.gameObject);
                return;
            }

            room.roomName = roomName;
            room.AddPlayer(creator);

            LobbyRooms.AddRoom(room);
            TargetRoomJoinedRpc(creator.Connection, room.GetInfo());
            UpdateLobbyRooms();
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateRoomOrJoinServerRpc(int maxPlayers, string selectedLocation, string loginName, int senderId)
        {
            Player player = CreatePlayer(loginName, senderId);
            if (player.Connection == null)
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetNotFullAutoRoom();
            if (serverRoom == null)
            {
                serverRoom = CreateServerRoom(maxPlayers, selectedLocation, true);
                serverRoom.AddPlayer(player);
                LobbyRooms.AddRoom(serverRoom);
                serverRoom.RunTimerAsync();
                serverRoom.OnTimeIsUp += StartGame;
            }
            else
            {
                serverRoom.AddPlayer(player);
            }

            if (serverRoom.IsFull())
            {
                StartGame(serverRoom);
            }
        }

        private void StartGame(ServerRoom room)
        {
            foreach (Player player in room.GetPlayers())
            {
                if (!player.isBot)
                {
                    HideLoadingFindPlayers(player.Connection, false);
                }
            }

            room.isInGame = true;
            LoadScene(room);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelFindRoomServerRpc(int clientId)
        {
            NetworkConnection connection = GetConnectionOrNull(clientId);
            if (connection == null)
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(connection);
            if (serverRoom != null)
            {
                Player player = serverRoom.GetPlayerByConnection(connection);
                if (player != null && !serverRoom.isInGame)
                {
                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
                }
            }

            HideLoadingFindPlayers(connection, true);
        }

        [TargetRpc]
        public void HideLoadingFindPlayers(NetworkConnection target, bool isCancel)
        {
            if (isCancel)
            {
                MenuManager.OpenMenu(MenuType.MainMenu);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinRoomServerRpc(string roomId, string loginName, int senderId)
        {
            NetworkConnection senderConnection = GetConnectionOrNull(senderId);
            ServerRoom serverRoom = LobbyRooms.GetRoomById(roomId);

            if (serverRoom == null)
            {
                if (senderConnection != null)
                {
                    TargetJoinRoomResponseRpc(senderConnection, false, "Room not found", null);
                }
                return;
            }

            if (serverRoom.PlayersCount() >= serverRoom.maxPlayers)
            {
                if (senderConnection != null)
                {
                    TargetJoinRoomResponseRpc(senderConnection, false, "Room is full", null);
                }
                return;
            }

            if (serverRoom.GetPlayers().Exists(player => player.loginName == loginName))
            {
                if (senderConnection != null)
                {
                    TargetJoinRoomResponseRpc(senderConnection, false, "You are already in the room", serverRoom.GetInfo());
                }
                return;
            }

            Player newPlayer = CreatePlayer(loginName, senderId);
            if (newPlayer.Connection == null)
            {
                return;
            }

            LobbyRooms.AddPlayerToRoom(roomId, newPlayer);
            TargetJoinRoomResponseRpc(newPlayer.Connection, true, string.Empty, serverRoom.GetInfo());

            UpdateLobbyRooms();
            UpdateCountPlayerInRoom(serverRoom);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveRoomServerRpc(string roomId, string loginName)
        {
            LobbyRooms.RemovePlayerFromRoom(roomId, loginName);
            ServerRoom serverRoom = LobbyRooms.GetRoomById(roomId);

            if (serverRoom != null)
            {
                UpdateCountPlayerObserversRpc(serverRoom.GetInfo());
            }

            UpdateLobbyRooms();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyServerRpc(string roomId, string loginName, int senderId)
        {
            ServerRoom serverRoom = LobbyRooms.GetRoomById(roomId);
            if (serverRoom == null)
            {
                return;
            }

            Player player = serverRoom.GetPlayers().Find(item => item.loginName == loginName);
            if (player == null)
            {
                return;
            }

            UpdateCountPlayerInRoom(serverRoom);

            if (serverRoom.IsFull())
            {
                LoadScene(serverRoom);
            }
        }

        private void LoadScene(ServerRoom serverRoom)
        {
            int offset = GameplaySpawner.In.sceneOffsetX;
            List<NetworkConnection> connections = GetRealPlayerConnections(serverRoom);

            ServerRoom updateServerRoom = LobbyRooms.GetRoomById(serverRoom.roomId);
            updateServerRoom.loadedSceneName = serverRoom.selectedLocation;

            SceneLoadData sceneLoadData = new SceneLoadData(serverRoom.selectedLocation)
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
                        serverRoom
                    },
                    ClientParams = BitConverter.GetBytes(offset)
                }
            };

            foreach (NetworkConnection connection in connections)
            {
                TargetStartGameResponseRpc(connection);
            }

            SceneManager.LoadConnectionScenes(connections.ToArray(), sceneLoadData);
            UpdateLobbyRooms();
        }

        [TargetRpc]
        public void TargetRoomJoinedRpc(NetworkConnection target, ClientRoom clientRoom)
        {
            if (lobbyUI != null)
            {
                lobbyUI.OnRoomJoined(clientRoom);
            }
        }

        [TargetRpc]
        public void TargetStartGameResponseRpc(NetworkConnection target)
        {
            if (lobbyUI != null)
            {
                lobbyUI.HideRoomUI();
            }
        }

        [TargetRpc]
        public void TargetJoinRoomResponseRpc(NetworkConnection target, bool success, string errorMessage, ClientRoom clientRoom)
        {
            if (lobbyUI != null)
            {
                lobbyUI.OnJoinRoomResponse(success, errorMessage, clientRoom);
            }
        }

        public void UpdateLobbyRooms()
        {
            UpdateLobbyRoomsObserversRpc(GetOpenRoomInfos());
        }

        [ObserversRpc]
        private void UpdateLobbyRoomsObserversRpc(List<ClientRoom> rooms)
        {
            if (lobbyUI != null)
            {
                lobbyUI.UpdateLobbyRooms(rooms);
            }
        }

        private void UpdateCountPlayerInRoom(ServerRoom serverRoom)
        {
            UpdateCountPlayerObserversRpc(serverRoom.GetInfo());
        }

        [ObserversRpc]
        private void UpdateCountPlayerObserversRpc(ClientRoom clientRoom)
        {
            if (roomUI != null)
            {
                roomUI.UpdateCountPlayerInRoom(clientRoom);
            }
        }

        private List<ClientRoom> GetOpenRoomInfos()
        {
            List<ClientRoom> roomInfos = new List<ClientRoom>();

            foreach (ServerRoom room in LobbyRooms.GetRoomsByState(false))
            {
                roomInfos.Add(room.GetInfo());
            }

            return roomInfos;
        }

        private List<NetworkConnection> GetRealPlayerConnections(ServerRoom serverRoom)
        {
            List<NetworkConnection> connections = new List<NetworkConnection>();

            foreach (Player player in serverRoom.GetPlayers())
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

        private ServerRoom CreateServerRoom(int maxPlayers, string selectedLocation, bool isAutoRoom)
        {
            ServerRoom room = Instantiate(serverRoomPrefab, transform);
            room.roomId = Guid.NewGuid().ToString();
            room.maxPlayers = maxPlayers;
            room.selectedLocation = selectedLocation;
            room.isAutoRoom = isAutoRoom;
            return room;
        }
    }
}
