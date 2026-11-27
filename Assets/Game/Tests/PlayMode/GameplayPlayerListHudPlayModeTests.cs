using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using System.Runtime.Serialization;
using UnityEngine;

namespace RAV.Tests.PlayMode
{
    public sealed class GameplayPlayerListHudPlayModeTests
    {
        private GameObject _hudObject;

        [TearDown]
        public void TearDown()
        {
            if (_hudObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hudObject);
            }
        }

        [Test]
        public void BeginBattleClearsRowsAndIndexesFromThePreviousBattle()
        {
            Type hudType = Type.GetType(
                "RAV.Scripts.UI.HUD.GameplayPlayerListHud, Assembly-CSharp");
            Assert.IsNotNull(hudType);

            _hudObject = new GameObject("GameplayPlayerListHudRegression");
            Component hud = _hudObject.AddComponent(hudType);

            Type rowType = hudType.GetNestedType(
                "PlayerListRow",
                BindingFlags.NonPublic);
            Assert.IsNotNull(rowType);

            object retainedRow = CreateRow(rowType);
            IList rows = GetField<IList>(hud, "_rows");
            IDictionary rowsByPlayerId = GetField<IDictionary>(hud, "_rowsByPlayerId");
            IDictionary rowsByVehicleRoot = GetField<IDictionary>(hud, "_rowsByVehicleRoot");
            rows.Add(retainedRow);
            rowsByPlayerId.Add("previous-battle-player", retainedRow);

            InvokeMethod(hud, "BeginBattle");

            Assert.That(rows.Count, Is.EqualTo(0));
            Assert.That(rowsByPlayerId.Count, Is.EqualTo(0));
            Assert.That(rowsByVehicleRoot.Count, Is.EqualTo(0));
        }

        [Test]
        public void LocalVehicleDeathRowSurvivesDespawnUntilTheNextBattle()
        {
            Type hudType = Type.GetType(
                "RAV.Scripts.UI.HUD.GameplayPlayerListHud, Assembly-CSharp");
            Assert.IsNotNull(hudType);

            _hudObject = new GameObject("GameplayPlayerListHudLocalVehicleRegression");
            Component hud = _hudObject.AddComponent(hudType);
            Type rowType = hudType.GetNestedType(
                "PlayerListRow",
                BindingFlags.NonPublic);
            Assert.IsNotNull(rowType);

            object localRow = CreateRow(rowType);
            SetField(localRow, "PlayerId", "local-player");
            SetField(localRow, "CurrentHealth", 0f);
            SetField(localRow, "IsDead", true);

            IList rows = GetField<IList>(hud, "_rows");
            IDictionary rowsByPlayerId = GetField<IDictionary>(hud, "_rowsByPlayerId");
            rows.Add(localRow);
            rowsByPlayerId.Add("local-player", localRow);

            InvokeMethod(hud, "OnLocalPlayerVehicleChanged", (object)null);

            Assert.That(rows.Count, Is.EqualTo(1));
            Assert.That(GetField<bool>(localRow, "IsDead"), Is.True);
            Assert.That(GetField<float>(localRow, "CurrentHealth"), Is.EqualTo(0f));
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing field " + fieldName + ".");
            return (T)field.GetValue(target);
        }

        private static object CreateRow(Type rowType)
        {
            return FormatterServices.GetUninitializedObject(rowType);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing field " + fieldName + ".");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Missing method " + methodName + ".");
            method.Invoke(target, args);
        }
    }
}
