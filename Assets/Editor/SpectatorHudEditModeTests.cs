using System.Reflection;
using RAV.Scripts.Gameplay.Robots;
using RAV.Scripts.UI.HUD;
using RAV.Scripts.UI.Settings;
using NUnit.Framework;
using UnityEngine;

namespace RAV.Editor.Tests
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

        [TestCase(true, ExpectedResult = true)]
        [TestCase(false, ExpectedResult = false)]
        public bool AimingCircleVisibilityMatchesSpectatorState(bool deathSpectatorActive)
        {
            MethodInfo method = typeof(GunCrosshair).GetMethod(
                "ShouldHideSpectatorAimingCircle",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            return (bool)method.Invoke(null, new object[] { deathSpectatorActive });
        }

        [TestCase(-0.2f, ExpectedResult = 0f)]
        [TestCase(0.35f, ExpectedResult = 0.35f)]
        [TestCase(1.2f, ExpectedResult = 1f)]
        public float ReticleOpacityClampsToValidRange(float value)
        {
            return ClientGameplaySettings.ClampReticleOpacity(value);
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
