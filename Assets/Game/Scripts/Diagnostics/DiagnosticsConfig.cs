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
        public const string EditorClientTargetFpsEnvironmentVariable = "EDITOR_CLIENT_TARGET_FPS";
        public const string EditorServerTargetFpsEnvironmentVariable = "EDITOR_SERVER_TARGET_FPS";
        public const string DisableEditorGcSmoothingEnvironmentVariable = "DISABLE_EDITOR_GC_SMOOTHING";
        public const string EditorGcBudgetUsEnvironmentVariable = "EDITOR_GC_BUDGET_US";
        public const string EnableUnityProfilerRecordersEnvironmentVariable = "ENABLE_UNITY_PROFILER_RECORDERS";
        public const string EnableUnityGcAllocRecorderEnvironmentVariable = "ENABLE_UNITY_GC_ALLOC_RECORDER";
        public const string EditorPrefsEnabledKey = "WarOfMachines.Diagnostics.Enabled";
        public const string EditorPrefsDisabledKey = "WarOfMachines.Diagnostics.Disabled";

        public static bool Enabled;

        public bool IsEnabled;
        public string StateReason = string.Empty;
        public bool EnableHttpServer = true;
        public bool EnableJsonl = true;
        public bool EnableOverlay = true;
        public bool ApplyEditorClientFramePacingGuard = true;
        public bool ApplyEditorGcSmoothing = true;
        public bool EditorClientDisableVSync = true;
        public bool EnableUnityProfilerRecorders = false;
        public bool EnableUnityGcAllocRecorder = false;
        public bool AllowPortFallback = true;
        public string BindAddress = "127.0.0.1";
        public int HttpPort = 8765;
        public int EditorClientTargetFrameRate = 60;
        public int EditorServerTargetFrameRate = 60;
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
            string reason;
            config.IsEnabled = ResolveEnabled(out reason);
            config.StateReason = reason;
            config.HttpPort = ReadInt(PortEnvironmentVariable, ReadCommandLineInt("-diagnosticsPort", config.HttpPort));
            config.Token = ReadString(TokenEnvironmentVariable, ReadCommandLineString("-diagnosticsToken", config.Token));
            config.ApplyEditorClientFramePacingGuard = !ReadBool(DisableEditorClientFramePacingEnvironmentVariable, false);
            config.EditorClientTargetFrameRate = Mathf.Clamp(ReadInt(EditorClientTargetFpsEnvironmentVariable, ReadCommandLineInt("-editorClientTargetFps", config.EditorClientTargetFrameRate)), 30, 240);
            config.EditorServerTargetFrameRate = Mathf.Clamp(ReadInt(EditorServerTargetFpsEnvironmentVariable, ReadCommandLineInt("-editorServerTargetFps", config.EditorServerTargetFrameRate)), 30, 240);
            config.ApplyEditorGcSmoothing = !ReadBool(DisableEditorGcSmoothingEnvironmentVariable, false);
            int gcBudgetUs = Mathf.Clamp(ReadInt(EditorGcBudgetUsEnvironmentVariable, ReadCommandLineInt("-editorGcBudgetUs", config.EditorGcIncrementalBudgetNanoseconds / 1000)), 100, 5000);
            config.EditorGcIncrementalBudgetNanoseconds = gcBudgetUs * 1000;
            config.EnableUnityProfilerRecorders = ReadBool(EnableUnityProfilerRecordersEnvironmentVariable, false);
            config.EnableUnityGcAllocRecorder = config.EnableUnityProfilerRecorders && ReadBool(EnableUnityGcAllocRecorderEnvironmentVariable, false);

            string bindAddress = ReadCommandLineString("-diagnosticsBind", config.BindAddress);
            if (!string.IsNullOrEmpty(bindAddress))
            {
                config.BindAddress = bindAddress;
            }

            config.BufferSeconds = Mathf.Clamp(ReadCommandLineInt("-diagnosticsBufferSeconds", config.BufferSeconds), 10, 300);
            config.SampleIntervalSeconds = Mathf.Clamp(ReadCommandLineFloat("-diagnosticsSampleInterval", config.SampleIntervalSeconds), 0.1f, 2f);
            return config;
        }

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
