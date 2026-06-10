# API Resources And Vehicle Data

Read this file before changing backend API calls, profile loading, vehicle registry, vehicle resource lookup, or menu data fed by API models.

Current owner scripts:
- `Assets/Game/Scripts/API/HttpLink.cs`
  - Shared HTTP/API link configuration.
- `Assets/Game/Scripts/API/Endpoints/*.cs`
  - Endpoint managers for leaderboard, maps, matches, players, register, user vehicles, vehicles.
- `Assets/Game/Scripts/API/ServerManagers/ProfileServer.cs`
  - Profile loading/update flow used by menu and player data.
- `Assets/Game/Scripts/API/ServerManagers/RegisterServer.cs`
  - Registration/auth support.
- `Assets/Game/Scripts/API/Models/*.cs`
  - DTO/model classes, including `PlayerProfile` and token/auth data.
- `Assets/Game/Scripts/Core/Resources/GameResourceManager.cs`
  - Static access point for vehicle prefab/icon lookup.
  - Uses a scene/prefab assigned `RobotRegistry` instance.
- `Assets/Game/Scripts/ScriptableObjects/RobotRegistry.cs`
  - ScriptableObject list of vehicle codes, prefabs, and icons.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleStatsProvider.cs`
  - Loads runtime vehicle stats for spawned vehicles.

Vehicle resource flow:
1. `GameResourceManager` is present in the scene and has a `RobotRegistry` reference.
2. Vehicle code is selected from player profile, room player state, or bot/default code.
3. `MatchVehicleSpawner` asks `GameResourceManager.GetPrefab(vehicleCode)`.
4. Spawned vehicle receives runtime stats from `VehicleStatsProvider.GetAsync`.
5. UI can ask `GameResourceManager.GetIcon(code)`.

Vehicle armor API contract:
- `VehicleLite.turretArmor` and `VehicleLite.hullArmor` are slash-separated `front/side/rear` millimeter values.
- `VehicleRuntimeStats` parses those strings into `TurretArmor` and `HullArmor`.
- `VehicleArmorController` uses the values for the collider's configured turret/hull array. A missing, malformed, zero, or negative directional value becomes `1000 mm`.

Rules when editing API/resource code:
- Keep API DTO changes synchronized with backend expectations.
- Do not use runtime scene searches for vehicle prefabs; use `GameResourceManager` and `RobotRegistry`.
- If vehicle prefab contracts change, update `robot-control.md`.
- If spawn vehicle selection changes, update `match-lobby-spawn.md`.
