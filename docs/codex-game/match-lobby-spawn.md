# Match Lobby Spawn

Read this file before changing matchmaking, rooms, additive scene loading, player/bot population, vehicle spawn, teams, battle end, or match visibility.

Current owner scripts:
- `Assets/Game/Scripts/Networking/Lobby/LobbyManager.cs`
  - High-level lobby/network flow.
- `Assets/Game/Scripts/Networking/Lobby/LobbyRooms.cs`
  - Static room registry and room lookup helpers.
- `Assets/Game/Scripts/Networking/Lobby/ServerRoom.cs`
  - Runtime room state: players, selected map, additive scene handle, scene slot offset, match flags, timer, visibility service.
  - Provides room state for bot target acquisition through `Visibility`; raw `GetPlayers()` is no longer the bot combat target source.
- `Assets/Game/Scripts/Networking/Lobby/Player.cs`
  - Per-room player state including connection, name, bot flag, team, active vehicle, and spawned `VehicleRoot`.
- `Assets/Game/Scripts/Networking/Lobby/GameplaySpawner.cs`
  - Server/client additive scene load flow, scene slot reservation, spawn lifecycle, visibility ticking, disconnect handling.
  - Ticks `ServerRoom.Visibility` for active matches so player map HUDs and bots share the same logical map spotting state.
  - Sends unreliable visibility snapshots with a monotonic state version, FishNet server tick, and per-entry remaining lifetime. The client uses the tick to subtract packet age before applying a deadline.
- `Assets/Game/Scripts/Networking/Lobby/MatchVisibilityService.cs`
  - Builds team visibility from room participants, view range, line of sight, and spotted memory.
  - Sends snapshots to real players only, but also exposes server-side `MatchVisibleEnemy` results for bot target acquisition and last-known-position navigation.
  - Marks whether a visible enemy is directly spotted or memory-only so bots do not shoot using hidden live transforms.
  - Emits `MapVehicleVisibilityRelation.Enemy` for direct team spotting and `EnemyLastKnown` for spotted-memory entries. Both relations keep the client map marker and 3D vehicle visible; both disappear together when spotted memory expires.
  - Starts `spottedMemorySeconds` once on the confirmed direct-spotted to hidden transition instead of refreshing an approximate expiry on every visible tick.
  - Prioritizes rechecks for targets that were directly spotted on the previous visibility frame. An exhausted raycast budget may reuse the last positive cached result, but does not convert a known visible target into a false hidden result.
  - `guaranteedDetectionRange` bypasses the vehicle view-range limit only. When line of sight is required, terrain and walls still block spotting at close range.
- `Assets/Game/Scripts/Networking/Lobby/MatchVehicleSpawner.cs`
  - Spawns player and bot vehicles in loaded match scenes.
  - Applies runtime stats, assigns `player.playerRoot`, initializes teams/identity, and starts bot brain.
- `Assets/Game/GameResources/PrefabObjects.asset`
  - FishNet spawnable-prefab collection assigned to `Assets/Game/Prefabs/NetworkManager.prefab`.
  - Every vehicle prefab referenced by `RobotRegistry` and spawned in a match must also be present here.
  - Add new prefabs at the end so existing network prefab IDs remain stable.
- `Assets/Game/Scripts/Networking/Lobby/MatchBotPopulationService.cs`
  - Adds bot players to matches according to settings.
- `Assets/Game/Scripts/Networking/Lobby/MatchTeam.cs`
  - Team identity and same-team checks.
- `Assets/Game/Scripts/Networking/Lobby/BattleStatisticsService.cs`
  - Authoritatively records damage and kills in the room `Player` state.
  - After a confirmed enemy kill, updates the attacker's `VehicleNetworkInitializer.Kills` SyncVar so all clients receive the current frag count for the gameplay player list.
- `Assets/Game/Scripts/World/Spawns/SpawnPoint.cs`
  - Spawn point selection by scene/team.
- `Assets/Game/Scripts/World/Spawns/AutomaticPositionSpawnpoints.cs`
  - Spawnpoint setup support.
- `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`
  - Test-only local host spawn harness for `VehicleTest`.
  - Loads the configured gameplay scene through FishNet with automatic unload disabled and waits for the local owner connection to be present in that scene through FishNet's client-loaded acknowledgement before calling `MatchVehicleSpawner`.
  - Maintains a test `ServerRoom` with the local test player plus any VehicleTest-created bots, preserving bot entries when the player vehicle is respawned.
  - The VehicleTest `Bots` tab adds random ally/enemy bot `Player` records and uses `MatchVehicleSpawner.SpawnBotAsync` instead of custom bot instantiation.

Spawn flow:
1. Room is created and players/bots are assigned.
2. `GameplaySpawner` loads the selected map scene additively for the room.
3. The room receives a scene slot and scene offset so multiple matches can exist at once.
4. `MatchVehicleSpawner` chooses a free `SpawnPoint` for each player/bot team.
5. Vehicle prefab is resolved by vehicle code through `GameResourceManager`.
6. FishNet resolves that prefab on clients through `Assets/Game/GameResources/PrefabObjects.asset`.
7. Runtime stats are loaded through `VehicleStatsProvider` and applied before/after FishNet spawn.
8. Player vehicles are spawned with owner connection; bots are spawned with null owner connection.
9. `VehicleNetworkInitializer.ServerInit` configures player/bot type, name, team, and scene.
10. Bot vehicles start `VehicleBotBrain.StartBrain` after spawn.
11. In VehicleTest, bot buttons require a spawned local player and a `SpawnPoint` in that player's current scene. Ally bots receive the test player's team; enemy bots receive the opposing team.
12. On a server-only process, `VehicleRoot.OnStartServer` removes the inspector-configured model visual GameObjects from each spawned vehicle. Functional transforms, armor colliders, movement, aiming, and damage logic remain.

Rules when editing match/spawn:
- Keep room state authoritative on server.
- Do not use global scene searches for runtime player/bot lookup; use room/player references.
- Preserve additive scene slot/offset behavior for multiple concurrent matches.
- Keep direct spotting and spotted memory as separate relations for map-position semantics, but treat both as visible in the client vehicle presenter so the model and map marker disappear on the same snapshot update.
- Keep visibility snapshot versions monotonic within a match. Client state rejects older unreliable packets and expires finite enemy entries locally from the server-provided lifetime.
- In VehicleTest, do not spawn into an additive map scene until the local owner connection is authenticated, start scenes are loaded, the map has a `SpawnPoint`, and `ownerConnection.Scenes` contains the map scene because FishNet acknowledged that the client loaded it. Do not manually add the connection to the additive map to bypass that acknowledgement.
- If bots are affected, update `ai-bots.md`.
- If vehicle prefab contracts change, update `robot-control.md`.
