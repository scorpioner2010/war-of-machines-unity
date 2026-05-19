using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsOverlay : MonoBehaviour
    {
        private bool _visible;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _warnStyle;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            DiagnosticsManager manager = DiagnosticsManager.Instance;
            if (!_visible || manager == null || !manager.IsRunning)
            {
                return;
            }

            using (ProfileScope.Measure("DebugOverlay.OnGUI", DiagnosticsCategories.Editor))
            {
                DiagnosticsSnapshot snapshot = manager.GetCurrentSnapshot();
                if (snapshot == null || snapshot.Current == null)
                {
                    return;
                }

                EnsureStyles();
                DiagnosticsMetricSample sample = snapshot.Current;

                GUILayout.BeginArea(new Rect(10f, 380f, 460f, 430f), GUI.skin.box);
                GUILayout.Label("Live Diagnostics (F9)", _titleStyle);
                DrawLine("Mode", sample.Mode + " | map " + sample.Map);
                DrawLine("FPS", Format(sample.Client.Fps) + " | frame " + Format(sample.Client.FrameMs) + "ms | p95 " + Format(sample.Client.FrameMsP95_10s) + "ms");
                DrawLine("Server", "tick " + Format(sample.Server.ServerTickMs) + "ms | players " + Format(sample.Server.ActivePlayers) + " | entities " + Format(sample.Server.ActiveEntities));
                DrawLine("Network", "ping " + Format(sample.Network.PingMs) + "ms | jitter " + Format(sample.Network.JitterMs) + "ms | loss " + Format(sample.Network.PacketLossPercent) + "%");
                DrawLine("Memory", Format(sample.Client.MemoryMb) + " MB | projectiles " + Format(sample.Server.ActiveProjectiles));
                DrawLine("Net IO", Format(sample.Network.IncomingKbps) + " kbps in | " + Format(sample.Network.OutgoingKbps) + " kbps out");
                GUILayout.Space(4f);
                GUILayout.Label("Top client", _warnStyle);
                DrawScopes(sample.Client.TopSlowScopes5s);
                GUILayout.Label("Top server", _warnStyle);
                DrawScopes(sample.Server.TopSlowScopes5s);
                GUILayout.Label("Last spike", _warnStyle);
                if (snapshot.Spikes != null && snapshot.Spikes.Count > 0)
                {
                    DiagnosticsSpike spike = snapshot.Spikes[snapshot.Spikes.Count - 1];
                    GUILayout.Label(spike.Type + " | " + spike.Severity + " | " + spike.TopSuspect, _labelStyle);
                }
                else
                {
                    GUILayout.Label("none", _labelStyle);
                }

                GUILayout.EndArea();
            }
        }

        private void DrawScopes(List<DiagnosticsScopeSummary> scopes)
        {
            if (scopes == null || scopes.Count == 0)
            {
                GUILayout.Label("none", _labelStyle);
                return;
            }

            int count = Mathf.Min(3, scopes.Count);
            for (int i = 0; i < count; i++)
            {
                DiagnosticsScopeSummary scope = scopes[i];
                GUILayout.Label(scope.Name + " total " + Format(scope.TotalMs) + "ms avg " + Format(scope.AvgMs) + "ms max " + Format(scope.MaxMs) + "ms", _labelStyle);
            }
        }

        private void DrawLine(string label, string value)
        {
            GUILayout.Label(label + ": " + value, _labelStyle);
        }

        private static string Format(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "null";
        }

        private static string Format(int? value)
        {
            return value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null";
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };
            _warnStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.82f, 0.35f) }
            };
        }
    }
}
