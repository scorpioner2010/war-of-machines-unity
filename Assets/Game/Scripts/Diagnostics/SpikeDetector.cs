using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Diagnostics
{
    public sealed class SpikeDetector
    {
        private readonly DiagnosticsConfig _config;
        private readonly Dictionary<string, double> _lastSpikeTimes = new Dictionary<string, double>(16);

        public SpikeDetector(DiagnosticsConfig config)
        {
            _config = config;
        }

        public List<DiagnosticsSpike> Detect(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer)
        {
            List<DiagnosticsSpike> spikes = new List<DiagnosticsSpike>(4);
            if (sample == null || buffer == null)
            {
                return spikes;
            }

            DetectClientSpikes(sample, buffer, spikes);
            DetectServerSpikes(sample, buffer, spikes);
            DetectNetworkSpikes(sample, buffer, spikes);
            DetectMemoryAndScaleSpikes(sample, buffer, spikes);
            DetectRpcStorm(sample, buffer, spikes);
            return spikes;
        }

        private void DetectClientSpikes(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            DiagnosticsClientMetrics client = sample.Client;
            if (client == null)
            {
                return;
            }

            if (client.FrameMs.HasValue && client.FrameMs.Value > _config.ClientFrameSpikeMs)
            {
                spikes.Add(CreateSpike(sample, buffer, "client_frame_spike", "client", Severity(client.FrameMs.Value, _config.ClientFrameSpikeMs), "Client frame time exceeded threshold."));
                return;
            }

            if (client.Fps.HasValue && client.Fps.Value < _config.ClientLowFps)
            {
                spikes.Add(CreateSpike(sample, buffer, "client_fps_low", "client", Severity(_config.ClientLowFps, client.Fps.Value), "Client FPS dropped below threshold."));
            }
        }

        private void DetectServerSpikes(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            DiagnosticsServerMetrics server = sample.Server;
            if (server == null || !server.ServerTickMs.HasValue)
            {
                return;
            }

            double threshold = _config.ServerTickSpikeMs;
            if (server.TickRate.HasValue && server.TickRate.Value > 0)
            {
                double targetMs = 1000d / server.TickRate.Value;
                threshold = Mathf.Max(_config.ServerTickSpikeMs, (float)(targetMs * _config.ServerTickMultiplier));
            }

            if (server.ServerTickMs.Value > threshold)
            {
                spikes.Add(CreateSpike(sample, buffer, "server_tick_spike", "server", Severity(server.ServerTickMs.Value, threshold), "Server tick time exceeded threshold."));
            }
        }

        private void DetectNetworkSpikes(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            DiagnosticsNetworkMetrics network = sample.Network;
            if (network == null)
            {
                return;
            }

            if (network.PacketLossPercent.HasValue && network.PacketLossPercent.Value > _config.PacketLossSpikePercent)
            {
                spikes.Add(CreateSpike(sample, buffer, "network_packet_loss", "network", Severity(network.PacketLossPercent.Value, _config.PacketLossSpikePercent), "Packet loss exceeded threshold."));
                return;
            }

            if (network.PingMs.HasValue && network.PingMs.Value > _config.PingSpikeMs)
            {
                spikes.Add(CreateSpike(sample, buffer, "network_ping_spike", "network", Severity(network.PingMs.Value, _config.PingSpikeMs), "Ping exceeded threshold."));
                return;
            }

            if (network.JitterMs.HasValue && network.JitterMs.Value > _config.JitterSpikeMs)
            {
                spikes.Add(CreateSpike(sample, buffer, "network_jitter_spike", "network", Severity(network.JitterMs.Value, _config.JitterSpikeMs), "Jitter exceeded threshold."));
            }
        }

        private void DetectMemoryAndScaleSpikes(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            double? clientGrowth = buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Client, 30);
            if (clientGrowth.HasValue && clientGrowth.Value > _config.MemoryGrowthMbPerMinute)
            {
                spikes.Add(CreateSpike(sample, buffer, "client_memory_growth", "client", Severity(clientGrowth.Value, _config.MemoryGrowthMbPerMinute), "Client memory is growing quickly."));
            }

            bool hasServerMetrics = sample.Server != null
                                    && (sample.Server.ServerTickMs.HasValue
                                        || sample.Server.ActivePlayers.HasValue
                                        || sample.Server.ActiveEntities.HasValue);
            double? serverGrowth = hasServerMetrics ? buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Server, 30) : null;
            if (serverGrowth.HasValue && serverGrowth.Value > _config.MemoryGrowthMbPerMinute)
            {
                spikes.Add(CreateSpike(sample, buffer, "server_memory_growth", "server", Severity(serverGrowth.Value, _config.MemoryGrowthMbPerMinute), "Server memory is growing quickly."));
            }

            int? entityGrowth = buffer.GetEntityGrowth(10);
            if (entityGrowth.HasValue && entityGrowth.Value > _config.EntityGrowthSpikeCount)
            {
                spikes.Add(CreateSpike(sample, buffer, "entity_growth_spike", "server", "medium", "Active entity count increased quickly."));
            }
        }

        private void DetectRpcStorm(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            double rpcPerSecond = buffer.GetEventCountPerSecond(DiagnosticsCategories.Rpc, 1);
            bool rpcHigh = rpcPerSecond > _config.RpcStormCountPerSecond;
            bool networkHigh = sample.Network != null
                               && ((sample.Network.IncomingMessagesPerSecond.HasValue && sample.Network.IncomingMessagesPerSecond.Value > _config.NetworkMessageStormPerSecond)
                                   || (sample.Network.OutgoingMessagesPerSecond.HasValue && sample.Network.OutgoingMessagesPerSecond.Value > _config.NetworkMessageStormPerSecond));

            if (rpcHigh || networkHigh)
            {
                spikes.Add(CreateSpike(sample, buffer, "rpc_or_network_storm", "network", rpcHigh ? "high" : "medium", "RPC or network message rate is unusually high."));
            }
        }

        private DiagnosticsSpike CreateSpike(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, string type, string domain, string severity, string summary)
        {
            if (!ShouldEmit(type, sample.TimeSeconds))
            {
                return null;
            }

            List<DiagnosticsScopeSummary> topSlow = buffer.GetTopScopes(domain == "client" ? DiagnosticsCategories.Client : DiagnosticsCategories.Server, 5, 5);
            List<DiagnosticsScopeSummary> topRpc = buffer.GetTopEvents(DiagnosticsCategories.Rpc, 5, 5, TopSortMode.Count);
            DiagnosticsScopeSummary suspect = topSlow.Count > 0 ? topSlow[0] : null;

            return new DiagnosticsSpike
            {
                TimeSeconds = sample.TimeSeconds,
                Timestamp = sample.Timestamp,
                Type = type,
                Domain = domain,
                Severity = severity,
                Summary = summary,
                TopSuspect = suspect != null ? suspect.Name : null,
                FrameMs = sample.Client != null ? sample.Client.FrameMs : null,
                ServerTickMs = sample.Server != null ? sample.Server.ServerTickMs : null,
                PingMs = sample.Network != null ? sample.Network.PingMs : null,
                JitterMs = sample.Network != null ? sample.Network.JitterMs : null,
                PacketLossPercent = sample.Network != null ? sample.Network.PacketLossPercent : null,
                ActivePlayers = sample.Server != null ? sample.Server.ActivePlayers : null,
                ActiveEntities = sample.Server != null ? sample.Server.ActiveEntities : null,
                Map = sample.Map,
                Mode = sample.Mode,
                TopSlowScopes5s = topSlow,
                TopRpcEvents5s = topRpc
            };
        }

        private bool ShouldEmit(string type, double now)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            double cooldownSeconds = type.Contains("memory")
                ? Mathf.Max(30f, _config.SpikeCooldownSeconds)
                : _config.SpikeCooldownSeconds;
            if (_lastSpikeTimes.TryGetValue(type, out double lastTime) && now - lastTime < cooldownSeconds)
            {
                return false;
            }

            _lastSpikeTimes[type] = now;
            return true;
        }

        private static string Severity(double value, double threshold)
        {
            if (threshold <= 0d)
            {
                return "medium";
            }

            double ratio = value / threshold;
            if (ratio >= 2d)
            {
                return "critical";
            }

            if (ratio >= 1.35d)
            {
                return "high";
            }

            return "medium";
        }
    }
}
