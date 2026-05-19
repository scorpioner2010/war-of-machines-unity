using FishNet;
using FishNet.Managing.Timing;
using Game.Scripts.Diagnostics;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class PingController : MonoBehaviour
    {
        public TMP_Text pingText;
        public GameObject criticalPing;
        [SerializeField] private float updateInterval = 0.25f;

        private float _nextUpdateTime;
        private long _lastPing = long.MinValue;
        private PingState _lastState = PingState.Unknown;

        private void Update()
        {
            using (ProfileScope.Measure("Client.UI.PingController.Update", DiagnosticsCategories.Ui))
            {
                if (Time.unscaledTime < _nextUpdateTime)
                {
                    return;
                }

                _nextUpdateTime = Time.unscaledTime + Mathf.Max(0.1f, updateInterval);

                long ping;
                TimeManager tm = InstanceFinder.TimeManager;

                if (tm == null)
                {
                    ping = 0;
                    ApplyPing(ping);
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

                    ApplyPing(ping);
                }
            }
        }

        private void ApplyPing(long ping)
        {
            PingState state = ResolveState(ping);
            if (ping == _lastPing && state == _lastState)
            {
                return;
            }

            _lastPing = ping;
            _lastState = state;

            if (pingText != null)
            {
                pingText.text = "Ping: " + ping;
                if (state == PingState.Warning)
                {
                    pingText.color = Color.yellow;
                }
                else if (state == PingState.Critical)
                {
                    pingText.color = Color.red;
                }
                else
                {
                    pingText.color = Color.white;
                }
            }

            if (criticalPing != null)
            {
                bool showCritical = state == PingState.Critical;
                if (criticalPing.activeSelf != showCritical)
                {
                    criticalPing.SetActive(showCritical);
                }
            }
        }

        private static PingState ResolveState(long ping)
        {
            if (ping > 150)
            {
                return PingState.Critical;
            }

            if (ping > 80)
            {
                return PingState.Warning;
            }

            return PingState.Normal;
        }

        private enum PingState
        {
            Unknown,
            Normal,
            Warning,
            Critical
        }
    }
}
