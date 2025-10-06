using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class RoomUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private LobbyManager lobbyManager;
        
        private ClientRoom _currentClientRoom;
        
        private void Awake()
        {
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }

        private void OnLeaveButtonClicked()
        {
            LeaveRoom();
        }

        public void ShowRoom(ClientRoom clientRoom)
        {
            _currentClientRoom = clientRoom;
            UpdateCountPlayerInRoom(clientRoom);
            IPlayerClientInfo playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            lobbyManager.ToggleReadyServerRpc(_currentClientRoom.roomId,  playerClientInfo.Profile.username, playerClientInfo.ClientId);
        }

        public void UpdateCountPlayerInRoom(ClientRoom clientRoom)
        {
            if (clientRoom == null)
            {
                return;
            }

            if (_currentClientRoom != null && _currentClientRoom.roomId != clientRoom.roomId)
            {
                return;
            }
          
            playerCountText.text = "Players ready " + clientRoom.GetPlayerCountText();
        }

        public void LeaveRoom()
        {
            IPlayerClientInfo playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();

            if (_currentClientRoom != null)
            {
                string roomId = _currentClientRoom.roomId;
                lobbyManager.LeaveRoomServerRpc(roomId, playerClientInfo.Profile.username);
            }
            
            MenuManager.OpenMenu(MenuType.CustomLobby);
        }
        
        private void OnApplicationQuit()
        {
            LeaveRoom();
        }
    }
}