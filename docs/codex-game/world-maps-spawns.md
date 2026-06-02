# World Maps And Spawns

Read this file before changing map metadata, spawn points, map-scoped registries, additive map offsets, or world placement behavior.

Current owner scripts:
- `Assets/Game/Scripts/World/Maps/MapInfo.cs`
  - Map metadata.
- `Assets/Game/Scripts/World/Maps/MapScopedObjectRegistry.cs`
  - Registers map-scoped scene objects when additive scenes are loaded.
- `Assets/Game/Scripts/World/Spawns/SpawnPoint.cs`
  - Static active spawn point registry.
  - Selects a free spawn point in a specific scene and optional team.
  - Temporarily reserves selected spawn points with a networked `IsNotFree` flag.
  - Team ownership is inferred from hierarchy names containing `TeamA` or `TeamB`.
- `Assets/Game/Scripts/World/Spawns/AutomaticPositionSpawnpoints.cs`
  - Spawn point setup/automation support.
- `Assets/Game/Scripts/Networking/Lobby/MatchSceneOffsetService.cs`
  - Applies map scene offsets for concurrent matches.
- `Assets/Game/Scripts/Networking/Lobby/MatchSceneSlotAllocator.cs`
  - Allocates scene slots/offsets.

Spawn point flow:
1. Spawn points register into a static active list on enable.
2. `MatchVehicleSpawner` requests `SpawnPoint.GetFreePoint(additiveServerScene, player.team)`.
3. Spawn point checks same scene, free flag, and team preference.
4. Chosen point is reserved temporarily to avoid immediate reuse.
5. Vehicle is instantiated at the chosen position/rotation.

Rules when editing maps/spawns:
- Keep spawn selection scene-scoped; do not choose points from another additive match scene.
- Preserve team preference and fallback behavior unless intentionally changing match rules.
- If spawn behavior changes, update `match-lobby-spawn.md`.
- If map object registration changes, update this file and any affected visibility docs.
