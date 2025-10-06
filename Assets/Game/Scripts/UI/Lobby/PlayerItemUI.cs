using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.Lobby
{
    public class PlayerItemUI : MonoBehaviour
    {
        public TMP_Text playerNameText;
        public TMP_Text readyStatusText;

        public void SetPlayerInfo(Scripts.Networking.Lobby.Player info)
        {
            playerNameText.text = info.loginName;
            //readyStatusText.text = info.IsReady ? "Ready" : "Not Ready";
        }
    }
}