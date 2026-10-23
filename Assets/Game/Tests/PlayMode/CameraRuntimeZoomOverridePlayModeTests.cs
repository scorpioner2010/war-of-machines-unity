using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    public sealed class CameraRuntimeZoomOverridePlayModeTests
    {
        [Test]
        public void EnabledOverrideMovesCameraToConfiguredRuntimeZoomIndex()
        {
            GameObject settingsObject = new GameObject("ClientSettingsRuntimeZoomOverrideTest");
            GameObject cameraObject = new GameObject("CameraRuntimeZoomOverrideTest");
            FieldInfo clientSettingsIn = null;
            object previousClientSettings = null;

            try
            {
                Type clientSettingsType = GetType("Game.Scripts.Client.ClientSettings, Assembly-CSharp");
                Type runtimeSettingsType = GetType("Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp");
                Type profileType = GetType("Game.Scripts.Client.CameraZoomProfile, Assembly-CSharp");
                Type cameraControllerType = GetType("Game.Scripts.Gameplay.Robots.CameraController, Assembly-CSharp");

                clientSettingsIn = clientSettingsType.GetField("In", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(clientSettingsIn);
                previousClientSettings = clientSettingsIn.GetValue(null);

                Component clientSettings = settingsObject.AddComponent(clientSettingsType);
                object runtimeSettings = CreateRuntimeSettings(runtimeSettingsType, profileType);
                SetField(runtimeSettings, "runtimeZoomOverrideEnabled", true);
                SetField(runtimeSettings, "runtimeZoomIndex", 0);
                SetField(clientSettings, "gameplayRuntime", runtimeSettings);

                Component cameraController = cameraObject.AddComponent(cameraControllerType);
                MethodInfo applyOverride = cameraControllerType.GetMethod(
                    "ApplyRuntimeZoomOverride",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo currentZoomStep = cameraControllerType.GetProperty(
                    "CurrentZoomStep",
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(applyOverride);
                Assert.IsNotNull(currentZoomStep);

                SetField(runtimeSettings, "runtimeZoomIndex", 1);
                bool applied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsTrue(applied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));

                SetField(runtimeSettings, "runtimeZoomIndex", 99);
                bool invalidApplied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsFalse(invalidApplied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));

                SetField(runtimeSettings, "runtimeZoomOverrideEnabled", false);
                bool disabledApplied = (bool)applyOverride.Invoke(cameraController, new object[] { false });

                Assert.IsFalse(disabledApplied);
                Assert.That((int)currentZoomStep.GetValue(cameraController), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(settingsObject);
                if (clientSettingsIn != null)
                {
                    clientSettingsIn.SetValue(null, previousClientSettings);
                }
            }
        }

        [Test]
        public void RuntimeZoomOverrideFieldsSurviveValidationAndCopy()
        {
            Type runtimeSettingsType = GetType("Game.Scripts.Client.GameplayRuntimeSettings, Assembly-CSharp");
            object source = Activator.CreateInstance(runtimeSettingsType);
            object target = Activator.CreateInstance(runtimeSettingsType);

            SetField(source, "runtimeZoomOverrideEnabled", true);
            SetField(source, "runtimeZoomIndex", -2);
            Invoke(source, "Validate");

            Invoke(target, "CopyFrom", source);

            Assert.IsTrue((bool)GetField(source, "runtimeZoomOverrideEnabled"));
            Assert.That((int)GetField(source, "runtimeZoomIndex"), Is.EqualTo(-2));
            Assert.IsTrue((bool)GetField(target, "runtimeZoomOverrideEnabled"));
            Assert.That((int)GetField(target, "runtimeZoomIndex"), Is.EqualTo(-2));
        }

        private static object CreateRuntimeSettings(Type runtimeSettingsType, Type profileType)
        {
            object runtimeSettings = Activator.CreateInstance(runtimeSettingsType);
            Array profiles = Array.CreateInstance(profileType, 2);
            object defaultProfile = Activator.CreateInstance(profileType);
            object forwardProfile = Activator.CreateInstance(profileType);
            SetField(defaultProfile, "zoomIndex", 0);
            SetField(defaultProfile, "cameraDistance", 10f);
            SetField(defaultProfile, "fieldOfView", 60f);
            SetField(forwardProfile, "zoomIndex", 1);
            SetField(forwardProfile, "cameraDistance", 6f);
            SetField(forwardProfile, "fieldOfView", 50f);
            profiles.SetValue(defaultProfile, 0);
            profiles.SetValue(forwardProfile, 1);
            SetField(runtimeSettings, "cameraZoomProfiles", profiles);
            return runtimeSettings;
        }

        private static Type GetType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, assemblyQualifiedName);
            return type;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(target);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, methodName);
            return method.Invoke(target, arguments);
        }
    }
}
