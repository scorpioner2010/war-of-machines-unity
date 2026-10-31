#if UNITY_EDITOR
using System;
using System.IO;
using Game.Rendering;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class DecalWindowsSmokeBuild
    {
        private const string ScenePath = "Assets/Game/Scenes/DecalWindowsSmoke.unity";
        private const string WallMaterialPath = "Assets/Game/Content/Combat/Projectiles/ImpactOtherObject/Materials/DecalWindowsSmokeWall.mat";
        private const string DecalMaterialPath = "Assets/Game/Content/Combat/Projectiles/ImpactOtherObject/Materials/ImpactDecal.mat";
        private const string BuildDirectoryName = "DecalWindowsSmoke";
        private const string ExecutableName = "WarOfMachinesDecalWindowsSmoke.exe";

        [MenuItem("War Of Machines/Local Build/Windows Decal Smoke Test")]
        public static void Build()
        {
            string scenePath = CreateScene();
            string outputDirectory = GetOutputDirectory();
            PrepareOutputDirectory(outputDirectory);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = Path.Combine(outputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows decal smoke build failed.");

            Debug.Log($"Windows decal smoke build created at {outputDirectory}.");
        }

        private static string CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Decal Smoke Wall";
            wall.transform.SetPositionAndRotation(new Vector3(0f, 0f, 2f), Quaternion.identity);
            wall.transform.localScale = new Vector3(6f, 4f, 0.3f);
            wall.GetComponent<Renderer>().renderingLayerMask = 3;
            wall.GetComponent<Renderer>().sharedMaterial = GetWallMaterial();

            GameObject decalObject = new GameObject("Impact Decal");
            decalObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, 1.82f), Quaternion.identity);
            DecalProjector decal = decalObject.AddComponent<DecalProjector>();
            decal.material = AssetDatabase.LoadAssetAtPath<Material>(DecalMaterialPath);
            decal.size = new Vector3(2.2f, 2.2f, 0.4f);
            decal.pivot = Vector3.zero;
            decal.startAngleFade = 0f;
            decal.endAngleFade = 90f;
            decal.renderingLayerMask = 2;
            decal.fadeFactor = 1f;
            decal.drawDistance = 1000f;

            GameObject cameraObject = new GameObject("Decal Smoke Camera");
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -8f), Quaternion.identity);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.03f, 0.03f);
            camera.fieldOfView = 35f;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;

            GameObject lightObject = new GameObject("Decal Smoke Light");
            lightObject.transform.SetPositionAndRotation(new Vector3(-2f, 3f, -4f), Quaternion.Euler(25f, -25f, 0f));
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;

            GameObject captureObject = new GameObject("Decal Smoke Capture");
            captureObject.AddComponent<DecalWindowsSmokeCapture>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            return ScenePath;
        }

        private static Material GetWallMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { name = "DecalWindowsSmokeWall" };
            material.SetColor("_BaseColor", new Color(0.65f, 0.65f, 0.65f));
            AssetDatabase.CreateAsset(material, WallMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static string GetOutputDirectory()
        {
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectDirectory, "..", "war-of-machines-local-builds", BuildDirectoryName));
        }

        private static void PrepareOutputDirectory(string outputDirectory)
        {
            string normalizedOutputDirectory = Path.GetFullPath(outputDirectory);
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            string buildsDirectory = Path.GetFullPath(Path.Combine(projectDirectory, "..", "war-of-machines-local-builds"));
            string expectedPrefix = buildsDirectory + Path.DirectorySeparatorChar;

            if (!normalizedOutputDirectory.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Decal smoke output is outside the approved build directory.");

            if (Directory.Exists(normalizedOutputDirectory))
                Directory.Delete(normalizedOutputDirectory, true);

            Directory.CreateDirectory(normalizedOutputDirectory);
        }
    }
}
#endif
