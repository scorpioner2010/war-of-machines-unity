# API Resources And Vehicle Data

Read this file before changing backend API calls, profile loading, vehicle registry, vehicle resource lookup, or menu data fed by API models.

Current owner scripts:
- Backend API source: `D:\Programing\ServerProjects\war-of-machines-API\WarOfMachinesAPI`
  - The vehicle JSON contract is defined by `Controllers/VehiclesController.cs`; its `VehicleDto.TraverseAcceleration` serializes as `traverseAcceleration`.
- `Assets/Game/Scripts/API/HttpLink.cs`
  - Shared HTTP/API link configuration.
  - The default API base for Editor, client builds, and server builds is the local API at `https://localhost:7216`.
  - `WOM_API_BASE`, `API_BASE`, or the `-apiBase`/`--api-base` command-line override can still point a process at another API when explicitly needed.
- `Assets/Game/Scripts/API/Endpoints/*.cs`
  - Endpoint managers for leaderboard, maps, matches, players, register, user vehicles, vehicles.
- `Assets/Game/Scripts/API/ServerManagers/ProfileServer.cs`
  - Profile loading/update flow used by menu and player data.
  - Proxies Free XP conversion through the owning player's server session, then refreshes the profile after a successful backend response.
  - When `ClientMenuTestMode.enableOfflineMenuTest` is enabled, it builds the local owned
    profile from `testVehicles` instead of calling the backend. The local selected vehicle
    is stored by that test component and applied by `RobotView`.
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
4. Match-spawned vehicle prefabs must also be registered in `Assets/Game/Content/Configuration/Networking/PrefabObjects.asset`, the FishNet collection used by `NetworkManager.prefab`.
5. Spawned vehicle receives runtime stats from `VehicleStatsProvider.GetAsync`.
6. UI can ask `GameResourceManager.GetIcon(code)`.

Free XP conversion flow:
1. `FreeXpConversionPopup` sends the chosen owned `vehicleId` and whole XP amount to `ProfileServer.ConvertFreeXp`.
2. `ProfileServer` gets the caller's session token and calls `UserVehiclesManager.ConvertFreeXp`, which POSTs to `/user-vehicles/{vehicleId}/convert-freexp`.
3. The backend remains authoritative for ownership and available Free XP; on success, the client profile is reloaded so both Free XP and vehicle XP displays update.

Vehicle movement API contract:
- `VehicleLite.traverseAcceleration` receives JSON `traverseAcceleration` and is carried by `VehicleRuntimeStats` to `VehicleMovementController`.
- A positive value is the hull yaw acceleration in degrees per second squared. It accelerates/decelerates hull turning only; it does not affect `VehicleTurretRotationController`.
- `VehicleRuntimeStats.ResolveFaction` maps API `factionCode` to `RobotFaction` (`IronAlliance`, `NovaSyndicate`, or `Unknown`) so gameplay systems do not infer a faction from a vehicle-code prefix.

Vehicle armor API contract:
- `VehicleLite.turretArmor` and `VehicleLite.hullArmor` are slash-separated `front/side/rear` millimeter values.
- `VehicleRuntimeStats` parses those strings into `TurretArmor` and `HullArmor`.
- `VehicleArmorController` uses the values for the collider's configured turret/hull array. A missing, malformed, zero, or negative directional value becomes `1000 mm`.

Rules when editing API/resource code:
- Keep API DTO changes synchronized with backend expectations.
- Do not use runtime scene searches for vehicle prefabs; use `GameResourceManager` and `RobotRegistry`.
- If vehicle prefab contracts change, update `robot-control.md`.
- If spawn vehicle selection changes, update `match-lobby-spawn.md`.
