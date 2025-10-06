using System.Collections.Generic;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using UnityEngine;

namespace Game.Scripts.UI.Lobby
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Transform roomListParent;
        [SerializeField] private GameObject roomItemPrefab;
        
        public List<RoomItemUI> localRooms = new();
        
        [SerializeField] private RoomUI roomUI;
        [SerializeField] private CreateRoomUI createRoomUI;
        [SerializeField] private LobbyManager lobbyManager;
        
        private IPlayerClientInfo _playerClientInfo;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            _playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();
        }

        public void HideRoomUI()
        {
            MenuManager.CloseMenu(MenuType.RoomUI);
        }
        
        public void UpdateLobbyRooms(List<ClientRoom> rooms)
        {
            foreach (RoomItemUI room in localRooms)
            {
                Destroy(room.gameObject);
            }
            
            localRooms.Clear();
            
            foreach (ClientRoom room in rooms)
            {
                GameObject roomItemUI = Instantiate(roomItemPrefab, roomListParent);
                RoomItemUI newRoomItem = roomItemUI.GetComponent<RoomItemUI>();
                
                if (newRoomItem != null)
                {
                    newRoomItem.SetRoomData(room);
                    newRoomItem.OnRoomJoin += OnRoomJoin;
                    localRooms.Add(newRoomItem);
                }
            }
        }

        private void OnRoomJoin(string roomId)
        {
            lobbyManager.JoinRoomServerRpc(roomId, _playerClientInfo.Profile.username, _playerClientInfo.ClientId);
        }

        public void OnRoomJoined(ClientRoom clientRoom)
        {
            MenuManager.OpenMenu(MenuType.RoomUI);
            roomUI.ShowRoom(clientRoom);
        }

        public void OnJoinRoomResponse(bool success, string errorMessage, ClientRoom clientRoom)
        {
            if (success)
            {
                OnRoomJoined(clientRoom);
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to join room: " + errorMessage);
            }
        }
    }
}
