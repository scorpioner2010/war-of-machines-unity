# Codex Task Handoff

This is the durable state for one active task. A new Codex chat must read this file and inspect the recorded working-tree changes before acting on a request to continue.

## Active task

- Status: `IMPLEMENTED_NEEDS_VERIFICATION`
- Started: 2026-10-18
- Last updated: 2026-10-18
- Original user wording: "окей реалізуй це"
- Objective: replace the raster `SniperScopeMask` scope overlay with a procedural, resolution-independent UI mask.

### Acceptance criteria

1. `SniperScopeMask` no longer samples `Hud_Aim_Bracket.png`.
2. A black fullscreen overlay has a precisely circular transparent aperture at the screen centre at every aspect ratio and display resolution.
3. The aperture has only pixel-scale antialiasing, not texture-scaling blur.
4. Existing sniper-mode activation and `SniperScopeOverlay` CanvasGroup fade continue to work.
5. The HUD documentation describes the new shader, material, scene owner, and tuning properties.

### Assumptions

- Retain the current black overlay opacity (`0.72`) and centre the aperture unless visual testing identifies a required aim-point offset.
- Use an inspector-wired UI material and shader; do not add runtime component discovery or gameplay code.
- The existing raster scope PNG becomes obsolete only if no other asset references remain after the scene migration.

### Work recorded

- Orientation complete: read `README.md`, `ui-hud.md`, `robot-control.md`, `weapons-damage.md`, and `server-settings.md`.
- Inspection found that `Client.unity/SniperScopeMask` is a standard `Image` using `Hud_Aim_Bracket.png` and a `CanvasGroup` fade through `SniperScopeOverlay`.
- The source texture is `1024x1024`, has mipmaps and compression enabled, and inherits the default 512-pixel import cap, while its scene `RectTransform` is stretched to `8080x8863` UI units.
- Added `Assets/Game/Content/Rendering/Shaders/ProceduralSniperScopeMask.shader`: a UI-compatible signed-distance-field shader that corrects for display aspect ratio and retains one-pixel derivative antialiasing without sampling a texture.
- Added `Assets/Game/Content/UI/Materials/ProceduralSniperScopeMask.mat`: the aperture radius is `0.373` of display height, matching the old 102-pixel source opening after its old UI scale; centre is `(0.5, 0.5)` and extra softness is disabled.
- `Client.unity/SniperScopeMask` now uses that material, has no sprite, stretches directly to `SniperScopeOverlay`, and does not receive raycasts. The black image tint and serialized `CanvasGroup` remain unchanged, preserving the existing fade behavior.
- Removed the obsolete `Assets/Game/Content/UI/Textures/HUD/Hud_Aim_Bracket.png` and its `.meta`; project-wide reference search found no remaining scene/content reference to its GUID.
- Updated `ui-hud.md` with the scene hierarchy, shader/material ownership, tuning properties, and CanvasGroup dependency.

### Verification recorded

- `rg` confirmed that no scene/content asset still references the removed `Hud_Aim_Bracket` GUID and that scene, material, and shader GUID links form one complete chain.
- Confirmed Unity UGUI's `Image.OnPopulateMesh` calls its base full-rectangle quad generator when no sprite is assigned, so the sprite-less `Image` still submits the procedural mask geometry.
- Unity 6 reimported the shader, material, and `Client.unity`; `Editor.log` shows the shader compiler started and contains no `ProceduralSniperScopeMask`, shader-compilation, or C# compilation error.
- The Client scene entered and exited Play Mode without a shader error. It remained at the authorization/connection screen, so no spawned player vehicle was available to trigger sniper mode.
- `git diff --check`: passed; only Git's line-ending warnings were emitted.
- `Assets/Game/Content/Fonts/RobotoCondensed-Regular SDF.asset` is currently modified but is outside this scope-mask task; its change was not reviewed or intentionally edited here.

### Remaining work

1. Join or create a test match with a sniper-capable vehicle, enter and exit sniper zoom, and verify the round aperture is centred, sharp at the desired size, and fades with the rest of `SniperScopeOverlay`.
2. Tune `ProceduralSniperScopeMask.mat` only if needed: `Aperture Radius (Screen Height)` sets circle radius; `Aperture Center (UV)` moves it; keep `Extra Edge Softness (Pixels)` at zero unless a deliberately softer edge is desired.

## Completed task history

### Main-menu robot-card scrolling and preview stability — closed by user without Play Mode verification

- Previous status: `IMPLEMENTED_NEEDS_VERIFICATION`.
- User instructed that this task be closed on 2026-10-18 before beginning the procedural scope-mask task.
- Recorded implementation: `Client.unity` added a horizontal robot-card `ScrollRect`; `RobotView.cs` retains matching cards and horizontal position; `VehicleSlotView.cs` supports in-place updates; `menus-settings-progression.md` documents it.
- Recorded automated checks: `git diff --check` passed (only line-ending warnings); the last 400 lines of `Editor.log` contained no matching compiler errors or exceptions.
- Not verified: the four required Play Mode criteria and the unrelated large diff in `RobotoCondensed-Regular SDF.asset`.
