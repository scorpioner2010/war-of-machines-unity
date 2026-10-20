# Codex Task History

This is the canonical, append-only index of project tasks. It is separate from the ChatGPT/Codex conversation sidebar: deleting or losing a chat does not remove these repository records, and this file cannot restore a conversation that was never saved by the client.

## Update rules

- Keep exactly one active task in `task-handoff.md`.
- Before replacing that active task, add a compact entry here; never delete an unfinished task.
- Use `IN_PROGRESS`, `IMPLEMENTED_NEEDS_VERIFICATION`, `COMPLETED`, `BLOCKED`, or `SUPERSEDED`.
- `COMPLETED` requires verification covering every acceptance criterion. A build alone is not enough when Play Mode or user reproduction is required.
- Keep detailed acceptance criteria, files, and verification notes in `task-handoff.md`; this file is the stable index.

## Current and recent tasks

### 2026-10-21 - Centralize projectile glow settings

- Status: `COMPLETED`
- Scope: move projectile-local glow and Bloom-source tuning from impact materials into inspector fields on `PooledImpactFx`, while leaving global camera Bloom in the URP volume.
- Acceptance: independent halo/crack/overall strength, HDR halo color, halo size, fade, prefab defaults, documentation, and build validation.
- Verification: `PooledImpactFx` applies all new fields to its runtime material instances; the prefab serializes the current defaults; the C# build passed with 0 errors; static assertions and `git diff --check` passed.
- Detail: completed in `task-handoff.md`.

### 2026-10-21 - Follow-up: stronger projectile Bloom diagnosis completed

- Status: `COMPLETED`
- Scope: replace the ineffective sparse-Decal-only Bloom path with a projectile-owned soft HDR halo, preserve the regular impact, and verify the fade path.
- Root cause: the Decal emission texture contains thin crack pixels, so increasing `Emission_Strength` made crack detail brighter but could not create the requested broad halo; the shared HDR/Bloom path itself was working.
- Final settings: `ImpactGlowDecal.mat` emission `50`, prefab `Impact Glow Strength=5`, `Impact Glow Fade Duration=5` as currently saved, `ImpactGlowHalo.mat` with the soft radial mask, and Bloom profile `threshold=0.5`, `intensity=2`, `scatter=1`, high-quality filtering enabled.
- Verification: Unity runtime prefab probe showed a strong circular Bloom halo; same-runtime fade capture removed the halo while the regular impact remained visible. C# build passed with 0 errors; `git diff --check` passed.
- Detail: implementation and verification are archived in `task-handoff.md`.

### 2026-10-21 — Follow-up: stronger projectile Bloom diagnosis

- Status: `IN_PROGRESS`
- Scope: diagnose why manually raising projectile glow values still does not produce the requested strong Bloom halo, then verify a corrected projectile-only HDR/Bloom path in Unity Play Mode.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 — Projectile Bloom rendering-layer fix and Play Mode verification

- Status: `COMPLETED`
- Scope: identify why projectile Bloom did not respond to stronger values, restore the world decal rendering path, raise the projectile-only emission profile, and keep the regular impact persistent while the glow fades over two seconds.
- Root cause: world impacts used rendering-layer mask `2`, but the active URP decal renderers accepted only mask `1`, filtering the glow projector before Bloom.
- Final settings: `ImpactGlowDecal.mat` emission `8`, prefab multiplier `2` (effective `16`), projector size `1.2`, and glow fade `2` seconds; runtime fades the material emission and projector factor together.
- Verification: Unity baseline shot showed no halo; the boosted/fixed Play Mode shot showed a visible immediate Bloom halo; a frame after more than two seconds showed the halo gone while the regular impact remained; Console had `0` errors; C# build and `git diff --check` passed.
- Detail: archived in `task-handoff.md`, active task section.

### 2026-10-21 — Refresh saved projectile Bloom emission

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: refresh the pooled glow material's source `Emission_Strength` before each impact and persist the screenshot's base value `22`, so editor changes reach the active URP decal and its Bloom input.
- Verification: source inspection confirms refresh-before-activation ordering; Unity Play Mode visual confirmation remains pending.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 — Fix cached Bloom emission strength

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: apply the per-instance HDR glow material strength before enabling the URP `DecalProjector`, ensuring the decal entity cache receives the configured emission intensity used by Bloom.
- Verification: source ordering assertion passed and `git diff --check` passed; Unity Play Mode visual confirmation remains pending.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 — HDR impact glow strength

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: allow `Impact Glow Strength` values above `1` by scaling each pooled glow material's `Emission_Strength`, while keeping normalized fade control and existing decal pooling behavior.
- Verification: upper Inspector/runtime clamps were removed, the runtime material property path is present, and `git diff --check` passed; Unity Inspector/Play Mode visual confirmation remains pending.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 — Impact glow fade and persistent scorch

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: keep the regular impact scorch visible while fading the emission impact glow with unscaled smooth-step timing over two seconds; preserve bounded pooling and reset behavior.
- Verification: obsolete field/method search is clean, prefab serialization uses `impactGlowFadeDuration: 2`, and `git diff --check` passed; Unity Play Mode visual confirmation remains pending.
- Detail: `task-handoff.md`, active task.

### 2026-10-20 — Remove unused projectile assets

- Status: `COMPLETED`
- Scope: remove only unreferenced resources from `Assets/Game/Content/Combat/Projectiles`, preserving active projectile, muzzle, impact, material, texture, and shader dependencies.
- Verification: four unused legacy textures were archived with GUIDs preserved; all remaining projectile assets are reached by active dependency closure; archived assets have no active references; the active folder contains no legacy files; documentation diff check passed.
- Detail: `task-handoff.md`, active task.

### 2026-10-20 — Projectile asset organization

- Status: `COMPLETED`
- Scope: group active projectile, impact, muzzle, tracer, material, texture, shader, and related shot FX assets under `Assets/Game/Content/Combat/Projectiles` without breaking Unity references.
- Verification: five moved assets retained their original GUIDs, active shot dependencies are contained in the target folder, scene/prefab references resolve, old Effects paths are absent, and `git diff --check` passed.
- Detail: `task-handoff.md`, active task.

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
