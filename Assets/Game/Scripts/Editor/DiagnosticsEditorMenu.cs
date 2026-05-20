#if UNITY_EDITOR
using Game.Scripts.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    public sealed class DiagnosticsEditorMenu : EditorWindow
    {
        private const string MenuRoot = "Діагностика";
        private DiagnosticsConfig _config;
        private DiagnosticsConfig.EditorMode _mode;
        private Vector2 _scroll;
        private bool _showOutput = true;
        private bool _showEditorRuntime = true;
        private bool _showHttp = true;
        private bool _showSampling = true;
        private bool _showThresholds = true;

        [MenuItem(MenuRoot + "/Налаштування...", false, 0)]
        private static void OpenWindow()
        {
            DiagnosticsEditorMenu window = GetWindow<DiagnosticsEditorMenu>("Діагностика");
            window.minSize = new Vector2(440f, 560f);
            window.LoadFromPrefs();
            window.Show();
        }

        [MenuItem(MenuRoot + "/Увімкнути", false, 20)]
        private static void EnableDiagnostics()
        {
            DiagnosticsConfig.SetEditorMode(DiagnosticsConfig.EditorMode.ForceEnabled);
            ApplyRuntimeState();
        }

        [MenuItem(MenuRoot + "/Увімкнути", true)]
        private static bool ValidateEnableDiagnostics()
        {
            Menu.SetChecked(MenuRoot + "/Увімкнути", DiagnosticsConfig.GetEditorMode() == DiagnosticsConfig.EditorMode.ForceEnabled);
            return true;
        }

        [MenuItem(MenuRoot + "/Вимкнути", false, 21)]
        private static void DisableDiagnostics()
        {
            DiagnosticsConfig.SetEditorMode(DiagnosticsConfig.EditorMode.ForceDisabled);
            ApplyRuntimeState();
        }

        [MenuItem(MenuRoot + "/Вимкнути", true)]
        private static bool ValidateDisableDiagnostics()
        {
            Menu.SetChecked(MenuRoot + "/Вимкнути", DiagnosticsConfig.GetEditorMode() == DiagnosticsConfig.EditorMode.ForceDisabled);
            return true;
        }

        [MenuItem(MenuRoot + "/За замовчуванням", false, 22)]
        private static void UseDefaultDiagnosticsMode()
        {
            DiagnosticsConfig.SetEditorMode(DiagnosticsConfig.EditorMode.Default);
            ApplyRuntimeState();
        }

        [MenuItem(MenuRoot + "/За замовчуванням", true)]
        private static bool ValidateUseDefaultDiagnosticsMode()
        {
            Menu.SetChecked(MenuRoot + "/За замовчуванням", DiagnosticsConfig.GetEditorMode() == DiagnosticsConfig.EditorMode.Default);
            return true;
        }

        [MenuItem(MenuRoot + "/Вимкнути FPS Guard", false, 40)]
        private static void DisableFramePacingGuard()
        {
            DiagnosticsConfig config = DiagnosticsConfig.LoadEditorPrefsPreview();
            config.ApplyEditorClientFramePacingGuard = false;
            DiagnosticsConfig.SaveEditorPrefs(config);
            ApplyRuntimeState();
        }

        [MenuItem(MenuRoot + "/Вимкнути FPS Guard", true)]
        private static bool ValidateDisableFramePacingGuard()
        {
            DiagnosticsConfig config = DiagnosticsConfig.LoadEditorPrefsPreview();
            Menu.SetChecked(MenuRoot + "/Вимкнути FPS Guard", !config.ApplyEditorClientFramePacingGuard);
            return true;
        }

        [MenuItem(MenuRoot + "/Скинути всі налаштування", false, 100)]
        private static void ResetAllSettings()
        {
            if (!EditorUtility.DisplayDialog(
                    "Діагностика",
                    "Скинути всі editor-налаштування діагностики до значень за замовчуванням?",
                    "Скинути",
                    "Скасувати"))
            {
                return;
            }

            DiagnosticsConfig.ResetEditorPrefs();
            ApplyRuntimeState();
        }

        private void OnEnable()
        {
            LoadFromPrefs();
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                LoadFromPrefs();
            }

            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawMode();
            DrawOutput();
            DrawEditorRuntime();
            DrawHttp();
            DrawSampling();
            DrawThresholds();
            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void LoadFromPrefs()
        {
            _config = DiagnosticsConfig.LoadEditorPrefsPreview();
            _mode = DiagnosticsConfig.GetEditorMode();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Діагностика", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Поточний режим", GetModeLabel(_mode));
            EditorGUILayout.LabelField("Runtime стан", _config != null && _config.IsEnabled ? "Enabled: " + _config.StateReason : "Disabled: " + (_config != null ? _config.StateReason : string.Empty));

            DiagnosticsManager manager = DiagnosticsManager.Instance;
            string active = manager != null && manager.IsRunning ? "active" : "not running";
            EditorGUILayout.LabelField("Active manager", active);
            EditorGUILayout.Space(4f);
        }

        private void DrawMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Увімкнення", EditorStyles.boldLabel);
            _mode = (DiagnosticsConfig.EditorMode)EditorGUILayout.EnumPopup("Mode", _mode);
            EditorGUILayout.HelpBox("Default у редакторі вмикає діагностику fallback-ом. Force Disabled повністю блокує автозапуск у Play Mode.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawOutput()
        {
            _showOutput = EditorGUILayout.Foldout(_showOutput, "Output", true);
            if (!_showOutput)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _config.EnableHttpServer = EditorGUILayout.Toggle("HTTP server", _config.EnableHttpServer);
            _config.EnableJsonl = EditorGUILayout.Toggle("JSONL log", _config.EnableJsonl);
            _config.EnableOverlay = EditorGUILayout.Toggle("Overlay", _config.EnableOverlay);
            _config.EnableUnityProfilerRecorders = EditorGUILayout.Toggle("Unity profiler recorders", _config.EnableUnityProfilerRecorders);
            using (new EditorGUI.DisabledScope(!_config.EnableUnityProfilerRecorders))
            {
                _config.EnableUnityGcAllocRecorder = EditorGUILayout.Toggle("GC alloc recorder", _config.EnableUnityGcAllocRecorder);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEditorRuntime()
        {
            _showEditorRuntime = EditorGUILayout.Foldout(_showEditorRuntime, "Editor Runtime", true);
            if (!_showEditorRuntime)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _config.ApplyEditorClientFramePacingGuard = EditorGUILayout.Toggle("FPS guard", _config.ApplyEditorClientFramePacingGuard);
            _config.ApplyEditorFocusedClientRefreshCap = EditorGUILayout.Toggle("Focused refresh cap", _config.ApplyEditorFocusedClientRefreshCap);
            _config.ApplyEditorBackgroundPlayerLoopKeepAlive = EditorGUILayout.Toggle("Background keepalive", _config.ApplyEditorBackgroundPlayerLoopKeepAlive);
            _config.EditorClientDisableVSync = EditorGUILayout.Toggle("Disable VSync", _config.EditorClientDisableVSync);
            _config.EditorClientTargetFrameRate = EditorGUILayout.IntSlider("Client target FPS", _config.EditorClientTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
            _config.EditorServerTargetFrameRate = EditorGUILayout.IntSlider("Server target FPS", _config.EditorServerTargetFrameRate, DiagnosticsConfig.MinEditorTargetFrameRate, DiagnosticsConfig.MaxEditorTargetFrameRate);
            _config.EditorServerRenderFrameInterval = EditorGUILayout.IntSlider("Server render interval", _config.EditorServerRenderFrameInterval, 1, 120);
            _config.ApplyEditorGcSmoothing = EditorGUILayout.Toggle("GC smoothing", _config.ApplyEditorGcSmoothing);
            int gcBudgetUs = Mathf.Clamp(_config.EditorGcIncrementalBudgetNanoseconds / 1000, 100, 5000);
            gcBudgetUs = EditorGUILayout.IntSlider("GC budget us", gcBudgetUs, 100, 5000);
            _config.EditorGcIncrementalBudgetNanoseconds = gcBudgetUs * 1000;
            EditorGUILayout.EndVertical();
        }

        private void DrawHttp()
        {
            _showHttp = EditorGUILayout.Foldout(_showHttp, "HTTP", true);
            if (!_showHttp)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _config.BindAddress = EditorGUILayout.TextField("Bind address", _config.BindAddress);
            _config.HttpPort = Mathf.Clamp(EditorGUILayout.IntField("Port", _config.HttpPort), 1, 65535);
            _config.AllowPortFallback = EditorGUILayout.Toggle("Port fallback", _config.AllowPortFallback);
            _config.MaxPortFallbackAttempts = Mathf.Clamp(EditorGUILayout.IntField("Fallback attempts", _config.MaxPortFallbackAttempts), 1, 100);
            _config.Token = EditorGUILayout.TextField("Token", _config.Token);
            EditorGUILayout.EndVertical();
        }

        private void DrawSampling()
        {
            _showSampling = EditorGUILayout.Foldout(_showSampling, "Sampling", true);
            if (!_showSampling)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _config.BufferSeconds = Mathf.Clamp(EditorGUILayout.IntField("Buffer seconds", _config.BufferSeconds), 10, 300);
            _config.SampleIntervalSeconds = Mathf.Clamp(EditorGUILayout.FloatField("Sample interval", _config.SampleIntervalSeconds), 0.1f, 2f);
            _config.JsonlMetricIntervalSeconds = Mathf.Clamp(EditorGUILayout.FloatField("JSONL interval", _config.JsonlMetricIntervalSeconds), 0.1f, 10f);
            _config.SpikeCooldownSeconds = Mathf.Clamp(EditorGUILayout.FloatField("Spike cooldown", _config.SpikeCooldownSeconds), 0f, 60f);
            _config.SlowScopeLogThresholdMs = Mathf.Max(0f, EditorGUILayout.FloatField("Slow scope log ms", _config.SlowScopeLogThresholdMs));
            _config.MinScopeSampleMs = Mathf.Max(0f, EditorGUILayout.FloatField("Min scope sample ms", _config.MinScopeSampleMs));
            _config.MinEditorScopeSampleMs = Mathf.Max(0f, EditorGUILayout.FloatField("Min editor scope ms", _config.MinEditorScopeSampleMs));
            _config.TrackScopeAllocations = EditorGUILayout.Toggle("Track allocations", _config.TrackScopeAllocations);
            _config.MinScopeAllocatedBytes = System.Math.Max(0L, EditorGUILayout.LongField("Min allocated bytes", _config.MinScopeAllocatedBytes));
            _config.MaxScopeSamples = Mathf.Clamp(EditorGUILayout.IntField("Max scope samples", _config.MaxScopeSamples), 100, 200000);
            EditorGUILayout.EndVertical();
        }

        private void DrawThresholds()
        {
            _showThresholds = EditorGUILayout.Foldout(_showThresholds, "Thresholds", true);
            if (!_showThresholds)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _config.ClientFrameSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("Client frame spike ms", _config.ClientFrameSpikeMs));
            _config.ClientLowFps = Mathf.Max(1f, EditorGUILayout.FloatField("Client low FPS", _config.ClientLowFps));
            _config.ClientGcSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("Client GC spike ms", _config.ClientGcSpikeMs));
            _config.PingSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("Ping spike ms", _config.PingSpikeMs));
            _config.JitterSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("Jitter spike ms", _config.JitterSpikeMs));
            _config.PacketLossSpikePercent = Mathf.Clamp(EditorGUILayout.FloatField("Packet loss spike %", _config.PacketLossSpikePercent), 0f, 100f);
            _config.MemoryGrowthMbPerMinute = Mathf.Max(0f, EditorGUILayout.FloatField("Memory growth MB/min", _config.MemoryGrowthMbPerMinute));
            _config.ServerTickSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("Server tick spike ms", _config.ServerTickSpikeMs));
            _config.ServerTickMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("Server tick multiplier", _config.ServerTickMultiplier));
            _config.RpcHandlerSpikeMs = Mathf.Max(0f, EditorGUILayout.FloatField("RPC handler spike ms", _config.RpcHandlerSpikeMs));
            _config.RpcStormCountPerSecond = Mathf.Max(0f, EditorGUILayout.FloatField("RPC storm count/s", _config.RpcStormCountPerSecond));
            _config.NetworkMessageStormPerSecond = Mathf.Max(0f, EditorGUILayout.FloatField("Network message storm/s", _config.NetworkMessageStormPerSecond));
            _config.EntityGrowthSpikeCount = Mathf.Max(0, EditorGUILayout.IntField("Entity growth spike", _config.EntityGrowthSpikeCount));
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Зберегти і застосувати", GUILayout.Height(28f)))
            {
                SaveAndApply();
            }

            if (GUILayout.Button("Вимкнути", GUILayout.Height(28f)))
            {
                _mode = DiagnosticsConfig.EditorMode.ForceDisabled;
                SaveAndApply();
            }

            if (GUILayout.Button("Скинути", GUILayout.Height(28f)))
            {
                DiagnosticsConfig.ResetEditorPrefs();
                LoadFromPrefs();
                ApplyRuntimeState();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private void SaveAndApply()
        {
            DiagnosticsConfig.SetEditorMode(_mode);
            DiagnosticsConfig.SaveEditorPrefs(_config);
            LoadFromPrefs();
            ApplyRuntimeState();
        }

        private static void ApplyRuntimeState()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            DiagnosticsConfig config = DiagnosticsConfig.LoadRuntime();
            DiagnosticsManager manager = DiagnosticsManager.Instance;
            if (!config.IsEnabled)
            {
                if (manager != null)
                {
                    DestroyImmediate(manager.gameObject);
                }

                return;
            }

            if (manager != null)
            {
                DestroyImmediate(manager.gameObject);
            }

            DiagnosticsManager.EnsureStarted(config);
        }

        private static string GetModeLabel(DiagnosticsConfig.EditorMode mode)
        {
            if (mode == DiagnosticsConfig.EditorMode.ForceEnabled)
            {
                return "Force Enabled";
            }

            if (mode == DiagnosticsConfig.EditorMode.ForceDisabled)
            {
                return "Force Disabled";
            }

            return "Default";
        }
    }
}
#endif
