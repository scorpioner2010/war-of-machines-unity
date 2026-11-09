# War of Machines Performance Audit

Date: 2026-11-07

This report records the repository-wide static audit and the behavior-preserving lookup/allocation changes completed in this pass. Runtime performance numbers are never inferred from source inspection or build results.

The follow-up ranked list of potentially heavy methods is maintained in `docs/codex-game/heavy-method-audit.md`, with the complete production/test/editor/third-party method table in `docs/codex-game/heavy-method-audit-all.md`; these are static evidence only and do not replace matched Unity Profiler captures. Behavior-preserving remediation options and rollout rules are documented in `docs/codex-game/heavy-method-remediation-options.md`.

## Scope and evidence

- Every `Assets/**/*.cs` file is included in `docs/codex-game/script-performance-inventory.md`: 1,472 source files.
- Unity `Library` generated C# is excluded from ownership: 6,659 generated files were present during the audit.
- The inventory records lifecycle methods, physics, networking/RPC, component lookup, search, `AddComponent`, LINQ, conservative allocation flags, priority, responsibility, and audit status for every source file.
- The reproducible inventory generator is `tools/GenerateScriptPerformanceInventory.ps1`.
- Third-party/demo source was classified but not rewritten.

Inventory categories:

| Category | Files |
| --- | ---: |
| Production | 240 |
| Tests | 15 |
| Editor | 15 |
| Owned support/demo | 37 |
| Third-party | 1,165 |
| Total | 1,472 |

Current static totals across all source files are 227 component-lookup call matches, 49 Find/search matches, 129 `AddComponent` call matches, and 70 confirmed LINQ import/`Enumerable` matches. These are source-pattern counts, not runtime invocation counts or profiler measurements. The Find total also includes project methods named `Find...`, so it must not be interpreted as 49 Unity global searches.

## Implemented Tier A batch

- `CameraCollision` caches the Inspector camera/focus transforms and refreshes the cache when the assigned objects change.
- `FloatingDamageText` caches the active gameplay camera transform for the existing pooled animation lifecycle.
- `RobotEngineAudio` caches the resolved audio anchor and invalidates it when the vehicle root changes.
- `BotLineOfFireChecker` computes the shooter aim origin once per multi-probe decision while preserving probe order and hit selection.
- `MatchVisibilityService` pre-sizes participant/entry lists and grows snapshot arrays geometrically while preserving `Count`-bounded network payloads.
- `GameplayPlayerListHud` indexes player IDs and vehicle roots, retaining a linear fallback so duplicate IDs and stale references keep the previous first-match behavior.

This batch changes no raycast mask, trigger mode, cast endpoint, hit tie-break, RPC/tick order, authority, serialized scene value, or third-party source. The three C# assemblies build with zero errors; the attempted Unity batch import compiled scripts but emitted no test-results XML because the project is already open in another Unity process, so no new PlayMode pass is claimed.

## Current lookup result

The exact component-lookup regex used by the generator reports:

| Scope | `GetComponent*` / `TryGetComponent` calls |
| --- | ---: |
| All `Assets/**/*.cs` | 227 |
| Owned `Assets/Game/Scripts` | 21 |
| Owned production runtime (`Assets/Game/Scripts`, excluding `Testing` and `Editor`) | **0** |
| Owned test/editor harness | 21 |

The remaining 21 owned calls are deliberate test/editor tooling. They inspect arbitrary prefabs, construct temporary test objects, resolve test-only scene services, or validate dynamically spawned objects. Replacing those with gameplay Inspector references would either break the generic editor tool or change test setup behavior. Third-party/library calls remain outside project ownership.

No owned production runtime call remains for `GetComponent*`, `TryGetComponent`, `FindFirstObjectByType`, `FindAnyObjectByType`, `FindObjectsByType`, `FindObjectOfType`, `Resources.FindObjectsOfTypeAll`, `GameObject.Find`, `Camera.main`, `AddComponent`, or `Shader.Find`.

## Lookup cleanup

The following production paths now use serialized references, lifecycle caches, or focused registries:

- `AudioPreset.cs`: serialized audio source is used directly; runtime source lookup was removed.
- `ButtonComponent.cs`: editor configuration uses the authored button and graphic list instead of scanning child graphics.
- `BattleCountdownAuthority.cs` and `GameplayTimerNetwork.prefab`: the timer is Inspector-wired.
- `DeathLogic.cs` and `RobotDestructionParticleController.cs`: authored renderers, colliders, rigidbodies, and debris centers are reused; repeated child renderer/collider scans and temporary arrays were removed.
- `RobotDestructionParticleController.cs` and `RobotEngineAudio.cs`: stable vehicle-root references are Inspector-wired on the robot prefabs.
- `PooledImpactFx.cs`: authored particle arrays are used; child particle-system discovery was removed from pooled setup.
- `BattleCountdownView.cs`: the authored `CanvasGroup` is used directly.
- `GameplayMapHud.cs`: authored icon graphics are Inspector-wired; pooled marker objects carry their graphic reference without component lookup.
- `GunCrosshair.cs`: reticle and battle-countdown `CanvasGroup` references are Inspector-wired; runtime `GetComponent*` and `AddComponent` fallback setup was removed.
- `MenuManager.cs` and `GameResultUI.cs`: the inactive Game Result presenter is registered through the menu entry; `Resources.FindObjectsOfTypeAll`, runtime `GetComponent`, and runtime `AddComponent` were removed.
- `DecalWindowsSmokeCapture.cs` and `DecalWindowsSmoke.unity`: the smoke-test camera is serialized instead of found globally.
- `ScreenSpaceHoverOutlineFeature.cs` and the three URP renderer assets: fallback `Shader.Find` calls were replaced by serialized shader references. Existing material references remain unchanged.

The UI and prefab fields were wired in `Client.unity`, `GameplayTimerNetwork.prefab`, the eight owned robot prefabs, `DecalWindowsSmoke.unity`, and the three renderer assets. Network authority, serialized ordering, layer masks, collision selection, scene ownership, and visible UI behavior were not intentionally changed.

## Other safe optimizations

- `GameplayMapHud.cs`: active marker lookup uses object ID indexing and inactive mini/full marker instances are reused instead of destroyed and recreated on visibility churn.
- `SpectatorVehicleHudPresenter.cs`: unchanged HP/name TMP values no longer trigger repeated writes and interpolation string creation; the health-bar interpolation remains per-frame.
- `VehicleColliderRegistry.cs`: the temporary inactive-parent activation buffer is reused; frame-local impact-surface caching, squared-distance selection, and duplicate visual mesh-collider suppression are preserved.
- `Projectile.cs`: the already-computed ballistic segment distance is reused instead of recalculating the same magnitude.
- `VehicleGroundAlignmentController.cs`: configured ground-ray offsets are precomputed when configuration changes instead of recalculating sine/cosine values every frame.

These changes preserve check order, hit selection, projectile pooling, authority, ray count/order, collider restoration, marker identity, and UI target rebinding.

## Static before/after effects

| Path | Before | After | Runtime measurement |
| --- | --- | --- | --- |
| Owned production component lookups | 18 matching calls in 11 files | 0 | Not measured |
| Game Result presenter resolution | Global `Resources` scan plus dynamic component creation | Menu-entry presenter registry | Not measured |
| Crosshair group resolution | Child scan plus dynamic `CanvasGroup` creation | Serialized `CanvasGroup` arrays | Not measured |
| Smoke capture camera resolution | Global camera search | Serialized camera reference | Not measured |
| Renderer shader fallback | `Shader.Find` during feature setup | Serialized shader references | Not measured |
| Map marker lookup | Linear active-list search | Object-ID dictionary lookup | Not measured |
| Map marker lifecycle | Destroy/reinstantiate on snapshot churn | Reuse inactive mini/full instances | Not measured |
| Spectator HP/name labels | Repeated TMP writes and string creation | Change-gated writes; bar interpolation retained | Not measured |
| Temporary mesh-raycast activation list | Per-call list allocation when needed | Reused main-thread list | Not measured |

## Requested counters

- Production `GetComponent*` / `TryGetComponent` calls removed or replaced: **18**; current production runtime count: **0**.
- Production global/object searches removed: Game Result `Resources.FindObjectsOfTypeAll`, smoke-capture camera search, and crosshair child scans; current production Unity-object search count: **0**.
- Production runtime `AddComponent` fallbacks removed: Game Result UI and crosshair `CanvasGroup`; current production runtime count: **0**.
- Production `Shader.Find` fallback removed: **1**; renderer assets now hold both shader references.
- Owned production LINQ: no confirmed `System.Linq`/`Enumerable` usage was found before or after this pass.
- Allocation-producing churn addressed: map-marker lifetime churn, spectator label-string/UI churn, temporary activation-list churn, and repeated debris/particle child scans.

These counters are code-structure counts, not claims about measured CPU or memory reduction.

## Baseline and measurements

The requested matched before/after runtime baseline is still unavailable: the fresh client was rebuilt after the safe static changes, so these captures are a current-runtime observation rather than an A/B comparison. No improvement percentage is claimed.

| Metric | Before | After | Status |
| --- | --- | --- | --- |
| FPS / frame time | Not available | Not available | No matched live session |
| CPU main thread | Not available | Not available | Profiler recorder data unavailable |
| GC allocations / pauses | Not available | Not available | Runtime allocation stream unavailable |
| Physics time | Not available | Not available | No live physics profile |
| Network tick / RPC time | Not available | Not available | No live server/client profile |
| Active robots / projectiles / UI objects | Not available | Not available | No matched battle session |
| Top profiler scopes | Not available | Not available | No matched before/after capture |

## Autobattle runtime capture

The fresh client/server pair was tested while the user manually completed login, robot selection, battle entry, and autobattle activation. Raw samples are retained in `Temp/autobattle-profile-20261107.json` (60 seconds) and `Temp/autobattle-profile-20261107-followup.json` (90 seconds). Sessions were client `20261107-121923` on port `8765` and server `20261107-121911` on port `8770`.

### First 60 seconds

| Metric | Observed value |
| --- | ---: |
| Client FPS / frame time | 182.912–341.951 FPS; 2.924–5.467 ms; average 260.437 FPS / 3.955 ms |
| Client frame p95 | Average 4.775 ms across samples |
| Client memory | 228.083–231.145 MB during this window |
| Active visible entities | 10 down to 6; average 7.583 |
| Active entities | 20 down to 16; average 17.583 |
| Active projectiles | 0–1; average 0.083 |
| Network | 4–10 RPC/s; average 6.917 RPC/s; ping 27–33 ms; packet loss 0% |
| Server tick | 0.113–0.220 ms; average 0.176 ms |
| Server AI scope | 0.501–3.620 ms when sampled |

The slow-scope candidates were `Client.Projectile.Update` (13 samples, 269.428 ms total, 20.725 ms average, 33.205 ms max/p95, 0 reported allocation), `Server.Projectile.Update` (18 samples, 439.439 ms total, 24.413 ms average, 39.130 ms max/p95), `Camera.Controller.Update` (21 samples, 0.316 ms average), and `Server.BotCombat.Tick` (84 samples, 0.291 ms average). Projectile scopes were intermittent, not a per-frame sustained cost.

### Follow-up 90 seconds

| Metric | Observed value |
| --- | ---: |
| Client FPS / frame time | 383.054–500.149 FPS; 1.999–2.611 ms; average 486.020 FPS / 2.068 ms |
| Client frame p95 | 2.166–3.097 ms across samples |
| Client memory | 259.221–260.738 MB; average 259.961 MB |
| GC collection count | 231 to 276; allocation bytes were not exposed by this runtime capture |
| Active visible entities | 3 throughout the follow-up window |
| Active entities | 15 down to 13 |
| Active projectiles | 0–1; average 0.111 |
| Server tick | 0.100–0.216 ms; average 0.150 ms |
| Diagnostics spikes | None returned by `/diagnostics/spikes?seconds=120` on either process |

The only client top scope was `Camera.Controller.Update` (240 samples, 145.029 ms total, 0.604 ms average, 2.914 ms max/p95). The server projectile scope appeared only 5 times (117.023 ms total, 23.405 ms average, 29.948 ms max/p95). The diagnostics analyzer classified both windows as low severity with no clear lag signature.

`Server.physicsStepMs` is a sum of measured physics-category scopes in this diagnostics implementation, not a direct Unity PhysX wall-time recorder. The projectile scope is therefore the only measured physics candidate that warrants deeper impact-path profiling; reducing projectile tick rate, changing collision masks/order, removing `Physics.SyncTransforms`, or changing AI/network cadence is not justified by this capture and could change gameplay behavior.

Historical diagnostics logs are context only and are not a before/after benchmark.

## Domain audit

| Domain | Result |
| --- | --- |
| Projectile / combat | Pooled projectiles use non-alloc raycast paths; ballistic distance reuse and impact-surface cache work are preserved. |
| Robots / movement | Stable component references, debris centers, engine audio roots, ground-ray offset precomputation, and collider registry reuse are covered. |
| Aiming / turrets | No owned production component lookup, global search, or confirmed LINQ remains; precision, authority, and aim ordering were not changed. |
| AI / bots | No broad algorithm replacement was justified without a repeatable battle profile; AI behavior was not changed. |
| Networking | Timer/presenter wiring does not change network authority, RPC order, tick scheduling, or snapshot semantics. |
| Visibility | Map marker object-ID indexing and pooling preserve snapshot ordering and target identity. |
| HUD / UI | Crosshair, countdown, map, Game Result, button, and spectator presenters use authored references or registries. |
| Audio / VFX | Audio source, engine root, particle profile, and destruction child lookups were migrated without changing playback ownership. |
| Scene / bootstrap | Scene/prefab/renderer references were updated only where the corresponding component was already authored and stable. |
| Tests / editor | Dynamic lookup calls remain only where the harness must inspect arbitrary or runtime-created objects. |

## Changed files

The lookup-refactor batch changed 18 owned C# scripts, the corresponding client/smoke scenes, three URP renderer assets, the gameplay timer prefab, eight robot prefabs, the inventory generator/output, HUD documentation, and this report. The complete exact file list is available from `git status --short` and the task journal.

## Validation

### Compilation and static checks

- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: passed, 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo`: passed, 0 errors.
- `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo`: passed, 0 errors.
- Unity batch invocation returned exit code 0, but the requested focused runner emitted no XML result in this environment; this is not reported as a test pass.
- `git diff --check`: passed; only normal Git line-ending normalization warnings were emitted.
- Inventory regeneration completed with 1,472 rows.

Previously recorded Unity validation remains context, not a new result for this lookup batch: focused projectile 5/5, combat particles 9/9, spectator visibility 2/2, full EditMode 21/21, and full PlayMode 67 passed with 7 existing environment/test failures. The focused 10v9 run still had the prior host/client-readiness failure.

## Unresolved risks and limitations

- No matched post-change in-game profiler capture exists; CPU, GC, physics, network, and frame-time reduction remain unmeasured.
- The 21 owned test/editor component lookups remain because they operate on arbitrary editor assets or dynamically created test objects; converting them to production Inspector links would reduce test coverage or alter the harness contract.
- Third-party and imported demo libraries remain unchanged: 1,165 scripts and their static lookup/LINQ/allocation patterns are not project-owned.
- `Physics.SyncTransforms` remains in the inactive-collider raycast path because removing it can change raycast visibility and timing.
- Death-only effect instantiation and setup-time array copies remain where pooling or ownership changes could alter lifetime/API behavior; they are not per-frame component lookups.
- A real performance conclusion requires two equivalent diagnostics captures covering startup, movement, aiming/zoom, sustained firing, projectile impact, spectator cycling, and a 10v9 battle.
