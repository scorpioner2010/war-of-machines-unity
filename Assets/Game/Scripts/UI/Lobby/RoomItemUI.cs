using System;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class RoomItemUI : MonoBehaviour
    {
        [SerializeField]
        private Sprite mapPreviewSprite;
        
        [SerializeField]
        private TMP_Text playerCountText;
        
        [SerializeField]
        private ClientRoom clientRoomData;
        
        [SerializeField]
        private Image location;
        
        public event Action<string> OnRoomJoin; 
        
        [SerializeField]
        private Button button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        public void SetRoomData(ClientRoom clientRoom)
        {
            clientRoomData = clientRoom;
            playerCountText.text = clientRoom.GetPlayerCountText();

            if (clientRoom.selectedLocation == GameMaps.Map.ToString())
            {
                location.sprite = mapPreviewSprite;
            }
        }

        private void OnClick()
        {
            OnRoomJoin?.Invoke(clientRoomData.roomId);
        }
    }
}
