using System;
using FishNet.Managing;
using Game.Scripts.UI.HUD;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Diagnostics
{
    public sealed class ClientDiagnosticsCollector : IDisposable
    {
        private const int FrameBufferCapacity = 1800;

        private readonly double[] _frameTimesMs = new double[FrameBufferCapacity];
        private readonly double[] _frameTimeSeconds = new double[FrameBufferCapacity];
        private readonly double[] _scratch = new double[FrameBufferCapacity];
        private readonly UnityFrameProfilerRecorder _frameProfiler;
        private int _frameIndex;
        private int _frameCount;
        private int _lastGcCollectionCount;
        private int _lastFrameGcCollectionCount = -1;

        public ClientDiagnosticsCollector(DiagnosticsConfig config)
        {
            _frameProfiler = new UnityFrameProfilerRecorder(config);
        }

        public DiagnosticsFrameSpike RecordFrame(double nowSeconds, float deltaTime, double spikeThresholdMs, RollingMetricsBuffer buffer)
        {
            if (deltaTime <= 0f)
            {
                return null;
            }

            double frameMs = deltaTime * 1000d;
            _frameTimesMs[_frameIndex] = frameMs;
            _frameTimeSeconds[_frameIndex] = nowSeconds;
            _frameIndex = (_frameIndex + 1) % FrameBufferCapacity;
            if (_frameCount < FrameBufferCapacity)
            {
                _frameCount++;
            }

            int gcAfter = GetGcCollectionCount();
            int gcBefore = _lastFrameGcCollectionCount >= 0 ? _lastFrameGcCollectionCount : gcAfter;
            _lastFrameGcCollectionCount = gcAfter;

            if (frameMs <= spikeThresholdMs)
            {
                return null;
            }

            DiagnosticsClientMetrics profilerMetrics = new DiagnosticsClientMetrics();
            _frameProfiler.Collect(profilerMetrics);
            return new DiagnosticsFrameSpike
            {
                TimeSeconds = nowSeconds,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture),
                FrameMs = frameMs,
                ApplicationFocused = Application.isFocused,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                FullscreenMode = Screen.fullScreenMode.ToString(),
                GcCollectionCountBefore = gcBefore,
                GcCollectionCountAfter = gcAfter,
                GcAllocatedBytesInFrame = profilerMetrics.GcAllocatedBytesInFrame,
                MainThreadMs = profilerMetrics.MainThreadMs,
                RenderThreadMs = profilerMetrics.RenderThreadMs,
                GfxWaitForPresentMs = profilerMetrics.GfxWaitForPresentMs,
                ScriptUpdateMs = profilerMetrics.ScriptUpdateMs,
                BehaviourUpdateMs = profilerMetrics.BehaviourUpdateMs,
                LateUpdateMs = profilerMetrics.LateUpdateMs,
                FixedUpdateMs = profilerMetrics.FixedUpdateMs,
                CameraRenderMs = profilerMetrics.CameraRenderMs,
                UiRenderMs = profilerMetrics.UiRenderMs,
                TopSuspects = buffer != null ? buffer.GetTopAllocatingScopes(DiagnosticsCategories.Client, 5, 5) : new System.Collections.Generic.List<DiagnosticsScopeSummary>()
            };
        }

        public DiagnosticsClientMetrics Collect(
            NetworkManager networkManager,
            RollingMetricsBuffer buffer,
            DiagnosticsNetworkMetrics networkMetrics)
        {
            DiagnosticsClientMetrics metrics = new DiagnosticsClientMetrics();
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0f)
            {
                metrics.FrameMs = deltaTime * 1000d;
                metrics.Fps = 1d / deltaTime;
            }

            metrics.FrameMsP95_10s = CalculatePercentile(10d, 0.95d);
            metrics.FrameMsMax_10s = CalculateMax(10d);
            metrics.MemoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024d * 1024d);
            metrics.GcCollectionCount = GetGcCollectionCount();
            CollectFramePacing(metrics);
            _frameProfiler.Collect(metrics);

            int collectionDelta = metrics.GcCollectionCount.Value - _lastGcCollectionCount;
            _lastGcCollectionCount = metrics.GcCollectionCount.Value;
            if (collectionDelta <= 0)
            {
                metrics.GcSpikeMs = null;
            }

            // Unity GC allocation recorder is intentionally opt-in. In the Editor it can add
            // enough profiler overhead to create the focused Game View spikes we are diagnosing.
            metrics.GcAllocatedBytesPerSecond = null;

            metrics.ActiveVisibleEntities = GameplayMapVisibilityState.Count;
            metrics.ActiveGameObjects = null;
            metrics.ActiveEntities = GetClientNetworkEntityCount(networkManager);
            metrics.UiUpdateMs = buffer.SumScopeMs(DiagnosticsCategories.Ui, 1);
            metrics.RenderMs = buffer.SumScopeMs(DiagnosticsCategories.Render, 1);
            metrics.PhysicsMs = buffer.SumScopeMs(DiagnosticsCategories.Physics, 1);
            metrics.LocalSimulationMs = buffer.SumScopeMs(DiagnosticsCategories.Client, 1);

            if (networkMetrics != null)
            {
                metrics.IncomingMessagesPerSecond = networkMetrics.IncomingMessagesPerSecond;
                metrics.OutgoingMessagesPerSecond = networkMetrics.OutgoingMessagesPerSecond;
                metrics.IncomingBytesPerSecond = networkMetrics.IncomingBytesPerSecond;
                metrics.OutgoingBytesPerSecond = networkMetrics.OutgoingBytesPerSecond;
                metrics.PingMs = networkMetrics.PingMs;
                metrics.JitterMs = networkMetrics.JitterMs;
                metrics.PacketLossPercent = networkMetrics.PacketLossPercent;
            }

            // Top scopes are built on demand by /diagnostics/top/* and spike capture.
            // Building them in every periodic sample allocates enough in the Unity Editor
            // to create GC-driven frame spikes during 4K focused play.
            return metrics;
        }

        public void Dispose()
        {
            _frameProfiler.Dispose();
        }

        private static void CollectFramePacing(DiagnosticsClientMetrics metrics)
        {
            metrics.ApplicationFocused = Application.isFocused;
            metrics.ApplicationRunInBackground = Application.runInBackground;
            metrics.ScreenWidth = Screen.width;
            metrics.ScreenHeight = Screen.height;
            metrics.FullscreenMode = Screen.fullScreenMode.ToString();
            metrics.QualityLevel = QualitySettings.GetQualityLevel();
            string[] qualityNames = QualitySettings.names;
            if (qualityNames != null && metrics.QualityLevel.Value >= 0 && metrics.QualityLevel.Value < qualityNames.Length)
            {
                metrics.QualityName = qualityNames[metrics.QualityLevel.Value];
            }
            else
            {
                metrics.QualityName = "unknown";
            }

            metrics.VSyncCount = QualitySettings.vSyncCount;
            metrics.TargetFrameRate = Application.targetFrameRate;
#if UNITY_2022_2_OR_NEWER
            metrics.RefreshRate = Screen.currentResolution.refreshRateRatio.value;
#else
            metrics.RefreshRate = Screen.currentResolution.refreshRate;
#endif
            metrics.FixedDeltaTime = Time.fixedDeltaTime;
            metrics.MaximumDeltaTime = Time.maximumDeltaTime;
            metrics.TimeScale = Time.timeScale;
            metrics.CaptureFramerate = Time.captureFramerate;
#if UNITY_EDITOR
            metrics.EditorApplicationIsPlaying = EditorApplication.isPlaying;
            metrics.EditorPaused = EditorApplication.isPaused;
            metrics.IsEditor = true;
#else
            metrics.EditorApplicationIsPlaying = null;
            metrics.EditorPaused = null;
            metrics.IsEditor = false;
#endif
        }

        private int? GetClientNetworkEntityCount(NetworkManager networkManager)
        {
            if (networkManager == null || networkManager.ClientManager == null || networkManager.ClientManager.Objects == null)
            {
                return null;
            }

            return networkManager.ClientManager.Objects.Spawned.Count;
        }

        private static int GetGcCollectionCount()
        {
            return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        private double? CalculateMax(double seconds)
        {
            if (_frameCount == 0)
            {
                return null;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double cutoff = now - seconds;
            bool hasAny = false;
            double max = 0d;
            for (int i = 0; i < _frameCount; i++)
            {
                if (_frameTimeSeconds[i] < cutoff)
                {
                    continue;
                }

                double value = _frameTimesMs[i];
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
            if (_frameCount == 0)
            {
                return null;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double cutoff = now - seconds;
            int count = 0;
            for (int i = 0; i < _frameCount; i++)
            {
                if (_frameTimeSeconds[i] < cutoff)
                {
                    continue;
                }

                _scratch[count] = _frameTimesMs[i];
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
