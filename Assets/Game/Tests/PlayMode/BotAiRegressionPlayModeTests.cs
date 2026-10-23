using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace Game.Tests.PlayMode
{
    public sealed class BotAiRegressionPlayModeTests
    {
        [UnityTest]
        public IEnumerator MovingTargetLeadUsesInterceptTime()
        {
            Type predictionType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotLeadPrediction, Assembly-CSharp");
            Assert.IsNotNull(predictionType);

            MethodInfo solve = predictionType.GetMethod("Solve", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(solve);

            object result = solve.Invoke(null, new object[]
            {
                new Vector3(20f, 0f, 0f),
                Vector3.zero,
                new Vector3(10f, 0f, 0f),
                20f,
                3f,
                4,
                1f,
                0f
            });

            Vector3 predicted = (Vector3)result;
            Assert.That(predicted.x, Is.EqualTo(38.75f).Within(0.02f));
            Assert.That(predicted.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(predicted.z, Is.EqualTo(0f).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LineOfFireSelectsAnExposedTargetSide()
        {
            GameObject shooterObject = new GameObject("BotFireRegressionShooter");
            GameObject targetObject = new GameObject("BotFireRegressionTarget");
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Type vehicleRootType = Type.GetType("Game.Scripts.Gameplay.Robots.VehicleRoot, Assembly-CSharp");
                Assert.IsNotNull(vehicleRootType);
                Component shooterRoot = shooterObject.AddComponent(vehicleRootType);
                Component targetRoot = targetObject.AddComponent(vehicleRootType);
                shooterObject.transform.position = new Vector3(0f, 1f, 0f);
                targetObject.transform.SetPositionAndRotation(new Vector3(10f, 1f, 0f), Quaternion.Euler(0f, 90f, 0f));
                wallObject.transform.SetPositionAndRotation(new Vector3(5f, 1f, 0f), Quaternion.identity);
                wallObject.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
                Physics.SyncTransforms();

                Type checkerType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotLineOfFireChecker, Assembly-CSharp");
                Type settingsType = Type.GetType("Game.Scripts.Server.BotCombatSettings, Assembly-CSharp");
                Assert.IsNotNull(checkerType);
                Assert.IsNotNull(settingsType);

                object checker = Activator.CreateInstance(checkerType, true);
                object settings = Activator.CreateInstance(settingsType);
                MethodInfo method = checkerType.GetMethod("TryGetBestAimPoint", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(method);
                object[] arguments =
                {
                    shooterRoot,
                    new Vector3(10f, 1f, 0f),
                    targetRoot,
                    settings,
                    Vector3.zero
                };

                bool hasLineOfFire = (bool)method.Invoke(checker, arguments);
                Assert.IsTrue(hasLineOfFire);
                Vector3 selectedAimPoint = (Vector3)arguments[4];
                Assert.That(Mathf.Abs(selectedAimPoint.z), Is.GreaterThan(0.5f));
            }
            finally
            {
                UnityEngine.Object.Destroy(shooterObject);
                UnityEngine.Object.Destroy(targetObject);
                UnityEngine.Object.Destroy(wallObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MatchVisibilitySelectsAVisibleTargetProbeAroundCover()
        {
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wallObject.name = "BotVisibilityRegressionWall";
                wallObject.layer = 8;
                wallObject.transform.SetPositionAndRotation(new Vector3(0f, 1.4f, 2.5f), Quaternion.identity);
                wallObject.transform.localScale = new Vector3(0.4f, 3f, 0.4f);

                Type serviceType = Type.GetType("Game.Scripts.Networking.Lobby.MatchVisibilityService, Assembly-CSharp");
                Type settingsType = Type.GetType("Game.Scripts.Server.MatchVisibilityGlobalSettings, Assembly-CSharp");
                Assert.IsNotNull(serviceType);
                Assert.IsNotNull(settingsType);

                object service = Activator.CreateInstance(serviceType);
                object settings = Activator.CreateInstance(settingsType);
                settingsType.GetField("lineOfSightMask", BindingFlags.Public | BindingFlags.Instance)
                    .SetValue(settings, (LayerMask)(1 << 8));
                settingsType.GetField("targetProbeHeight", BindingFlags.Public | BindingFlags.Instance)
                    .SetValue(settings, 1.4f);
                settingsType.GetField("targetProbeRadius", BindingFlags.Public | BindingFlags.Instance)
                    .SetValue(settings, 0.75f);
                settingsType.GetField("lineOfSightRecheckSeconds", BindingFlags.Public | BindingFlags.Instance)
                    .SetValue(settings, 0.5f);

                MethodInfo method = serviceType.GetMethod(
                    "HasCachedLineOfSight",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(method);
                Physics.SyncTransforms();

                object[] arguments =
                {
                    101,
                    202,
                    new Vector3(0f, 2f, 0f),
                    new Vector3(0f, 0f, 5f),
                    0f,
                    settings,
                    1f,
                    16
                };

                bool visible = (bool)method.Invoke(service, arguments);
                Assert.IsTrue(visible);
                Assert.That((int)arguments[7], Is.EqualTo(14));
            }
            finally
            {
                UnityEngine.Object.Destroy(wallObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator BotGunPitchUsesSmoothFrameRateIndependentConvergence()
        {
            Type aimType = Type.GetType("Game.Scripts.Gameplay.Robots.WeaponAimController, Assembly-CSharp");
            Assert.IsNotNull(aimType);

            MethodInfo smoothAngle = aimType.GetMethod(
                "SmoothAngle",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(smoothAngle);

            float frameAt60Fps = (float)smoothAngle.Invoke(null, new object[] { 0f, 90f, 10f, 1f / 60f });
            float frameAt30Fps = (float)smoothAngle.Invoke(null, new object[] { 0f, 90f, 10f, 1f / 30f });
            float twoFramesAt60Fps = (float)smoothAngle.Invoke(
                null,
                new object[] { frameAt60Fps, 90f, 10f, 1f / 60f });

            Assert.That(frameAt60Fps, Is.GreaterThan(0f));
            Assert.That(frameAt60Fps, Is.LessThan(90f));
            Assert.That(twoFramesAt60Fps, Is.EqualTo(frameAt30Fps).Within(0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BotSteeringSuppressesMicroOscillationAndSmoothsDirectionChanges()
        {
            Type navigatorType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotNavigator, Assembly-CSharp");
            Assert.IsNotNull(navigatorType);

            MethodInfo resolveTurn = navigatorType.GetMethod(
                "ResolveSteeringTurn",
                BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo smoothTurn = navigatorType.GetMethod(
                "SmoothSteeringTurn",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(resolveTurn);
            Assert.IsNotNull(smoothTurn);

            float deadZoneTurn = (float)resolveTurn.Invoke(null, new object[] { 2f, 90f, 4f });
            float positiveTurn = (float)resolveTurn.Invoke(null, new object[] { 30f, 90f, 4f });
            float negativeTurn = (float)resolveTurn.Invoke(null, new object[] { -30f, 90f, 4f });
            Assert.That(deadZoneTurn, Is.EqualTo(0f).Within(0.001f));
            Assert.That(positiveTurn, Is.GreaterThan(0f));
            Assert.That(negativeTurn, Is.LessThan(0f));

            float firstTurn = (float)smoothTurn.Invoke(null, new object[] { 0f, 1f, 7f, 0.25f });
            float oppositeTurn = (float)smoothTurn.Invoke(null, new object[] { firstTurn, -1f, 7f, 0.25f });
            Assert.That(firstTurn, Is.GreaterThan(0f));
            Assert.That(firstTurn, Is.LessThan(1f));
            Assert.That(oppositeTurn, Is.GreaterThan(-1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BotTurretHoldsInsideMicroYawDeadZone()
        {
            Type turretType = Type.GetType(
                "Game.Scripts.Gameplay.Robots.VehicleTurretRotationController, Assembly-CSharp");
            Assert.IsNotNull(turretType);

            MethodInfo shouldHoldYaw = turretType.GetMethod(
                "ShouldHoldTargetYaw",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(shouldHoldYaw);

            bool holdsSmallError = (bool)shouldHoldYaw.Invoke(null, new object[] { 0f, 0.2f, 0.25f });
            bool holdsWrappedSmallError = (bool)shouldHoldYaw.Invoke(null, new object[] { 179.9f, -179.9f, 0.25f });
            bool movesForRealError = (bool)shouldHoldYaw.Invoke(null, new object[] { 0f, 1f, 0.25f });
            Assert.IsTrue(holdsSmallError);
            Assert.IsTrue(holdsWrappedSmallError);
            Assert.IsFalse(movesForRealError);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BotPivotCountsOnlyRealYawProgressAsMovement()
        {
            Type navigatorType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotNavigator, Assembly-CSharp");
            Assert.IsNotNull(navigatorType);

            MethodInfo hasTurnProgress = navigatorType.GetMethod(
                "HasTurnProgress",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(hasTurnProgress);

            bool smallYawChange = (bool)hasTurnProgress.Invoke(null, new object[] { 2f, 4f });
            bool wrappedYawChange = (bool)hasTurnProgress.Invoke(null, new object[] { 3f, 4f });
            bool realYawChange = (bool)hasTurnProgress.Invoke(null, new object[] { 6f, 4f });
            Assert.IsFalse(smallYawChange);
            Assert.IsFalse(wrappedYawChange);
            Assert.IsTrue(realYawChange);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProximityMaskAlwaysIncludesDeathChassisLayer()
        {
            int chassisLayer = LayerMask.NameToLayer("Chassis");
            Assert.That(chassisLayer, Is.GreaterThanOrEqualTo(0));

            Type navigatorType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotNavigator, Assembly-CSharp");
            Assert.IsNotNull(navigatorType);
            MethodInfo getMask = navigatorType.GetMethod(
                "GetProximityMask",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getMask);

            LayerMask result = (LayerMask)getMask.Invoke(null, new object[] { (LayerMask)0 });
            Assert.That(result.value & (1 << chassisLayer), Is.Not.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetachedDeathChassisColliderIsAnObstacle()
        {
            int chassisLayer = LayerMask.NameToLayer("Chassis");
            Assert.That(chassisLayer, Is.GreaterThanOrEqualTo(0));

            GameObject debrisObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Rigidbody debrisRigidbody = debrisObject.AddComponent<Rigidbody>();
            debrisObject.layer = chassisLayer;
            try
            {
                Type navigatorType = Type.GetType("Game.Scripts.AI.WaypointGraph.BotNavigator, Assembly-CSharp");
                Assert.IsNotNull(navigatorType);
                MethodInfo isDeathDebris = navigatorType.GetMethod(
                    "IsDeathDebrisCollider",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(isDeathDebris);

                Collider collider = debrisObject.GetComponent<Collider>();
                bool isObstacle = (bool)isDeathDebris.Invoke(null, new object[] { collider });
                Assert.IsTrue(isObstacle);
            }
            finally
            {
                UnityEngine.Object.Destroy(debrisRigidbody);
                UnityEngine.Object.Destroy(debrisObject);
            }

            yield return null;
        }
    }
}
