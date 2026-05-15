using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class GameplayTimerDisplay : MonoBehaviour
    {
        public TMP_Text timerText;
        private static GameplayTimerDisplay _instance;

        private void Awake()
        {
            _instance = this;
        }

        public static void SetTime(float time)
        {
            if (_instance == null || _instance.timerText == null)
            {
                return;
            }

            _instance.timerText.text = time.ToString();
        }
    }
}
