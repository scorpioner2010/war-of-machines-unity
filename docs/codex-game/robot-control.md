# Robot Control

Read this file before changing vehicle input, movement, turret rotation, gun pitch, aim solving, runtime stats, or vehicle prefab contracts.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/VehicleRoot.cs`
  - Central reference hub for a vehicle prefab.
  - Holds inspector-wired references to input, movement, health, turret, weapon, HUD, bot brain, colliders, armor maps, and configured component lists.
  - Applies `IVehicleRootAware`, `IVehicleInitializable`, and `IVehicleStatsConsumer` to configured components.
  - Tracks `LocalPlayerVehicle` and active vehicles.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleInputController.cs`
  - Handles owner local input and server external input.
  - Implements `IBotInputReceiver` for bot movement.
  - Applies server input to movement, shoot/action state, turret yaw, gun pitch, and desired aim point.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleServerInput.cs`
  - Small struct for server-side movement/combat input.
  - `Movement` carries movement only.
  - `Combat` carries movement plus shoot/action and aim state.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleMovementController.cs`
  - Server-side movement simulation on FishNet ticks.
  - Reads `vehicleRoot.inputManager.Move` and drives a `CharacterController`.
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
- Server authoritative movement reads `VehicleInputController.Move` from `VehicleMovementController`.
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
