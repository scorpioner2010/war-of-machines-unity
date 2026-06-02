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
  - Plain C# server-side combat brain owned by `VehicleBotBrain`.
  - Acquires targets from `ServerRoom.GetPlayers()`.
  - Uses line-of-fire raycasts for visibility/fire validation.
  - Sends aim/shoot input through `VehicleServerInput` and `VehicleInputController.ServerSetExternalInput`.
- `Assets/Game/Scripts/AI/WaypointGraph/IBotInputReceiver.cs`
  - Small interface used by navigator to avoid coupling to a specific input controller class.
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
- Target scan reads `ServerRoom.GetPlayers()`, not the waypoint graph.
- Candidate filters: not null, not self, not dead, enemy team when team data is available, inside acquire distance.
- If `requireLineOfSightToAcquire` is true, acquisition requires `HasLineOfFire` to the candidate aim point.
- Aim points prefer turret bounds, then health colliders/armor maps, then turret transform, then root fallback height.
- Bot aim is solved with `VehicleAimInputSolver.SolveForAimPoint` so turret/gun constraints are respected.
- Bot firing goes through reload and shooter systems, not direct damage calls.
- While target is visible and `holdPositionWithLineOfFire` is true, combat suppresses navigation so the bot fires from distance instead of circling the target.

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
- `BotCombatSettings.maxAcquireDistance`
- `BotCombatSettings.requireLineOfSightToAcquire`
- `BotCombatSettings.holdPositionWithLineOfFire`
- `BotCombatSettings.aimAlongTravelDirectionWhenNoTarget`

Rules when editing bots:
- Do not add runtime `AddComponent`, `GetComponent*`, `FindObject*`, tag searches, or scene-wide searches.
- Prefer server-side logic and player-like input over directly moving transforms or forcing weapon state.
- Keep movement and combat ownership separate: navigator owns driving, combat owns target/aim/fire decisions.
- If bot behavior changes, update this file and `server-settings.md` if settings changed.
