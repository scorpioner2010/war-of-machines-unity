# Match Lobby Spawn

Read this file before changing matchmaking, rooms, additive scene loading, player/bot population, vehicle spawn, teams, battle end, or match visibility.

Current owner scripts:
- `Assets/Game/Scripts/Networking/Lobby/LobbyManager.cs`
  - High-level lobby/network flow.
- `Assets/Game/Scripts/Networking/Lobby/LobbyRooms.cs`
  - Static room registry and room lookup helpers.
- `Assets/Game/Scripts/Networking/Lobby/ServerRoom.cs`
  - Runtime room state: players, selected map, additive scene handle, scene slot offset, match flags, timer, visibility service.
  - Provides `GetPlayers()` used by bot target acquisition.
- `Assets/Game/Scripts/Networking/Lobby/Player.cs`
  - Per-room player state including connection, name, bot flag, team, active vehicle, and spawned `VehicleRoot`.
- `Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs`
  - Server/client additive scene load flow, scene slot reservation, spawn lifecycle, visibility ticking, disconnect handling.
- `Assets/Game/Scripts/Networking/Lobby/MatchVehicleSpawner.cs`
  - Spawns player and bot vehicles in loaded match scenes.
  - Applies runtime stats, assigns `player.playerRoot`, initializes teams/identity, and starts bot brain.
- `Assets/Game/Scripts/Networking/Lobby/MatchBotPopulationService.cs`
  - Adds bot players to matches according to settings.
- `Assets/Game/Scripts/Networking/Lobby/MatchTeam.cs`
  - Team identity and same-team checks.
- `Assets/Game/Scripts/World/Spawns/SpawnPoint.cs`
  - Spawn point selection by scene/team.
- `Assets/Game/Scripts/World/Spawns/AutomaticPositionSpawnpoints.cs`
  - Spawnpoint setup support.

Spawn flow:
1. Room is created and players/bots are assigned.
2. `GameplaySpawner` loads the selected map scene additively for the room.
3. The room receives a scene slot and scene offset so multiple matches can exist at once.
4. `MatchVehicleSpawner` chooses a free `SpawnPoint` for each player/bot team.
5. Vehicle prefab is resolved by vehicle code through `GameResourceManager`.
6. Runtime stats are loaded through `VehicleStatsProvider` and applied before/after FishNet spawn.
7. Player vehicles are spawned with owner connection; bots are spawned with null owner connection.
8. `VehicleNetworkInitializer.ServerInit` configures player/bot type, name, team, and scene.
9. Bot vehicles start `VehicleBotBrain.StartBrain` after spawn.

Rules when editing match/spawn:
- Keep room state authoritative on server.
- Do not use global scene searches for runtime player/bot lookup; use room/player references.
- Preserve additive scene slot/offset behavior for multiple concurrent matches.
- If bots are affected, update `ai-bots.md`.
- If vehicle prefab contracts change, update `robot-control.md`.
