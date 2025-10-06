using FishNet;
using FishNet.Managing.Timing;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class PingController : MonoBehaviour
    {
        public TMP_Text pingText;
        public GameObject criticalPing;
        private void Update()
        {
            long ping;
            TimeManager tm = InstanceFinder.TimeManager;
            
            if (tm == null)
            {
                ping = 0;
            }
            else
            {
                ping = tm.RoundTripTime;
                long deduction = 0;
                
                if (true)
                {
                    deduction = (long)(tm.TickDelta * 2000d);
                }

                ping = (long)Mathf.Max(1, ping - deduction);
                
                pingText.text = "Ping: "+ ping;

                if (ping > 80 && ping < 149)
                {
                    pingText.color = Color.yellow;
                    criticalPing.SetActive(false);
                }
                else if (ping > 150)
                {
                    pingText.color = Color.red;
                    criticalPing.SetActive(true);
                }
                else
                {
                    pingText.color = Color.white;
                    criticalPing.SetActive(false);
                }
            }
        }
    }
}
