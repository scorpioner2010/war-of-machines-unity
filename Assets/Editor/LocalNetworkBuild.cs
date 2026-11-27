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
        private const string WindowsClientBuildProfilePath = "Assets/Game/Content/Configuration/UnitySettings/BuildProfiles/Windows.asset";

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
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerWindowHandler);
            SynchronizeWindowsClientBuildProfile(RAVDevelopmentControlPanel.SelectedClientTarget);
        }

        internal static void SynchronizeWindowsClientBuildProfile(RAVDevelopmentControlPanel.ClientTarget target)
        {
            bool development = target == RAVDevelopmentControlPanel.ClientTarget.Local;
            EditorUserBuildSettings.development = development;

            BuildProfile windowsProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(WindowsClientBuildProfilePath);
            if (windowsProfile != null && !SetProfileDevelopment(windowsProfile, development, false))
            {
                Debug.LogWarning("Could not synchronize the Windows Client Build Profile Development Build setting.");
            }

            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null
                && activeProfile != windowsProfile
                && IsWindowsClientProfile(activeProfile)
                && !SetProfileDevelopment(activeProfile, development, true))
            {
                Debug.LogWarning("Could not synchronize the active Windows Client Build Profile Development Build setting.");
            }
        }

        private static bool IsWindowsClientProfile(BuildProfile profile)
        {
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty buildTarget = serializedProfile.FindProperty("m_BuildTarget");
            SerializedProperty subtarget = serializedProfile.FindProperty("m_Subtarget");
            return buildTarget != null
                && buildTarget.intValue == (int)BuildTarget.StandaloneWindows64
                && subtarget != null
                && subtarget.intValue == (int)StandaloneBuildSubtarget.Player;
        }

        private static bool SetProfileDevelopment(BuildProfile profile, bool development, bool requireClientProfile)
        {
            SerializedObject serializedProfile = new SerializedObject(profile);
            SerializedProperty buildTarget = serializedProfile.FindProperty("m_BuildTarget");
            if (buildTarget == null || buildTarget.intValue != (int)BuildTarget.StandaloneWindows64)
            {
                return false;
            }

            if (requireClientProfile)
            {
                SerializedProperty subtarget = serializedProfile.FindProperty("m_Subtarget");
                if (subtarget == null || subtarget.intValue != (int)StandaloneBuildSubtarget.Player)
                {
                    return false;
                }
            }

            SerializedProperty platformSettings = serializedProfile.FindProperty("m_PlatformBuildProfile");
            SerializedProperty developmentProperty = platformSettings?.FindPropertyRelative("m_Development");
            if (developmentProperty == null)
            {
                return false;
            }

            if (developmentProperty.boolValue != development)
            {
                developmentProperty.boolValue = development;
                serializedProfile.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            return true;
        }

        [MenuItem("RAV/Local Build/Windows Server")]
        public static void BuildLocalServer()
        {
            lastServerBuildOutputDirectory = Build("Server-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8), ServerExecutableName, ServerScenes, StandaloneBuildSubtarget.Server);
        }

        public static string BuildControlPanelServer(string outputRoot)
        {
            string buildName = "Server-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string outputDirectory = Path.Combine(Path.GetFullPath(outputRoot), buildName);
            lastServerBuildOutputDirectory = BuildAtPath(outputDirectory, ServerExecutableName, ServerScenes, StandaloneBuildSubtarget.Server);
            return lastServerBuildOutputDirectory;
        }

        public static string BuildControlPanelLinuxServer(string outputRoot)
        {
            ConfigureLinuxServerBuildProfile();
            string buildName = "LinuxServer-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string outputDirectory = Path.Combine(Path.GetFullPath(outputRoot), buildName);
            PrepareCustomOutputDirectory(outputDirectory);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.Mono2x);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = ServerScenes,
                locationPathName = Path.Combine(outputDirectory, "RAVServer.x86_64"),
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPlayerWithProgress(buildOptions, buildName);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Linux Dedicated Server build failed. See the Unity Console and Editor.log for details.");
            }

            lastServerBuildOutputDirectory = outputDirectory;
            Debug.Log($"Linux Dedicated Server build created at {outputDirectory}.");
            return outputDirectory;
        }

        [MenuItem("RAV/Build Selected Windows Client")]
        public static void BuildLocalClient()
        {
            lastClientBuildOutputDirectory = BuildControlPanelClient(
                RAVDevelopmentControlPanel.ClientBuildOutputRoot,
                RAVDevelopmentControlPanel.SelectedClientTarget);
        }

        internal static string BuildControlPanelClient(string outputRoot, RAVDevelopmentControlPanel.ClientTarget target)
        {
            string buildFlavor = target == RAVDevelopmentControlPanel.ClientTarget.Netcup
                ? "Netcup-Release"
                : "Local-Development";
            string buildName = "Client-" + buildFlavor + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string outputDirectory = Path.Combine(Path.GetFullPath(outputRoot), buildName);
            BuildOptions options = target == RAVDevelopmentControlPanel.ClientTarget.Netcup
                ? BuildOptions.None
                : BuildOptions.Development;
            lastClientBuildOutputDirectory = BuildAtPath(outputDirectory, ClientExecutableName, ClientScenes, StandaloneBuildSubtarget.Player, options);
            if (!RAVDevelopmentControlPanel.TryWriteClientRuntimeConfiguration(lastClientBuildOutputDirectory, target, out string error))
            {
                throw new InvalidOperationException("Client build completed, but its runtime configuration was not packaged: " + error);
            }

            Debug.Log($"{target} Windows Client build created at {lastClientBuildOutputDirectory}.");
            return lastClientBuildOutputDirectory;
        }

        private static void BuildPlayerWindowHandler(BuildPlayerOptions options)
        {
            if (options.target != BuildTarget.StandaloneWindows64
                || options.subtarget != (int)StandaloneBuildSubtarget.Player)
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                return;
            }

            RAVDevelopmentControlPanel.ClientTarget target = RAVDevelopmentControlPanel.SelectedClientTarget;
            if (target == RAVDevelopmentControlPanel.ClientTarget.Netcup)
            {
                options.options &= ~BuildOptions.Development;
            }
            else
            {
                options.options |= BuildOptions.Development;
            }

            string buildName = Path.GetFileNameWithoutExtension(options.locationPathName);
            BuildReport report = BuildPlayerWithProgress(options, string.IsNullOrWhiteSpace(buildName) ? "Selected Client" : buildName);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Selected Windows Client build failed. See the Unity Console and Editor.log for details.");
            }

            string outputPath = string.IsNullOrWhiteSpace(report.summary.outputPath)
                ? options.locationPathName
                : report.summary.outputPath;
            string outputDirectory = Directory.Exists(outputPath) ? outputPath : Path.GetDirectoryName(outputPath);
            if (!RAVDevelopmentControlPanel.TryWriteClientRuntimeConfiguration(outputDirectory, target, out string error))
            {
                throw new InvalidOperationException("Client build completed, but its runtime configuration was not packaged: " + error);
            }

            lastClientBuildOutputDirectory = outputDirectory;
            Debug.Log($"{target} Windows Client build created at {outputDirectory}.");
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

            BuildReport report = BuildPlayerWithProgress(buildOptions, "Linux Dedicated Server");
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

            return BuildPlayerAtPath(outputDirectory, buildName, executableName, scenes, subtarget);
        }

        private static string BuildAtPath(string outputDirectory, string executableName, string[] scenes, StandaloneBuildSubtarget subtarget, BuildOptions options = BuildOptions.Development)
        {
            PrepareCustomOutputDirectory(outputDirectory);
            return BuildPlayerAtPath(outputDirectory, Path.GetFileName(outputDirectory), executableName, scenes, subtarget, options);
        }

        private static string BuildPlayerAtPath(string outputDirectory, string buildName, string executableName, string[] scenes, StandaloneBuildSubtarget subtarget, BuildOptions options = BuildOptions.Development)
        {

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, executableName),
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)subtarget,
                options = options
            };

            BuildReport report = BuildPlayerWithProgress(buildOptions, buildName);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{buildName} build failed. See the Unity Console and Editor.log for details.");
            }

            Debug.Log($"Local {buildName.ToLowerInvariant()} build created at {outputDirectory}.");
            return outputDirectory;
        }

        private static BuildReport BuildPlayerWithProgress(BuildPlayerOptions options, string buildName)
        {
            EditorUtility.DisplayProgressBar("RAV Build", "Building " + buildName + ". Exact percentage is not available here.", 0f);
            try
            {
                return BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
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

        private static void PrepareCustomOutputDirectory(string outputDirectory)
        {
            string normalizedOutputDirectory = Path.GetFullPath(outputDirectory);
            string projectRoot = Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string projectPrefix = projectRoot + Path.DirectorySeparatorChar;
            if (normalizedOutputDirectory.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedOutputDirectory, projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Control Panel builds must be outside the Unity project folder.");
            }

            if (Directory.Exists(normalizedOutputDirectory) || File.Exists(normalizedOutputDirectory))
            {
                throw new InvalidOperationException($"Build output already exists and will not be overwritten: {normalizedOutputDirectory}");
            }

            Directory.CreateDirectory(normalizedOutputDirectory);
        }
    }

    internal sealed class WindowsClientBuildProfileProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null
                || report.summary.platform != BuildTarget.StandaloneWindows64
                || IsDedicatedServerBuild(report))
            {
                return;
            }

            bool expectedDevelopment = RAVDevelopmentControlPanel.SelectedClientTarget == RAVDevelopmentControlPanel.ClientTarget.Local;
            bool actualDevelopment = (report.summary.options & BuildOptions.Development) != 0;
            if (actualDevelopment != expectedDevelopment)
            {
                string selectedTarget = RAVDevelopmentControlPanel.SelectedClientTarget.ToString();
                throw new BuildFailedException("Windows Client build stopped before output: the active Build Profile does not match the selected "
                    + selectedTarget + " Client. Toggle the Client target in RAV Control Panel and build again.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null
                || report.summary.result != BuildResult.Succeeded
                || report.summary.platform != BuildTarget.StandaloneWindows64
                || IsDedicatedServerBuild(report))
            {
                return;
            }

            string outputPath = report.summary.outputPath;
            string executablePath = Directory.Exists(outputPath)
                ? FindWindowsClientExecutable(outputPath)
                : outputPath;
            string outputDirectory = string.IsNullOrWhiteSpace(executablePath)
                ? string.Empty
                : Path.GetDirectoryName(executablePath);

            string error = string.Empty;
            bool packaged = !string.IsNullOrWhiteSpace(outputDirectory)
                && RAVDevelopmentControlPanel.TryWriteClientRuntimeConfiguration(
                    outputDirectory,
                    RAVDevelopmentControlPanel.SelectedClientTarget,
                    out error);
            if (!packaged)
            {
                throw new BuildFailedException("Windows Client built, but its selected API/DTLS runtime settings could not be packaged: "
                    + (string.IsNullOrWhiteSpace(outputDirectory) ? "the executable output path could not be resolved." : error));
            }

            Debug.Log("Windows Client runtime configuration and public DTLS CA packaged for "
                + RAVDevelopmentControlPanel.SelectedClientTarget + ".");
        }

        private static bool IsDedicatedServerBuild(BuildReport report)
        {
            string outputName = Path.GetFileNameWithoutExtension(report.summary.outputPath ?? string.Empty);
            if (outputName.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile == null)
            {
                return false;
            }

            SerializedObject serializedProfile = new SerializedObject(activeProfile);
            SerializedProperty target = serializedProfile.FindProperty("m_BuildTarget");
            SerializedProperty subtarget = serializedProfile.FindProperty("m_Subtarget");
            return target != null
                && target.intValue == (int)BuildTarget.StandaloneWindows64
                && subtarget != null
                && subtarget.intValue == (int)StandaloneBuildSubtarget.Server;
        }

        private static string FindWindowsClientExecutable(string outputDirectory)
        {
            string[] executables = Directory.GetFiles(outputDirectory, "*.exe", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < executables.Length; i++)
            {
                if (Path.GetFileNameWithoutExtension(executables[i]).IndexOf("server", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return executables[i];
                }
            }

            return string.Empty;
        }
    }
}
#endif
