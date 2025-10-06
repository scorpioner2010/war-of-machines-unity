using System.Collections.Generic;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using Game.Scripts.World.Maps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class CreateRoomUI : MonoBehaviour
    {
        [SerializeField] private Button hideButton;
        
        [SerializeField] private Image mapIcon;
        
        [SerializeField] private Button changeMapLeft;
        [SerializeField] private Button changeMapRight;

        [SerializeField] private TMP_InputField playerNumberInput;
        
        [SerializeField] private Button createRoomButton;
        [SerializeField] private LobbyManager lobbyManager;
        
        public List<MapInfo> mapList = new();
        public GameMaps currentMap;
        private int _currentIndex;
        private IPlayerClientInfo _playerClientInfo;
        
        private void Awake()
        {
            playerNumberInput.text = 1.ToString();
            
            hideButton.onClick.AddListener(() =>
            {
                MenuManager.OpenMenu(MenuType.CustomLobby);
            });
            
            createRoomButton.onClick.AddListener(CreateRoom);
            changeMapLeft.onClick.AddListener(MapPrevious);
            changeMapRight.onClick.AddListener(MapNext);
            currentMap = Current().mapName;
            SetMapIcon();
        }
        
        private void Start() => Initialize();

        private void Initialize()
        {
            _playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();
        }
        
        private void MapPrevious()
        {
            Previous();
            SetMapIcon();
        }
        
        private void MapNext()
        {
            Next();
            SetMapIcon();
        }

        private MapInfo Current() => mapList[_currentIndex];
        private void Next()
        {
            _currentIndex = (_currentIndex + 1) % mapList.Count;
            currentMap = Current().mapName;
        }

        private void Previous()
        {
            _currentIndex = (_currentIndex - 1 + mapList.Count) % mapList.Count;
            currentMap = Current().mapName;
        }

        private void SetMapIcon()
        {
            foreach (MapInfo mapInfo in mapList)
            {
                if (mapInfo.mapName == currentMap)
                {
                    mapIcon.sprite = mapInfo.icon;
                }
            }
        }

        public void CreateRoom()
        {
            lobbyManager.CreateRoomServerRpc("Name "+currentMap, int.Parse(playerNumberInput.text), currentMap.ToString(), _playerClientInfo.Profile.username, _playerClientInfo.ClientId);
        }
    }
}