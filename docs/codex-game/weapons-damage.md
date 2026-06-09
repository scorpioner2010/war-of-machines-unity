# Weapons And Damage

Read this file before changing shooting, reload, projectile visuals, prediction, hit resolution, armor, damage, or reticle dispersion.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/WeaponReloadController.cs`
  - Owns ammo count, reload timer, reload HUD state, and fire gating.
  - Human owner path starts local prediction, while `ServerTryApproveOwnerShot` atomically validates ownership/reload/ammo, consumes one shell, and starts server reload.
  - Bot/server path uses `ServerTryFireAuthoritative`.
- `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`
  - Owns projectile fire, projectile visual spawning, dispersion, authoritative hit resolution, and network RPCs for shot visuals/results.
  - Owns the single human fire RPC. The server validates finite shot data, limits the client muzzle offset to 8 meters, requests ammo/reload approval, and only then creates the authoritative projectile.
  - Rejected owner shots cancel the predicted projectile and reconcile the local reload timer to the server value.
  - `ServerFireAuthoritative` is used by bots and other server-authoritative fire paths.
- `Assets/Game/Scripts/Gameplay/Robots/ServerHitResolver.cs`
  - Server-side raycast/hit resolution helper.
- `Assets/Game/Scripts/Gameplay/Robots/DamageService.cs`
  - Applies resolved damage to vehicle health.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHealth.cs`
  - Networked health state and death handling integration.
  - Raises `OnDamaged` on observers after authoritative damage is applied; the local HUD `DamageScreen` uses this event for the red hit indicator.
- `Assets/Game/Scripts/Gameplay/Robots/ArmorMap.cs`
  - Armor zone/collider data and line-of-sight armor sampling.
- `Assets/Game/Scripts/Gameplay/Robots/GunDispersion.cs`
  - Dispersion settings and runtime dispersion model.
- `Assets/Game/Scripts/Gameplay/Robots/BallisticProjectileMath.cs`
  - Ballistic math helpers.
- `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`
  - Projectile visual/simulation component.
  - Authoritative projectiles own hit callbacks and damage-resolution timing.
  - Client visual projectiles sweep against the configured hit mask while flying. A local collision immediately spawns cosmetic impact FX, stops the shell/tracer head, and waits for the authoritative result without applying damage.
  - Client projectiles retain their `shotId` while pooled references are tracked, preventing a late RPC from resolving a reused projectile instance.
  - Authoritative confirmation suppresses a duplicate impact when the predicted and server target object IDs match. If a target ID is unavailable, world impacts within 2 meters are treated as the same impact. A different target or farther point produces a corrective authoritative impact.
- `Assets/Game/Scripts/Gameplay/Projectiles/ProjectileRuntimePool.cs`
  - Runtime pooling for projectiles.
- `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`
  - Impact FX pooling.
  - Applies local impact camera shake through CFXR effects with the gameplay camera, distance falloff, the client camera-shake setting, and a runtime strength scale of `0.3` so pooled impact shake is 70% weaker than prefab-authored CFXR strength.
- `Assets/Game/Scripts/Gameplay/Robots/ProjectileVisualSpawner.cs`
  - Visual projectile spawning support.

Fire flow:
- Human fire input is owned by `VehicleInputController` and consumed by `WeaponReloadController` on the owner.
- Owner path uses client prediction plus server RPC validation.
- The owner sends one fire RPC through `NetworkWeaponShooter`; projectile creation and ammo/reload consumption cannot be approved independently.
- Predicted owner and observer projectiles use client-only collision to prevent visible wall/target overshoot caused by RPC latency.
- Cosmetic impact FX is predicted immediately for the owner and observers. Hit/miss, armor, penetration, damage, HP, kill state, and shot-result HUD remain server-authoritative.
- A matching authoritative hit completes the waiting visual without replaying the same impact. A divergent authoritative hit may add a correction impact. A server miss releases the waiting visual; an already-played cosmetic prediction is not rolled back.
- The authoritative projectile catches up by the elapsed client tick time, capped at 0.30 seconds, so server damage timing stays close to client projectile timing.
- Server validates reload/ammo in `WeaponReloadController` before `NetworkWeaponShooter` creates the authoritative projectile.
- Bot fire uses `WeaponReloadController.ServerTryFireAuthoritative` and then `NetworkWeaponShooter.ServerFireAuthoritative`.
- Damage should be applied only through authoritative server hit resolution, not directly from bot AI.
- Local damage feedback is client-side UI only: `VehicleHealth.OnDamaged` drives `DamageScreen` without affecting damage calculation.

Dispersion and aim:
- `NetworkWeaponShooter` updates server and owner dispersion in `Update`.
- Crosshair/HUD reads owner/server state through existing components.
- Bots decide when to press fire based on line of fire, reload state, aim alignment, and the current `BotCombatTacticSelector` aim-readiness requirement; shot spread is still applied by weapon systems.
- `GunDispersionModel` now uses three runtime inputs for spread: horizontal vehicle speed, actual turret-local yaw speed, and recent shots.
- Vehicle speed contributes up to 50% of the weapon's `minDispersionDeg` to `maxDispersionDeg` range: 50% max speed = +25% spread, 100% max speed = +50% spread.
- Turret movement contributes proportionally up to 50% spread: actual `VehicleTurretRotationController.CurrentLocalYaw` speed is divided by `VehicleTurretRotationController.rotationSpeed`, with a small deadzone so very slow turret tracking stays at minimum spread.
- A shot adds a temporary +50% spread after the shot ray is built; it affects the next aiming state, not the already-fired projectile.
- The final target factor is `clamp01(vehicleSpeed01 * 0.5 + turretSpeed01 * 0.5 + shotDispersion01)`, then lerped between `minDispersionDeg` and `maxDispersionDeg`.
- Hull rotation, gun pitch motion, and camera aim motion no longer add dispersion.
- Max speed for normalization comes from `VehicleRoot.RuntimeStats.Speed` when available, otherwise `VehicleMovementController.maxSpeed`.

Rules when editing weapons:
- Do not bypass reload/ammo gating.
- Do not apply damage from client-only visuals.
- Do not create projectiles or impact FX with unmanaged runtime allocations if existing pools can be used.
- Preserve FishNet ownership rules for owner RPC fire requests.
- If bot fire behavior changes, update `ai-bots.md` too.
