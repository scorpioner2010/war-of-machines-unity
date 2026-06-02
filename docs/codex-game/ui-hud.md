# UI HUD

Read this file before changing gameplay HUD, crosshair, reload/ammo display, map/player list HUD, pause HUD, or menu UI wiring.

Current owner scripts:
- `Assets/Game/Scripts/UI/HUD/VehicleHUD.cs`
  - Vehicle-specific HUD root and root-aware binding.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHudInitializer.cs`
  - Initializes HUD for a vehicle owner.
- `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`
  - Gun crosshair UI, reload/ammo text, dispersion display target for weapon systems.
- `Assets/Game/Scripts/Gameplay/Robots/WeaponReticlePresenter.cs`
  - Positions/presents reticle based on weapon aim.
- `Assets/Game/Scripts/Gameplay/Robots/WeaponReloadController.cs`
  - Updates reload/ammo HUD state for owner.
- `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`
  - Applies crosshair dispersion to owner HUD.
- `Assets/Game/Scripts/UI/HUD/GameplayGUI.cs`
  - Gameplay GUI root.
- `Assets/Game/Scripts/UI/HUD/GameplayHudRuntimeBinder.cs`
  - Runtime HUD binding support.
- `Assets/Game/Scripts/UI/HUD/GameplayMapHud.cs`
  - Gameplay map HUD.
- `Assets/Game/Scripts/UI/HUD/GameplayMapVisibilityState.cs`
  - Map visibility state.
- `Assets/Game/Scripts/UI/HUD/GameplayPlayerListHud.cs`
  - Player list HUD.
- `Assets/Game/Scripts/UI/HUD/GameplayTimerDisplay.cs`
  - Match timer display.
- `Assets/Game/Scripts/UI/HUD/PauseMenu.cs`
  - Pause menu behavior.
- `Assets/Game/Scripts/MenuController/MenuManager.cs`
  - Global menu state; also affects gameplay input blocking.

Rules when editing HUD/UI:
- Keep gameplay input blocking behavior in sync with `VehicleInputController.IsGameplayInputBlockedByUi`.
- Do not let client-only UI state drive authoritative gameplay.
- Prefer inspector-wired references for UI components.
- If weapon HUD changes, update `weapons-damage.md` too.
- If menu input blocking changes, update `robot-control.md` too.
