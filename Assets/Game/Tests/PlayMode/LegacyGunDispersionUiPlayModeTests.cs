using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class LegacyGunDispersionUiPlayModeTests
    {
        [Test]
        public void LegacyDiameterInterpolatesWithZoom()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 200f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxDistance", 10f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxZoom", 30f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxDistance", 5f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxZoom", 15f);

            float distantDiameter = GetUiDiameter(settings, 2f, 1f, 0f);
            float zoomedDiameter = GetUiDiameter(settings, 2f, 1f, 1f);

            Assert.That(distantDiameter, Is.EqualTo(35f).Within(0.001f));
            Assert.That(zoomedDiameter, Is.EqualTo(65f).Within(0.001f));
        }

        [Test]
        public void LegacyDiameterClampsToConfiguredMaximum()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 100f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxZoom", 50f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxDistance", 50f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxZoom", 50f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxDistance", 50f);

            float diameter = GetUiDiameter(settings, 10f, 1f, 1f);

            Assert.That(diameter, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void ZoomProfileDiameterIsFullyAimedBaseAndBloomed()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 200f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxDistance", 10f);
            SetField(settings, "uiFullyAimedPixelsPerDegreeAtMaxZoom", 30f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxDistance", 5f);
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxZoom", 15f);

            float nearMinimumDiameter = GetUiDiameter(settings, 1f, 1f, 1f, 1.01f);
            float fullyAimedDiameter = GetUiDiameter(settings, 1f, 1f, 1f, 20f);
            float bloomedDiameter = GetUiDiameter(settings, 2f, 1f, 1f, 20f);
            float maximumDiameter = GetUiDiameter(settings, 1f, 1f, 1f, 340f);
            float oversizedDiameter = GetUiDiameter(settings, 1f, 1f, 1f, 400f);
            float oversizedBloomedDiameter = GetUiDiameter(settings, 2f, 1f, 1f, 400f);

            Assert.That(nearMinimumDiameter, Is.EqualTo(1.01f).Within(0.001f));
            Assert.That(fullyAimedDiameter, Is.EqualTo(20f).Within(0.001f));
            Assert.That(bloomedDiameter, Is.EqualTo(35f).Within(0.001f));
            Assert.That(maximumDiameter, Is.EqualTo(340f).Within(0.001f));
            Assert.That(oversizedDiameter, Is.EqualTo(400f).Within(0.001f));
            Assert.That(oversizedBloomedDiameter, Is.EqualTo(415f).Within(0.001f));
        }

        [Test]
        public void ZoomProfileDiameterCanBeBelowGlobalMinimum()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 200f);

            float diameter = GetUiDiameter(settings, 0f, 0f, 0f, 0.5f);

            Assert.That(diameter, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void ZoomProfileTinyDiameterStillGrowsWithBloom()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 200f);

            const float configuredDiameter = 0.000001f;
            float diameter = GetUiDiameter(settings, 10f, 1f, 1f, configuredDiameter);

            Assert.That(diameter, Is.EqualTo(135.000001f).Within(0.001f));
        }

        [Test]
        public void ZoomProfileZeroDiameterStillGrowsWithBloom()
        {
            object settings = CreateSettings();
            SetField(settings, "uiBloomPixelsPerDegreeAtMaxZoom", 15f);

            float diameter = GetUiDiameter(settings, 2f, 1f, 1f, 0f);

            Assert.That(diameter, Is.EqualTo(15f).Within(0.001f));
        }

        [Test]
        public void TinyZoomProfileDiameterRendersAsOnePixel()
        {
            object settings = CreateSettings();
            SetField(settings, "uiMinDiameter", 20f);
            SetField(settings, "uiMaxDiameter", 200f);

            const float configuredDiameter = 0.000001f;
            float calculatedDiameter = GetUiDiameter(settings, 1f, 1f, 1f, configuredDiameter);
            GameObject gameObject = new GameObject("GunCrosshairTinyDiameterTest");
            try
            {
                RectTransform target = gameObject.AddComponent<RectTransform>();
                target.sizeDelta = new Vector2(100f, 100f);
                Type crosshairType = Type.GetType("Game.Scripts.UI.HUD.GunCrosshair, Assembly-CSharp");
                Assert.IsNotNull(crosshairType);
                MethodInfo method = crosshairType.GetMethod(
                    "ApplyAimingDiameter",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(method);

                method.Invoke(
                    null,
                    new object[] { target, calculatedDiameter, 18f, Vector2.zero, false, 100f, true });

                Assert.That(target.sizeDelta, Is.EqualTo(Vector2.one));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GunCrosshairClampsZeroDiameterToOnePixel()
        {
            GameObject gameObject = new GameObject("GunCrosshairDiameterTest");
            try
            {
                RectTransform target = gameObject.AddComponent<RectTransform>();
                target.sizeDelta = new Vector2(100f, 100f);
                Type crosshairType = Type.GetType("Game.Scripts.UI.HUD.GunCrosshair, Assembly-CSharp");
                Assert.IsNotNull(crosshairType);
                MethodInfo method = crosshairType.GetMethod(
                    "ApplyAimingDiameter",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(method);

                method.Invoke(
                    null,
                    new object[] { target, 0f, 18f, Vector2.zero, false, 100f, true });

                Assert.That(target.sizeDelta, Is.EqualTo(Vector2.one));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GunCrosshairPreservesConfiguredProfileDiameter()
        {
            GameObject gameObject = new GameObject("GunCrosshairConfiguredDiameterTest");
            try
            {
                RectTransform target = gameObject.AddComponent<RectTransform>();
                target.sizeDelta = new Vector2(100f, 100f);
                Type crosshairType = Type.GetType("Game.Scripts.UI.HUD.GunCrosshair, Assembly-CSharp");
                Assert.IsNotNull(crosshairType);
                MethodInfo method = crosshairType.GetMethod(
                    "ApplyAimingDiameter",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(method);

                method.Invoke(
                    null,
                    new object[] { target, 1.01f, 0f, Vector2.zero, false, 100f, true });
                Assert.That(target.sizeDelta, Is.EqualTo(new Vector2(1.01f, 1.01f)));

                method.Invoke(
                    null,
                    new object[] { target, 20f, 0f, Vector2.zero, false, 100f, true });
                Assert.That(target.sizeDelta, Is.EqualTo(new Vector2(20f, 20f)));

                method.Invoke(
                    null,
                    new object[] { target, 400f, 0f, Vector2.zero, false, 100f, true });
                Assert.That(target.sizeDelta, Is.EqualTo(new Vector2(400f, 400f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object CreateSettings()
        {
            Type settingsType = Type.GetType("Game.Scripts.Gameplay.Robots.GunDispersionGlobalSettings, Assembly-CSharp");
            Assert.IsNotNull(settingsType);
            return Activator.CreateInstance(settingsType);
        }

        private static void SetField(object settings, string fieldName, float value)
        {
            FieldInfo field = settings.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(settings, value);
        }

        private static float GetUiDiameter(object settings, float dispersionDeg, float fullyAimedDispersionDeg, float cameraZoom01)
        {
            MethodInfo method = settings.GetType().GetMethod(
                "GetUiDiameter",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(float), typeof(float), typeof(float) },
                null);
            Assert.IsNotNull(method);
            return (float)method.Invoke(settings, new object[] { dispersionDeg, fullyAimedDispersionDeg, cameraZoom01 });
        }

        private static float GetUiDiameter(
            object settings,
            float dispersionDeg,
            float fullyAimedDispersionDeg,
            float cameraZoom01,
            float aimingCircleDiameter)
        {
            MethodInfo method = settings.GetType().GetMethod(
                "GetUiDiameter",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(float), typeof(float), typeof(float), typeof(float) },
                null);
            Assert.IsNotNull(method);
            return (float)method.Invoke(
                settings,
                new object[] { dispersionDeg, fullyAimedDispersionDeg, cameraZoom01, aimingCircleDiameter });
        }
    }
}
