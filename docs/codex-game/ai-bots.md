# AI Bots

Read this file before changing bot movement, bot combat, bot perception, target selection, or bot input behavior.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/VehicleBotBrain.cs`
  - Server-side MonoBehaviour entry point for a bot vehicle.
  - Owns one `BotCombatController` instance.
  - Requires `VehicleRoot` and an inspector-wired `BotNavigator` reference on the vehicle prefab.
  - Starts navigator and combat controller from `StartBrain(root, room)`.
- `Assets/Game/Scripts/AI/WaypointGraph/BotNavigator.cs`
  - Server-side movement/path following controller.
  - Uses `WaypointGraphRuntime` and `WaypointAStarPathfinder` when a graph exists.
  - Falls back to random movement if no graph is available.
  - Sends movement through `IBotInputReceiver.ApplyBotInput`, currently implemented by `VehicleInputController`.
  - Does not create/find required runtime components.
- `Assets/Game/Scripts/AI/WaypointGraph/BotCombatController.cs`
  - Plain C# server-side combat coordinator owned by `VehicleBotBrain`.
  - Runs the high-level combat tick only: settings gate, map-visible target refresh/scan, navigation command, aim/fire command.
  - Delegates target scanning, validation, aim points, line-of-fire raycasts, lead prediction, fire gates, navigation control, input writing, and idle aim to focused helper classes in the same folder.
- `Assets/Game/Scripts/AI/WaypointGraph/BotTargetScanner.cs`
  - Acquires candidates from `ServerRoom.Visibility` through `MatchVisibilityService.FillVisibleEnemiesFor(..., List<MatchVisibleEnemy>)`.
  - Starts/refreshes match visibility for server-side bot queries when needed; it does not send map RPCs to bots.
  - Scores only map-visible enemies. With `requireLineOfSightToAcquire` enabled, enemies with line of fire and an aim solution are preferred first, then lower aim error, then distance. If no clean firing candidate exists, the bot still selects a visible map target and moves toward its map/last-known position.
- `Assets/Game/Scripts/AI/WaypointGraph/BotTargetValidator.cs`
  - Filters invalid targets: null, self, dead, or same assigned team.
- `Assets/Game/Scripts/AI/WaypointGraph/BotAimPointResolver.cs`
  - Resolves target aim points from turret bounds, health colliders, armor maps, turret transform, or fallback height.
- `Assets/Game/Scripts/AI/WaypointGraph/BotLineOfFireChecker.cs`
  - Performs combat line-of-fire raycasts and accepts hits on the expected target while ignoring the shooter's own colliders.
- `Assets/Game/Scripts/AI/WaypointGraph/BotTargetMotionTracker.cs`
  - Tracks target velocity samples and applies projectile lead prediction.
- `Assets/Game/Scripts/AI/WaypointGraph/BotAimController.cs`
  - Solves aim through `VehicleAimInputSolver` and estimates/validates yaw, pitch, and muzzle alignment.
- `Assets/Game/Scripts/AI/WaypointGraph/BotFireDecision.cs`
  - Gates firing by target hold time, reaction delay, reload state, shooter availability, and aim alignment.
- `Assets/Game/Scripts/AI/WaypointGraph/BotCombatNavigationController.cs`
  - Sends combat navigation intent to `BotNavigator`: move toward the visible map/last-known target position or suppress movement while holding a clean line of fire.
- `Assets/Game/Scripts/AI/WaypointGraph/BotCombatInputWriter.cs`
  - Sends combat and movement-only server input through `VehicleInputController.ServerSetExternalInput`.
- `Assets/Game/Scripts/AI/WaypointGraph/BotIdleAimController.cs`
  - Aims the turret along travel direction or forward when no map-visible target is selected.
- `Assets/Game/Scripts/AI/WaypointGraph/BotCombatState.cs`
  - Holds current target, map target position, randomized aim offset, scan/think timers, and fire delay state.
- `Assets/Game/Scripts/AI/WaypointGraph/BotCombatUtility.cs`
  - Shared lightweight helpers for move transform/position, aim origin, shell speed, finite checks, hierarchy checks, and random aim offset.
- `Assets/Game/Scripts/AI/WaypointGraph/IBotInputReceiver.cs`
  - Small interface used by navigator to avoid coupling to a specific input controller class.
- `Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs`
  - Owns logical map visibility and spotted-memory state used by both player map HUD snapshots and bot target acquisition.
  - Provides `MatchVisibleEnemy` results with the position/yaw visible to the bot's team and whether the target is directly spotted this frame.
  - Bot navigation uses map/last-known positions instead of omniscient live transforms.
- `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`
  - Test-only UI can add a random enemy bot or random ally bot into the spawned test player's current scene.
  - It creates bot `Player` entries in the test `ServerRoom` and spawns them through `MatchVehicleSpawner.SpawnBotAsync`; it does not implement movement, combat, or waypoint behavior itself.
- `Assets/Game/Scripts/Server/ServerSettings.cs`
  - Contains `BotWanderSettings` and `BotCombatSettings`.
- `Assets/Game/Prefabs/ServerSettings.prefab`
  - Serialized runtime settings. If a field is added to settings, check this prefab and add serialized values if Unity has not done it yet.

Bot movement behavior:
- `BotNavigator.Initialize` receives `VehicleRoot`, `ServerRoom`, and `WaypointGraphRuntime`.
- If a graph is built, navigator finds a path from nearest node to either an explicit target or a random destination node.
- If no graph exists, navigator uses fallback random wander input.
- Movement input is always sent as player-like input: forward and turn through `VehicleInputController.ApplyBotInput`.
- Navigator publishes a desired travel direction through `TryGetDesiredTravelDirection` so combat/idle aim can point the turret where the bot is driving.
- Movement can be suppressed by combat via `SetMovementSuppressed(true)`. When suppressed, navigator sends zero movement input and clears travel direction.

Current waypoint arrival behavior:
- `waypointReachDistance` accepts a waypoint if the bot enters the reach radius.
- `turnInPlaceEnterAngle` and `turnInPlaceExitAngle` add hysteresis for pivot turning. The bot stops forward movement, rotates in place toward the waypoint, then resumes driving once aligned.
- `waypointApproachSlowDistance` reduces forward input near the waypoint for tighter arrival.
- `waypointPassDistance` and `waypointPassedAngle` let the navigator accept a waypoint if the bot passed it nearby instead of circling forever.
- `HasMovedPastPathSegment` also advances a waypoint if the bot has crossed beyond the segment from the previous waypoint to the current one.

Bot combat behavior:
- Target scan reads map-visible enemies from `ServerRoom.Visibility`, not raw `ServerRoom.GetPlayers()` and not the waypoint graph.
- A bot can target only enemies currently visible in its team's logical map visibility state, including spotted-memory entries while the map still exposes their last-known position.
- Memory-only targets are navigation targets only: the bot may aim at and move toward the last-known map position, but it does not line-of-fire check, lead, or shoot using the hidden vehicle's live transform until the target is directly spotted again.
- Candidate filters: not null, not self, not dead, enemy team when team data is available.
- If several map-visible enemies exist, candidates with clean line of fire and an aim solution are preferred, then candidates requiring less turret/gun correction, then closer candidates. The current target receives a small sticky bonus to avoid excessive target flipping.
- If no candidate has clean line of fire, the bot still selects a map-visible enemy and moves toward the position provided by map visibility.
- `requireLineOfSightToAcquire` now controls line-of-fire priority during target scoring; it no longer prevents the bot from moving toward an obstructed map-visible enemy when no better firing target exists.
- Aim points prefer turret bounds, then health colliders/armor maps, then turret transform, then root fallback height.
- Bot aim is solved with `VehicleAimInputSolver.SolveForAimPoint` so turret/gun constraints are respected.
- Bot firing goes through reload and shooter systems, not direct damage calls.
- While target is visible and `holdPositionWithLineOfFire` is true, combat suppresses navigation so the bot fires from distance instead of circling the target.
- If the current target disappears from logical map visibility, the target is cleared. Target forgetting is now driven by match visibility/spotted-memory settings instead of direct line-of-fire loss alone.
- VehicleTest-created bots use the same combat scan. Enemy test bots are assigned to the opposing team from the test player; ally test bots use the same team and are ignored by same-team target filtering.

No-target turret behavior:
- If no enemy target is selected, `BotCombatController.ApplyNoTargetTravelAim` points the turret along navigator travel direction.
- If there is no fresh navigator direction, it derives direction from current movement input.
- If idle and `aimForwardWhenNoTargetIdle` is true, it aims forward.
- This uses the same server input path as combat aim and never fires.

Important settings:
- `BotWanderSettings.thinkInterval`
- `BotWanderSettings.waypointReachDistance`
- `BotWanderSettings.turnInPlaceEnterAngle`
- `BotWanderSettings.turnInPlaceExitAngle`
- `BotWanderSettings.waypointApproachSlowDistance`
- `BotWanderSettings.waypointPassDistance`
- `BotWanderSettings.waypointPassedAngle`
- `BotWanderSettings.dynamicAvoidanceRadius`
- `BotWanderSettings.dynamicAvoidanceWeight`
- `BotCombatSettings.enabled`
- `BotCombatSettings.targetScanInterval`
- `BotCombatSettings.maxAcquireDistance` (legacy combat cap; target visibility range is now owned by `MatchVisibilityGlobalSettings`)
- `BotCombatSettings.requireLineOfSightToAcquire`
- `BotCombatSettings.holdPositionWithLineOfFire`
- `BotCombatSettings.aimAlongTravelDirectionWhenNoTarget`
- `MatchVisibilityGlobalSettings.enabled`
- `MatchVisibilityGlobalSettings.tickInterval`
- `MatchVisibilityGlobalSettings.guaranteedDetectionRange`
- `MatchVisibilityGlobalSettings.requireLineOfSight`
- `MatchVisibilityGlobalSettings.spottedMemorySeconds`

Rules when editing bots:
- Do not add runtime `AddComponent`, `GetComponent*`, `FindObject*`, tag searches, or scene-wide searches.
- Prefer server-side logic and player-like input over directly moving transforms or forcing weapon state.
- Keep movement and combat ownership separate: navigator owns driving, combat owns target/aim/fire decisions.
- If bot behavior changes, update this file and `server-settings.md` if settings changed.
