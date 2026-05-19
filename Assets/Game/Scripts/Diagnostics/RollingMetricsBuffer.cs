using System;
using System.Collections.Generic;

namespace Game.Scripts.Diagnostics
{
    public sealed class RollingMetricsBuffer
    {
        private readonly object _sync = new object();
        private readonly List<DiagnosticsMetricSample> _samples = new List<DiagnosticsMetricSample>(256);
        private readonly List<DiagnosticsScopeSample> _scopeSamples = new List<DiagnosticsScopeSample>(4096);
        private readonly List<DiagnosticsEventSample> _eventSamples = new List<DiagnosticsEventSample>(1024);
        private readonly List<DiagnosticsSpike> _spikes = new List<DiagnosticsSpike>(64);
        private readonly List<DiagnosticsFrameSpike> _frameSpikes = new List<DiagnosticsFrameSpike>(128);
        private readonly int _bufferSeconds;
        private readonly int _maxScopeSamples;

        private DiagnosticsMetricSample _current;

        public RollingMetricsBuffer(int bufferSeconds, int maxScopeSamples)
        {
            _bufferSeconds = Math.Max(10, bufferSeconds);
            _maxScopeSamples = Math.Max(1024, maxScopeSamples);
        }

        public void AddSample(DiagnosticsMetricSample sample)
        {
            if (sample == null)
            {
                return;
            }

            lock (_sync)
            {
                _current = sample;
                _samples.Add(sample);
                Prune(sample.TimeSeconds);
            }
        }

        public void AddScopeSample(DiagnosticsScopeSample sample)
        {
            if (string.IsNullOrEmpty(sample.Name))
            {
                return;
            }

            lock (_sync)
            {
                _scopeSamples.Add(sample);
                TrimScopeSamplesByCapacity();
                RemoveOldScopeSamples(sample.TimeSeconds - _bufferSeconds);
            }
        }

        public void AddEventSample(DiagnosticsEventSample sample)
        {
            if (string.IsNullOrEmpty(sample.Name) || sample.Count <= 0)
            {
                return;
            }

            lock (_sync)
            {
                _eventSamples.Add(sample);
                Prune(sample.TimeSeconds);
            }
        }

        public void AddSpike(DiagnosticsSpike spike)
        {
            if (spike == null)
            {
                return;
            }

            lock (_sync)
            {
                _spikes.Add(spike);
                Prune(spike.TimeSeconds);
            }
        }

        public void AddFrameSpike(DiagnosticsFrameSpike spike)
        {
            if (spike == null)
            {
                return;
            }

            lock (_sync)
            {
                _frameSpikes.Add(spike);
                if (_frameSpikes.Count > 512)
                {
                    _frameSpikes.RemoveRange(0, _frameSpikes.Count - 512);
                }

                Prune(spike.TimeSeconds);
            }
        }

        public DiagnosticsSnapshot GetCurrentSnapshot(int spikeSeconds)
        {
            lock (_sync)
            {
                DiagnosticsSnapshot snapshot = new DiagnosticsSnapshot
                {
                    Current = _current
                };

                CopySpikesNoLock(snapshot.Spikes, GetNowNoLock(), spikeSeconds);
                return snapshot;
            }
        }

        public List<DiagnosticsMetricSample> GetSamples(int seconds)
        {
            List<DiagnosticsMetricSample> results = new List<DiagnosticsMetricSample>();
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                for (int i = 0; i < _samples.Count; i++)
                {
                    DiagnosticsMetricSample sample = _samples[i];
                    if (sample != null && sample.TimeSeconds >= cutoff)
                    {
                        results.Add(sample);
                    }
                }
            }

            return results;
        }

        public List<DiagnosticsSpike> GetSpikes(int seconds)
        {
            List<DiagnosticsSpike> results = new List<DiagnosticsSpike>();
            lock (_sync)
            {
                CopySpikesNoLock(results, GetNowNoLock(), seconds);
            }

            return results;
        }

        public List<DiagnosticsFrameSpike> GetFrameSpikes(int seconds)
        {
            List<DiagnosticsFrameSpike> results = new List<DiagnosticsFrameSpike>();
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                for (int i = 0; i < _frameSpikes.Count; i++)
                {
                    DiagnosticsFrameSpike spike = _frameSpikes[i];
                    if (spike != null && spike.TimeSeconds >= cutoff)
                    {
                        results.Add(spike);
                    }
                }
            }

            return results;
        }

        public List<DiagnosticsScopeSummary> GetTopScopes(string group, int seconds, int maxCount)
        {
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                return BuildTopScopesNoLock(group, cutoff, maxCount, TopSortMode.TotalMs);
            }
        }

        public List<DiagnosticsScopeSummary> GetTopAllocatingScopes(string group, int seconds, int maxCount)
        {
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                return BuildTopScopesNoLock(group, cutoff, maxCount, TopSortMode.TotalAllocatedBytes);
            }
        }

        public List<DiagnosticsScopeSummary> GetTopEvents(string group, int seconds, int maxCount, TopSortMode sortMode)
        {
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                return BuildTopEventsNoLock(group, cutoff, maxCount, sortMode);
            }
        }

        public double? SumScopeMs(string group, int seconds)
        {
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                double total = 0d;
                bool hasAny = false;
                for (int i = 0; i < _scopeSamples.Count; i++)
                {
                    DiagnosticsScopeSample sample = _scopeSamples[i];
                    if (sample.TimeSeconds < cutoff || !MatchesGroup(sample.Name, sample.Category, group))
                    {
                        continue;
                    }

                    total += sample.DurationMs;
                    hasAny = true;
                }

                return hasAny ? total : null;
            }
        }

        public double? SumScopeMsByPrefix(string prefix, int seconds)
        {
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - Math.Max(1, seconds);
                double total = 0d;
                bool hasAny = false;
                for (int i = 0; i < _scopeSamples.Count; i++)
                {
                    DiagnosticsScopeSample sample = _scopeSamples[i];
                    if (sample.TimeSeconds < cutoff || string.IsNullOrEmpty(sample.Name) || !sample.Name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    total += sample.DurationMs;
                    hasAny = true;
                }

                return hasAny ? total : null;
            }
        }

        public double GetEventCountPerSecond(string group, int seconds)
        {
            int window = Math.Max(1, seconds);
            lock (_sync)
            {
                double cutoff = GetNowNoLock() - window;
                int count = 0;
                for (int i = 0; i < _eventSamples.Count; i++)
                {
                    DiagnosticsEventSample sample = _eventSamples[i];
                    if (sample.TimeSeconds < cutoff || !MatchesGroup(sample.Name, sample.Category, group))
                    {
                        continue;
                    }

                    count += sample.Count;
                }

                return count / (double)window;
            }
        }

        public DiagnosticsNetworkSummary GetNetworkSummary(int seconds)
        {
            lock (_sync)
            {
                int window = Math.Max(1, seconds);
                double cutoff = GetNowNoLock() - window;
                DiagnosticsNetworkSummary summary = new DiagnosticsNetworkSummary
                {
                    Seconds = window,
                    Current = _current != null ? _current.Network : null
                };

                int count = 0;
                double inMessages = 0d;
                double outMessages = 0d;
                double inKbps = 0d;
                double outKbps = 0d;
                for (int i = 0; i < _samples.Count; i++)
                {
                    DiagnosticsMetricSample sample = _samples[i];
                    if (sample == null || sample.TimeSeconds < cutoff || sample.Network == null)
                    {
                        continue;
                    }

                    DiagnosticsNetworkMetrics network = sample.Network;
                    count++;
                    AddAverageValue(network.IncomingMessagesPerSecond, ref inMessages);
                    AddAverageValue(network.OutgoingMessagesPerSecond, ref outMessages);
                    AddAverageValue(network.IncomingKbps, ref inKbps);
                    AddAverageValue(network.OutgoingKbps, ref outKbps);
                    summary.MaxIncomingMessagesPerSecond = MaxNullable(summary.MaxIncomingMessagesPerSecond, network.IncomingMessagesPerSecond);
                    summary.MaxOutgoingMessagesPerSecond = MaxNullable(summary.MaxOutgoingMessagesPerSecond, network.OutgoingMessagesPerSecond);
                    summary.MaxPingMs = MaxNullable(summary.MaxPingMs, network.PingMs);
                    summary.MaxJitterMs = MaxNullable(summary.MaxJitterMs, network.JitterMs);
                    summary.MaxPacketLossPercent = MaxNullable(summary.MaxPacketLossPercent, network.PacketLossPercent);
                }

                if (count > 0)
                {
                    summary.AverageIncomingMessagesPerSecond = inMessages / count;
                    summary.AverageOutgoingMessagesPerSecond = outMessages / count;
                    summary.AverageIncomingKbps = inKbps / count;
                    summary.AverageOutgoingKbps = outKbps / count;
                }

                if (_current != null && _current.Network != null)
                {
                    summary.IncomingBytesTotal = _current.Network.IncomingBytesTotal;
                    summary.OutgoingBytesTotal = _current.Network.OutgoingBytesTotal;
                }

                if (_current != null && _current.Server != null && _current.Server.NetworkBytesPerClient != null)
                {
                    for (int i = 0; i < _current.Server.NetworkBytesPerClient.Count; i++)
                    {
                        summary.PerClient.Add(_current.Server.NetworkBytesPerClient[i]);
                    }
                }

                return summary;
            }
        }

        public double? GetMemoryGrowthMbPerMinute(string side, int seconds)
        {
            lock (_sync)
            {
                if (_samples.Count < 2)
                {
                    return null;
                }

                double now = GetNowNoLock();
                double cutoff = now - Math.Max(2, seconds);
                DiagnosticsMetricSample first = null;
                DiagnosticsMetricSample last = null;
                for (int i = 0; i < _samples.Count; i++)
                {
                    DiagnosticsMetricSample sample = _samples[i];
                    if (sample == null || sample.TimeSeconds < cutoff)
                    {
                        continue;
                    }

                    if (first == null)
                    {
                        first = sample;
                    }

                    last = sample;
                }

                if (first == null || last == null || first == last)
                {
                    return null;
                }

                double elapsed = Math.Max(0.001d, last.TimeSeconds - first.TimeSeconds);
                double minimumElapsed = Math.Max(20d, seconds * 0.95d);
                if (elapsed < minimumElapsed)
                {
                    return null;
                }

                double? firstMemory = side == DiagnosticsCategories.Server
                    ? first.Server != null ? first.Server.MemoryMb : null
                    : first.Client != null ? first.Client.MemoryMb : null;
                double? lastMemory = side == DiagnosticsCategories.Server
                    ? last.Server != null ? last.Server.MemoryMb : null
                    : last.Client != null ? last.Client.MemoryMb : null;
                if (!firstMemory.HasValue || !lastMemory.HasValue)
                {
                    return null;
                }

                return (lastMemory.Value - firstMemory.Value) / elapsed * 60d;
            }
        }

        public int? GetEntityGrowth(int seconds)
        {
            lock (_sync)
            {
                if (_samples.Count < 2)
                {
                    return null;
                }

                double cutoff = GetNowNoLock() - Math.Max(2, seconds);
                DiagnosticsMetricSample first = null;
                DiagnosticsMetricSample last = null;
                for (int i = 0; i < _samples.Count; i++)
                {
                    DiagnosticsMetricSample sample = _samples[i];
                    if (sample == null || sample.TimeSeconds < cutoff)
                    {
                        continue;
                    }

                    if (first == null)
                    {
                        first = sample;
                    }

                    last = sample;
                }

                if (first == null || last == null || first == last || !first.Server.ActiveEntities.HasValue || !last.Server.ActiveEntities.HasValue)
                {
                    return null;
                }

                return last.Server.ActiveEntities.Value - first.Server.ActiveEntities.Value;
            }
        }

        private List<DiagnosticsScopeSummary> BuildTopScopesNoLock(string group, double cutoff, int maxCount, TopSortMode sortMode)
        {
            Dictionary<string, ScopeAccumulator> accumulators = new Dictionary<string, ScopeAccumulator>(64);
            for (int i = 0; i < _scopeSamples.Count; i++)
            {
                DiagnosticsScopeSample sample = _scopeSamples[i];
                if (sample.TimeSeconds < cutoff || !MatchesGroup(sample.Name, sample.Category, group))
                {
                    continue;
                }

                if (!accumulators.TryGetValue(sample.Name, out ScopeAccumulator accumulator))
                {
                    accumulator = new ScopeAccumulator(sample.Name, sample.Category);
                    accumulators[sample.Name] = accumulator;
                }

                accumulator.Add(sample.DurationMs, sample.AllocatedBytes);
            }

            return BuildSortedSummaries(accumulators, maxCount, sortMode);
        }

        private List<DiagnosticsScopeSummary> BuildTopEventsNoLock(string group, double cutoff, int maxCount, TopSortMode sortMode)
        {
            Dictionary<string, ScopeAccumulator> accumulators = new Dictionary<string, ScopeAccumulator>(64);
            for (int i = 0; i < _eventSamples.Count; i++)
            {
                DiagnosticsEventSample sample = _eventSamples[i];
                if (sample.TimeSeconds < cutoff || !MatchesGroup(sample.Name, sample.Category, group))
                {
                    continue;
                }

                if (!accumulators.TryGetValue(sample.Name, out ScopeAccumulator accumulator))
                {
                    accumulator = new ScopeAccumulator(sample.Name, sample.Category);
                    accumulators[sample.Name] = accumulator;
                }

                accumulator.AddEvent(sample.Count, sample.TotalMs);
            }

            return BuildSortedSummaries(accumulators, maxCount, sortMode);
        }

        private static List<DiagnosticsScopeSummary> BuildSortedSummaries(Dictionary<string, ScopeAccumulator> accumulators, int maxCount, TopSortMode sortMode)
        {
            List<DiagnosticsScopeSummary> summaries = new List<DiagnosticsScopeSummary>(accumulators.Count);
            foreach (ScopeAccumulator accumulator in accumulators.Values)
            {
                summaries.Add(accumulator.ToSummary());
            }

            summaries.Sort((left, right) =>
            {
                double leftValue = GetSortValue(left, sortMode);
                double rightValue = GetSortValue(right, sortMode);
                return rightValue.CompareTo(leftValue);
            });

            int limit = Math.Max(0, maxCount);
            if (limit > 0 && summaries.Count > limit)
            {
                summaries.RemoveRange(limit, summaries.Count - limit);
            }

            return summaries;
        }

        private static double GetSortValue(DiagnosticsScopeSummary summary, TopSortMode sortMode)
        {
            if (summary == null)
            {
                return 0d;
            }

            if (sortMode == TopSortMode.Count)
            {
                return summary.Count;
            }

            if (sortMode == TopSortMode.AvgMs)
            {
                return summary.AvgMs;
            }

            if (sortMode == TopSortMode.MaxMs)
            {
                return summary.MaxMs;
            }

            if (sortMode == TopSortMode.TotalAllocatedBytes)
            {
                return summary.TotalAllocatedBytes;
            }

            return summary.TotalMs;
        }

        private static bool MatchesGroup(string name, string category, string group)
        {
            if (string.IsNullOrEmpty(group))
            {
                return true;
            }

            if (group == category)
            {
                return true;
            }

            if (group == DiagnosticsCategories.Client)
            {
                return category == DiagnosticsCategories.Ui
                       || category == DiagnosticsCategories.Render
                       || category == DiagnosticsCategories.Editor
                       || StartsWith(name, "Client.");
            }

            if (group == DiagnosticsCategories.Editor)
            {
                return category == DiagnosticsCategories.Editor
                       || StartsWith(name, "Editor.")
                       || StartsWith(name, "DebugOverlay.")
                       || StartsWith(name, "Gizmos.")
                       || StartsWith(name, "OnGUI.");
            }

            if (group == DiagnosticsCategories.Server)
            {
                return category == DiagnosticsCategories.Physics
                       || category == DiagnosticsCategories.Ai
                       || category == DiagnosticsCategories.Rpc
                       || StartsWith(name, "Server.")
                       || StartsWith(name, "RPC.")
                       || StartsWith(name, "Network.");
            }

            return false;
        }

        private static bool StartsWith(string value, string prefix)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(prefix, StringComparison.Ordinal);
        }

        private static void AddAverageValue(double? value, ref double total)
        {
            if (value.HasValue)
            {
                total += value.Value;
            }
        }

        private static double? MaxNullable(double? current, double? value)
        {
            if (!value.HasValue)
            {
                return current;
            }

            if (!current.HasValue || value.Value > current.Value)
            {
                return value.Value;
            }

            return current;
        }

        private void CopySpikesNoLock(List<DiagnosticsSpike> results, double now, int seconds)
        {
            double cutoff = now - Math.Max(1, seconds);
            for (int i = 0; i < _spikes.Count; i++)
            {
                DiagnosticsSpike spike = _spikes[i];
                if (spike != null && spike.TimeSeconds >= cutoff)
                {
                    results.Add(spike);
                }
            }
        }

        private double GetNowNoLock()
        {
            if (_current != null)
            {
                return _current.TimeSeconds;
            }

            if (_samples.Count > 0)
            {
                return _samples[_samples.Count - 1].TimeSeconds;
            }

            return 0d;
        }

        private void Prune(double now)
        {
            double cutoff = now - _bufferSeconds;
            RemoveOldSamples(_samples, cutoff);
            RemoveOldScopeSamples(cutoff);
            RemoveOldEventSamples(cutoff);
            RemoveOldSpikes(cutoff);
            RemoveOldFrameSpikes(cutoff);
        }

        private static void RemoveOldSamples(List<DiagnosticsMetricSample> list, double cutoff)
        {
            int removeCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                DiagnosticsMetricSample item = list[i];
                if (item == null || item.TimeSeconds < cutoff)
                {
                    removeCount++;
                    continue;
                }

                break;
            }

            if (removeCount > 0)
            {
                list.RemoveRange(0, removeCount);
            }
        }

        private void RemoveOldScopeSamples(double cutoff)
        {
            int removeCount = 0;
            for (int i = 0; i < _scopeSamples.Count; i++)
            {
                if (_scopeSamples[i].TimeSeconds < cutoff)
                {
                    removeCount++;
                    continue;
                }

                break;
            }

            if (removeCount > 0)
            {
                _scopeSamples.RemoveRange(0, removeCount);
            }
        }

        private void TrimScopeSamplesByCapacity()
        {
            if (_scopeSamples.Count <= _maxScopeSamples)
            {
                return;
            }

            int batchSize = Math.Max(256, _maxScopeSamples / 10);
            int targetCount = Math.Max(1024, _maxScopeSamples - batchSize);
            int removeCount = _scopeSamples.Count - targetCount;
            if (removeCount <= 0)
            {
                return;
            }

            _scopeSamples.RemoveRange(0, removeCount);
        }

        private void RemoveOldEventSamples(double cutoff)
        {
            int removeCount = 0;
            for (int i = 0; i < _eventSamples.Count; i++)
            {
                if (_eventSamples[i].TimeSeconds < cutoff)
                {
                    removeCount++;
                    continue;
                }

                break;
            }

            if (removeCount > 0)
            {
                _eventSamples.RemoveRange(0, removeCount);
            }
        }

        private void RemoveOldSpikes(double cutoff)
        {
            int removeCount = 0;
            for (int i = 0; i < _spikes.Count; i++)
            {
                DiagnosticsSpike spike = _spikes[i];
                if (spike == null || spike.TimeSeconds < cutoff)
                {
                    removeCount++;
                    continue;
                }

                break;
            }

            if (removeCount > 0)
            {
                _spikes.RemoveRange(0, removeCount);
            }
        }

        private void RemoveOldFrameSpikes(double cutoff)
        {
            int removeCount = 0;
            for (int i = 0; i < _frameSpikes.Count; i++)
            {
                DiagnosticsFrameSpike spike = _frameSpikes[i];
                if (spike == null || spike.TimeSeconds < cutoff)
                {
                    removeCount++;
                    continue;
                }

                break;
            }

            if (removeCount > 0)
            {
                _frameSpikes.RemoveRange(0, removeCount);
            }
        }

        private sealed class ScopeAccumulator
        {
            public readonly string Name;
            public readonly string Category;
            public int Count;
            public double TotalMs;
            public double MaxMs;
            public long TotalAllocatedBytes;
            public long MaxAllocatedBytes;

            public ScopeAccumulator(string name, string category)
            {
                Name = name;
                Category = string.IsNullOrEmpty(category) ? DiagnosticsCategories.Unknown : category;
            }

            public void Add(double durationMs, long allocatedBytes)
            {
                Count++;
                TotalMs += durationMs;
                if (durationMs > MaxMs)
                {
                    MaxMs = durationMs;
                }

                if (allocatedBytes > 0)
                {
                    TotalAllocatedBytes += allocatedBytes;
                    if (allocatedBytes > MaxAllocatedBytes)
                    {
                        MaxAllocatedBytes = allocatedBytes;
                    }
                }
            }

            public void AddEvent(int count, double totalMs)
            {
                Count += count;
                TotalMs += totalMs;
                double avg = count > 0 ? totalMs / count : 0d;
                if (avg > MaxMs)
                {
                    MaxMs = avg;
                }
            }

            public DiagnosticsScopeSummary ToSummary()
            {
                double avg = Count > 0 ? TotalMs / Count : 0d;
                long avgAllocatedBytes = Count > 0 ? TotalAllocatedBytes / Count : 0;
                return new DiagnosticsScopeSummary
                {
                    Name = Name,
                    Category = Category,
                    Count = Count,
                    TotalMs = TotalMs,
                    AvgMs = avg,
                    MaxMs = MaxMs,
                    P95Ms = MaxMs,
                    TotalAllocatedBytes = TotalAllocatedBytes,
                    AvgAllocatedBytes = avgAllocatedBytes,
                    MaxAllocatedBytes = MaxAllocatedBytes
                };
            }
        }
    }

    public enum TopSortMode
    {
        TotalMs,
        Count,
        AvgMs,
        MaxMs,
        TotalAllocatedBytes
    }
}
