# Audio presentation ownership

## Responsibilities

- `Assets/Game/Scripts/Audio/SoundCaller.cs` owns the process-wide audio gateway, menu lifecycle, channel routing, and battle ambience/music lifecycle.
- `Assets/Game/Scripts/Audio/AudioManager.cs` and `AudioPreset.cs` own source pooling, spatial mode, runtime SFX distance/rolloff application, volume application, looping, and playback lifetime. `AudioManager` applies the configured SFX `minDistance`, `maxDistance`, and `AudioRolloffMode` to active and future positional effects.
- `Assets/Game/Scripts/Audio/AudioResources.cs` maps stable event names to the clips serialized on the `AudioManager` prefab. Robot SFX are stored in the editable `soundCategories` list; each category selects a faction and contains `robotGroups`, while each robot group selects `All Robots` or one stable registry robot and owns its nested `clips` list. New factions and robot groups can be added as list elements without adding fields or enums to code. UI and music remain separate resource arrays. Each resource exposes its own volume, delay, and previewable clip entries. Source filenames do not participate in gameplay lookup. Robot-aware lookups prefer an exact robot group, then the faction group, then Common.
- `Assets/Editor/AudioResourcesEditor.cs` owns the editor-only organization of `AudioResources`: each faction category expands into its own robot-group list, group targets are populated from the active `RobotRegistry`, and every resource keeps the inline volume-aware preview button.
- `Assets/Game/Scripts/Audio/AudioEventSelector.cs` owns deterministic robot-family variant selection. It does not own gameplay state.
- Active audio assets are organized under `Assets/Resources/GameAudio/Music`, `UI`, `Robots/General`, `Robots/Walker`, and `Robots/Tracked`; asset `.meta` files preserve stable Unity GUID references during renames and moves.

## Presentation owners

- Menu and progression owners emit UI events: `SettingsView`, `DevelopmentTree`, `RoomController`, `LobbyManager`, `PopupScreen`, `GameResultUI`, and `EndGameUI`.
- `ProfileStatisticsPopup` owns the profile-window music presentation lifecycle by starting `Profile_Music` on open and stopping the music channel on close; audio routing remains in `SoundCaller` and `AudioManager`.
- Combat owners emit positional effects: `NetworkWeaponShooter` emits shot and resolved armor-impact effects; `WeaponReloadController` emits reload/empty-ammo effects; `VehicleTurretRotationController` emits turret movement effects.
- Locomotion owners emit positional movement effects: `TrackedVehicleAnimator` and `WalkerAnimationController` own their movement loops and step triggers.
- Local combat presentation owners emit HUD cues: `VehicleAutoAimController`, `CameraController`, `GunCrosshair`, and the local death callback.

## Boundary rules

- UI and music use the non-positional routes. Robot movement, weapon, armor, turret, and death effects use the positional SFX route with the emitting robot transform.
- `SoundCaller` exposes live runtime SFX spatial settings through `sfxMinDistance`, `sfxMaxDistance`, `sfxRolloffMode`, and `SetRuntimeSfxSpatialSettings(...)`. The active-map defaults are 2–50 metres with logarithmic rolloff: the full-volume zone is slightly larger than the widest robot, while the maximum range is about half the map width and below the opposing spawn separation. Changing these values affects positional robot/world SFX only, while UI and music remain 2D.
- `AudioRobotTargetIds.Common` is the default for shared effects. `AudioRobotTargetIds.IronAllianceAll` and `AudioRobotTargetIds.NovaSyndicateAll` target every active robot in that faction. Specific robot options use the stable `RobotRegistry.Item.code` value, so adding a registry entry does not shift existing assignments; development-only black-tree robots are excluded until they have an active prefab.
- `Cannon_Medium_Shot` and `Cannon_Heavy_Shot` are the active shot events. Their entries are split between the six active non-colossus robot groups and the two active colossus groups, so each active robot gets its assigned shot variant without changing the event names used by gameplay. Common keeps one fallback of each event for a newly added robot that has not received a specific group yet.
- `Walker_Step` and `Walker_Turn` remain faction-wide defaults for Nova Syndicate, while `Heavy_Walker_Step` and `Heavy_Walker_Turn` override them in the `nv_l2_colossus` group. Walker movement, hydraulics, and reload-ready sounds remain in the Nova faction-wide group; tracked movement and reload-ready sounds remain in the Iron Alliance faction-wide group.
- Server-only execution must not create audible playback. Host clients remain eligible because they have an initialized client.
- The disabled enemy-destruction voice is intentionally not selected or called. No code path may use `UI_Enemy_Destroyed`.
- Battle start starts one randomly selected battle-music clip once. Battle end stops the music before result presentation; former ambience tracks remain available as named music resources but are not layered in the battle channel.
