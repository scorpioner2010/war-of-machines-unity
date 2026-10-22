using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public sealed class PooledImpactFxVisualSurfacePlayModeTests
    {
        private const string RobotPrefabPath =
            "Assets/Game/Content/Robots/IA/IA_L1_Starter/IA_L1_Starter.prefab";
        private const string FencePrefabPath =
            "Assets/Game/Content/Map_v100_v200/Environment/Fences/Concrete_fences/Concrete_fence_v1/Concrete_fence_v1_wall_set_v2.prefab";
        private const string RobotImpactFxPrefabPath =
            "Assets/Game/Content/Combat/Projectiles/ImpactRobot/RobotImpact.prefab";
        private const string OtherObjectImpactFxPrefabPath =
            "Assets/Game/Content/Combat/Projectiles/ImpactOtherObject/OtherObjectImpact.prefab";
        private const string ProjectileVisualPrefabPath =
            "Assets/Game/Content/Combat/Projectiles/ProjectileVisual.prefab";
        private const float RaycastDistance = 100f;
        private const float SurfacePositionTolerance = 0.01f;

        private readonly List<GameObject> _createdObjects = new List<GameObject>(4);

        [UnityTest]
        public IEnumerator SpawnRobotAndShootFromSide_DisablesImpactGlowHaloForRobotProfile()
        {
            Type vehicleRootType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot");
            Type colliderRegistryType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleColliderRegistry");
            Type pooledImpactFxType = FindGameType("PooledImpactFx");

            GameObject robotPrefab = LoadAsset<GameObject>(RobotPrefabPath, "IA_L1_Starter");
            Assert.IsNotNull(robotPrefab, $"Missing robot prefab at {RobotPrefabPath}.");

            GameObject robotObject = UnityEngine.Object.Instantiate(robotPrefab);
            _createdObjects.Add(robotObject);
            robotObject.SetActive(true);
            yield return null;

            MeshCollider[] visualMeshColliders = robotObject.GetComponentsInChildren<MeshCollider>(true);
            ActivateVisualMeshColliderObjects(visualMeshColliders);
            Physics.SyncTransforms();

            Component vehicleRoot = robotObject.GetComponent(vehicleRootType);
            Assert.IsNotNull(vehicleRoot, "The spawned test robot has no VehicleRoot.");
            List<Renderer> targetRenderers = GetImpactRenderers(
                colliderRegistryType,
                vehicleRootType,
                vehicleRoot);
            Assert.IsNotNull(targetRenderers, "The spawned robot did not register its impact renderers.");
            uint[] originalRenderingLayerMasks = new uint[targetRenderers.Count];
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                originalRenderingLayerMasks[i] = targetRenderers[i].renderingLayerMask;
                Assert.AreNotEqual(
                    0u,
                    originalRenderingLayerMasks[i] & 1u,
                    $"Robot renderer {targetRenderers[i].name} lost the default lighting layer before impact FX setup.");
            }

            Assert.IsTrue(
                TryGetVisualBounds(robotObject, out Bounds visualBounds),
                "The spawned robot has no visual renderer bounds.");

            Assert.IsTrue(
                TryFindSideVisualMeshHit(
                    visualMeshColliders,
                    visualBounds,
                    out RaycastHit expectedVisualHit,
                    out Vector3 shotOrigin,
                    out Vector3 shotDirection),
                "The spawned robot has no raycastable visual mesh for the deterministic side shot.");

            GameObject proxyObject = new GameObject("Deterministic Impact Test Proxy Collider");
            _createdObjects.Add(proxyObject);
            proxyObject.transform.SetPositionAndRotation(visualBounds.center, Quaternion.identity);
            BoxCollider proxyCollider = proxyObject.AddComponent<BoxCollider>();
            proxyCollider.size = new Vector3(
                Mathf.Max(visualBounds.size.x + 1f, 1f),
                Mathf.Max(visualBounds.size.y + 1f, 1f),
                Mathf.Max(visualBounds.size.z + 1f, 1f));
            InvokeStatic(
                colliderRegistryType,
                "RegisterVehicle",
                new[] { typeof(Collider), vehicleRootType },
                new object[] { proxyCollider, vehicleRoot });

            Physics.SyncTransforms();
            Ray shotRay = new Ray(shotOrigin, shotDirection);
            Assert.IsTrue(
                Physics.Raycast(
                    shotRay,
                    out RaycastHit physicsHit,
                    RaycastDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore),
                $"The deterministic side shot did not hit the spawned robot. proxyBounds={proxyCollider.bounds}, origin={shotOrigin}, direction={shotDirection}, enabled={proxyCollider.enabled}, active={proxyObject.activeInHierarchy}.");
            Assert.AreSame(
                proxyCollider,
                physicsHit.collider,
                "The test shot did not hit the intentionally oversized proxy collider first.");

            Component hitRoot = TryGetRoot(
                colliderRegistryType,
                vehicleRootType,
                physicsHit.collider);
            Assert.IsNotNull(hitRoot, "The physics hit did not resolve to a registered VehicleRoot.");
            Assert.AreSame(vehicleRoot, hitRoot, "The side shot resolved to the wrong robot root.");

            object[] surfaceArguments =
            {
                vehicleRoot,
                physicsHit.point,
                physicsHit.normal,
                null,
                null
            };
            bool resolved = (bool)InvokeStatic(
                colliderRegistryType,
                "TryGetImpactVisualSurface",
                new[]
                {
                    vehicleRootType,
                    typeof(Vector3),
                    typeof(Vector3),
                    typeof(Vector3).MakeByRefType(),
                    typeof(Vector3).MakeByRefType()
                },
                surfaceArguments);
            Assert.IsTrue(
                resolved,
                "The robot visual surface registry did not resolve a surface behind the proxy collider.");

            Vector3 resolvedVisualPosition = (Vector3)surfaceArguments[3];
            Vector3 resolvedVisualNormal = (Vector3)surfaceArguments[4];
            Bounds resolvedSurfaceBounds = visualBounds;
            resolvedSurfaceBounds.Expand(SurfacePositionTolerance * 10f);
            Assert.IsTrue(
                resolvedSurfaceBounds.Contains(resolvedVisualPosition),
                $"Resolved visual point {resolvedVisualPosition} is outside the robot visual bounds {resolvedSurfaceBounds}.");
            Assert.Greater(
                Vector3.Dot(resolvedVisualNormal.normalized, expectedVisualHit.normal.normalized),
                0f,
                $"Resolved visual normal {resolvedVisualNormal} points away from the shot surface normal {expectedVisualHit.normal}.");

            GameObject impactFxPrefabObject =
                LoadAsset<GameObject>(RobotImpactFxPrefabPath, "Robot Impact");
            Assert.IsNotNull(impactFxPrefabObject, $"Missing impact FX prefab at {RobotImpactFxPrefabPath}.");
            Component impactFxPrefab = impactFxPrefabObject.GetComponent(pooledImpactFxType);
            Assert.IsNotNull(impactFxPrefab, $"Prefab at {RobotImpactFxPrefabPath} has no PooledImpactFx component.");

            GameObject impactFxObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
            _createdObjects.Add(impactFxObject);
            Component impactFx = impactFxObject.GetComponent(pooledImpactFxType);
            InvokeInstance(pooledImpactFxType, impactFx, "Initialize", new[] { typeof(float) }, new object[] { 5f });
            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "PlayImpact",
                new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                new object[] { physicsHit.point, physicsHit.normal, vehicleRoot, physicsHit.collider });
            yield return null;

            object impactProfile = pooledImpactFxType
                .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactFx);
            Type profileType = impactProfile.GetType();
            MeshRenderer haloRenderer = (MeshRenderer)profileType
                .GetField("impactGlowHaloRenderer", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            bool enableImpactGlowHalo = (bool)profileType
                .GetField("enableImpactGlowHalo", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            Component impactDecal = (Component)profileType
                .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            Assert.IsNotNull(haloRenderer, "The robot impact profile has no Impact Glow Halo renderer.");
            Assert.IsFalse(enableImpactGlowHalo, "The robot impact profile unexpectedly enables the Impact Glow Halo.");
            Assert.IsFalse(haloRenderer.enabled, "The robot Impact Glow Halo was enabled after the hit.");
            Assert.IsFalse(haloRenderer.gameObject.activeSelf, "The robot Impact Glow Halo object was activated after the hit.");
            Assert.IsNotNull(impactDecal, "The robot impact profile has no regular impact decal.");
            Assert.IsTrue(impactDecal.gameObject.activeSelf, "Disabling the robot Halo also disabled the regular impact decal.");

            Debug.Log(
                $"Deterministic robot shot: proxy={physicsHit.point}, mesh={expectedVisualHit.point}, "
                + $"resolved={resolvedVisualPosition}, robotHaloEnabled={haloRenderer.enabled}.");

            uint robotImpactRenderingLayerMask = GetUIntProperty(impactDecal, "renderingLayerMask");
            Assert.AreNotEqual(
                1u,
                robotImpactRenderingLayerMask,
                "Robot impact decal did not acquire a target-only rendering layer.");

            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "PrepareForPoolRelease",
                Array.Empty<Type>(),
                Array.Empty<object>());
            Assert.IsFalse(haloRenderer.enabled, "Pool release did not disable the Impact Glow Halo.");
            Assert.AreEqual(
                2u,
                haloRenderer.renderingLayerMask,
                "Pool release did not reset the Impact Glow Halo rendering layer.");
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                Assert.AreEqual(
                    originalRenderingLayerMasks[i],
                    targetRenderers[i].renderingLayerMask,
                    $"Pool release did not restore renderer layer {i}.");
            }

            InvokeStatic(
                colliderRegistryType,
                "Unregister",
                new[] { typeof(Collider), vehicleRootType },
                new object[] { proxyCollider, vehicleRoot });
        }

        [UnityTest]
        public IEnumerator TurretImpactLayerDoesNotAffectGunRenderer()
        {
            Type vehicleRootType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot");
            Type colliderRegistryType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleColliderRegistry");
            Type pooledImpactFxType = FindGameType("PooledImpactFx");
            GameObject impactFxPrefabObject = LoadAsset<GameObject>(RobotImpactFxPrefabPath, "Robot Impact");
            Assert.IsNotNull(impactFxPrefabObject, $"Missing impact FX prefab at {RobotImpactFxPrefabPath}.");

            GameObject robotObject = new GameObject("Per-part impact test robot");
            _createdObjects.Add(robotObject);
            Component vehicleRoot = robotObject.AddComponent(vehicleRootType);

            GameObject turretObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _createdObjects.Add(turretObject);
            turretObject.name = "Turret visual part";
            turretObject.transform.SetParent(robotObject.transform, false);
            BoxCollider turretBoxCollider = turretObject.GetComponent<BoxCollider>();
            turretBoxCollider.enabled = false;
            MeshCollider turretMeshCollider = turretObject.AddComponent<MeshCollider>();
            turretMeshCollider.sharedMesh = turretObject.GetComponent<MeshFilter>().sharedMesh;
            turretMeshCollider.convex = true;
            turretMeshCollider.enabled = false;
            MeshRenderer turretRenderer = turretObject.GetComponent<MeshRenderer>();

            GameObject gunObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _createdObjects.Add(gunObject);
            gunObject.name = "Gun visual part";
            gunObject.transform.SetParent(robotObject.transform, false);
            gunObject.transform.localPosition = Vector3.right * 3f;
            BoxCollider gunBoxCollider = gunObject.GetComponent<BoxCollider>();
            gunBoxCollider.enabled = false;
            MeshCollider gunMeshCollider = gunObject.AddComponent<MeshCollider>();
            gunMeshCollider.sharedMesh = gunObject.GetComponent<MeshFilter>().sharedMesh;
            gunMeshCollider.convex = true;
            gunMeshCollider.enabled = false;
            MeshRenderer gunRenderer = gunObject.GetComponent<MeshRenderer>();

            InvokeStatic(
                colliderRegistryType,
                "RegisterRenderers",
                new[] { vehicleRootType, typeof(Renderer[]) },
                new object[] { vehicleRoot, new Renderer[] { turretRenderer, gunRenderer } });
            InvokeStatic(
                colliderRegistryType,
                "RegisterImpactMeshColliders",
                new[] { vehicleRootType, typeof(List<MeshCollider>) },
                new object[]
                {
                    vehicleRoot,
                    new List<MeshCollider> { turretMeshCollider, gunMeshCollider }
                });
            InvokeStatic(
                colliderRegistryType,
                "RegisterImpactVisualPart",
                new[] { vehicleRootType, typeof(MeshCollider), typeof(Renderer) },
                new object[] { vehicleRoot, turretMeshCollider, turretRenderer });
            InvokeStatic(
                colliderRegistryType,
                "RegisterImpactVisualPart",
                new[] { vehicleRootType, typeof(MeshCollider), typeof(Renderer) },
                new object[] { vehicleRoot, gunMeshCollider, gunRenderer });

            Physics.SyncTransforms();
            uint originalTurretMask = turretRenderer.renderingLayerMask;
            uint originalGunMask = gunRenderer.renderingLayerMask;

            GameObject turretImpactObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
            _createdObjects.Add(turretImpactObject);
            Component turretImpact = turretImpactObject.GetComponent(pooledImpactFxType);
            InvokeInstance(
                pooledImpactFxType,
                turretImpact,
                "Initialize",
                new[] { typeof(float) },
                new object[] { 5f });
            InvokeInstance(
                pooledImpactFxType,
                turretImpact,
                "PlayImpact",
                new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                new object[]
                {
                    turretObject.transform.position + Vector3.right * 0.5f,
                    Vector3.right,
                    vehicleRoot,
                    turretMeshCollider
                });
            yield return null;

            object impactProfile = pooledImpactFxType
                .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(turretImpact);
            Component turretImpactDecal = (Component)impactProfile.GetType()
                .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            uint turretImpactLayer = GetUIntProperty(turretImpactDecal, "renderingLayerMask");
            Assert.GreaterOrEqual(turretImpactLayer, 4u, "Turret impact did not acquire a target-only layer.");
            Assert.AreNotEqual(
                0u,
                turretRenderer.renderingLayerMask & turretImpactLayer,
                "The turret renderer did not receive its impact layer.");
            Assert.AreEqual(
                0u,
                gunRenderer.renderingLayerMask & turretImpactLayer,
                "The turret impact layer leaked onto the gun renderer.");

            List<Component> repeatedTurretImpacts = new List<Component>(8);
            for (int repeatIndex = 0; repeatIndex < 8; repeatIndex++)
            {
                GameObject repeatedImpactObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
                _createdObjects.Add(repeatedImpactObject);
                Component repeatedImpact = repeatedImpactObject.GetComponent(pooledImpactFxType);
                InvokeInstance(
                    pooledImpactFxType,
                    repeatedImpact,
                    "Initialize",
                    new[] { typeof(float) },
                    new object[] { 5f });
                InvokeInstance(
                    pooledImpactFxType,
                    repeatedImpact,
                    "PlayImpact",
                    new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                    new object[]
                    {
                        turretObject.transform.position + Vector3.right * 0.5f,
                        Vector3.right,
                        vehicleRoot,
                        turretMeshCollider
                    });
                yield return null;

                object repeatedImpactProfile = pooledImpactFxType
                    .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(repeatedImpact);
                Component repeatedImpactDecal = (Component)repeatedImpactProfile.GetType()
                    .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(repeatedImpactProfile);
                Assert.AreEqual(
                    turretImpactLayer,
                    GetUIntProperty(repeatedImpactDecal, "renderingLayerMask"),
                    "Repeated impacts on one renderer did not reuse its target-only layer.");
                repeatedTurretImpacts.Add(repeatedImpact);
            }

            GameObject gunImpactObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
            _createdObjects.Add(gunImpactObject);
            Component gunImpact = gunImpactObject.GetComponent(pooledImpactFxType);
            InvokeInstance(
                pooledImpactFxType,
                gunImpact,
                "Initialize",
                new[] { typeof(float) },
                new object[] { 5f });
            InvokeInstance(
                pooledImpactFxType,
                gunImpact,
                "PlayImpact",
                new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                new object[]
                {
                    gunObject.transform.position - Vector3.right * 0.5f,
                    Vector3.left,
                    vehicleRoot,
                    gunMeshCollider
                });
            yield return null;

            object gunImpactProfile = pooledImpactFxType
                .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(gunImpact);
            Component gunImpactDecal = (Component)gunImpactProfile.GetType()
                .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(gunImpactProfile);
            uint gunImpactLayer = GetUIntProperty(gunImpactDecal, "renderingLayerMask");
            Assert.GreaterOrEqual(gunImpactLayer, 4u, "Gun impact did not acquire a target-only layer.");
            Assert.AreNotEqual(turretImpactLayer, gunImpactLayer, "Different robot parts shared one impact layer.");
            Assert.AreNotEqual(
                0u,
                gunRenderer.renderingLayerMask & gunImpactLayer,
                "The gun renderer did not receive its own impact layer.");
            Assert.AreEqual(
                0u,
                turretRenderer.renderingLayerMask & gunImpactLayer,
                "The gun impact layer leaked onto the turret renderer.");

            for (int repeatIndex = 0; repeatIndex < repeatedTurretImpacts.Count; repeatIndex++)
            {
                InvokeInstance(
                    pooledImpactFxType,
                    repeatedTurretImpacts[repeatIndex],
                    "PrepareForPoolRelease",
                    Array.Empty<Type>(),
                    Array.Empty<object>());
            }

            InvokeInstance(
                pooledImpactFxType,
                turretImpact,
                "PrepareForPoolRelease",
                Array.Empty<Type>(),
                Array.Empty<object>());
            InvokeInstance(
                pooledImpactFxType,
                gunImpact,
                "PrepareForPoolRelease",
                Array.Empty<Type>(),
                Array.Empty<object>());
            Assert.AreEqual(originalTurretMask, turretRenderer.renderingLayerMask);
            Assert.AreEqual(originalGunMask, gunRenderer.renderingLayerMask);
            InvokeStatic(
                colliderRegistryType,
                "UnregisterImpactMeshColliders",
                new[] { vehicleRootType },
                new object[] { vehicleRoot });
        }

        [UnityTest]
        public IEnumerator RobotVariants_DoNotShowImpactHaloWhenShotFromSide()
        {
            Type vehicleRootType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot");
            Type colliderRegistryType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleColliderRegistry");
            Type pooledImpactFxType = FindGameType("PooledImpactFx");
            Type projectileType = FindGameType("Projectile");
            string[] robotPrefabPaths =
            {
                "Assets/Game/Content/Robots/IA/IA_L1_Starter/IA_L1_Starter.prefab",
                "Assets/Game/Content/Robots/IA/IA_L2_Colossus/IA_L2_Colossus.prefab",
                "Assets/Game/Content/Robots/IA/IA_L2_Guardian/IA_L2_Guardian.prefab",
                "Assets/Game/Content/Robots/IA/IA_L2_Scout/IA_L2_Scout.prefab",
                "Assets/Game/Content/Robots/NV/NV_L1_Starter/NV_L1_Starter.prefab",
                "Assets/Game/Content/Robots/NV/NV_L2_Colossus/NV_L2_Colossus.prefab",
                "Assets/Game/Content/Robots/NV/NV_L2_Guardian/NV_L2_Guardian.prefab",
                "Assets/Game/Content/Robots/NV/NV_L2_Scout/NV_L2_Scout.prefab"
            };

            GameObject impactFxPrefabObject = LoadAsset<GameObject>(RobotImpactFxPrefabPath, "Robot Impact");
            Assert.IsNotNull(impactFxPrefabObject, $"Missing impact FX prefab at {RobotImpactFxPrefabPath}.");

            GameObject projectileVisualPrefab = LoadAsset<GameObject>(
                ProjectileVisualPrefabPath,
                "Projectile Visual");
            Assert.IsNotNull(projectileVisualPrefab, $"Missing projectile prefab at {ProjectileVisualPrefabPath}.");
            Component projectileVisual = projectileVisualPrefab.GetComponent(projectileType);
            Assert.IsNotNull(projectileVisual, $"Prefab at {ProjectileVisualPrefabPath} has no Projectile component.");
            Component robotImpactFxPrefab = impactFxPrefabObject.GetComponent(pooledImpactFxType);

            for (int robotIndex = 0; robotIndex < robotPrefabPaths.Length; robotIndex++)
            {
                string prefabPath = robotPrefabPaths[robotIndex];
                GameObject robotPrefab = LoadAsset<GameObject>(prefabPath, prefabPath);
                Assert.IsNotNull(robotPrefab, $"Missing robot prefab at {prefabPath}.");

                GameObject robotObject = UnityEngine.Object.Instantiate(robotPrefab);
                _createdObjects.Add(robotObject);
                robotObject.SetActive(true);
                yield return null;

                Component vehicleRoot = robotObject.GetComponent(vehicleRootType);
                Assert.IsNotNull(vehicleRoot, $"The spawned robot has no VehicleRoot: {prefabPath}.");
                object selectedImpactPrefab = projectileVisual.GetType()
                    .GetMethod("GetImpactFxPrefab", new[] { vehicleRootType })
                    .Invoke(projectileVisual, new object[] { vehicleRoot });
                Assert.AreSame(
                    robotImpactFxPrefab,
                    selectedImpactPrefab,
                    $"ProjectileVisual selected a non-robot impact profile for {prefabPath}.");
                MeshCollider[] visualMeshColliders = robotObject.GetComponentsInChildren<MeshCollider>(true);
                ActivateVisualMeshColliderObjects(visualMeshColliders);
                Assert.IsTrue(
                    TryGetVisualBounds(robotObject, out Bounds visualBounds),
                    $"The robot has no visual renderer bounds: {prefabPath}.");
                Assert.IsTrue(
                    TryFindSideVisualMeshHit(
                        visualMeshColliders,
                        visualBounds,
                        out RaycastHit visualHit,
                        out _,
                        out _),
                    $"The robot has no raycastable visual mesh: {prefabPath}.");

                object[] surfaceArguments =
                {
                    vehicleRoot,
                    visualHit.point,
                    visualHit.normal,
                    null,
                    null
                };
                bool resolved = (bool)InvokeStatic(
                    colliderRegistryType,
                    "TryGetImpactVisualSurface",
                    new[]
                    {
                        vehicleRootType,
                        typeof(Vector3),
                        typeof(Vector3),
                        typeof(Vector3).MakeByRefType(),
                        typeof(Vector3).MakeByRefType()
                    },
                    surfaceArguments);
                Assert.IsTrue(resolved, $"Robot visual surface was not registered: {prefabPath}.");

                GameObject impactFxObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
                _createdObjects.Add(impactFxObject);
                Component impactFx = impactFxObject.GetComponent(pooledImpactFxType);
                InvokeInstance(
                    pooledImpactFxType,
                    impactFx,
                    "Initialize",
                    new[] { typeof(float) },
                    new object[] { 5f });
                InvokeInstance(
                    pooledImpactFxType,
                    impactFx,
                    "PlayImpact",
                    new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                    new object[] { visualHit.point, visualHit.normal, vehicleRoot, visualHit.collider });
                yield return null;

                object impactProfile = pooledImpactFxType
                    .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(impactFx);
                MeshRenderer haloRenderer = (MeshRenderer)impactProfile.GetType()
                    .GetField("impactGlowHaloRenderer", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(impactProfile);
                bool enableImpactGlowHalo = (bool)impactProfile.GetType()
                    .GetField("enableImpactGlowHalo", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(impactProfile);
                Assert.IsNotNull(haloRenderer, $"Robot profile has no Halo: {prefabPath}.");
                Assert.IsFalse(enableImpactGlowHalo, $"Robot profile unexpectedly enables Halo: {prefabPath}.");
                Assert.IsFalse(haloRenderer.enabled, $"Robot Halo was enabled after hit: {prefabPath}.");
                Assert.IsFalse(haloRenderer.gameObject.activeSelf, $"Robot Halo object was activated: {prefabPath}.");
                Component impactDecal = (Component)impactProfile.GetType()
                    .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(impactProfile);
                Assert.IsNotNull(impactDecal, $"Robot profile has no regular decal: {prefabPath}.");
                Assert.IsTrue(impactDecal.gameObject.activeSelf, $"Robot decal was not active after hit: {prefabPath}.");
                uint impactLayer = GetUIntProperty(impactDecal, "renderingLayerMask");
                Assert.GreaterOrEqual(
                    impactLayer,
                    4u,
                    $"Robot decal used the world-only layer for {prefabPath}: {impactLayer}.");
                List<Renderer> targetRenderers = GetImpactRenderers(
                    colliderRegistryType,
                    vehicleRootType,
                    vehicleRoot);
                Assert.IsNotNull(targetRenderers, $"Robot renderers were not registered: {prefabPath}.");
                bool targetLayerApplied = false;
                for (int rendererIndex = 0; rendererIndex < targetRenderers.Count; rendererIndex++)
                {
                    Renderer targetRenderer = targetRenderers[rendererIndex];
                    if (targetRenderer != null && (targetRenderer.renderingLayerMask & impactLayer) != 0u)
                    {
                        targetLayerApplied = true;
                        break;
                    }
                }

                Assert.IsTrue(targetLayerApplied, $"Robot decal layer was not applied to the target: {prefabPath}.");
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.IsTrue(impactDecal.gameObject.activeSelf, $"Robot decal did not remain active: {prefabPath}.");
                Debug.Log(
                    $"Robot impact regression: prefab={prefabPath}, mesh={visualHit.point}, "
                    + $"decalLayer={impactLayer}, haloEnabled={haloRenderer.enabled}.");

                InvokeInstance(
                    pooledImpactFxType,
                    impactFx,
                    "PrepareForPoolRelease",
                    Array.Empty<Type>(),
                    Array.Empty<object>());
                Assert.IsFalse(haloRenderer.enabled, $"Pool reset left Halo enabled: {prefabPath}.");
            }
        }

        [UnityTest]
        public IEnumerator PersistentWorldImpactDoesNotProjectOntoRobotAfterItEntersImpactVolume()
        {
            Type vehicleRootType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot");
            Type colliderRegistryType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleColliderRegistry");
            Type pooledImpactFxType = FindGameType("PooledImpactFx");
            Type projectileType = FindGameType("Projectile");
            GameObject robotPrefab = LoadAsset<GameObject>(RobotPrefabPath, "IA_L1_Starter");
            Assert.IsNotNull(robotPrefab, $"Missing robot prefab at {RobotPrefabPath}.");

            GameObject robotObject = UnityEngine.Object.Instantiate(robotPrefab);
            _createdObjects.Add(robotObject);
            robotObject.SetActive(true);
            yield return null;
            MeshCollider[] visualMeshColliders = robotObject.GetComponentsInChildren<MeshCollider>(true);
            ActivateVisualMeshColliderObjects(visualMeshColliders);
            Physics.SyncTransforms();

            Component vehicleRoot = robotObject.GetComponent(vehicleRootType);
            Assert.IsNotNull(vehicleRoot, "The spawned robot has no VehicleRoot.");
            List<Renderer> targetRenderers = GetImpactRenderers(
                colliderRegistryType,
                vehicleRootType,
                vehicleRoot);
            Assert.IsNotNull(targetRenderers, "The spawned robot did not register its impact renderers.");
            Assert.IsNotEmpty(targetRenderers, "The spawned robot has no registered impact renderers.");

            robotObject.transform.position = new Vector3(-5f, 0f, 0f);
            Physics.SyncTransforms();
            yield return null;

            Assert.IsTrue(
                TryGetVisualBounds(robotObject, out Bounds visualBounds),
                "The spawned robot has no visual renderer bounds.");
            Vector3 groundImpactPosition = new Vector3(
                0f,
                visualBounds.min.y,
                visualBounds.center.z);

            GameObject groundObject = new GameObject("Persistent World Impact Test Ground");
            _createdObjects.Add(groundObject);
            groundObject.transform.position = groundImpactPosition + Vector3.down * 0.1f;
            BoxCollider groundCollider = groundObject.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(6f, 0.2f, 6f);
            Assert.IsNull(
                InvokeStatic(
                    projectileType,
                    "GetImpactAttachmentParent",
                    new[] { typeof(Collider) },
                    new object[] { groundCollider }),
                "A world impact unexpectedly kept an attachment parent.");

            GameObject impactFxPrefabObject =
                LoadAsset<GameObject>(OtherObjectImpactFxPrefabPath, "Other Object Impact");
            Assert.IsNotNull(impactFxPrefabObject, $"Missing impact FX prefab at {OtherObjectImpactFxPrefabPath}.");
            GameObject impactFxObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
            _createdObjects.Add(impactFxObject);
            Component impactFx = impactFxObject.GetComponent(pooledImpactFxType);
            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "Initialize",
                new[] { typeof(float) },
                new object[] { 5f });
            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "PlayImpact",
                new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                new object[] { groundImpactPosition, Vector3.up, null, groundCollider });
            yield return null;

            object impactProfile = pooledImpactFxType
                .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactFx);
            Component impactDecal = (Component)impactProfile.GetType()
                .GetField("impactDecal", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            Assert.IsNotNull(impactDecal, "The world impact profile has no regular impact decal.");
            Assert.IsTrue(impactDecal.gameObject.activeSelf, "The world impact decal was not activated.");
            Assert.AreEqual(
                2u,
                GetUIntProperty(impactDecal, "renderingLayerMask"),
                "The world impact decal did not use the world rendering layer.");

            robotObject.transform.position = new Vector3(0f, 0f, 0f);
            Physics.SyncTransforms();
            yield return null;

            for (int i = 0; i < targetRenderers.Count; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                Assert.AreEqual(
                    0u,
                    targetRenderer.renderingLayerMask & 2u,
                    $"World impact layer leaked onto robot renderer {targetRenderer.name} after the robot entered the impact volume.");
                Assert.AreNotEqual(
                    0u,
                    targetRenderer.renderingLayerMask & 1u,
                    $"World impact isolation removed the default lighting layer from robot renderer {targetRenderer.name}.");
            }

            Debug.Log(
                $"Persistent world impact regression: impact={groundImpactPosition}, "
                + $"robotBefore=(-5, 0, 0), robotAfter={robotObject.transform.position}, "
                + $"worldMask={GetUIntProperty(impactDecal, "renderingLayerMask")}.");
        }

        [UnityTest]
        public IEnumerator FenceAndShootFromSide_UsesVisualMeshForImpactHalo()
        {
            Type vehicleRootType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleRoot");
            Type colliderRegistryType = FindGameType("Game.Scripts.Gameplay.Robots.VehicleColliderRegistry");
            Type pooledImpactFxType = FindGameType("PooledImpactFx");
            Type impactVisualSurfaceType = FindGameType("ImpactVisualSurface");
            GameObject fencePrefab = LoadAsset<GameObject>(FencePrefabPath, "Concrete_fence_v1_wall_set_v2");
            Assert.IsNotNull(fencePrefab, $"Missing fence prefab at {FencePrefabPath}.");

            GameObject fenceObject = UnityEngine.Object.Instantiate(fencePrefab);
            _createdObjects.Add(fenceObject);
            fenceObject.SetActive(true);
            yield return null;

            Assert.IsNotNull(
                fenceObject.GetComponent(impactVisualSurfaceType),
                "The reported fence prefab has no ImpactVisualSurface registration.");
            BoxCollider physicsCollider = fenceObject.GetComponent<BoxCollider>();
            MeshCollider visualMeshCollider = fenceObject.GetComponent<MeshCollider>();
            MeshRenderer visualRenderer = fenceObject.GetComponent<MeshRenderer>();
            Assert.IsNotNull(physicsCollider, "The reported fence has no physics BoxCollider.");
            Assert.IsNotNull(visualMeshCollider, "The reported fence has no visual MeshCollider.");
            Assert.IsNotNull(visualRenderer, "The reported fence has no MeshRenderer.");

            Assert.IsTrue(
                TryFindColliderMismatch(
                    physicsCollider,
                    visualMeshCollider,
                    visualRenderer.bounds,
                    out RaycastHit physicsHit,
                    out RaycastHit expectedVisualHit,
                    out Vector3 shotOrigin,
                    out Vector3 shotDirection),
                "The reported fence did not produce a deterministic collider-versus-mesh mismatch.");

            object[] surfaceArguments =
            {
                physicsCollider,
                physicsHit.point,
                physicsHit.normal,
                null,
                null
            };
            bool resolved = (bool)InvokeStatic(
                colliderRegistryType,
                "TryGetImpactVisualSurface",
                new[]
                {
                    typeof(Collider),
                    typeof(Vector3),
                    typeof(Vector3),
                    typeof(Vector3).MakeByRefType(),
                    typeof(Vector3).MakeByRefType()
                },
                surfaceArguments);
            Assert.IsTrue(resolved, "The fence visual surface registry did not resolve the visual mesh.");
            Vector3 resolvedVisualPosition = (Vector3)surfaceArguments[3];
            Vector3 resolvedVisualNormal = (Vector3)surfaceArguments[4];
            Assert.Less(
                Vector3.Distance(resolvedVisualPosition, expectedVisualHit.point),
                SurfacePositionTolerance,
                $"Fence resolved point {resolvedVisualPosition} differs from mesh point {expectedVisualHit.point}.");

            GameObject impactFxPrefabObject = LoadAsset<GameObject>(OtherObjectImpactFxPrefabPath, "Other Object Impact");
            GameObject impactFxObject = UnityEngine.Object.Instantiate(impactFxPrefabObject);
            _createdObjects.Add(impactFxObject);
            Component impactFx = impactFxObject.GetComponent(pooledImpactFxType);
            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "Initialize",
                new[] { typeof(float) },
                new object[] { 5f });
            InvokeInstance(
                pooledImpactFxType,
                impactFx,
                "PlayImpact",
                new[] { typeof(Vector3), typeof(Vector3), vehicleRootType, typeof(Collider) },
                new object[] { physicsHit.point, physicsHit.normal, null, physicsCollider });
            yield return null;

            object impactProfile = pooledImpactFxType
                .GetField("impactProfile", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactFx);
            Type profileType = impactProfile.GetType();
            MeshRenderer haloRenderer = (MeshRenderer)profileType
                .GetField("impactGlowHaloRenderer", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            float surfaceOffset = (float)profileType
                .GetField("impactDecalSurfaceOffset", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            bool enableImpactGlowHalo = (bool)profileType
                .GetField("enableImpactGlowHalo", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(impactProfile);
            Vector3 expectedHaloPosition = resolvedVisualPosition + resolvedVisualNormal * surfaceOffset;
            float haloToMeshDistance = Vector3.Distance(haloRenderer.transform.position, expectedHaloPosition);
            float haloToPhysicsDistance = Vector3.Distance(haloRenderer.transform.position, physicsHit.point);
            Debug.Log(
                $"Fence impact regression: physics={physicsHit.point}, mesh={expectedVisualHit.point}, "
                + $"resolved={resolvedVisualPosition}, halo={haloRenderer.transform.position}, "
                + $"origin={shotOrigin}, direction={shotDirection}, "
                + $"haloToMesh={haloToMeshDistance:F5}, haloToPhysics={haloToPhysicsDistance:F5}.");

            Assert.IsTrue(enableImpactGlowHalo, "The other-object profile unexpectedly disables the Impact Glow Halo.");
            Assert.IsTrue(haloRenderer.enabled, "Fence Impact Glow Halo was not enabled.");
            Assert.Less(haloToMeshDistance, SurfacePositionTolerance, "Fence Halo is not on the visual mesh.");
            Assert.Greater(
                haloToPhysicsDistance,
                SurfacePositionTolerance,
                "Fence Halo still uses the oversized BoxCollider hit point.");
        }

        [TearDown]
        public void DestroyCreatedObjects()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                GameObject createdObject = _createdObjects[i];
                if (createdObject != null)
                {
                    UnityEngine.Object.Destroy(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        private static Type FindGameType(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.IsNotNull(type, $"Game type {typeName} is not available in Assembly-CSharp.");
            return type;
        }

        private static uint GetUIntProperty(Component component, string propertyName)
        {
            Assert.IsNotNull(component, $"Cannot read {propertyName} from a missing component.");
            PropertyInfo property = component.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"Component {component.GetType().FullName} has no {propertyName} property.");
            object value = property.GetValue(component);
            if (value is IConvertible)
            {
                return Convert.ToUInt32(value);
            }

            FieldInfo[] fields = value.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(uint)
                    || fields[i].FieldType == typeof(int))
                {
                    return Convert.ToUInt32(fields[i].GetValue(value));
                }
            }

            PropertyInfo[] properties = value.GetType().GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType == typeof(uint)
                    || properties[i].PropertyType == typeof(int))
                {
                    return Convert.ToUInt32(properties[i].GetValue(value));
                }
            }

            Assert.Fail($"Property {propertyName} on {component.GetType().FullName} has unsupported type {value.GetType().FullName}.");
            return 0u;
        }

        private static T LoadAsset<T>(string assetPath, string resourcesPath)
            where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
            return Resources.Load<T>(resourcesPath);
#endif
        }

        private static object InvokeStatic(
            Type declaringType,
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            MethodInfo method = declaringType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.IsNotNull(method, $"Missing method {declaringType.FullName}.{methodName}.");
            return method.Invoke(null, arguments);
        }

        private static void InvokeInstance(
            Type declaringType,
            object instance,
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            MethodInfo method = declaringType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.IsNotNull(method, $"Missing method {declaringType.FullName}.{methodName}.");
            method.Invoke(instance, arguments);
        }

        private static List<Renderer> GetImpactRenderers(
            Type registryType,
            Type vehicleRootType,
            Component vehicleRoot)
        {
            object[] arguments = { vehicleRoot, null };
            bool result = (bool)InvokeStatic(
                registryType,
                "TryGetImpactRenderers",
                new[] { vehicleRootType, typeof(List<Renderer>).MakeByRefType() },
                arguments);
            return result ? arguments[1] as List<Renderer> : null;
        }

        private static Component TryGetRoot(
            Type registryType,
            Type vehicleRootType,
            Collider collider)
        {
            object[] arguments = { collider, null };
            bool result = (bool)InvokeStatic(
                registryType,
                "TryGetRoot",
                new[] { typeof(Collider), vehicleRootType.MakeByRefType() },
                arguments);
            return result ? arguments[1] as Component : null;
        }

        private static bool TryGetVisualBounds(GameObject robotObject, out Bounds bounds)
        {
            bounds = default;
            bool foundRenderer = false;
            Renderer[] renderers = robotObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return foundRenderer;
        }

        private static bool TryFindSideVisualMeshHit(
            MeshCollider[] meshColliders,
            Bounds visualBounds,
            out RaycastHit closestHit,
            out Vector3 shotOrigin,
            out Vector3 shotDirection)
        {
            closestHit = default;
            shotDirection = Vector3.right;
            shotOrigin = new Vector3(visualBounds.min.x - 2f, visualBounds.center.y, visualBounds.center.z);
            const int sampleCount = 15;

            for (int yIndex = 0; yIndex < sampleCount; yIndex++)
            {
                float y = Mathf.Lerp(visualBounds.min.y, visualBounds.max.y, (yIndex + 0.5f) / sampleCount);
                for (int zIndex = 0; zIndex < sampleCount; zIndex++)
                {
                    float z = Mathf.Lerp(visualBounds.min.z, visualBounds.max.z, (zIndex + 0.5f) / sampleCount);
                    shotOrigin = new Vector3(visualBounds.min.x - 2f, y, z);
                    Ray visualRay = new Ray(shotOrigin, shotDirection);

                    for (int i = 0; i < meshColliders.Length; i++)
                    {
                        MeshCollider meshCollider = meshColliders[i];
                        if (meshCollider == null || meshCollider.sharedMesh == null)
                        {
                            continue;
                        }

                        bool wasEnabled = meshCollider.enabled;
                        if (!wasEnabled)
                        {
                            meshCollider.enabled = true;
                        }

                        try
                        {
                            Physics.SyncTransforms();
                            if (meshCollider.Raycast(visualRay, out RaycastHit visualHit, RaycastDistance)
                                && (closestHit.collider == null || visualHit.distance < closestHit.distance))
                            {
                                closestHit = visualHit;
                            }
                        }
                        finally
                        {
                            if (!wasEnabled && meshCollider != null)
                            {
                                meshCollider.enabled = false;
                            }
                        }
                    }
                }

                if (closestHit.collider != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindColliderMismatch(
            BoxCollider physicsCollider,
            MeshCollider visualMeshCollider,
            Bounds visualBounds,
            out RaycastHit physicsHit,
            out RaycastHit visualHit,
            out Vector3 shotOrigin,
            out Vector3 shotDirection)
        {
            physicsHit = default;
            visualHit = default;
            shotDirection = Vector3.right;
            shotOrigin = new Vector3(visualBounds.min.x - 2f, visualBounds.center.y, visualBounds.center.z);
            const int sampleCount = 17;
            bool wasEnabled = visualMeshCollider.enabled;
            visualMeshCollider.enabled = true;
            try
            {
                for (int side = 0; side < 2; side++)
                {
                    shotDirection = side == 0 ? Vector3.right : Vector3.left;
                    float sideStart = side == 0 ? visualBounds.min.x - 2f : visualBounds.max.x + 2f;
                    for (int yIndex = 0; yIndex < sampleCount; yIndex++)
                    {
                        float y = Mathf.Lerp(visualBounds.min.y, visualBounds.max.y, (yIndex + 0.5f) / sampleCount);
                        for (int zIndex = 0; zIndex < sampleCount; zIndex++)
                        {
                            float z = Mathf.Lerp(visualBounds.min.z, visualBounds.max.z, (zIndex + 0.5f) / sampleCount);
                            shotOrigin = new Vector3(sideStart, y, z);
                            Ray ray = new Ray(shotOrigin, shotDirection);
                            if (!physicsCollider.Raycast(ray, out RaycastHit candidatePhysicsHit, RaycastDistance)
                                || !visualMeshCollider.Raycast(ray, out RaycastHit candidateVisualHit, RaycastDistance))
                            {
                                continue;
                            }

                            if (Vector3.Distance(candidatePhysicsHit.point, candidateVisualHit.point)
                                <= SurfacePositionTolerance)
                            {
                                continue;
                            }

                            physicsHit = candidatePhysicsHit;
                            visualHit = candidateVisualHit;
                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                visualMeshCollider.enabled = wasEnabled;
            }
        }

        private static void ActivateVisualMeshColliderObjects(MeshCollider[] meshColliders)
        {
            for (int i = 0; i < meshColliders.Length; i++)
            {
                MeshCollider meshCollider = meshColliders[i];
                if (meshCollider == null)
                {
                    continue;
                }

                Transform currentTransform = meshCollider.transform;
                while (currentTransform != null)
                {
                    currentTransform.gameObject.SetActive(true);
                    currentTransform = currentTransform.parent;
                }
            }
        }

    }
}
