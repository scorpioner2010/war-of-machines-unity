using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player.Data;
using UnityEngine;

namespace Game.Scripts.Gameplay.SceneManagement
{
    public class GameSceneLoader : NetworkBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        private float _currentProgress;
        private IPlayerClientInfo _playerClientInfo;

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            _playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            SceneManager.OnLoadPercentChange += OnLoadPercentChange;
            SceneManager.OnLoadEnd += OnLoadEnd;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            
            SceneManager.OnLoadPercentChange -= OnLoadPercentChange;
            SceneManager.OnLoadEnd -= OnLoadEnd;
        }

        private void OnLoadPercentChange(SceneLoadPercentEventArgs args)
        {
            // Update only if the loaded scene is the game scene you expect (you can filter by scene name if needed)
            _currentProgress = args.Percent;
        }

        private void OnLoadEnd(SceneLoadEndEventArgs args)
        {
            _currentProgress = 1f;
        }
    }
}