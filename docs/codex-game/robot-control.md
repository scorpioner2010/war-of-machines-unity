# Robot Control

Read this file before changing vehicle input, movement, turret rotation, gun pitch, aim solving, runtime stats, or vehicle prefab contracts.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/VehicleRoot.cs`
  - Central reference hub for a vehicle prefab.
  - Holds inspector-wired references to input, movement, health, turret, weapon, HUD, client visibility, bot brain, centralized armor, and vehicle-specific animation controllers.
  - Applies `IVehicleRootAware`, `IVehicleInitializable`, and `IVehicleStatsConsumer` to configured components.
  - Tracks `LocalPlayerVehicle` and active vehicles.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHUD.cs`
  - Robot-owned world HP/vehicle-name HUD component referenced by `VehicleRoot.vehicleHUD`.
  - Rotates the world HP bar to face the gameplay camera and applies distance scaling from `GameplayRuntimeSettings`.
  - Refreshes HP fill immediately during root binding/spawn and on `VehicleHealth.OnHealthChanged`/damage events.
  - Consumes runtime vehicle stats to display the robot name, with code and prefab instance name as fallbacks.
  - Uses overlay materials assigned by `Assets/Game/NameCanvas.prefab`, so its name and HP images render through walls.
  - Uses `VehicleClientVisibility` for activation: it disappears with the robot's map marker, but while active its overlay materials render through walls.
  - Suppresses its complete world-space presentation on the local owner's gameplay vehicle, independently of spotting updates and nickname initialization.
  - Suppresses the same world-space presentation for menu-preview vehicles after `VehicleRoot.Init(true)`.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleClientVisibility.cs`
  - Owns client-side rendering visibility for a spawned vehicle without changing network/gameplay activation.
  - On a server-only spawn, destroys the GameObjects that own every inspector-wired `visualRenderers` entry. This removes robot model meshes, wheel meshes, and track renderers from the dedicated server while leaving functional transforms, colliders, armor, and movement components intact.
  - Server visual stripping is skipped for a host process because the local client still needs the model.
  - Treats direct enemy spotting and spotted-memory map presence as visible, so the model disappears exactly when the map marker disappears.
  - Requires every visual `Renderer` to be assigned in `visualRenderers`; hidden enemies are suppressed with `forceRenderingOff`.
  - On vehicle death, freezes the visibility state that was active at that moment and stops processing later spotting updates. Visible debris therefore stays rendered after detaching, while a vehicle that was already hidden does not reveal its death position.
  - `Assets/Game/Prefabs/T1Hunter.prefab` and `Assets/Game/Prefabs/T2.prefab` own the serialized component, `VehicleRoot.clientVisibility` reference, and complete renderer lists.
  - Applies its first client visibility result immediately from `OnStartClient`, preventing an unconfirmed remote vehicle from rendering for an initial frame.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleAutoAimController.cs`
  - Rejects enemies hidden by `VehicleClientVisibility`, including an already locked target after its direct spotting expires.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHoverOutline.cs`
  - Rejects hidden enemies and clears an active outline when the visibility presenter hides its vehicle.
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
  - Carries the server-authored synchronized kill count used by gameplay player-list HUD rows.
  - Publishes a client team-change notification so vehicle visibility refreshes after post-spawn FishNet `Team` SyncVar updates.

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
- Battle vehicle prefabs must assign `VehicleRoot.clientVisibility` and every model renderer in `VehicleClientVisibility.visualRenderers`; missing renderer entries can leak hidden geometry.
- The migrated rigid visual parts in `Assets/Game/Prefabs/T1Hunter.prefab` and `Assets/Game/Prefabs/T2.prefab` separate functional transforms from editable meshes. Functional parent objects keep colliders, rigidbodies, armor, animation, and gameplay components; their direct visual child keeps only `Transform`, `MeshFilter`, and `MeshRenderer`.
- T1 Hunter preserves its existing functional transform names and rig paths. Each of its 15 mesh-bearing objects now has a direct `VisualMesh` child, and visibility, outline, and death renderer references point to those children. Local position, rotation, and scale corrections belong on `VisualMesh`; gameplay movement continues to act on its parent.
- T1 Hunter armor is separate from visuals and debris. It has eight damage surfaces: three turret/gun cubes below `Body` and five hull/leg cubes below `Chassis/ChassisMain`. Child surfaces contain only Unity components; the root `VehicleArmorController` owns the two armor arrays and runtime registration.
- T1 Hunter walking is owned by one root `WalkerAnimationController`. It contains both foot references, ground placement settings, step animation, and body bobbing. One root `VehicleGroundAlignmentController` owns all three ground-aligned transforms.
- Root walker, tracked, suspension, and ground-alignment controllers stop updating when `VehicleHealth.IsDead`; `DeathLogic` also disables them before debris detachment.
- T2 uses `CabineReal`, `WeaponReal`, and `MeshReal` as functional turret, gun, and hull debris parents. Each parent owns a disabled convex debris `MeshCollider` and has a direct `VisualMesh` child that owns the rendered mesh.
- `Assets/Game/Prefabs/T2-RM.prefab` follows the same movement/debris contract: its prefab root stays at local origin, while the `CabineReal`, `WeaponReal`, and `MeshReal` debris colliders remain disabled until `DeathLogic` detaches them. Enabling those layer-0 colliders during normal play makes them overlap the layer-7 `CharacterController`, causing server-side depenetration that FishNet then synchronizes as uncontrolled vehicle movement. Its eight `WheelA1_*` parent transforms keep centered colliders and use local X offsets `0.12833688` on the left and `-0.12833679` on the right with unit scale; mesh-only alignment belongs on their visual children.
- T2 armor is separate from visuals: all 13 damage colliders are children of the three objects named `Armor` (six hull colliders, six turret colliders, and one gun collider). One root `VehicleArmorController` owns those arrays plus the registry-only `ChassisT2` collider. Armor renderers are disabled and are not included in visual visibility/outline lists.
- T1 Hunter and T2 configure their armor renderers in `VehicleArmorController.serverEditorArmorRenderers`. A server-only process running in the Unity Editor enables those renderers after spawn so server armor geometry remains visible for inspection. Player builds do not enable armor rendering.
- `VehicleRoot.OnStartServer` owns the server-only presentation transition: it enables Editor armor visualization, then asks `VehicleClientVisibility` to destroy configured model visual objects.
- The old T2 primary meshes `MeshBody`, `MeshGun`, `MeshR`, the old root armor mesh, and all `WhelMesh`/`WheMesh` wheel instances are removed.
- T2 wheels use one centered functional `WheelA1_01..04` or `WheelA2_01..07` parent directly below `LeftWhels`/`RightWhels`, followed by the nested `a1`/`a2` mesh. The redundant numeric A1 holder level is removed.
- Every wheel parent owns its disabled mesh-fitted `BoxCollider` and kinematic `Rigidbody`. The A1 wheel parent is also the suspension target; one root `TrackedVehicleAnimator` owns left/right wheel arrays, per-wheel rotation speeds, and both track renderers.
- One root `CaterpillarSuspensionController` owns all 74 suspension targets. Raycast settings are shared and each target retains only its required position offset.
- T2 tracks remain visual-only and use the enabled `CaterpillarTrackLeft/Cube` and `CaterpillarTrackRight/Cube` skinned renderers from `t2v2.fbx`. The obsolete hidden `Mesh` physics duplicates are not part of the prefab.
- On T2 death, all 22 wheel parents detach alongside `CabineReal`, `WeaponReal`, and `MeshReal`. The centralized tracked animator stops when vehicle health is dead. The two track roots are visual-only and are deactivated instead of becoming debris.
- Do not add runtime component discovery for required vehicle parts.
- If a new vehicle subsystem is needed, add serialized/configured references and validate missing configuration with clear errors.

Known failure: apparent network movement caused by prefab colliders:
- Recorded case: `T2-RM` moved backward uncontrollably and could be pushed out of the map immediately after entering a match. Menu preview rendering remained correct.
- This can look like FishNet desync or bad network movement because the server-authoritative `VehicleMovementController` uses a `CharacterController`, then FishNet replicates the resulting server transform. FishNet can therefore expose the movement without being its cause.
- In the recorded case, diagnostics reported `0%` packet loss. The actual cause was that the convex debris colliders on `CabineReal`, `WeaponReal`, and `MeshReal` were enabled while the robot was alive and overlapped the layer-7 `CharacterController`. Unity physics depenetration moved the server vehicle, and FishNet synchronized that movement to clients.
- Required `T2-RM` prefab state: root local position `(0, 0, 0)`; the three debris colliders disabled while alive; `WeaponReal` collider assigned to its `DeathLogic.detachableVisuals` entry. `DeathLogic` enables configured debris colliders only after death.
- `RobotRegistry` selects the prefab but does not own this movement behavior. For this symptom, inspect the selected vehicle prefab, `VehicleMovementController`, `CharacterController`, debris colliders, and `DeathLogic` before changing registry or FishNet code.
- If this symptom returns, run the mandatory diagnostics workflow first. When packet loss and network health are normal, inspect for enabled colliders overlapping the root `CharacterController`, a non-zero prefab root transform, and missing `DeathLogic` references. After correction, restart Play Mode, reproduce a match, and run `./game-diag.cmd analyze --last 30`.

When changing this mechanic:
- Update this file if input shape, aim solving, vehicle references, movement authority, or stats consumption changes.
- Update `ai-bots.md` if bot input behavior changes.
- Update `weapons-damage.md` if shoot/reload/fire input changes.
