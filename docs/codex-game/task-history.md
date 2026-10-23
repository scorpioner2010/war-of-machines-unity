# Codex Task History

This is the canonical, append-only index of project tasks. It is separate from the ChatGPT/Codex conversation sidebar: deleting or losing a chat does not remove these repository records, and this file cannot restore a conversation that was never saved by the client.

## Update rules

- Keep exactly one active task in `task-handoff.md`.
- Before replacing that active task, add a compact entry here; never delete an unfinished task.
- Use `IN_PROGRESS`, `IMPLEMENTED_NEEDS_VERIFICATION`, `COMPLETED`, `BLOCKED`, or `SUPERSEDED`.
- `COMPLETED` requires verification covering every acceptance criterion. A build alone is not enough when Play Mode or user reproduction is required.
- Keep detailed acceptance criteria, files, and verification notes in `task-handoff.md`; this file is the stable index.

## Current and recent tasks

### 2026-10-23 - Smooth tracked reticle presentation

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make the local reticle follow the live point of an active auto-aim target while preserving manual gun-ray behavior, physical dispersion sizing, and existing exponential smoothing.
- Acceptance: active auto-aim reticle follows a moving target smoothly; manual reticle behavior and projectile/dispersion calculations remain unchanged; focused build/static checks pass and Play Mode reproduction is recorded.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Pre-global aiming rollback and zoom reimplementation

- Status: `IN_PROGRESS`
- Scope: restore the complete pre-`bdc51fa3` aiming/reticle system first, verify that baseline, then reapply the configurable zoom profile arrays and wheel-step logic without reintroducing the global physical-ring behavior.
- Acceptance: the legacy sight works independently of the new zoom; the new zoom arrays/steps, FOV, distance, sniper presentation, and HUD step display work after phase two; projectile direction, server fire validation, authoritative dispersion, and manual aim remain unchanged; focused builds/tests and runtime limitations are recorded.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Dead robot invisible collision isolation

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: ensure live vehicles collide only with configured detached non-armor mesh debris after an enemy dies; disable dead-root/gameplay/armor blockers and preserve optimized movable wreck obstacles for bots.
- Acceptance: no invisible dead-root collision remains; detached mesh colliders stay movable and pushable; bot obstacle detection remains correct; focused verification and documentation are recorded.
- Detail: active record is in `docs/codex-game/task-handoff.md`.
- Verification update: focused Unity PlayMode `DeadRobotDisablesGameplayCollidersAndKeepsOnlyDetachedMeshDebris` passed 1/1 across all eight registered IA/NV prefabs. The full `Game.PlayMode.Tests` run passed 17/20; the three unrelated failures were a server connection failure in `TenEnemyBotsAndNineAllyBotsRunUntilOneTeamFalls` and null player creation in the two `MatchBattleEndPlayModeTests` cases. `DetachedDeathChassisColliderIsAnObstacle` and `ProximityMaskAlwaysIncludesDeathChassisLayer` both passed. Live vehicle drive-through/debris-push reproduction remains pending.

### 2026-10-23 - Death camera, persistent wrecks, and spectator cycling

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: after a player dies, keep the destroyed robot visible, show the killer through the gameplay camera for three seconds without orbit input, return to the wreck, then allow left-click spectator cycling through all robots including the destroyed owner; make dead robots physical obstacles for bots.
- Acceptance: authoritative killer identity reaches the client; death-cam and spectator transitions preserve the existing camera/HUD ownership; destroyed vehicles and detached parts do not disappear during the match; bots raycast dead robot geometry and steer around it without selecting/shooting it as a live target; focused tests are compiled and runtime verification remains pending in the open Editor.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Dead player freezes after team victory

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: diagnose and fix the match-end deadlock when a connected player dies, waits for the surviving team to win, and remains stuck in the gameplay HUD instead of reaching the end-game/result flow.
- Acceptance: a dead connected real player still receives the end-game transition after the vehicle despawns; the post-battle result can progress to the main menu; focused regression coverage and Unity/build verification are recorded.
- Initial finding: `BattleRules.ShouldReceiveEndGame` required `player.playerRoot != null`, while `GameResultUI` waits for `MainMenu`; after the dead vehicle despawned, no EndGame menu was sent and the queued result had no path to display.
- Implementation: removed the vehicle-root lifetime requirement from the EndGame recipient gate and added focused PlayMode coverage for a connected real player with a null `playerRoot`, plus disconnected/abandoned negative cases. Runtime and test C# projects build successfully; Unity PlayMode execution remains pending because the project Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Final repeat 10v9 bot battle

- Status: `COMPLETED`
- Scope: run one final exact Client battle with 10 enemy bots and 9 allied bots after the completed bot AI repairs.
- Acceptance: battle completes without timeout or crash; final alive counts and runtime movement/turret telemetry are recorded.
- Verification: Unity batch PlayMode `BotBattle10v9PlayModeTests` passed `1/1` with `0` failures and `0` skipped in `193.208s`; the final battle completed without timeout or crash and ended after the last ally was eliminated. Final telemetry reached `5 enemy / 1 ally`, with maximum observed body direction flips `66` and turret direction flips `74`.

### 2026-10-23 - Deep 10v9 bot battle audit

- Status: `COMPLETED`
- Scope: run the Client with exactly 10 enemy bots and 9 ally bots, observe the battle to completion, diagnose body oscillation/turret tracking and every recurring movement or combat failure, then fix and rerun.
- Acceptance: complete live 10v9 observation; root-cause fixes for repeated left/right rotation and turret-follow failures; focused regression coverage or runtime evidence for each production change; successful post-fix battle and Unity verification.
- Verification: Unity PlayMode `BotAiRegressionPlayModeTests` passed `7/7`; the exact `10 enemy / 9 ally` Client battle passed in `225.609s` and reached `3 enemy / 1 ally` before one team was eliminated. A repeat exact `10 enemy / 9 ally` battle passed in `216.603s` and ended with the last ally eliminated. Runtime fixes covered steering smoothing/dead zones, preservation of steering while the same target moves, stuck-pivot progress, turret yaw hold, no-line-of-fire kite recovery, and a narrower same-team fire-lane blocker radius. CUA visual control was unavailable, so the deep observation used runtime telemetry.

### 2026-10-23 - Make bot navigation, perception, and combat human-like

- Status: `COMPLETED`
- Scope: run reproducible Client/PlayMode bot scenarios with allied and enemy robots, diagnose spinning/stuck movement, missed visibility around cover, failed moving-target aim, and missed firing opportunities, then fix the root causes and rerun verification.
- Acceptance: stable open-route navigation; responsive target acquisition and lead aim; authoritative firing when a valid shot exists; sensible team-aware tactical movement; successful post-fix Unity/build verification or a concrete recorded blocker.
- Verification update: replaced visual bot gun-pitch snapping with frame-rate-independent exponential `Mathf.LerpAngle` convergence, set all eight registered IA/NV robot prefabs to `smoothSpeed: 10`, and added a regression proving smooth convergence is frame-rate independent. Fresh Unity Test Runner `Game.PlayMode.Tests.dll` passed `11/11` tests with `0` failures and `0` skipped in `12.333s`; both C# project builds passed with `0` errors. Interactive post-fix pitch comparison remains a manual limitation because the embedded Game View input/focus path was unreliable; the earlier Client session still verified active ally/enemy combat and movement.

### 2026-10-23 - Real Unity verification and repair of robot impact decals

- Status: `COMPLETED`
- Scope: execute real PlayMode/runtime checks for enemy and allied robot impact decals, then repair and rerun until both factions are verified.
- Acceptance: actual Unity test execution, runtime reproduction for both factions, a fix for any remaining root cause, and a successful post-fix rerun.
- Verification: Unity Test Runner `Game.PlayMode.Tests.dll` passed `7/7` tests with `0` failures and `0` skipped in `4.998s`. The IA/NV robot-variant test covered all four IA and four NV robot prefabs, selected the robot impact profile through `ProjectileVisual.prefab`, confirmed the regular decal remained active after `0.25` seconds, and confirmed target-only rendering layers. Robot, world, turret, fence, and dispersion regressions also passed. Both C# project builds passed with `0` errors. `git diff --check` reports only pre-existing trailing whitespace in `Assets/Game/Scenes/Map.unity`.
- Result: the existing root re-registration and target-layer reuse fixes are validated in Unity PlayMode; no additional production patch was needed after the runtime rerun. Detailed record: `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Diagnose enemy impact decals not persisting

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: find and fix the difference between enemy and allied robot decal persistence without regressing target-only rendering layers or pooled cleanup.
- Acceptance: enemy robot impacts retain the regular decal like allied impacts; world and other-object impacts remain unchanged; root cause and verification are recorded.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo` passed with 0 errors; static impact-layer audit and `git diff --check` passed. Unity PlayMode and visual network verification remain pending.
- Status update 2026-10-23: root cause was target-only layer exhaustion. Six bits (`2-7`) were allocated once per persistent impact; after exhaustion, the code silently used world mask `2`, while `VehicleColliderRegistry` intentionally removes world bit `1` from robot renderers. The fix reuses a layer for repeated impacts on the same renderer set and expands the allocator to bits `2-31`; a PlayMode regression now covers eight repeated impacts on one turret renderer. Networked roots also re-register their assigned `NetworkObject.ObjectId` at server/client start so authoritative enemy impacts cannot lose the target root and fall back to the world profile.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Connect emission textures for split impact FX

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: connect the current emission texture/material inputs in both robot-impact and other-object impact FX folders without replacing user texture assignments.
- Acceptance: both variants expose connected emission inputs, preserve the user's current texture resources, and keep valid prefab/profile serialization.
- Verification: both variants' material/GUID audit, shader graph emission-edge audit, and `git diff --check` passed; Unity import and Play Mode visual verification remain pending.
- Status update 2026-10-23: current `Emossion.png` is bound to each `ImpactGlowDecal.mat` `Base_Map`; current `Glow.png` is bound to each halo `_BaseMap` and `_EmissionMap`; the other-object regular decal now points to its current local `Decal.png`.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Split robot and non-robot impact FX into independent prefabs

- Status: `IN_PROGRESS`
- Scope: replace the combined `PooledImpactFx` two-profile setup with separate robot-hit and other-object impact prefabs, asset folders, and runtime pool selection.
- Acceptance: robot hits use only the robot FX prefab/assets; world and non-robot hits use only the other-object prefab/assets; no child/material references are shared; projectile prediction, authoritative correction, generic playback, and pooling remain functional.
- Verification: not started; unrelated existing font asset modification is preserved.
- Status update 2026-10-23: implementation complete; production/editor C# builds, `git diff --check`, and serialized GUID/material separation audit passed. Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Smooth reload circle and match floating HP colors

- Status: `IN_PROGRESS`
- Scope: make the weapon reload/charge circle fill continuously during charging and color floating damage text blue for allies and red for enemies, matching the world HP bar relation color.
- Acceptance: the reload circle uses the smooth local/server charge progress without bypassing reload authority; lower-progress resets remain immediate; floating text receives the same ally/enemy color as the target HP bar.
- Verification: targeted static assertions, `git diff --check`, and production/editor C# builds pass with 0 errors; Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Disable DevelopmentTree robot image when no technique is selected

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: keep the selected robot icon on `DevelopmentTree/RightInfoPanel/Image`, but deactivate its GameObject when no tree technique is selected.
- Verification: Image GameObject toggle and scene reference pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Show selected robot image in DevelopmentTree

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: bind the existing `DevelopmentTreeNew/RightInfoPanel/Image` to the selected robot icon and clear it when no node is selected.
- Verification: serialized Image reference and selected/clear sprite binding pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Disable Vehicle BlackScreen when unselected

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: deactivate the `BlackScreen` GameObject for unselected vehicle items and activate it only for the selected pulse.
- Verification: BlackScreen inactive default, selection-driven activation/deactivation, alpha restoration, and no-scale behavior pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Limit Vehicle pulse to selected items

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: gate the `Vehicle` BlackScreen alpha pulse by `VehicleSlotVehicleData.IsSelected` and restore the authored alpha when deselected.
- Verification: selection gating, authored-alpha restoration, serialized references, and no-scale behavior pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Add Vehicle BlackScreen alpha pulse

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add the same configurable ping-pong alpha pulse used by `TreeItem` to the `Vehicle` prefab, targeting `BlackScreen` and omitting scale animation.
- Verification: serialized `VehicleBlink` reference, `BlackScreen` CanvasGroup target, alpha-only implementation, and configurable settings pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Sync TreeItem CanvasGroup alpha with selection

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: set the TreeItem root CanvasGroup alpha to `0.35` when unselected and `1` when selected, while preserving the existing `IsSelect` behavior.
- Verification: serialized CanvasGroup reference, default alpha, and selected/unselected assignments pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Bind robot names and TreeItem level text

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: write the resolved robot name into the main-menu `Vehicle` prefab and wire `TreeItem` name/level TMP references so the existing runtime data appears.
- Verification: `Vehicle.nameText`, `TreeItem.vehicleName`, and `TreeItem.level` serialized references pass static checks; production/editor C# builds pass with 0 errors. Unity Play Mode visual verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Synchronize TreeItem IsSelect with development-tree selection

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: expose the `IsSelect` child through `TreeItem` and make `DevelopmentTree` keep exactly the selected item active while every other item is inactive.
- Verification: `IsSelect` is wired and inactive by default; selection switching, faction changes, and tree rebuild clearing are covered by source assertions. Production/editor C# builds passed with 0 errors; Play Mode verification remains pending.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Add configurable TreeItem image blinking

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add a reusable inspector-configurable blink component to `TreeItem.prefab`, assign `Image (0)` through `Image (3)`, and animate alpha plus local Y scale together at an adjustable speed.
- Verification: `TreeItemBlink` is serialized on the prefab root with all four image references in order; production/editor C# builds and targeted diff checks pass. Optional Unity Play Mode visual verification remains pending after Inspector tuning.
- Detail: active task record in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Resize robot images to 1024 square

- Status: `COMPLETED`
- Scope: resize four IA and four NV robot PNG images from 1254x1254 to 1024x1024 while preserving transparency, filenames, and Unity metadata.
- Verification: all eight PNGs are 1024x1024 RGBA with transparency retained; `.meta` sidecars are unchanged. Original PNGs were backed up under `work/robot-images-1024-originals`.
- Detail: active task record is in `docs/codex-game/task-handoff.md`.

### 2026-10-22 - Keep reticle narrowing continuous to full aim

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: prevent the reticle from visually plateauing at `reticleBaseDiameter` while the server dispersion and HUD aim status continue narrowing.
- Bug: `GetCrosshairDiameter` used the profile base diameter as a hard `Mathf.Max` floor, so the ring stopped early even though `AimStatus` still reflected remaining dispersion.
- Detail: the hard base-diameter floor was replaced by a calibrated fully-aimed baseline plus continuous physical expansion; builds/static checks pass, while Client Play Mode visual verification remains pending.

### 2026-10-22 - Use server-authoritative dispersion for HUD narrowing

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make the visible reticle and HUD aim-progress indicator follow the synchronized server dispersion value instead of diverging from it through the client-only model.
- Bug: the ring narrows faster than the HUD aim status because `NetworkWeaponShooter` sends `_ownerDispersion.CurrentDeg` to the main ring/status while the server value is only used for the optional server reticle.
- Detail: the main ring and `AimStatus` now use synchronized server dispersion; builds and static checks pass, while final Client Play Mode visual verification remains pending.

### 2026-10-22 - Preserve aim point during sniper camera transition

- Status: `COMPLETED`
- Scope: prevent the visible aim point from shifting when the camera moves to or from a checked sniper-scope zoom profile.
- Bug: the wheel path changes the camera origin to the gun/sniper anchor but leaves the previous orbit yaw/pitch unchanged, creating parallax and changing the apparent target.
- Detail: `ApplyZoomStep` now preserves the existing world-space aim point when moving the camera to/from any checked sniper profile; Client Play Mode, production/editor builds, static checks, and `git diff --check` passed.

### 2026-10-22 - Allow multiple sniper-scope zoom profiles

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make every checked forward zoom profile reachable and keep the black sniper-scope overlay active on each checked step.
- Bug: `ZoomIn` stopped whenever the current profile was marked sniper, so the next checked profile could never be reached. Wheel traversal assertions and production/editor builds now pass; manual Play Mode verification remains pending.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Correct sniper-scope checkbox semantics

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make `showReticle = true` on a forward zoom profile activate the existing black sniper-scope overlay and central reticle.
- Correction: the previous migration used `false` as the sniper marker, which did not match the user's inspector expectation and conflated the scope overlay with the ordinary dispersion ring. Runtime/editor builds and static assertions now pass; manual Play Mode verification remains pending.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Forward zoom profile controls sniper mode

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: remove the standalone sniper zoom profile and select sniper mode from a forward-array profile using a serialized `showReticle` checkbox.
- Acceptance: the flagged forward profile drives sniper camera/scope behavior and all profile values; the standalone field and stale serialization/editor references are removed.
- Correction: added `CameraZoomProfile.showReticle`, migrated the old sniper values into the final forward-array element with `showReticle = false`, removed `cameraZoomSniper`, and refactored camera step selection. Builds and static migration checks passed; PlayMode was blocked by the already-open Unity Editor/licensing process.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - HUD ZoomLevel display

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: display the live camera zoom level in the existing HUD `ZoomLevel` text using `0` for default, negative backward steps, and positive forward steps.
- Acceptance: serialized HUD text reference is wired; values update on initialization, vehicle/HUD changes, and every zoom change without changing camera behavior.
- Correction: `GameplayGUI` updates both scene `ZoomLevel` texts from the signed `_currentZoomStep`; default is `0`, backward steps are negative, forward steps are positive, and sniper uses the next positive step. Builds and focused PlayMode tests passed 2/2; manual native-input verification remains pending.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Runtime zoom distance not applied

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make every serialized zoom profile field work at runtime; user reports FOV/profile parameters apply, but `cameraDistance` does not visibly change the live camera.
- Acceptance: default, forward, backward, and sniper profile distances affect the camera after zoom input, smoothing, collision, and recovery without changing reticle/projectile logic.
- Correction: `CameraController` refreshes `cameraDistance` every update before writing the camera transform; the sniper path and sniper alignment now also apply the selected profile distance. Builds and fresh focused PlayMode tests passed 2/2. Manual Client Play Mode input verification remains pending.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Reticle over-expansion during movement and turret rotation

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: fix IA_L2_Scout's reticle becoming much larger than the actual projectile hit area while moving, rotating the turret, or doing both.
- Root cause found: `NetworkWeaponShooter.GetCrosshairDiameter` multiplied the base/calibrated reticle by `currentDispersion / minimumDispersion`; IA_L2_Scout's `6° / 0.35°` range created an accidental approximately `17.1x` expansion.
- Correction: use the current camera-projected physical cone as the authoritative size, with the profile base diameter as a floor only and no dispersion-ratio inflation. Production/editor builds passed, fresh focused PlayMode projection tests passed 2/2, and the Client scene is open for manual reproduction.
- Remaining verification: native input injection is unavailable, so the movement/turret Play Mode sequence still needs user confirmation.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Full zoom and dispersion ring rewrite

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: replace the legacy zoom-step and approximate reticle sizing with a default profile plus independent forward/backward profile arrays and camera-projected dispersion rings.
- Acceptance: old zoom fields and zoom-normalized ring math are removed; the active camera projection visualizes the same physical projectile cone during every zoom/FOV transition; profile values and multipliers are serialized and documented; static checks, builds, and Unity verification pass.
- Detail: active task record is in `task-handoff.md`; runtime/editor builds pass, Unity PlayMode projection tests pass 2/2, and only full-scene manual boundary testing remains.

### 2026-10-22 - AI reverse braking inertia

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: diagnose and fix robots taking longer to stop when reverse input is pressed than when movement is released, for both AI and player input.
- Root cause found: `VehicleMovementController` applies faction stopping multipliers only for neutral coasting, while active direction reversal uses only chassis braking multipliers; active prefab values make reversal slower (`IA 3` vs `4`, `NV 3` vs `10`).
- Verification: production/editor C# builds passed with 0 errors, reversal-rate assertions passed for IA/NV standard and legged movement, and `git diff --check` passed; Play Mode/user reproduction remains pending.
- Detail: active task in `task-handoff.md`.

### 2026-10-22 - AI reverse braking distance ratio

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make active reverse input reduce the stopping distance to approximately half of neutral coasting distance for the same robot speed and movement profile.
- Prior implementation summary: the shared movement path combines faction stopping and chassis braking multipliers during direction reversal, and local `ClientVehicleTestMode` instantiates the assigned `ServerSettings` prefab. IA stopping is `0.2` versus NV `1.0`.
- New acceptance: releasing movement remains the baseline; pressing the opposite direction must produce approximately `2x` deceleration, so a `10 m` neutral stop should become about `5 m` when braking.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Reduce robot inertia fiftyfold

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: reduce the configured neutral stopping inertia for both NV and IA robots by `50x` because the current stopping response is still too long.
- Configuration change: increase the stopping-strength multipliers from NV `1.0` to `50.0` and IA `0.2` to `10.0`; keep the reversal-only brake multiplier at `2.0`.
- Expected result: for the same vehicle and speed, neutral stopping time/distance is approximately `1/50` of the previous profile, and holding reverse remains twice as strong as neutral coasting.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - Tune robot inertia to tenfold strength

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: soften the previous `NV=50` / `IA=10` stopping profile because it made robots stop too abruptly.
- Configuration change: use NV `10.0` and IA `2.0`, preserving the IA/NV five-to-one neutral stopping-time ratio and the reversal-only multiplier `2.0`.
- Expected result: stopping strength is one fifth of the previous profile (`10x` over the original NV baseline), with reverse braking still twice as strong as neutral coasting.
- Detail: active task record is in `task-handoff.md`.

### 2026-10-22 - AI robot wheel logic and references

- Status: `COMPLETED`
- Scope: restore rebuilt AI wheel collider/Rigidbody contracts, repair serialized wheel references, and correct left/right wheel rotation while preserving authored positions.
- Acceptance: all affected wheel physics and references resolve; wheel animation directions are correct for forward/reverse and pivot turns; positions and unrelated geometry remain unchanged; documentation and build/static validation are updated.
- Implementation: editor repair now restores IA wheel-side arrays, per-mesh disabled convex colliders, kinematic rigidbodies, and matching death entries without changing wheel transforms.
- Verification: Unity batch repair/validation passed; static prefab checks confirmed actual left/right hierarchy ownership, side counts, `+900/-900` rotation signs, per-mesh physics, and unchanged transform properties. Production/editor builds and `git diff --check` passed.
- Detail: active task in `task-handoff.md`.

### 2026-10-22 - Center rusted_wheel FBX pivot

- Status: `COMPLETED`
- Scope: center the single mesh pivot in its geometry bounds and replace only the FBX while preserving world geometry, materials, importer metadata, and sibling files.
- Verification: Blender 5.1.2 reimport preserved names, topology, UV data, material slots, and world bounds; max world-vertex delta `0.0`; pivot local center `(0, 0, 0)`; sibling files unchanged.
- Detail: completed in `task-handoff.md`.
### 2026-10-21 - Per-part robot impact projector isolation

- Status: `COMPLETED`
- Scope: isolate a robot impact projector to the turret/body mesh that was hit, so another overlapping part such as the gun does not receive the turret impact.
- Implementation: `DeathLogic` registers detachable mesh-collider/renderer pairs; `VehicleColliderRegistry` resolves the hit pair; `PooledImpactFx` assigns one temporary layer per active impact and restores every mask on release.
- Verification: Unity PlayMode regression suite passed `5/5`, including simultaneous turret/gun isolation; production and PlayMode C# builds passed with `0` errors; `git diff --check` passed.
- Detail: active task in `task-handoff.md`.

### 2026-10-21 - Robots invisible before first shot

- Status: `COMPLETED`
- Scope: diagnose and fix robot renderers remaining invisible until a projectile is fired at a robot, while preserving intentional visibility culling and impact FX behavior.
- Root cause: the impact-layer cleanup removed rendering-layer bit `0` from robot renderers; that bit is the default lighting layer, so robots lost their normal lighting before any shot occurred.
- Verification: corrected cleanup to remove only world-impact bit `1`; Unity PlayMode regression suite passed `4/4`, production and PlayMode C# builds passed with `0` errors, and `git diff --check` passed.
- Detail: active task in `task-handoff.md`.

### 2026-10-21 - Ground impact projection regression

- Status: `COMPLETED`
- Scope: stop persistent world impact projectors from rendering on robot renderers after a robot walks into the impact volume, and keep world FX unparented.
- Root cause: registered robot renderers still contained world rendering layer bit `0`, while world impact projectors intentionally use bit `0`; the robot-only target layer was applied only on robot hits.
- Verification: fresh Unity PlayMode fixture passed `4/4`, including the robot movement regression; production and PlayMode C# builds passed with 0 errors; `git diff --check` passed.
- Detail: completed in `task-handoff.md`.

### 2026-10-21 - Robot Impact Glow Halo toggle follow-up

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add a per-profile `Impact Glow Halo` toggle, disable it for robot hits in the active impact prefab, and preserve it for non-robot hits.
- Verification: the script/prefab/test implementation compiled and the Unity PlayMode test assembly recompiled; a fresh PlayMode run was still pending when this regression task started.
- Detail: archived in `task-handoff.md`.

### 2026-10-21 — Deterministic robot impact surface PlayMode verification

- Status: `COMPLETED`
- Scope: Replaced unreliable manual aiming with a deterministic real-prefab side-hit test; registered serialized robot visual mesh colliders, corrected robot impact points before FX setup, and verified pool rendering-layer cleanup.
- Verification: Unity selected PlayMode test passed with `haloToMesh=0.00000` and `haloToProxy=0.52875`; the production assembly build passed with 0 errors.
- Detail: The test uses `IA_L1_Starter`, an oversized proxy collider, a real `PooledImpactFx` prefab, and logs the measured proxy, visual mesh, resolved, and halo positions.

### 2026-10-21 - Reproduce Hangar impact alignment in Client

- Status: `IN_PROGRESS`
- Scope: reproduce the requested `Target1` shot against `Hangar_v1_full (2)` in Client, identify the actual collider/mesh mismatch, and verify the correct impact placement.
- Acceptance: exact Unity reproduction, measured root-cause fix, repeated shot verification, build, and documentation.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 - Align impact FX with deeper visual mesh

- Status: `COMPLETED`
- Scope: correct impact FX placement when `RaycastHit.point` lands on a proxy collider outside the visible mesh, using a signed `PooledImpactFx` surface offset.
- Acceptance: inward/outward tuning, consistent decal/halo placement, preserved defaults, documentation, and build validation.
- Verification: signed offset is applied to all impact visual layers and the FX root; the prefab default remains `0.003`; build, static assertions, and `git diff --check` passed.
- Detail: completed in `task-handoff.md`.

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

### 2026-10-21 - Separate robot and non-robot impact FX profiles

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: split `PooledImpactFx` into complete `robotImpactProfile` and `otherObjectImpactProfile` inspector profiles, selected by the presence of a registered `VehicleRoot`, and migrated all five serialized prefab instances.
- Verification: C# build passed with `0` errors, all five prefab instances passed profile-serialization assertions, and targeted `git diff --check` passed; Play Mode comparison with intentionally different values remains pending.
- Detail: `task-handoff.md`, active task.

### 2026-10-21 — Visual-mesh Impact Glow Halo alignment

- Status: `COMPLETED`
- Scope: resolve `Impact Glow Halo` against serialized visible meshes for robots and non-robot proxy colliders, carry the local hit collider into `PooledImpactFx`, map the five concrete fence prefabs, and preserve robot layer isolation/pool reset.
- Verification: fresh Unity PlayMode run passed `3/3` tests, covering all eight IA/NV robot variants, the original oversized-proxy robot, and `Concrete_fence_v1_wall_set_v2`; the fence measured `haloToMesh=0.00000` versus `haloToPhysics=0.20009`. Final C# build passed with `0` errors; `git diff --check` passed.
- Detail: archived in `task-handoff.md` under `Active task`.

### 2026-10-21 — Hangar impact surface alignment

- Status: `COMPLETED`
- Scope: fix the Client projectile and impact FX position on `Hangar_v1_full (2)` by replacing its oversized convex hull with a non-convex static MeshCollider that follows the visible mesh.
- Verification: Client Play Mode reproduced the requested `target1` shot with `IA Skirmisher`; the persistent decal and temporary red glow landed on the Hangar front door mesh, the projectile/FX hit-point path remained unchanged, Inspector showed `Convex` disabled, and Unity Console ended with `0` errors.
- Detail: `task-handoff.md`, active task.
### 2026-10-22 - AI movement settings runtime source and fivefold inertia
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make the local editor vehicle test host use the assigned `ServerSettings` prefab and configure IA with five times more movement inertia than NV.
- Root cause: `Client.unity` started a local host without a `ServerSettings` instance, so `ServerSettings.GetRobotMovement()` returned code defaults and ignored Inspector values.
- Implementation: `ClientVehicleTestMode` now instantiates the assigned settings prefab; `ServerSettings.prefab` uses NV stopping `1.0` and IA stopping `0.2`. Shared values remain standard acceleration `1`, standard braking `3`, legged acceleration `2.25`, and legged braking `2.75`.
- Verification: serialized assertions, production/editor builds, and `git diff --check` passed with zero build errors. Unity Play Mode reproduction is still pending.
- Detail: active task record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Pre-global aiming rollback verification update

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Verification: restored legacy reticle/server settings and layered the configurable zoom arrays without physical ring projection; production and test `dotnet build` commands passed with 0 errors and `git diff --check` passed. Unity batch PlayMode did not produce an XML result because the project was already locked by another Unity instance; manual Client Play Mode remains required.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - VehicleTest hit-marker visibility regression

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make the Inspector-enabled hit-marker sphere appear in the current `Client.unity` editor test mode while preserving legacy `VehicleTest` behavior and authoritative hit resolution.
- Root cause: `VehicleTestRuntimeSettings.OnAuthoritativeProjectileHit` accepted only `gameObject.scene.name == "VehicleTest"`, but the current test-mode object is hosted in `Client`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` and `dotnet build Game.PlayMode.Tests.csproj --no-restore --nologo` passed with 0 errors; `git diff --check` passed. Manual Client Test Mode firing remains pending while Unity Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - VehicleTest camera shake override

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add `VehicleTestRuntimeSettings.disableCameraShake` for impact camera-shake suppression during zoom and hit testing.
- Implementation: `ClientGameplaySettings` now supports a non-persistent nullable override; VehicleTest applies it after spawn and clears it when the settings component is disabled or destroyed, leaving `PlayerPrefs` unchanged.
- Verification: source/build checks are recorded in the active handoff; manual Client Test Mode firing and comparison with the saved camera-shake preference remain pending while Unity Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Per-zoom aiming circle diameter

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add a required `aimingCircleDiameter` value to the default, forward, and backward `CameraZoomProfile` elements and apply the active profile value to both local and server dispersion reticles.
- Implementation: profile cloning/validation and Client scene serialization now preserve the value; `GunDispersionGlobalSettings` uses it as the profile base diameter while existing zoom interpolation, bloom expansion, and min/max clamps remain active.
- Verification: production and PlayMode test assemblies built with 0 errors; `git diff --check` passed; the Client scene contains 7 serialized profile fields. Manual Unity Play Mode zoom comparison remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Single indexed ClientSettings zoom array
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: replace separate default/forward/backward camera zoom settings with one `GameplayRuntimeSettings.cameraZoomProfiles` array keyed by explicit signed `zoomIndex` values.
- Implementation: `CameraController` now resolves the active profile, wheel bounds, sniper step, normalized zoom endpoint, camera distance/FOV, and HUD step from the matching index; the existing per-profile aiming-circle diameter remains available for local/server reticles. Client scene serialization now contains one array with 7 indexed profiles.
- Verification: production and PlayMode test assemblies built with 0 errors; `git diff --check` passed; static audit found no obsolete scene fields and confirmed indices `0,1,2,3,4,-1,-2`. Manual Unity Play Mode wheel/reticle verification remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Fractional per-zoom aiming circle diameter
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: allow `ClientSettings` zoom profiles to use aiming-circle base diameters below `1` and make those values effective below the legacy global UI minimum.
- Implementation: `aimingCircleDiameter` now validates from `0`; explicit finite profile overrides bypass `uiMinDiameter` but remain capped by `uiMaxDiameter`, while the legacy overload keeps the global minimum fallback. Added focused coverage for a `0.5` profile diameter.
- Verification: production and PlayMode assemblies built with 0 errors; `git diff --check` passed; focused test source covers the new lower-than-global-minimum behavior. Manual Unity Play Mode verification remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Zero aiming circle HUD clamp
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: preserve a zero per-zoom aiming-circle diameter through `GunCrosshair` so the configured dispersion sprite can actually disappear.
- Root cause: `GunCrosshair.ApplyAimingDiameter` applied `Mathf.Max(1f, diameter)` after the profile/global calculations, restoring a visible sprite even when the profile value was zero.
- Implementation: zero now immediately clears the smoothed diameter and writes zero to the active reticle size/scale; focused PlayMode source coverage verifies the `RectTransform` size becomes `Vector2.zero`.
- Verification: production and PlayMode assemblies built with 0 errors; `git diff --check` passed; focused PlayMode source coverage checks the zero-size `RectTransform` path. Manual Unity Play Mode confirmation remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Zero aiming circle one-pixel minimum
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make a zero `ClientSettings` zoom-profile diameter produce a `1x1` dispersion sprite instead of the `64.38217`-sized result caused by adding minimum weapon dispersion.
- Root cause: the profile value was treated as a base diameter, so `0` still received aimed-dispersion pixels and bloom pixels before reaching `GunCrosshair`.
- Implementation: `GunDispersionGlobalSettings.GetUiDiameter` now returns `1` for an explicit zero/negative profile value; `GunCrosshair` retains its `1f` lower bound. Positive fractional values and the legacy fallback path are unchanged.
- Verification: focused source coverage checks both the zero-profile final diameter and the HUD `1x1` `RectTransform` result; production and PlayMode assemblies built with 0 errors and `git diff --check` passed. Manual Unity Play Mode remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-24 - Per-zoom base diameter bloom regression
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: restore movement/turret/shot dispersion growth above the configured fully-aimed `aimingCircleDiameter` without undoing fractional sizing, values above `uiMaxDiameter`, or runtime zoom override behavior.
- Root cause: the finite profile branch in `GunDispersionGlobalSettings.GetUiDiameter` returned before adding the existing bloom contribution.
- Acceptance: explicit values are exact at full aim, current dispersion above fully aimed adds bloom, zero/tiny bases can grow with bloom, legacy no-profile behavior is unchanged, and focused/build/static checks pass.
- Implementation: finite profile values now replace only the fully aimed base contribution; movement/turret/shot bloom is added using the existing zoom-scaled coefficients. The legacy no-profile branch remains unchanged, and runtime zoom override fields/flow are preserved.
- Verification: production and PlayMode assemblies built with 0 errors; the temporary compiled harness returned `20`, `35`, `400`, `415`, and `15` for the fully aimed, bloomed, oversized, and zero-base cases; `git diff --check` passed. Unity PlayMode/Inspector confirmation remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Tiny aiming circle regression
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: reproduce the exact `ClientSettings` value `0.000001` from the user's screenshot and make the final `GunCrosshair` Image size `1x1` instead of `64.38217x64.38217`.
- Root cause under investigation: only exact zero bypassed aimed/bloom additions; a positive near-zero profile value still received the minimum weapon-dispersion contribution.
- Acceptance: add an end-to-end focused test for the tiny profile value, preserve the one-pixel minimum for exact zero, keep values above `1` unchanged, and record build/runtime verification.
- Implementation: profile values at or below `1` now bypass aimed/bloom additions; the compiled production method returned `0.000001` for the exact screenshot value, and `GunCrosshair` clamps that result to `1x1`.
- Verification: production and PlayMode assemblies built with 0 errors; `dotnet test` did not run Unity tests because no .NET adapter is configured; a temporary .NET 8 harness executed the compiled `Assembly-CSharp.dll` calculation successfully; `git diff --check` passed. Unity PlayMode/Inspector confirmation remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-23 - Absolute per-zoom aiming circle diameter
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: make the serialized zoom-profile diameter an exact final `GunCrosshair` Image size, so values `1`, `1.01`, and `20` do not receive hidden dispersion additions.
- Root cause: `GunDispersionGlobalSettings.GetUiDiameter` treated explicit profile values above `1` as a base diameter and added aimed/bloom pixels, producing the observed `1.01 -> 43.50203` jump.
- Acceptance: preserve explicit values through the local/server reticle path, keep the legacy no-profile formula unchanged, add exact-value regression tests, and record compiled/runtime verification.
- Implementation: finite zoom-profile overrides now return as absolute diameters; `GunCrosshair` keeps the one-pixel lower bound for zero/tiny values, while the legacy `NaN` path retains dispersion and zoom calculations.
- Verification: production and PlayMode assemblies built with 0 errors; compiled `Assembly-CSharp.dll` returned `1`, `1.01`, and `20` unchanged for the exact profile inputs; `git diff --check` passed. Unity PlayMode/Inspector confirmation remains pending while the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-24 - Explicit zoom diameter upper clamp

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: allow an explicit `ClientSettings` zoom-profile diameter of `400` to enlarge the dispersion sprite beyond the legacy `uiMaxDiameter` value of `340`.
- Root cause: the absolute explicit-profile branch still passed the configured value through `Mathf.Clamp(..., uiMaxDiameter)`, so every value above the global legacy maximum rendered as `340`.
- Implementation: the explicit-profile branch now returns the nonnegative configured diameter without the legacy maximum; regression coverage asserts `340` and `400` in both the calculation and `GunCrosshair` `RectTransform`, while the legacy no-profile path remains capped.
- Verification: production and PlayMode assemblies built with 0 errors; the temporary compiled harness returned `explicit 340 -> 340`, `explicit 400 -> 400`, and `legacy -> 340`; `git diff --check` passed. Unity PlayMode/Inspector confirmation remains pending because the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.

### 2026-10-24 - Runtime ClientSettings zoom override

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Scope: add an Inspector checkbox and signed `runtimeZoomIndex` next to the ClientSettings zoom-profile array so developers can change the active local camera zoom during runtime.
- Implementation: `CameraController` applies valid runtime index changes during normal updates and initialization through `ApplyZoomStep`; wheel zoom is suppressed while the override is valid and active, and resumes when disabled or when the index is missing.
- Verification: production and PlayMode assemblies built with 0 errors; focused PlayMode source coverage compiled for enabled `0 -> 1`, invalid-index ignore, disabled override, and validation/copy behavior; `git diff --check` passed. Unity PlayMode/Inspector confirmation remains pending because the Editor is open.
- Detail: active record is in `docs/codex-game/task-handoff.md`.
