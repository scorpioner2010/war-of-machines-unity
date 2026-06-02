# Vehicle Test Scene

Read this file before changing `VehicleTest` scene bootstrapping, test vehicle spawning, test runtime overrides, or the IMGUI vehicle test panel.

Current owner scripts:
- `Assets/Game/Scripts/Testing/VehicleTestSceneController.cs`
  - Starts the local FishNet host for the test scene.
  - Sets the local test host `TimeManager` tick rate from `localTestTickRate` before starting FishNet; default is 120 Hz so host-only movement does not visibly step at editor frame rates.
  - Loads API vehicle stats and lets the user spawn the selected vehicle.
  - Instantiates `Assets/Game/Prefabs/UI/GameplayHUD.prefab` under the scene Canvas and opens it for the test scene.
  - Loads the configured gameplay map scene additively when `loadGameplaySceneForSpawns` is enabled.
  - Loads the additive gameplay scene through FishNet with `AutomaticallyUnload = false` so test shutdown does not auto-unload the map from an inactive/destroying `NetworkManager`.
  - Waits for the local owner connection to be authenticated, to have loaded start scenes, and to be present in the additive gameplay scene through FishNet's real client-loaded acknowledgement before spawning the selected vehicle.
  - Uses `MatchVehicleSpawner` for map spawn points when spawning into the gameplay scene.
  - Hides the test IMGUI panel while the spawned vehicle is being controlled; pressing Escape restores cursor/test GUI mode.
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
7. The test GUI/cursor mode is disabled so the vehicle can be driven without the IMGUI panel rendering every frame.

Important constraints:
- `WaypointPointSpawner.cs` is waypoint authoring and should not own VehicleTest spawn or movement fixes.
- Keep VehicleTest using the same gameplay vehicle prefabs, `MatchVehicleSpawner`, and `GameplayHUD.prefab` as normal gameplay where practical.
- Do not add gameplay hot-path scene searches to vehicle movement to fix VehicleTest-only issues; fix the test bootstrap instead.
- VehicleTest is a local host, so server-authoritative movement is visible directly on the host object. Keep the test tick rate high enough for smooth local inspection instead of changing shared vehicle movement semantics.
- `Assets/Game/Scenes/VehicleTest.unity` owns the serialized `localTestTickRate` value; keep it at `120` unless the test harness deliberately needs a different local-host simulation rate.
- Add a Ukrainian explanation with a practical example to `Assets/Editor/VehicleTestRuntimeSettingsEditor.cs` whenever `VehicleTestRuntimeSettings` gains a serialized field.
