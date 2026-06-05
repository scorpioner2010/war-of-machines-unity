# UI HUD

Read this file before changing gameplay HUD, crosshair, reload/ammo display, map/player list HUD, pause HUD, or menu UI wiring.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHUD.cs`
  - Robot-prefab world HP/nickname HUD root and root-aware binding.
  - Owns rotating the world HP bar toward the gameplay camera and scaling it by camera distance.
  - Subscribes to `VehicleHealth.OnHealthChanged` and `OnDamaged`, so the HP fill is initialized on spawn and updates on later health changes.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHudInitializer.cs`
  - Initializes HUD for a vehicle owner.
- `Assets/Game/Scripts/UI/HUD/DamageScreen.cs`
  - Fullscreen local hit indicator in `Assets/Game/Prefabs/UI/GameplayHUD.prefab`.
  - Subscribes to `VehicleRoot.LocalPlayerVehicleChanged`, then to the local vehicle `VehicleHealth.OnDamaged`.
  - Flashes a red fullscreen `Image` through a serialized `CanvasGroup`.
  - On damage, enables the image immediately at canvas group alpha 1, fades alpha to 0 over 1 second, then disables the image object. It is visual only and does not affect gameplay.
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
- `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`
  - VehicleTest bootstrap that instantiates `Assets/Game/Prefabs/UI/GameplayHUD.prefab` under the scene Canvas.
  - Registers and opens the HUD through `MenuManager` when present; otherwise opens the HUD `Menu` directly because VehicleTest has no scene `MenuManager`.
  - Hides the instantiated `GameplayHUD` while the centered expanded VehicleTest panel is open, then reopens it when the panel is collapsed or vehicle control resumes.
  - Rebinds `GunCrosshair` canvas references after instantiation.
  - When VehicleTest spawns the test player or a bot, it directly binds the prefab's `VehicleHUD` to the test camera, vehicle root, and nickname so the standard in-world HP bar is visible and scales/rotates correctly.
- `Assets/Game/Scripts/UI/HUD/GameplayMapHud.cs`
  - Gameplay map HUD.
- `Assets/Game/Scripts/UI/HUD/GameplayMapVisibilityState.cs`
  - Map visibility state.
- `Assets/Game/Scripts/UI/HUD/GameplayPlayerListHud.cs`
  - Player list HUD for ally/enemy rows under the `GameplayHUD` prefab containers.
  - Auto-tracks active non-menu `VehicleRoot` instances, binds each row to `VehicleHealth`, and refreshes names, vehicle type, HP, death state, and team relation.
  - Clears instantiated row items when the HUD is disabled or the local player vehicle becomes null. Its vehicle scan removes every row not seen in the current active-vehicle pass, including rows whose Unity `VehicleRoot` was destroyed during battle scene unload, so stale ally/enemy rows do not persist into the next battle.
- `Assets/Game/Scripts/UI/HUD/GameplayTimerDisplay.cs`
  - Match timer display.
- `Assets/Game/Scripts/UI/HUD/PauseMenu.cs`
  - Pause menu behavior.
  - Suppresses normal gameplay pause handling whenever the `VehicleTest` scene is loaded, not only when it is the active scene, so additive test maps do not open centered pause UI or hide `GameplayHUD`.
- `Assets/Game/Scripts/MenuController/MenuManager.cs`
  - Global menu state; also affects gameplay input blocking.
- `Assets/Game/Scripts/MenuController/Menu.cs`
  - Menu animation controller used by HUD/pause/settings menus.
  - Provides `CloseImmediate()` for VehicleTest to hide `GameplayHUD` instantly without a DOTween close animation fighting the test overlay visibility.

Rules when editing HUD/UI:
- Keep gameplay input blocking behavior in sync with `VehicleInputController.IsGameplayInputBlockedByUi`.
- Do not let client-only UI state drive authoritative gameplay.
- Prefer inspector-wired references for UI components.
- For local hit feedback, use `DamageScreen` and `VehicleHealth.OnDamaged`; do not poll health or search for the local vehicle from the scene.
- VehicleTest uses the same `GameplayHUD.prefab` as the main gameplay UI through `VehicleTestSceneController.clientGameplayHudPrefab`; keep this scene field assigned when changing HUD prefabs.
- If weapon HUD changes, update `weapons-damage.md` too.
- If menu input blocking changes, update `robot-control.md` too.
