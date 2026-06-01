using System;
using System.Collections.Generic;
using System.Diagnostics;
using FishNet.Managing;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsManager : MonoBehaviour
    {
        private static DiagnosticsManager _instance;
        private static bool _missingManagerLogged;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker JsonlFrameSpikeMarker = new ProfilerMarker("Diagnostics.Jsonl.FrameSpike");
        private static readonly ProfilerMarker JsonlScopeMarker = new ProfilerMarker("Diagnostics.Jsonl.Scope");
        private static readonly ProfilerMarker JsonlMetricMarker = new ProfilerMarker("Diagnostics.Jsonl.Metric");
        private static readonly ProfilerMarker JsonlSpikeMarker = new ProfilerMarker("Diagnostics.Jsonl.Spike");
#endif

        private DiagnosticsConfig _config;
        private RollingMetricsBuffer _buffer;
        private ClientDiagnosticsCollector _clientCollector;
        private ServerDiagnosticsCollector _serverCollector;
        private NetworkDiagnosticsCollector _networkCollector;
        private SpikeDetector _spikeDetector;
        private DiagnosticsAnalyzer _analyzer;
        private DiagnosticsJsonlWriter _jsonlWriter;
        private DiagnosticsHttpServer _httpServer;
        private NetworkManager _networkManager;
        private TimeManager _timeManager;
        private double _nextSampleTime;
        private double _nextJsonlMetricTime;
        private long _startTimestamp;
        private long _serverTickStart;
        private string _sessionId;
#if UNITY_EDITOR
        private double _nextEditorClientFramePacingCheckTime;
        private bool _savedEditorClientFramePacing;
        private bool _loggedEditorClientFramePacing;
        private bool _loggedEditorGcSmoothing;
        private bool _loggedEditorGcSmoothingUnavailable;
        private bool _editorBackgroundPlayerLoopSubscribed;
        private bool _loggedEditorBackgroundPlayerLoopKeepAlive;
        private double _lastEditorBackgroundPlayerLoopRequestTime;
        private int _previousTargetFrameRate;
        private int _previousVSyncCount;
        private int _previousRenderFrameInterval;
        private bool _previousRunInBackground;
#endif

        public static DiagnosticsManager Instance => _instance;
        public bool IsRunning => _config != null && _config.IsEnabled && _buffer != null;
        public DiagnosticsConfig Config => _config;
        public RollingMetricsBuffer Buffer => _buffer;
        public NetworkDiagnosticsCollector NetworkCollector => _networkCollector;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnLoad()
        {
            DiagnosticsConfig config = DiagnosticsConfig.LoadRuntime();
            if (!config.IsEnabled)
            {
                UnityEngine.Debug.Log("[Diagnostics] Disabled: " + config.StateReason);
                return;
            }

            StartConfiguredManager(config, false);
        }

        public static bool EnsureStarted(DiagnosticsConfig config)
        {
            if (_instance != null && _instance.IsRunning)
            {
                return true;
            }

            if (config == null)
            {
                config = DiagnosticsConfig.LoadRuntime();
            }

            if (!config.IsEnabled)
            {
                config.IsEnabled = true;
            }

            if (string.IsNullOrEmpty(config.StateReason))
            {
                config.StateReason = "manual start";
            }

            return StartConfiguredManager(config, true);
        }

        private static bool StartConfiguredManager(DiagnosticsConfig config, bool logMissingManager)
        {
            if (_instance == null)
            {
                if (logMissingManager && !_missingManagerLogged)
                {
                    _missingManagerLogged = true;
                    UnityEngine.Debug.LogWarning(
                        "[Diagnostics] DiagnosticsManager is not configured in the scene. Add it to a prefab/scene and wire optional DiagnosticsOverlay there.");
                }

                return false;
            }

            if (_instance.IsRunning)
            {
                return true;
            }

            _instance.Initialize(config);
            return _instance.IsRunning;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            _missingManagerLogged = false;
        }

        public void Initialize(DiagnosticsConfig config)
        {
            if (config == null || !config.IsEnabled)
            {
                string reason = config != null ? config.StateReason : "missing config";
                UnityEngine.Debug.Log("[Diagnostics] Disabled: " + reason);
                enabled = false;
                return;
            }

            _config = config;
            enabled = true;
            _sessionId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            _buffer = new RollingMetricsBuffer(_config.BufferSeconds, _config.MaxScopeSamples);
            _clientCollector = new ClientDiagnosticsCollector(_config);
            _serverCollector = new ServerDiagnosticsCollector();
            _networkCollector = new NetworkDiagnosticsCollector();
            _spikeDetector = new SpikeDetector(_config);
            _analyzer = new DiagnosticsAnalyzer(_config);
            _startTimestamp = Stopwatch.GetTimestamp();
            _nextSampleTime = Time.realtimeSinceStartupAsDouble;

            if (_config.EnableJsonl)
            {
                _jsonlWriter = new DiagnosticsJsonlWriter(_sessionId);
            }

            bool httpStarted = false;
            if (_config.EnableHttpServer)
            {
                _httpServer = new DiagnosticsHttpServer(this, _config);
                httpStarted = _httpServer.Start();
            }

            string httpStatus = _config.EnableHttpServer ? (httpStarted && _httpServer != null ? _httpServer.Url : "unavailable") : "disabled";
            string logPath = _jsonlWriter != null ? _jsonlWriter.FilePath : "disabled";
            UnityEngine.Debug.Log("[Diagnostics] Enabled: " + _config.StateReason + ". HTTP " + httpStatus + ". JSONL " + logPath);
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            DiagnosticsFrameSpike frameSpike = _clientCollector.RecordFrame(now, Time.unscaledDeltaTime, _config.ClientFrameSpikeMs, _buffer);
            if (frameSpike != null)
            {
                _buffer.AddFrameSpike(frameSpike);
                EnqueueJsonlFrameSpike(frameSpike);
            }

            ResolveNetworkManager();
            MaintainEditorClientFramePacingGuard(now);
            MaintainEditorBackgroundPlayerLoopKeepAlive();
            MaintainEditorGcSmoothing();

            if (now < _nextSampleTime)
            {
                return;
            }

            _nextSampleTime = now + _config.SampleIntervalSeconds;
            CollectSample(now);
        }

        private void OnDestroy()
        {
            if (_timeManager != null)
            {
                _timeManager.OnPreTick -= OnPreTick;
                _timeManager.OnPostTick -= OnPostTick;
            }

            if (_networkCollector != null)
            {
                _networkCollector.Unsubscribe();
            }

            if (_clientCollector != null)
            {
                _clientCollector.Dispose();
            }

            if (_httpServer != null)
            {
                _httpServer.Dispose();
                _httpServer = null;
            }

            if (_jsonlWriter != null)
            {
                _jsonlWriter.Dispose();
                _jsonlWriter = null;
            }

            RestoreEditorClientFramePacingGuard();
            SetEditorBackgroundPlayerLoopKeepAlive(false);

            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void RecordScope(string name, string category, double durationMs)
        {
            RecordScope(name, category, durationMs, 0);
        }

        public void RecordScope(string name, string category, double durationMs, long allocatedBytes)
        {
            if (!IsRunning || string.IsNullOrEmpty(name))
            {
                return;
            }

            string resolvedCategory = string.IsNullOrEmpty(category) ? DiagnosticsCategories.Unknown : category;
            if (resolvedCategory == DiagnosticsCategories.Rpc)
            {
                RecordEvent(name, DiagnosticsCategories.Rpc, 1, durationMs);
            }

            double minimumDurationMs = resolvedCategory == DiagnosticsCategories.Editor
                ? _config.MinEditorScopeSampleMs
                : _config.MinScopeSampleMs;
            if (durationMs < minimumDurationMs && allocatedBytes < _config.MinScopeAllocatedBytes)
            {
                return;
            }

            DiagnosticsScopeSample sample = new DiagnosticsScopeSample
            {
                TimeSeconds = Time.realtimeSinceStartupAsDouble,
                Timestamp = string.Empty,
                Name = name,
                Category = resolvedCategory,
                DurationMs = durationMs,
                AllocatedBytes = allocatedBytes > 0 ? allocatedBytes : 0
            };

            _buffer.AddScopeSample(sample);

            if (_jsonlWriter != null && durationMs >= _config.SlowScopeLogThresholdMs)
            {
                sample.Timestamp = UtcNowIso();
                EnqueueJsonlScope(sample);
            }
        }

        public void RecordEvent(string name, string category, int count, double totalMs)
        {
            if (!IsRunning || string.IsNullOrEmpty(name) || count <= 0)
            {
                return;
            }

            DiagnosticsEventSample sample = new DiagnosticsEventSample
            {
                TimeSeconds = Time.realtimeSinceStartupAsDouble,
                Timestamp = string.Empty,
                Name = name,
                Category = string.IsNullOrEmpty(category) ? DiagnosticsCategories.Unknown : category,
                Count = count,
                TotalMs = totalMs
            };

            _buffer.AddEventSample(sample);
        }

        public void RecordOutgoingNetworkMessage(string eventName, int estimatedBytes, int connectionId)
        {
            if (!IsRunning)
            {
                return;
            }

            _networkCollector.RecordOutgoing(estimatedBytes, connectionId);
            if (!string.IsNullOrEmpty(eventName))
            {
                RecordEvent(eventName, DiagnosticsCategories.Network, 1, 0d);
            }
        }

        public static void RecordOutgoing(string eventName, int estimatedBytes)
        {
            RecordOutgoing(eventName, estimatedBytes, -1);
        }

        public static void RecordOutgoing(string eventName, int estimatedBytes, int connectionId)
        {
            DiagnosticsManager manager = Instance;
            if (manager == null)
            {
                return;
            }

            manager.RecordOutgoingNetworkMessage(eventName, estimatedBytes, connectionId);
        }

        public string BuildHealthJson()
        {
            double uptime = 0d;
            if (_startTimestamp > 0)
            {
                uptime = (Stopwatch.GetTimestamp() - _startTimestamp) / (double)Stopwatch.Frequency;
            }

            return DiagnosticsJson.HealthToJson(
                true,
                IsRunning,
                uptime,
                _config != null ? _config.BufferSeconds : 0,
                _sessionId,
                _jsonlWriter != null ? _jsonlWriter.FilePath : string.Empty,
                _config != null ? _config.BindAddress : string.Empty,
                _config != null ? _config.HttpPort : 0);
        }

        public string BuildCurrentSnapshotJson()
        {
            if (_buffer == null)
            {
                return "{}";
            }

            return DiagnosticsJson.SnapshotToJson(_buffer.GetCurrentSnapshot(10));
        }

        public string BuildLastSamplesJson(int seconds)
        {
            return DiagnosticsJson.SamplesToJson(_buffer.GetSamples(seconds), seconds);
        }

        public string BuildSpikesJson(int seconds)
        {
            return DiagnosticsJson.SpikesToJson(_buffer.GetSpikes(seconds), seconds);
        }

        public string BuildFrameSpikesJson(int seconds)
        {
            return DiagnosticsJson.FrameSpikesToJson(_buffer.GetFrameSpikes(seconds), seconds);
        }

        public string BuildTopScopesJson(string group, int seconds)
        {
            return DiagnosticsJson.TopScopesToJson(group, seconds, _buffer.GetTopScopes(group, seconds, 10));
        }

        public string BuildNetworkJson(int seconds)
        {
            return DiagnosticsJson.NetworkSummaryToJson(_buffer.GetNetworkSummary(seconds));
        }

        public string BuildAnalyzeJson(int seconds)
        {
            return DiagnosticsJson.AnalysisToJson(_analyzer.Analyze(_buffer, seconds));
        }

        public DiagnosticsSnapshot GetCurrentSnapshot()
        {
            return _buffer != null ? _buffer.GetCurrentSnapshot(10) : null;
        }

        private void ResolveNetworkManager()
        {
            NetworkManager manager = FindNetworkManager();
            if (manager == _networkManager)
            {
                if (_networkCollector != null)
                {
                    _networkCollector.Resolve(manager);
                }

                return;
            }

            if (_timeManager != null)
            {
                _timeManager.OnPreTick -= OnPreTick;
                _timeManager.OnPostTick -= OnPostTick;
                _timeManager = null;
            }

            _networkManager = manager;
            if (_networkCollector != null)
            {
                _networkCollector.Resolve(_networkManager);
            }

            if (_networkManager != null && _networkManager.TimeManager != null)
            {
                _timeManager = _networkManager.TimeManager;
                _timeManager.OnPreTick += OnPreTick;
                _timeManager.OnPostTick += OnPostTick;
            }
        }

        private static NetworkManager FindNetworkManager()
        {
            IReadOnlyList<NetworkManager> instances = NetworkManager.Instances;
            if (instances == null || instances.Count == 0)
            {
                return null;
            }

            return instances[0];
        }

        private void OnPreTick()
        {
            if (_networkManager == null || !_networkManager.IsServerStarted)
            {
                return;
            }

            _serverTickStart = Stopwatch.GetTimestamp();
        }

        private void OnPostTick()
        {
            if (_networkManager == null || !_networkManager.IsServerStarted || _serverTickStart <= 0)
            {
                return;
            }

            long elapsed = Stopwatch.GetTimestamp() - _serverTickStart;
            double durationMs = elapsed * 1000d / Stopwatch.Frequency;
            _serverCollector.RecordServerTick(Time.realtimeSinceStartupAsDouble, durationMs);
            _serverTickStart = 0;
        }

        private void CollectSample(double now)
        {
            DiagnosticsNetworkMetrics networkMetrics = _networkCollector.Collect(_networkManager);
            DiagnosticsMetricSample sample = new DiagnosticsMetricSample
            {
                TimeSeconds = now,
                Timestamp = UtcNowIso(),
                SessionId = _sessionId,
                Map = SceneManager.GetActiveScene().name,
                Mode = GetMode(_networkManager),
                Network = networkMetrics
            };

            sample.Client = _clientCollector.Collect(_networkManager, _buffer, networkMetrics);
            sample.Server = _serverCollector.Collect(_networkManager, _buffer, _networkCollector, networkMetrics);
            _buffer.AddSample(sample);

            if (_jsonlWriter != null && now >= _nextJsonlMetricTime)
            {
                _nextJsonlMetricTime = now + Mathf.Max(0.5f, _config.JsonlMetricIntervalSeconds);
                EnqueueJsonlMetric(sample);
            }

            List<DiagnosticsSpike> spikes = _spikeDetector.Detect(sample, _buffer);
            for (int i = 0; i < spikes.Count; i++)
            {
                DiagnosticsSpike spike = spikes[i];
                if (spike == null)
                {
                    continue;
                }

                _buffer.AddSpike(spike);
                EnqueueJsonlSpike(spike);
            }
        }

        private void EnqueueJsonlFrameSpike(DiagnosticsFrameSpike frameSpike)
        {
            if (_jsonlWriter == null || frameSpike == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (JsonlFrameSpikeMarker.Auto())
#endif
            {
                _jsonlWriter.Enqueue(DiagnosticsJson.JsonlFrameSpikeEvent(frameSpike));
            }
        }

        private void EnqueueJsonlScope(DiagnosticsScopeSample sample)
        {
            if (_jsonlWriter == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (JsonlScopeMarker.Auto())
#endif
            {
                _jsonlWriter.Enqueue(DiagnosticsJson.JsonlScopeEvent(sample));
            }
        }

        private void EnqueueJsonlMetric(DiagnosticsMetricSample sample)
        {
            if (_jsonlWriter == null || sample == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (JsonlMetricMarker.Auto())
#endif
            {
                _jsonlWriter.Enqueue(DiagnosticsJson.JsonlMetricEvent(sample));
            }
        }

        private void EnqueueJsonlSpike(DiagnosticsSpike spike)
        {
            if (_jsonlWriter == null || spike == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (JsonlSpikeMarker.Auto())
#endif
            {
                _jsonlWriter.Enqueue(DiagnosticsJson.JsonlSpikeEvent(spike, _buffer.GetCurrentSnapshot(10)));
            }
        }

        private static string GetMode(NetworkManager manager)
        {
            if (manager == null)
            {
                return "offline";
            }

            bool server = manager.IsServerStarted;
            bool client = manager.IsClientStarted;
            if (server && client)
            {
                return "client-server";
            }

            if (server)
            {
                return "server";
            }

            if (client)
            {
                return "client";
            }

            return "offline";
        }

        private static string UtcNowIso()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void MaintainEditorClientFramePacingGuard(double now)
        {
#if UNITY_EDITOR
            if (_config == null || !_config.ApplyEditorClientFramePacingGuard)
            {
                RestoreEditorClientFramePacingGuard();
                return;
            }

            if (now < _nextEditorClientFramePacingCheckTime)
            {
                return;
            }

            _nextEditorClientFramePacingCheckTime = now + 1d;
            if (!IsEditorNetworkPlayMode())
            {
                RestoreEditorClientFramePacingGuard();
                return;
            }

            if (!_savedEditorClientFramePacing)
            {
                _previousTargetFrameRate = Application.targetFrameRate;
                _previousVSyncCount = QualitySettings.vSyncCount;
                _previousRenderFrameInterval = OnDemandRendering.renderFrameInterval;
                _previousRunInBackground = Application.runInBackground;
                _savedEditorClientFramePacing = true;
            }

            int configuredTargetFrameRate = IsStandaloneClientEditor()
                ? Mathf.Clamp(_config.EditorClientTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate)
                : Mathf.Clamp(_config.EditorServerTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
            int targetFrameRate = ResolveEffectiveEditorTargetFrameRate(configuredTargetFrameRate);
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
            }

            if (_config.EditorClientDisableVSync && QualitySettings.vSyncCount != 0)
            {
                QualitySettings.vSyncCount = 0;
            }

            if (Application.targetFrameRate != targetFrameRate)
            {
                Application.targetFrameRate = targetFrameRate;
            }

            int renderFrameInterval = ResolveEffectiveEditorRenderFrameInterval();
            if (OnDemandRendering.renderFrameInterval != renderFrameInterval)
            {
                OnDemandRendering.renderFrameInterval = renderFrameInterval;
            }

            if (!_loggedEditorClientFramePacing)
            {
                UnityEngine.Debug.Log("[Diagnostics] Editor frame pacing guard (" + GetMode(_networkManager) + "): targetFrameRate="
                                      + Application.targetFrameRate
                                      + ", configuredTargetFrameRate="
                                      + configuredTargetFrameRate
                                      + ", renderFrameInterval="
                                      + OnDemandRendering.renderFrameInterval
                                      + ", vSyncCount="
                                      + QualitySettings.vSyncCount
                                      + ", runInBackground="
                                      + Application.runInBackground);
                _loggedEditorClientFramePacing = true;
            }
#endif
        }

        private bool IsStandaloneClientEditor()
        {
#if UNITY_EDITOR
            return _networkManager != null
                   && _networkManager.IsClientStarted
                   && !_networkManager.IsServerStarted;
#else
            return false;
#endif
        }

        private int ResolveEffectiveEditorTargetFrameRate(int configuredTargetFrameRate)
        {
#if UNITY_EDITOR
            int targetFrameRate = configuredTargetFrameRate;
            if (_config != null
                && _config.ApplyEditorFocusedClientRefreshCap
                && IsStandaloneClientEditor()
                && Application.isFocused
                && IsHighResolutionGameView())
            {
                int refreshRateCap = GetCurrentRefreshRateFrameCap();
                if (refreshRateCap > 0 && targetFrameRate > refreshRateCap)
                {
                    targetFrameRate = refreshRateCap;
                }
            }

            return Mathf.Clamp(targetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
#else
            return configuredTargetFrameRate;
#endif
        }

        private int ResolveEffectiveEditorRenderFrameInterval()
        {
#if UNITY_EDITOR
            if (_config != null && IsStandaloneServerEditor())
            {
                return Mathf.Clamp(_config.EditorServerRenderFrameInterval, 1, 120);
            }

            return 1;
#else
            return 1;
#endif
        }

        private bool IsStandaloneServerEditor()
        {
#if UNITY_EDITOR
            return _networkManager != null
                   && _networkManager.IsServerStarted
                   && !_networkManager.IsClientStarted;
#else
            return false;
#endif
        }

        private static bool IsHighResolutionGameView()
        {
#if UNITY_EDITOR
            return Screen.width >= 1920 && Screen.height >= 1080;
#else
            return false;
#endif
        }

        private static int GetCurrentRefreshRateFrameCap()
        {
#if UNITY_EDITOR
#if UNITY_2022_2_OR_NEWER
            double refreshRate = Screen.currentResolution.refreshRateRatio.value;
#else
            double refreshRate = Screen.currentResolution.refreshRate;
#endif
            if (double.IsNaN(refreshRate) || double.IsInfinity(refreshRate) || refreshRate < 30d)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.CeilToInt((float)refreshRate), DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
#else
            return 0;
#endif
        }

        private bool IsEditorNetworkPlayMode()
        {
#if UNITY_EDITOR
            return _networkManager != null
                   && (_networkManager.IsClientStarted || _networkManager.IsServerStarted);
#else
            return false;
#endif
        }

        private void RestoreEditorClientFramePacingGuard()
        {
#if UNITY_EDITOR
            if (!_savedEditorClientFramePacing)
            {
                return;
            }

            Application.targetFrameRate = _previousTargetFrameRate;
            QualitySettings.vSyncCount = _previousVSyncCount;
            OnDemandRendering.renderFrameInterval = _previousRenderFrameInterval;
            Application.runInBackground = _previousRunInBackground;
            _savedEditorClientFramePacing = false;
            _loggedEditorClientFramePacing = false;
#endif
        }

        private void MaintainEditorBackgroundPlayerLoopKeepAlive()
        {
#if UNITY_EDITOR
            if (_config == null || !_config.ApplyEditorBackgroundPlayerLoopKeepAlive || !IsEditorNetworkPlayMode())
            {
                SetEditorBackgroundPlayerLoopKeepAlive(false);
                return;
            }

            SetEditorBackgroundPlayerLoopKeepAlive(true);
#endif
        }

        private void SetEditorBackgroundPlayerLoopKeepAlive(bool enabled)
        {
#if UNITY_EDITOR
            if (_editorBackgroundPlayerLoopSubscribed == enabled)
            {
                return;
            }

            if (enabled)
            {
                EditorApplication.update += OnEditorBackgroundPlayerLoopUpdate;
            }
            else
            {
                EditorApplication.update -= OnEditorBackgroundPlayerLoopUpdate;
                _loggedEditorBackgroundPlayerLoopKeepAlive = false;
            }

            _editorBackgroundPlayerLoopSubscribed = enabled;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorBackgroundPlayerLoopUpdate()
        {
            if (_config == null || !_config.ApplyEditorBackgroundPlayerLoopKeepAlive || !IsEditorNetworkPlayMode())
            {
                SetEditorBackgroundPlayerLoopKeepAlive(false);
                return;
            }

            if (Application.isFocused)
            {
                return;
            }

            int configuredFrameRate = IsStandaloneClientEditor()
                ? Mathf.Clamp(_config.EditorClientTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate)
                : Mathf.Clamp(_config.EditorServerTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
            int keepAliveFrameRate = Mathf.Clamp(configuredFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
            double now = EditorApplication.timeSinceStartup;
            double minInterval = 1d / keepAliveFrameRate;
            if (now - _lastEditorBackgroundPlayerLoopRequestTime < minInterval)
            {
                return;
            }

            _lastEditorBackgroundPlayerLoopRequestTime = now;
            EditorApplication.QueuePlayerLoopUpdate();

            if (!_loggedEditorBackgroundPlayerLoopKeepAlive)
            {
                UnityEngine.Debug.Log("[Diagnostics] Editor background PlayerLoop keepalive (" + GetMode(_networkManager) + "): keepAliveFrameRate="
                                      + keepAliveFrameRate
                                      + ", configuredTargetFrameRate="
                                      + configuredFrameRate);
                _loggedEditorBackgroundPlayerLoopKeepAlive = true;
            }
        }
#endif

        private void MaintainEditorGcSmoothing()
        {
#if UNITY_EDITOR
            if (_config == null || !_config.ApplyEditorGcSmoothing || !IsEditorNetworkPlayMode())
            {
                return;
            }

            if (!UnityEngine.Scripting.GarbageCollector.isIncremental)
            {
                if (!_loggedEditorGcSmoothingUnavailable)
                {
                    UnityEngine.Debug.LogWarning("[Diagnostics] Editor incremental GC smoothing requested, but incremental GC is disabled in Project Settings.");
                    _loggedEditorGcSmoothingUnavailable = true;
                }

                return;
            }

            _loggedEditorGcSmoothingUnavailable = false;
            int budgetNanoseconds = Mathf.Max(100000, _config.EditorGcIncrementalBudgetNanoseconds);
            UnityEngine.Scripting.GarbageCollector.CollectIncremental((ulong)budgetNanoseconds);
            if (!_loggedEditorGcSmoothing)
            {
                UnityEngine.Debug.Log("[Diagnostics] Editor incremental GC smoothing: budgetNs=" + budgetNanoseconds);
                _loggedEditorGcSmoothing = true;
            }
#endif
        }
    }
}
