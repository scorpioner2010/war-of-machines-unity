# Diagnostics

Primary diagnostics details live in `docs/diagnostics.md`.

Read this file before changing diagnostics code or responding to lag, freezes, stutter, desync, rubber-banding, high ping, bad FPS, or server performance reports.

Current owner scripts:
- `Assets/Game/Scripts/Diagnostics/DiagnosticsManager.cs`
- `Assets/Game/Scripts/Diagnostics/GameplayRuntimeDiagnostics.cs`
  - Writes lifecycle events to `Logs/GameplayRuntimeDiagnostics.log` beside the Unity executable/project data directory; rotates at 5 MiB and retains two archives.
  - Each process run has a `session_id`; match-result profile refreshes share a `flow_id`, and their API calls include a matching `request_id` for API/server correlation.
  - Match-end records include HTTP status/result, elapsed time, response size, error category, reward fallback, and client delivery/receipt state.
  - It records identifiers, codes, counts, and bounded failure categories; known credential fields and bearer/inline secrets are redacted. Access tokens and passwords must never be passed to it.
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
- `Assets/Game/Scripts/Server/ServerDebugOverlay.cs`
  - The `Local client (host only)` line reports whether the server process itself also runs a FishNet client. It correctly remains `Stopped` on a dedicated server when remote players connect.
  - `Connected clients` is the remote connection count from `ServerManager.Clients.Count` and is the line to use for player connections.

Mandatory workflow is defined in `AGENTS.md` and must be followed before gameplay edits for performance reports.

Quick command reminder from repository root on Windows:
- `./game-diag.cmd health`
- `./game-diag.cmd analyze --last 10`
- `./game-diag.cmd spikes --last 30`
- `./game-diag.cmd frame-spikes --last 60`
- `./game-diag.cmd top client --last 10`
- `./game-diag.cmd top server --last 10`
- `./game-diag.cmd network --last 10`

Known diagnostic trap:
- Server-authoritative physics displacement can appear as network desync because FishNet correctly synchronizes the displaced transform. Normal network metrics, especially `0%` packet loss, do not prove the movement code itself is at fault.
- Recorded example: enabled `CabineReal`, `WeaponReal`, and `MeshReal` debris colliders in `T2-RM.prefab` overlapped the vehicle `CharacterController`. Unity depenetration moved the server vehicle backward/out of the map, then FishNet replicated it.
- For this failure signature, classify the network itself as healthy and inspect the vehicle prefab for enabled overlapping colliders, root transform offsets, and incomplete `DeathLogic` wiring. The detailed repair contract is in `docs/codex-game/robot-control.md`.

Rules when editing diagnostics:
- Do not remove diagnostics code unless explicitly asked.
- Keep diagnostic categories and profile scopes meaningful.
- If performance-related gameplay code changes, consider adding/updating `ProfileScope` only when it gives actionable data.
- Use `GameplayRuntimeDiagnostics` for event-oriented runtime investigations instead of adding per-frame logs. Keep high-volume events bounded to meaningful requests, hits, failures, and state transitions.
