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
- `Assets/Game/Scripts/Gameplay/Robots/DeathLogic.cs`
  - Disables configured gameplay/armor colliders on death and releases configured visual debris.
  - `detachableVisuals` separates each visual debris root, debris collider, rigidbody, and renderer from the colliders that receive gameplay hits.
  - Optional `behavioursToDisable` entries stop visual animation before a detached rigidbody becomes dynamic.
  - The legacy parallel debris arrays remain as a fallback for prefabs that have not moved to `detachableVisuals`.
  - Detached parts are registered with `MapScopedObjectRegistry` and remain in the client map scene until that map is unloaded; death logic has no timed debris cleanup.
- `Assets/Game/Scripts/Gameplay/Robots/ArmorMap.cs`
  - Armor zone/collider data and line-of-sight armor sampling.
  - Armor zones are explicit: `Turret = 0` is the default inspector value and `Hull = 1`. There is no automatic name-based zone detection.
  - Base armor comes only from API runtime stats: `turretArmor` or `hullArmor` in `front/side/rear` order. Armor textures and inspector min/max thickness values are not used.
  - A missing, malformed, zero, or negative directional armor value resolves to `1000 mm`; an armor zone never falls back to values from the other zone.
  - Its collider is a hidden serialized cache maintained from the collider on the same GameObject by editor `Reset`/`OnValidate`; it is not a manual inspector reference.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderReference.cs`
  - Registers its local collider and optional local `ArmorMap` with `VehicleColliderRegistry`.
  - Both same-object dependencies are hidden caches maintained by editor `Reset`/`OnValidate`. Runtime initialization only receives the external `VehicleRoot` and validates that the local collider cache exists.
- `Assets/Game/Scripts/Editor/ArmorPrefabHighlighter.cs`
  - In Prefab Mode, draws the red armor overlay only for objects whose `ArmorMap` has cached the collider on that same object.
  - The Unity `Armor` layer alone does not make an object eligible for the overlay, so visual or debris meshes left on that layer are not presented as gameplay armor.
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

T2 armor and destruction prefab contract:
- `Assets/Game/Prefabs/T2.prefab` registers only colliders below objects named `Armor` as armor damage surfaces.
- The six hull colliders use `ArmorMap.ArmorZone.Hull`; the six turret colliders and one gun collider use `ArmorMap.ArmorZone.Turret`.
- `CabineReal`, `WeaponReal`, and `MeshReal` are functional debris parents with inspector-wired disabled debris `BoxCollider` and kinematic `Rigidbody` components. Their direct `VisualMesh` child owns only the rendered mesh.
- All 22 `WheelA1_*`/`WheelA2_*` parent pivots use the same debris pattern: the parent owns `WheelSpinAnimator`, a disabled `BoxCollider`, and a kinematic `Rigidbody`; the direct `a1`/`a2` child owns only the mesh components.
- On death, all 13 armor colliders are disabled, then 25 configured visual parents detach, move recursively to the `Chassis` layer, enable their debris colliders, and become non-kinematic. Wheel spin behaviours are disabled before physics starts.
- `CaterpillarTrackLeft` and `CaterpillarTrackRight` remain visual-only and are deactivated on death instead of detaching.
- `VehicleClientVisibility` releases the death renderers from future spotting changes while preserving whether the vehicle was visible at the death moment. Visible debris stays visible until map unload; already-hidden enemies do not reveal debris.

T1 Hunter destruction prefab contract:
- `Assets/Game/Prefabs/T1Hunter.prefab` currently uses eight armor damage surfaces. The three cubes below `Body` use `ArmorMap.ArmorZone.Turret`; the central hull cube and four leg-section cubes below `Chassis/ChassisMain` use `ArmorMap.ArmorZone.Hull`.
- Their local position, rotation, and scale are authored independently. Adding or duplicating an armor cube automatically refreshes its own `ArmorMap`/collider links in the editor, but the prefab-owned `VehicleRoot` and `DeathLogic` arrays must still include the new component and collider.
- The old nine mesh-based armor colliders and old mesh-part `VehicleColliderReference` components are removed. On death, `collidersToDisableOnDeath` disables all eight current armor cubes.
- The legacy debris arrays keep colliders and rigidbodies on the existing functional parent objects while `debrisRenderers` points to their direct `VisualMesh` children.
- All 15 legacy debris colliders remain disabled while the robot is alive. The lower-right leg parent now has its own disabled `BoxCollider` and kinematic `Rigidbody`; the other debris keeps its existing convex mesh colliders.
- Death detaches and simulates each functional parent, so every visual body/gun/turret/leg part can remain in the map scene and local mesh corrections on `VisualMesh` stay intact.

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
