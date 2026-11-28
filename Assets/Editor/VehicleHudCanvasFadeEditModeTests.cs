using System.Reflection;
using RAV.Scripts.Gameplay.Robots;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RAV.Editor.Tests
{
    public sealed class VehicleHudCanvasFadeEditModeTests
    {
        private static readonly string[] ProductionPrefabPaths =
        {
            "Assets/Game/Content/Robots/IronAlliance/IA_L1_Starter/TZ-18.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Colossus/YUG Titan.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Guardian/TZ-42.prefab",
            "Assets/Game/Content/Robots/IronAlliance/IA_L2_Scout/TZ-23 101F.prefab",
            "Assets/Game/Content/Robots/NovaSyndicate/NV_L1_Starter/N-16 Whitecap.prefab",
            "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Colossus/Cormorant 8C.prefab",
            "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Guardian/ARGUS-7B.prefab",
            "Assets/Game/Content/Robots/NovaSyndicate/NV_L2_Scout/N-17.prefab"
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
