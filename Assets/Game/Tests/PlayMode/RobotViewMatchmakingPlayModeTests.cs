using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RAV.Tests.PlayMode
{
    public sealed class RobotViewMatchmakingPlayModeTests
    {
        private GameObject _generatorObject;

        [TearDown]
        public void TearDown()
        {
            if (_generatorObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_generatorObject);
            }
        }

        [Test]
        public void MatchmakingDisablesGeneratorAndRestoresPreviousActiveState()
        {
            Type robotViewType = FindType("RAV.Scripts.UI.MainMenu.RobotView");
            Assert.IsNotNull(robotViewType);

            _generatorObject = new GameObject("RobotViewGeneratorMatchmakingRegression");
            _generatorObject.AddComponent(robotViewType);

            RaiseMenuEvent("OnEnable", "Matchmaking");
            Assert.IsFalse(_generatorObject.activeSelf);

            RaiseMenuEvent("OnEnable", "MainMenu");
            Assert.IsTrue(_generatorObject.activeSelf);

            RaiseMenuEvent("OnEnable", "Matchmaking");
            RaiseMenuEvent("OnDisable", "Matchmaking");
            Assert.IsTrue(_generatorObject.activeSelf);

            _generatorObject.SetActive(false);
            RaiseMenuEvent("OnEnable", "Matchmaking");
            RaiseMenuEvent("OnEnable", "MainMenu");
            Assert.IsFalse(_generatorObject.activeSelf);
        }

        private static Type FindType(string typeName)
        {
            return Type.GetType(typeName + ", Assembly-CSharp");
        }

        private static void RaiseMenuEvent(string eventName, string menuName)
        {
            Type menuManagerType = FindType("RAV.Scripts.MenuController.MenuManager");
            Type menuType = FindType("RAV.Scripts.MenuController.MenuType");
            Assert.IsNotNull(menuManagerType);
            Assert.IsNotNull(menuType);

            FieldInfo eventField = menuManagerType.GetField(
                eventName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(eventField, "Missing menu event " + eventName + ".");

            Delegate callback = eventField.GetValue(null) as Delegate;
            Assert.IsNotNull(callback, "No subscriber for menu event " + eventName + ".");

            object menuValue = Enum.Parse(menuType, menuName);
            callback.DynamicInvoke(menuValue);
        }
    }
}
