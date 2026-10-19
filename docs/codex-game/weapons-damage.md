# Weapons And Damage

Read this file before changing shooting, reload, projectile visuals, prediction, hit resolution, armor, damage, or reticle dispersion.

Current owner scripts:
- `Assets/Game/Scripts/Gameplay/Robots/WeaponReloadController.cs`
  - Owns ammo count, reload timer, reload HUD state, and fire gating.
  - Human owner path starts local prediction, while `ServerTryApproveOwnerShot` atomically validates ownership/reload/ammo, consumes one shell, and starts server reload.
  - Bot/server path uses `ServerTryFireAuthoritative`.
  - For the local owner, it passes `VehicleRuntimeStats.Faction` to `GunCrosshair` and drives Nova Syndicate `CrosshairType2` light-element alpha from the predicted reload timer, reconciled against the authoritative reload remainder when available. Alpha is `0` immediately after the predicted shot and reaches `1` when the weapon is ready.
- `Assets/Game/Scripts/Gameplay/Robots/NetworkWeaponShooter.cs`
  - Owns projectile fire, projectile visual spawning, dispersion, authoritative hit resolution, and network RPCs for shot visuals/results.
  - Owns the single human fire RPC. The server validates finite shot data, limits the client muzzle offset to 8 meters, requests ammo/reload approval, and only then creates the authoritative projectile.
  - Rejected owner shots cancel the predicted projectile and reconcile the local reload timer to the server value.
  - `ServerFireAuthoritative` is used by bots and other server-authoritative fire paths.
  - The owner shot-result `TargetRpc` writes `Miss`, `Penetrated`, or `No penetration` directly to the active `GunCrosshair`; the status remains visible until another shot result replaces it.
- `Assets/Game/Scripts/Gameplay/Robots/ServerHitResolver.cs`
  - Server-side raycast/hit resolution helper.
- `Assets/Game/Scripts/Gameplay/Robots/DamageService.cs`
  - Applies resolved damage to vehicle health.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleHealth.cs`
  - Networked health state and death handling integration.
  - Raises `OnDamaged` on observers after authoritative damage is applied; the local HUD `DamageScreen` uses this event for the red hit indicator.
  - On a confirmed death, disables configured hit colliders immediately, sends the observer death event, and server-despawns the robot after the temporary `5`-second debris hold plus a `1`-second client fade and `0.25`-second safety margin. The client never decides when to despawn the network object.
- `Assets/Game/Scripts/Gameplay/Robots/DeathLogic.cs`
  - Disables configured gameplay/armor colliders on death and releases configured visual debris.
  - Disables the root tracked, suspension, walker, ground-alignment, and gun-recoil presentation controllers before detaching debris, preventing post-death transform writes against deactivated or detached renderers.
  - In Editor play mode only, scans the full multi-selection and clears it when any selected object belongs to the dying vehicle before hierarchy mutation, avoiding Unity SceneView selection-outline renderer assertions. Player builds contain no editor-selection code.
  - Every registered IA/NV robot prefab has one configured detachable visual per non-armor `MeshRenderer` object. Meshes below `Armor` remain editor-only and are excluded from the serialized death list. The list contains only valid mesh-renderer roots and has no null placeholder entries.
  - `detachableVisuals` separates each visual debris root, debris collider, rigidbody, and renderer from the colliders that receive gameplay hits.
  - Fully empty `detachableVisuals` slots are ignored so prefab inspector capacity does not produce runtime errors; a slot with any assigned collider, rigidbody, renderer, mesh collider, or behaviour but no root still logs a configuration error.
  - Optional `behavioursToDisable` entries stop visual animation before a detached rigidbody becomes dynamic.
  - The legacy parallel debris arrays are empty on registered IA/NV robot prefabs; all death debris uses `detachableVisuals`.
  - Detached parts are registered with `MapScopedObjectRegistry`, remain for `5` seconds, fade over the following `1` second, then have their colliders disabled and are destroyed locally. The registry remains the fallback cleanup path for map unload or an external despawn before the timer finishes.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleArmorController.cs`
  - Single root-level armor owner for a vehicle prefab.
  - `turretColliders` and `hullColliders` are the only armor-surface lists. `additionalVehicleColliders` registers non-armor hit surfaces with the vehicle root.
  - Registers every configured collider with `VehicleColliderRegistry`, including its explicit turret/hull zone, and owns line-of-sight armor sampling.
  - Base armor comes only from API runtime stats: `turretArmor` or `hullArmor` in `front/side/rear` order. A missing, malformed, zero, or negative directional value resolves to `1000 mm`.
  - `highlightArmorInPrefab` enables or disables the red Prefab Mode armor overlay for that vehicle.
- `Assets/Game/Scripts/Gameplay/Robots/VehicleColliderRegistry.cs`
  - Static runtime lookup from a configured collider to its vehicle root and optional centralized armor controller/zone.
  - Also indexes registered vehicle roots by FishNet `NetworkObject.ObjectId` so an authoritative impact RPC can attach its visual to the moving robot without scene-wide lookup.
  - Merges the inspector-wired vehicle visibility and armor renderer lists per root so robot decals can target only the hit robot through URP rendering layers.
- `Assets/Game/Scripts/Editor/ArmorPrefabHighlighter.cs`
  - In Prefab Mode, draws the red overlay for active objects referenced by the root `VehicleArmorController` turret/hull arrays; an object disabled with `GameObject.SetActive(false)` is not highlighted.
  - Respects the prefab-owned `highlightArmorInPrefab` checkbox.
- `Assets/Game/Scripts/Gameplay/Robots/GunDispersion.cs`
  - Dispersion settings and runtime dispersion model.
- `Assets/Game/Scripts/Gameplay/Robots/BallisticProjectileMath.cs`
  - Ballistic math helpers.
- `Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`
  - Projectile visual/simulation component.
  - Authoritative projectiles own hit callbacks and damage-resolution timing.
  - Client visual projectiles sweep against the configured hit mask while flying. A local collision immediately spawns cosmetic impact FX, stops the shell/tracer head, and waits for the authoritative result without applying damage.
  - A predicted impact passes `RaycastHit.collider.transform` through the visual path so the impact FX root follows the exact moving mesh/collider object.
  - Client projectiles retain their `shotId` while pooled references are tracked, preventing a late RPC from resolving a reused projectile instance.
  - Authoritative confirmation suppresses a duplicate impact when the predicted and server target object IDs match. If a target ID is unavailable, world impacts within 2 meters are treated as the same impact. A different target or farther point produces a corrective authoritative impact.
- `Assets/Game/Scripts/Gameplay/Projectiles/ProjectileRuntimePool.cs`
  - Runtime pooling for projectiles.
  - Rejects impact and muzzle FX configuration, prewarming, and spawning in a server-only process. Hosts retain client FX because they also run a local client.
  - Keeps impact FX bounded by the configured inactive-pool size: when all pooled impact FX are active, the oldest active instance is reused instead of instantiating an unbounded number of persistent decals.
  - Removes destroyed impact FX from the active ownership list and skips destroyed inactive entries before reuse, preventing a destroyed Unity object from reaching `PrepareForPoolRelease`.
  - Accepts an optional attachment parent. Robot hits use the hit collider transform (or the registered `VehicleRoot` for an RPC fallback); static Map hits remain unparented. The FX is reparented to the pool root on release.
  - Robot hits pass the registered target `VehicleRoot` into the pooled FX. Map hits pass no target and continue using the default projector layer.
- `Assets/Game/Scripts/Gameplay/Projectiles/PooledImpactFx.cs`
  - Impact FX pooling.
  - Applies local impact camera shake through CFXR effects with the gameplay camera, distance falloff, the client camera-shake setting, and a runtime strength scale of `0.3` so pooled impact shake is 70% weaker than prefab-authored CFXR strength.
  - Owns the inspector-wired URP `DecalProjector` on the impact FX prefab. `PlayImpact` positions the decal at the authoritative or predicted `hit.point`, projects along `-hit.normal`, randomizes roll around the surface normal, fades through `DecalProjector.fadeFactor`, and releases it after the configured decal lifetime.
  - Drives the paired `impactGlowDecal` projector with the same transform, size, target-only rendering-layer mask, pool reset, and fade as the dark impact projector. The glow uses the Unity/URP-compatible Point Light decal graph in `ImpactGlowDecal.shadergraph` through `ImpactGlowDecal.mat`; its emissive red falloff is self-lit and requires no runtime light. The crack texture remains on the transparent `ImpactDecal.mat` layer.
  - Robot impacts acquire one shared temporary Decal Layer per target `VehicleRoot`, add that layer to the registered robot renderers, and assign it to the projector. The layer is reference-counted and all renderer masks are restored on pool release or external destruction; Map impacts keep the default layer.
  - Notifies `ProjectileRuntimePool` when an instance is destroyed externally and ignores a destroyed component if a release race reaches `PrepareForPoolRelease`.
- `Assets/Game/Scripts/Gameplay/Robots/ProjectileVisualSpawner.cs`
  - Visual projectile spawning support.

Fire flow:
- Human fire input is owned by `VehicleInputController` and consumed by `WeaponReloadController` on the owner.
- Owner path uses client prediction plus server RPC validation.
- `NetworkWeaponShooter` triggers `VehicleGunRecoilController.PlayRecoil` with the predicted owner shot and with each observer shot RPC, so the firing robot visibly recoils for both the local player and remote clients without waiting for hit resolution.
- The owner sends one fire RPC through `NetworkWeaponShooter`; projectile creation and ammo/reload consumption cannot be approved independently.
- Predicted owner and observer projectiles use client-only collision to prevent visible wall/target overshoot caused by RPC latency.
- Cosmetic impact FX is predicted immediately for the owner and observers. Hit/miss, armor, penetration, damage, HP, kill state, and shot-result HUD remain server-authoritative.
- Authoritative simulation uses `Assets/Game/Content/Combat/Projectiles/ServerProjectile.prefab`, which contains only `Transform` and `Projectile`: no mesh, renderer, trail, particle system, collider, rigidbody, material, or explosion FX reference.
- `NetworkWeaponShooter` prepares the authoritative projectile pool only for the server phase. Client projectile, impact, muzzle-flash, and muzzle-smoke pools are prepared only from client visual paths.
- A matching authoritative hit completes the waiting visual without replaying the same impact. A divergent authoritative hit may add a correction impact. A server miss releases the waiting visual; an already-played cosmetic prediction is not rolled back.
- The authoritative projectile catches up by the elapsed client tick time, capped at 0.30 seconds, so server damage timing stays close to client projectile timing.
  - Server validates reload/ammo in `WeaponReloadController` before `NetworkWeaponShooter` creates the authoritative projectile.
- `Assets/Game/Scripts/UI/HUD/GunCrosshair.cs`
  - Presents the owner weapon state in `Client.unity/Canvas/GameplayHUD/GameplayHudContent`. It selects the matching `AmmoCountText`, `ReloadStatusText`, reload fill `Image`, `AimStatus`, and penetration-status bindings for the active faction; switching bindings forces the current cached ammo/reload state and last penetration result into the new UI target.
  - Uses the serialized `GameplayHudContent` regular-HUD root and `SniperScopeOverlay` scope root when switching between regular and sniper presentation.
- Bot fire uses `WeaponReloadController.ServerTryFireAuthoritative` and then `NetworkWeaponShooter.ServerFireAuthoritative`.
- Impact particles are spawned one Unity unit toward the local gameplay camera, keeping their screen position aligned with the actual hit point even at an oblique shot angle. If no gameplay camera exists, they fall back to the collision-surface normal.
- The existing `Assets/Game/Content/Effects/Prefabs/CFXR Explosion.prefab` has inspector-wired `Impact Decal` and `Impact Glow Decal` children with URP `DecalProjector` components. `ImpactDecal.mat` uses the transparent `Expl2_DarkScorch_Decal.png` texture for the visible scorch and cracks; `ImpactGlowDecal.mat` adds a lightweight self-lit red halo and is paired by `PooledImpactFx`.
- The impact projector uses a `0.4`-unit depth centered on the hit point, with angle fade from `0` to `90` degrees. Centering the volume prevents thin flat Map meshes such as `Concrete_fence_v1_wall_set_v2` from being clipped when the physics hit lies on the projection boundary; its mesh and `BoxCollider` bounds are aligned and do not require a heavier `MeshCollider`.
- The impact FX root may remain camera-facing, but robot hits are parented with world position/rotation/scale preserved to the hit mesh/collider transform so the projector follows hull/turret movement. Static Map hits use the world-space projector with no attachment parent and remain world-static. Pool release always returns the root to the FX pool hierarchy and restores target renderer masks.
- `Assets/Game/Content/Effects/Prefabs/CFXR Explosion.prefab` contains disabled projector children for the dark scorch and self-lit red halo. There is no mesh-sprite fallback or per-impact runtime light for robot hits; a Bloom post-process can optionally add a wider halo. Keep `_LightIntensity` and `_LightColor` on `ImpactGlowDecal.mat` as the glow tuning controls.
- `Assets/Settings/URP-HighFidelity-Renderer.asset`, `URP-Balanced-Renderer.asset`, and `URP-Performant-Renderer.asset` all contain the active URP `DecalRendererFeature` with `decalLayers: 1`; this is required for target-only projector filtering.
- Damage should be applied only through authoritative server hit resolution, not directly from bot AI.
- Local damage feedback is client-side UI only: `VehicleHealth.OnDamaged` drives `DamageScreen` without affecting damage calculation.

T2 armor and destruction prefab contract:
- Registered IA robot prefabs under `Assets/Game/Content/Robots/IA` register only colliders below objects named `Armor` as armor damage surfaces.
- Its single root `VehicleArmorController` contains six hull colliders and seven turret/gun colliders. `ChassisT2` is the one registry-only non-armor collider.
- `CabineReal`, `WeaponReal`, `MeshReal`, and wheel holders are transform/animation parents only. Each non-armor `MeshRenderer` child owns the matching disabled convex `MeshCollider` and kinematic `Rigidbody`, using that object's `MeshFilter.sharedMesh`. Armor mesh objects remain editor-only and keep their armor-controller collider setup.
- On death, all configured mesh-renderer objects detach, move recursively to the `Chassis` layer, enable their mesh colliders, and become non-kinematic. Wheel and body presentation behaviours are disabled before physics starts.
- `CaterpillarTrackLeft` and `CaterpillarTrackRight` remain visual-only and are deactivated on death instead of detaching.
- `VehicleClientVisibility` releases the death renderers from future spotting changes while preserving whether the vehicle was visible at the death moment. Visible debris stays visible until map unload; already-hidden enemies do not reveal debris.

T1 Hunter destruction prefab contract:
- Registered NV robot prefabs under `Assets/Game/Content/Robots/NV` currently use eight armor damage surfaces. The root armor controller assigns the three cubes below `Body` to its turret array and the central hull cube plus four leg-section cubes below `Chassis/ChassisMain` to its hull array.
- The root `VehicleArmorController` contains three turret/gun colliders and five hull/leg colliders. Child armor objects contain only Unity collider/renderer components.
- Their local position, rotation, and scale are authored independently. Adding or duplicating an armor cube requires adding its collider to the matching root armor array and the health collider list.
- On death, `DeathLogic` asks the centralized armor controller to disable all configured armor surfaces.
- The previous legacy debris arrays and parent-level rigidbodies/mesh colliders are removed. Each NV mesh-renderer object is directly assigned as a `DeathLogic.detachableVisuals` root and remains disabled physically until death.
- Death detaches and simulates each rendered mesh object, so every visual body/gun/turret/leg part uses its own mesh-aligned center of mass and collider shape.

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
