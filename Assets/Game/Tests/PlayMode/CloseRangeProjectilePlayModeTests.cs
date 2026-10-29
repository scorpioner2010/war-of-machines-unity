using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public sealed class CloseRangeProjectilePlayModeTests
    {
        [Test]
        public void ShotHudStatusUsesGlyphOnlyForAutonomousControl()
        {
            Type shooterType = FindGameType("Game.Scripts.Gameplay.Robots.NetworkWeaponShooter");
            MethodInfo getStatusText = shooterType.GetMethod(
                "GetShotHudStatusText",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(getStatusText);

            Assert.AreEqual("Miss", getStatusText.Invoke(null, new object[] { (byte)0, false }));
            Assert.AreEqual("Penetrated", getStatusText.Invoke(null, new object[] { (byte)1, false }));
            Assert.AreEqual("No penetration", getStatusText.Invoke(null, new object[] { (byte)2, false }));
            Assert.AreEqual("〰〰〰", getStatusText.Invoke(null, new object[] { (byte)1, true }));
            Assert.AreEqual("〰〰〰", getStatusText.Invoke(null, new object[] { (byte)2, true }));
            Assert.AreEqual("Miss", getStatusText.Invoke(null, new object[] { (byte)0, true }));
            Assert.AreEqual("Miss", getStatusText.Invoke(null, new object[] { (byte)255, false }));
        }

        [Test]
        public void BackwardAimPointUsesMuzzleForwardDirection()
        {
            Type shooterType = FindGameType("Game.Scripts.Gameplay.Robots.NetworkWeaponShooter");
            GameObject shooterObject = new GameObject("CloseRangeShooter");
            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(shooterObject.transform);
            Component shooter = shooterObject.AddComponent(shooterType);

            try
            {
                shooterType.GetField("muzzleTransform", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(shooter, muzzleObject.transform);

                MethodInfo buildRay = shooterType.GetMethod(
                    "BuildDispersedShotRay",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(buildRay);

                object result = buildRay.Invoke(
                    shooter,
                    new object[]
                    {
                        Vector3.zero,
                        Vector3.back * 2f,
                        1,
                        0f,
                        null
                    });
                FieldInfo directionField = result.GetType().GetField(
                    "Direction",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Vector3 direction = (Vector3)directionField.GetValue(result);

                Assert.That(Vector3.Angle(Vector3.forward, direction), Is.LessThan(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shooterObject);
            }
        }

        [UnityTest]
        public IEnumerator ProjectileStartingInsideArmorResolvesForwardHit()
        {
            Type projectileType = FindGameType("Projectile");
            GameObject shooterObject = new GameObject("CloseRangeShooter");
            GameObject targetObject = new GameObject("EmbeddedTarget");
            GameObject projectileObject = new GameObject("Projectile");
            Collider targetCollider = null;
            bool hitCalled = false;
            Vector3 hitDirection = Vector3.zero;
            Collider resolvedCollider = null;

            try
            {
                shooterObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 1f);
                targetObject.layer = LayerMask.NameToLayer("Armor");
                BoxCollider targetBox = targetObject.AddComponent<BoxCollider>();
                targetBox.size = new Vector3(2f, 2f, 2f);
                targetCollider = targetBox;

                Component projectile = projectileObject.AddComponent(projectileType);
                MethodInfo init = projectileType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(init);
                init.Invoke(
                    projectile,
                    new object[]
                    {
                        new Vector3(0f, 0f, 1f),
                        Vector3.forward * 100f,
                        Vector3.zero,
                        3f,
                        100f,
                        0.05f,
                        0f,
                        true
                    });

                Action<RaycastHit, Vector3> onHit = (hit, direction) =>
                {
                    hitCalled = true;
                    resolvedCollider = hit.collider;
                    hitDirection = direction;
                };
                MethodInfo configure = projectileType.GetMethod(
                    "ConfigureLiveCollision",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(configure);
                configure.Invoke(
                    projectile,
                    new object[]
                    {
                        shooterObject.transform,
                        onHit,
                        new Action(() => { }),
                        100f
                    });

                Physics.SyncTransforms();
                yield return null;

                Assert.IsTrue(hitCalled);
                Assert.AreSame(targetCollider, resolvedCollider);
                Assert.That(Vector3.Dot(Vector3.forward, hitDirection), Is.GreaterThan(0.99f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(shooterObject);
            }
        }

        [UnityTest]
        public IEnumerator ProjectileStartingInsideDefaultWorldGeometryResolvesForwardHit()
        {
            Type projectileType = FindGameType("Projectile");
            GameObject shooterObject = new GameObject("CloseRangeShooter");
            GameObject wallObject = new GameObject("EmbeddedWall");
            GameObject projectileObject = new GameObject("Projectile");
            Collider wallCollider = null;
            bool hitCalled = false;
            Collider resolvedCollider = null;

            try
            {
                wallObject.transform.position = new Vector3(0f, 0f, 1f);
                BoxCollider wallBox = wallObject.AddComponent<BoxCollider>();
                wallBox.size = new Vector3(2f, 2f, 2f);
                wallCollider = wallBox;

                Component projectile = projectileObject.AddComponent(projectileType);
                MethodInfo init = projectileType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(init);
                init.Invoke(
                    projectile,
                    new object[]
                    {
                        new Vector3(0f, 0f, 1f),
                        Vector3.forward * 100f,
                        Vector3.zero,
                        3f,
                        100f,
                        0.05f,
                        0f,
                        true
                    });

                Action<RaycastHit, Vector3> onHit = (hit, direction) =>
                {
                    hitCalled = true;
                    resolvedCollider = hit.collider;
                };
                MethodInfo configure = projectileType.GetMethod(
                    "ConfigureLiveCollision",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(configure);
                configure.Invoke(
                    projectile,
                    new object[]
                    {
                        shooterObject.transform,
                        onHit,
                        new Action(() => { }),
                        100f
                    });

                Physics.SyncTransforms();
                yield return null;

                Assert.IsTrue(hitCalled);
                Assert.AreSame(wallCollider, resolvedCollider);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                UnityEngine.Object.DestroyImmediate(wallObject);
                UnityEngine.Object.DestroyImmediate(shooterObject);
            }
        }

        private static Type FindGameType(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.IsNotNull(type, $"Game type {typeName} is not available in Assembly-CSharp.");
            return type;
        }
    }
}
