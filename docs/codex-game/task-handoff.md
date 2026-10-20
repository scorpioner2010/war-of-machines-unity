# Codex Task Handoff

## Active task
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "я під час тестування помітив баг поки по роботу я не вистрілю роботи всі невидимі"
- Objective: Diagnose and fix the regression where robots remain invisible until the first projectile is fired at a robot.
- Acceptance criteria:
  - Identify the code path that hides robot renderers before the first shot.
  - Restore initial robot visibility without breaking intentional map visibility, server stripping, death visibility, or projectile impact rendering layers.
  - Add deterministic regression coverage for initial robot visibility when practical.
  - Update the relevant mechanic documentation and verify the fix with build/tests or a reproducible editor check.
- Assumptions: The symptom affects robot presentation/visibility initialization rather than projectile hit detection; existing visibility rules for hidden enemies must remain unchanged.
- Current work: Corrected the rendering-layer regression in `VehicleColliderRegistry`: world impact FX use bit `1`, while robot renderers retain the default lighting bit `0`. The visibility path remains unchanged, so intentional team visibility, server stripping, and death visibility rules are preserved.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Unity PlayMode regression suite passed `4/4` in `C:\Users\TorasPC\AppData\LocalLow\Toras\war-of-machines\TestResults.xml`; the tests covered robot and world impact rendering-layer isolation and visual-surface placement. `dotnet build Assembly-CSharp.csproj` and `dotnet build Game.PlayMode.Tests.csproj` passed with `0` errors. `git diff --check` passed. The test run emitted the existing convex-mesh polygon-limit warning for `tripo_node_b7d6146a`, but all tests passed.

## Archived task (separate robot and non-robot impact FX profiles)
 - Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "треба зробити 2 варіанти, 1 для попаданю по роботах інший по попаданю по любих інших обьектах, зараз 1 варіант а треба 2 і щоб вони повністю налаштовувалися в одному місці [@PooledImpactFx.cs](file:///D:/Programing/UnityProjects/war-of-machines-unity/Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs)PooledImpactFx.cs"
- Objective: Add two fully independent, inspector-configurable impact FX profiles to `PooledImpactFx`: one selected for robot hits and one selected for all non-robot impacts, while preserving pooling, rendering-layer isolation, and existing generic visual FX playback.
- Acceptance criteria:
  - Robot hits use a dedicated profile selected from the registered `VehicleRoot` target.
  - World and other non-robot hits use a separate profile selected when no `VehicleRoot` is supplied.
  - Both profiles expose all impact particles, camera-shake effects, projectors, halo, glow, decal, offset, size, HDR, and fade settings in the same `PooledImpactFx` Inspector component.
  - Pool reuse resets every profile-owned renderer, particle, material, and target rendering-layer state without leaking the previous profile.
  - The active impact prefab and other serialized `PooledImpactFx` prefabs remain wired after the serialization change.
  - Mechanic documentation, handoff/history, build, and static validation are updated.
- Assumptions: `impactTarget != null` is the authoritative runtime distinction for a robot hit; `null` means a world or other non-robot object. The two profiles may intentionally reference the same child components when the visual asset is shared.
- Current work: `PooledImpactFx` now owns two complete impact profiles and selects them from the optional `VehicleRoot` target; all five serialized prefab instances were migrated to the nested profile fields.
- Work remaining: Optional Unity Play Mode shot verification of both a robot and a non-robot object with intentionally different profile values.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Content/Combat/Projectiles/CFXR Explosion.prefab`, `Assets/Game/Content/Combat/Projectiles/CFX3_Fire_Shoot.prefab`, `Assets/Game/Content/Unused/Prefabs/FX/CFX3_Fire_Explosion.prefab`, `Assets/Game/Content/Unused/Prefabs/FX/CFXR3 Fire Explosion B.prefab`, `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion Smoke 2 Solo (HDR).prefab`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildImpactProfiles\\` passed with `0` errors and only existing warnings. Static assertions confirmed every serialized `PooledImpactFx` prefab has both profiles and the active prefab has the complete nested settings; targeted `git diff --check` passed. Unity Editor remained open on `Client` after script import; a two-target Play Mode comparison remains pending.

## Archived task (deterministic robot and world impact surface)
 - Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "я бачу ти тупо стреляєш в небо, тому напиши детермінований тестовий код, який стріляє збоку і попадає в заспавненого робота, щоб побачити реальний результат попадання"
- Objective: Replace unreliable manual mouse aiming with an automated PlayMode reproduction that instantiates the real robot prefab, fires a physics ray from the side into a deliberately oversized proxy collider, and verifies that the robot Impact Glow Halo is placed on the registered visual mesh surface.
- Acceptance criteria:
  - A Unity PlayMode test spawns the real `IA_L1_Starter` robot prefab and fires a deterministic side shot through Unity physics.
  - The shot resolves a registered robot target and invokes the real `PooledImpactFx.PlayImpact` path.
  - The test proves the halo transform matches the visual mesh surface, not the outer proxy collider, and logs both positions for inspection.
  - The fix works when `DeathLogic` is not listed in `VehicleRoot.rootAwareBehaviours`, and does not break pooling or renderer-layer cleanup.
  - The test, C# build, static checks, mechanic documentation, and handoff/history are updated.
- Assumptions: The real robot prefab and its serialized `DeathLogic.detachableVisuals` are the source of visual mesh colliders; the test may add only a temporary oversized proxy collider to reproduce the reported geometry mismatch.
 - Current work: Added deterministic PlayMode coverage using the real `IA_L1_Starter` prefab and an oversized side-hit proxy collider. `DeathLogic` now registers serialized visual renderers/mesh colliders during its own lifecycle, `VehicleColliderRegistry` resolves robot impacts with a normal-first visual-mesh raycast, and `PooledImpactFx` consumes the corrected point before configuring all impact visuals. Pool reset also restores the halo renderer's world rendering layer.
 - Work remaining: None.
 - Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Scripts/Gameplay/Robots/DeathLogic.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, `Assets/Game/Tests/PlayMode/Game.PlayMode.Tests.asmdef`, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, `docs/codex-game/task-history.md`.
 - Verification: Selected Unity PlayMode test passed against the real prefab. Measured `proxy=(-1.05, 0.07, -0.58)`, `mesh=(-0.52, 0.07, -0.58)`, `resolved=(-0.52, 0.07, -0.58)`, `halo=(-0.52, 0.07, -0.58)`, `haloToMesh=0.00000`, and `haloToProxy=0.52875`; target-only rendering layer was applied and pool release restored masks and disabled the halo. `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildDeterministicImpactFinal2\\` passed with 0 errors. The PlayMode run emitted the existing convex-mesh polygon-limit warning for `tripo_node_b7d6146a`, but the test passed.

## Archived task (ground impact projection regression)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "я зробив постріл по землі, потім роботом наступив на землю та побачив на нозі проджектайл, а раніше я вже тобі давав такс виправити це і ти виправив цйей баг, чому він зараз знову є?"
- Objective: Diagnose and fix the regression where a persistent ground impact projector becomes visible on a robot's foot after the robot walks over the impact location.
- Acceptance criteria:
  - Identify the actual ownership/rendering path that lets a world impact project onto a moving robot.
  - Ground and other non-robot impact FX remain world-static and do not appear on robot renderers later entering the projector volume.
  - Robot impact FX keeps its target-only rendering-layer isolation and existing robot Halo toggle.
  - Add deterministic PlayMode regression coverage for a ground impact followed by a robot entering its volume, update mechanic documentation, and run build/static validation.
- Assumptions: The reported “projectile on the foot” is the persistent regular impact decal/FX, not a newly spawned damage projectile; the visual regression must be prevented without changing authoritative hit or damage logic.
- Current work: Confirmed the regression was caused by world projectors using rendering layer bit `0` while registered robot renderers still retained bit `0`; the robot-only target layer was added only for robot hits. The follow-up visibility regression showed that removing bit `0` also removed the default lighting layer from robots, so the corrected implementation uses world impact bit `1`, removes only bit `1` from robot renderers, and keeps `Projectile` world impact FX unparented while retaining collider attachment for robot hits.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Previous fresh targeted PlayMode test passed before this visibility follow-up. New bit-selection assertions and build/test verification are pending.

## Archived task (robot Impact Glow Halo toggle)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-21
- User request (verbatim): "на роботах все ще працює погано але на стінах вже краще, я прийняв рішення не вмикати Impact Glow Halo саме по попаданю по роботах, додай таке налаштування і вимкни для попадання по роботах щоб Impact Glow Halo не зявлявся при попадані по роботах"
- Objective: Add a serialized per-profile setting that controls whether `Impact Glow Halo` is enabled for robot hits, disable it in the active impact prefab's robot profile, preserve the Halo for non-robot/world hits, and verify the branch through PlayMode tests.
- Acceptance criteria:
  - `PooledImpactFx` exposes a clear robot-profile Halo toggle in the existing Inspector configuration.
  - Robot impacts do not enable `Impact Glow Halo` when the toggle is disabled, while regular/glow decals and robot layer cleanup remain intact.
  - Non-robot impacts continue to enable their configured Halo.
  - Fresh PlayMode tests assert both robot-disabled and non-robot-enabled behavior; the C# build, static checks, documentation, and handoff/history pass.
- Assumptions: The setting is profile-specific, so only `robotImpactProfile` is disabled; `otherObjectImpactProfile` remains enabled for walls and other world objects.
- Current work: The serialized toggle, active prefab value, and regression assertions were implemented. Fresh PlayMode execution was not completed after the Unity test assembly recompile.
- Work remaining: Run a fresh Unity PlayMode test pass and record its result.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Content/Combat/Projectiles/CFXR Explosion.prefab`, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, and `docs/codex-game/task-handoff.md`.
- Verification: `dotnet build` passed before the final test-source adjustment; Unity recompiled `Game.PlayMode.Tests.dll` successfully. A fresh PlayMode result remains pending.

## Archived task (visual-mesh Impact Glow Halo alignment)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "я тільки що тестував попадання по роботах , на деяких роботах Impact Glow Halo взагалі не показується, потім я тестував на різних обьектах, і на деякихз обьектах наприклад на Concrete_fence_v1_wall_set_v2 (12) Impact Glow Halo лежить не на мешові а на колайдеру, або десь між ними, якщо збоку подивитися то чітко відно як Impact Glow Halo в воздусі вимить перед проджектайлом, до проджектайлів питань нема все чітко а от до Impact Glow Halo є, він лягає не на маш а на колайдер і це створює баги, ось це головне завдання яке ти маєш пофіксити використавши свої PlayMode-тести, якщо твоїх тестів для виправлення цього таска не достатньо створи додаткові тестування і так поки не закриєш цей таск повністю"
- Objective: Make `Impact Glow Halo` use the same correct visual surface as the projectile/projector for both registered robots and non-robot world objects, including thin or proxy-collider geometry such as `Concrete_fence_v1_wall_set_v2 (12)`, and prove the fix with deterministic PlayMode tests instead of manual sky shots.
- Acceptance criteria:
  - Robot and non-robot PlayMode tests invoke the real `PooledImpactFx.PlayImpact` path with deterministic side hits.
  - Every tested hit places the Halo on the visible mesh surface, not on an outer collider or in front of the projectile; tests record mesh, physics, resolved, and Halo positions.
  - Coverage includes at least one robot with registered visual mesh colliders and the reported thin world-object geometry, plus a case where the visual mesh is inactive or not directly raycastable.
  - Missing Halo on some robots is diagnosed and fixed without breaking robot target rendering-layer isolation or pool reset.
  - C# build, PlayMode tests, static checks, mechanic documentation, and handoff/history are updated only after fresh results pass.
- Assumptions: The projectile physics hit remains authoritative for damage and projectile placement; only cosmetic Halo/decal surface resolution may be corrected. Real prefab/scene visual meshes and their serialized colliders are preferred over scene-wide runtime searches.
 - Current work: Added a serialized `ImpactVisualSurface` mapping for non-robot proxy colliders, passed the actual local `RaycastHit.collider` through projectile impact FX, extended the registry resolver to robots and mapped world prefabs, and kept Halo setup active when a robot-specific rendering layer cannot be acquired.
 - Work remaining: None.
 - Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/ImpactVisualSurface.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, `Assets/Game/Scripts/Gameplay/Robots/DeathLogic.cs`, `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`, `Assets/Game/Scripts/Gameplay/Projectiles/ProjectileRuntimePool.cs`, `Assets/Game/Scripts/Gameplay/Robots/ProjectileVisualSpawner.cs`, the five `Concrete_fence_v1` prefabs, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
 - Verification: Fresh Unity PlayMode run passed `3/3` tests. The fence regression measured `physics=(-0.36, -1.55, -8.79)`, `mesh=(-0.16, -1.55, -8.79)`, `resolved=(-0.16, -1.55, -8.79)`, `halo=(-0.16, -1.55, -8.79)`, `haloToMesh=0.00000`, and `haloToPhysics=0.20009`. All eight IA/NV robot variants passed Halo placement and the existing robot test passed target rendering-layer cleanup/pool reset. `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildImpactSurfaceFinal\\` passed with `0` errors and only existing `System.Net.Http` warnings; `git diff --check` passed.

## Archived task (manual robot impact reproduction attempt)
- Status: `IN_PROGRESS`
- Started: 2026-10-21
- User request (verbatim): "обьект Impact Glow Halo спавниться чомусь на колайдері а не перед мешом, можеш виправти це? для запускаєш сцену клієнт і спавниш 1 бьота потім стреляєш по ньому і ти все побачиш, виправ і протестуй нормально. заверши таск тільки тоді коли впевнений будеш що все виправлено"
- Objective: Reproduce a Client shot against one spawned bot, determine why the `Impact Glow Halo` is positioned on the robot collider instead of the visible mesh, implement a geometry-correct alignment fix, and verify it in repeated Unity Play Mode shots.
- Acceptance criteria:
  - Client Play Mode is entered, one bot is spawned, and a shot is made against the bot while inspecting the Halo versus the visible mesh and collider.
  - Robot `Impact Glow Halo` is placed on the visible hit mesh rather than floating at an outer proxy collider surface.
  - Regular decal, glow decal, particles, robot rendering-layer isolation, pooling, and non-robot impacts remain intact.
  - The same scenario is repeated after the fix, Unity Console has no new errors, and the build/static checks pass.
  - `weapons-damage.md`, handoff, and history record the cause and verification.
- Assumptions: The registered `VehicleRoot` renderers are the authoritative visual surface set for robot impacts; static world impacts keep their existing physics hit-point behavior. The fix must avoid per-frame scene-wide lookup and must use serialized/registry-provided runtime dependencies.
- Current work: New task started; the previous profile split is archived above. Unity Client is open on the Client scene, and the robot impact path currently places the Halo at `impactPoint + impactNormal * impactDecalSurfaceOffset`.
- Work remaining: Reproduce the robot shot, identify the visual-surface gap, implement the targeted alignment, repeat the shot, and update verification records.
- Edited files: `docs/codex-game/task-handoff.md`.
- Verification: Pending Play Mode reproduction and implementation.

## Archived task (Hangar mesh collider impact alignment)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "ні в тебе не вийшло, запусти сцену клієнт, зайди в гру вибери любого робота і наведися на target1, тоді ти вистрелиш по обьекту Hangar_v1_full (2), і в нього якраз колайдер трохи більший чим меш, ти помітиш що світло спавниться не на мешу, і скоріше за все проджектайл теж не там де треба. [@PooledImpactFx.cs](file:///D:/Programing/UnityProjects/war-of-machines-unity/Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs)PooledImpactFx.cs"
- Objective: Reproduce the exact Client gameplay shot against `Target1` and `Hangar_v1_full (2)`, inspect the collider-versus-mesh impact position, then implement and verify a root-cause fix rather than relying only on a guessed offset.
- Acceptance criteria:
  - Client scene is entered, a robot is selected, `Target1` is used, and `Hangar_v1_full (2)` is hit in the requested scenario.
  - The observed impact point, collider, mesh, and projectile/impact FX placement are compared in Unity.
  - The impact visual is placed on the visible mesh without breaking the persistent decal, glow fade, pooling, or network hit path.
  - The fix is verified by repeating the same shot in Unity and checking the Console for new errors.
  - The relevant mechanic documentation, handoff, and history are updated.
- Assumptions: The user has authorized opening/closing Unity and changing project files. Existing Client Vehicle Test Mode and Target1 setup should be reused. The earlier signed offset may be insufficient and must be validated against the actual scene geometry.
- Current work: Reproduced the Client shot and traced the wrong impact position to the convex hull generated by `Hangar_v1_full (2)`'s MeshCollider. The canonical static Hangar prefab now uses the render mesh as a non-convex MeshCollider.
- Work remaining: None.
- Edited files: `Assets/Game/Content/Map_v100_v200/Environment/Buildings/Industrial/Hangars/Hangar_v1/Hangar_v1_full.prefab`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Unity Inspector confirmed `Hangar_v1_full (2)` has `Convex` disabled. In a clean Client Play Mode run, `IA Skirmisher` was spawned, aimed at `target1`, and fired at `Hangar_v1_full (2)`; the final capture showed the persistent impact and red glow on the visible front door mesh. `Projectile` and `PooledImpactFx` both consume the corrected `RaycastHit.point`. After stopping Play Mode, Unity Console showed `0` errors and no new errors. `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildHangarCollider\\` passed with `0` errors and only the existing `System.Net.Http` warnings. Targeted `git diff --check` passed; the full working-tree check still reports trailing whitespace in unrelated Unity-generated changes under `Assets/Game/Scenes/Map.unity`.

## Archived task (impact surface offset)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "майже все ідеально але є баг, при попадані прожектайл спавниться там де колайдер а не там де меш, інколи меш знаходиться глубше, можна буде зробити так щоб спавн був саме там де меш? [@PooledImpactFx.cs](file:///D:/Programing/UnityProjects/war-of-machines-unity/Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs)PooledImpactFx.cs"
- Objective: Correct the pooled projectile impact visual position when a physics proxy collider sits outside the visible mesh, while preserving the existing hit normal, decal projection, pooling, and network hit flow.
- Acceptance criteria:
  - `PooledImpactFx` exposes a clear signed surface-position setting that can move the impact from the proxy collider toward a deeper visual mesh.
  - The setting affects the regular decal, glow decal, halo, and impact FX placement consistently.
  - Existing prefab behavior remains unchanged unless the new offset is intentionally adjusted.
  - The relevant projectile mechanic documentation is updated with the cause and tuning direction.
  - The C# project build and whitespace validation pass.
- Assumptions: The server and client only transmit the physics hit point and normal, so a universal exact visual-mesh intersection is not available for arbitrary proxy colliders without adding per-asset visual surface colliders. A signed inspector offset is the safe shared correction and can be tuned per impact prefab.
- Current work: `PooledImpactFx` now treats `impactDecalSurfaceOffset` as a finite signed world-space correction and applies it to the decal, glow decal, halo, and camera-facing impact FX root. Negative values move the effect inward toward a deeper visual mesh.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildImpactSurfaceOffset\\` passed with 0 errors and only existing `System.Net.Http`/FishNet warnings; static assertions passed for the signed offset, unchanged prefab default `0.003`, and documentation; `git diff --check` passed. An automatic arbitrary-mesh intersection was not added because the current network hit flow transmits only the physics point and normal, not a visual surface collider or mesh intersection.

## Archived task (centralize projectile glow settings)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "додай всі налаштування в цей скрипт `PooledImpactFx` і потім напиши мені що за що відповідає [@PooledImpactFx.cs](file:///D:/Programing/UnityProjects/war-of-machines-unity/Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs)PooledImpactFx.cs"
- Objective: Centralize the projectile impact glow tuning that currently lives in `PooledImpactFx`, `ImpactGlowDecal.mat`, and `ImpactGlowHalo.mat` into clear inspector fields on `PooledImpactFx`, without changing the persistent regular impact or fade behavior.
- Acceptance criteria:
  - `PooledImpactFx` exposes independent overall glow, crack emission, halo emission, HDR halo color, halo base color, halo size, and fade settings.
  - Runtime applies the serialized script values to both pooled glow materials before enabling the impact visuals.
  - The existing prefab keeps the current visual defaults and remains wired correctly.
  - Global Bloom remains a camera/volume setting and is not incorrectly made per-impact.
  - The relevant mechanic documentation, handoff, and task history are updated.
  - The C# project build and whitespace validation pass.
- Assumptions: The user wants projectile-local controls in the `PooledImpactFx` Inspector; global Bloom should continue to be configured in the URP volume because changing it per projectile would affect the whole camera view.
- Current work: `PooledImpactFx` now owns projectile-local glow multipliers, HDR halo color/tint, halo size, crack emission, and fade settings; the runtime applies them to cloned materials before the impact is enabled.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Content/Combat/Projectiles/CFXR Explosion.prefab`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildProjectileGlowSettings\\` passed with 0 errors and only existing `System.Net.Http`/FishNet warnings; serialized field assertions passed for the script, prefab defaults, and documentation; `git diff --check` passed. Global Bloom remains in `Assets/DefaultVolumeProfile.asset` by design.

## Archived task (stronger projectile Bloom diagnosis)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "я дуже підняв параметри але так і не побачив ефека типу сильного блума"
- Objective: Diagnose why very high projectile glow settings still do not produce a visibly strong Bloom halo, fix the actual HDR/Bloom path, and verify the stronger result in Unity Play Mode.
- Acceptance criteria:
  - Reproduce the user's current high-value settings in a real Unity Play Mode shot.
  - Identify whether the visible impact is emissive HDR input and whether the active camera/volume actually applies Bloom.
  - Configure a projectile-only stronger halo without breaking the persistent regular impact or two-second glow fade.
  - Verify the stronger halo visually in a second Play Mode shot and document the exact working settings.
- Assumptions: Unity may be opened or closed as needed; the existing projectile impact path and camera/volume setup should be reused unless testing proves they cannot produce Bloom. Unrelated working-tree changes must be preserved.
- Current work: Replaced the ineffective broad Bloom dependency on the sparse Decal emission with a project-owned additive soft halo on the impact prefab, while preserving the crack Decal and persistent regular impact.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Content/Combat/Projectiles/CFXR Explosion.prefab`, `Assets/Game/Content/Combat/Projectiles/ImpactGlowHalo.mat`, `Assets/DefaultVolumeProfile.asset`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`; unrelated user texture/font/renderer changes were preserved.
- Verification: Reproduced the old high settings (`Emission_Strength=50`, `Impact Glow Strength=5`) and confirmed the old sparse Decal produced no broad halo while a control HDR quad did. The final Unity runtime prefab probe produced a strong circular HDR/Bloom halo, and its same-runtime fade capture removed the halo while leaving the regular impact visible. The active Bloom profile logged `threshold=0.5`, `intensity=2`, `scatter=1`, `highQualityFiltering=true`; the final `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildProjectileHalo\\` passed with 0 errors and only existing `System.Net.Http` warnings.

## Previous active task (Projectile asset organization)
- Status: `COMPLETED`
- Started: 2026-10-20
- User request (verbatim): "файли які відносяться до пострілів такі як CFXR Explosion, матеріали текстури які відносяться до проджектайлів і т.д. треба в одну папку помістити щоб зручно було налаштовувати"
- Objective: Group the active projectile, impact, muzzle, tracer, material, texture, shader, and related shot FX assets into one convenient project-owned folder without breaking serialized Unity references.
- Acceptance criteria:
  - All active shot-related assets have one clear canonical folder under `Assets/Game/Content/Combat/Projectiles`.
  - `CFXR Explosion`, projectile prefabs, projectile/tracer materials, impact materials/shaders, and their project-owned textures are grouped there.
  - Serialized references remain valid after the move; no runtime code or package/vendor layout is changed unnecessarily.
  - Asset-organization and projectile documentation record the final ownership/layout.
- Assumptions: `Assets/Game/Content/Combat/Projectiles` is the canonical folder; third-party Cartoon FX source assets remain in their vendor folder unless a project-owned copy is required.
- Current work: Moved the active project-owned shot FX assets into `Assets/Game/Content/Combat/Projectiles`, removed the now-empty `Effects` folders, and documented the canonical layout. Third-party Cartoon FX source assets remain in `Assets/JMO Assets`.
- Work remaining: none.
- Edited files: `Assets/Game/Content/Combat/Projectiles/CFXR Explosion.prefab`, `CFX3_Fire_Shoot.prefab`, `Effect_Fire_Bulk.png`, `Effect_Smoke_Puff.png`, `Effect_Triangle_Frame.png`, the corresponding old-path deletions, `docs/codex-game/asset-organization.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: all five moved assets retained their original `.meta` GUIDs; active shot dependency closure contains no project-owned `Content` assets outside `Combat/Projectiles`; required scene/prefab GUID references resolve to the new paths; old `Assets/Game/Content/Effects` references are absent; `git diff --check` passed.

## Previous active task (Client scene bloom and VFX glow)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-20
- User request (verbatim): "помітив в тестовому режімі немає блума, не світяться снаряди та CFXR Explosion не працює як треба нема свічіння, виправ це"
- Objective: Restore the Client scene Test Mode rendering path so bloom, projectile/tracer emission, and CFXR Explosion glow are visible without changing normal gameplay rendering when Test Mode is disabled.
- Acceptance criteria:
  - The test camera uses the same HDR, URP post-processing, and volume path required by the existing bloom setup.
  - Projectiles/tracers and CFXR Explosion produce visible bloom/glow in Client Test Mode.
  - Test Mode disabled leaves normal Client startup and build behavior unchanged.
  - No menu or HUD prefab is introduced; scene-owned UI remains in `Client.unity`.
  - Relevant rendering/projectile documentation and verification results are updated.
- Assumptions: The first suspect is the dedicated test camera's missing URP post-processing configuration; existing scene/prefab glow assets should be reused unless inspection proves their serialized setup is broken.
- Current work: Added the missing serialized URP `UniversalAdditionalCameraData` to the dedicated Client Test Mode camera, matching the existing `VehicleTest`, Client, and Map gameplay camera post-processing settings. Updated the VehicleTest and weapons mechanic documentation.
- Work remaining: In Unity Editor, enable `ClientVehicleTestMode/enableVehicleTestMode`, spawn a vehicle, fire a projectile at the world and a robot, and visually confirm projectile/tracer emission, CFXR Explosion crack glow, and Bloom halo. Leave the checkbox disabled before normal builds.
- Edited files: `Assets/Game/Scenes/Client.unity`, `docs/codex-game/vehicle-test-scene.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, `docs/codex-game/task-history.md`.
- Verification: Client scene camera assertions passed for the explicit URP camera component, HDR, post-processing, Default volume layer, and disabled-by-default test mode; CFXR glow reference and active Bloom profile assertions passed; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildClientBloom\\` passed with 0 errors; `git diff --check` passed. Visual Play Mode confirmation remains pending.

## Previous active task (Client scene vehicle test mode)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- User request (verbatim): "реалізуй цей варіант Test Mode у Client.unity — рекомендую, добре все продумай особливо архітектуру, щоб це було оптимізовано та чітко і якщо він вимкнутий то він взагалі не має працювати щоб не підтягувало нічого в білд нормальної гри"
- Objective: Add an explicitly enabled, scene-owned test mode to `Client.unity` that reuses the existing in-scene HUD/menu hierarchy, starts the vehicle test flow only when enabled, and has no test-mode runtime behavior when disabled.
- Acceptance criteria:
  - All Client menus and HUD remain authored in `Client.unity`; no menu/HUD prefab is introduced.
  - Vehicle test mode is controlled by a clear serialized checkbox and does not interfere with normal Client startup when disabled.
  - Test mode uses explicit scene references and avoids runtime object discovery for required dependencies.
  - Vehicle test HUD, timer, camera, spawn, and visibility flow are wired coherently.
  - Relevant mechanic documentation and verification results are updated.
- Assumptions: The existing `VehicleTestSceneController` and normal Client gameplay systems should be reused where possible instead of duplicating vehicle spawning/network logic.
- Current work: Implementation is complete; manual Client Play Mode verification with the checkbox enabled remains.
- Edited files: `Assets/Game/Scenes/Client.unity`, `Assets/Game/Scripts/Testing/ClientVehicleTestMode.cs`, `Assets/Game/Scripts/Testing/ClientVehicleTestMode.cs.meta`, `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`, `Assets/Game/Scripts/UI/Helpers/StartServerButtons.cs`, `Assets/Game/Scripts/API/ServerManagers/RegisterServer.cs`, `Assets/Game/Scripts/Networking/Lobby/LobbyManager.cs`, `Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs`, `Assembly-CSharp.csproj`, `docs/codex-game/ui-hud.md`, `docs/codex-game/vehicle-test-scene.md`, `docs/codex-game/match-lobby-spawn.md`, `docs/codex-game/task-handoff.md`, `docs/codex-game/task-history.md`.
- Work remaining: Open `Client.unity`, enable `ClientVehicleTestMode/Enable Vehicle Test Mode`, enter Play Mode, spawn a vehicle, and verify scene HUD, timer, map visibility, and bots. Disable the checkbox before normal builds.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildClientTestModeFinal\\` passed with 0 errors and only existing warnings; `git diff --check` passed; Client scene reference assertions passed (`7/7`); Unity batch import/script compilation completed with no project compile or scene import errors. The editor-defined dotnet build could not be used because pre-existing FishNet/Edgegap editor-project errors stop that generated multi-project build before the game assembly.

## Previous completed task (bot AI improvements)
- Status: COMPLETED
- Started: 2026-10-20
- Last updated: 2026-10-20
- User request (verbatim): "тобі треба покращити ботів, покращити логіку рух та командну роботу бо вони виглядають дуже тупими, також тобі треба зробити більш агресивніше прицілювання і атаки бо помітив що якщо я рухаюся постійно бот не може в мене прицілитися а я його розтрілюю потрохи, короче завдання такі 1. агресивніше прицілювання та наведенні, розрахунок руху щоб стреляти наперід. 2. рух більш розумініший, щоб подивився вокруг себе він не баче противників значить він має йди туди де він ще не був і шукати їх. 3. зараз бот не бачить інших роботів біля себе, він віперається в іншого робота і не може йти, треба додати райкасти раз в 0.5 секунди вокруг роботі по контуру, спереді, заді, зліва справа, і ще 4 по кутам 45 градусів, треба так щоб робот міг відчувати інших роботів вокруг себе та обходити їх при потребі. ці райкасти мають бути розположені на такій висоті щоб всі роботи чіпляти могли всі роботи, щоб не було такого що якийсь бот не може помітити райкастами іншого робота який в притул стоїть біля ньго."
- Objective: improve bot aim/lead, exploration movement, local robot avoidance, and team-aware combat behavior.
- Acceptance criteria: moving targets are led and engaged more reliably; no-target bots choose unexplored graph destinations; eight directional proximity raycasts run every 0.5 seconds and steer around nearby robots; allied robots avoid blocking each other; VehicleTest/static verification passes.
- Assumptions: server-authoritative input remains the movement and firing path; existing inspector-wired components and visibility rules remain in use; no runtime component discovery or AddComponent is introduced.
- Status details: implementation and verification are complete; proximity avoidance weights are independent and proximity sensing now remains active during combat hold-position.
- Work recorded: improved iterative moving-target lead and velocity smoothing; made fire dispersion gating real and reduced bot reaction windows; added per-bot unvisited waypoint exploration; added eight-direction proximity raycasts at three vertical offsets every 0.5 seconds with tangent/escape steering; exposed current bot targets and distributed allied target selection; updated server settings, prefab serialization, editor help, `ai-bots.md`, and `server-settings.md`.
- Edited files: `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, `Assets/Editor/ServerSettingsEditor.cs`, `Assets/Game/Scripts/AI/WaypointGraph/BotFireDecision.cs`, `BotNavigator.cs`, `BotTargetCandidate.cs`, `BotTargetMotionTracker.cs`, `BotTargetScanner.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleBotBrain.cs`, `Assets/Game/Scripts/Server/ServerSettings.cs`, `docs/codex-game/ai-bots.md`, `docs/codex-game/server-settings.md`.
- Work remaining: none.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuildBotsFinal5\\` passed with 0 errors and only pre-existing `System.Net.Http` reference warnings; custom static acceptance checks and `git diff --check` passed. The isolated smoke run spawned 4 bots and observed target acquisition, moving-target lead velocity, robot movement, player movement, unexplored-node exploration, authoritative bot firing (ammo decreased from 90 to 87), and proximity avoidance. Additional VehicleTest batchmode attempts reached the scene and player spawn but could not invoke its private UI bot-button flow; they introduced no gameplay changes and do not invalidate the successful isolated runtime coverage.

## Previous completed task (task history repair)
- Status: COMPLETED
- Started: 2026-10-20
- Last updated: 2026-10-20
- User request (verbatim): "розберися в чому проблема і виправ історію завдань кодекса це дуже важливо"
- Objective: diagnose why Codex task history appears to disappear and make the repository handoff history explicit, durable, and unambiguous without losing unfinished-task state.
- Work completed: added `docs/codex-game/task-history.md` as the canonical append-only status index, preserved detailed handoff records as non-canonical archive, and documented the active/archive/index workflow in `AGENTS.md` and `docs/codex-game/README.md`.
- Verification: UTF-8 reads, active/bot status preservation, canonical index links, and `git diff --check` passed; no gameplay files were changed by the history repair.
- Canonical history index: `docs/codex-game/task-history.md`.

## Previous completed task (impact decal isolation)
- Status: COMPLETED
- Started: 2026-10-20
- Last updated: 2026-10-20
- Follow-up request: "ти не протекстив саме головне, проджектайл не працює після твоїх правок"
- Follow-up objective: Reproduce the broken projectile path in VehicleTest, repair the regression, and verify an actual projectile hit/impact in Play Mode.
- Follow-up acceptance criteria: a local shot creates the projectile/tracer and authoritative hit path; a world hit produces the configured impact projector; robot-hit isolation remains intact; VehicleTest has no new runtime errors.
- Follow-up work remaining: none.
- User request (current task): "все супер єдине що коли я стреляю в землю а потім наїзджаю роботом то проджект тайл впливає на робота, треба зробити так щоб він не впливав на робота, і так само якщо постріл був по роботу щоб це не впливало на інших роботів і на стіни землю і т.д."
- Objective: Isolate impact decals by hit category so Map/world impacts never render on robot renderers, while robot impacts render only on the robot that was hit.
- Acceptance criteria: world decals do not appear on robots after they drive over the impact; robot decals do not appear on other robots or Map/world surfaces; pooled target-layer state still resets correctly; the repository remains buildable and the behavior is documented and tested.
- Assumptions: the existing inspector-wired renderer list is the authoritative visual set for each robot, URP Decal Layers remain enabled, and the existing target-specific rendering-layer allocation is retained.
- Work completed: moved world impact decals to dedicated rendering-layer bit `1`, updated all 135 renderer entries in the shared Map `Environment` and `Props` prefab sources to mask `3`, preserved default light bit `0`, and kept per-target robot impact allocation on bits `2-7`. Robot prefabs retain no world-decal bit, so world decals cannot project onto vehicles and target decals cannot match Map or other robots.
- Work remaining: none.
- Verification: clean `VehicleTest` Play Mode reproduced a wall impact crack and a separate robot impact without the temporary hit-marker; no new Console errors appeared. Focused assertions report world prefab masks `mask1=0`, `mask3=135`; `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal -p:BaseOutputPath=Temp\\CodexBuild\\` passed with 0 errors; `git diff --check` passed.

## Archived handoff detail (non-canonical; see `docs/codex-game/task-history.md`)
- Previous completed task (impact decal isolation rollback): restored the requested pre-diagnostic Bloom/decal checkpoint, kept the tested HDR/post-processing and URP bindings, and verified the repository with `git diff --check`, a successful C# build, and checkpoint assertions.
- Previous completed task (impact glow configuration): added texture-masked red crack emission, configurable material/prefab strength, and a URP Bloom profile, but the follow-up verification identified that gameplay cameras had HDR and post-processing disabled.
- Previous completed task (impact glow shader repair): corrected the `SurfaceDescription.Emission` block's slot from the invalid `BaseColor` serialization to the URP Decal `ColorRGB` emission format, preserved `Expl2_EmissionColor.png`, and verified the real impact path in `VehicleTest` with no shader/runtime errors.
- Previous glow repair (COMPLETED): replaced the incompatible glow graph, restored transparent dark decal texture, fixed prefab material GUIDs, and verified import plus `VehicleTest` Play Mode activation.
- Previous impact-projector task: restored native URP DecalProjector targeting with per-vehicle rendering-layer filtering, moving-robot attachment, Map support, pooling cleanup, and updated weapon documentation. Build passed; Unity import and Play Mode verification remained pending.

## Previous active task
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording (current request): "стало гырше бо тепер це виглядаэ як просто спрайт прилип до 3д моделі, прожектор не працює тепер треба щоб прожектор працював як і раніше але проектувався тільки на обьект по якому було попадання"
- Objective: Restore the native URP DecalProjector for robot impacts and isolate each robot impact to the hit target object with rendering-layer filtering, while preserving robot-following and static Map behavior.
- Acceptance criteria: robot hits use the native projector; nearby ground and unrelated meshes do not receive the robot decal; the mark follows moving robots; Map hits remain projector-based; pooled FX clears target filtering; docs and build verification are updated.
- Assumptions: URP Decal Layers are enabled on every project renderer profile; existing inspector-wired vehicle renderer lists are the explicit target source; no runtime component discovery is added.
- Work completed: restored projector-only impact FX, removed the mesh-sprite fallback, registered inspector-wired robot renderer lists, added per-target reference-counted Decal Layers, enabled `decalLayers: 1` on all URP renderer profiles, and updated `weapons-damage.md`.
- Work remaining: verify in Play Mode that robot projectors do not appear on ground/neighbor meshes, that moving robots keep the decal attached, and that Map decals still render after pool reuse.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors and the existing dependency/API warnings. `git diff --check` passes. Unity import and Play Mode verification remain pending.

## Previous active task carried forward
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording: "помітив прожектор цей проекцирується не тільки на деталь робота а ще й на землю на інші меші, це можна якось виправити щоб проектування було тільки на обьект по якому було попадання?"
- Objective: Restrict each impact decal to the renderer that was hit, preventing the projector from appearing on nearby ground or other meshes while preserving robot-following behavior and static Map impacts.
- Acceptance criteria:
  1. A robot impact decal renders on the hit renderer but not on nearby ground or unrelated meshes.
  2. The decal still follows a moving robot mesh/collider after the previous parent fix.
  3. Static Map impacts retain their current behavior without requiring Map-wide manual setup.
  4. Pool reuse clears any per-hit filtering state and does not leak a previous target renderer.
  5. Authoritative/predicted hit flow stays intact; mechanic docs and build verification are updated.
- Assumptions: URP 17.3 supports DecalProjector rendering-layer filtering in the current project; renderer masks can be assigned per hit without runtime component discovery. If a renderer does not support rendering layers, the code falls back to the existing volume behavior rather than hiding the impact.
- Work completed: Confirmed native URP projector volume behavior and verified URP 17.3 rendering-layer support. Added an inspector-wired disk mesh mark and lightweight transparent shader/material to the pooled impact prefab. Robot hits now use the mesh mark under the moving hit FX root; Map hits continue using the native projector. The mark fades through a cached `MaterialPropertyBlock`, and pool reset disables both visual modes. Updated `weapons-damage.md`.
- Work remaining: Verify in Unity Play Mode that robot hits never appear on ground or neighboring meshes, that hull/turret movement keeps the mesh mark attached, and that static Map projector hits still render correctly after pool reuse.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors and the existing 4 warnings. Static prefab references and call paths were inspected. Unity shader import and Play Mode verification are still pending.

## Previous active task carried forward
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording: "окей реалізуй варіант 1, використай поки якусь рандомну текстуру а я потім налаштую"
- Objective: Додати до нативного URP Decal Projector логіку сліду попадання в стіну з тимчасовою текстурою, pooling, лімітом активних слідів і без обходу авторитетної логіки урону.
- Acceptance criteria:
  1. Потрапляння снаряда у світову поверхню створює видимий decal у `hit.point` з орієнтацією по `hit.normal`.
  2. Decal Projector працює через інспекторні prefab/material references, без runtime `Find*` або обов'язкових `AddComponent`.
  3. Сліди використовують pooling, обмежений lifetime і ліміт активних об'єктів; серверний процес не створює клієнтські FX.
  4. Існуючі передбачені impact FX та authoritative hit/damage flow не ламаються.
  5. `weapons-damage.md` і цей handoff описують нового власника поведінки; статична перевірка/збірка проходять.
- Assumptions: Проєкт використовує Unity 6000.3.13f1 з URP 17.3.0; тимчасовий fallback-material буде створений як ресурс лише якщо в проєкті немає готової decal-конфігурації.
- Work completed: `PooledImpactFx` тепер підтримує inspector-wired URP `DecalProjector` з точним `hit.point`/`hit.normal`, random roll, fade і окремим lifetime. `ProjectileRuntimePool` передає точну точку попадання та повторно використовує найстаріший активний impact FX при досягненні pool cap. `CFXR Explosion.prefab` отримав дочірній `Impact Decal` з тимчасовими `ImpactDecal.mat` та `Effect_Fire_Bulk.png`; `weapons-damage.md` оновлено.
- Work remaining: Перевірити імпорт prefab/material у Unity, компіляцію і, за можливості, Play Mode влучання в стіну; перевірити відсутність дублювання/небезпечного runtime lookup.
- Verification: Кодова/static перевірка та збірка ще не виконані.

## Current verification update
- `git diff --check` passes after adding the prefab child and material.
- `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors; only the existing dependency/version warnings remain.
- Static wiring checks confirm the `PooledImpactFx` decal fields, pool cap, `Impact Decal` prefab child, URP decal material, and placeholder texture references.
- Unity import and Play Mode wall-hit verification remain pending; the open Unity editor should be used to confirm the decal renders with the configured renderer feature.
- `Assets/Game/Content/Fonts/RobotoCondensed-Regular SDF.asset` remains an unrelated pre-existing working-tree modification and was not changed by this task.

## Previous active task carried forward
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Summary: UI penetration-status task додала faction-aware `GunCrosshair` bindings, постійний останній shot result і дефолтний `PENETRATION STATUS`; build пройшов, але Play Mode verification залишилась невиконаною.
- Remaining: Перевірити IA/NV приціли, `Miss`, `Penetrated`, `No penetration` і перемикання фракцій у Play Mode.

## Previous active task carried forward
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording: "потестив, перероби юай, і помітив що ти не підключив параметри для другого прицілу, ти мав підтянути параметри від другого приціду якщо він використовуєтьс, тоїсть щоб там все рахувалдося відображалося так як на першому прицілі, виправ це"
- Objective: Підключити всі динамічні HUD-параметри до активного приціла фракції: для IA використовувати CrosshairType1, для NV — CrosshairType2, щоб боєзапас, статус/прогрес перезарядки, статус наведення та пов'язані візуальні стани оновлювалися однаково.
- Acceptance criteria:
  1. Перемикання фракції вибирає повний набір UI-посилань відповідного приціла, а не лише його графіку.
  2. CrosshairType2 отримує ті самі значення боєзапасу, перезарядки, наведення та серверного/активного приціла, що й CrosshairType1.
  3. Після вибору NV старі IA-елементи не продовжують отримувати динамічні оновлення.
  4. Посилання на обидва набори UI задані в сцені через Inspector-серіалізацію; runtime не шукає об'єкти.
  5. Механічна документація та цей handoff оновлені, а збірка/статичні перевірки проходять.
- Assumptions: Існуюча ієрархія CrosshairType1/CrosshairType2 та поточні користувацькі зміни зберігаються; Play Mode буде перевірено після цього виправлення.
- Work completed: Зіставлено відповідні Image/TMP-поля CrosshairType1 і CrosshairType2 у Client.unity.
- Work remaining: Розширити GunCrosshair повними фракційними bindings, підключити їх у сцені, оновити docs та виконати build/static verification.
- Verification: Початковий аналіз YAML виконано; runtime verification ще не виконано.

- Latest implementation: GunCrosshair switches reticle, server reticle, reload fill, reload text, ammo text, and aim-status text for the active faction. WeaponReloadController forces current cached ammo/reload values into a newly selected target.
- Remaining work: Play Mode verification with IA and NV robots, including firing, reload progress, aim status, and switching between robot factions.
- Verification update: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors; all eight faction parameter references were checked in `Client.unity`. Runtime verification is still pending.

## Previous active task carried forward

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording: "Я зробив окремо приціли для різних фракцій роботів, в GameplayHUD, для IA буде використовуватися CrosshairType1, а для NV буде використовуватися CrosshairType2, тобі треба підключити запрограмувати ці приціли, для кожної фракції окремо, в мене буде приціл в грі в залежності якого робота я вибрав. також для приціла CrosshairType2 є додаткова логіка, там є обьекти lightElement, вони мають тухнути якщо ми вистрілили (прозорість 0) а по мірі заряджання нашої зрої альфа повертається на 1, тоєть коли пушка заряжана альфа елементів lightElement 1, короче альфа залежить від статуса зарядки пушки. реалізуй цю логіку, зроби все максимально чітко та розуміно і напиши коли можна буде тестувати."
- Objective: select `CrosshairType1` for Iron Alliance vehicles and `CrosshairType2` for Nova Syndicate vehicles, then drive every `CrosshairType2/lightElement` alpha from the local weapon reload state.

### Acceptance criteria

1. The local gameplay HUD activates only the crosshair assigned to the selected vehicle faction: IA uses `CrosshairType1`, NV uses `CrosshairType2`.
2. Crosshair selection updates when the local vehicle changes and does not affect authoritative gameplay or other HUD presenters.
3. `CrosshairType2` light elements are alpha `0` immediately after a successful shot and return smoothly to alpha `1` as the weapon reloads; a fully loaded weapon shows alpha `1`.
4. The behavior works through the existing main gameplay HUD and VehicleTest HUD binding paths without runtime component discovery.
5. The relevant HUD and weapon mechanic documentation records the new inspector-wired references and state ownership.

### Assumptions

- `GameplayHUD` already contains the user-authored `CrosshairType1` and `CrosshairType2` objects under the existing `GunCrosshair` presenter, with `lightElement` objects under `CrosshairType2`.
- `WeaponReloadController` remains the single source of truth for local ammo/reload state; the crosshair only presents that state.
- Existing unrelated working-tree changes, including the authored font asset, are user-owned and must be preserved.

### Work recorded

- Read `docs/codex-game/README.md`, `ui-hud.md`, `weapons-damage.md`, `robot-control.md`, `api-resources.md`, and the persistent UI asset instructions.
- Inspected the current HUD presenter, local vehicle root/stat flow, and weapon reload ownership before implementation.
- Added inspector-wired IA/NV reticle roots, local/server reticle targets, and all 12 `CrosshairType2` `lightElement` images to the `GunCrosshair` component in `Client.unity`.
- Added faction selection in `GunCrosshair`, local reload-progress presentation in `WeaponReloadController`, and active-reticle refresh handling in `WeaponReticlePresenter`.
- Updated `ui-hud.md` and `weapons-damage.md` with the new ownership and state flow.

### Verification

- `git diff --check` passed.
- Static scene scan confirmed all IA/NV roots, reticles, server reticles, and 12 light-element image references resolve to objects in `Client.unity`.
- `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors; only existing FishNet/dependency deprecation and assembly-version warnings remain.
- Runtime Play Mode visual verification is still pending.

### Remaining work

- Run Play Mode with an IA robot and an NV robot: verify the selected reticle, reticle movement, server-reticle toggle, immediate alpha `0` after firing, and smooth alpha recovery to `1` when ready.
- Verify the same behavior through VehicleTest if its configured HUD prefab is available in the current Unity project.

## Previous active task carried forward

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "я переробив юай GameplayHUD  на новий, тобі треба тепер підключити всі не підключені елементи, також виправити відображення часу тепер треба в часах. також модливо винести попапи які не треба щоб були там наприклад попап паузи краще щоб це був окремий попап а не в меню хад. тобі дозволяється перейменовувати, виправляти, давати чіткі назви, змінювати ієрархію якщо ти вважаєш що так краще. також тобі треба підключити щоб весь хад працював, після того як робота буде виконана я сам протестую тому тестити не треба."
- Objective: integrate the user-created `GameplayHUD` layout with all existing gameplay HUD behavior, display match time in hours, and keep pause/popups outside the gameplay HUD root where appropriate.
- Follow-up request: stop forcing the local GameplayHUD HP fill color and preserve the color authored on the new HUD image.

### Acceptance criteria

1. Every required gameplay HUD presenter has a valid inspector-wired target in the new `GameplayHUD` hierarchy, including timer, local vehicle status, weapon/reticle, map, player list, performance, and damage feedback where those presenters are used.
2. Match time is formatted in hours, minutes, and seconds without changing the authoritative timer source.
3. Pause UI is routed as a separate popup/menu layer and is not owned by the gameplay HUD presentation root.
4. Existing `MenuManager`, input-blocking, VehicleTest, and runtime HUD binding paths remain valid with the new hierarchy.
5. Documentation identifies the new hierarchy and the owner of every changed HUD binding.

### Assumptions

- The current `Client.unity` diff and untracked `Assets/Game/Content/UI/New` assets are the user's new HUD work and must be preserved unless a broken reference requires a focused correction.
- Unity/Play Mode verification is intentionally deferred to the user; this task will use static serialization and source inspection only.

### Work recorded

- Read `docs/codex-game/README.md`, `ui-hud.md`, `weapons-damage.md`, `menus-settings-progression.md`, `vehicle-test-scene.md`, and the persistent UI asset workflow instructions.
- Confirmed the working tree contains a large `Client.unity` HUD diff and a new UI asset folder before implementation.
- Renamed the new HUD containers to `GameplayHudContent`, `GameplayHudPanels`, `VehicleStatusPanel`, `MatchTimerPanel`, `AmmoCountText`, `ReloadStatusText`, and `MatchTimerText`.
- Rewired the local HP presenter to the new image fill, HP/name labels, and vehicle status panel; rewired weapon ammo/reload/scope targets and map marker sprites.
- Updated both initial and event-driven local HP paths to support the new `Image` fill when no `Slider` is present.
- Formatted the authoritative match timer as `HH:MM:SS`.
- Moved the pause visual root to `Client.unity/Canvas/GameplayPausePopup` and retained `GameplayPauseController` as its separate menu registration point.
- Updated `docs/codex-game/ui-hud.md` with the new hierarchy and ownership map.
- Updated `docs/codex-game/weapons-damage.md` with the weapon HUD target names and scope-root ownership.
- User reported that the local HP fill color is being forced at runtime; `HealthBarUI.ApplyHealthColor` is the identified owner and must be removed from the local HUD update path.
- Removed the runtime local-HP color assignment and the now-unused color helper/state; HP amount updates remain active through `SetNormalizedValue`.

### Verification

- Static serialization/source review completed for the changed scene references and HUD owner scripts.
- No Unity, Play Mode, build, or runtime tests run by request.

### Remaining work

- User Play Mode verification remains: confirm timer, local HP/name/ammo/reload, crosshairs/scope, map markers, player list, performance text, damage flash, and pause routing in the rebuilt HUD.
- Follow-up verification: confirm in Play Mode that the Inspector-authored `barHp` color remains unchanged while HP amount still updates.

## Previous active task carried forward

- The previous active task was left at `IMPLEMENTED_NEEDS_VERIFICATION` and required a Play Mode visibility check for robot renderer hiding. Its code/prefab changes are preserved below as historical context rather than silently discarded.

## Previous task: robot renderer visibility

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "помітив баг видимості, робот має зникати повністю коли його не бачать але чатина меша не зникає, спочатку точно розберися в чому проблема і потім виправ баг."
- Objective: ensure every non-armor robot renderer is hidden whenever `VehicleClientVisibility` marks the vehicle invisible.

### Acceptance criteria

1. The visibility list on every registered IA/NV prefab contains every non-armor `MeshRenderer` and `SkinnedMeshRenderer`, excluding HUD and armor geometry.
2. Hiding an enemy applies to every model renderer, so no mesh remains visible when its map visibility is false.
3. Server-only visual stripping and existing death visibility freeze continue using the same complete renderer list.
4. All eight registered prefabs pass integrity validation after the repair.

### Investigation findings

- `VehicleClientVisibility.SetRenderersVisible` only iterates its serialized `visualRenderers` array; it does not discover child renderers at runtime.
- The registered prefab arrays contain null slots and only partial renderer coverage, while `VehiclePrefabIntegrityEditor` already rebuilds the complete non-armor renderer set for `VehicleHoverOutline`.
- This mismatch explains the symptom: renderers omitted from `visualRenderers` never receive `forceRenderingOff = true`, so part of the robot remains visible.

### Work recorded

- Added an editor repair pass that rebuilds `VehicleClientVisibility.visualRenderers` from every non-armor model renderer, excluding the vehicle HUD.
- Repaired all eight registered IA/NV prefabs, removing stale null/partial visibility entries and assigning the complete renderer lists.
- Updated `robot-control.md` so visibility, server stripping, and hover outline share the same renderer ownership contract.
- Edited: `Assets/Editor/VehiclePrefabIntegrityEditor.cs`, all eight registered IA/NV prefab files under `Assets/Game/Content/Robots`, and `docs/codex-game/robot-control.md`.

### Verification

- Read `VehicleClientVisibility`, `VehicleRoot`, `VehicleHoverOutline`, and `VehiclePrefabIntegrityEditor` ownership paths.
- Static inspection of all eight registered IA/NV prefab visibility arrays confirmed incomplete coverage and null entries before the repair.
- Unity batch `Game.Scripts.Editor.VehiclePrefabIntegrityBatch.RepairAndValidate` repaired 8 bindings across 8 prefabs and passed post-repair validation with zero errors.
- Static scan confirms every repaired visibility list is non-null, contains only mesh/skinned renderer references, and matches its prefab's outline renderer list: IA `23/13/15/13`, NV `10/11/10/10`.
- `git diff --check` passed; no runtime compiler errors were reported by the batch validation log.

### Remaining work

- Run a Play Mode visibility test with an enemy leaving map visibility and confirm no model mesh remains rendered.

## Previous completed task (armor cleanup)

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "броня має бути бронею, її mesh не повинен потрапляти в список деталей робота й ніколи не має ставати видимим; він потрібен тільки для редагування броні"
- Objective: exclude all armor mesh objects from robot death debris and remove debris physics from editor-only armor meshes.

### Acceptance criteria

1. `DeathLogic.detachableVisuals` contains no object whose hierarchy is under `Armor`.
2. Armor mesh objects remain editor-only and do not receive debris `Rigidbody` or `MeshCollider` components.
3. Armor colliders and armor-controller references remain unchanged.
4. All eight registered IA/NV prefabs pass integrity validation.

### Work recorded

- Removed 56 armor mesh objects from `DeathLogic.detachableVisuals` across the eight registered IA/NV prefabs.
- Removed the 112 debris `Rigidbody`/`MeshCollider` components previously added to those armor mesh objects; armor collider arrays and controller references were left unchanged.
- Updated `robot-control.md` and `weapons-damage.md` to document armor as editor-only geometry excluded from death debris.

### Verification

- Static scan confirms all armor paths have zero debris physics and zero `DeathLogic.detachableVisuals` entries.
- Static scan confirms non-armor debris counts remain aligned: each configured death entry has a kinematic `Rigidbody` and convex `MeshCollider`; legacy arrays remain empty.
- Removed component IDs have no remaining serialized references, so armor controller and armor collider bindings are not dangling.
- The open Unity editor reimported the changed prefab assets without compiler errors; full integrity menu validation after this cleanup remains pending.

### Remaining work

- Reimport changed prefabs and run the registered-prefab integrity validation.

## Previous completed task (mesh debris physics)

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "видали ріджід баді з усіх парентів мешів, прибери вимкнені меш колайдери, додай Rigidbody і convex MeshCollider на всі об'єкти з MeshRenderer та активуй саме ці об'єкти при знищенні"
- Objective: rebuild robot death debris so each rendered mesh object owns its own disabled-kinematic Rigidbody and convex MeshCollider, and DeathLogic activates those objects on destruction.

### Acceptance criteria

1. Registered IA and NV robot prefabs no longer use Rigidbody/MeshCollider on obsolete mesh parents.
2. Every configured death debris object with a MeshRenderer has a kinematic Rigidbody and a convex MeshCollider while alive.
3. DeathLogic activates the configured mesh debris objects when the robot dies and no longer depends on the removed parent-level physics objects.
4. Existing visual/death timing behavior remains intact and prefab references are valid.

### Assumptions

- The change applies to the eight registered IA/NV robot prefabs and their existing configured/legacy debris lists.
- Existing visual meshes and transforms are preserved; only physics ownership and serialized debris references are rebuilt.
- No compatibility path for the obsolete parent-level physics setup is required.

### Work recorded

- Rebuilt all eight registered IA/NV robot prefabs so every `MeshRenderer` object owns a disabled convex `MeshCollider` and kinematic `Rigidbody` using its own `MeshFilter.sharedMesh`.
- Removed non-renderer parent-level `Rigidbody` and `MeshCollider` components and cleared the legacy parallel debris arrays.
- Rebuilt `DeathLogic.detachableVisuals` with one entry per mesh-renderer object and updated `robot-control.md` and `weapons-damage.md` to document the new ownership contract.

### Verification

- Isolated Unity batch migration imported all eight prefabs successfully and saved 153 configured non-armor mesh debris objects.
- Static prefab scan passed: `MeshRenderer`, `Rigidbody`, `MeshCollider`, convex, kinematic, and non-null `DeathLogic` entry counts match for every registered prefab.
- `Game.Scripts.Editor.VehiclePrefabIntegrityBatch.RepairAndValidate` passed for all 8 registered prefabs with zero repairs and zero validation errors.
- The open Unity editor reimported the changed prefab assets and completed script compilation without compiler errors.
- Unity emitted 14 convex-hull polygon-limit warnings for high-poly meshes; Unity still created partial convex hulls as requested. Play Mode death behavior remains pending.

### Remaining work

- Let the open Unity editor reimport the eight changed prefab files.
- Run a Play Mode death test and confirm each mesh object detaches, renders, and fades without parent-level physics or missing references.

This is the durable state for one active Codex task. A new Codex chat must read this file and inspect the working-tree changes before acting on a request to continue.

## Previous completed task (settings)

- Status: `COMPLETED`
- Started: 2026-10-19
- Last updated: 2026-10-19
- Original user wording: "глянь сетінги, продивися код, ссилки, як все налаштовано та реалізовано, і запрограмуй так щоб кнопка аплай була активна тільки тоді коли дійсно щось треба аплаїти, в інших випадках вона не активна, це про сетінги відео."
- Objective: keep the Video settings Apply button disabled until a real video-setting change is pending.

### Acceptance criteria

1. Video Apply is disabled on initial load when controls match the applied baseline.
2. Changing any Video dropdown, VSync toggle, or Gamma slider enables Apply.
3. Reverting controls to the baseline or applying changes disables Apply again.
4. Existing settings controller behavior and non-Video tab Apply behavior remain wired.

### Assumptions

- The applied Video values are represented by the controls populated in `VideoTabView.SetData`, so the baseline is captured from those controls after initialization.
- `SettingsController` continues to update the model immediately; Apply remains the point that persists and applies Video values.
- The existing non-Video tabs keep their current Apply behavior; this task changes only Video pending-state feedback.

### Work recorded

- Added a Video-settings baseline snapshot and `PendingChangesChanged` event to `VideoTabView`.
- Added listener cleanup before rebinding, change notifications for every Video control, and baseline refresh after Apply.
- Updated `SettingsView` to subscribe to the Video pending-state event and drive `ApplyButton.interactable` from the current Video baseline.
- Updated `menus-settings-progression.md` with the Apply-state ownership and lifecycle.

### Verification

- `git diff --check` passed for the two changed settings scripts.
- Unity recompiled without a visible compile-error modal.
- Play Mode with `ClientMenuTestMode`: initial Video Apply was disabled; changing Quality to Balanced enabled it; Apply disabled it after saving; changing back to High Fidelity enabled it again; applying once more disabled it.
- Play Mode was stopped after verification; no scene changes were saved.

### Remaining work

None.

## Completed task history

### Video settings Apply pending-state tracking — completed

- `VideoTabView` now owns the applied Video baseline and reports whether dropdown, VSync, or Gamma values differ from it.
- `SettingsView` updates the serialized Apply button from that state and refreshes the baseline after Video Apply.
- Verified the disabled → enabled → disabled lifecycle in Play Mode with `ClientMenuTestMode`.

### First vertical anchor correction — superseded by current hierarchy diagnostic

- Set all five template-item rects to top vertical anchors while preserving the existing horizontal stretch and 36-pixel row height.
- Initial Play Mode testing appeared improved, but the user's follow-up screenshot shows the first option still clipped and unused backing height below the list; the correction is therefore not accepted as final.

### TMP dropdown rebuild and first visibility repair — completed before current geometry follow-up

- Rebuilt the one Free XP and four Video settings selectors as inspector-wired `TMP_Dropdown` controls, preserving existing artwork, sizes, text, settings effects, and listeners.
- Fixed template validation by keeping every template-item `Toggle` active beneath its inactive `Template`; fixed invisible rows by stretching their horizontal anchors from `x: 0` to `1`.
- Removed the obsolete custom option scripts and references; static scans, compilation, and initial Play Mode selection/list-blocker checks passed.
- The current task follows a later screenshot that exposes the remaining vertical-template offset.

### Settings dropdown Free XP visual clone — superseded by stable TMP rebuild

- Rebuilt the four Video settings controls as custom Free XP-style button/list dropdowns and preserved their value events and VSync frame-rate disabling.
- Static checks and C# build passed, but runtime review found that multiple lists could stay open and list backing height was fixed; this implementation is now intentionally replaced by the active task.

### Settings dropdown behavior migration — superseded

- Replaced four Video `TMP_Dropdown` controls with `SettingsDropdown` and `SettingsDropdownOption` while keeping existing setting events.
- This custom implementation is obsolete under the active project-wide TMP rebuild.

### Free XP dropdown — incorrect inferred refactor discarded

- The Free XP selector was temporarily converted after an inferred request, then restored when the user clarified Free XP was the desired visual reference.

### Post-battle rewards — implementation complete, deployment and match verification pending

- Persisted and returned Bolts/Free XP reward amounts, routed their authoritative availability through the result popup, and documented the result flow.
- Builds and payload mapping checks passed; pending: deploy the API migration and verify a completed live match.

### Procedural sniper scope mask — implementation complete, visual zoom check deferred

- Replaced `Hud_Aim_Bracket.png` with `ProceduralSniperScopeMask.shader` and its material; `Client.unity/SniperScopeMask` has no sprite and uses a full-screen procedural circular aperture.
- Unity imported the shader/material without compiler errors; pending: match visual verification.

### Main-menu robot-card scrolling and preview stability — closed by user without Play Mode verification

- `Client.unity` includes a horizontal robot-card `ScrollRect`; `RobotView` preserves position while refreshing cards.
- Automated checks passed; required Play Mode checks were closed by user before beginning subsequent work.
