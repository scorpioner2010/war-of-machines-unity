using System;
using FishNet.Managing;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.Profiling;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.Diagnostics
{
    public sealed class ServerDiagnosticsCollector
    {
        private const int TickBufferCapacity = 1200;

        private readonly double[] _tickTimesMs = new double[TickBufferCapacity];
        private readonly double[] _tickTimeSeconds = new double[TickBufferCapacity];
        private readonly double[] _scratch = new double[TickBufferCapacity];
        private int _tickIndex;
        private int _tickCount;
        private double? _lastTickMs;
        private int _lastGcCollectionCount;

        public void RecordServerTick(double nowSeconds, double durationMs)
        {
            _lastTickMs = durationMs;
            _tickTimesMs[_tickIndex] = durationMs;
            _tickTimeSeconds[_tickIndex] = nowSeconds;
            _tickIndex = (_tickIndex + 1) % TickBufferCapacity;
            if (_tickCount < TickBufferCapacity)
            {
                _tickCount++;
            }
        }

        public DiagnosticsServerMetrics Collect(
            NetworkManager networkManager,
            RollingMetricsBuffer buffer,
            NetworkDiagnosticsCollector networkCollector,
            DiagnosticsNetworkMetrics networkMetrics)
        {
            DiagnosticsServerMetrics metrics = new DiagnosticsServerMetrics();
            metrics.ServerTickMs = _lastTickMs;
            metrics.ServerTickMsP95_10s = CalculatePercentile(10d, 0.95d);
            metrics.ServerTickMsMax_10s = CalculateMax(10d);

            if (networkManager != null && networkManager.TimeManager != null)
            {
                metrics.TickRate = networkManager.TimeManager.TickRate;
            }

            if (networkManager != null && networkManager.IsServerStarted && networkManager.ServerManager != null)
            {
                metrics.ActivePlayers = networkManager.ServerManager.Clients != null ? networkManager.ServerManager.Clients.Count : 0;
                metrics.ActiveEntities = networkManager.ServerManager.Objects != null ? networkManager.ServerManager.Objects.Spawned.Count : 0;
            }

            metrics.ActiveProjectiles = DiagnosticsRuntimeCounters.ActiveProjectiles;
            metrics.ActiveNPCs = CountActiveBots();
            metrics.PhysicsStepMs = buffer.SumScopeMs(DiagnosticsCategories.Physics, 1);
            metrics.AiPathfindingMs = buffer.SumScopeMs(DiagnosticsCategories.Ai, 1);
            metrics.VisibilityCalculationMs = buffer.SumScopeMsByPrefix("Server.Visibility.", 1);
            metrics.SnapshotSendMs = buffer.SumScopeMsByPrefix("Network.SendMapVisibility", 1);
            metrics.RpcEventsPerSecond = buffer.GetEventCountPerSecond(DiagnosticsCategories.Rpc, 1);
            metrics.MemoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024d * 1024d);
            metrics.GcCollectionCount = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

            int collectionDelta = metrics.GcCollectionCount.Value - _lastGcCollectionCount;
            _lastGcCollectionCount = metrics.GcCollectionCount.Value;
            if (collectionDelta <= 0)
            {
                metrics.GcAllocatedBytesPerSecond = null;
            }

            if (networkMetrics != null)
            {
                metrics.NetworkBytesInTotal = networkMetrics.IncomingBytesTotal;
                metrics.NetworkBytesOutTotal = networkMetrics.OutgoingBytesTotal;
                metrics.PendingPackets = networkMetrics.PendingPackets;
            }

            if (networkCollector != null)
            {
                networkCollector.CopyPerClientMetrics(metrics.NetworkBytesPerClient);
            }

            // Top scope/RPC summaries are intentionally built on demand by CLI/API commands.
            // Keeping this periodic sample allocation-free avoids diagnostics-induced GC spikes.
            return metrics;
        }

        private static int? CountActiveBots()
        {
            if (LobbyRooms.Rooms == null)
            {
                return null;
            }

            int count = 0;
            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                System.Collections.Generic.List<LobbyPlayer> players = room.GetPlayers();
                if (players == null)
                {
                    continue;
                }

                for (int i = 0; i < players.Count; i++)
                {
                    LobbyPlayer player = players[i];
                    if (player != null && player.isBot && !player.leftBattle)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private double? CalculateMax(double seconds)
        {
            if (_tickCount == 0)
            {
                return null;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double cutoff = now - seconds;
            bool hasAny = false;
            double max = 0d;
            for (int i = 0; i < _tickCount; i++)
            {
                if (_tickTimeSeconds[i] < cutoff)
                {
                    continue;
                }

                double value = _tickTimesMs[i];
                if (!hasAny || value > max)
                {
                    max = value;
                    hasAny = true;
                }
            }

            return hasAny ? max : null;
        }

        private double? CalculatePercentile(double seconds, double percentile)
        {
            if (_tickCount == 0)
            {
                return null;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double cutoff = now - seconds;
            int count = 0;
            for (int i = 0; i < _tickCount; i++)
            {
                if (_tickTimeSeconds[i] < cutoff)
                {
                    continue;
                }

                _scratch[count] = _tickTimesMs[i];
                count++;
            }

            if (count == 0)
            {
                return null;
            }

            Array.Sort(_scratch, 0, count);
            int index = (int)Math.Ceiling(count * percentile) - 1;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= count)
            {
                index = count - 1;
            }

            return _scratch[index];
        }
    }
}
