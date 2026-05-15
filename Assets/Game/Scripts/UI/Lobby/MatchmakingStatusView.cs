using Game.Scripts.Core.Helpers;
using Game.Scripts.Server;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.Lobby
{
    public class MatchmakingStatusView : MonoBehaviour
    {
        private static MatchmakingStatusView _instance;
            
        public TMP_Text timer;
        public TMP_Text players;
        
        private void Awake()
        {
            _instance = this;
        }
    
        public static void UpdateInfo(float time, int players)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.timer.text = GameplayAssistant.ConvertToTime(time);
            _instance.players.text = players + "/" + RemoteServerSettings.MaxPlayersForFindRoom;
        }
    }
}
