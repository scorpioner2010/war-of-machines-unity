using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Scripts.Diagnostics
{
    public static class DiagnosticsJson
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string SnapshotToJson(DiagnosticsSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Current == null)
            {
                return "{}";
            }

            StringBuilder sb = new StringBuilder(2048);
            AppendMetricSample(sb, snapshot.Current, false);
            if (sb.Length > 0 && sb[sb.Length - 1] == '}')
            {
                sb.Length--;
            }

            sb.Append(",\"spikes\":");
            AppendSpikes(sb, snapshot.Spikes);
            sb.Append('}');
            return sb.ToString();
        }

        public static string MetricSampleToJson(DiagnosticsMetricSample sample)
        {
            StringBuilder sb = new StringBuilder(2048);
            AppendMetricSample(sb, sample, true);
            return sb.ToString();
        }

        public static string SamplesToJson(IList<DiagnosticsMetricSample> samples, int seconds)
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.Append("{\"seconds\":");
            sb.Append(seconds);
            sb.Append(",\"samples\":[");
            for (int i = 0; i < samples.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                AppendMetricSample(sb, samples[i], true);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        public static string SpikesToJson(IList<DiagnosticsSpike> spikes, int seconds)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append("{\"seconds\":");
            sb.Append(seconds);
            sb.Append(",\"spikes\":");
            AppendSpikes(sb, spikes);
            sb.Append('}');
            return sb.ToString();
        }

        public static string FrameSpikesToJson(IList<DiagnosticsFrameSpike> spikes, int seconds)
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.Append("{\"seconds\":");
            sb.Append(seconds);
            sb.Append(",\"frameSpikes\":");
            AppendFrameSpikes(sb, spikes);
            sb.Append('}');
            return sb.ToString();
        }

        public static string TopScopesToJson(string group, int seconds, IList<DiagnosticsScopeSummary> scopes)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append("{\"seconds\":");
            sb.Append(seconds);
            sb.Append(",\"group\":");
            AppendString(sb, group);
            sb.Append(",\"scopes\":");
            AppendScopeSummaries(sb, scopes);
            sb.Append('}');
            return sb.ToString();
        }

        public static string NetworkSummaryToJson(DiagnosticsNetworkSummary summary)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append("{\"seconds\":");
            sb.Append(summary != null ? summary.Seconds : 0);
            sb.Append(",\"network\":");
            AppendNetworkMetrics(sb, summary != null ? summary.Current : null);
            sb.Append(",\"averageIncomingMessagesPerSecond\":");
            AppendNullable(sb, summary != null ? summary.AverageIncomingMessagesPerSecond : null);
            sb.Append(",\"averageOutgoingMessagesPerSecond\":");
            AppendNullable(sb, summary != null ? summary.AverageOutgoingMessagesPerSecond : null);
            sb.Append(",\"maxIncomingMessagesPerSecond\":");
            AppendNullable(sb, summary != null ? summary.MaxIncomingMessagesPerSecond : null);
            sb.Append(",\"maxOutgoingMessagesPerSecond\":");
            AppendNullable(sb, summary != null ? summary.MaxOutgoingMessagesPerSecond : null);
            sb.Append(",\"averageIncomingKbps\":");
            AppendNullable(sb, summary != null ? summary.AverageIncomingKbps : null);
            sb.Append(",\"averageOutgoingKbps\":");
            AppendNullable(sb, summary != null ? summary.AverageOutgoingKbps : null);
            sb.Append(",\"maxPingMs\":");
            AppendNullable(sb, summary != null ? summary.MaxPingMs : null);
            sb.Append(",\"maxJitterMs\":");
            AppendNullable(sb, summary != null ? summary.MaxJitterMs : null);
            sb.Append(",\"maxPacketLossPercent\":");
            AppendNullable(sb, summary != null ? summary.MaxPacketLossPercent : null);
            sb.Append(",\"incomingBytesTotal\":");
            sb.Append(summary != null ? summary.IncomingBytesTotal : 0L);
            sb.Append(",\"outgoingBytesTotal\":");
            sb.Append(summary != null ? summary.OutgoingBytesTotal : 0L);
            sb.Append(",\"perClient\":");
            AppendPerClient(sb, summary != null ? summary.PerClient : null);
            sb.Append('}');
            return sb.ToString();
        }

        public static string AnalysisToJson(DiagnosticsAnalysis analysis)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append('{');
            sb.Append("\"classification\":");
            AppendString(sb, analysis != null ? analysis.Classification : "UNKNOWN");
            sb.Append(",\"confidence\":");
            AppendNumber(sb, analysis != null ? analysis.Confidence : 0d);
            sb.Append(",\"severity\":");
            AppendString(sb, analysis != null ? analysis.Severity : "low");
            sb.Append(",\"summary\":");
            AppendString(sb, analysis != null ? analysis.Summary : "Diagnostics data is unavailable.");
            sb.Append(",\"evidence\":");
            AppendStringArray(sb, analysis != null ? analysis.Evidence : null);
            sb.Append(",\"topSuspects\":");
            AppendSuspects(sb, analysis != null ? analysis.TopSuspects : null);
            sb.Append(",\"recommendedNextSteps\":");
            AppendStringArray(sb, analysis != null ? analysis.RecommendedNextSteps : null);
            sb.Append(",\"filesToInspect\":");
            AppendStringArray(sb, analysis != null ? analysis.FilesToInspect : null);
            sb.Append('}');
            return sb.ToString();
        }

        public static string HealthToJson(bool ok, bool enabled, double uptimeSeconds, int bufferSeconds, string sessionId, string logPath, string bindAddress, int httpPort)
        {
            StringBuilder sb = new StringBuilder(512);
            sb.Append("{\"ok\":");
            sb.Append(ok ? "true" : "false");
            sb.Append(",\"diagnosticsEnabled\":");
            sb.Append(enabled ? "true" : "false");
            sb.Append(",\"uptimeSeconds\":");
            AppendNumber(sb, uptimeSeconds);
            sb.Append(",\"bufferSeconds\":");
            sb.Append(bufferSeconds);
            sb.Append(",\"sessionId\":");
            AppendString(sb, sessionId);
            sb.Append(",\"logPath\":");
            AppendString(sb, logPath);
            sb.Append(",\"bindAddress\":");
            AppendString(sb, bindAddress);
            sb.Append(",\"httpPort\":");
            sb.Append(httpPort);
            sb.Append('}');
            return sb.ToString();
        }

        public static string JsonlMetricEvent(DiagnosticsMetricSample sample)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append("{\"type\":\"metric_sample\",\"timestamp\":");
            AppendString(sb, sample != null ? sample.Timestamp : string.Empty);
            sb.Append(",\"sample\":");
            AppendMetricSampleForJsonl(sb, sample);
            sb.Append('}');
            return sb.ToString();
        }

        public static string JsonlSpikeEvent(DiagnosticsSpike spike, DiagnosticsSnapshot snapshot)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append("{\"type\":\"spike\",\"timestamp\":");
            AppendString(sb, spike != null ? spike.Timestamp : string.Empty);
            sb.Append(",\"spike\":");
            AppendSpike(sb, spike);
            sb.Append(",\"snapshot\":");
            sb.Append(SnapshotToJson(snapshot));
            sb.Append('}');
            return sb.ToString();
        }

        public static string JsonlScopeEvent(DiagnosticsScopeSample sample)
        {
            StringBuilder sb = new StringBuilder(512);
            sb.Append("{\"type\":\"scope\",\"timestamp\":");
            AppendString(sb, sample.Timestamp);
            sb.Append(",\"name\":");
            AppendString(sb, sample.Name);
            sb.Append(",\"durationMs\":");
            AppendNumber(sb, sample.DurationMs);
            sb.Append(",\"allocatedBytes\":");
            sb.Append(sample.AllocatedBytes);
            sb.Append(",\"category\":");
            AppendString(sb, sample.Category);
            sb.Append('}');
            return sb.ToString();
        }

        public static string JsonlFrameSpikeEvent(DiagnosticsFrameSpike spike)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{\"type\":\"frame_spike\",\"timestamp\":");
            AppendString(sb, spike != null ? spike.Timestamp : string.Empty);
            sb.Append(",\"frameSpike\":");
            AppendFrameSpike(sb, spike);
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendMetricSample(StringBuilder sb, DiagnosticsMetricSample sample, bool includeTimeSeconds)
        {
            if (sample == null)
            {
                sb.Append("{}");
                return;
            }

            sb.Append('{');
            sb.Append("\"timestamp\":");
            AppendString(sb, sample.Timestamp);
            if (includeTimeSeconds)
            {
                sb.Append(",\"timeSeconds\":");
                AppendNumber(sb, sample.TimeSeconds);
            }

            sb.Append(",\"sessionId\":");
            AppendString(sb, sample.SessionId);
            sb.Append(",\"map\":");
            AppendString(sb, sample.Map);
            sb.Append(",\"mode\":");
            AppendString(sb, sample.Mode);
            sb.Append(",\"client\":");
            AppendClientMetrics(sb, sample.Client);
            sb.Append(",\"server\":");
            AppendServerMetrics(sb, sample.Server);
            sb.Append(",\"network\":");
            AppendNetworkMetrics(sb, sample.Network);
            sb.Append('}');
        }

        private static void AppendMetricSampleForJsonl(StringBuilder sb, DiagnosticsMetricSample sample)
        {
            if (sample == null)
            {
                sb.Append("{}");
                return;
            }

            sb.Append('{');
            sb.Append("\"timestamp\":");
            AppendString(sb, sample.Timestamp);
            sb.Append(",\"timeSeconds\":");
            AppendNumber(sb, sample.TimeSeconds);
            sb.Append(",\"sessionId\":");
            AppendString(sb, sample.SessionId);
            sb.Append(",\"map\":");
            AppendString(sb, sample.Map);
            sb.Append(",\"mode\":");
            AppendString(sb, sample.Mode);
            sb.Append(",\"client\":");
            AppendClientMetricsForJsonl(sb, sample.Client);
            sb.Append(",\"server\":");
            AppendServerMetricsForJsonl(sb, sample.Server);
            sb.Append(",\"network\":");
            AppendNetworkMetrics(sb, sample.Network);
            sb.Append('}');
        }

        private static void AppendClientMetricsForJsonl(StringBuilder sb, DiagnosticsClientMetrics metrics)
        {
            sb.Append('{');
            AppendField(sb, "fps", metrics != null ? metrics.Fps : null, true);
            AppendField(sb, "frameMs", metrics != null ? metrics.FrameMs : null, false);
            AppendField(sb, "frameMsP95_10s", metrics != null ? metrics.FrameMsP95_10s : null, false);
            AppendField(sb, "frameMsMax_10s", metrics != null ? metrics.FrameMsMax_10s : null, false);
            AppendField(sb, "memoryMb", metrics != null ? metrics.MemoryMb : null, false);
            AppendField(sb, "gcAllocatedBytesPerSecond", metrics != null ? metrics.GcAllocatedBytesPerSecond : null, false);
            AppendField(sb, "gcSpikeMs", metrics != null ? metrics.GcSpikeMs : null, false);
            AppendField(sb, "gcCollectionCount", metrics != null ? metrics.GcCollectionCount : null, false);
            AppendField(sb, "activeVisibleEntities", metrics != null ? metrics.ActiveVisibleEntities : null, false);
            AppendField(sb, "activeGameObjects", metrics != null ? metrics.ActiveGameObjects : null, false);
            AppendField(sb, "activeEntities", metrics != null ? metrics.ActiveEntities : null, false);
            AppendField(sb, "uiUpdateMs", metrics != null ? metrics.UiUpdateMs : null, false);
            AppendField(sb, "renderMs", metrics != null ? metrics.RenderMs : null, false);
            AppendField(sb, "physicsMs", metrics != null ? metrics.PhysicsMs : null, false);
            AppendField(sb, "localSimulationMs", metrics != null ? metrics.LocalSimulationMs : null, false);
            AppendField(sb, "mainThreadMs", metrics != null ? metrics.MainThreadMs : null, false);
            AppendField(sb, "renderThreadMs", metrics != null ? metrics.RenderThreadMs : null, false);
            AppendField(sb, "gfxWaitForPresentMs", metrics != null ? metrics.GfxWaitForPresentMs : null, false);
            AppendField(sb, "scriptUpdateMs", metrics != null ? metrics.ScriptUpdateMs : null, false);
            AppendField(sb, "behaviourUpdateMs", metrics != null ? metrics.BehaviourUpdateMs : null, false);
            AppendField(sb, "lateUpdateMs", metrics != null ? metrics.LateUpdateMs : null, false);
            AppendField(sb, "fixedUpdateMs", metrics != null ? metrics.FixedUpdateMs : null, false);
            AppendField(sb, "cameraRenderMs", metrics != null ? metrics.CameraRenderMs : null, false);
            AppendField(sb, "uiRenderMs", metrics != null ? metrics.UiRenderMs : null, false);
            AppendField(sb, "gcAllocatedBytesInFrame", metrics != null ? metrics.GcAllocatedBytesInFrame : null, false);
            AppendField(sb, "incomingMessagesPerSecond", metrics != null ? metrics.IncomingMessagesPerSecond : null, false);
            AppendField(sb, "outgoingMessagesPerSecond", metrics != null ? metrics.OutgoingMessagesPerSecond : null, false);
            AppendField(sb, "incomingBytesPerSecond", metrics != null ? metrics.IncomingBytesPerSecond : null, false);
            AppendField(sb, "outgoingBytesPerSecond", metrics != null ? metrics.OutgoingBytesPerSecond : null, false);
            AppendField(sb, "pingMs", metrics != null ? metrics.PingMs : null, false);
            AppendField(sb, "jitterMs", metrics != null ? metrics.JitterMs : null, false);
            AppendField(sb, "packetLossPercent", metrics != null ? metrics.PacketLossPercent : null, false);
            AppendField(sb, "applicationFocused", metrics != null ? metrics.ApplicationFocused : null, false);
            AppendField(sb, "applicationRunInBackground", metrics != null ? metrics.ApplicationRunInBackground : null, false);
            AppendField(sb, "screenWidth", metrics != null ? metrics.ScreenWidth : null, false);
            AppendField(sb, "screenHeight", metrics != null ? metrics.ScreenHeight : null, false);
            AppendField(sb, "fullscreenMode", metrics != null ? metrics.FullscreenMode : null, false);
            AppendField(sb, "qualityLevel", metrics != null ? metrics.QualityLevel : null, false);
            AppendField(sb, "qualityName", metrics != null ? metrics.QualityName : null, false);
            AppendField(sb, "vSyncCount", metrics != null ? metrics.VSyncCount : null, false);
            AppendField(sb, "targetFrameRate", metrics != null ? metrics.TargetFrameRate : null, false);
            AppendField(sb, "refreshRate", metrics != null ? metrics.RefreshRate : null, false);
            AppendField(sb, "fixedDeltaTime", metrics != null ? metrics.FixedDeltaTime : null, false);
            AppendField(sb, "maximumDeltaTime", metrics != null ? metrics.MaximumDeltaTime : null, false);
            AppendField(sb, "timeScale", metrics != null ? metrics.TimeScale : null, false);
            AppendField(sb, "captureFramerate", metrics != null ? metrics.CaptureFramerate : null, false);
            AppendField(sb, "editorApplicationIsPlaying", metrics != null ? metrics.EditorApplicationIsPlaying : null, false);
            AppendField(sb, "editorPaused", metrics != null ? metrics.EditorPaused : null, false);
            AppendField(sb, "isEditor", metrics != null ? metrics.IsEditor : null, false);
            sb.Append('}');
        }

        private static void AppendServerMetricsForJsonl(StringBuilder sb, DiagnosticsServerMetrics metrics)
        {
            sb.Append('{');
            AppendField(sb, "serverTickMs", metrics != null ? metrics.ServerTickMs : null, true);
            AppendField(sb, "serverTickMsP95_10s", metrics != null ? metrics.ServerTickMsP95_10s : null, false);
            AppendField(sb, "serverTickMsMax_10s", metrics != null ? metrics.ServerTickMsMax_10s : null, false);
            AppendField(sb, "tickRate", metrics != null ? metrics.TickRate : null, false);
            AppendField(sb, "activePlayers", metrics != null ? metrics.ActivePlayers : null, false);
            AppendField(sb, "activeEntities", metrics != null ? metrics.ActiveEntities : null, false);
            AppendField(sb, "activeProjectiles", metrics != null ? metrics.ActiveProjectiles : null, false);
            AppendField(sb, "activeNPCs", metrics != null ? metrics.ActiveNPCs : null, false);
            AppendField(sb, "physicsStepMs", metrics != null ? metrics.PhysicsStepMs : null, false);
            AppendField(sb, "aiPathfindingMs", metrics != null ? metrics.AiPathfindingMs : null, false);
            AppendField(sb, "visibilityCalculationMs", metrics != null ? metrics.VisibilityCalculationMs : null, false);
            AppendField(sb, "snapshotSendMs", metrics != null ? metrics.SnapshotSendMs : null, false);
            AppendField(sb, "rpcEventsPerSecond", metrics != null ? metrics.RpcEventsPerSecond : null, false);
            AppendField(sb, "networkBytesInTotal", metrics != null ? metrics.NetworkBytesInTotal : null, false);
            AppendField(sb, "networkBytesOutTotal", metrics != null ? metrics.NetworkBytesOutTotal : null, false);
            AppendField(sb, "memoryMb", metrics != null ? metrics.MemoryMb : null, false);
            AppendField(sb, "gcAllocatedBytesPerSecond", metrics != null ? metrics.GcAllocatedBytesPerSecond : null, false);
            AppendField(sb, "gcCollectionCount", metrics != null ? metrics.GcCollectionCount : null, false);
            AppendField(sb, "pendingPackets", metrics != null ? metrics.PendingPackets : null, false);
            sb.Append('}');
        }

        private static void AppendClientMetrics(StringBuilder sb, DiagnosticsClientMetrics metrics)
        {
            sb.Append('{');
            AppendField(sb, "fps", metrics != null ? metrics.Fps : null, true);
            AppendField(sb, "frameMs", metrics != null ? metrics.FrameMs : null, false);
            AppendField(sb, "frameMsP95_10s", metrics != null ? metrics.FrameMsP95_10s : null, false);
            AppendField(sb, "frameMsMax_10s", metrics != null ? metrics.FrameMsMax_10s : null, false);
            AppendField(sb, "memoryMb", metrics != null ? metrics.MemoryMb : null, false);
            AppendField(sb, "gcAllocatedBytesPerSecond", metrics != null ? metrics.GcAllocatedBytesPerSecond : null, false);
            AppendField(sb, "gcSpikeMs", metrics != null ? metrics.GcSpikeMs : null, false);
            AppendField(sb, "gcCollectionCount", metrics != null ? metrics.GcCollectionCount : null, false);
            AppendField(sb, "activeVisibleEntities", metrics != null ? metrics.ActiveVisibleEntities : null, false);
            AppendField(sb, "activeGameObjects", metrics != null ? metrics.ActiveGameObjects : null, false);
            AppendField(sb, "activeEntities", metrics != null ? metrics.ActiveEntities : null, false);
            AppendField(sb, "uiUpdateMs", metrics != null ? metrics.UiUpdateMs : null, false);
            AppendField(sb, "renderMs", metrics != null ? metrics.RenderMs : null, false);
            AppendField(sb, "physicsMs", metrics != null ? metrics.PhysicsMs : null, false);
            AppendField(sb, "localSimulationMs", metrics != null ? metrics.LocalSimulationMs : null, false);
            AppendField(sb, "mainThreadMs", metrics != null ? metrics.MainThreadMs : null, false);
            AppendField(sb, "renderThreadMs", metrics != null ? metrics.RenderThreadMs : null, false);
            AppendField(sb, "gfxWaitForPresentMs", metrics != null ? metrics.GfxWaitForPresentMs : null, false);
            AppendField(sb, "scriptUpdateMs", metrics != null ? metrics.ScriptUpdateMs : null, false);
            AppendField(sb, "behaviourUpdateMs", metrics != null ? metrics.BehaviourUpdateMs : null, false);
            AppendField(sb, "lateUpdateMs", metrics != null ? metrics.LateUpdateMs : null, false);
            AppendField(sb, "fixedUpdateMs", metrics != null ? metrics.FixedUpdateMs : null, false);
            AppendField(sb, "cameraRenderMs", metrics != null ? metrics.CameraRenderMs : null, false);
            AppendField(sb, "uiRenderMs", metrics != null ? metrics.UiRenderMs : null, false);
            AppendField(sb, "gcAllocatedBytesInFrame", metrics != null ? metrics.GcAllocatedBytesInFrame : null, false);
            AppendField(sb, "incomingMessagesPerSecond", metrics != null ? metrics.IncomingMessagesPerSecond : null, false);
            AppendField(sb, "outgoingMessagesPerSecond", metrics != null ? metrics.OutgoingMessagesPerSecond : null, false);
            AppendField(sb, "incomingBytesPerSecond", metrics != null ? metrics.IncomingBytesPerSecond : null, false);
            AppendField(sb, "outgoingBytesPerSecond", metrics != null ? metrics.OutgoingBytesPerSecond : null, false);
            AppendField(sb, "pingMs", metrics != null ? metrics.PingMs : null, false);
            AppendField(sb, "jitterMs", metrics != null ? metrics.JitterMs : null, false);
            AppendField(sb, "packetLossPercent", metrics != null ? metrics.PacketLossPercent : null, false);
            AppendField(sb, "applicationFocused", metrics != null ? metrics.ApplicationFocused : null, false);
            AppendField(sb, "applicationRunInBackground", metrics != null ? metrics.ApplicationRunInBackground : null, false);
            AppendField(sb, "screenWidth", metrics != null ? metrics.ScreenWidth : null, false);
            AppendField(sb, "screenHeight", metrics != null ? metrics.ScreenHeight : null, false);
            AppendField(sb, "fullscreenMode", metrics != null ? metrics.FullscreenMode : null, false);
            AppendField(sb, "qualityLevel", metrics != null ? metrics.QualityLevel : null, false);
            AppendField(sb, "qualityName", metrics != null ? metrics.QualityName : null, false);
            AppendField(sb, "vSyncCount", metrics != null ? metrics.VSyncCount : null, false);
            AppendField(sb, "targetFrameRate", metrics != null ? metrics.TargetFrameRate : null, false);
            AppendField(sb, "refreshRate", metrics != null ? metrics.RefreshRate : null, false);
            AppendField(sb, "fixedDeltaTime", metrics != null ? metrics.FixedDeltaTime : null, false);
            AppendField(sb, "maximumDeltaTime", metrics != null ? metrics.MaximumDeltaTime : null, false);
            AppendField(sb, "timeScale", metrics != null ? metrics.TimeScale : null, false);
            AppendField(sb, "captureFramerate", metrics != null ? metrics.CaptureFramerate : null, false);
            AppendField(sb, "editorApplicationIsPlaying", metrics != null ? metrics.EditorApplicationIsPlaying : null, false);
            AppendField(sb, "editorPaused", metrics != null ? metrics.EditorPaused : null, false);
            AppendField(sb, "isEditor", metrics != null ? metrics.IsEditor : null, false);
            sb.Append(",\"topSlowScopes_1s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes1s : null);
            sb.Append(",\"topSlowScopes_5s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes5s : null);
            sb.Append(",\"topSlowScopes_10s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes10s : null);
            sb.Append('}');
        }

        private static void AppendFrameSpikes(StringBuilder sb, IList<DiagnosticsFrameSpike> spikes)
        {
            sb.Append('[');
            if (spikes != null)
            {
                for (int i = 0; i < spikes.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    AppendFrameSpike(sb, spikes[i]);
                }
            }

            sb.Append(']');
        }

        private static void AppendFrameSpike(StringBuilder sb, DiagnosticsFrameSpike spike)
        {
            if (spike == null)
            {
                sb.Append("{}");
                return;
            }

            sb.Append('{');
            sb.Append("\"timestamp\":");
            AppendString(sb, spike.Timestamp);
            AppendField(sb, "frameMs", spike.FrameMs, false);
            AppendField(sb, "focused", spike.ApplicationFocused, false);
            AppendField(sb, "screenWidth", spike.ScreenWidth, false);
            AppendField(sb, "screenHeight", spike.ScreenHeight, false);
            AppendField(sb, "fullscreenMode", spike.FullscreenMode, false);
            AppendField(sb, "gcCollectionCountBefore", spike.GcCollectionCountBefore, false);
            AppendField(sb, "gcCollectionCountAfter", spike.GcCollectionCountAfter, false);
            AppendField(sb, "gcAllocatedBytesInFrame", spike.GcAllocatedBytesInFrame, false);
            AppendField(sb, "mainThreadMs", spike.MainThreadMs, false);
            AppendField(sb, "renderThreadMs", spike.RenderThreadMs, false);
            AppendField(sb, "gfxWaitForPresentMs", spike.GfxWaitForPresentMs, false);
            AppendField(sb, "scriptUpdateMs", spike.ScriptUpdateMs, false);
            AppendField(sb, "behaviourUpdateMs", spike.BehaviourUpdateMs, false);
            AppendField(sb, "lateUpdateMs", spike.LateUpdateMs, false);
            AppendField(sb, "fixedUpdateMs", spike.FixedUpdateMs, false);
            AppendField(sb, "cameraRenderMs", spike.CameraRenderMs, false);
            AppendField(sb, "uiRenderMs", spike.UiRenderMs, false);
            sb.Append(",\"topSuspects\":");
            AppendScopeSummaries(sb, spike.TopSuspects);
            sb.Append('}');
        }

        private static void AppendServerMetrics(StringBuilder sb, DiagnosticsServerMetrics metrics)
        {
            sb.Append('{');
            AppendField(sb, "serverTickMs", metrics != null ? metrics.ServerTickMs : null, true);
            AppendField(sb, "serverTickMsP95_10s", metrics != null ? metrics.ServerTickMsP95_10s : null, false);
            AppendField(sb, "serverTickMsMax_10s", metrics != null ? metrics.ServerTickMsMax_10s : null, false);
            AppendField(sb, "tickRate", metrics != null ? metrics.TickRate : null, false);
            AppendField(sb, "activePlayers", metrics != null ? metrics.ActivePlayers : null, false);
            AppendField(sb, "activeEntities", metrics != null ? metrics.ActiveEntities : null, false);
            AppendField(sb, "activeProjectiles", metrics != null ? metrics.ActiveProjectiles : null, false);
            AppendField(sb, "activeNPCs", metrics != null ? metrics.ActiveNPCs : null, false);
            AppendField(sb, "physicsStepMs", metrics != null ? metrics.PhysicsStepMs : null, false);
            AppendField(sb, "aiPathfindingMs", metrics != null ? metrics.AiPathfindingMs : null, false);
            AppendField(sb, "visibilityCalculationMs", metrics != null ? metrics.VisibilityCalculationMs : null, false);
            AppendField(sb, "snapshotSendMs", metrics != null ? metrics.SnapshotSendMs : null, false);
            AppendField(sb, "rpcEventsPerSecond", metrics != null ? metrics.RpcEventsPerSecond : null, false);
            AppendField(sb, "networkBytesInTotal", metrics != null ? metrics.NetworkBytesInTotal : null, false);
            AppendField(sb, "networkBytesOutTotal", metrics != null ? metrics.NetworkBytesOutTotal : null, false);
            AppendField(sb, "memoryMb", metrics != null ? metrics.MemoryMb : null, false);
            AppendField(sb, "gcAllocatedBytesPerSecond", metrics != null ? metrics.GcAllocatedBytesPerSecond : null, false);
            AppendField(sb, "gcCollectionCount", metrics != null ? metrics.GcCollectionCount : null, false);
            AppendField(sb, "pendingPackets", metrics != null ? metrics.PendingPackets : null, false);
            sb.Append(",\"topRpcEventsByCount\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopRpcEventsByCount : null);
            sb.Append(",\"topRpcEventsByTotalTime\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopRpcEventsByTotalTime : null);
            sb.Append(",\"topRpcEventsByAverageTime\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopRpcEventsByAverageTime : null);
            sb.Append(",\"networkBytesPerClient\":");
            AppendPerClient(sb, metrics != null ? metrics.NetworkBytesPerClient : null);
            sb.Append(",\"topSlowScopes_1s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes1s : null);
            sb.Append(",\"topSlowScopes_5s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes5s : null);
            sb.Append(",\"topSlowScopes_10s\":");
            AppendScopeSummaries(sb, metrics != null ? metrics.TopSlowScopes10s : null);
            sb.Append('}');
        }

        private static void AppendNetworkMetrics(StringBuilder sb, DiagnosticsNetworkMetrics metrics)
        {
            sb.Append('{');
            AppendField(sb, "pingMs", metrics != null ? metrics.PingMs : null, true);
            AppendField(sb, "jitterMs", metrics != null ? metrics.JitterMs : null, false);
            AppendField(sb, "packetLossPercent", metrics != null ? metrics.PacketLossPercent : null, false);
            AppendField(sb, "incomingMessagesPerSecond", metrics != null ? metrics.IncomingMessagesPerSecond : null, false);
            AppendField(sb, "outgoingMessagesPerSecond", metrics != null ? metrics.OutgoingMessagesPerSecond : null, false);
            AppendField(sb, "incomingKbps", metrics != null ? metrics.IncomingKbps : null, false);
            AppendField(sb, "outgoingKbps", metrics != null ? metrics.OutgoingKbps : null, false);
            AppendField(sb, "incomingBytesPerSecond", metrics != null ? metrics.IncomingBytesPerSecond : null, false);
            AppendField(sb, "outgoingBytesPerSecond", metrics != null ? metrics.OutgoingBytesPerSecond : null, false);
            sb.Append(",\"incomingBytesTotal\":");
            sb.Append(metrics != null ? metrics.IncomingBytesTotal : 0L);
            sb.Append(",\"outgoingBytesTotal\":");
            sb.Append(metrics != null ? metrics.OutgoingBytesTotal : 0L);
            AppendField(sb, "pendingPackets", metrics != null ? metrics.PendingPackets : null, false);
            sb.Append('}');
        }

        private static void AppendSpikes(StringBuilder sb, IList<DiagnosticsSpike> spikes)
        {
            sb.Append('[');
            if (spikes != null)
            {
                for (int i = 0; i < spikes.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    AppendSpike(sb, spikes[i]);
                }
            }

            sb.Append(']');
        }

        private static void AppendSpike(StringBuilder sb, DiagnosticsSpike spike)
        {
            if (spike == null)
            {
                sb.Append("{}");
                return;
            }

            sb.Append('{');
            sb.Append("\"timestamp\":");
            AppendString(sb, spike.Timestamp);
            sb.Append(",\"type\":");
            AppendString(sb, spike.Type);
            sb.Append(",\"severity\":");
            AppendString(sb, spike.Severity);
            sb.Append(",\"domain\":");
            AppendString(sb, spike.Domain);
            sb.Append(",\"summary\":");
            AppendString(sb, spike.Summary);
            sb.Append(",\"topSuspect\":");
            AppendString(sb, spike.TopSuspect);
            AppendField(sb, "frameMs", spike.FrameMs, false);
            AppendField(sb, "serverTickMs", spike.ServerTickMs, false);
            AppendField(sb, "pingMs", spike.PingMs, false);
            AppendField(sb, "jitterMs", spike.JitterMs, false);
            AppendField(sb, "packetLossPercent", spike.PacketLossPercent, false);
            AppendField(sb, "activePlayers", spike.ActivePlayers, false);
            AppendField(sb, "activeEntities", spike.ActiveEntities, false);
            sb.Append(",\"map\":");
            AppendString(sb, spike.Map);
            sb.Append(",\"mode\":");
            AppendString(sb, spike.Mode);
            sb.Append(",\"topSlowScopes_5s\":");
            AppendScopeSummaries(sb, spike.TopSlowScopes5s);
            sb.Append(",\"topRpcEvents_5s\":");
            AppendScopeSummaries(sb, spike.TopRpcEvents5s);
            sb.Append('}');
        }

        private static void AppendScopeSummaries(StringBuilder sb, IList<DiagnosticsScopeSummary> scopes)
        {
            sb.Append('[');
            if (scopes != null)
            {
                for (int i = 0; i < scopes.Count; i++)
                {
                    DiagnosticsScopeSummary scope = scopes[i];
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append('{');
                    sb.Append("\"name\":");
                    AppendString(sb, scope.Name);
                    sb.Append(",\"category\":");
                    AppendString(sb, scope.Category);
                    sb.Append(",\"count\":");
                    sb.Append(scope.Count);
                    sb.Append(",\"totalMs\":");
                    AppendNumber(sb, scope.TotalMs);
                    sb.Append(",\"avgMs\":");
                    AppendNumber(sb, scope.AvgMs);
                    sb.Append(",\"maxMs\":");
                    AppendNumber(sb, scope.MaxMs);
                    sb.Append(",\"p95Ms\":");
                    AppendNumber(sb, scope.P95Ms);
                    sb.Append(",\"totalAllocatedBytes\":");
                    sb.Append(scope.TotalAllocatedBytes);
                    sb.Append(",\"avgAllocatedBytes\":");
                    sb.Append(scope.AvgAllocatedBytes);
                    sb.Append(",\"maxAllocatedBytes\":");
                    sb.Append(scope.MaxAllocatedBytes);
                    sb.Append('}');
                }
            }

            sb.Append(']');
        }

        private static void AppendPerClient(StringBuilder sb, IList<DiagnosticsPerClientNetworkMetrics> perClient)
        {
            sb.Append('[');
            if (perClient != null)
            {
                for (int i = 0; i < perClient.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    DiagnosticsPerClientNetworkMetrics item = perClient[i];
                    sb.Append('{');
                    sb.Append("\"clientIdHash\":");
                    AppendString(sb, item.ClientIdHash);
                    sb.Append(",\"bytesInTotal\":");
                    sb.Append(item.BytesInTotal);
                    sb.Append(",\"bytesOutTotal\":");
                    sb.Append(item.BytesOutTotal);
                    sb.Append('}');
                }
            }

            sb.Append(']');
        }

        private static void AppendSuspects(StringBuilder sb, IList<DiagnosticsSuspect> suspects)
        {
            sb.Append('[');
            if (suspects != null)
            {
                for (int i = 0; i < suspects.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    DiagnosticsSuspect suspect = suspects[i];
                    sb.Append('{');
                    sb.Append("\"name\":");
                    AppendString(sb, suspect.Name);
                    sb.Append(",\"reason\":");
                    AppendString(sb, suspect.Reason);
                    sb.Append(",\"category\":");
                    AppendString(sb, suspect.Category);
                    sb.Append(",\"avgMs\":");
                    AppendNullable(sb, suspect.AvgMs);
                    sb.Append(",\"maxMs\":");
                    AppendNullable(sb, suspect.MaxMs);
                    sb.Append(",\"fileHint\":");
                    AppendString(sb, suspect.FileHint);
                    sb.Append('}');
                }
            }

            sb.Append(']');
        }

        private static void AppendStringArray(StringBuilder sb, IList<string> values)
        {
            sb.Append('[');
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    AppendString(sb, values[i]);
                }
            }

            sb.Append(']');
        }

        private static void AppendField(StringBuilder sb, string name, double? value, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            AppendString(sb, name);
            sb.Append(':');
            AppendNullable(sb, value);
        }

        private static void AppendField(StringBuilder sb, string name, int? value, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            AppendString(sb, name);
            sb.Append(':');
            if (value.HasValue)
            {
                sb.Append(value.Value);
            }
            else
            {
                sb.Append("null");
            }
        }

        private static void AppendField(StringBuilder sb, string name, long? value, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            AppendString(sb, name);
            sb.Append(':');
            if (value.HasValue)
            {
                sb.Append(value.Value);
            }
            else
            {
                sb.Append("null");
            }
        }

        private static void AppendField(StringBuilder sb, string name, bool? value, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            AppendString(sb, name);
            sb.Append(':');
            if (value.HasValue)
            {
                sb.Append(value.Value ? "true" : "false");
            }
            else
            {
                sb.Append("null");
            }
        }

        private static void AppendField(StringBuilder sb, string name, string value, bool first)
        {
            if (!first)
            {
                sb.Append(',');
            }

            AppendString(sb, name);
            sb.Append(':');
            AppendString(sb, value);
        }

        private static void AppendNullable(StringBuilder sb, double? value)
        {
            if (value.HasValue)
            {
                AppendNumber(sb, value.Value);
            }
            else
            {
                sb.Append("null");
            }
        }

        private static void AppendNumber(StringBuilder sb, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                sb.Append("null");
                return;
            }

            sb.Append(value.ToString("0.###", Invariant));
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
                else if (c == '\n')
                {
                    sb.Append("\\n");
                }
                else if (c == '\r')
                {
                    sb.Append("\\r");
                }
                else if (c == '\t')
                {
                    sb.Append("\\t");
                }
                else if (c < 32)
                {
                    sb.Append("\\u");
                    sb.Append(((int)c).ToString("x4", Invariant));
                }
                else
                {
                    sb.Append(c);
                }
            }

            sb.Append('"');
        }
    }
}
