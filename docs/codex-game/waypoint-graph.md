# Waypoint Graph

Read this file before changing waypoint generation, graph building, pathfinding, or bot path traversal.

Current owner scripts:
- `Assets/Game/Scenes/WaypointPointSpawner.cs`
  - Editor-focused generator for waypoint points and connections in a map scene.
  - Samples points inside a contour, checks ground with raycasts, checks clearance with overlap/sphere casts, and builds point connections.
  - Checks every prospective connection with `Physics.CheckCapsule` on `obstacleMask`; this includes overlaps at both waypoint ends, so an edge cannot be created through an obstacle even when a waypoint is too close to it.
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

Connection obstacle requirements:
- The collider's own GameObject must be in the generator's `obstacleMask`; a parent layer alone does not include a collider on a child object.
- `Map111` uses layer `Obstacle` (layer 8) as the mask. `Concrete_fence_v2_S` owns enabled non-trigger `BoxCollider` components on its prefab root and a column child; both collider owners use layer 8 in the prefab, while all 27 map instances override the root to layer 8.
- `Concrete_fence_v2_S_half` and `Concrete_fence_v2_Gate` own their blocking colliders on child objects. Their prefab collider owners and all current `Map111` instance overrides use layer `Obstacle` (8), not only the prefab root.
- The `Map111` static-object audit also requires child collider owners in `Hangar_v2` and `UNIConcrete_wall_v1_W_3` to be on layer `Obstacle` (8). The source prefabs and their scene overrides are configured accordingly.
- Keep `minDistanceFromObstacles` at least `connectionCheckRadius` when possible. Smaller values are safe with the capsule check but can generate isolated waypoints that have no valid connections.
- `Map111` uses `minDistanceFromObstacles = 1` and `connectionCheckRadius = 0.5`; this leaves a half-metre movement clearance without treating every obstacle within two metres of a point as a blocked edge.
- A clear pair is still not guaranteed to be connected: `connectionRadius` limits the distance and `maxConnectionsPerPoint` limits each point's nearest valid links. In `Map111`, those values are 15 and 5 respectively.
