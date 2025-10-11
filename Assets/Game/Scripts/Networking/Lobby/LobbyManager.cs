using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.MenuController;
using Game.Scripts.UI.Lobby;
using Game.Scripts.UI.MainMenu;
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
        
        [SerializeField] private MainMenu mainMenu;
        
        private void Awake()
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            backCreateButton.onClick.AddListener(OnBackCreateClicked);
            backLobbyButton.onClick.AddListener(OnBackLobbyClicked);
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
        private void RequestLoginServerRpc(int clientID)
        {
            NetworkConnection senderConn = ServerManager.Clients[clientID];
            if (senderConn == null)
            {
                UnityEngine.Debug.LogError("RequestLoginServerRpc: Owner is null. Ensure this object is spawned with the correct owner.");
                return;
            }
            
            List<ServerRoom> rooms = LobbyRooms.GetRoomsByState(false).ToList();
            List<ClientRoom> roomInfos = rooms.Select(room => room.GetInfo()).ToList();
            UpdateLobbyRoomsTargetRpc(senderConn, roomInfos);
        }

        [TargetRpc]
        private void UpdateLobbyRoomsTargetRpc(NetworkConnection target, List<ClientRoom> roomList)
        {
            lobbyUI.UpdateLobbyRooms(roomList);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateRoomServerRpc(string roomName, int maxPlayers, string selectedLocation, string loginName, int senderId) //створення кімнати гравцем
        {
            string roomId = Guid.NewGuid().ToString();

            ServerRoom room = Instantiate(serverRoomPrefab, transform);

            room.roomId = roomId;
            room.maxPlayers = maxPlayers;
            room.selectedLocation = selectedLocation;
            room.isAutoRoom = false;
            
            Player creator = new Player
            {
                loginName = loginName,
                Connection = ServerManager.Clients[senderId]
            };
            
            room.AddPlayer(creator);
            
            LobbyRooms.AddRoom(room);
            
            NetworkConnection senderConn = ServerManager.Clients[senderId];
            TargetRoomJoinedRpc(senderConn, room.GetInfo());
            UpdateLobbyRooms();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void CreateRoomOrJoinServerRpc(int maxPlayers, string selectedLocation, string loginName, int senderId)
        {
            ServerRoom serverRoom = LobbyRooms.GetNotFullAutoRoom();

            if (serverRoom == null)
            {
                ServerRoom room = Instantiate(serverRoomPrefab, transform);

                room.roomId = Guid.NewGuid().ToString();
                room.maxPlayers = maxPlayers;
                room.selectedLocation = selectedLocation;
                room.isAutoRoom = true;

                Player player = new Player
                {
                    loginName = loginName,
                    Connection = ServerManager.Clients[senderId]
                };
                
                room.AddPlayer(player);
                
                LobbyRooms.AddRoom(room);
                room.RunTimerAsync(); //start timer
                room.OnTimeIsUp += StartGame;
                serverRoom = room;
            }
            else
            {
                serverRoom.AddPlayer(new Player
                {
                    loginName = loginName,
                    Connection = ServerManager.Clients[senderId]
                });
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
                if (player.isBot == false)
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
            // 1) Отримуємо підключення
            NetworkConnection connection = ServerManager.Clients[clientId];
            
            // 3) Якщо гравець вже в кімнаті (авто або створеній вручну) — видаляємо звідти
            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(connection);
            if (serverRoom != null)
            {
                Player player = serverRoom.GetPlayerBuyConnection(connection);
                if (!serverRoom.isInGame)
                {
                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
                }
            }

            // 4) Приховуємо індикатор пошуку на клієнті
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
            ServerRoom serverRoom = LobbyRooms.GetRoomById(roomId);

            if (serverRoom == null)
            {
                if (ServerManager.Clients.TryGetValue(senderId, out NetworkConnection senderConn))
                {
                    TargetJoinRoomResponseRpc(senderConn, false, "Room not found", null);
                }
                return;
            }
            if (serverRoom.PlayersCount() >= serverRoom.maxPlayers)
            {
                if (ServerManager.Clients.TryGetValue(senderId, out NetworkConnection senderConn))
                {
                    TargetJoinRoomResponseRpc(senderConn, false, "Room is full", null);
                }
                return;
            }
            if (serverRoom.GetPlayers().Exists(p => p.loginName == loginName))
            {
                if (ServerManager.Clients.TryGetValue(senderId, out NetworkConnection senderConn))
                {
                    TargetJoinRoomResponseRpc(senderConn, false, "You are already in the room", serverRoom.GetInfo());
                }
                return;
            }
    
            Player newPlayer = new Player
            {
                loginName = loginName,
                Connection = ServerManager.Clients[senderId]
            };

            LobbyRooms.AddPlayerToRoom(roomId, newPlayer);

            if (ServerManager.Clients.TryGetValue(senderId, out NetworkConnection conn))
            {
                TargetJoinRoomResponseRpc(conn, true, "", serverRoom.GetInfo());
            }

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
            
            Player player = serverRoom.GetPlayers().Find(p => p.loginName == loginName);
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
            
            List<NetworkConnection> connections = new();
            
            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player.isBot == false)
                {
                    connections.Add(player.Connection);
                }
            }
            
            ServerRoom updateServerRoom = LobbyRooms.GetRoomById(serverRoom.roomId);
            updateServerRoom.loadedSceneName = serverRoom.selectedLocation;
            
            SceneLoadData sld = new SceneLoadData(serverRoom.selectedLocation)
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
                    
                    ClientParams = BitConverter.GetBytes(offset)  // передаємо зсув клієнтам
                }
            };

            foreach (NetworkConnection networkConnection in connections)
            {
                TargetStartGameResponseRpc(networkConnection);
            }

            SceneManager.LoadConnectionScenes(connections.ToArray(), sld);
            UpdateLobbyRooms();
        }
        
        [TargetRpc]
        public void TargetRoomJoinedRpc(NetworkConnection target, ClientRoom clientRoom)
        {
            lobbyUI.OnRoomJoined(clientRoom);
        }

        [TargetRpc]
        public void TargetStartGameResponseRpc(NetworkConnection target)
        {
            lobbyUI.HideRoomUI();
        }

        [TargetRpc]
        public void TargetJoinRoomResponseRpc(NetworkConnection target, bool success, string errorMessage, ClientRoom clientRoom)
        {
            lobbyUI.OnJoinRoomResponse(success, errorMessage, clientRoom);
        }

        public void UpdateLobbyRooms()
        {
            List<ClientRoom> roomInfos = LobbyRooms.GetRoomsByState(false).Select(r => r.GetInfo()).ToList();
            UpdateLobbyRoomsObserversRpc(roomInfos);
        }

        [ObserversRpc]
        private void UpdateLobbyRoomsObserversRpc(List<ClientRoom> rooms)
        {
            lobbyUI.UpdateLobbyRooms(rooms);
        }

        private void UpdateCountPlayerInRoom(ServerRoom serverRoom)
        {
            UpdateCountPlayerObserversRpc(serverRoom.GetInfo());
        }

        [ObserversRpc]
        private void UpdateCountPlayerObserversRpc(ClientRoom clientRoom)
        {
            roomUI.UpdateCountPlayerInRoom(clientRoom);
        }
    }
}
