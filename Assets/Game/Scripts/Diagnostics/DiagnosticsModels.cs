using System.Collections.Generic;

namespace Game.Scripts.Diagnostics
{
    public static class DiagnosticsCategories
    {
        public const string Client = "client";
        public const string Server = "server";
        public const string Network = "network";
        public const string Rpc = "rpc";
        public const string Ui = "ui";
        public const string Physics = "physics";
        public const string Ai = "ai";
        public const string Render = "render";
        public const string Db = "db";
        public const string Editor = "editor";
        public const string Unknown = "unknown";
    }

    public sealed class DiagnosticsScopeSummary
    {
        public string Name;
        public string Category;
        public int Count;
        public double TotalMs;
        public double AvgMs;
        public double MaxMs;
        public double P95Ms;
        public long TotalAllocatedBytes;
        public long AvgAllocatedBytes;
        public long MaxAllocatedBytes;
    }

    public struct DiagnosticsScopeSample
    {
        public double TimeSeconds;
        public string Timestamp;
        public string Name;
        public string Category;
        public double DurationMs;
        public long AllocatedBytes;
    }

    public struct DiagnosticsEventSample
    {
        public double TimeSeconds;
        public string Timestamp;
        public string Name;
        public string Category;
        public int Count;
        public double TotalMs;
    }

    public sealed class DiagnosticsClientMetrics
    {
        public double? Fps;
        public double? FrameMs;
        public double? FrameMsP95_10s;
        public double? FrameMsMax_10s;
        public double? MemoryMb;
        public double? GcAllocatedBytesPerSecond;
        public double? GcSpikeMs;
        public int? GcCollectionCount;
        public int? ActiveVisibleEntities;
        public int? ActiveGameObjects;
        public int? ActiveEntities;
        public double? UiUpdateMs;
        public double? RenderMs;
        public double? PhysicsMs;
        public double? LocalSimulationMs;
        public double? MainThreadMs;
        public double? RenderThreadMs;
        public double? GfxWaitForPresentMs;
        public double? ScriptUpdateMs;
        public double? BehaviourUpdateMs;
        public double? LateUpdateMs;
        public double? FixedUpdateMs;
        public double? CameraRenderMs;
        public double? UiRenderMs;
        public long? GcAllocatedBytesInFrame;
        public double? IncomingMessagesPerSecond;
        public double? OutgoingMessagesPerSecond;
        public double? IncomingBytesPerSecond;
        public double? OutgoingBytesPerSecond;
        public double? PingMs;
        public double? JitterMs;
        public double? PacketLossPercent;
        public bool? ApplicationFocused;
        public bool? ApplicationRunInBackground;
        public int? ScreenWidth;
        public int? ScreenHeight;
        public string FullscreenMode;
        public int? QualityLevel;
        public string QualityName;
        public int? VSyncCount;
        public int? TargetFrameRate;
        public double? RefreshRate;
        public double? FixedDeltaTime;
        public double? MaximumDeltaTime;
        public double? TimeScale;
        public int? CaptureFramerate;
        public bool? EditorApplicationIsPlaying;
        public bool? EditorPaused;
        public bool? IsEditor;
        public List<DiagnosticsScopeSummary> TopSlowScopes1s = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopSlowScopes5s = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopSlowScopes10s = new List<DiagnosticsScopeSummary>();
    }

    public sealed class DiagnosticsServerMetrics
    {
        public double? ServerTickMs;
        public double? ServerTickMsP95_10s;
        public double? ServerTickMsMax_10s;
        public int? TickRate;
        public int? ActivePlayers;
        public int? ActiveEntities;
        public int? ActiveProjectiles;
        public int? ActiveNPCs;
        public double? PhysicsStepMs;
        public double? AiPathfindingMs;
        public double? VisibilityCalculationMs;
        public double? SnapshotSendMs;
        public double? RpcEventsPerSecond;
        public long? NetworkBytesInTotal;
        public long? NetworkBytesOutTotal;
        public double? MemoryMb;
        public double? GcAllocatedBytesPerSecond;
        public int? GcCollectionCount;
        public List<DiagnosticsScopeSummary> TopRpcEventsByCount = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopRpcEventsByTotalTime = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopRpcEventsByAverageTime = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsPerClientNetworkMetrics> NetworkBytesPerClient = new List<DiagnosticsPerClientNetworkMetrics>();
        public int? PendingPackets;
        public List<DiagnosticsScopeSummary> TopSlowScopes1s = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopSlowScopes5s = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopSlowScopes10s = new List<DiagnosticsScopeSummary>();
    }

    public sealed class DiagnosticsNetworkMetrics
    {
        public double? PingMs;
        public double? JitterMs;
        public double? PacketLossPercent;
        public double? IncomingMessagesPerSecond;
        public double? OutgoingMessagesPerSecond;
        public double? IncomingKbps;
        public double? OutgoingKbps;
        public double? IncomingBytesPerSecond;
        public double? OutgoingBytesPerSecond;
        public long IncomingBytesTotal;
        public long OutgoingBytesTotal;
        public int? PendingPackets;
    }

    public sealed class DiagnosticsPerClientNetworkMetrics
    {
        public string ClientIdHash;
        public long BytesInTotal;
        public long BytesOutTotal;
    }

    public sealed class DiagnosticsMetricSample
    {
        public double TimeSeconds;
        public string Timestamp;
        public string SessionId;
        public string Map;
        public string Mode;
        public DiagnosticsClientMetrics Client = new DiagnosticsClientMetrics();
        public DiagnosticsServerMetrics Server = new DiagnosticsServerMetrics();
        public DiagnosticsNetworkMetrics Network = new DiagnosticsNetworkMetrics();
    }

    public sealed class DiagnosticsSpike
    {
        public double TimeSeconds;
        public string Timestamp;
        public string Type;
        public string Severity;
        public string Domain;
        public string Summary;
        public string TopSuspect;
        public double? FrameMs;
        public double? ServerTickMs;
        public double? PingMs;
        public double? JitterMs;
        public double? PacketLossPercent;
        public int? ActivePlayers;
        public int? ActiveEntities;
        public string Map;
        public string Mode;
        public List<DiagnosticsScopeSummary> TopSlowScopes5s = new List<DiagnosticsScopeSummary>();
        public List<DiagnosticsScopeSummary> TopRpcEvents5s = new List<DiagnosticsScopeSummary>();
    }

    public sealed class DiagnosticsFrameSpike
    {
        public double TimeSeconds;
        public string Timestamp;
        public double FrameMs;
        public bool? ApplicationFocused;
        public int? ScreenWidth;
        public int? ScreenHeight;
        public string FullscreenMode;
        public int? GcCollectionCountBefore;
        public int? GcCollectionCountAfter;
        public long? GcAllocatedBytesInFrame;
        public double? MainThreadMs;
        public double? RenderThreadMs;
        public double? GfxWaitForPresentMs;
        public double? ScriptUpdateMs;
        public double? BehaviourUpdateMs;
        public double? LateUpdateMs;
        public double? FixedUpdateMs;
        public double? CameraRenderMs;
        public double? UiRenderMs;
        public List<DiagnosticsScopeSummary> TopSuspects = new List<DiagnosticsScopeSummary>();
    }

    public sealed class DiagnosticsSnapshot
    {
        public DiagnosticsMetricSample Current;
        public List<DiagnosticsSpike> Spikes = new List<DiagnosticsSpike>();
    }

    public sealed class DiagnosticsNetworkSummary
    {
        public int Seconds;
        public DiagnosticsNetworkMetrics Current;
        public double? AverageIncomingMessagesPerSecond;
        public double? AverageOutgoingMessagesPerSecond;
        public double? MaxIncomingMessagesPerSecond;
        public double? MaxOutgoingMessagesPerSecond;
        public double? AverageIncomingKbps;
        public double? AverageOutgoingKbps;
        public double? MaxPingMs;
        public double? MaxJitterMs;
        public double? MaxPacketLossPercent;
        public long IncomingBytesTotal;
        public long OutgoingBytesTotal;
        public List<DiagnosticsPerClientNetworkMetrics> PerClient = new List<DiagnosticsPerClientNetworkMetrics>();
    }

    public sealed class DiagnosticsSuspect
    {
        public string Name;
        public string Reason;
        public string Category;
        public double? AvgMs;
        public double? MaxMs;
        public string FileHint;
    }

    public sealed class DiagnosticsAnalysis
    {
        public string Classification;
        public double Confidence;
        public string Severity;
        public string Summary;
        public List<string> Evidence = new List<string>();
        public List<DiagnosticsSuspect> TopSuspects = new List<DiagnosticsSuspect>();
        public List<string> RecommendedNextSteps = new List<string>();
        public List<string> FilesToInspect = new List<string>();
    }
}
