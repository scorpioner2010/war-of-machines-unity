using System.Collections.Generic;

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsAnalyzer
    {
        private readonly DiagnosticsConfig _config;

        public DiagnosticsAnalyzer(DiagnosticsConfig config)
        {
            _config = config;
        }

        public DiagnosticsAnalysis Analyze(RollingMetricsBuffer buffer, int seconds)
        {
            if (buffer == null)
            {
                return Unknown("Diagnostics buffer is unavailable.");
            }

            DiagnosticsSnapshot snapshot = buffer.GetCurrentSnapshot(seconds);
            if (snapshot == null || snapshot.Current == null)
            {
                return Unknown("No diagnostics samples have been collected yet.");
            }

            DiagnosticsMetricSample sample = snapshot.Current;
            DiagnosticsClientMetrics client = sample.Client;
            DiagnosticsServerMetrics server = sample.Server;
            DiagnosticsNetworkMetrics network = sample.Network;
            List<DiagnosticsScopeSummary> topClient = buffer.GetTopScopes(DiagnosticsCategories.Client, seconds, 5);
            List<DiagnosticsScopeSummary> topServer = buffer.GetTopScopes(DiagnosticsCategories.Server, seconds, 5);
            List<DiagnosticsScopeSummary> topEditor = buffer.GetTopScopes(DiagnosticsCategories.Editor, seconds, 5);
            List<DiagnosticsScopeSummary> topRpc = buffer.GetTopEvents(DiagnosticsCategories.Rpc, seconds, 5, TopSortMode.Count);

            bool clientBad = IsClientBad(client);
            bool serverBad = IsServerBad(server);
            bool networkBad = IsNetworkBad(network);
            bool memoryBad = IsMemoryBad(buffer, snapshot.Spikes);
            bool rpcStorm = IsRpcStorm(buffer, network, seconds, topRpc);
            bool entityScale = IsEntityScaleBound(buffer, server, topServer);
            bool clientEditorBound = IsClientEditorBound(client, server);

            if (clientEditorBound)
            {
                return BuildClientEditorBound(sample, topClient, topEditor);
            }

            if (rpcStorm)
            {
                return BuildRpcStorm(sample, topRpc, topServer, network);
            }

            if (memoryBad)
            {
                return BuildMemoryBound(sample, buffer, topClient, topServer);
            }

            if (entityScale)
            {
                return BuildEntityScale(sample, topServer);
            }

            if (serverBad)
            {
                return BuildServerBound(sample, topServer, topRpc);
            }

            if (networkBad && !clientBad && !serverBad)
            {
                return BuildNetworkBound(sample);
            }

            if (clientBad && !serverBad && !networkBad)
            {
                return BuildClientBound(sample, topClient);
            }

            if (clientBad && serverBad)
            {
                DiagnosticsAnalysis analysis = BuildServerBound(sample, topServer, topRpc);
                analysis.Confidence = 0.66d;
                analysis.Evidence.Add("Client frame time is also high, but server tick is unhealthy and can affect all clients.");
                return analysis;
            }

            return BuildUnknownHealthy(sample, topClient, topServer);
        }

        private bool IsClientBad(DiagnosticsClientMetrics client)
        {
            if (client == null)
            {
                return false;
            }

            double severeFrameSpikeMs = _config.ClientFrameSpikeMs * 2d;
            return (client.FrameMsP95_10s.HasValue && client.FrameMsP95_10s.Value > _config.ClientFrameSpikeMs)
                   || (client.FrameMs.HasValue && client.FrameMs.Value > _config.ClientFrameSpikeMs)
                   || (client.FrameMsMax_10s.HasValue && client.FrameMsMax_10s.Value > severeFrameSpikeMs)
                   || (client.Fps.HasValue && client.Fps.Value < _config.ClientLowFps);
        }

        private bool IsServerBad(DiagnosticsServerMetrics server)
        {
            if (server == null || !server.ServerTickMsP95_10s.HasValue)
            {
                return false;
            }

            double threshold = _config.ServerTickSpikeMs;
            if (server.TickRate.HasValue && server.TickRate.Value > 0)
            {
                double targetMs = 1000d / server.TickRate.Value;
                double multiplied = targetMs * _config.ServerTickMultiplier;
                if (multiplied > threshold)
                {
                    threshold = multiplied;
                }
            }

            return server.ServerTickMsP95_10s.Value > threshold
                   || (server.ServerTickMs.HasValue && server.ServerTickMs.Value > threshold);
        }

        private bool IsNetworkBad(DiagnosticsNetworkMetrics network)
        {
            if (network == null)
            {
                return false;
            }

            return (network.PingMs.HasValue && network.PingMs.Value > _config.PingSpikeMs)
                   || (network.JitterMs.HasValue && network.JitterMs.Value > _config.JitterSpikeMs)
                   || (network.PacketLossPercent.HasValue && network.PacketLossPercent.Value > _config.PacketLossSpikePercent);
        }

        private bool IsClientEditorBound(DiagnosticsClientMetrics client, DiagnosticsServerMetrics server)
        {
            if (client == null || !IsClientBad(client) || IsServerBad(server))
            {
                return false;
            }

            if (!client.IsEditor.HasValue || !client.IsEditor.Value)
            {
                return false;
            }

            bool highResolution = (client.ScreenWidth.HasValue && client.ScreenWidth.Value >= 1920)
                                  || (client.ScreenHeight.HasValue && client.ScreenHeight.Value >= 1080);
            bool focused = !client.ApplicationFocused.HasValue || client.ApplicationFocused.Value;
            return highResolution && focused;
        }

        private bool IsMemoryBad(RollingMetricsBuffer buffer, List<DiagnosticsSpike> spikes)
        {
            double? clientGrowth = buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Client, 30);
            double? serverGrowth = buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Server, 30);
            if ((clientGrowth.HasValue && clientGrowth.Value > _config.MemoryGrowthMbPerMinute)
                || (serverGrowth.HasValue && serverGrowth.Value > _config.MemoryGrowthMbPerMinute))
            {
                return true;
            }

            if (spikes == null)
            {
                return false;
            }

            for (int i = 0; i < spikes.Count; i++)
            {
                DiagnosticsSpike spike = spikes[i];
                if (spike != null && spike.Type != null && spike.Type.Contains("memory"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsRpcStorm(RollingMetricsBuffer buffer, DiagnosticsNetworkMetrics network, int seconds, List<DiagnosticsScopeSummary> topRpc)
        {
            double rpcPerSecond = buffer.GetEventCountPerSecond(DiagnosticsCategories.Rpc, seconds);
            bool rpcHigh = rpcPerSecond > _config.RpcStormCountPerSecond;
            bool networkHigh = network != null
                               && ((network.IncomingMessagesPerSecond.HasValue && network.IncomingMessagesPerSecond.Value > _config.NetworkMessageStormPerSecond)
                                   || (network.OutgoingMessagesPerSecond.HasValue && network.OutgoingMessagesPerSecond.Value > _config.NetworkMessageStormPerSecond));

            return rpcHigh || (networkHigh && topRpc != null && topRpc.Count > 0);
        }

        private bool IsEntityScaleBound(RollingMetricsBuffer buffer, DiagnosticsServerMetrics server, List<DiagnosticsScopeSummary> topServer)
        {
            int? entityGrowth = buffer.GetEntityGrowth(10);
            if (entityGrowth.HasValue && entityGrowth.Value > _config.EntityGrowthSpikeCount)
            {
                return true;
            }

            if (server == null || !server.ActiveEntities.HasValue || server.ActiveEntities.Value < 200)
            {
                return false;
            }

            if (topServer == null)
            {
                return false;
            }

            for (int i = 0; i < topServer.Count; i++)
            {
                string name = topServer[i].Name;
                if (ContainsAny(name, "Visibility", "Projectile", "Physics", "Movement", "BotNavigator", "AI"))
                {
                    return true;
                }
            }

            return false;
        }

        private DiagnosticsAnalysis BuildClientBound(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topClient)
        {
            DiagnosticsAnalysis analysis = Base("CLIENT_BOUND", 0.86d, "high", "Problem looks client-bound. Server tick and network are normal, but client frame time/FPS is unhealthy.");
            AddClientEvidence(analysis, sample);
            AddFramePacingEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            AddNetworkEvidence(analysis, sample);
            AddSuspects(analysis, topClient, "dominates client frame time during recent samples");
            analysis.RecommendedNextSteps.Add("Inspect the top slow client scope first.");
            analysis.RecommendedNextSteps.Add("Check whether the UI/render/client simulation work runs every frame without culling or throttling.");
            analysis.RecommendedNextSteps.Add("Patch the top suspect, then re-run game-diag analyze --last 30.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildClientEditorBound(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topClient, List<DiagnosticsScopeSummary> topEditor)
        {
            DiagnosticsAnalysis analysis = Base(
                "CLIENT_EDITOR_BOUND",
                0.88d,
                "high",
                "Problem appears in the client Unity Editor at high resolution while server tick is normal. This points to client Editor/render/frame pacing/debug UI/focus-dependent Update behavior, not server simulation.");
            AddClientEvidence(analysis, sample);
            AddFramePacingEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            AddNetworkEvidence(analysis, sample);
            AddSuspects(analysis, topEditor, "editor/debug/IMGUI work during the focused client window");
            AddSuspects(analysis, topClient, "client frame pacing or high-resolution client work");
            analysis.RecommendedNextSteps.Add("Run A/B/C captures and compare applicationFocused, resolution, frame p95, ping, and top editor/client scopes.");
            analysis.RecommendedNextSteps.Add("If only the focused high-resolution client is bad, test disabling Game View Gizmos/Stats/debug overlay/HUD before touching server simulation.");
            analysis.RecommendedNextSteps.Add("Check frame-rate pacing settings: runInBackground, targetFrameRate, vSyncCount, Game View scale, and Maximize On Play.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildServerBound(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topServer, List<DiagnosticsScopeSummary> topRpc)
        {
            DiagnosticsAnalysis analysis = Base("SERVER_BOUND", 0.84d, "high", "Problem looks server-bound. Server tick exceeds threshold; inspect top server systems and RPC handlers.");
            AddServerEvidence(analysis, sample);
            AddClientEvidence(analysis, sample);
            AddNetworkEvidence(analysis, sample);
            AddSuspects(analysis, topServer, "dominates server time during recent samples");
            AddSuspects(analysis, topRpc, "RPC/event handler is frequent or expensive");
            analysis.RecommendedNextSteps.Add("Open the server scope file shown in filesToInspect.");
            analysis.RecommendedNextSteps.Add("Check for per-tick loops over all players/entities, missing throttles, or expensive physics/pathfinding.");
            analysis.RecommendedNextSteps.Add("Patch the top server suspect, then re-run game-diag analyze --last 30.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildNetworkBound(DiagnosticsMetricSample sample)
        {
            DiagnosticsAnalysis analysis = Base("NETWORK_BOUND", 0.82d, "high", "Problem looks network-bound. FPS and server tick are not the main signal, but ping/jitter/loss are unhealthy.");
            AddClientEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            AddNetworkEvidence(analysis, sample);
            analysis.RecommendedNextSteps.Add("Check transport/hosting path, packet loss, and server region before editing gameplay code.");
            analysis.RecommendedNextSteps.Add("Inspect high message-rate RPCs only if network message rate is also high.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildMemoryBound(DiagnosticsMetricSample sample, RollingMetricsBuffer buffer, List<DiagnosticsScopeSummary> topClient, List<DiagnosticsScopeSummary> topServer)
        {
            DiagnosticsAnalysis analysis = Base("MEMORY_GC_BOUND", 0.78d, "high", "Problem looks like memory allocation or GC pressure.");
            AddClientEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            double? clientGrowth = buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Client, 30);
            double? serverGrowth = buffer.GetMemoryGrowthMbPerMinute(DiagnosticsCategories.Server, 30);
            if (clientGrowth.HasValue)
            {
                analysis.Evidence.Add("client memory growth is " + Format(clientGrowth.Value) + " MB/min");
            }
            if (serverGrowth.HasValue)
            {
                analysis.Evidence.Add("server memory growth is " + Format(serverGrowth.Value) + " MB/min");
            }
            AddSuspects(analysis, topClient, "may allocate on the client hot path");
            AddSuspects(analysis, topServer, "may allocate on the server hot path");
            analysis.RecommendedNextSteps.Add("Look for allocations in the top suspect scopes and repeated Instantiate/Destroy or collection rebuilds.");
            analysis.RecommendedNextSteps.Add("Prefer pooling, cached buffers, and throttled UI/snapshot rebuilds.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildEntityScale(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topServer)
        {
            DiagnosticsAnalysis analysis = Base("ENTITY_SCALE_BOUND", 0.78d, "high", "Problem scales with active entities. Suspect visibility, physics, projectiles, AI, or interest management.");
            AddServerEvidence(analysis, sample);
            AddSuspects(analysis, topServer, "cost is likely scaling with active entity count");
            analysis.RecommendedNextSteps.Add("Check whether the suspect loops over all entities or all pairs every tick.");
            analysis.RecommendedNextSteps.Add("Add culling, throttling, pooling, batching, or spatial partitioning where the top scope points.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildRpcStorm(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topRpc, List<DiagnosticsScopeSummary> topServer, DiagnosticsNetworkMetrics network)
        {
            DiagnosticsAnalysis analysis = Base("RPC_STORM", 0.83d, "high", "Likely RPC storm. One or more RPC/events or network messages are too frequent.");
            AddNetworkEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            if (network != null)
            {
                if (network.IncomingMessagesPerSecond.HasValue)
                {
                    analysis.Evidence.Add("incoming messages/sec is " + Format(network.IncomingMessagesPerSecond.Value));
                }
                if (network.OutgoingMessagesPerSecond.HasValue)
                {
                    analysis.Evidence.Add("outgoing messages/sec is " + Format(network.OutgoingMessagesPerSecond.Value));
                }
            }

            AddSuspects(analysis, topRpc, "dominates RPC/event count or time");
            AddSuspects(analysis, topServer, "server work may be caused by the message storm");
            analysis.RecommendedNextSteps.Add("Check whether the top RPC is called from Update/Tick without throttle or change detection.");
            analysis.RecommendedNextSteps.Add("Add send interval, deadzone, batching, or state-change guards.");
            return analysis;
        }

        private DiagnosticsAnalysis BuildUnknownHealthy(DiagnosticsMetricSample sample, List<DiagnosticsScopeSummary> topClient, List<DiagnosticsScopeSummary> topServer)
        {
            DiagnosticsAnalysis analysis = Base("UNKNOWN", 0.42d, "low", "No clear lag signature in the current diagnostics window.");
            AddClientEvidence(analysis, sample);
            AddServerEvidence(analysis, sample);
            AddNetworkEvidence(analysis, sample);
            AddSuspects(analysis, topClient, "largest recent client scope, but below configured thresholds");
            AddSuspects(analysis, topServer, "largest recent server scope, but below configured thresholds");
            analysis.RecommendedNextSteps.Add("Reproduce the lag and run game-diag analyze --last 30 during the spike.");
            return analysis;
        }

        private static DiagnosticsAnalysis Unknown(string summary)
        {
            return Base("UNKNOWN", 0d, "low", summary);
        }

        private static DiagnosticsAnalysis Base(string classification, double confidence, string severity, string summary)
        {
            return new DiagnosticsAnalysis
            {
                Classification = classification,
                Confidence = confidence,
                Severity = severity,
                Summary = summary
            };
        }

        private static void AddClientEvidence(DiagnosticsAnalysis analysis, DiagnosticsMetricSample sample)
        {
            if (sample == null || sample.Client == null)
            {
                return;
            }

            DiagnosticsClientMetrics client = sample.Client;
            if (client.FrameMsP95_10s.HasValue)
            {
                analysis.Evidence.Add("client frameMs p95 is " + Format(client.FrameMsP95_10s.Value) + "ms");
            }
            if (client.FrameMsMax_10s.HasValue)
            {
                analysis.Evidence.Add("client frameMs max is " + Format(client.FrameMsMax_10s.Value) + "ms");
            }
            if (client.Fps.HasValue)
            {
                analysis.Evidence.Add("client FPS is " + Format(client.Fps.Value));
            }
        }

        private static void AddFramePacingEvidence(DiagnosticsAnalysis analysis, DiagnosticsMetricSample sample)
        {
            if (sample == null || sample.Client == null)
            {
                return;
            }

            DiagnosticsClientMetrics client = sample.Client;
            if (client.ApplicationFocused.HasValue)
            {
                analysis.Evidence.Add("application focused is " + client.ApplicationFocused.Value);
            }
            if (client.ApplicationRunInBackground.HasValue)
            {
                analysis.Evidence.Add("runInBackground is " + client.ApplicationRunInBackground.Value);
            }
            if (client.ScreenWidth.HasValue && client.ScreenHeight.HasValue)
            {
                analysis.Evidence.Add("screen is " + client.ScreenWidth.Value + "x" + client.ScreenHeight.Value);
            }
            if (!string.IsNullOrEmpty(client.FullscreenMode))
            {
                analysis.Evidence.Add("fullscreen mode is " + client.FullscreenMode);
            }
            if (!string.IsNullOrEmpty(client.QualityName))
            {
                analysis.Evidence.Add("quality is " + client.QualityName);
            }
            if (client.VSyncCount.HasValue)
            {
                analysis.Evidence.Add("vSyncCount is " + client.VSyncCount.Value);
            }
            if (client.TargetFrameRate.HasValue)
            {
                analysis.Evidence.Add("targetFrameRate is " + client.TargetFrameRate.Value);
            }
            if (client.RefreshRate.HasValue)
            {
                analysis.Evidence.Add("refreshRate is " + Format(client.RefreshRate.Value));
            }
            if (client.FixedDeltaTime.HasValue)
            {
                analysis.Evidence.Add("fixedDeltaTime is " + Format(client.FixedDeltaTime.Value));
            }
            if (client.MaximumDeltaTime.HasValue)
            {
                analysis.Evidence.Add("maximumDeltaTime is " + Format(client.MaximumDeltaTime.Value));
            }
            if (client.TimeScale.HasValue)
            {
                analysis.Evidence.Add("timeScale is " + Format(client.TimeScale.Value));
            }
            if (client.IsEditor.HasValue)
            {
                analysis.Evidence.Add("isEditor is " + client.IsEditor.Value);
            }
            if (client.EditorPaused.HasValue)
            {
                analysis.Evidence.Add("editorPaused is " + client.EditorPaused.Value);
            }
        }

        private static void AddServerEvidence(DiagnosticsAnalysis analysis, DiagnosticsMetricSample sample)
        {
            if (sample == null || sample.Server == null)
            {
                return;
            }

            DiagnosticsServerMetrics server = sample.Server;
            if (server.ServerTickMsP95_10s.HasValue)
            {
                analysis.Evidence.Add("server tick p95 is " + Format(server.ServerTickMsP95_10s.Value) + "ms");
            }
            if (server.ActivePlayers.HasValue)
            {
                analysis.Evidence.Add("active players is " + server.ActivePlayers.Value);
            }
            if (server.ActiveEntities.HasValue)
            {
                analysis.Evidence.Add("active entities is " + server.ActiveEntities.Value);
            }
        }

        private static void AddNetworkEvidence(DiagnosticsAnalysis analysis, DiagnosticsMetricSample sample)
        {
            if (sample == null || sample.Network == null)
            {
                return;
            }

            DiagnosticsNetworkMetrics network = sample.Network;
            if (network.PingMs.HasValue)
            {
                analysis.Evidence.Add("ping is " + Format(network.PingMs.Value) + "ms");
            }
            if (network.JitterMs.HasValue)
            {
                analysis.Evidence.Add("jitter is " + Format(network.JitterMs.Value) + "ms");
            }
            if (network.PacketLossPercent.HasValue)
            {
                analysis.Evidence.Add("packet loss is " + Format(network.PacketLossPercent.Value) + "%");
            }
        }

        private static void AddSuspects(DiagnosticsAnalysis analysis, List<DiagnosticsScopeSummary> scopes, string reason)
        {
            if (analysis == null || scopes == null)
            {
                return;
            }

            for (int i = 0; i < scopes.Count && analysis.TopSuspects.Count < 5; i++)
            {
                DiagnosticsScopeSummary scope = scopes[i];
                if (scope == null || string.IsNullOrEmpty(scope.Name))
                {
                    continue;
                }

                string file = ResolveFileHint(scope.Name);
                analysis.TopSuspects.Add(new DiagnosticsSuspect
                {
                    Name = scope.Name,
                    Reason = reason,
                    Category = scope.Category,
                    AvgMs = scope.AvgMs,
                    MaxMs = scope.MaxMs,
                    FileHint = file
                });

                if (!string.IsNullOrEmpty(file) && !analysis.FilesToInspect.Contains(file))
                {
                    analysis.FilesToInspect.Add(file);
                }
            }
        }

        private static string ResolveFileHint(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName))
            {
                return null;
            }

            if (scopeName.Contains("GameplayMapHud"))
            {
                return "Assets/Game/Scripts/UI/HUD/GameplayMapHud.cs";
            }
            if (scopeName.Contains("ServerDebugOverlay"))
            {
                return "Assets/Game/Scripts/Server/ServerDebugOverlay.cs";
            }
            if (scopeName.Contains("DebugOverlay"))
            {
                return "Assets/Game/Scripts/Diagnostics/DiagnosticsOverlay.cs";
            }
            if (scopeName.Contains("FPSCounter"))
            {
                return "Assets/Game/Scripts/Core/Utils/FPSCounter.cs";
            }
            if (scopeName.Contains("ArmorPrefabHighlighter"))
            {
                return "Assets/Game/Scripts/Editor/ArmorPrefabHighlighter.cs";
            }
            if (scopeName.Contains("WaypointPointSpawner"))
            {
                return "Assets/Game/Scenes/WaypointPointSpawner.cs";
            }
            if (scopeName.Contains("PingController"))
            {
                return "Assets/Game/Scripts/UI/HUD/PingController.cs";
            }
            if (scopeName.Contains("NetworkSnapshot"))
            {
                return "Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs";
            }
            if (scopeName.Contains("Visibility") || scopeName.Contains("SendMapVisibility"))
            {
                return "Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs";
            }
            if (scopeName.Contains("GameplaySpawner"))
            {
                return "Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs";
            }
            if (scopeName.Contains("SendControls"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/VehicleInputController.cs";
            }
            if (scopeName.Contains("WeaponAim"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/WeaponAimController.cs";
            }
            if (scopeName.Contains("WeaponReticle"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/WeaponReticlePresenter.cs";
            }
            if (scopeName.Contains("Weapon") || scopeName.Contains("FireRequest") || scopeName.Contains("Shoot"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs";
            }
            if (scopeName.Contains("Movement"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/RobotMovementMotor.cs";
            }
            if (scopeName.Contains("VehicleTurret"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/VehicleTurretRotationController.cs";
            }
            if (scopeName.Contains("BotNavigator") || scopeName.Contains("AI"))
            {
                return "Assets/Game/Scripts/AI/WaypointGraph/BotNavigator.cs";
            }
            if (scopeName.Contains("Camera.Controller"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/CameraController.cs";
            }
            if (scopeName.Contains("Camera.Sync"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/CameraSync.cs";
            }
            if (scopeName.Contains("Camera.Collision"))
            {
                return "Assets/Game/Scripts/Player/Camera/CameraCollision.cs";
            }
            if (scopeName.Contains("VehicleHUD"))
            {
                return "Assets/Game/Scripts/UI/HUD/VehicleHUD.cs";
            }
            if (scopeName.Contains("PositionSync") || scopeName.Contains("Interpolation"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/PositionSync.cs";
            }
            if (scopeName.Contains("Projectile.Spawn") || scopeName.Contains("ImpactFx"))
            {
                return "Assets/Game/Scripts/Gameplay/Robots/ProjectileVisualSpawner.cs";
            }
            if (scopeName.Contains("Projectile"))
            {
                return "Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs";
            }

            return null;
        }

        private static bool ContainsAny(string value, string a, string b, string c, string d, string e, string f)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.Contains(a)
                   || value.Contains(b)
                   || value.Contains(c)
                   || value.Contains(d)
                   || value.Contains(e)
                   || value.Contains(f);
        }

        private static string Format(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
