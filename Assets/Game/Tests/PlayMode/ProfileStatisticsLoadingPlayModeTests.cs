using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RAV.Tests.PlayMode
{
    public sealed class ProfileStatisticsLoadingPlayModeTests
    {
        private GameObject _overlayObject;
        private GameObject _serviceObject;

        [TearDown]
        public void TearDown()
        {
            Type overlayType = GetOverlayType();
            InvokeStaticMethod(overlayType, "SetSceneLoadingSuppressed", false);

            if (_serviceObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_serviceObject);
            }

            if (_overlayObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_overlayObject);
            }
        }

        [Test]
        public void ProfileStatisticsLoadingScopeControlsStandardOverlay()
        {
            Type overlayType = GetOverlayType();
            Assert.IsNotNull(overlayType);
            InvokeStaticMethod(overlayType, "SetSceneLoadingSuppressed", false);

            _overlayObject = new GameObject("ProfileStatisticsLoadingOverlayRegression");
            Component overlay = _overlayObject.AddComponent(overlayType);
            GameObject loadingObject = new GameObject("StandardLoading");
            loadingObject.transform.SetParent(_overlayObject.transform, false);
            loadingObject.SetActive(false);
            SetField(overlay, "standardLoading", loadingObject);

            _serviceObject = new GameObject("ProfileStatisticsLoadingServiceRegression");
            Type profileServerType = Type.GetType(
                "RAV.Scripts.API.ServerManagers.ProfileServer, Assembly-CSharp");
            Assert.IsNotNull(profileServerType);
            Component profileServer = _serviceObject.AddComponent(profileServerType);

            MethodInfo beginMethod = profileServerType.GetMethod(
                "BeginProfileStatisticsLoadingScope",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo endMethod = profileServerType.GetMethod(
                "EndProfileStatisticsLoadingScope",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);
            Assert.IsNotNull(beginMethod);
            Assert.IsNotNull(endMethod);

            int requestVersion = (int)beginMethod.Invoke(profileServer, null);

            Assert.IsTrue(GetStaticProperty<bool>(overlayType, "IsOpen"));
            Assert.IsTrue(loadingObject.activeSelf);

            endMethod.Invoke(profileServer, new object[] { requestVersion });

            Assert.IsFalse(GetStaticProperty<bool>(overlayType, "IsOpen"));
            Assert.IsFalse(loadingObject.activeSelf);
        }

        private static Type GetOverlayType()
        {
            return Type.GetType("RAV.Scripts.UI.Helpers.StandardLoadingOverlay, Assembly-CSharp");
        }

        private static T GetStaticProperty<T>(Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(property, "Missing property " + propertyName + ".");
            return (T)property.GetValue(null);
        }

        private static void InvokeStaticMethod(Type type, string methodName, params object[] args)
        {
            if (type == null)
            {
                return;
            }

            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Missing method " + methodName + ".");
            method.Invoke(null, args);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing field " + fieldName + ".");
            field.SetValue(target, value);
        }
    }
}
