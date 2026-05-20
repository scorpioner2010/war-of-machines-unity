using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsConfig
    {
        public const string EnableEnvironmentVariable = "ENABLE_DIAGNOSTICS";
        public const string DisableEnvironmentVariable = "DISABLE_DIAGNOSTICS";
        public const string PortEnvironmentVariable = "DIAGNOSTICS_PORT";
        public const string TokenEnvironmentVariable = "DIAGNOSTICS_TOKEN";
        public const string DisableEditorClientFramePacingEnvironmentVariable = "DISABLE_EDITOR_CLIENT_FRAME_PACING";
        public const string DisableEditorFocusedClientRefreshCapEnvironmentVariable = "DISABLE_EDITOR_FOCUSED_CLIENT_REFRESH_CAP";
        public const string DisableEditorBackgroundPlayerLoopKeepAliveEnvironmentVariable = "DISABLE_EDITOR_BACKGROUND_PLAYER_LOOP_KEEPALIVE";
        public const string EditorClientTargetFpsEnvironmentVariable = "EDITOR_CLIENT_TARGET_FPS";
        public const string EditorServerTargetFpsEnvironmentVariable = "EDITOR_SERVER_TARGET_FPS";
        public const string EditorServerRenderFrameIntervalEnvironmentVariable = "EDITOR_SERVER_RENDER_FRAME_INTERVAL";
        public const string DisableEditorGcSmoothingEnvironmentVariable = "DISABLE_EDITOR_GC_SMOOTHING";
        public const string EditorGcBudgetUsEnvironmentVariable = "EDITOR_GC_BUDGET_US";
        public const string EnableUnityProfilerRecordersEnvironmentVariable = "ENABLE_UNITY_PROFILER_RECORDERS";
        public const string EnableUnityGcAllocRecorderEnvironmentVariable = "ENABLE_UNITY_GC_ALLOC_RECORDER";
        public const string EditorPrefsEnabledKey = "WarOfMachines.Diagnostics.Enabled";
        public const string EditorPrefsDisabledKey = "WarOfMachines.Diagnostics.Disabled";
        public const int MinEditorTargetFrameRate = 30;
        public const int MaxEditorTargetFrameRate = 500;
        public const int DefaultEditorTargetFrameRate = 500;
#if UNITY_EDITOR
        public const string EditorPrefsPrefix = "WarOfMachines.Diagnostics.";
#endif

        public static bool Enabled;

        public bool IsEnabled;
        public string StateReason = string.Empty;
        public bool EnableHttpServer = true;
        public bool EnableJsonl = true;
        public bool EnableOverlay = true;
        public bool ApplyEditorClientFramePacingGuard = true;
        public bool ApplyEditorFocusedClientRefreshCap = true;
        public bool ApplyEditorBackgroundPlayerLoopKeepAlive = true;
        public bool ApplyEditorGcSmoothing = true;
        public bool EditorClientDisableVSync = true;
        public bool EnableUnityProfilerRecorders = true;
        public bool EnableUnityGcAllocRecorder = false;
        public bool AllowPortFallback = true;
        public string BindAddress = "127.0.0.1";
        public int HttpPort = 8765;
        public int EditorClientTargetFrameRate = DefaultEditorTargetFrameRate;
        public int EditorServerTargetFrameRate = DefaultEditorTargetFrameRate;
        public int EditorServerRenderFrameInterval = 20;
        public int EditorGcIncrementalBudgetNanoseconds = 500000;
        public int MaxPortFallbackAttempts = 10;
        public string Token = string.Empty;
        public int BufferSeconds = 60;
        public float SampleIntervalSeconds = 1f;
        public float JsonlMetricIntervalSeconds = 1f;
        public float SpikeCooldownSeconds = 5f;
        public float SlowScopeLogThresholdMs = 5f;
        public float MinScopeSampleMs = 0.25f;
        public float MinEditorScopeSampleMs = 1f;
        public bool TrackScopeAllocations = false;
        public long MinScopeAllocatedBytes = 4096;
        public int MaxScopeSamples = 12000;

        public float ClientFrameSpikeMs = 50f;
        public float ClientLowFps = 30f;
        public float ClientGcSpikeMs = 20f;
        public float PingSpikeMs = 150f;
        public float JitterSpikeMs = 50f;
        public float PacketLossSpikePercent = 2f;
        public float MemoryGrowthMbPerMinute = 256f;

        public float ServerTickSpikeMs = 50f;
        public float ServerTickMultiplier = 2f;
        public float RpcHandlerSpikeMs = 20f;
        public float RpcStormCountPerSecond = 120f;
        public float NetworkMessageStormPerSecond = 400f;
        public int EntityGrowthSpikeCount = 100;

        public static DiagnosticsConfig LoadRuntime()
        {
            DiagnosticsConfig config = new DiagnosticsConfig();
#if UNITY_EDITOR
            ApplyEditorPrefs(config);
#endif
            string reason;
            config.IsEnabled = ResolveEnabled(out reason);
            config.StateReason = reason;
            config.HttpPort = ReadInt(PortEnvironmentVariable, ReadCommandLineInt("-diagnosticsPort", config.HttpPort));
            config.Token = ReadString(TokenEnvironmentVariable, ReadCommandLineString("-diagnosticsToken", config.Token));
            config.ApplyEditorClientFramePacingGuard = !ReadBool(DisableEditorClientFramePacingEnvironmentVariable, !config.ApplyEditorClientFramePacingGuard);
            config.ApplyEditorFocusedClientRefreshCap = !ReadBool(DisableEditorFocusedClientRefreshCapEnvironmentVariable, !config.ApplyEditorFocusedClientRefreshCap);
            config.ApplyEditorBackgroundPlayerLoopKeepAlive = !ReadBool(DisableEditorBackgroundPlayerLoopKeepAliveEnvironmentVariable, !config.ApplyEditorBackgroundPlayerLoopKeepAlive);
            config.EditorClientTargetFrameRate = Mathf.Clamp(ReadInt(EditorClientTargetFpsEnvironmentVariable, ReadCommandLineInt("-editorClientTargetFps", config.EditorClientTargetFrameRate)), MinEditorTargetFrameRate, MaxEditorTargetFrameRate);
            config.EditorServerTargetFrameRate = Mathf.Clamp(ReadInt(EditorServerTargetFpsEnvironmentVariable, ReadCommandLineInt("-editorServerTargetFps", config.EditorServerTargetFrameRate)), MinEditorTargetFrameRate, MaxEditorTargetFrameRate);
            config.EditorServerRenderFrameInterval = Mathf.Clamp(ReadInt(EditorServerRenderFrameIntervalEnvironmentVariable, ReadCommandLineInt("-editorServerRenderFrameInterval", config.EditorServerRenderFrameInterval)), 1, 120);
            config.ApplyEditorGcSmoothing = !ReadBool(DisableEditorGcSmoothingEnvironmentVariable, !config.ApplyEditorGcSmoothing);
            int gcBudgetUs = Mathf.Clamp(ReadInt(EditorGcBudgetUsEnvironmentVariable, ReadCommandLineInt("-editorGcBudgetUs", config.EditorGcIncrementalBudgetNanoseconds / 1000)), 100, 5000);
            config.EditorGcIncrementalBudgetNanoseconds = gcBudgetUs * 1000;
            config.EnableUnityProfilerRecorders = ReadBool(EnableUnityProfilerRecordersEnvironmentVariable, config.EnableUnityProfilerRecorders);
            config.EnableUnityGcAllocRecorder = config.EnableUnityProfilerRecorders && ReadBool(EnableUnityGcAllocRecorderEnvironmentVariable, config.EnableUnityGcAllocRecorder);

            string bindAddress = ReadCommandLineString("-diagnosticsBind", config.BindAddress);
            if (!string.IsNullOrEmpty(bindAddress))
            {
                config.BindAddress = bindAddress;
            }

            config.BufferSeconds = Mathf.Clamp(ReadCommandLineInt("-diagnosticsBufferSeconds", config.BufferSeconds), 10, 300);
            config.SampleIntervalSeconds = Mathf.Clamp(ReadCommandLineFloat("-diagnosticsSampleInterval", config.SampleIntervalSeconds), 0.1f, 2f);
            return config;
        }

#if UNITY_EDITOR
        public enum EditorMode
        {
            Default = 0,
            ForceEnabled = 1,
            ForceDisabled = 2
        }

        public static DiagnosticsConfig LoadEditorPrefsPreview()
        {
            DiagnosticsConfig config = new DiagnosticsConfig();
            ApplyEditorPrefs(config);
            string reason;
            config.IsEnabled = ResolveEnabled(out reason);
            config.StateReason = reason;
            return config;
        }

        public static EditorMode GetEditorMode()
        {
            if (EditorPrefs.GetBool(EditorPrefsDisabledKey, false))
            {
                return EditorMode.ForceDisabled;
            }

            if (EditorPrefs.GetBool(EditorPrefsEnabledKey, false))
            {
                return EditorMode.ForceEnabled;
            }

            return EditorMode.Default;
        }

        public static void SetEditorMode(EditorMode mode)
        {
            if (mode == EditorMode.ForceDisabled)
            {
                Enabled = false;
                EditorPrefs.SetBool(EditorPrefsDisabledKey, true);
                EditorPrefs.SetBool(EditorPrefsEnabledKey, false);
                return;
            }

            if (mode == EditorMode.ForceEnabled)
            {
                Enabled = true;
                EditorPrefs.SetBool(EditorPrefsDisabledKey, false);
                EditorPrefs.SetBool(EditorPrefsEnabledKey, true);
                return;
            }

            Enabled = false;
            EditorPrefs.DeleteKey(EditorPrefsDisabledKey);
            EditorPrefs.DeleteKey(EditorPrefsEnabledKey);
        }

        public static void SaveEditorPrefs(DiagnosticsConfig config)
        {
            if (config == null)
            {
                return;
            }

            WriteBool(nameof(EnableHttpServer), config.EnableHttpServer);
            WriteBool(nameof(EnableJsonl), config.EnableJsonl);
            WriteBool(nameof(EnableOverlay), config.EnableOverlay);
            WriteBool(nameof(ApplyEditorClientFramePacingGuard), config.ApplyEditorClientFramePacingGuard);
            WriteBool(nameof(ApplyEditorFocusedClientRefreshCap), config.ApplyEditorFocusedClientRefreshCap);
            WriteBool(nameof(ApplyEditorBackgroundPlayerLoopKeepAlive), config.ApplyEditorBackgroundPlayerLoopKeepAlive);
            WriteBool(nameof(ApplyEditorGcSmoothing), config.ApplyEditorGcSmoothing);
            WriteBool(nameof(EditorClientDisableVSync), config.EditorClientDisableVSync);
            WriteBool(nameof(EnableUnityProfilerRecorders), config.EnableUnityProfilerRecorders);
            WriteBool(nameof(EnableUnityGcAllocRecorder), config.EnableUnityGcAllocRecorder);
            WriteBool(nameof(AllowPortFallback), config.AllowPortFallback);
            WriteString(nameof(BindAddress), config.BindAddress);
            WriteInt(nameof(HttpPort), config.HttpPort);
            WriteInt(nameof(EditorClientTargetFrameRate), config.EditorClientTargetFrameRate);
            WriteInt(nameof(EditorServerTargetFrameRate), config.EditorServerTargetFrameRate);
            WriteInt(nameof(EditorServerRenderFrameInterval), config.EditorServerRenderFrameInterval);
            WriteInt(nameof(EditorGcIncrementalBudgetNanoseconds), config.EditorGcIncrementalBudgetNanoseconds);
            WriteInt(nameof(MaxPortFallbackAttempts), config.MaxPortFallbackAttempts);
            WriteString(nameof(Token), config.Token);
            WriteInt(nameof(BufferSeconds), config.BufferSeconds);
            WriteFloat(nameof(SampleIntervalSeconds), config.SampleIntervalSeconds);
            WriteFloat(nameof(JsonlMetricIntervalSeconds), config.JsonlMetricIntervalSeconds);
            WriteFloat(nameof(SpikeCooldownSeconds), config.SpikeCooldownSeconds);
            WriteFloat(nameof(SlowScopeLogThresholdMs), config.SlowScopeLogThresholdMs);
            WriteFloat(nameof(MinScopeSampleMs), config.MinScopeSampleMs);
            WriteFloat(nameof(MinEditorScopeSampleMs), config.MinEditorScopeSampleMs);
            WriteBool(nameof(TrackScopeAllocations), config.TrackScopeAllocations);
            WriteLong(nameof(MinScopeAllocatedBytes), config.MinScopeAllocatedBytes);
            WriteInt(nameof(MaxScopeSamples), config.MaxScopeSamples);
            WriteFloat(nameof(ClientFrameSpikeMs), config.ClientFrameSpikeMs);
            WriteFloat(nameof(ClientLowFps), config.ClientLowFps);
            WriteFloat(nameof(ClientGcSpikeMs), config.ClientGcSpikeMs);
            WriteFloat(nameof(PingSpikeMs), config.PingSpikeMs);
            WriteFloat(nameof(JitterSpikeMs), config.JitterSpikeMs);
            WriteFloat(nameof(PacketLossSpikePercent), config.PacketLossSpikePercent);
            WriteFloat(nameof(MemoryGrowthMbPerMinute), config.MemoryGrowthMbPerMinute);
            WriteFloat(nameof(ServerTickSpikeMs), config.ServerTickSpikeMs);
            WriteFloat(nameof(ServerTickMultiplier), config.ServerTickMultiplier);
            WriteFloat(nameof(RpcHandlerSpikeMs), config.RpcHandlerSpikeMs);
            WriteFloat(nameof(RpcStormCountPerSecond), config.RpcStormCountPerSecond);
            WriteFloat(nameof(NetworkMessageStormPerSecond), config.NetworkMessageStormPerSecond);
            WriteInt(nameof(EntityGrowthSpikeCount), config.EntityGrowthSpikeCount);
        }

        public static void ResetEditorPrefs()
        {
            Enabled = false;
            EditorPrefs.DeleteKey(EditorPrefsEnabledKey);
            EditorPrefs.DeleteKey(EditorPrefsDisabledKey);
            DeleteKey(nameof(EnableHttpServer));
            DeleteKey(nameof(EnableJsonl));
            DeleteKey(nameof(EnableOverlay));
            DeleteKey(nameof(ApplyEditorClientFramePacingGuard));
            DeleteKey(nameof(ApplyEditorFocusedClientRefreshCap));
            DeleteKey(nameof(ApplyEditorBackgroundPlayerLoopKeepAlive));
            DeleteKey(nameof(ApplyEditorGcSmoothing));
            DeleteKey(nameof(EditorClientDisableVSync));
            DeleteKey(nameof(EnableUnityProfilerRecorders));
            DeleteKey(nameof(EnableUnityGcAllocRecorder));
            DeleteKey(nameof(AllowPortFallback));
            DeleteKey(nameof(BindAddress));
            DeleteKey(nameof(HttpPort));
            DeleteKey(nameof(EditorClientTargetFrameRate));
            DeleteKey(nameof(EditorServerTargetFrameRate));
            DeleteKey(nameof(EditorServerRenderFrameInterval));
            DeleteKey(nameof(EditorGcIncrementalBudgetNanoseconds));
            DeleteKey(nameof(MaxPortFallbackAttempts));
            DeleteKey(nameof(Token));
            DeleteKey(nameof(BufferSeconds));
            DeleteKey(nameof(SampleIntervalSeconds));
            DeleteKey(nameof(JsonlMetricIntervalSeconds));
            DeleteKey(nameof(SpikeCooldownSeconds));
            DeleteKey(nameof(SlowScopeLogThresholdMs));
            DeleteKey(nameof(MinScopeSampleMs));
            DeleteKey(nameof(MinEditorScopeSampleMs));
            DeleteKey(nameof(TrackScopeAllocations));
            DeleteKey(nameof(MinScopeAllocatedBytes));
            DeleteKey(nameof(MaxScopeSamples));
            DeleteKey(nameof(ClientFrameSpikeMs));
            DeleteKey(nameof(ClientLowFps));
            DeleteKey(nameof(ClientGcSpikeMs));
            DeleteKey(nameof(PingSpikeMs));
            DeleteKey(nameof(JitterSpikeMs));
            DeleteKey(nameof(PacketLossSpikePercent));
            DeleteKey(nameof(MemoryGrowthMbPerMinute));
            DeleteKey(nameof(ServerTickSpikeMs));
            DeleteKey(nameof(ServerTickMultiplier));
            DeleteKey(nameof(RpcHandlerSpikeMs));
            DeleteKey(nameof(RpcStormCountPerSecond));
            DeleteKey(nameof(NetworkMessageStormPerSecond));
            DeleteKey(nameof(EntityGrowthSpikeCount));
        }

        private static void ApplyEditorPrefs(DiagnosticsConfig config)
        {
            config.EnableHttpServer = ReadEditorBool(nameof(EnableHttpServer), config.EnableHttpServer);
            config.EnableJsonl = ReadEditorBool(nameof(EnableJsonl), config.EnableJsonl);
            config.EnableOverlay = ReadEditorBool(nameof(EnableOverlay), config.EnableOverlay);
            config.ApplyEditorClientFramePacingGuard = ReadEditorBool(nameof(ApplyEditorClientFramePacingGuard), config.ApplyEditorClientFramePacingGuard);
            config.ApplyEditorFocusedClientRefreshCap = ReadEditorBool(nameof(ApplyEditorFocusedClientRefreshCap), config.ApplyEditorFocusedClientRefreshCap);
            config.ApplyEditorBackgroundPlayerLoopKeepAlive = ReadEditorBool(nameof(ApplyEditorBackgroundPlayerLoopKeepAlive), config.ApplyEditorBackgroundPlayerLoopKeepAlive);
            config.ApplyEditorGcSmoothing = ReadEditorBool(nameof(ApplyEditorGcSmoothing), config.ApplyEditorGcSmoothing);
            config.EditorClientDisableVSync = ReadEditorBool(nameof(EditorClientDisableVSync), config.EditorClientDisableVSync);
            config.EnableUnityProfilerRecorders = ReadEditorBool(nameof(EnableUnityProfilerRecorders), config.EnableUnityProfilerRecorders);
            config.EnableUnityGcAllocRecorder = ReadEditorBool(nameof(EnableUnityGcAllocRecorder), config.EnableUnityGcAllocRecorder);
            config.AllowPortFallback = ReadEditorBool(nameof(AllowPortFallback), config.AllowPortFallback);
            config.BindAddress = ReadEditorString(nameof(BindAddress), config.BindAddress);
            config.HttpPort = ReadEditorInt(nameof(HttpPort), config.HttpPort);
            config.EditorClientTargetFrameRate = ReadEditorTargetFrameRate(nameof(EditorClientTargetFrameRate), config.EditorClientTargetFrameRate);
            config.EditorServerTargetFrameRate = ReadEditorTargetFrameRate(nameof(EditorServerTargetFrameRate), config.EditorServerTargetFrameRate);
            config.EditorServerRenderFrameInterval = Mathf.Clamp(ReadEditorInt(nameof(EditorServerRenderFrameInterval), config.EditorServerRenderFrameInterval), 1, 120);
            config.EditorGcIncrementalBudgetNanoseconds = ReadEditorInt(nameof(EditorGcIncrementalBudgetNanoseconds), config.EditorGcIncrementalBudgetNanoseconds);
            config.MaxPortFallbackAttempts = ReadEditorInt(nameof(MaxPortFallbackAttempts), config.MaxPortFallbackAttempts);
            config.Token = ReadEditorString(nameof(Token), config.Token);
            config.BufferSeconds = ReadEditorInt(nameof(BufferSeconds), config.BufferSeconds);
            config.SampleIntervalSeconds = ReadEditorFloat(nameof(SampleIntervalSeconds), config.SampleIntervalSeconds);
            config.JsonlMetricIntervalSeconds = ReadEditorFloat(nameof(JsonlMetricIntervalSeconds), config.JsonlMetricIntervalSeconds);
            config.SpikeCooldownSeconds = ReadEditorFloat(nameof(SpikeCooldownSeconds), config.SpikeCooldownSeconds);
            config.SlowScopeLogThresholdMs = ReadEditorFloat(nameof(SlowScopeLogThresholdMs), config.SlowScopeLogThresholdMs);
            config.MinScopeSampleMs = ReadEditorFloat(nameof(MinScopeSampleMs), config.MinScopeSampleMs);
            config.MinEditorScopeSampleMs = ReadEditorFloat(nameof(MinEditorScopeSampleMs), config.MinEditorScopeSampleMs);
            config.TrackScopeAllocations = ReadEditorBool(nameof(TrackScopeAllocations), config.TrackScopeAllocations);
            config.MinScopeAllocatedBytes = ReadEditorLong(nameof(MinScopeAllocatedBytes), config.MinScopeAllocatedBytes);
            config.MaxScopeSamples = ReadEditorInt(nameof(MaxScopeSamples), config.MaxScopeSamples);
            config.ClientFrameSpikeMs = ReadEditorFloat(nameof(ClientFrameSpikeMs), config.ClientFrameSpikeMs);
            config.ClientLowFps = ReadEditorFloat(nameof(ClientLowFps), config.ClientLowFps);
            config.ClientGcSpikeMs = ReadEditorFloat(nameof(ClientGcSpikeMs), config.ClientGcSpikeMs);
            config.PingSpikeMs = ReadEditorFloat(nameof(PingSpikeMs), config.PingSpikeMs);
            config.JitterSpikeMs = ReadEditorFloat(nameof(JitterSpikeMs), config.JitterSpikeMs);
            config.PacketLossSpikePercent = ReadEditorFloat(nameof(PacketLossSpikePercent), config.PacketLossSpikePercent);
            config.MemoryGrowthMbPerMinute = ReadEditorFloat(nameof(MemoryGrowthMbPerMinute), config.MemoryGrowthMbPerMinute);
            config.ServerTickSpikeMs = ReadEditorFloat(nameof(ServerTickSpikeMs), config.ServerTickSpikeMs);
            config.ServerTickMultiplier = ReadEditorFloat(nameof(ServerTickMultiplier), config.ServerTickMultiplier);
            config.RpcHandlerSpikeMs = ReadEditorFloat(nameof(RpcHandlerSpikeMs), config.RpcHandlerSpikeMs);
            config.RpcStormCountPerSecond = ReadEditorFloat(nameof(RpcStormCountPerSecond), config.RpcStormCountPerSecond);
            config.NetworkMessageStormPerSecond = ReadEditorFloat(nameof(NetworkMessageStormPerSecond), config.NetworkMessageStormPerSecond);
            config.EntityGrowthSpikeCount = ReadEditorInt(nameof(EntityGrowthSpikeCount), config.EntityGrowthSpikeCount);
        }

        private static string BuildEditorKey(string name)
        {
            return EditorPrefsPrefix + name;
        }

        private static void DeleteKey(string name)
        {
            EditorPrefs.DeleteKey(BuildEditorKey(name));
        }

        private static bool ReadEditorBool(string name, bool fallback)
        {
            return EditorPrefs.GetBool(BuildEditorKey(name), fallback);
        }

        private static int ReadEditorInt(string name, int fallback)
        {
            return EditorPrefs.GetInt(BuildEditorKey(name), fallback);
        }

        private static int ReadEditorTargetFrameRate(string name, int fallback)
        {
            string key = BuildEditorKey(name);
            if (!EditorPrefs.HasKey(key))
            {
                return fallback;
            }

            int value = EditorPrefs.GetInt(key, fallback);
            if (value == 120 && fallback == DefaultEditorTargetFrameRate)
            {
                return DefaultEditorTargetFrameRate;
            }

            return value;
        }

        private static long ReadEditorLong(string name, long fallback)
        {
            string value = EditorPrefs.GetString(BuildEditorKey(name), fallback.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (long.TryParse(value, out long parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static float ReadEditorFloat(string name, float fallback)
        {
            return EditorPrefs.GetFloat(BuildEditorKey(name), fallback);
        }

        private static string ReadEditorString(string name, string fallback)
        {
            return EditorPrefs.GetString(BuildEditorKey(name), fallback);
        }

        private static void WriteBool(string name, bool value)
        {
            EditorPrefs.SetBool(BuildEditorKey(name), value);
        }

        private static void WriteInt(string name, int value)
        {
            EditorPrefs.SetInt(BuildEditorKey(name), value);
        }

        private static void WriteLong(string name, long value)
        {
            EditorPrefs.SetString(BuildEditorKey(name), value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static void WriteFloat(string name, float value)
        {
            EditorPrefs.SetFloat(BuildEditorKey(name), value);
        }

        private static void WriteString(string name, string value)
        {
            EditorPrefs.SetString(BuildEditorKey(name), value ?? string.Empty);
        }
#endif

        public bool RequiresToken()
        {
            return !string.IsNullOrEmpty(Token) || BindAddress != "127.0.0.1";
        }

        private static bool ResolveEnabled(out string reason)
        {
            if (ReadBool(DisableEnvironmentVariable, false))
            {
                reason = DisableEnvironmentVariable + "=true";
                return false;
            }

            string env = Environment.GetEnvironmentVariable(EnableEnvironmentVariable);
            if (!string.IsNullOrEmpty(env))
            {
                bool enabled = IsTruthy(env);
                reason = enabled ? EnableEnvironmentVariable + "=" + env : EnableEnvironmentVariable + " is set but not truthy";
                return enabled;
            }

            if (Enabled)
            {
                reason = "DiagnosticsConfig.Enabled=true";
                return true;
            }

            if (HasCommandLineFlag("-enableDiagnostics"))
            {
                reason = "-enableDiagnostics";
                return true;
            }

#if UNITY_EDITOR
            if (EditorPrefs.GetBool(EditorPrefsDisabledKey, false))
            {
                reason = "Unity Editor menu disabled diagnostics";
                return false;
            }

            if (EditorPrefs.GetBool(EditorPrefsEnabledKey, false))
            {
                reason = "Unity Editor menu setting";
                return true;
            }

            reason = "Unity Editor fallback";
            return true;
#else
            if (Debug.isDebugBuild)
            {
                reason = "Development Build";
                return true;
            }

            reason = "production build default";
            return false;
#endif
        }

        private static bool ReadBool(string name, bool fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            return IsTruthy(value);
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }

        private static int ReadInt(string name, int fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static string ReadString(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static bool HasCommandLineFlag(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ReadCommandLineInt(string flag, int fallback)
        {
            string value = ReadCommandLineString(flag, null);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            if (int.TryParse(value, out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static float ReadCommandLineFloat(string flag, float fallback)
        {
            string value = ReadCommandLineString(flag, null);
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static string ReadCommandLineString(string flag, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
