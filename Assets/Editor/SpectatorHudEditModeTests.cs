using System.Reflection;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.HUD;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
{
    public sealed class SpectatorHudEditModeTests
    {
        [TestCase(true, true, true, ExpectedResult = true)]
        [TestCase(true, true, false, ExpectedResult = false)]
        [TestCase(true, false, true, ExpectedResult = false)]
        [TestCase(false, true, true, ExpectedResult = false)]
        public bool CrosshairVisibilityMatchesSpectatorTargetState(
            bool deathSpectatorActive,
            bool isLocalTarget,
            bool targetIsDead)
        {
            MethodInfo method = typeof(GunCrosshair).GetMethod(
                "ShouldHideSpectatorCrosshairState",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            return (bool)method.Invoke(
                null,
                new object[] { deathSpectatorActive, isLocalTarget, targetIsDead });
        }

        [Test]
        public void SpeedPresenterUsesSelectedVehicleMovementTransform()
        {
            GameObject vehicleObject = new GameObject("SpectatorSpeedVehicle");
            GameObject moverObject = new GameObject("SpectatorSpeedMover");
            try
            {
                VehicleRoot vehicleRoot = vehicleObject.AddComponent<VehicleRoot>();
                VehicleMovementController movement = moverObject.AddComponent<VehicleMovementController>();
                vehicleRoot.objectMover = movement;

                MethodInfo method = typeof(VehicleHudInitializer).GetMethod(
                    "ResolveDisplayTransform",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(method);

                Transform result = method.Invoke(
                    null,
                    new object[] { vehicleRoot }) as Transform;
                Assert.AreSame(moverObject.transform, result);
            }
            finally
            {
                Object.DestroyImmediate(vehicleObject);
                Object.DestroyImmediate(moverObject);
            }
        }
    }
}
