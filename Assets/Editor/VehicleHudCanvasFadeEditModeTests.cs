using System.Reflection;
using Game.Scripts.Gameplay.Robots;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    public sealed class VehicleHudCanvasFadeEditModeTests
    {
        private static readonly string[] ProductionPrefabPaths =
        {
            "Assets/Game/Content/Robots/IA/IA_L1_Starter/IA_L1_Starter.prefab",
            "Assets/Game/Content/Robots/IA/IA_L2_Colossus/IA_L2_Colossus.prefab",
            "Assets/Game/Content/Robots/IA/IA_L2_Guardian/IA_L2_Guardian.prefab",
            "Assets/Game/Content/Robots/IA/IA_L2_Scout/IA_L2_Scout.prefab",
            "Assets/Game/Content/Robots/NV/NV_L1_Starter/NV_L1_Starter.prefab",
            "Assets/Game/Content/Robots/NV/NV_L2_Colossus/NV_L2_Colossus.prefab",
            "Assets/Game/Content/Robots/NV/NV_L2_Guardian/NV_L2_Guardian.prefab",
            "Assets/Game/Content/Robots/NV/NV_L2_Scout/NV_L2_Scout.prefab"
        };

        [Test]
        public void ProductionVehicleHudPrefabsKeepCanvasGroupReference()
        {
            FieldInfo canvasGroupField = typeof(VehicleHUD).GetField(
                "nameCanvasGroup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(canvasGroupField);

            for (int i = 0; i < ProductionPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionPrefabPaths[i]);
                Assert.IsNotNull(prefab, ProductionPrefabPaths[i]);

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Assert.IsNotNull(instance, ProductionPrefabPaths[i]);

                VehicleHUD hud = instance.GetComponentInChildren<VehicleHUD>(true);
                CanvasGroup group = hud != null ? hud.GetComponent<CanvasGroup>() : null;
                CanvasGroup referencedGroup = hud != null
                    ? canvasGroupField.GetValue(hud) as CanvasGroup
                    : null;
                Assert.IsNotNull(hud, ProductionPrefabPaths[i]);
                Assert.IsNotNull(group, ProductionPrefabPaths[i]);
                Assert.AreSame(group, referencedGroup, ProductionPrefabPaths[i]);

                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void NameCanvasUsesCameraDistanceForAlpha()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Content/UI/Prefabs/World/NameCanvas.prefab");
            Assert.IsNotNull(prefab);

            GameObject canvasObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            GameObject cameraObject = new GameObject("VehicleHudCanvasFadeTestCamera");
            VehicleHUD hud = canvasObject.GetComponent<VehicleHUD>();
            CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
            Camera camera = cameraObject.AddComponent<Camera>();

            Assert.IsNotNull(hud);
            Assert.IsNotNull(group);

            canvasObject.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(4f, 0f, 0f);
            hud.SetCamera(camera);
            Assert.That(group.alpha, Is.EqualTo(0.5f).Within(0.02f));

            cameraObject.transform.position = new Vector3(3f, 0f, 0f);
            hud.SetCamera(camera);
            Assert.That(group.alpha, Is.EqualTo(0f).Within(0.02f));

            cameraObject.transform.position = new Vector3(6f, 0f, 0f);
            hud.SetCamera(camera);
            Assert.That(group.alpha, Is.EqualTo(1f).Within(0.02f));

            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
