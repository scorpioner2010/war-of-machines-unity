using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class GameplayTimerDisplay : MonoBehaviour
    {
        public TMP_Text timerText;
        private static GameplayTimerDisplay _instance;
        private int _lastShownTime = int.MinValue;

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

            int shownTime = Mathf.CeilToInt(Mathf.Max(0f, time));
            if (_instance._lastShownTime == shownTime)
            {
                return;
            }

            _instance._lastShownTime = shownTime;
            _instance.timerText.SetText("{0}", shownTime);
        }
    }
}
