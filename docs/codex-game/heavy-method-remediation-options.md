# Heavy Method Remediation Options

## Purpose

This document translates the static heavy-method audit into implementation options that can be applied without changing gameplay, network authority, balance, robot behaviour, aim, weapons, HUD, or scenes.

The source audit is a review ranking, not a profiler result. It covers 1,472 C# files and reports 4,983 method candidates, including 917 owned production candidates, 30 owned P0 candidates, and 137 owned P1 candidates. The complete list is in `docs/codex-game/heavy-method-audit.md` and `docs/codex-game/heavy-method-audit-all.md`.

No gameplay code is changed by this document. Each option below must be implemented as a separate, measured batch.

## Decision rules

### Tier A: safe first changes

These changes preserve the existing inputs, order, layer masks, query parameters, serialized defaults, and outputs:

- Cache a stable serialized component, `Transform`, `Camera`, `Renderer`, `Collider`, settings object, or parent reference in `Awake`, `OnEnable`, or the existing initialization path.
- Reuse an already allocated buffer or list and clear it instead of creating a new collection per call.
- Pre-size collections and snapshot arrays using the known maximum or geometric growth; never shrink them on a realtime tick.
- Skip an assignment only when the value is bitwise/equivalent to the current value and the setter has no required notification side effect.
- Gate diagnostics, `OnGUI`, verbose logging, and profiler-only string construction behind the existing enabled/development condition.
- Keep setup-time object/material creation in setup and reuse the existing pooled or cached instance.
- Replace a distance comparison with squared distance only when there is no externally visible distance value, threshold rounding, or normalization dependent on the original square root.
- Add counters and profiler markers without changing scheduling or result selection.

### Tier B: conditional changes

These can be valuable but require focused comparison against the current behaviour:

- Reuse or grow a physics result buffer only after proving that the current maximum is not silently truncating results, or that overflow handling has the same result.
- Batch line-of-sight or ground queries only if the existing pair cursor, probe order, hit selection, and tick budget remain identical.
- Stagger or lower query frequency only behind an explicit setting/feature flag and only where the current game already permits stale data.
- Pool projectiles, impact effects, UI rows, markers, and tree arrows only after proving reset, parent, active-state, event subscription, material instance, and lifetime equivalence.
- Batch UI rebuilds only when row/marker identity, ordering, selection, layout timing, and accessibility state remain unchanged.
- Remove `Physics.SyncTransforms` only after a controlled experiment proves that the same transforms are visible to the same physics query on every supported Unity/runtime configuration.

### Tier C: do not do mechanically

The following are not safe optimizations without a formal behaviour comparison:

- Changing `Raycast`/`Overlap` parameters, layer masks, trigger interaction, ray order, cast endpoints, cast padding, or hit tie-breaking.
- Replacing a sorted/nearest-hit selection with the first returned hit from a NonAlloc call.
- Changing `RaycastNonAlloc` buffer size without an overflow test and deterministic fallback.
- Moving server authority, RPC order, tick order, visibility expiry, prediction, or projectile impact callbacks.
- Replacing `.material` with `.sharedMaterial` when per-instance material state is intentional.
- Removing `SetActive`, collider enable/disable, or `Physics.SyncTransforms` from collider-surface resolution without reproducing inactive hierarchy semantics.
- Adding a global registry that changes registration order, scene reload cleanup, ownership, or destruction timing.
- Introducing approximate culling, lower precision, AI throttling, or altered probe counts under the name of a cache.

## Remediation matrix

| Method | Current cost/semantics | Safe first option | Conditional option | Invariants to verify |
|---|---|---|---|---|
| `VehicleColliderRegistry.TryRaycastMeshCollider` (`Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs:540`) | Walks inactive parents, temporarily activates objects, enables the collider, calls `Physics.SyncTransforms`, raycasts, and restores state in `finally`. This is expensive but deliberately supports inactive visual hierarchies. | Instrument activation count, sync count, ray count, and cache hit rate. Reuse the existing activation buffer and avoid any repeated caller-side surface lookup when the existing `ImpactSurfaceCacheByRoot` is valid. | Group multiple surface requests so one sync occurs after all required state changes, or replace temporary activation with an equivalent explicit collision representation. Both require a scene matrix covering inactive parents and disabled colliders. | Same hit/no-hit result, hit point, normal, collider, inactive state, enabled state, and exception cleanup. Never remove the `finally` restoration. |
| `Projectile.Update` (`Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs:890`) | Runs for every active projectile; preserves authoritative wait states, initial embedded collision, ballistic segment, live collision, impact callback, transform update, and lifetime limits. | Keep the exact order. Cache immutable settings/masks and keep debug drawing disabled unless requested. Measure active count, collision-query count, and time per authoritative/client path. | Pool only the projectile visual/effect lifecycle; do not alter projectile simulation. A fixed-step or query budget change is unsafe unless the network/ballistic contract is explicitly unchanged. | Segment endpoints, `dt` catch-up, embedded check, cast padding, collision mask, authority, callbacks, impact point, and miss limits. |
| `Projectile.TryCastEmbeddedCollision` (`Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs:1157`) | Uses `OverlapSphereNonAlloc`, filters ignored root/layer, computes backtrack distance from collider bounds, then calls the normal cast. It is already non-allocating. | Keep the existing buffer and the one-time `_initialEmbeddedCollisionChecked` guard. Cache the resolved collision-layer mask and profile buffer saturation. | Resize the buffer during setup or add a deterministic overflow path only after testing scenes with the maximum collider density. | Same overlap radius, collider filtering, bounds-based backtrack, direction fallback, and first-impact selection. |
| `Projectile.TryCastCollision` (`Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs:1220`) | Uses `RaycastNonAlloc`, then separately selects the nearest blocking hit and nearest armor hit. This selection is gameplay-critical. | Keep the manual selection. Cache masks and avoid redundant `EnsureCollisionLayers` work after initialization. | Use a dedicated per-pool buffer or deterministic overflow retry only if it preserves both blocker/armor precedence and nearest-distance rules. | Blocking-vs-armor precedence, ignored root, layer mask, trigger mode, distance tie behaviour, and selected collider. |
| `ServerHitResolver.TryRaycast` (`Assets/Game/Scripts/Gameplay/Robots/ServerHitResolver.cs:178`) | Uses direct `Physics.Raycast` when there is no ignored root; otherwise uses a buffer and scans for the nearest valid hit. | Keep the direct path for the common no-ignore case, cache the server mask/configuration, and instrument ignored-root frequency. | Unify buffer ownership only if no concurrent/nested call can overwrite it and nearest-hit selection remains exact. | Server-only authority, ignored-root filtering, nearest valid hit, layer/trigger parameters, and callback ordering. |
| `VehicleHoverOutline.TryFindHoverTarget` (`Assets/Game/Scripts/Gameplay/Robots/VehicleHoverOutline.cs:122`) | Per-frame hover path with `RaycastNonAlloc`; scans hits and resolves the vehicle root. | Cache camera/settings references, avoid repeating the same ray when the input/view has not changed if the existing hover update cadence permits it, and keep the current buffer/selection. | Event-driven hover checks or a lower cadence are possible only if outline response timing is accepted as unchanged by the current contract. | Same cursor ray, max distance, mask, nearest valid root, outline enable/disable timing, and null cleanup. |
| `CameraCollision.LateUpdate` (`Assets/Game/Scripts/Player/Camera/CameraCollision.cs:27`) | One ray every frame plus two magnitude calculations; writes a corrected camera position when blocked. | Cache `mainCamera.transform` and `focusPoint.transform`; use the already computed ray distance for hit distance only if the numeric comparison is proven equivalent. Avoid setting the same position if no transform notification is required. | NonAlloc raycast is not automatically better for one hit; test both. Update only when focus/camera inputs changed only if camera collision response remains identical. | Cushion, origin, direction, maximum distance, hit point, near obstruction behaviour, and camera update timing. |
| `VehicleGroundAlignmentController.AlignWithGround` (`Assets/Game/Scripts/Gameplay/Robots/VehicleGroundAlignmentController.cs:73`) | Performs `raysCount` downward physics queries per target each frame and averages normals/distances. Ray offsets are already cached. | Cache the parent transform and stable target metadata at initialization; avoid repeated `EnsureRayOffsets` work when values are unchanged. Measure rays per robot and valid-hit ratio. | Reuse a preallocated result buffer with an equivalent query path, or schedule the same targets across ticks only if visual response is explicitly allowed to lag. | Ray count/order, offset generation, layer mask, ray length, average normal/distance, interpolation, and `ClearTerrainContact` on zero hits. |
| `CaterpillarSuspensionController.RefreshTargetLocalPosition` (`Assets/Game/Scripts/Gameplay/Robots/t2/CaterpillarSuspensionController.cs:90`) | Repeated suspension/ground physics work from `Update`; static audit flags allocations and multiple queries. | Cache target transforms, parent transforms, ray parameters, and reusable result storage where the current API permits it. Keep target iteration and fallback unchanged. | Staggering or reducing samples requires a movement/visual regression matrix for slopes, jumps, scene reload, and death. | Local/world conversion order, ray order, hit fallback, suspension limits, and per-target update timing. |
| `WalkerAnimationController.ResolveFootGroundPosition` (`Assets/Game/Scripts/Gameplay/Robots/t1/WalkerAnimationController.cs:401`) | Raycasts each resolved foot position, with a parent-position fallback; called from per-frame foot animation. | Cache `foot.parent` and stable foot data after `InitializeFoot`; keep the ray and fallback exactly as written. Measure walking vs stopped-foot query counts. | A shared foot-query pass is possible only if each foot receives the same hit/fallback result it receives today and the call order is retained. | Ground layer, origin offset, ray length, `footOffset`, blend factor, and fallback parent position. |
| `WeaponAimController.TryRaycastAim` (`Assets/Game/Scripts/Gameplay/Robots/WeaponAimController.cs:416`) | NonAlloc raycast scans all hits to select the nearest hit outside the ignored root; it is used by camera and gun aim paths. | Cache aim mask/configuration and keep the nearest-hit scan. Avoid re-running the same camera aim query twice in one LateUpdate by reusing the current computed result only when origin/direction/distance/ignored root are identical. | A per-frame aim result cache is acceptable only with explicit invalidation on camera, gun, target, authority, and serialized setting changes. | Camera-vs-gun origin, ray direction, cast distance, ignored root, nearest hit, server/client path, and aim fallback. |
| `BotLineOfFireChecker.HasClearShot` (`Assets/Game/Scripts/AI/WaypointGraph/BotLineOfFireChecker.cs:91`) | NonAlloc raycast plus nearest-hit selection; callers may probe up to four target points in a fixed order. | Cache the aim origin for one check, reuse target transform data for the same call, and retain the current buffer and probe order. | Tick-level LOS caching or candidate throttling requires preserving AI decision timing and invalidating on shooter/target movement, rotation, and obstacle changes. | Probe order, expected target matching, nearest collider, mask, trigger mode, and AI fire decision timing. |
| `VehicleAutoAimController.TryAcquire` (`Assets/Game/Scripts/Gameplay/Robots/VehicleAutoAimController.cs:121`) | User-triggered view ray; scans candidate colliders and chooses the nearest valid enemy armor. It is not inherently a per-frame hot path. | Profile before changing it. Cache the gameplay camera/settings reference and keep the current nearest valid armor selection. | No automatic throttling. A query cache is only valid within the same user action and identical ray/settings. | User action timing, view ray, max distance, enemy validation, nearest hit, armor/root association, and acquired-target state. |
| `MatchVisibilityService.UpdateTeamSpottingPass` (`Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs:442`) | Iterates a participant-pair grid with a persistent cursor, separates previously spotted targets, and consumes a configured LOS budget. | Pre-size participant/temporary collections, reuse dictionaries/lists, and instrument pair inspections, LOS checks, cache hits, and early exits. Keep cursor and budgets unchanged. | Replace the pair grid with an equivalent indexed active-pair list only after proving identical cursor progression and skip semantics. | Pair order/cursor, dead/team checks, memory expiry, budget consumption, direct spotting state, and snapshot output. |
| `MatchVisibilityService.HasCachedLineOfSight` / `HasLineOfSight` (`Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs:561`, `:621`) | Cache keyed by spotter/target; up to three probes in fixed order; NonAlloc raycast per probe. | Reuse the static raycast buffer safely, pre-size the LOS dictionary, and avoid recomputing unchanged settings values inside the tick. | Cache invalidation can use movement/yaw stamps only if it expires at the same effective time and never returns a result for a changed position/rotation. | Probe height/radius, target yaw, cache interval, budget fallback to old value, mask, trigger mode, and authority. |
| `MatchVisibilityService.EnsureSnapshotCapacity` (`Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs:1201`) | Allocates five parallel arrays whenever participant count exceeds capacity. | Grow geometrically (for example, to the next required capacity) and retain arrays across snapshots; preserve index order and `Count`. | Pool snapshots only if ownership/serialization does not retain the arrays after send. | Array contents, version, count, order, scene reload clearing, and network serialization lifetime. |
| `GameplayPlayerListHud.CreateRow` / `LayoutRows` (`Assets/Game/Scripts/UI/HUD/GameplayPlayerListHud.cs:315`, `:546`) | Creates/configures rows and lays them out; may be called after roster changes and state updates. | Pool/reuse rows, keep stable player-id mapping, update text/colors only on value changes, and disable unused rows in one controlled pass. | Batch layout rebuilds until the end of one roster update; do not defer visible state across a frame without testing. | Row identity, ally/enemy order, selection, death/visibility state, localization, layout timing, and cleanup. |
| `GameplayMapHud.CreateIconInstance` (`Assets/Game/Scripts/UI/HUD/GameplayMapHud.cs:438`) | Icon setup is flagged by the static audit; current code already uses pooling in the steady state. | Verify pool warm-up and reset paths; prewarm only during existing HUD setup and reuse icons without changing marker identity. | Increase pool size from measured peak or batch activation; never grow the pool in a network/visibility callback without profiling. | Mini/full map parent, icon sprite/color, object id mapping, active state, relation, and despawn/reuse reset. |
| `ArrowDrawer.CreateSegment` / `CreateArrowHead` / `ClearLayer` (`Assets/Game/Scripts/UI/Tree/ArrowDrawer.cs:152`, `:180`, `:258`) | Creates and clears UI arrow objects during tree redraw; allocations are event/setup-heavy rather than continuous gameplay. | Pool arrow segments/heads, reuse `RectTransform`s, and clear by disabling/reparenting rather than destroying when the existing lifecycle permits it. | Incremental redraw or retained geometry is possible only if tree selection and layout updates remain synchronous. | Arrow endpoints, order, layer, active state, tree rebuild timing, and scene unload cleanup. |
| `DevelopmentTree.LoadDataFromServer` / `BuildFactionTree` (`Assets/Game/Scripts/UI/Tree/DevelopmentTree.cs:115`, `:277`) | Server data and tree UI construction allocate lists/objects; normally setup or data-refresh work. | Reuse model lists, pre-size from received counts, and rebuild only when the data/version changed. | Diff-based tree updates require proving that removed/unlocked/selected nodes receive the same events and visual state. | Server response interpretation, faction order, selection, unlocked state, and refresh timing. |
| `FloatingDamageText.LateUpdate` (`Assets/Game/Scripts/UI/HUD/FloatingDamageText.cs:102`) | Per-frame animation/position/lifetime for active damage text. | Cache canvas/camera/rect references, pool text instances, and avoid writing unchanged UI values. | Batch world-to-screen work only if every active text keeps the same ordering and expiry. | Spawn position, text formatting, camera projection, fade curve, lifetime, and pool reset. |
| `ProfileStatisticsPopup.ApplyStatistics` (`Assets/Game/Scripts/UI/MainMenu/ProfileStatisticsPopup.cs:116`) | Formats and applies multiple statistics, likely setup/event driven; static ranking is not proof of frame cost. | Cache field references, use explicit formatting without LINQ, and update only when the received statistics object changes. | Virtualized/stat-row pooling only if the same visible order and localization behaviour remains. | Formatting, rounding, localization, missing values, and display order. |
| `FPSCounter.OnGUI`, `ServerDebugOverlay.OnGUI`, `DiagnosticsOverlay.OnGUI` (`Assets/Game/Scripts/Core/Utils/FPSCounter.cs:26`, `Assets/Game/Scripts/Server/ServerDebugOverlay.cs:113`, `Assets/Game/Scripts/Diagnostics/DiagnosticsOverlay.cs:21`) | Immediate-mode diagnostics can allocate strings/styles every repaint; they are support/debug paths. | Return before building strings when disabled; cache GUI styles and display text cadence while preserving the existing enabled behaviour. | Replace with retained UI only as a diagnostics-only change after comparing visibility and content. | No diagnostic code in release path, enabled toggle, text cadence, and reported values. |
| `RobotEngineAudio.Update` (`Assets/Game/Scripts/Gameplay/Robots/RobotEngineAudio.cs:37`) | Per-frame audio distance/volume work. | Cache listener/robot transforms and use squared distance for threshold checks only; retain the same volume curve input when a curve needs actual distance. | Update at a lower cadence only if audio interpolation preserves the existing audible result. | Distance thresholds, volume/pitch curve, local/remote authority, and stop/start timing. |
| `PooledImpactFx.EnsureImpactGlowMaterial` (`Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs:688`) | Material setup/first use, not a steady-state projectile query. | Cache the instance and perform setup once per pooled object; reset only the properties currently reset by release. | Shared material use is unsafe unless per-instance property ownership is proven. | Material instance semantics, renderer property blocks, release reset, and visual isolation. |

## Options by domain

### Projectile and combat

1. **Measure first.** Add counters around projectile update, embedded overlap, collision cast, hit resolution, impact FX acquisition, and release. Capture active projectile count and calls per projectile. Do not infer cost from method size.
2. **Apply Tier A.** Preserve the current ballistic segment and collision order, keep existing NonAlloc buffers, cache collision-layer resolution, and remove only duplicate setup work.
3. **Pool visual lifetime separately from simulation.** A projectile can be pooled only if simulation state, trail state, ignored root, authoritative wait flags, callbacks, transform parent, and visual state are reset exactly. The pool must not change collision timing.
4. **Treat collider-surface resolution as a separate risk.** Its temporary activation is a correctness mechanism, not an obvious allocation bug. Optimize cache misses and batching before attempting to remove activation or synchronization.

### Robots, movement, and camera

1. Cache stable serialized references and parent transforms during the existing lifecycle. Do not introduce runtime searches to fill missing Inspector references.
2. Reduce repeated metadata work first: ray-offset generation, parent lookup, settings lookup, and repeated mask preparation.
3. Keep ray count/order and averaging unchanged. Any staggered ground/suspension schedule changes visual response and must be treated as a gameplay-visible experiment.
4. For camera collision, compare direct `Raycast` against NonAlloc. A single direct query may be cheaper than buffer management; the API name alone is not an optimization proof.

### Aiming, turrets, and hover

1. Keep nearest-hit selection and ignored-root filtering. These are aim semantics.
2. Cache the camera/configuration and share an aim result only within one frame when all inputs are identical.
3. Auto-aim is user-triggered, so it should not receive a speculative per-frame throttle. Profile it separately from the continuous weapon aim path.
4. Keep camera aim and gun aim as separate queries unless the origin, direction, distance, and ignored root are exactly equal.

### AI and bots

1. Retain candidate probe order and nearest-collider resolution in line-of-fire checks.
2. Replace repeated collection construction and transform/settings lookup before considering an AI tick budget change.
3. If an LOS cache is introduced, key it by shooter, target, relevant position/rotation stamps, mask/settings version, and expiry. Invalidate on scene reload, death, teleport, obstacle rebuild, and team change.
4. Validate fire/no-fire decisions on the same simulation ticks, not merely final win/loss results.

### Networking and visibility

1. Instrument pair inspections, cache hits, budget exhaustion, snapshot sizes, and RPC/payload counts before changing the scheduler.
2. Preserve the persistent pair cursor and the two-pass previously-spotted/direct-spotting semantics.
3. Reuse arrays and dictionaries, but do not reuse network payload storage while a transport serializer can still reference it.
4. Any change to LOS interval, budget, memory duration, snapshot order, or delivery timing is a network/visibility behaviour change even if the scene looks the same.

### HUD and UI

1. Separate setup/event allocations from per-frame allocations. `CreateRow`, tree construction, and map icon creation are not automatically frame hot paths.
2. Pool only objects with a complete reset contract: active state, parent, sibling/order, text, colors, sprites, animation, event handlers, layout state, and ownership IDs.
3. Batch writes within one existing roster/tree/map update, not across frames, unless one-frame latency is explicitly allowed.
4. Do not replace `.material` with `.sharedMaterial` as a UI optimization when instances can carry different visual state.

### Audio, VFX, diagnostics, bootstrap

1. Cache transforms and settings in audio updates; preserve curve inputs and thresholds.
2. Pool effects after checking trails, particle systems, lights, materials, parent attachment, and release timing.
3. Remove diagnostic allocations by gating their construction, not by deleting diagnostic information.
4. Leave scene/bootstrap/network load sequencing unchanged; optimize only repeated setup and unused temporary collections after profiling.

## Recommended rollout

### Batch 0: instrumentation, no behaviour change

- Add counters/profile scopes for the methods in the matrix.
- Record active robots, projectiles, UI rows/icons, pair counts, ray/overlap counts, buffer saturation, GC.Alloc, physics time, network tick/RPC time, and main-thread frame time.
- Capture representative cases: menu, one robot, 10v9 battle, spectator, AI battle, projectile storm, scene reload, and HUD-heavy roster changes.

### Batch 1: Tier A caches and allocations

- Cache stable references already expected by the prefab/scene.
- Reuse existing buffers/lists and geometrically grow long-lived snapshot arrays.
- Gate diagnostics and unchanged UI writes.
- Compile and run focused tests after each domain, not after a large combined patch.

### Batch 2: physics and visibility experiments

- Test one physics candidate at a time with before/after hit identity logs.
- Test visibility cache/budget changes behind a development-only switch first.
- Compare the exact same deterministic scenario, including network authority and tick sequence.

### Batch 3: pooling and UI reuse

- Pool one object family at a time: impact FX, projectiles, damage text, map markers, player rows, tree arrows.
- Add reset assertions and scene reload tests before enabling the pool by default.

### Batch 4: acceptance and cleanup

- Remove only instrumentation proven unnecessary or retain low-cost counters that protect regressions.
- Update the audit with measured deltas and mark unresolved candidates with the reason they remain unchanged.

## Validation matrix

Every implementation batch must compare exact behaviour as well as performance:

| Area | Required checks |
|---|---|
| Projectile/combat | Spawn/despawn, embedded collision, blocker vs armor hit, projectile collision, impact point/normal, miss expiry, trail fade, pooled reset. |
| Robots/movement | Ground alignment, suspension, walker feet, death transition, scene reload, inactive hierarchy cases. |
| Aim/weapon | Crosshair, zoom, camera-vs-gun aim, nearest hit, ignored own robot, auto-aim acquire/clear, server/client authority. |
| AI | Line-of-fire probe order, fire/no-fire tick, waypoint movement, AI battle, 10v9 battle. |
| Visibility/network | Team spotting, LOS cache expiry, pair cursor, snapshot version/order, RPC/payload count, spectator mode, reconnect/scene reload. |
| HUD/UI | Player rows, map mini/full markers, tree arrows, floating damage, selection/order, layout rebuild, localization and disabled-state reset. |
| Audio/VFX | Engine thresholds/curves, impact FX attachment, trail/particle reset, material instance isolation. |
| Validation tools | `dotnet build` main/test assemblies, Unity compilation, focused PlayMode tests, full PlayMode suite, `git diff --check`. |

A performance improvement is accepted only when the relevant exact-behaviour checks pass. FPS or GC improvement alone is not sufficient.

## Candidates intentionally not changed yet

- Any method whose static score is caused by setup-only UI, editor tooling, diagnostics, or a one-shot user action must be profiled before refactoring.
- Third-party packages and imported demos remain outside the safe ownership boundary unless a package upgrade or isolated wrapper is explicitly approved.
- `RaycastNonAlloc` sites already avoid managed result-array allocation; their remaining cost is physics execution and hit scanning, not automatically a GC problem.
- `new Vector3`, `new Ray`, and other value-type construction in these methods are not managed allocations by themselves.
- No CPU, GC, physics, network, or frame-time improvement is claimed until a Unity profiler capture or equivalent runtime telemetry is available.

## Current outcome

The first Tier A implementation batch is now applied in the owned runtime code. It covers stable transform/anchor caching, bot line-of-fire origin reuse, visibility list/snapshot capacity growth, and O(1) player-list indexes with linear fallback for duplicate/stale entries. Physics query parameters/order, hit selection, network scheduling/RPC payload semantics, pooling, and `Physics.SyncTransforms` were intentionally left unchanged. Runtime performance deltas still require a matched Unity Profiler capture.
