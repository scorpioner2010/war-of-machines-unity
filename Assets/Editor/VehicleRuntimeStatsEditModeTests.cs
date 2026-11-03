using Game.Scripts.API;
using Game.Scripts.Gameplay.Robots;
using NUnit.Framework;

public sealed class VehicleRuntimeStatsEditModeTests
{
    [Test]
    public void MissingEnginePowerUsesLegacyBaseline()
    {
        VehicleRuntimeStats stats = VehicleRuntimeStats.FromVehicleLite(new VehicleLite
        {
            id = 1,
            code = "IA_Skirmisher"
        });

        Assert.That(stats.EnginePower, Is.EqualTo(VehicleRuntimeStats.DefaultEnginePower));
    }

    [TestCase(700f, 0.7f)]
    [TestCase(860f, 0.86f)]
    [TestCase(360f, 0.36f)]
    [TestCase(1000f, 1f)]
    public void EnginePowerScalesAccelerationFromLegacyBaseline(float enginePower, float expectedMultiplier)
    {
        float scaledAcceleration = VehicleRuntimeStats.ScaleAccelerationByEnginePower(30f, enginePower);

        Assert.That(scaledAcceleration, Is.EqualTo(30f * expectedMultiplier).Within(0.001f));
    }

    [Test]
    public void ClonePreservesNormalizedEnginePower()
    {
        VehicleRuntimeStats stats = VehicleRuntimeStats.FromVehicleLite(new VehicleLite
        {
            id = 1,
            code = "NV_Wisp",
            enginePower = 360f
        });

        VehicleRuntimeStats clone = stats.Clone();

        Assert.That(clone.EnginePower, Is.EqualTo(360f));
    }
}
