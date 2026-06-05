# Robot Control

Read this file before changing vehicle input, movement, turret rotation, gun pitch, aim solving, runtime stats, or vehicle prefab contracts.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/VehicleRoot.cs`
  - Central reference hub for a vehicle prefab.
  - Holds inspector-wired references to input, movement, health, turret, weapon, HUD, bot brain, colliders, armor maps, and configured component lists.
  - Applies `IVehicleRootAware`, `IVehicleInitializable`, and `IVehicleStatsConsumer` to configured components.
  - Tracks `LocalPlayerVehicle` and active vehicles.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHUD.cs`
  - Robot-owned world HP/nickname HUD component referenced by `VehicleRoot.vehicleHUD`.
  - Rotates the world HP bar to face the gameplay camera and applies distance scaling from `GameplayRuntimeSettings`.
  - Refreshes HP fill immediately during root binding/spawn and on `VehicleHealth.OnHealthChanged`/damage events.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleInputController.cs`
  - Handles owner local input and server external input.
  - Owner local input supports World of Tanks-style cruise control as one signed speed level from `-3` to `3`: `R` adds one forward step, `F` adds one reverse step, and each step maps to `1/3` of `Move.y`.
  - Manual `W`/`S`, `Space`, or blocked UI/gameplay input clears cruise control before movement is sent to the server.
  - Implements `IBotInputReceiver` for bot movement.
  - Applies server input to movement, shoot/action state, turret yaw, gun pitch, and desired aim point.
- `Assets/Game/Scripts/Gameplay/Robots/CameraController.cs`
  - Owns local gameplay camera orbit, zoom steps, and sniper camera placement.
  - When entering sniper mode, aligns the camera yaw/pitch from the sniper anchor to the current gun aim point before moving the camera, so Shift/scroll zoom does not retarget the gun lower because of camera-origin parallax.
  - When exiting sniper mode, aligns the camera yaw/pitch from the normal orbit pivot (`rig.position`) to the current gun aim point before restoring the third-person zoom, so the gun does not climb upward when the camera origin changes back.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleServerInput.cs`
  - Small struct for server-side movement/combat input.
  - `Movement` carries movement only.
  - `Combat` carries movement plus shoot/action and aim state.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs`
  - Server-side movement simulation on FishNet ticks.
  - Reads `vehicleRoot.inputManager.Move` and drives a `CharacterController`.
  - Caps reverse movement at 50% of forward max speed for all vehicles, including human input, cruise control, and bot input.
  - Applies runtime stats for speed, acceleration, and traverse speed.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleTurretRotationController.cs`
  - Turret yaw controller.
- `Assets/Game/Scripts/Gameplay/Robots/WeaponAimController.cs`
  - Gun pitch and aim point controller.
  - Resolves camera/gun aim rays and pitch constraints.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleAimInputSolver.cs`
  - Converts an aim point/forward direction into target turret yaw and gun pitch.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleRuntimeStats.cs`
  - Runtime stat values and default stat resolution helpers.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleStatsProvider.cs`
  - Async stat lookup for spawned vehicles.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleNetworkInitializer.cs`
  - Initializes player/bot identity, team, scene, and owner/menu setup.

Input flow:
- Human owner input is read by `VehicleInputController.Update` and sent to server RPCs.
- For human owner movement, `W`/`S` still send full forward/reverse input; cruise control sends fractional `Move.y` values from local `R`/`F` state without changing the server input struct.
- Server authoritative movement reads `VehicleInputController.Move` from `VehicleMovementController`; negative `Move.y` is always simulated with the 50% reverse speed cap.
- Bots use the same input channel through `VehicleInputController.ApplyBotInput` and `ServerSetExternalInput`.
- Combat/idle aim uses `VehicleServerInput.Combat` with `HasAim = true`.
- Movement-only bot input preserves current shoot/action state and does not clear aim when `HasAim = false`.

Aiming flow:
- Aim requests are solved through `VehicleAimInputSolver`.
- `VehicleInputController.ServerSetExternalInput` applies desired aim point/forward, target pitch, and target yaw.
- Turret and gun constraints are respected by the existing controllers.

Prefab/reference rule:
- `VehicleRoot` references should be assigned in prefab/scene inspector.
- Do not add runtime component discovery for required vehicle parts.
- If a new vehicle subsystem is needed, add serialized/configured references and validate missing configuration with clear errors.

When changing this mechanic:
- Update this file if input shape, aim solving, vehicle references, movement authority, or stats consumption changes.
- Update `ai-bots.md` if bot input behavior changes.
- Update `weapons-damage.md` if shoot/reload/fire input changes.
