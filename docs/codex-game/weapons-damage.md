# Weapons And Damage

Read this file before changing shooting, reload, projectile visuals, prediction, hit resolution, armor, damage, or reticle dispersion.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/WeaponReloadController.cs`
  - Owns ammo count, reload timer, reload HUD state, and fire gating.
  - Human owner path predicts/request fires through RPC.
  - Bot/server path uses `ServerTryFireAuthoritative`.
- `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`
  - Owns projectile fire, projectile visual spawning, dispersion, authoritative hit resolution, and network RPCs for shot visuals/results.
  - `ServerFireAuthoritative` is used by bots and other server-authoritative fire paths.
- `Assets/Game/Scripts/Gameplay/Robots/ServerHitResolver.cs`
  - Server-side raycast/hit resolution helper.
- `Assets/Game/Scripts/Gameplay/Robots/DamageService.cs`
  - Applies resolved damage to vehicle health.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHealth.cs`
  - Networked health state and death handling integration.
- `Assets/Game/Scripts/Gameplay/Robots/ArmorMap.cs`
  - Armor zone/collider data and line-of-sight armor sampling.
- `Assets/Game/Scripts/Gameplay/Robots/GunDispersion.cs`
  - Dispersion settings and runtime dispersion model.
- `Assets/Game/Scripts/Gameplay/Robots/BallisticProjectileMath.cs`
  - Ballistic math helpers.
- `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`
  - Projectile visual/simulation component.
- `Assets/Game/Scripts/Gameplay/Projectiles/ProjectileRuntimePool.cs`
  - Runtime pooling for projectiles.
- `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`
  - Impact FX pooling.
- `Assets/Game/Scripts/Gameplay/Robots/ProjectileVisualSpawner.cs`
  - Visual projectile spawning support.

Fire flow:
- Human fire input is owned by `VehicleInputController` and consumed by `WeaponReloadController` on the owner.
- Owner path uses client prediction plus server RPC validation.
- Server validates reload/ammo in `WeaponReloadController`.
- Bot fire uses `WeaponReloadController.ServerTryFireAuthoritative` and then `NetworkWeaponShooter.ServerFireAuthoritative`.
- Damage should be applied only through authoritative server hit resolution, not directly from bot AI.

Dispersion and aim:
- `NetworkWeaponShooter` updates server and owner dispersion in `Update`.
- Crosshair/HUD reads owner/server state through existing components.
- Bots currently decide when to press fire based on line of fire, reload state, and aim alignment; shot spread is still applied by weapon systems.

Rules when editing weapons:
- Do not bypass reload/ammo gating.
- Do not apply damage from client-only visuals.
- Do not create projectiles or impact FX with unmanaged runtime allocations if existing pools can be used.
- Preserve FishNet ownership rules for owner RPC fire requests.
- If bot fire behavior changes, update `ai-bots.md` too.
