#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class LocalNetworkBuild
    {
        private const string ServerExecutableName = "WarOfMachinesServer.exe";
        private const string ClientExecutableName = "WarOfMachinesClient.exe";

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

        [MenuItem("War Of Machines/Local Build/Windows Server")]
        public static void BuildLocalServer()
        {
            Build("Server", ServerExecutableName, ServerScenes, StandaloneBuildSubtarget.Server);
        }

        [MenuItem("War Of Machines/Local Build/Windows Client")]
        public static void BuildLocalClient()
        {
            Build("Client", ClientExecutableName, ClientScenes, StandaloneBuildSubtarget.Player);
        }

        [MenuItem("War Of Machines/Local Build/Windows Server and Client")]
        public static void BuildLocalServerAndClient()
        {
            BuildLocalServer();
            BuildLocalClient();
        }

        private static void Build(string buildName, string executableName, string[] scenes, StandaloneBuildSubtarget subtarget)
        {
            string outputDirectory = GetOutputDirectory(buildName);
            PrepareOutputDirectory(outputDirectory);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, executableName),
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)subtarget,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{buildName} build failed. See the Unity Console and Editor.log for details.");
            }

            Debug.Log($"Local {buildName.ToLowerInvariant()} build created at {outputDirectory}.");
        }

        private static string GetOutputDirectory(string buildName)
        {
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            string buildsDirectory = Path.GetFullPath(Path.Combine(projectDirectory, "..", "war-of-machines-local-builds"));
            return Path.Combine(buildsDirectory, buildName);
        }

        private static void PrepareOutputDirectory(string outputDirectory)
        {
            string normalizedOutputDirectory = Path.GetFullPath(outputDirectory);
            string normalizedBuildsDirectory = Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "..", "war-of-machines-local-builds"));
            string expectedPrefix = normalizedBuildsDirectory + Path.DirectorySeparatorChar;

            if (normalizedOutputDirectory.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException("Local build output is outside the approved build directory.");
            }

            if (Directory.Exists(normalizedOutputDirectory))
            {
                Directory.Delete(normalizedOutputDirectory, true);
            }

            Directory.CreateDirectory(normalizedOutputDirectory);
        }
    }
}
#endif
