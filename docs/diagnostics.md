# Live Diagnostics Bridge

Live Diagnostics Bridge is an isolated debug-only diagnostics tool for this Unity/FishNet project. It starts in dev/debug builds or when explicitly enabled with `ENABLE_DIAGNOSTICS=true`.

Production builds are disabled by default unless you pass the enable flag yourself.

## Enable Diagnostics

Options:

- Environment variable: `ENABLE_DIAGNOSTICS=true`
- Unity command line: `-enableDiagnostics`
- Static config before bootstrap: `DiagnosticsConfig.Enabled = true`
- Dev/debug builds: `Debug.isDebugBuild` enables diagnostics automatically

Useful optional settings:

- `DIAGNOSTICS_PORT=8765`
- `DIAGNOSTICS_TOKEN=your-local-token`
- `GAME_DIAG_URL=http://127.0.0.1:8765` for the CLI
- `-diagnosticsPort 8765`
- `-diagnosticsToken your-local-token`
- `-diagnosticsBufferSeconds 60`

Editor manual GC smoothing is disabled by default. Unity already schedules incremental GC
around `Application.targetFrameRate`; enable the diagnostics setting only for explicit A/B tests.

Disable diagnostics even in debug:

- `DISABLE_DIAGNOSTICS=true`

## Runtime Modules

Runtime code is under `Assets/Game/Scripts/Diagnostics`.

Main pieces:

- `DiagnosticsManager` auto-starts the bridge and samples metrics.
- `ProfileScope` / `MeasureAsync` measure hot paths.
- `RollingMetricsBuffer` keeps the last 60 seconds.
- `SpikeDetector` records frame, tick, network, memory, entity, and RPC spikes.
- `DiagnosticsAnalyzer` classifies the issue.
- `DiagnosticsHttpServer` exposes localhost JSON endpoints.
- `DiagnosticsJsonlWriter` writes append-only JSONL logs.
- `DiagnosticsOverlay` is toggled with F9.

## HTTP API

Default bind: `http://127.0.0.1:8765`

Endpoints:

- `GET /diagnostics/health`
- `GET /diagnostics/current`
- `GET /diagnostics/last?seconds=10`
- `GET /diagnostics/spikes?seconds=30`
- `GET /diagnostics/frame-spikes?seconds=60`
- `GET /diagnostics/top/client?seconds=10`
- `GET /diagnostics/top/server?seconds=10`
- `GET /diagnostics/network?seconds=10`
- `GET /diagnostics/analyze?seconds=10`

All endpoints return JSON.

## CLI

From the repository root on Windows PowerShell:

```powershell
.\game-diag.cmd health
.\game-diag.cmd current
.\game-diag.cmd snapshot --last 10
.\game-diag.cmd spikes --last 30
.\game-diag.cmd frame-spikes --last 60
.\game-diag.cmd top client --last 10
.\game-diag.cmd top server --last 10
.\game-diag.cmd network --last 10
.\game-diag.cmd analyze --last 10
.\game-diag.cmd export --last 60 --out diagnostics-report.json
```

If `game-diag.cmd` is on PATH, `game-diag ...` works too.

Exit codes:

- `0`: ok
- `1`: diagnostics unavailable
- `2`: game not running and no fallback log
- `3`: invalid command or invalid response
- `4`: severe/high spike detected, command successfully collected data

The CLI reads the local HTTP API first. If the API is unavailable, it tries the newest `diagnostics/logs/session-*.jsonl`.

## Classifications

- `CLIENT_BOUND`: client frame time/FPS is bad while server tick and network are normal.
- `SERVER_BOUND`: server tick is above threshold; inspect server systems/RPCs.
- `NETWORK_BOUND`: FPS and server tick are normal, but ping/jitter/loss are bad.
- `MEMORY_GC_BOUND`: memory growth or GC-like periodic spikes are the strongest signal.
- `ENTITY_SCALE_BOUND`: active entity growth correlates with tick/frame cost.
- `RPC_STORM`: RPC/event count or network message rate is unusually high.
- `UNKNOWN`: no clear signature in the sampled window.

## JSONL Logs

Logs are written to:

```text
diagnostics/logs/session-YYYYMMDD-HHMMSS.jsonl
```

Events:

- `metric_sample`
- `spike`
- `frame_spike`
- `scope` for slow scopes over the configured threshold

JSONL writes happen on a background thread. If file IO fails, diagnostics disables file writing and the game continues.

## Current Hooks

Measured scopes include:

- `Server.GameplaySpawner.Update`
- `Server.Visibility.*`
- `Network.SendMapVisibility`
- `Client.MapVisibility.Apply`
- `Client.VehicleInput.Update`
- `RPC.SendControls`
- `Client.Weapon.PredictAndRequest`
- `RPC.FireRequest`
- `Server.Weapon.Update`
- `Client.Weapon.Update`
- `Server.Projectile.*`
- `Client.Projectile.Update`
- `Server.VehicleMovement.FixedUpdate`
- `Server.BotNavigator.FixedUpdate`
- `Client.UI.GameplayMapHud.Update`
- `Client.UI.PingController.Update`

Add more scopes with:

```csharp
using (ProfileScope.Measure("Server.UpdateProjectiles", DiagnosticsCategories.Server))
{
    // existing code
}
```

For async code:

```csharp
await ProfileScope.MeasureAsync("Database.LoadPlayer", DiagnosticsCategories.Db, async () =>
{
    await LoadPlayerAsync();
});
```

## Metrics Notes

Available now:

- FPS and frame time
- frame p95/max over 10 seconds
- memory MB
- GC collection count
- active visible map entities
- FishNet spawned entity count
- server tick time p95/max
- tick rate
- active players
- active projectiles via runtime counter
- active bots
- incoming transport messages/bytes
- outgoing estimates from instrumented RPC/send hooks
- ping and jitter from FishNet time manager
- packet loss if the active FishNet transport supports it
- top slow scopes over 1/5/10 seconds
- top RPC/event count/time summaries
- exact frame-spike records with focus, resolution, GC count before/after, and Unity `ProfilerRecorder` timings when the marker is exposed by the active Editor/build

Currently `null` when unavailable or not cheaply exposed:

- exact GC allocated bytes/sec
- exact GC pause duration
- Unity render pipeline timings when the specific marker name is not exposed by the current Unity version/render pipeline
- full active GameObject count
- transport pending queue size
- exact outgoing bytes for FishNet-generated internal packets not covered by explicit hooks

These are intentionally not guessed. Add a cheap Unity `ProfilerRecorder` or transport-level hook if exact values become necessary.

## Security

- HTTP binds to `127.0.0.1` by default.
- Remote bind requires a token via `DIAGNOSTICS_TOKEN` or `-diagnosticsToken`.
- Player IDs in per-client network summaries are hashed.
- Diagnostics never sends data outside the local process.

## Lag Workflow

When the game lags:

```powershell
.\game-diag.cmd health
.\game-diag.cmd analyze --last 10
.\game-diag.cmd spikes --last 30
.\game-diag.cmd frame-spikes --last 60
.\game-diag.cmd top client --last 10
.\game-diag.cmd top server --last 10
.\game-diag.cmd network --last 10
```

Patch only the top suspect first, reproduce the issue, then run:

```powershell
.\game-diag.cmd analyze --last 30
```
