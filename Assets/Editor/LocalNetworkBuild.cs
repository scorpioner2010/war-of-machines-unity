#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class LocalNetworkBuild
    {
        private const string ServerExecutableName = "WarOfMachinesServer.exe";
        private const string ClientExecutableName = "WarOfMachinesClient.exe";
        private const string ServerBuildRequestFileName = "server-build-request.txt";
        private const string ServerBuildResultFileName = "server-build-result.txt";
        private const string ClientBuildRequestFileName = "client-build-request.txt";
        private const string ClientBuildResultFileName = "client-build-result.txt";

        private static bool externalBuildInProgress;

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
                WriteExternalServerBuildResult(buildToken, "SUCCESS", string.Empty);
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
                WriteExternalBuildResult(ClientBuildResultFileName, buildToken, "SUCCESS", string.Empty);
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

            if (Directory.Exists(normalizedOutputDirectory))
            {
                Directory.Delete(normalizedOutputDirectory, true);
            }

            Directory.CreateDirectory(normalizedOutputDirectory);
        }
    }
}
#endif
