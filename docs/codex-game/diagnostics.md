# Diagnostics

Primary diagnostics details live in `docs/diagnostics.md`.

Read this file before changing diagnostics code or responding to lag, freezes, stutter, desync, rubber-banding, high ping, bad FPS, or server performance reports.

Current owner scripts:
- `Assets/Game/Scripts/Diagnostics/DiagnosticsManager.cs`
- `Assets/Game/Scripts/Diagnostics/ClientDiagnosticsCollector.cs`
- `Assets/Game/Scripts/Diagnostics/ServerDiagnosticsCollector.cs`
- `Assets/Game/Scripts/Diagnostics/NetworkDiagnosticsCollector.cs`
- `Assets/Game/Scripts/Diagnostics/DiagnosticsAnalyzer.cs`
- `Assets/Game/Scripts/Diagnostics/DiagnosticsHttpServer.cs`
- `Assets/Game/Scripts/Diagnostics/ProfileScope.cs`
- `Assets/Game/Scripts/Diagnostics/SpikeDetector.cs`
- `Assets/Game/Scripts/Diagnostics/RollingMetricsBuffer.cs`
- `Assets/Game/Scripts/Diagnostics/UnityFrameProfilerRecorder.cs`
- `tools/` and `game-diag.cmd`

Mandatory workflow is defined in `AGENTS.md` and must be followed before gameplay edits for performance reports.

Quick command reminder from repository root on Windows:
- `./game-diag.cmd health`
- `./game-diag.cmd analyze --last 10`
- `./game-diag.cmd spikes --last 30`
- `./game-diag.cmd frame-spikes --last 60`
- `./game-diag.cmd top client --last 10`
- `./game-diag.cmd top server --last 10`
- `./game-diag.cmd network --last 10`

Rules when editing diagnostics:
- Do not remove diagnostics code unless explicitly asked.
- Keep diagnostic categories and profile scopes meaningful.
- If performance-related gameplay code changes, consider adding/updating `ProfileScope` only when it gives actionable data.
