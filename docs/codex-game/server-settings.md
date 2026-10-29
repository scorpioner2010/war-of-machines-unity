# Server Settings

Read this file before adding/changing server-wide gameplay settings, bot settings, movement settings, projectile settings, diagnostics settings, or prefab-backed configuration.

Current owner files:
- `Assets/Game/Scripts/Server/ServerSettings.cs`
  - Main settings MonoBehaviour and nested settings classes.
  - Provides static getters such as bot wander/combat, movement, dispersion, projectile ballistics, and other runtime settings.
  - Validates values in `Validate` methods.
- `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab`
  - Serialized settings source used in runtime scenes/prefabs.
  - New serialized fields may need explicit YAML/prefab updates if Unity has not serialized them yet.
- `Assets/Game/Scripts/Server/RemoteServerSettings.cs`
  - Remote/server settings support.
- `Assets/Game/Scripts/Server/ServerDebugOverlay.cs`
  - Debug overlay for server-side settings/diagnostics.
- `Assets/Editor/ServerSettingsEditor.cs`
  - Custom Unity inspector for `ServerSettings`.
  - Keeps technical serialized field names visible and shows a Ukrainian explanation with a practical example as a tooltip when hovering every root and nested setting.
- `Assets/Editor/DocumentedSettingsInspector.cs`
  - Shared editor-only renderer used by settings inspectors.
  - Draws nested serialized settings recursively and provides tooltip documentation without adding description rows below fields.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleBodySwayController.cs`
  - IA robot body-sway values are prefab-local presentation settings, not `ServerSettings` values.
  - Tune them on each IA prefab root; the component uses synchronized input plus local ground-ray results only for the local gameplay owner. It does not change server-authoritative movement or network state.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleBodyVerticalSpringController.cs`
  - Body vertical-spring values are prefab-local presentation settings, not `ServerSettings` values.
  - Tune them on each robot prefab's `Body` object; only the local gameplay owner samples its serialized `Chassis` world Y motion, turns sudden vertical-speed changes into local spring impulses, and lets its assigned `PositionSync` apply the result. `maxUpwardOffset` and `maxDownwardOffset` bound positive and negative local Y independently; `endStopRebound` retains a controlled returning speed at either limit. It does not change server-authoritative movement or network state.

Important settings classes currently used by recent gameplay work:
- `BotWanderSettings`
  - Bot movement cadence, waypoint arrival, pivot turning, stuck handling, per-bot exploration, dynamic avoidance, and eight-direction robot proximity sensing.
- `BotCombatSettings`
  - Bot combat scan cadence, line-of-fire target scoring, smoothed iterative lead aim, dispersion-aware fire gate, no-target turret aim, and reaction timing.
  - Target visibility range is now owned by `MatchVisibilityGlobalSettings`; bots select only enemies visible in logical map visibility.
- `RobotMovementGlobalSettings`
  - Movement fallback speed/acceleration, braking, gravity, grounded snap.
  - `nvStoppingAccelerationMultiplier` and `iaStoppingAccelerationMultiplier` control NV and IA deceleration after forward/reverse input is released and are also applied during an actual direction reversal. They are braking-strength multipliers: higher values mean shorter stopping inertia. The current prefab profile is NV `10.0` and IA `2.0`; IA therefore retains five times the NV neutral stopping time, the profile is five times softer than the previous `50.0`/`10.0` values, and both factions are ten times stronger than the original `1.0`/`0.2` profile.
  - `standardAccelerationMultiplier` and `leggedAccelerationMultiplier` scale base acceleration for tracked/standard and walking vehicles. `standardBrakingMultiplier` and `leggedBrakingMultiplier` scale active same-direction slowing. `directionReversalBrakingMultiplier` applies only while input requests the opposite direction; the current `2.0` value makes reversal deceleration twice as strong as neutral coasting, which approximately halves stopping distance. Runtime vehicle acceleration stats override the prefab component acceleration, while these global multipliers still apply.
  - Editor-only `ClientVehicleTestMode` instantiates its inspector-assigned `ServerSettings` prefab before starting the local FishNet host, so Vehicle Test movement uses the same `robotMovement` values as the server scene.
- `GunDispersionGlobalSettings`
  - Accuracy/dispersion conversion, legacy reticle sizing, reticle/network smoothing, and global on/off behavior for the current speed/turret/shot dispersion model.
  - Runtime spread itself is not configured with old reference traverse speeds anymore: `GunDispersionModel` derives it from normalized vehicle speed, normalized turret-local yaw speed, and fixed post-shot spread.
  - Weapon prefabs keep the per-weapon range through `minDispersionDeg`, `maxDispersionDeg`, and `aimTime`; the server settings prefab no longer serializes hull/gun/camera reference traverse fields.
  - `uiMinDiameter` and `uiMaxDiameter` clamp the legacy ring size. `uiFullyAimedPixelsPerDegreeAtMaxZoom`, `uiFullyAimedPixelsPerDegreeAtMaxDistance`, `uiBloomPixelsPerDegreeAtMaxZoom`, and `uiBloomPixelsPerDegreeAtMaxDistance` control the aimed and bloom portions of that size across the normalized zoom scalar.
  - `uiDiameterLerpSpeed` smooths the local/server ring diameter. `uiReticleHorizontalLerpSpeed`, `uiReticleVerticalLerpSpeed`, and their sniper variants smooth only reticle position in `WeaponReticlePresenter`.
- `GameplayRuntimeSettings` (client-only settings embedded in `Assets/Game/Scenes/Client.unity`)
  - Owns the single `cameraZoomProfiles` array. Each entry has an explicit signed `zoomIndex`: `0` is default, positive values are forward wheel steps, and negative values are backward wheel steps. The old standalone `cameraZoomSniper` field is removed; every profile with `showReticle = true` supplies its own camera distance, FOV, and black sniper-scope presentation state. These settings are not server-authoritative; the lowest-index flagged forward profile is the max-zoom endpoint for the legacy reticle scalar, or the highest configured forward index when none is flagged.
  - `runtimeZoomOverrideEnabled` and `runtimeZoomIndex` are client-only debug controls beside the profile array. When enabled in Play Mode, `CameraController` forces the local camera to the matching signed profile index through the same `ApplyZoomStep` path as mouse-wheel zoom; changing the index in the Inspector updates camera distance, FOV, sniper presentation, and HUD zoom state. An index not present in the array is ignored, and disabling the checkbox restores normal wheel control.
  - Every zoom profile also requires `aimingCircleDiameter` in UI pixels; a finite profile value is the local/server dispersion-sprite base diameter at full aim, so `1.01`, `20`, and `400` remain those values when fully aimed without the legacy `uiMaxDiameter` cap. Current dispersion above fully aimed adds the existing bloom contribution on top of the configured base. The HUD clamps values below `1` to `1x1`; the legacy three-argument diameter path still uses `uiMinDiameter`, `uiMaxDiameter`, and its dispersion/zoom formula when no profile override is supplied.
  - `worldHpBarCanvasFadeStartDistance` and `worldHpBarCanvasFullyTransparentDistance` control the shared robot `NameCanvas` alpha in `VehicleHUD`. With the default `5` and `3`, distance `5+` is fully visible, distance `4` is alpha `0.5`, and distance `3-` is fully transparent. The values are client-only, validated so the transparent threshold stays below the fade start, and are evaluated against `CameraSync.gameplayCamera`.
- `GameplayRuntimeSettings.shotResultVisibleTime` was removed because the faction-aware penetration status is now persistent and is replaced only by the next shot result.
- `ProjectileBallisticsSettings`
  - Projectile gravity/ballistic settings.
- `PooledImpactFx.ImpactFxProfile`
  - Robot and non-robot impact presentation profiles are prefab-local client FX settings, not server-authoritative `ServerSettings` values and are configured on the impact prefab's `PooledImpactFx` component.
- `MatchVisibilityGlobalSettings`
  - Logical map spotting for teams, including tick interval, fallback/max view range, guaranteed detection range, line-of-sight mask, and spotted-memory duration.
  - Bot target acquisition uses this visibility state through `MatchVisibilityService`, not an omniscient room player scan.
  - `spottedMemorySeconds` starts at the server-confirmed transition from direct spotting to hidden and keeps the `EnemyLastKnown` last-known-position map entry for that duration; the client hides the live enemy 3D model until direct spotting returns.
  - `guaranteedDetectionRange` bypasses only the normal view-range statistic. It does not bypass terrain or walls while `requireLineOfSight` is enabled.
  - `maxLineOfSightChecksPerTick` limits the server raycast budget. `MatchVisibilityService` walks ordered participant pairs with a circular cursor and reserves part of the budget for already spotted pairs; direct spotter links persist when a pair is not reached in the current tick, so the budget can delay a state change but cannot create a false hidden transition. The current prefab value is `96`.
  - `targetProbeRadius` is the horizontal half-width used for the two side probes around a target. With the current prefab value `0.75`, a partial peek around a corner can be spotted when any probe has clear line of sight; it does not bypass the obstacle between the observer and the target.

Rules when editing settings:
- Add fields to the correct nested settings class.
- Add validation in that class `Validate` method.
- Add copy support in `CopyFrom` when the settings class has one.
- Check `Assets/Game/Content/Server/Prefabs/ServerSettings.prefab` for serialized values.
- If the settings affect a documented mechanic, update that mechanic doc too.
- Add a Ukrainian explanation with a practical example to `Assets/Editor/ServerSettingsEditor.cs` for every new serialized `ServerSettings` field, including nested settings.
- Keep runtime settings lightweight and avoid per-frame allocations.

Current bot settings added recently:
- `ServerSettings.maxPlayersForFindRoom` is both the real-player threshold for an immediate start and the target total match population. The server waits up to `findRoomSeconds` for real players; when the timer expires, `botsEnabled` fills the difference between the real-player count and this target. The old fixed `botsPerMatch` setting was removed.
- `BotWanderSettings.turnInPlaceEnterAngle`
- `BotWanderSettings.turnInPlaceExitAngle`
- `BotWanderSettings.turnDeadZoneAngle` (default `4` degrees)
- `BotWanderSettings.turnInputSmoothing` (default `7`, exponential steering response)
- `BotWanderSettings.waypointApproachSlowDistance`
- `BotWanderSettings.waypointPassDistance`
- `BotWanderSettings.waypointPassedAngle`
- `BotCombatSettings.aimAlongTravelDirectionWhenNoTarget`
- `BotCombatSettings.noTargetTravelAimDistance`
- `BotCombatSettings.noTargetTravelDirectionMaxAgeSeconds`
- `BotCombatSettings.aimForwardWhenNoTargetIdle`
- `BotWanderSettings.exploreUnvisitedNodes`, `explorationVisitedPenalty`
- `BotWanderSettings.proximityScanInterval`, `proximityRayDistance`, `proximityRayHeight`, `proximityRayVerticalSpacing`, `proximityAvoidanceWeight`, `proximityMask`
- `BotCombatSettings.targetVelocitySmoothing`, `leadPredictionIterations`

Current bot combat visibility notes:
- `BotCombatSettings.targetScanInterval` still controls how often combat re-scores visible targets.
- `BotCombatSettings.targetVelocitySampleInterval` and `targetVelocitySmoothing` control the smoothed velocity used for lead; `leadPredictionIterations` refines travel time against the current projectile speed.
- `BotCombatSettings.maxFireDispersionMultiplier` and `maxFireDispersionAddDeg` are real fire gates: the bot may not request an authoritative shot until current server dispersion is within the configured allowance.
- `BotWanderSettings.proximityScanInterval` defaults to `0.5` seconds. Each scan uses eight compass/diagonal directions and three vertical offsets, then steers around live registered robot colliders. The ray height fields are offsets from the movement root, not world-space absolute heights.
- `BotWanderSettings.exploreUnvisitedNodes` makes no-target graph wandering prefer nodes that this individual bot has not visited; the visit memory is not shared between bots or persisted between spawns.
- `BotCombatSettings.requireLineOfSightToAcquire` now makes line-of-fire candidates win target scoring when available; it does not block movement toward an obstructed target that is still visible on the logical map.
- `MatchVisibilityGlobalSettings.maxLineOfSightChecksPerTick` and `targetProbeRadius` control fair spotting throughput and partial-peek detection; increasing the budget improves acquisition latency and loss confirmation latency at the cost of more server raycasts, but it is not required to keep an already confirmed direct source visible.
- `BotCombatSettings.holdPositionWithLineOfFire` suppresses bot navigation while a directly spotted target has a clean line of fire, the current tactic permits firing, and the weapon is ready; this keeps the chassis still while turret/gun aim settles. The suppression is released during authoritative reload, while obstructed or memory-only targets continue moving toward their tactical position.
- `BotCombatSettings.maxAcquireDistance`, `BotCombatSettings.viewRangeMultiplier`, and `BotCombatSettings.lostSightForgetSeconds` are legacy fields from the old direct room-scan acquisition path. Current bot target visibility and forgetting are controlled by `MatchVisibilityGlobalSettings` and spotted-memory state.
