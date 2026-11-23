#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RAV.Editor
{
    [InitializeOnLoad]
    public static class LocalNetworkBuild
    {
        private const string ServerExecutableName = "RAVServer.exe";
        private const string ClientExecutableName = "RAVClient.exe";
        private const string ServerBuildRequestFileName = "server-build-request.txt";
        private const string ServerBuildResultFileName = "server-build-result.txt";
        private const string ClientBuildRequestFileName = "client-build-request.txt";
        private const string ClientBuildResultFileName = "client-build-result.txt";
        private const string LinuxServerBuildProfilePath = "Assets/Settings/Build Profiles/Linux Server.asset";

        private static bool externalBuildInProgress;
        private static string lastServerBuildOutputDirectory = string.Empty;
        private static string lastClientBuildOutputDirectory = string.Empty;

        private static readonly string[] ServerScenes =
        {
            "Assets/Game/Scenes/Server.unity",
            "Assets/Game/Scenes/Map.unity"
        };

        private static readonly string[] ClientScenes =
        {
            "Assets/Game/Scenes/Client.unity",
            "Assets/Game/Scenes/Map.unity"
        };

        static LocalNetworkBuild()
        {
            EditorApplication.update -= PollExternalServerBuildRequest;
            EditorApplication.update += PollExternalServerBuildRequest;
            EditorApplication.update -= PollExternalClientBuildRequest;
            EditorApplication.update += PollExternalClientBuildRequest;
        }

        [MenuItem("RAV/Local Build/Windows Server")]
        public static void BuildLocalServer()
        {
            lastServerBuildOutputDirectory = Build("Server-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8), ServerExecutableName, ServerScenes, StandaloneBuildSubtarget.Server);
        }

        [MenuItem("RAV/Local Build/Windows Client")]
        public static void BuildLocalClient()
        {
            lastClientBuildOutputDirectory = Build("Client-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8), ClientExecutableName, ClientScenes, StandaloneBuildSubtarget.Player);
        }

        [MenuItem("RAV/Local Build/Windows Server and Client")]
        public static void BuildLocalServerAndClient()
        {
            BuildLocalServer();
            BuildLocalClient();
        }

        [MenuItem("RAV/Local Build/Linux Dedicated Server (Mono)")]
        public static void BuildLinuxDedicatedServer()
        {
            ConfigureLinuxServerBuildProfile();

            string buildName = "LinuxServer-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string outputDirectory = GetOutputDirectory(buildName);
            if (Directory.Exists(outputDirectory) || File.Exists(outputDirectory))
            {
                throw new InvalidOperationException($"Linux server build output already exists: {outputDirectory}");
            }

            Directory.CreateDirectory(outputDirectory);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.Mono2x);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = ServerScenes,
                locationPathName = Path.Combine(outputDirectory, "RAVServer.x86_64"),
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Linux Dedicated Server build failed. See the Unity Console and Editor.log for details.");
            }

            Debug.Log($"Linux Dedicated Server build created at {outputDirectory}.");
        }

        [MenuItem("RAV/Local Build/Configure Linux Server Profile")]
        public static void ConfigureLinuxServerBuildProfile()
        {
            BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(LinuxServerBuildProfilePath);
            if (profile == null)
            {
                throw new InvalidOperationException($"Linux Server Build Profile was not found at {LinuxServerBuildProfilePath}.");
            }

            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty overrideGlobalSceneList = serializedProfile.FindProperty("m_OverrideGlobalSceneList");
            SerializedProperty scenes = serializedProfile.FindProperty("m_Scenes");
            if (overrideGlobalSceneList == null || scenes == null || scenes.isArray == false)
            {
                throw new InvalidOperationException("Unity's Linux Server Build Profile scene-list serialization changed; no profile settings were saved.");
            }

            overrideGlobalSceneList.boolValue = true;
            scenes.arraySize = ServerScenes.Length;
            for (int i = 0; i < ServerScenes.Length; i++)
            {
                SerializedProperty scene = scenes.GetArrayElementAtIndex(i);
                scene.FindPropertyRelative("m_enabled").boolValue = true;
                scene.FindPropertyRelative("m_path").stringValue = ServerScenes[i];
            }

            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static void PollExternalServerBuildRequest()
        {
            if (externalBuildInProgress || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string requestPath = Path.Combine(GetBuildsDirectory(), ServerBuildRequestFileName);
            if (File.Exists(requestPath) == false)
            {
                return;
            }

            string buildToken;
            try
            {
                buildToken = File.ReadAllText(requestPath).Trim();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read the external server build request: {exception.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(buildToken))
            {
                TryDeleteFile(requestPath);
                return;
            }

            try
            {
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not claim the external server build request: {exception.Message}");
                return;
            }

            externalBuildInProgress = true;
            try
            {
                BuildLocalServer();
                WriteExternalServerBuildResult(buildToken, "SUCCESS", lastServerBuildOutputDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                try
                {
                    WriteExternalServerBuildResult(buildToken, "FAILURE", exception.Message);
                }
                catch (Exception resultException)
                {
                    Debug.LogException(resultException);
                }
            }
            finally
            {
                externalBuildInProgress = false;
            }
        }

        private static void PollExternalClientBuildRequest()
        {
            if (externalBuildInProgress || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string requestPath = Path.Combine(GetBuildsDirectory(), ClientBuildRequestFileName);
            if (File.Exists(requestPath) == false)
            {
                return;
            }

            string buildToken;
            try
            {
                buildToken = File.ReadAllText(requestPath).Trim();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read the external client build request: {exception.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(buildToken))
            {
                TryDeleteFile(requestPath);
                return;
            }

            try
            {
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not claim the external client build request: {exception.Message}");
                return;
            }

            externalBuildInProgress = true;
            try
            {
                BuildLocalClient();
                WriteExternalBuildResult(ClientBuildResultFileName, buildToken, "SUCCESS", lastClientBuildOutputDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                try
                {
                    WriteExternalBuildResult(ClientBuildResultFileName, buildToken, "FAILURE", exception.Message);
                }
                catch (Exception resultException)
                {
                    Debug.LogException(resultException);
                }
            }
            finally
            {
                externalBuildInProgress = false;
            }
        }

        private static void WriteExternalServerBuildResult(string buildToken, string status, string message)
        {
            WriteExternalBuildResult(ServerBuildResultFileName, buildToken, status, message);
        }

        private static void WriteExternalBuildResult(string resultFileName, string buildToken, string status, string message)
        {
            string buildsDirectory = GetBuildsDirectory();
            Directory.CreateDirectory(buildsDirectory);

            string resultPath = Path.Combine(buildsDirectory, resultFileName);
            string temporaryResultPath = resultPath + ".tmp";
            string safeMessage = (message ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace('|', '/');
            string result = $"{buildToken}|{status}|{safeMessage}";

            File.WriteAllText(temporaryResultPath, result, new UTF8Encoding(false));
            if (File.Exists(resultPath))
            {
                File.Replace(temporaryResultPath, resultPath, null);
            }
            else
            {
                File.Move(temporaryResultPath, resultPath);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not delete external server build request: {exception.Message}");
            }
        }

        private static string Build(string buildName, string executableName, string[] scenes, StandaloneBuildSubtarget subtarget)
        {
            string outputDirectory = GetOutputDirectory(buildName);
            PrepareOutputDirectory(outputDirectory);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, executableName),
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)subtarget,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{buildName} build failed. See the Unity Console and Editor.log for details.");
            }

            Debug.Log($"Local {buildName.ToLowerInvariant()} build created at {outputDirectory}.");
            return outputDirectory;
        }

        private static string GetOutputDirectory(string buildName)
        {
            return Path.Combine(GetBuildsDirectory(), buildName);
        }

        private static string GetBuildsDirectory()
        {
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectDirectory, "..", "war-of-machines-local-builds"));
        }

        private static void PrepareOutputDirectory(string outputDirectory)
        {
            string normalizedOutputDirectory = Path.GetFullPath(outputDirectory);
            string normalizedBuildsDirectory = GetBuildsDirectory();
            string expectedPrefix = normalizedBuildsDirectory + Path.DirectorySeparatorChar;

            if (normalizedOutputDirectory.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException("Local build output is outside the approved build directory.");
            }

            if (Directory.Exists(normalizedOutputDirectory) || File.Exists(normalizedOutputDirectory))
            {
                throw new InvalidOperationException($"Build output already exists and will not be overwritten: {normalizedOutputDirectory}");
            }

            Directory.CreateDirectory(normalizedOutputDirectory);
        }
    }
}
#endif
