# Waypoint Graph

Read this file before changing waypoint generation, graph building, pathfinding, or bot path traversal.

Current owner scripts:
- `Assets/Game/Scenes/WaypointPointSpawner.cs`
  - Editor-focused generator for waypoint points and connections in a map scene.
  - Samples points inside a contour, checks ground with raycasts, checks clearance with overlap/sphere casts, and builds point connections.
  - Provides generated points and `WaypointConnection` data to runtime graph code.
- `Assets/Game/Scripts/AI/WaypointGraph/WaypointGraphRuntime.cs`
  - Runtime graph component registered per scene handle.
  - Reads points/connections from a serialized `WaypointPointSpawner` reference.
  - Builds node positions and bidirectional edge lists.
  - Provides nearest-node lookup, random node selection, neighbor access, and node position access.
- `Assets/Game/Scripts/AI/WaypointGraph/WaypointAStarPathfinder.cs`
  - Finds paths through `WaypointGraphRuntime` nodes.
- `Assets/Game/Scripts/AI/WaypointGraph/WaypointGraphEdge.cs`
  - Small edge value object with destination node and cost.
- `Assets/Game/Scripts/AI/WaypointGraph/BotNavigator.cs`
  - Consumes runtime graph and pathfinder for bot movement.

Generation vs runtime:
- `WaypointPointSpawner` is the map/scene authoring side.
- `WaypointGraphRuntime` is the runtime graph side.
- Bot behavior such as combat, target detection, turret aiming, or shooting does not belong in `WaypointPointSpawner`.
- If a task references `WaypointPointSpawner.cs` but talks about runtime bot behavior, inspect `BotNavigator` and `BotCombatController` first.

Runtime graph flow:
1. Scene contains a `WaypointGraphRuntime` with a serialized `WaypointPointSpawner` source.
2. `WaypointGraphRuntime.Awake` registers the graph by scene handle and builds it when `buildOnAwake` is true.
3. `VehicleBotBrain.StartBrain` calls `WaypointGraphRuntime.FindOrCreateForScene(root.gameObject.scene)`.
4. `BotNavigator` uses the graph and `WaypointAStarPathfinder` to choose and follow a path.
5. For explicit transform or position targets, the graph path routes the bot to the nearest graph node first; after the path is exhausted, `BotNavigator` keeps the explicit target active and drives the final segment directly to the requested target position instead of switching back to random wander.

Important constraints:
- Runtime graph lookup uses a static dictionary by scene handle. Do not replace it with scene-wide searches.
- Required graph/source references should be wired in the scene.
- If graph is missing, bot navigator has fallback wander behavior.
- Do not put combat or perception rules into waypoint generator/editor code.

When changing this mechanic:
- Update this file if point generation, connection rules, runtime graph ownership, or bot path traversal changes.
- Update `ai-bots.md` if bot movement behavior changes.
- Update map/prefab documentation if serialized scene fields are added.
