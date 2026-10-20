# Codex Task History

This is the canonical, append-only index of project tasks. It is separate from the ChatGPT/Codex conversation sidebar: deleting or losing a chat does not remove these repository records, and this file cannot restore a conversation that was never saved by the client.

## Update rules

- Keep exactly one active task in `task-handoff.md`.
- Before replacing that active task, add a compact entry here; never delete an unfinished task.
- Use `IN_PROGRESS`, `IMPLEMENTED_NEEDS_VERIFICATION`, `COMPLETED`, `BLOCKED`, or `SUPERSEDED`.
- `COMPLETED` requires verification covering every acceptance criterion. A build alone is not enough when Play Mode or user reproduction is required.
- Keep detailed acceptance criteria, files, and verification notes in `task-handoff.md`; this file is the stable index.

## Current and recent tasks

### 2026-10-20 — Client Test Mode bloom and VFX glow

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: restore bloom/post-processing for Client scene Test Mode so projectiles/tracers and CFXR Explosion glow correctly, while keeping disabled Test Mode out of normal runtime behavior.
- Verification: explicit Client test-camera URP/HDR/post-processing assertions, active Bloom/CFXR references, C# build with 0 errors, and `git diff --check` passed; Play Mode visual confirmation remains pending.
- Detail: `task-handoff.md`.

### 2026-10-20 — Client scene vehicle test mode

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add an editor-gated, scene-owned vehicle test mode using the existing Client HUD/menu hierarchy while leaving normal Client startup unchanged when disabled.
- Detail: `task-handoff.md`.

### 2026-10-20 — Bot AI improvements

- Status: `COMPLETED`
- Scope: moving-target lead and aim, unexplored waypoint exploration, eight-direction proximity sensing every 0.5 seconds, local avoidance, and allied target distribution.
- Verification: C# build, static checks, and isolated runtime smoke passed. The smoke spawned 4 bots and observed target acquisition, moving-target lead velocity, robot/player movement, unexplored-node exploration, authoritative bot firing, and proximity avoidance. VehicleTest batchmode UI-button invocation was unavailable, but no gameplay changes were made by those attempts.
- Detail: `task-handoff.md`, section `Previous active task (bot AI improvements)`.

### 2026-10-20 — Impact decal isolation

- Status: `COMPLETED`
- Scope: world impacts no longer project onto robots; robot impacts are isolated from world and other robots; pooled rendering-layer state resets correctly.
- Verification: VehicleTest Play Mode reproduced separate wall and robot impacts without the temporary hit marker; prefab mask assertions, C# build, and `git diff --check` passed.
- Detail: `task-handoff.md`, section `Previous completed task (impact decal isolation)`.

### 2026-10-20 — Earlier impact decal implementations

- Status: `SUPERSEDED`
- Scope: the mesh-mark fallback, the initial projector targeting, and the robot-only rendering-layer experiments were replaced by the completed isolation implementation above.
- Detail: archived in `task-handoff.md` under the older `Previous active task` sections.

### 2026-10-20 — Faction crosshair and HUD wiring

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: faction-specific crosshair bindings, reload/light-element state, weapon HUD values, and the rebuilt GameplayHUD hierarchy.
- Verification: source/serialization checks and builds passed; the recorded Play Mode visual checks remained pending.
- Detail: archived in `task-handoff.md` under the crosshair and HUD sections.

### 2026-10-20 — Projectile impact decal foundation

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: pooled URP DecalProjector impact FX with hit-point/normal placement, lifetime, fade, and active-object limits.
- Verification: static checks and a C# build passed; Unity import and Play Mode verification were still pending before later isolation work superseded parts of this implementation.
- Detail: archived in `task-handoff.md` under `Previous active task carried forward`.

### 2026-10-19/20 — Robot visibility and death debris

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: complete robot renderer visibility lists, armor exclusion from debris, per-mesh debris physics, and death detachment.
- Verification: editor repair/static integrity checks and builds passed; Play Mode visibility/death confirmation remained pending.
- Detail: archived in `task-handoff.md` under the robot renderer, armor cleanup, and mesh debris sections.

### 2026-10-19 — Video settings Apply state

- Status: `COMPLETED`
- Scope: Video Apply is enabled only when controls differ from the applied baseline.
- Verification: Play Mode confirmed disabled → enabled → disabled behavior through change, revert, and Apply flows.
- Detail: archived in `task-handoff.md` under `Previous completed task (settings)`.

### Earlier UI and progression tasks

- Status: mixed (`COMPLETED`, `IMPLEMENTED_NEEDS_VERIFICATION`, `SUPERSEDED`, or user-closed), exactly as described in the archived handoff.
- Scope: dropdown rebuilds, main-menu robot-card scrolling, post-battle rewards, and procedural sniper scope mask.
- Detail: `task-handoff.md`, section `Completed task history`.

## Important limitation

The repository can preserve task state and verification notes, but it cannot control or recover the external Codex/ChatGPT chat list. If a conversation disappears from the sidebar, check the same account/workspace, archived chats, and client sync; use this file as the project-side recovery record.
