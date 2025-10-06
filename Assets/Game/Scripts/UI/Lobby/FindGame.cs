using Game.Scripts.Core.Helpers;
using Game.Scripts.Server;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.Lobby
{
    public class FindGame : MonoBehaviour
    {
        private static FindGame _in;
            
        public TMP_Text timer;
        public TMP_Text players;
        
        private void Awake()
        {
            _in = this;
        }
    
        public static void UpdateInfo(float time, int players)
        {
            _in.timer.text = GameplayAssistant.ConvertToTime(time);
            _in.players.text = players + "/" + ServerSettings.In.maxPlayersForFindRoom;
        }
    }
}
