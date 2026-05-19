using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Transporting;

namespace Game.Scripts.Diagnostics
{
    public sealed class NetworkDiagnosticsCollector
    {
        private readonly object _sync = new object();
        private readonly Dictionary<int, ClientNetworkCounters> _perClient = new Dictionary<int, ClientNetworkCounters>(16);

        private Transport _transport;
        private NetworkManager _networkManager;
        private bool _subscribed;
        private long _incomingMessagesTotal;
        private long _outgoingMessagesTotal;
        private long _incomingBytesTotal;
        private long _outgoingBytesTotal;
        private long _lastIncomingMessages;
        private long _lastOutgoingMessages;
        private long _lastIncomingBytes;
        private long _lastOutgoingBytes;
        private double _lastSampleTime;
        private double? _lastPingMs;
        private readonly double[] _recentPingDeltas = new double[16];
        private int _recentPingDeltaIndex;
        private int _recentPingDeltaCount;

        public void Resolve(NetworkManager networkManager)
        {
            if (networkManager == _networkManager && networkManager != null && networkManager.TransportManager != null && networkManager.TransportManager.Transport == _transport)
            {
                return;
            }

            Unsubscribe();
            _networkManager = networkManager;
            if (_networkManager == null || _networkManager.TransportManager == null)
            {
                return;
            }

            _transport = _networkManager.TransportManager.Transport;
            if (_transport == null)
            {
                return;
            }

            _transport.OnClientReceivedData += OnClientReceivedData;
            _transport.OnServerReceivedData += OnServerReceivedData;
            _subscribed = true;
        }

        public void Unsubscribe()
        {
            if (!_subscribed || _transport == null)
            {
                _subscribed = false;
                _transport = null;
                _networkManager = null;
                return;
            }

            _transport.OnClientReceivedData -= OnClientReceivedData;
            _transport.OnServerReceivedData -= OnServerReceivedData;
            _subscribed = false;
            _transport = null;
            _networkManager = null;
        }

        public void RecordOutgoing(int bytes, int connectionId)
        {
            if (bytes < 0)
            {
                bytes = 0;
            }

            lock (_sync)
            {
                _outgoingMessagesTotal++;
                _outgoingBytesTotal += bytes;
                if (connectionId >= 0)
                {
                    ClientNetworkCounters counters = GetCounters(connectionId);
                    counters.BytesOutTotal += bytes;
                }
            }
        }

        public DiagnosticsNetworkMetrics Collect(NetworkManager networkManager)
        {
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            double elapsed = _lastSampleTime > 0d ? Math.Max(0.001d, now - _lastSampleTime) : 1d;
            DiagnosticsNetworkMetrics metrics = new DiagnosticsNetworkMetrics();

            lock (_sync)
            {
                metrics.IncomingMessagesPerSecond = (_incomingMessagesTotal - _lastIncomingMessages) / elapsed;
                metrics.OutgoingMessagesPerSecond = (_outgoingMessagesTotal - _lastOutgoingMessages) / elapsed;
                metrics.IncomingBytesPerSecond = (_incomingBytesTotal - _lastIncomingBytes) / elapsed;
                metrics.OutgoingBytesPerSecond = (_outgoingBytesTotal - _lastOutgoingBytes) / elapsed;
                metrics.IncomingKbps = metrics.IncomingBytesPerSecond * 8d / 1000d;
                metrics.OutgoingKbps = metrics.OutgoingBytesPerSecond * 8d / 1000d;
                metrics.IncomingBytesTotal = _incomingBytesTotal;
                metrics.OutgoingBytesTotal = _outgoingBytesTotal;

                _lastIncomingMessages = _incomingMessagesTotal;
                _lastOutgoingMessages = _outgoingMessagesTotal;
                _lastIncomingBytes = _incomingBytesTotal;
                _lastOutgoingBytes = _outgoingBytesTotal;
            }

            _lastSampleTime = now;
            if (networkManager != null && networkManager.TimeManager != null)
            {
                metrics.PingMs = networkManager.TimeManager.RoundTripTime;
                UpdateJitter(metrics.PingMs.Value);
                metrics.JitterMs = CalculateJitter();
            }

            if (networkManager != null && networkManager.TransportManager != null && networkManager.TransportManager.Transport != null)
            {
                bool asServer = networkManager.IsServerStarted && !networkManager.IsClientStarted;
                metrics.PacketLossPercent = networkManager.TransportManager.Transport.GetPacketLoss(asServer);
            }

            metrics.PendingPackets = null;
            return metrics;
        }

        public void CopyPerClientMetrics(List<DiagnosticsPerClientNetworkMetrics> target)
        {
            if (target == null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (KeyValuePair<int, ClientNetworkCounters> pair in _perClient)
                {
                    target.Add(new DiagnosticsPerClientNetworkMetrics
                    {
                        ClientIdHash = HashClientId(pair.Key),
                        BytesInTotal = pair.Value.BytesInTotal,
                        BytesOutTotal = pair.Value.BytesOutTotal
                    });
                }
            }
        }

        private void OnClientReceivedData(ClientReceivedDataArgs args)
        {
            int count = args.Data.Count;
            lock (_sync)
            {
                _incomingMessagesTotal++;
                _incomingBytesTotal += count;
            }
        }

        private void OnServerReceivedData(ServerReceivedDataArgs args)
        {
            int count = args.Data.Count;
            lock (_sync)
            {
                _incomingMessagesTotal++;
                _incomingBytesTotal += count;
                ClientNetworkCounters counters = GetCounters(args.ConnectionId);
                counters.BytesInTotal += count;
            }
        }

        private ClientNetworkCounters GetCounters(int connectionId)
        {
            if (!_perClient.TryGetValue(connectionId, out ClientNetworkCounters counters))
            {
                counters = new ClientNetworkCounters();
                _perClient[connectionId] = counters;
            }

            return counters;
        }

        private void UpdateJitter(double pingMs)
        {
            if (_lastPingMs.HasValue)
            {
                _recentPingDeltas[_recentPingDeltaIndex] = Math.Abs(pingMs - _lastPingMs.Value);
                _recentPingDeltaIndex = (_recentPingDeltaIndex + 1) % _recentPingDeltas.Length;
                if (_recentPingDeltaCount < _recentPingDeltas.Length)
                {
                    _recentPingDeltaCount++;
                }
            }

            _lastPingMs = pingMs;
        }

        private double? CalculateJitter()
        {
            if (_recentPingDeltaCount == 0)
            {
                return null;
            }

            double total = 0d;
            for (int i = 0; i < _recentPingDeltaCount; i++)
            {
                total += _recentPingDeltas[i];
            }

            return total / _recentPingDeltaCount;
        }

        private static string HashClientId(int clientId)
        {
            unchecked
            {
                int hash = (clientId * 1103515245) + 12345;
                if (hash < 0)
                {
                    hash = -hash;
                }

                return "client-" + (hash % 100000).ToString("00000");
            }
        }

        private sealed class ClientNetworkCounters
        {
            public long BytesInTotal;
            public long BytesOutTotal;
        }
    }
}
