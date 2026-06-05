# Server Settings

Read this file before adding/changing server-wide gameplay settings, bot settings, movement settings, projectile settings, diagnostics settings, or prefab-backed configuration.

Current owner files:
- `Assets/Game/Scripts/Server/ServerSettings.cs`
  - Main settings MonoBehaviour and nested settings classes.
  - Provides static getters such as bot wander/combat, movement, dispersion, projectile ballistics, and other runtime settings.
  - Validates values in `Validate` methods.
- `Assets/Game/Prefabs/ServerSettings.prefab`
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

Important settings classes currently used by recent gameplay work:
- `BotWanderSettings`
  - Bot movement cadence, waypoint arrival, pivot turning, stuck handling, dynamic avoidance.
- `BotCombatSettings`
  - Bot target scan, acquire distance, line of sight, aim, fire gate, no-target turret aim.
- `RobotMovementGlobalSettings`
  - Movement fallback speed/acceleration, braking, gravity, grounded snap.
- `GunDispersionGlobalSettings`
  - Accuracy/dispersion conversion, UI/network smoothing, and global on/off behavior for the current speed/turret/shot dispersion model.
  - Runtime spread itself is not configured with old reference traverse speeds anymore: `GunDispersionModel` derives it from normalized vehicle speed, normalized turret-local yaw speed, and fixed post-shot spread.
  - Weapon prefabs keep the per-weapon range through `minDispersionDeg`, `maxDispersionDeg`, and `aimTime`; the server settings prefab no longer serializes hull/gun/camera reference traverse fields.
- `ProjectileBallisticsSettings`
  - Projectile gravity/ballistic settings.

Rules when editing settings:
- Add fields to the correct nested settings class.
- Add validation in that class `Validate` method.
- Add copy support in `CopyFrom` when the settings class has one.
- Check `Assets/Game/Prefabs/ServerSettings.prefab` for serialized values.
- If the settings affect a documented mechanic, update that mechanic doc too.
- Add a Ukrainian explanation with a practical example to `Assets/Editor/ServerSettingsEditor.cs` for every new serialized `ServerSettings` field, including nested settings.
- Keep runtime settings lightweight and avoid per-frame allocations.

Current bot settings added recently:
- `BotWanderSettings.turnInPlaceEnterAngle`
- `BotWanderSettings.turnInPlaceExitAngle`
- `BotWanderSettings.waypointApproachSlowDistance`
- `BotWanderSettings.waypointPassDistance`
- `BotWanderSettings.waypointPassedAngle`
- `BotCombatSettings.aimAlongTravelDirectionWhenNoTarget`
- `BotCombatSettings.noTargetTravelAimDistance`
- `BotCombatSettings.noTargetTravelDirectionMaxAgeSeconds`
- `BotCombatSettings.aimForwardWhenNoTargetIdle`
