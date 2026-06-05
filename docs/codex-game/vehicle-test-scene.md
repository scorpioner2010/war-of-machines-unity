# Vehicle Test Scene

Read this file before changing `VehicleTest` scene bootstrapping, test vehicle spawning, test runtime overrides, or the IMGUI vehicle test panel.

Current owner scripts:
- `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`
  - Starts the local FishNet host for the test scene.
  - Sets the local test host `TimeManager` tick rate from `localTestTickRate` before starting FishNet; default is 120 Hz so host-only movement does not visibly step at editor frame rates.
  - Loads API vehicle stats and lets the user spawn the selected vehicle.
  - Instantiates `Assets/Game/Prefabs/UI/GameplayHUD.prefab` under the scene Canvas and manages its visibility for the test scene.
  - Loads the configured gameplay map scene additively when `loadGameplaySceneForSpawns` is enabled.
  - Loads the additive gameplay scene through FishNet with `AutomaticallyUnload = false` so test shutdown does not auto-unload the map from an inactive/destroying `NetworkManager`.
  - Waits for the local owner connection to be authenticated, to have loaded start scenes, and to be present in the additive gameplay scene through FishNet's real client-loaded acknowledgement before spawning the selected vehicle.
  - Uses `MatchVehicleSpawner` for map spawn points when spawning into the gameplay scene.
  - Draws the IMGUI test panel as a centered collapsible panel with `Vehicle`, `Bots`, and `Runtime` tabs.
  - Keeps the VehicleTest IMGUI overlay visible after entering the gameplay map. When vehicle control starts, the panel collapses to the `Open Vehicle Test` button instead of disappearing; pressing Escape restores cursor/test GUI mode so the panel can be expanded.
  - Hides `GameplayHUD` while the expanded VehicleTest panel is open, and reopens it when the panel is collapsed or vehicle control resumes.
  - The `Bots` tab can add a random enemy bot or random ally bot for the spawned test player. Bot vehicle codes are picked from loaded API stats with a valid prefab, then fall back to server default/registry codes.
  - VehicleTest bot buttons create bot `Player` entries in the test `ServerRoom` and call `MatchVehicleSpawner.SpawnBotAsync`, so bots use normal spawn points, runtime stats, teams, `VehicleBotBrain`, and waypoint/fallback navigation.
  - After a VehicleTest player or bot vehicle is spawned, the controller binds that vehicle's prefab-assigned `VehicleHUD` to the test gameplay camera, vehicle root, and nickname so the world HP bar scales/rotates like normal gameplay.
  - For the spawned test player, reasserts the test camera's `CameraSync`, initializes the player's `CameraController`, and sets `CameraSync.target` to that controller so additive gameplay maps cannot leave the camera detached.
  - Makes the VehicleTest `testCamera` the primary gameplay camera by enabling it, raising its camera depth, and disabling other loaded `CameraSync` cameras such as `Map/GameplayCamera`.
- `Assets/Game/Scripts/Testing/VehicleTestRuntimeSettings.cs`
  - Builds test runtime stats for reload/ammo overrides.
  - Applies test-only weapon accuracy debug mode to the spawned vehicle.
  - Creates optional hit marker spheres in `VehicleTest`.
- `Assets/Editor/VehicleTestRuntimeSettingsEditor.cs`
  - Custom Unity inspector for `VehicleTestRuntimeSettings`.
  - Shows a Ukrainian explanation with a practical example as a tooltip when hovering every test parameter.
- `Assets/Editor/DocumentedSettingsInspector.cs`
  - Shared editor-only renderer used by the VehicleTest settings inspector.
  - Provides tooltip documentation without adding description rows below fields.

VehicleTest spawn flow:
1. `VehicleTestSceneController` starts a local FishNet host and waits for a ready local owner connection.
2. The selected API stats are cloned and test overrides are applied.
3. If `loadGameplaySceneForSpawns` is enabled, the controller loads `gameplaySceneName` through FishNet scene loading.
4. The controller waits until the scene is loaded, contains a `SpawnPoint`, and `ownerConnection.Scenes` contains that scene. Do not manually add the owner connection to the additive map before FishNet receives the client-loaded acknowledgement.
5. `MatchVehicleSpawner.SpawnPlayerAsync` spawns the vehicle at a map `SpawnPoint`.
6. Runtime stats are re-applied and synchronized to observers.
7. The test player's `CameraController` is bound back to the test camera, other loaded gameplay cameras are disabled for the test session, the prefab `VehicleHUD` is bound to the test camera/root/nickname, then test cursor mode is disabled so the vehicle can be driven. The IMGUI overlay remains visible as a collapsed button while the cursor is locked.
8. Optional bot buttons require a spawned test player and at least one `SpawnPoint` in the player's current scene. Enemy bots use the opposing assigned team; ally bots use the test player's team. Existing bot entries are preserved when the test player is respawned.
9. VehicleTest-created bots have their `VehicleHUD` configured immediately after spawn with the test camera, bot root, and bot nickname, matching the normal in-world HP bar behavior.
10. The normal `PauseMenu` is suppressed whenever the `VehicleTest` scene is loaded, including after the additive gameplay map becomes the active scene, so pressing Escape only toggles VehicleTest cursor/test UI. Expanded VehicleTest UI hides `GameplayHUD`; collapsed UI/vehicle control shows it again.

Important constraints:
- `WaypointPointSpawner.cs` is waypoint authoring and should not own VehicleTest spawn or movement fixes.
- Keep VehicleTest using the same gameplay vehicle prefabs, `MatchVehicleSpawner`, and `GameplayHUD.prefab` as normal gameplay where practical.
- Do not add gameplay hot-path scene searches to vehicle movement to fix VehicleTest-only issues; fix the test bootstrap instead.
- VehicleTest is a local host, so server-authoritative movement is visible directly on the host object. Keep the test tick rate high enough for smooth local inspection instead of changing shared vehicle movement semantics.
- `Assets/Game/Scenes/VehicleTest.unity` owns the serialized `localTestTickRate` value; keep it at `120` unless the test harness deliberately needs a different local-host simulation rate.
- Add a Ukrainian explanation with a practical example to `Assets/Editor/VehicleTestRuntimeSettingsEditor.cs` whenever `VehicleTestRuntimeSettings` gains a serialized field.
