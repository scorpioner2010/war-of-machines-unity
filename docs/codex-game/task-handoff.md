# Codex Task Handoff

## Active task
- Status: IMPLEMENTED_NEEDS_VERIFICATION
- Started: 2026-10-20
- Last updated: 2026-10-20
- Original user wording: "помітив що ти не підключив статус пробиття на другому прицілі, підключи його + додай логіку щоб він не зникав а показував останій статус, наприклад я скосив там пише міс, то щоб міс не зникав, а по замовчуваню там має бути дефолтний текст статус пробитя на англ"
- Objective: Підключити статус результату пострілу до CrosshairType1 і CrosshairType2 та зберігати останній статус постійно видимим. При старті показувати дефолтний англійський статус пробиття.
- Acceptance criteria:
  1. IA і NV приціли мають власні inspector-wired поля статусу пробиття.
  2. Результати `Miss`, `Penetrated` і `No penetration` оновлюють активний приціл.
  3. Останній статус не очищається через таймер і залишається видимим до наступного пострілу.
  4. До першого результату показується дефолтний англійський текст статусу пробиття.
  5. Документація оновлена, збірка проходить, runtime lookup UI не використовується.
- Assumptions: Поточний `GameplayGUI` є власником старого тексту результату пострілу; його legacy-поведінка буде замінена постійним faction-aware HUD статусом.
- Work completed: Додані IA/NV penetration-status bindings, постійне збереження останнього результату та дефолтний `PENETRATION STATUS`. `NetworkWeaponShooter` тепер пише результат напряму в активний `GunCrosshair`; старий shot-result timer і його settings field видалені.
- Work remaining: Play Mode verification для IA/NV після пострілу, включно з перемиканням фракції та перевіркою що `Miss`, `Penetrated`, `No penetration` не зникають.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --verbosity:minimal` passed with 0 errors; both faction penetration component references exist in `Client.unity`; no obsolete timer references remain in runtime/editor code.

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
