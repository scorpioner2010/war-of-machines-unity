# Asset Organization

`Assets/Game/Content` contains every non-code project asset used by the active game. Unity GUIDs are preserved when assets move, so serialized scene, prefab, and ScriptableObject references must be changed through the Unity `AssetDatabase` rather than Explorer moves.

Content layout:
- `Audio`, `Combat`, `Environment`, `Networking`, `Rendering`, `Server`, `Shared`, and `UI` group reusable assets by runtime role.
- `Combat/Projectiles` is the single project-owned configuration folder for active shot assets: projectile prefabs, muzzle/impact FX prefabs, projectile and tracer materials, impact shader graphs, and their textures. `ImpactRobot` and `ImpactOtherObject` contain the independent robot-hit and world/non-robot impact prefabs with their own project-owned materials and textures; `CFX3_Fire_Shoot` remains the active muzzle-flash prefab.
- `Configuration/Robots/RobotRegistry.asset` is the vehicle registry used by `GameResourceManager`.
- `Configuration/Networking/PrefabObjects.asset` is the FishNet spawnable-prefab collection assigned to `Networking/Prefabs/NetworkManager.prefab`.
- `Fonts/RobotoCondensed-Regular SDF.asset` is the dynamic TextMeshPro asset for Roboto Condensed Regular. It references `Fonts/static/RobotoCondensed-Regular.ttf`, which must remain in the project so the font can add glyphs at runtime.
- `Robots/IA/<vehicle-code>` and `Robots/NV/<vehicle-code>` keep each registered robot prefab beside its model, material, and texture files. Common parts are in the adjacent faction `Shared` folder.
- `UI/Backgrounds` contains 18 active UI backgrounds and decorations, including the `Bar` subfolder, `InfoPanel`, and `line`.
- `UI/ButtonBackgrounds` contains the seven active backgrounds verified on serialized Button objects or their children: `close_button_bg`, `login_button_blank`, `medium_tank_node_bg`, `nav_button_a_bg_v2`, `nav_button_gear_bg`, `popup_login_button_background`, and `tab_garage_active`.
- `UI/ButtonIcons` contains only the six active icons verified on serialized Button objects or their children: `close_x`, `emblem_a`, `mail`, `settings_gear`, `starS`, and `tree_sidebar_gear`.
- `UI/Icons` contains 13 active non-button UI icons, including the `png` tier-icon subfolder.
- `Unused` contains project-owned assets not found in the recursive dependencies of `Client.unity`, `Server.unity`, or `Map.unity`. It is an archive, not a runtime load location.
- `Unused/Gameplay/Projectiles` contains the legacy explosion textures removed from the active projectile configuration after the dependency audit: `Expl.png`, `Expl2.png`, `Expl2_EmissionMask.png`, and `ExplEmission.png`.
- `Unused/Backgrounds` contains archived UI textures with no serialized or explicit path reference in the project; its subfolder layout mirrors `UI/Backgrounds` where applicable.

Usage rules:
1. Put a new robot prefab and its exclusive visual assets in that robot code folder.
2. Put a part shared by multiple IA or NV robots into that faction's `Shared` folder.
3. Keep map-specific assets in `Environment` and UI assets in `UI`; do not recreate the previous top-level `Images`, `Models`, `Materials`, or `Prefabs` folders.
4. Before archiving an asset, check recursive dependencies of the three main scenes and any explicit runtime loading path. `VehicleTest.unity` remains a supported test scene under `Assets/Game/Scenes` and is not an unused resource.
5. Keep third-party Cartoon FX materials, meshes, scripts, and source textures in `JMO Assets`; only project-owned shot prefabs and textures belong in `Combat/Projectiles`.
6. Do not move third-party package/vendor folders into `Content`; they retain their package layouts and licences.
