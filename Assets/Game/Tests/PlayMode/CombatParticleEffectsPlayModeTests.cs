#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class CombatParticleEffectsPlayModeTests
    {
        private const string ProjectileVisualPath =
            "Assets/Game/Content/Combat/Projectiles/ProjectileVisual.prefab";
        private const string GunShootPath = "Assets/Game/Content/Combat/Effects/GunShoot.prefab";
        private const string GroundImpactPath = "Assets/Game/Content/Combat/Effects/ExplosionProjectileGround.prefab";
        private const string RobotHitPath = "Assets/Game/Content/Combat/Effects/ExplosionProjectileRobot.prefab";
        private const string MainExplosionPath = "Assets/Game/Content/Combat/Effects/ExplosionMain.prefab";
        private const string PartFirePath = "Assets/Game/Content/Combat/Effects/PartFire.prefab";

        [Test]
        public void ProjectileVisualUsesDirectionalGunAndImpactEffects()
        {
            GameObject projectileObject = LoadPrefab(ProjectileVisualPath);
            Component projectile = projectileObject.GetComponent("Projectile");

            Assert.IsNotNull(projectile);
            Assert.AreEqual(
                "ExplosionProjectileRobot",
                GetPrefabField(projectile, "robotImpactFxPrefab").name);
            Assert.AreEqual(
                "ExplosionProjectileGround",
                GetPrefabField(projectile, "groundImpactFxPrefab").name);
        }

        [Test]
        public void CombatParticlePrefabsUseExpectedParticleAndDecalProfiles()
        {
            AssertParticlePrefab(GunShootPath, false);
            AssertParticlePrefab(GroundImpactPath, true);
            AssertParticlePrefab(RobotHitPath, true);
            AssertParticlePrefab(MainExplosionPath, false);
        }

        [Test]
        public void ImpactPrefabsKeepTheirAuthoredRootDirectionMode()
        {
            Assert.AreEqual(true, GetRootDirectionMode(GroundImpactPath));
            Assert.AreEqual(false, GetRootDirectionMode(RobotHitPath));
        }

        [Test]
        public void PartFireRemainsLoopingAndUnpooled()
        {
            GameObject partFire = LoadPrefab(PartFirePath);
            Assert.IsNull(partFire.GetComponent("PooledImpactFx"));
            Assert.IsNull(partFire.GetComponent("CFX_AutoDestructShuriken"));

            ParticleSystem[] particles = partFire.GetComponentsInChildren<ParticleSystem>(true);
            Assert.IsNotEmpty(particles);
            for (int i = 0; i < particles.Length; i++)
            {
                Assert.IsTrue(particles[i].main.loop, particles[i].name + " must loop.");
            }
        }

        [Test]
        public void ImpactPrefabsKeepAuthoredRootScale()
        {
            Assert.AreEqual(Vector3.one, LoadPrefab(GroundImpactPath).transform.localScale);
            Assert.AreEqual(
                new Vector3(0.05f, 0.05f, 0.05f),
                LoadPrefab(RobotHitPath).transform.localScale);
        }

        [Test]
        public void ImpactFxUsesAttachedNeutralAnchorForNonUniformMeshScale()
        {
            GameObject impactPrefab = LoadPrefab(RobotHitPath);
            GameObject impactInstance = UnityEngine.Object.Instantiate(impactPrefab);
            Component impactFx = impactInstance.GetComponent("PooledImpactFx");
            MethodInfo attachMethod = impactFx.GetType().GetMethod(
                "AttachToImpactTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo detachMethod = impactFx.GetType().GetMethod(
                "DetachFromImpactTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(attachMethod);
            Assert.IsNotNull(detachMethod);

            GameObject mesh = new GameObject("ImpactFxMeshScaleTest");
            mesh.transform.localRotation = Quaternion.Euler(17f, 31f, 9f);
            mesh.transform.localScale = new Vector3(2f, 0.5f, 3f);
            try
            {
                attachMethod.Invoke(
                    impactFx,
                    new object[]
                    {
                        mesh.transform,
                        Vector3.zero,
                        Quaternion.identity,
                        new Vector3(0.05f, 0.05f, 0.05f)
                    });

                Transform anchor = impactInstance.transform.parent;
                Assert.IsNotNull(anchor);
                Assert.AreSame(mesh.transform, anchor.parent);
                Assert.AreEqual(0.05f, impactInstance.transform.lossyScale.x, 0.0001f);
                Assert.AreEqual(0.05f, impactInstance.transform.lossyScale.y, 0.0001f);
                Assert.AreEqual(0.05f, impactInstance.transform.lossyScale.z, 0.0001f);
            }
            finally
            {
                detachMethod.Invoke(impactFx, new object[] { null });
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(impactInstance);
            }
        }

        [Test]
        public void RobotDestructionFireLifetimeIsThirtySeconds()
        {
            GameObject robot = LoadPrefab(
                "Assets/Game/Content/Robots/IronAlliance/IA_L1_Starter/IA_L1_Starter.prefab");
            Component controller = robot.GetComponent("RobotDestructionParticleController");

            Assert.IsNotNull(controller);
            FieldInfo lifetimeField = controller.GetType().GetField(
                "partFireLifetime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(lifetimeField);
            Assert.AreEqual(30f, (float)lifetimeField.GetValue(controller));
        }

        [Test]
        public void ParticleCameraOffsetUsesOneWorldUnitByDefault()
        {
            Type runtimeSettingsType = Type.GetType(
                "Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp");
            Assert.IsNotNull(runtimeSettingsType);
            FieldInfo defaultOffsetField = runtimeSettingsType.GetField(
                "DefaultParticleCameraOffset",
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo clampMethod = runtimeSettingsType.GetMethod(
                "ClampParticleCameraOffset",
                BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(defaultOffsetField);
            Assert.IsNotNull(clampMethod);

            float defaultOffset = (float)defaultOffsetField.GetValue(null);
            Assert.AreEqual(
                defaultOffset,
                (float)clampMethod.Invoke(null, new object[] { defaultOffset }));
            Assert.AreEqual(
                0f,
                (float)clampMethod.Invoke(null, new object[] { -1f }));
        }

        [Test]
        public void RobotPrefabsOwnDestructionParticleController()
        {
            string[] robotPaths =
            {
                "Assets/Game/Content/Robots/IronAlliance/IA_L1_Starter/IA_L1_Starter.prefab",
                "Assets/Game/Content/Robots/IronAlliance/IA_L2_Colossus/IA_L2_Colossus.prefab",
                "Assets/Game/Content/Robots/IronAlliance/IA_L2_Guardian/IA_L2_Guardian.prefab",
                "Assets/Game/Content/Robots/IronAlliance/IA_L2_Scout/IA_L2_Scout.prefab",
                "Assets/Game/Content/Robots/NovaSyndicate/NV_L1_Starter/NV_L1_Starter.prefab",
                "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Colossus/NV_L2_Colossus.prefab",
                "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Guardian/NV_L2_Guardian.prefab",
                "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Scout/NV_L2_Scout.prefab"
            };

            for (int i = 0; i < robotPaths.Length; i++)
            {
                GameObject robot = LoadPrefab(robotPaths[i]);
                Assert.IsNotNull(robot.GetComponent("DeathLogic"), robotPaths[i]);
                Assert.IsNotNull(
                    robot.GetComponent("RobotDestructionParticleController"),
                    robotPaths[i]);
            }
        }

        private static void AssertParticlePrefab(string path, bool expectedSurfaceProjectors)
        {
            GameObject prefab = LoadPrefab(path);
            Component impactFx = prefab.GetComponent("PooledImpactFx");

            Assert.IsNotNull(impactFx, path);
            object profile = impactFx.GetType().GetField("impactProfile").GetValue(impactFx);
            Array particles = (Array)profile.GetType().GetField("particles").GetValue(profile);
            bool usesSurfaceProjectors = (bool)impactFx.GetType()
                .GetProperty("UsesSurfaceProjectors")
                .GetValue(impactFx);

            Assert.IsNotEmpty(particles, path);
            Assert.AreEqual(expectedSurfaceProjectors, usesSurfaceProjectors, path);
        }

        private static bool GetRootDirectionMode(string path)
        {
            GameObject prefab = LoadPrefab(path);
            Component impactFx = prefab.GetComponent("PooledImpactFx");
            FieldInfo directionField = impactFx.GetType().GetField("alignRootToImpactNormal");

            Assert.IsNotNull(directionField, path);
            return (bool)directionField.GetValue(impactFx);
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "Missing prefab: " + path);
            return prefab;
        }

        private static Component GetPrefabField(Component component, string fieldName)
        {
            return (Component)component.GetType().GetField(fieldName).GetValue(component);
        }
    }
}
#endif
