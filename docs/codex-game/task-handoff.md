# Codex Task Handoff

## Active task
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "помітив що круг зведення при зарядці заповніться не правно, треба щоб він плавно заповнівся виправ це, також колір відлітаючого ХП якщо ХП синє (союзник) то відлітати теж синє має, якщо красне як зараз (противник) то відлітає теж красне, виправ це"
- Objective: make the weapon charge/reload circle fill smoothly during charging and make floating damage text use the same ally/enemy color as the target world HP bar.
- Acceptance criteria:
  - The charge circle uses the existing local/server charge progress and no longer receives a visibly stepped server-only `fillAmount`.
  - A newly started reload can reset the circle immediately, then fills smoothly to full.
  - Floating damage text is blue for allied vehicles and red for enemy vehicles, matching the HP bar relation color.
  - Reload authority, ammo gating, pooled floating text behavior, and visibility rules remain unchanged.
- Assumptions: the reported circle is the inspector-wired `GunCrosshair.fillImage` radial reload image; `VehicleHUD` already owns the authoritative ally/enemy relation color used by the world HP bar.
- Current work: `GunCrosshair` now owns smoothed upward reload-fill presentation from the existing charge progress; `WeaponReloadController` no longer overwrites that fill directly; `VehicleHUD` passes the relation HP color into `FloatingDamageText`.
- Work remaining: optionally run the gameplay HUD in Unity Play Mode and confirm the circle fills continuously after a shot and allied/enemy floating text colors match their HP bars.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/VehicleHUD.cs`, `Assets/Game/Scripts/Gameplay/Robots/WeaponReloadController.cs`, `Assets/Game/Scripts/UI/HUD/FloatingDamageText.cs`, `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`, `docs/codex-game/ui-hud.md`, `docs/codex-game/weapons-damage.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: targeted static assertions passed; `git diff --check` passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (selected robot image in DevelopmentTree - previous active task)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "додай щоб підтягувало картинку вибраного робота в DevelopmentTree, картинку підтягувати має на обьект Image. якщо техніка не вибрана обьект Image має бути вимкнутий"
- Objective: use the `DevelopmentTree/RightInfoPanel/Image` object for the selected robot icon and deactivate that GameObject when no technique is selected.
- Acceptance criteria:
  - The target remains the existing `DevelopmentTree/RightInfoPanel/Image` object shown in the provided screenshot.
  - A selected tree node assigns its robot sprite to that Image.
  - No selected technique deactivates the Image GameObject, not only its `Image.enabled` property.
  - Selecting a technique activates the Image GameObject and displays its icon.
- Assumptions: the existing serialized `vehicleImage` reference is wired to the screenshot target and `GameResourceManager.GetIcon(node.code)` remains the authoritative icon source.
- Current work: `DevelopmentTreeDetailsPanel` now activates the serialized `RightInfoPanel/Image` GameObject only when the selected node resolves an icon.
- Work remaining: optionally run the DevelopmentTree screen in Unity Play Mode to confirm the selected and cleared visuals.
- Edited files: `Assets/Game/Scripts/UI/Tree/DevelopmentTreeDetailsPanel.cs`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/scene assertions passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (selected robot image in DevelopmentTree - previous implementation)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
 - User request: "додай щоб підтягувало картинку вибраного робота в DevelopmentTree, картинку підтягувати має на обьект Image"
- Objective: display the selected development-tree robot icon in the existing `RightInfoPanel/Image` UI object.
- Acceptance criteria:
  - `DevelopmentTreeDetailsPanel` owns a serialized `Image` reference for the existing right-panel image.
  - Selecting a tree node assigns the robot sprite resolved from its vehicle code.
  - Clearing the selection clears and disables the image.
  - The existing TreeItem image binding and selection behavior remain unchanged.
- Assumptions: `GameResourceManager.GetIcon(node.code)` is the authoritative robot image source, and `RightInfoPanel/Image` is the requested target.
- Current work: `DevelopmentTreeDetailsPanel` updates `RightInfoPanel/Image` from the selected node code and clears/disables it when no node is selected.
- Work remaining: optionally run the DevelopmentTree screen in Unity Play Mode to confirm the selected and cleared visuals.
- Edited files: `Assets/Game/Scripts/UI/Tree/DevelopmentTreeDetailsPanel.cs`, `Assets/Game/Scenes/Client.unity`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/scene assertions passed; targeted `git diff --check` passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (disable Vehicle BlackScreen when unselected)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "якщо елемент не вибраний BlackScreen має бути вимкнений"
- Objective: deactivate the `BlackScreen` GameObject for unselected main-menu vehicle items and activate it only for the selected item pulse.
- Acceptance criteria:
  - Unselected `Vehicle` items have the serialized `BlackScreen` GameObject inactive.
  - Selected `Vehicle` items activate `BlackScreen` and continue the alpha pulse.
  - Deselection restores the authored alpha before deactivating `BlackScreen`.
  - No scale behavior changes.
- Assumptions: the existing serialized `CanvasGroup` reference is sufficient to control the target `BlackScreen` GameObject through its `gameObject` property.
- Current work: `VehicleBlink` now toggles the serialized `BlackScreen` GameObject with selection; the prefab defaults it inactive.
- Work remaining: optionally run the main-menu screen in Unity Play Mode to confirm the selected and unselected visuals.
- Edited files: `Assets/Game/Scripts/UI/MainMenu/VehicleBlink.cs`, `Assets/Game/Content/UI/Prefabs/MainMenu/Vehicle.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted `git diff --check` passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (limit Vehicle pulse to selected items)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "тільки для вибраних айтемів має цей ефект працювати"
- Objective: run the `Vehicle` BlackScreen alpha pulse only while the main-menu vehicle item is selected.
- Acceptance criteria:
  - `VehicleItemView` passes `data.IsSelected` to the serialized `VehicleBlink` reference.
  - Selected vehicle items pulse the `BlackScreen` CanvasGroup alpha.
  - Unselected vehicle items do not update through the pulse and restore their authored alpha.
  - Scale behavior remains unchanged.
- Assumptions: `VehicleSlotVehicleData.IsSelected` is the authoritative selection state and the existing `VehicleBlink` component remains on the prefab root.
- Current work: `VehicleItemView` gates `VehicleBlink` with `data.IsSelected`; deselection and component disable restore the authored `BlackScreen` alpha.
- Work remaining: optionally run the main-menu screen in Unity Play Mode to confirm selected/unselected behavior.
- Edited files: `Assets/Game/Scripts/UI/MainMenu/VehicleBlink.cs`, `Assets/Game/Scripts/UI/MainMenu/VehicleItemView.cs`, `Assets/Game/Content/UI/Prefabs/MainMenu/Vehicle.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted `git diff --check` passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (add Vehicle BlackScreen alpha pulse)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "додай такий самий скріпт пульсації для Vehicle, але без скеййла і мінятися має канвас грп на BlackScreen обьекті"
- Objective: add an inspector-configurable alpha pulse to the `Vehicle` prefab, targeting the `BlackScreen` child CanvasGroup without changing scale.
- Acceptance criteria:
  - `Vehicle` owns a pulse component with configurable alpha A/B and speed.
  - The pulse changes only the serialized `BlackScreen` CanvasGroup alpha.
  - No image scale or transform scale is modified.
  - The `Vehicle.prefab` reference points to the existing `BlackScreen` CanvasGroup.
- Assumptions: the pulse component is attached to the `Vehicle` prefab root and runs with unscaled time, matching `TreeItemBlink`.
- Current work: `VehicleBlink` is attached to the `Vehicle` prefab root and drives the serialized `BlackScreen` CanvasGroup alpha through a smooth ping-pong cycle.
- Work remaining: optionally run the main-menu screen in Unity Play Mode to visually confirm the pulse and tune alpha/speed values.
- Edited files: `Assets/Game/Scripts/UI/MainMenu/VehicleBlink.cs`, its `.meta`, `Assets/Game/Content/UI/Prefabs/MainMenu/Vehicle.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted `git diff --check` passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (sync TreeItem CanvasGroup alpha with selection)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "TreeItem якщо елемент не активний то канвас груп альфа 0.35, якщо активний 1"
- Objective: synchronize the `TreeItem` root `CanvasGroup.alpha` with the selected state of the development-tree item.
- Acceptance criteria:
  - Unselected runtime `TreeItem` instances use alpha `0.35`.
  - The currently selected runtime `TreeItem` uses alpha `1`.
  - `TreeItem.isSelect` selection behavior remains unchanged and is synchronized by the same `SetSelected` call.
  - Initial creation, faction changes, and tree rebuilds leave non-selected items at alpha `0.35`.
- Assumptions: “active” means the selected development-tree item, matching the preceding `IsSelect` behavior; the root `TreeItem` already owns a `CanvasGroup`.
- Current work: `TreeItem.SetSelected(bool)` now updates both the serialized `IsSelect` child and root `CanvasGroup.alpha`; the prefab reference and default alpha are serialized.
- Work remaining: optionally run the development-tree screen in Unity Play Mode to visually confirm selection and deselection alpha.
- Edited files: `Assets/Game/Scripts/UI/Tree/TreeItem.cs`, `Assets/Game/Content/UI/Prefabs/Tree/TreeItem.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted `git diff --check` passed; production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (bind robot names and TreeItem level text)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "в префабі Vehicle для головного меню підтягни нейм робота, також в TreeItem піднягти нейм та левел."
- Objective: display the vehicle name in the main-menu `Vehicle` prefab and wire the existing name and level text fields in `TreeItem.prefab`.
- Acceptance criteria:
  - Main-menu `VehicleItemView` writes the resolved robot name into the prefab's `Name` TMP text.
  - `Vehicle.prefab` serializes that `Name` TMP reference.
  - `TreeItem.prefab` serializes its `Name` TMP into `TreeItem.vehicleName` and its `lvl` TMP into `TreeItem.level`.
  - Existing runtime data flow, selection visuals, and layout remain unchanged.
- Assumptions: the existing `VehicleSlotVehicleData.Name` is the authoritative display name, with `Vehicle` as the existing fallback when it is empty; `DevelopmentTree` already supplies `node.name` and `node.level`.
- Current work: `VehicleItemView` writes `VehicleSlotVehicleData.Name` into the serialized `Vehicle` prefab label; `TreeItem.prefab` now wires its `Name` and `lvl` TMP components to `TreeItem.vehicleName` and `TreeItem.level`.
- Work remaining: optionally run the main-menu and development-tree screens in Unity Play Mode to visually confirm the labels with live data.
- Edited files: `Assets/Game/Scripts/UI/MainMenu/VehicleItemView.cs`, `Assets/Game/Content/UI/Prefabs/MainMenu/Vehicle.prefab`, `Assets/Game/Content/UI/Prefabs/Tree/TreeItem.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: serialized-reference assertions passed; targeted `git diff --check` passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (synchronize TreeItem IsSelect with development-tree selection)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "тепер запрограмуй щоб IsSelect тру був тільки тоді коли елемент вибраний в дереві розвитку, якщо елемент не вибраний IsSelect фолс"
- Objective: make each runtime development-tree `TreeItem` activate its serialized `IsSelect` child only while that item is the currently selected node.
- Current work: `TreeItem.isSelect` is wired to the prefab's inactive `IsSelect` child; `DevelopmentTree` keeps one selected item, disables the previous item, and clears selection when changing faction or rebuilding.
- Work remaining: optionally reproduce clicking multiple nodes and switching factions in Unity Play Mode.
- Edited files: `Assets/Game/Scripts/UI/Tree/TreeItem.cs`, `Assets/Game/Scripts/UI/Tree/DevelopmentTree.cs`, `Assets/Game/Content/UI/Prefabs/Tree/TreeItem.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted diff check and production/editor C# builds passed with 0 errors. Unity Play Mode visual verification remains pending.

## Archived task (configurable TreeItem image blinking)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "є префаб TreeItem, тобі треба додати компонент мигання, що мається на увазі, тобі в масив треба додати імеджі Image (0) - Image (3), а мигати вони мають по хитрому, альфа від А до Б, + скейл по Y від А до Б, компонент має висіти на TreeItem, і параметри мають налаштовуватися, також швидкість мигання має налаштоваватися, створи цей компонент наципи на TreeItem, а я потім налаштую параметри"
- Objective: add an inspector-configurable blinking component to `TreeItem.prefab` and assign its serialized image graphics.
- Acceptance criteria: the root owns `TreeItemBlink`; its image array, alpha A/B, Y-scale A/B, and speed are serialized; the images animate together through a smooth repeating cycle; existing tree behavior remains intact.
- Current work: `TreeItemBlink` is attached to the prefab root. The user subsequently tuned the prefab to eight decorative images and changed its serialized values.
- Work remaining: optional Unity Play Mode visual verification after final Inspector tuning.
- Edited files: `Assets/Game/Scripts/UI/Tree/TreeItemBlink.cs`, its `.meta`, `Assets/Game/Content/UI/Prefabs/Tree/TreeItem.prefab`, `docs/codex-game/menus-settings-progression.md`, this handoff, and `docs/codex-game/task-history.md`.
- Verification: static source/prefab assertions passed; targeted diff check passed; production/editor C# builds passed with 0 errors. Full working-tree diff check still reports a pre-existing trailing-whitespace line in the user-modified prefab.

## Archived task (resize robot images to 1024 square)
- Status: `COMPLETED`
- Started: 2026-10-22
- User request: "зроби всі картинки розімра 1024 на 1024"
- Objective: resize every image in `Assets/Game/Content/Robots/NV/Images` and `Assets/Game/Content/Robots/IA/Images` to exactly 1024x1024.
- Acceptance criteria:
  - All eight PNG images in the two folders are 1024x1024.
  - Preserve each image's transparency, filename, and Unity `.meta` sidecar.
  - Preserve proportions using premultiplied-alpha LANCZOS resampling.
  - Keep originals backed up outside `Assets`; do not change scenes, prefabs, or code.
- Assumptions: the user asked to resize the files in place at the two specified paths; all eight source images are square 1254x1254 RGBA PNGs.
- Current work: all eight PNGs were resized in place with premultiplied-alpha LANCZOS.
- Work remaining: none.
- Edited files: the eight PNGs in `Assets/Game/Content/Robots/NV/Images` and `Assets/Game/Content/Robots/IA/Images`, plus this handoff and `docs/codex-game/task-history.md`.
- Verification: all eight PNGs were reopened with Pillow 9.5.0 and verified as 1024x1024 RGBA with alpha range 0-255; Unity `.meta` SHA256 hashes remained unchanged. Originals were backed up outside `Assets`. No scenes, prefabs, or code changed.

## Archived task (reticle narrowing continuous to full aim)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "ні щось тут все одно не те, приціл звузився і зупинився а текст показує ще що звужується, текст повільніше доходить до звуження чим саме кільце щось тут не те, знайди що не так та виправ"
- Objective: keep the visible dispersion ring changing until the server-authoritative dispersion reaches the same fully aimed state represented by `AimStatus`; a profile calibration value must not make the ring plateau early.
- Acceptance criteria:
  - The ring does not stop at `reticleBaseDiameter` while the server dispersion is still above minimum.
  - `reticleBaseDiameter` remains usable as the calibrated diameter at fully aimed state, while current dispersion expansion remains visible above it.
  - The ring and `AimStatus` reach their final state from the same server dispersion value.
  - Runtime/editor builds, focused static checks, documentation, and `git diff --check` pass.
- Assumptions: the remaining mismatch is caused by `GetCrosshairDiameter` applying `Mathf.Max(..., calibratedMinimumDiameter)` as a hard floor; current server synchronization is already correct.
- Current work: `GetCrosshairDiameter` now uses the calibrated fully-aimed baseline plus continuous physical dispersion expansion instead of a hard base-diameter floor.
- Work remaining: visually reproduce a shot/movement settle sequence in Client Play Mode and confirm the ring keeps narrowing until `AimStatus` reaches its final state.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: production/editor C# builds passed with 0 errors; static checks confirmed the hard floor is removed and the minimum-dispersion baseline is projected; `git diff --check` passed. Client Play Mode visual verification remains pending.

## Archived task (server-authoritative dispersion for HUD narrowing)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "я помітив що приціл швидше звужується чим показує це таймер звудження в хаді, скоріше за все якісь невірні параметри звуження, треба щоб звуження підхватувалося з сервера і тягнуло параметер звідти як це було раніше"
- Objective: make the visible dispersion ring and HUD aim-progress indicator use the server-authoritative dispersion value and remain consistent with the server's narrowing behavior.
- Acceptance criteria:
  - The local gameplay HUD no longer uses the client-only dispersion model as the primary narrowing source after the server value is available.
  - The visible ring and `AimStatus` percentage use the same server-authoritative `CurrentDeg` value.
  - The existing server fallback before the first sync remains safe and does not affect shot validation or projectile dispersion.
  - Runtime/editor builds, focused static checks, documentation, and `git diff --check` pass.
- Assumptions: the server already synchronizes `_serverDispersionDeg`; the mismatch is caused by HUD presentation using `_ownerDispersion.CurrentDeg` for the main ring and status text.
- Current work: the client HUD now uses the synchronized server dispersion value for the main ring and `AimStatus`, retaining the local model only as a pre-sync fallback.
- Work remaining: visually reproduce a shot/movement settle sequence in Client Play Mode and confirm the ring and HUD progress narrow together.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/ui-hud.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors; static source assertions confirmed `AimStatus` and the main ring use the server value; `git diff --check` passed. Client Play Mode visual verification remains pending.

## Archived task (preserve aim point during sniper camera transition)
- Status: `COMPLETED`
- Started: 2026-10-22
- User request: "майже все ідеально, але є зсув і виглядає він як баг, приціл ззивається, пояснюю коли вмикається снайперський приціл (чорний круг) пішка міняє трохи місце прицілювання, це виглядає як баг, запусти гру сцену клієнт, вибери першого робота і потім перемикай роликом вперід поки не дійдеш до зумі і ти помітиш що приціл зсувається, я понямаю що він скоріше за все зсувається щоб ми дивилися з місця пушки так це вірно але він не має змінювати місце наведення, розберися в чому проблема та виправ"
- Objective: preserve the current world-space aim point when entering or leaving any sniper-scope zoom profile; moving the camera to the gun anchor must not retarget the weapon.
- Acceptance criteria:
  - Scrolling into a checked sniper profile keeps the same world aim point visible before and after the camera-origin transition.
  - Leaving a sniper profile also preserves the current world aim point when returning to orbit camera presentation.
  - The weapon's desired/current aim is not overwritten with a new target solely because the camera moved to the sniper anchor.
  - Multiple checked forward profiles remain reachable and retain their existing scope behavior.
  - Runtime/editor builds, static checks, documentation, and `git diff --check` pass.
- Assumptions: the reported shift is camera parallax caused by changing the camera origin without recalculating yaw/pitch; projectile dispersion and weapon aim logic should remain unchanged.
- Current work: `ApplyZoomStep` now aligns the destination camera origin to the existing world-space gun aim point whenever the transition enters or leaves a checked sniper profile; weapon targeting and projectile logic remain untouched.
- Work remaining: none.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `docs/codex-game/robot-control.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Client Play Mode was tested with the first `IA Skirmisher`; zoom steps 1–4 were traversed, the black sniper scope activated at step 3, and no visible reticle/aim-point jump occurred at activation or the next zoom step. `dotnet build Assembly-CSharp.csproj --no-restore --nologo` and `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors; static transition checks and `git diff --check` passed.

## Archived task (allow multiple sniper-scope zoom profiles)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "я налаштував приціл вперед, чому на 2 та на 3 кроці не зявляється чорний круг вокруг приціла? я ж поставив галочки що не так?"
- Objective: allow every forward zoom profile with `showReticle = true` to show the black sniper-scope overlay, while allowing the wheel to continue through multiple flagged profiles.
- Acceptance criteria:
  - Forward profiles with `showReticle = true` each activate the black `SniperScopeOverlay` and central sniper reticle.
  - Scrolling forward/backward does not stop at the first flagged profile; adjacent flagged profiles remain reachable and keep the overlay active.
  - Scrolling from a flagged profile to an unflagged profile hides the overlay and restores ordinary camera presentation.
  - The Shift/toggle sniper shortcut still enters the first flagged forward profile and exits to the last non-sniper step.
  - Runtime/editor builds, static checks, documentation, and `git diff --check` pass.
- Assumptions: the screenshot's Element 2 and Element 3 are both intentional sniper presentation profiles; `showReticle` is therefore a per-profile presentation flag, not a single global sniper-step marker.
- Current work: wheel navigation now advances and retreats through every forward profile regardless of its `showReticle` value; each checked profile independently keeps sniper presentation active. The explicit sniper toggle still targets the first checked profile and exits to the last ordinary step.
- Work remaining: reproduce the two consecutive checked profiles in Client Play Mode with the user's native wheel input.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `docs/codex-game/robot-control.md`, `docs/codex-game/ui-hud.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: multiple-profile traversal assertions passed; production and editor C# builds passed with 0 errors; relevant documentation and `git diff --check` passed. Manual Play Mode verification remains pending because the Client Unity Editor is already open and native input injection is unavailable.

## Archived task (correct sniper-scope checkbox semantics)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "я ставлю галочку але круг не зявляється можливо ти мене не вірно зрозумів? це той круй який імітує снайперський приці, чорний фон а по сиридині наш приціл"
- Objective: correct the meaning of the forward-profile checkbox so it controls the black sniper-scope overlay with the central reticle, not the ordinary dispersion ring.
- Acceptance criteria:
  - `showReticle = true` on a forward zoom profile selects sniper presentation.
  - Sniper presentation activates the existing black `SniperScopeOverlay` and central sniper reticle through `GunCrosshair.SetSniperMode(true)`.
  - `showReticle = false` leaves the profile in ordinary camera presentation and does not hide or change projectile dispersion.
  - Default, normal forward, and backward profiles are serialized with `showReticle = false`; the migrated sniper profile is `true`.
  - Runtime/editor builds, static checks, documentation, and `git diff --check` pass.
- Assumptions: the user refers to the existing `SniperScopeOverlay` plus `sniperReticleOnlyRoot`; the ordinary dispersion ring remains controlled by its existing projection logic.
- Current work: the checkbox now uses `true` for the existing black `SniperScopeOverlay` and central sniper reticle; `false` keeps ordinary camera presentation.
- Work remaining: manually verify the checkbox in Client Play Mode by selecting a forward profile with `true` and scrolling into it.
- Edited files: `Assets/Game/Scripts/Client/ClientSettings.cs`, `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `Assets/Editor/ClientSettingsEditor.cs`, `Assets/Game/Scenes/Client.unity`, `docs/codex-game/robot-control.md`, `docs/codex-game/ui-hud.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: production and editor C# builds passed with 0 errors; static scene/source assertions passed; the generated font artifact is clean; relevant `git diff --check` passed. Manual Play Mode verification remains pending because the Client Unity Editor is already open and native input injection is unavailable.

## Archived task (forward zoom profile controls sniper mode)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "я налаштовував зум і поняв що треба пофиксити дещо, там є 1 крок камера зум снайпер, але так не повино бути бо в снайперський режим він перейде ще під час кроків у перед, тому цю настройку треба прибрати як окреме поле і додати галочку в елемент масиву типу галочка якщо тру то показується круг прицілювання якщо галочка фолс він не показується, ти зрозумів ідею? так зроби це в коді"
- Objective: remove the separate `cameraZoomSniper` profile and make sniper mode selectable from a forward zoom-array profile with a serialized `showReticle` checkbox.
- Acceptance criteria:
  - `CameraZoomProfile` contains `showReticle`; normal profiles show the reticle and the first forward profile with `showReticle = false` is the sniper profile.
  - `cameraZoomSniper` is removed from runtime settings, editor documentation, scene serialization, cloning, validation, and camera lookup code.
  - Forward scrolling can enter the flagged sniper profile during the forward array; sniper camera anchor, scope UI, reticle visibility, FOV, distance, and return-to-previous-step use that profile.
  - Existing default/backward/forward zoom values remain configurable and projectile dispersion is unchanged.
  - Production/editor builds, focused Unity tests, static migration checks, and relevant `git diff --check` pass.
- Assumptions: the existing separate sniper values are migrated into a final forward-array profile with `showReticle = false`, preserving the current settings while allowing the checkbox to be moved to another forward element later.
- Current work: `CameraZoomProfile.showReticle` now marks the first forward profile with `false` as sniper; the old standalone field is removed from runtime settings, editor docs, scene YAML, cloning, validation, and camera lookup. Existing sniper values were migrated into the final forward profile.
- Work remaining: manually scroll through forward/backward steps and enter/exit the flagged sniper profile in Client Play Mode with native input.
- Edited files: `Assets/Game/Scripts/Client/ClientSettings.cs`, `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `Assets/Editor/ClientSettingsEditor.cs`, `Assets/Game/Scenes/Client.unity`, `docs/codex-game/robot-control.md`, `docs/codex-game/ui-hud.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: stale runtime/editor/scene references are removed; scene migration has exactly one `showReticle: 0` forward profile and six regular profile flags; production/editor C# builds passed with 0 errors; relevant `git diff --check` passed. Focused PlayMode could not start because a Client Unity Editor instance was already open and Unity exited batch mode with licensing/client code 1; no test result XML was produced.

## Archived task (HUD ZoomLevel display)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "в юай хад я додав текст ZoomLevel, тобі треба запрограмувати це щначення, це буде відображення левела зуму 0 це дефоулт, -1 це 1 крок ролика назад, 1 крок вперед. додай щоб можна було чітко бачити рівень зуму"
- Objective: connect the HUD `ZoomLevel` text to the live camera zoom state and display `0` for default, negative values for backward wheel steps, and positive values for forward wheel steps.
- Acceptance criteria:
  - The existing HUD `ZoomLevel` text is wired to a serialized/runtime `TMP_Text` reference without creating components or discovering objects at runtime.
  - The displayed value is `0` at default, `-1`, `-2`, etc. for backward zoom steps, and `1`, `2`, etc. for forward zoom steps; sniper mode has an explicit documented value.
  - The label updates immediately when zoom changes and is initialized correctly when the HUD/camera starts or the controlled vehicle changes.
  - Existing zoom/FOV/distance/reticle behavior is unchanged; builds and focused checks pass.
- Assumptions: the user-authored `ZoomLevel` object is already present in the HUD scene and should be wired through the existing HUD owner rather than recreated.
- Current work: `GameplayGUI` owns the inspector-wired `zoomLevelTexts` array for both crosshair variants, initializes both labels to `0`, and receives the clamped signed step from `CameraController` whenever the profile changes.
- Work remaining: visually confirm the label while scrolling backward/forward and entering sniper mode in Client Play Mode with native input.
- Edited files: `Assets/Game/Scripts/UI/HUD/GameplayGUI.cs`, `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `Assets/Game/Scenes/Client.unity`, `docs/codex-game/ui-hud.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: production/editor C# builds passed with 0 errors; static scene/source checks passed; focused Unity PlayMode filter `Game.Tests.PlayMode.DispersionConeProjectionPlayModeTests` passed 2/2; relevant `git diff --check` passed. Native input injection is unavailable for the final visual label check.

## Archived task (runtime zoom distance not applied)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "помітив не всі параметри працюють в рантаймі, параметри камери працюють а от відстань камери чомусь не працює, зроби щоб працювали всі параметри в рантаймі"
- Objective: make every serialized zoom-profile field, including `cameraDistance`, affect the live camera at runtime for default, forward, backward, and sniper profiles.
- Acceptance criteria:
  - `cameraZoomDefault`, every forward/backward array entry, and `cameraZoomSniper` apply their serialized `cameraDistance`, `fieldOfView`, and reticle fields in runtime camera presentation.
  - Changing `cameraDistance` visibly changes the camera anchor/position after zoom input, during smooth transitions, and after camera collision recovery; no later camera step overwrites it with a hard-coded or legacy distance.
  - FOV, reticle projection, sniper transitions, and camera collision continue to use the active profile without changing projectile dispersion.
  - Production/editor builds, focused Unity tests, static checks, and relevant `git diff --check` pass; any remaining manual Play Mode limitation is recorded explicitly.
- Assumptions: the serialized profiles in `Client.unity` are the runtime source of truth; the reported issue is in the application of `cameraDistance`, not in Inspector serialization. The prior reticle calibration fix remains intact.
- Current work: `CameraController` now refreshes the active profile distance before every camera transform and adds the sniper profile distance to the configured sniper forward offset. The existing collision controller remains the final obstruction clamp.
- Work remaining: manually verify changing default/forward/backward/sniper distances in Client Play Mode with native mouse input.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `docs/codex-game/robot-control.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: runtime source assertions passed; `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors; fresh Unity PlayMode filter `Game.Tests.PlayMode.DispersionConeProjectionPlayModeTests` passed 2/2 after the final sniper alignment patch; relevant `git diff --check` passed. Native input injection is unavailable for the final visual distance check.

## Archived task (reticle over-expansion during movement and turret rotation)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: "запусти сцену клієнт, вибери IA_L2_Scout. почину рухатися і подивися як сильно збільшується приціл, також при повороті цей приціл теж дуже розширяється, а якщо йти та повертати башню дуже великий приціл... він реально не відповідає радіусу попадання снаряду, тому треба щоб ти налаштував цей момент"
- Objective: stop the reticle from over-expanding while IA_L2_Scout moves or rotates, and make its visible boundary match the actual projectile dispersion cone at every camera zoom/FOV state.
- Acceptance criteria:
  - The reticle no longer multiplies its base size by `currentDispersion / minimumDispersion`; for IA_L2_Scout this removes the accidental approximately `6 / 0.35 = 17.1x` inflation.
  - The actual camera-projected current dispersion cone is the authoritative reticle size during movement, turret rotation, zoom, FOV smoothing, and non-default aspect ratios.
  - `reticleBaseDiameter` remains only a configurable minimum visual floor and `reticleDiameterMultiplier` remains an optional calibration multiplier; neither may amplify dispersion by a ratio.
  - Shot randomization and projectile dispersion remain unchanged, so a target on the visible boundary stays inside the possible-hit area.
  - Production/editor builds, focused Unity tests, static checks, and `git diff --check` pass; any remaining manual Play Mode limitation is recorded explicitly.
- Assumptions: the previous rewrite's default/forward/backward profile arrays remain the source of camera state; the reported oversized ring is caused by the calibration formula in `NetworkWeaponShooter`, not by the physical cone helper or projectile randomization.
- Current work: Unity Editor was launched with the `Client` scene open and the IA_L2_Scout prefab values were inspected. The reticle formula was traced to an extra current/minimum dispersion ratio; the follow-up now keeps only physical projection plus the configured floor/multiplier.
- Work remaining: reproduce the movement and turret-rotation sequence manually in Play Mode when native input control is available, then confirm the visual ring against a target at its boundary.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/ui-hud.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors; fresh Unity PlayMode filter `Game.Tests.PlayMode.DispersionConeProjectionPlayModeTests` passed 2/2; static search found no `dispersionRatio`, `minimumScreenDiameter`, or old FOV calibration call in `NetworkWeaponShooter`; relevant `git diff --check` passed; Unity Editor is open on the `Client` scene. Native Computer Use cannot inject the requested movement/turret input, so full manual visual reproduction remains pending. Existing unrelated map changes were preserved.

## Archived task (full zoom and dispersion ring rewrite)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- User request: completely remove the old zoom and dispersion-ring implementation, write a clear configurable system from scratch, configure it, and test/iterate until the visual logic is reliable.
- Objective: use one default camera profile plus independent forward/backward step arrays with absolute camera distance, FOV, and ring calibration values; calculate the reticle from the same physical projectile cone through the actual camera projection.
- Acceptance criteria: legacy zoom/ring math is removed; profiles are serialized and documented; local/server rings use the current camera projection and dispersion angle; zoom does not alter projectile spread; builds, static checks, and focused Unity verification pass.
- Current work: replaced legacy zoom state and approximate reticle interpolation with profile-driven camera state and shared physical cone projection. The remaining issue was found in the calibration formula: it multiplied the physical result by a current/minimum dispersion ratio.
- Work remaining: resolve the over-expansion follow-up above and complete manual boundary verification when native Play Mode input is available.
- Edited files: `Assets/Game/Scripts/Client/ClientSettings.cs`, `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`, `Assets/Game/Scripts/Gameplay/Robots/GunDispersion.cs`, `Assets/Game/Scripts/Gameplay/Robots/Shared/DispersionConeProjection.cs`, `Assets/Game/Scripts/Gameplay/Robots/Shared/Game.Gameplay.Shared.asmdef`, `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`, `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`, `Assets/Game/Tests/PlayMode/Game.PlayMode.Tests.asmdef`, `Assets/Game/Tests/PlayMode/DispersionConeProjectionPlayModeTests.cs`, `Assets/Game/Scenes/Client.unity`, `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, registered robot prefabs, affected editor/network/settings helpers, and mechanic documentation.
- Verification: production/editor C# builds passed with 0 errors, focused Unity PlayMode projection tests passed 2/2, static legacy-field checks passed, and `git diff --check` passed. Full-scene manual boundary testing remained pending.

## Archived task (robot stopping inertia soft profile)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- Follow-up request: reduce the previous `NV=50` stopping strength to `10` because it is too strong.
- Objective: use the softer profile NV `10.0` and IA `2.0`, preserving IA's five-to-one neutral stopping time relative to NV and the `2x` reverse-braking ratio.
- Acceptance criteria:
  - NV stopping multiplier is `10.0`.
  - IA stopping multiplier is `2.0`.
  - The IA/NV neutral stopping-time ratio remains `5x`.
  - Holding the opposite direction still uses `directionReversalBrakingMultiplier = 2.0` relative to neutral coasting.
  - Relevant mechanic documentation, handoff/history, static validation, and build checks are updated.
- Assumptions: higher stopping multipliers mean stronger braking and less inertia; only serialized profile values need changing for this follow-up.
- Current work: the previous `NV=50` / `IA=10` profile passed static/build checks but was reported as too abrupt. This follow-up lowers both values by `5x` while preserving their ratio.
- Work remaining: reproduce the softer stopping response in Unity Play Mode, then mark the task completed if the stopping feel matches the intended profile.
- Edited files: `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, `docs/codex-game/ai-bots.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: serialized assertions passed (`NV=10`, `IA=2`, IA/NV neutral stopping time `5x`, reversal multiplier `2x`), production/editor C# builds passed with `0` errors, and `git diff --check` passed. Unity Play Mode/user reproduction remains pending.

## Archived task (fiftyfold robot inertia reduction)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- Follow-up request: reduce robot stopping inertia by `50x` because robots still stop too slowly.
- Objective: increase the faction stopping-strength multipliers so neutral stopping time/distance is approximately `1/50` of the current profile, while preserving the `2x` reverse-braking ratio.
- Acceptance criteria:
  - NV stopping multiplier changes from `1.0` to `50.0`.
  - IA stopping multiplier changes from `0.2` to `10.0`.
  - A neutral stop uses the new faction values and is approximately `50x` shorter than before for the same vehicle and speed.
  - Holding the opposite direction still uses `directionReversalBrakingMultiplier = 2.0` relative to neutral coasting.
  - Relevant mechanic documentation, handoff/history, static validation, and build checks are updated.
- Assumptions: the reported robot uses the shared `VehicleMovementController`; higher stopping multipliers mean stronger braking and less inertia.
- Current work: the previous task added a reversal-only multiplier of `2.0`, but the configured NV `1.0` and IA `0.2` neutral stopping values still produce too much inertia.
- Work remaining: reproduce the new stopping response in Unity Play Mode, then mark the task completed if the reduced inertia is controllable and matches the intended feel.
- Edited files: `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, `docs/codex-game/ai-bots.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: serialized assertions passed (`NV=50`, `IA=10`, both `50x` stronger than the previous profile, IA/NV neutral stopping time `5x`), production/editor C# builds passed with `0` errors, and `git diff --check` passed. Unity Play Mode/user reproduction remains pending.

## Archived task (twofold AI reverse braking distance)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- Follow-up request: make active reverse input stop the robot in half the neutral coasting distance.
- Objective: preserve the existing IA/NV inertia profile and make opposite-direction braking approximately twice as strong as neutral coasting for the same robot and speed.
- Acceptance criteria:
  - AI and player movement use the same server-side reversal braking path.
  - For the same vehicle, faction, class, speed, and surface, active reverse braking produces approximately half the neutral stopping distance.
  - The same two-to-one ratio applies when changing from reverse to forward.
  - Existing IA five-to-one versus NV neutral inertia and the local test-host settings source remain intact.
  - Same-direction speed reduction and final target-speed behavior remain intact unless the reversal-only factor is applied.
  - Relevant movement documentation, handoff/history, static validation, and build checks are updated.
- Assumptions: the reported robot uses the shared `VehicleMovementController`; AI and player input both reach `VehicleInputController.Move` on the authoritative server.
- Current work: the previous task established the shared reversal path, fixed the local test-host settings source, and configured IA stopping `0.2` versus NV `1.0`. This follow-up will add a reversal-only factor without changing neutral coasting.
- Work remaining: reproduce the distance ratio in Unity Play Mode or an actual local-host match, then mark the task completed if a neutral `10 m` stop is approximately `5 m` while holding reverse.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs`, `Assets/Game/Scripts/Server/ServerSettings.cs`, `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, `docs/codex-game/ai-bots.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: reversal-only setting assertions passed for standard and legged movement (`2x` reversal/coast), IA/NV neutral inertia remains `5x`, production/editor C# builds passed with `0` errors, and `git diff --check` passed. Unity Play Mode/user reproduction remains pending.

## Archived task (AI/NV fivefold inertia and local test settings)
- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-22
- Follow-up request: Test why serialized acceleration/braking settings have no effect, and configure IA/AI movement to have five times more inertia than NV.
- User request (verbatim): "помітив дуже дивний баг, коли робот розганяється АІ а потім я починаю тиснути клавішу руху назад в нього дуже довга інерція зупинки але коли я рожену робота і не тисну клавішу зупинки він зупиняється швидше, це дуже дивний баг тобі треба зрозіміти в чому проблема бо він навпаки якщо я жму клавішу назад має тормозити (зупинятися по інрції швидше чим коли я не тисну)"
- Objective: Make active reverse input brake a robot faster than releasing movement, make the local test host use the serialized server movement settings, and configure IA to have five times more stopping inertia than NV.
- Acceptance criteria:
  - AI and player movement use the same corrected server-side reversal braking path.
  - While moving forward, pressing reverse decelerates faster than releasing movement; the same applies when changing from reverse to forward.
  - Same-direction speed reduction and final reverse-speed behavior remain intact.
  - Client Vehicle Test Mode reads the same `ServerSettings` prefab as the server instead of silently using code defaults.
  - IA and NV stopping response has a five-to-one inertia ratio while shared acceleration/braking parameters remain explicit.
  - Relevant movement documentation, handoff/history, static validation, and build checks are updated.
- Assumptions: The reported robot is using the shared `VehicleMovementController`; AI input and player input both reach `VehicleInputController.Move` on the authoritative server.
- Current work: Confirmed the movement reversal regression and patched `VehicleMovementController`. Follow-up investigation found that local `ClientVehicleTestMode` starts a FishNet host from `Client.unity`, which has no `ServerSettings` instance; only `Server.unity` references the settings prefab, so the test host returns code defaults and ignores Inspector values such as IA `1111`.
- Work remaining: Reproduce the updated IA/NV response in Unity Play Mode or an actual local-host match, then mark the task completed if the stopping response matches the acceptance criteria.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs`, `Assets/Game/Scripts/Testing/ClientVehicleTestMode.cs`, `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`, `Assets/Game/Scenes/Client.unity`, `docs/codex-game/ai-bots.md`, `docs/codex-game/robot-control.md`, `docs/codex-game/server-settings.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Follow-up verification: serialized settings assertions passed (`NV stopping=1`, `IA stopping=0.2`, five-to-one stopping-time ratio; standard reversal strengths `3` vs `0.6`; legged reversal strengths `2.75` vs `0.55`); `Client.unity` references the settings prefab; production/editor C# builds passed with 0 errors; `git diff --check` passed. Play Mode/user reproduction remains pending.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore --nologo` passed with 0 errors; `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` passed with 0 errors; static source/settings assertions passed (`NV standard 10→30`, `IA standard 4→12`, `NV legged 10→27.5`, `IA legged 4→11`); `git diff --check` passed. Play Mode/user reproduction remains pending.

## Archived task (AI robot wheel logic and references)
- Status: `COMPLETED`
- Started: 2026-10-22
- User request (verbatim): "колеса у роботів АІ булим перероблені, твоє завдання налаштувати колайдера і ріджід боді кінематік як для інших мешів, налаштувати ссилки на всіх компонентах та класах де вони позлітали, а саме головне налаштувати правільне обертання цих коліс, тобі треба зрозуміти де ліві а де праві і налаштувати обертання щоб воно було вірним. позиції не повині поломатися твоє завдання пофіксити тільки логіку"
- Objective: Restore the AI robot wheel contracts after the wheel mesh rebuild: configure per-mesh disabled convex colliders and kinematic rigidbodies like the other rendered meshes, repair serialized component references, and correct left/right wheel rotation without changing authored positions.
- Acceptance criteria:
  - Every affected AI robot wheel mesh has the same live/death physics setup as the other non-armor rendered mesh debris.
  - All wheel references in suspension, animation, visibility, outline, death, and root-aware components resolve to the rebuilt wheel hierarchy.
  - Left and right wheel animation directions are physically correct for forward/reverse motion and pivot turns.
  - Wheel transforms/positions and unrelated robot geometry remain unchanged.
  - Relevant mechanic documentation, handoff/history, static validation, and build checks are updated.
- Assumptions: The affected scope is the registered IA/NV AI robot prefabs and their rebuilt wheel mesh objects; existing authored transform positions are authoritative and must not be rewritten.
- Current work: Repaired `Assets/Editor/VehiclePrefabIntegrityEditor.cs` so IA tracked prefab repair rebuilds wheel animator arrays from the current `LeftWhels`/`RightWhels` hierarchy, adds disabled convex `MeshCollider` plus kinematic `Rigidbody` to every non-armor rendered mesh that lacks the debris contract, and rebuilds `DeathLogic.detachableVisuals` without changing transforms. Unity saved all four IA prefabs with the repaired serialized references.
- Work remaining: None.
- Edited files: `Assets/Editor/VehiclePrefabIntegrityEditor.cs`, `Assets/Game/Content/Robots/IA/IA_L1_Starter/IA_L1_Starter.prefab`, `Assets/Game/Content/Robots/IA/IA_L2_Colossus/IA_L2_Colossus.prefab`, `Assets/Game/Content/Robots/IA/IA_L2_Guardian/IA_L2_Guardian.prefab`, `Assets/Game/Content/Robots/IA/IA_L2_Scout/IA_L2_Scout.prefab`, `docs/codex-game/robot-control.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Unity `VehiclePrefabIntegrityBatch.RepairAndValidate` checked 8 registered prefabs, repaired 8 bindings across 4 IA prefabs, and passed post-repair validation. Static inspection found no null death renderers, all IA wheel arrays resolve (`4/4`, `4/4`, `4/4`, `6/6`), all left speeds are `+900`, all right speeds are `-900`, every rebuilt `rusted_wheel` instance has two added physics components, and the diff contains no changed `m_LocalPosition`, `m_LocalRotation`, or `m_LocalScale` lines. The validator resolves each wheel from its actual `LeftWhels`/`RightWhels` ancestor, proving the side assignment used by the rotation signs. Production and editor C# builds passed with 0 errors; `git diff --check` passed.

## Archived task (center rusted_wheel FBX pivot)
- Status: `COMPLETED`
- Started: 2026-10-22
- User request (verbatim): "D:\Programing\UnityProjects\war-of-machines-unity\Assets\Game\Content\Robots\IA\Shared\rusted_wheel тут лежить фбікс, тобі треба півот цього меша зробити в центрі і замінити 3д модель на виправлену, все інше має залишитися як було"
- Objective: Center the pivot of the FBX mesh in the named Unity asset folder, replace that FBX in place, and preserve the visible geometry, mesh identity, material/texture links, Unity importer metadata, and sibling assets.
- Acceptance criteria:
  - The single mesh object's pivot is moved to the center of its geometry bounds.
  - The mesh remains in the same world-space position and shape after the pivot change.
  - The FBX keeps the same object and material names, mesh topology, UV layer, and texture/material linkage.
  - The original filename and Unity `.meta` GUID/import settings are preserved; sibling files are unchanged.
  - Reimported output is inspected and compared against the original for geometry bounds, topology, UVs, and material links.
- Assumptions: "Center" means the mesh geometry's local bounding-box center. The target folder was untracked in Git before this task; replace only the user-named FBX.
- Initial inspection: one mesh `tripo_node_4b2a365e-6ef1-4f2c-a84f-4681b04b7228`, 2,382 vertices, 7,140 edges, 4,760 polygons, one UV layer, one material; local bounds center `(0, 0.5, 0)` and object origin `(0, 0, 0)`.
- Current work: Recentered the mesh origin to the geometry bounds center, compensated vertex coordinates to preserve the world-space mesh, exported over the original FBX, and kept the existing `.meta` and sibling assets unchanged.
- Work remaining: None.
- Edited files: `Assets/Game/Content/Robots/IA/Shared/rusted_wheel/rusted_wheel.fbx`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Blender 5.1.2 reimport comparison passed: same mesh/object/material names, 2,382 vertices, 7,140 edges, 4,760 polygons, UV data and world bounds; maximum world-vertex delta was `0.0`. The local geometry bounds center is now `(0, 0, 0)` and the object origin is at the prior geometry center `(0, 0, 0.5)` in Blender coordinates. The FBX `.meta` GUID/importer settings, JPG, JPG `.meta`, MAT and MAT `.meta` hashes were unchanged during replacement; the folder listing is unchanged. Original FBX backup remains in the task `work/` directory.

## Archived task (per-part robot impact projector isolation)
- Status: `COMPLETED`
- Started: 2026-10-21
- User request (verbatim): "а можеш ще таким чамим чином розділити меші робота, щоб при попадані по башні це невпливало на пушку якщо пушка попадає в область проджектайлу? але так щоб не поломати нічого"
- Objective: Isolate robot impact projectors to the visual mesh part that was hit, so a turret impact does not project onto the gun when the gun overlaps the projector volume.
- Acceptance criteria:
  - A robot impact layer is applied only to the resolved visual mesh renderer(s) for the hit part when that mapping exists.
  - Existing robot/world rendering-layer isolation, projectile attachment, damage, pooling, and fallback behavior remain intact.
  - Multiple simultaneous impacts on different robot parts do not share one whole-robot layer.
  - Add deterministic PlayMode coverage for turret-versus-gun isolation when practical, update mechanic documentation, and verify with build/tests.
- Assumptions: Existing `DeathLogic.detachableVisuals` entries are the authoritative mesh-collider-to-renderer pairs for robot presentation; armor-only or unmapped hits retain the safe whole-robot fallback.
- Current work: `DeathLogic` now registers each detachable mesh collider with its matching renderer. `VehicleColliderRegistry` resolves and returns the visual part hit by the impact, and `PooledImpactFx` assigns a unique temporary rendering layer only to that renderer. Unmapped armor hits retain the whole-robot fallback.
- Work remaining: None.
- Edited files: `Assets/Game/Scripts/Gameplay/Robots/DeathLogic.cs`, `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`, `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`, `Assets/Game/Tests/PlayMode/PooledImpactFxVisualSurfacePlayModeTests.cs`, `docs/codex-game/robot-control.md`, `docs/codex-game/weapons-damage.md`, `docs/codex-game/task-handoff.md`, and `docs/codex-game/task-history.md`.
- Verification: Unity PlayMode regression suite passed `5/5`, including `TurretImpactLayerDoesNotAffectGunRenderer`, which verified separate simultaneous turret/gun layers and pool restoration. `dotnet build Assembly-CSharp.csproj` and `dotnet build Game.PlayMode.Tests.csproj` passed with `0` errors. `git diff --check` passed. The PlayMode run emitted the existing convex-mesh polygon-limit warning for high-poly robot meshes, but all tests passed.

## Archived task (robots invisible before first shot)
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
