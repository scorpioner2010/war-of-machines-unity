using System;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class RoomItemUI : MonoBehaviour
    {
        public Sprite test;
        
        [SerializeField]
        private TMP_Text playerCountText;
        
        [FormerlySerializedAs("roomData")] [SerializeField]
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

            if (clientRoom.selectedLocation == "Test")
            {
                location.sprite = test;
            }
        }

        private void OnClick()
        {
            OnRoomJoin?.Invoke(clientRoomData.roomId);
        }
    }
}