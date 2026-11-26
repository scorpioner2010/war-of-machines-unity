#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RAV.Scripts.API;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RAV.Editor
{
    public sealed class RAVDevelopmentControlPanel : EditorWindow
    {
        private const string Pref = "RAV.DevelopmentControlPanel.";
        private const string LocalServerRole = "LocalServer";
        private const string LocalApiRole = "LocalApi";
        private const string LocalClientRole = "LocalClient";
        private const string LocalDatabaseTunnelRole = "LocalDatabaseTunnel";
        private const int DefaultStartupTimeout = 120;
        private const int DefaultDatabaseTunnelPort = 15432;
        private const int StatusRefreshTimeoutSeconds = 10;
        private const int MaxRecentLogEntries = 180;
        private const int MaxFullLogBytes = 1024 * 1024;
        private static readonly Color RunningButtonColor = new Color(0.42f, 0.82f, 0.46f);
        private static readonly string[] IsolatedEnvironmentNames =
        {
            "RAV_SERVER_ID", "RAV_SERVER_CREDENTIAL", "WOM_SERVER_ID", "WOM_SERVER_CREDENTIAL",
            "RAV_SERVER_REGION", "RAV_SERVER_ADDRESS", "RAV_SERVER_PORT", "WOM_SERVER_REGION", "WOM_SERVER_ADDRESS", "WOM_SERVER_PORT",
            "RAV_UTP_SERVER_CERTIFICATE", "RAV_UTP_SERVER_PRIVATE_KEY", "RAV_UTP_SERVER_COMMON_NAME", "RAV_UTP_CA_CERTIFICATE",
            "WOM_UTP_SERVER_CERTIFICATE", "WOM_UTP_SERVER_PRIVATE_KEY", "WOM_UTP_SERVER_COMMON_NAME", "WOM_UTP_CA_CERTIFICATE",
            "DedicatedServers__Provisioned__0__ServerId", "DedicatedServers__Provisioned__0__Credential",
            "Jwt__Key", "DATABASE_URL", "ConnectionStrings__DefaultConnection",
            "RAV_DATABASE_TUNNEL_LOCAL_PORT", "RAV_DATABASE_ROOT_CERTIFICATE", "RAV_DATABASE_TLS_SERVER_NAME",
            "Database__ApplyMigrationsOnStartup", "Database__RunSeedOnStartup", "Database__SeedDevelopmentFixtures",
            "RAV_API_BASE", "WOM_API_BASE", "API_BASE",
            "ASPNETCORE_ENVIRONMENT", "ASPNETCORE_URLS", "DOTNET_ENVIRONMENT"
        };
        private static readonly List<LiveLogEntry> RecentLog = new List<LiveLogEntry>();
        private static readonly ConcurrentQueue<LiveLogEntry> PendingLogEntries = new ConcurrentQueue<LiveLogEntry>();
        private static readonly object LogFileLock = new object();
        private static IEnumerator activeOperation;
        private static readonly Stack<IEnumerator> operationStack = new Stack<IEnumerator>();
        private static Task waitingTask;
        private static bool pumpScheduled;
        private static bool securityPreparedThisOperation;
        private static string operationName = string.Empty;
        private static string operationPhase = "No operation has run yet.";
        private static string lastError = string.Empty;
        private static ComponentStatus databaseStatus = ComponentStatus.Unknown;
        private static ComponentStatus apiStatus = ComponentStatus.Unknown;
        private static ComponentStatus serverStatus = ComponentStatus.Unknown;
        private static Vector2 logScroll;
        private static OperationStatus operationStatus = OperationStatus.Completed;
        private static int currentStage;
        private static int completedStages;
        private static int totalStages = 1;
        private static bool stageStarted;
        private static double operationStartedAt;
        private static double lastOperationElapsed;
        private static string lastWaitingMessage = string.Empty;
        private static bool logLoaded;
        private static bool autoScroll = true;
        private static float liveLogHeight = 190f;
        private static RAVDevelopmentControlPanel openWindow;
        private static GUIStyle liveLogStyle;
        private static GUIStyle operationStatusStyle;

        private Vector2 panelScroll;

        private enum OperationStatus
        {
            Running,
            Waiting,
            Completed,
            Failed
        }

        private enum LiveLogLevel
        {
            Info,
            Active,
            Waiting,
            Success,
            Warning,
            Error
        }

        [Serializable]
        private sealed class LiveLogEntry
        {
            public string timestamp;
            public string message;
            public LiveLogLevel level;
        }

        private enum Location
        {
            Local,
            Netcup
        }

        private enum ComponentStatus
        {
            Stopped,
            Starting,
            Ready,
            Failed,
            Unknown
        }

        [Serializable]
        private sealed class ApiServerStatus
        {
            public ApiServerItem[] servers;
        }

        [Serializable]
        private sealed class ApiServerItem
        {
            public bool isOnline;
            public string address;
            public int port;
            public string lastHeartbeatUtc;
        }

        [Serializable]
        private sealed class ManagedProcessInfo
        {
            public int Pid;
            public long StartTicks;
            public string ExecutablePath;
        }

        private sealed class CommandResult
        {
            public int ExitCode;
            public bool TimedOut;
            public string Output = string.Empty;
            public string Error = string.Empty;
            public bool Success => !TimedOut && ExitCode == 0;
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private static string DeploymentRoot => Path.Combine(DesktopPath, "War of Machines Deployment");
        private static string HelperScriptsRoot => Path.Combine(DeploymentRoot, "scripts");
        private static string ApiDeploymentScriptsRoot => Path.Combine(Directory.GetParent(ApiProjectRoot)?.FullName ?? ApiProjectRoot, "deploy", "staging", "scripts");
        private static string DevelopmentControlPanelRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RAV", "DevelopmentControlPanel");
        private static string LogsRoot => Path.Combine(DevelopmentControlPanelRoot, "Logs");
        private static string LiveLogPath => Path.Combine(LogsRoot, "operations.log");
        private static string FullLogPath => Path.Combine(LogsRoot, "operations-full.log");

        private static string ApiProjectRoot
        {
            get => GetString("ApiProjectPath", @"D:\Programing\ServerProjects\rav-api\RavApi");
            set => SetString("ApiProjectPath", value);
        }

        private static string LocalApiUrl
        {
            get => GetString("LocalApiUrl", HttpLink.LocalAPIBase);
            set => SetString("LocalApiUrl", value);
        }

        private static string NetcupApiUrl
        {
            get => GetString("NetcupApiUrl", "https://api.staging.62-83-120-40.sslip.io");
            set => SetString("NetcupApiUrl", value);
        }

        private static string LocalSecurityPath
        {
            get => GetString("LocalSecurityPath", @"D:\Programing\UnityProjects\war-of-machines-local-builds\LocalSecurity");
            set => SetString("LocalSecurityPath", value);
        }

        private static string ServerBuildRoot
        {
            get => GetString("ServerBuildRoot", @"D:\Programing\UnityProjects\war-of-machines-local-builds");
            set => SetString("ServerBuildRoot", value);
        }

        private static string ClientBuildRoot
        {
            get => GetString("ClientBuildRoot", Path.Combine(DesktopPath, "RAV Builds"));
            set => SetString("ClientBuildRoot", value);
        }

        private static string NetcupServerHost
        {
            get => GetString("NetcupServerHost", "62.83.120.40");
            set => SetString("NetcupServerHost", value);
        }

        private static int LocalServerPort
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Pref + "LocalServerPort", 7770), 1, 65535);
            set => EditorPrefs.SetInt(Pref + "LocalServerPort", Mathf.Clamp(value, 1, 65535));
        }

        private static int NetcupServerPort
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Pref + "NetcupServerPort", 7770), 1, 65535);
            set => EditorPrefs.SetInt(Pref + "NetcupServerPort", Mathf.Clamp(value, 1, 65535));
        }

        private static int StartupTimeout
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Pref + "StartupTimeout", DefaultStartupTimeout), 15, 600);
            set => EditorPrefs.SetInt(Pref + "StartupTimeout", Mathf.Clamp(value, 15, 600));
        }

        private static string SshTarget
        {
            get => GetString("SshTarget", "ravadmin@62.83.120.40");
            set => SetString("SshTarget", value);
        }

        private static int SshPort
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Pref + "SshPort", 22), 1, 65535);
            set => EditorPrefs.SetInt(Pref + "SshPort", Mathf.Clamp(value, 1, 65535));
        }

        private static string SshKeyReference
        {
            get => GetString("SshKeyReference", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "rav-netcup-staging-ed25519"));
            set => SetString("SshKeyReference", value);
        }

        private static string PostgresCaPath => Path.Combine(DeploymentRoot, "certificates", "RAV-Staging-PostgreSQL-CA.crt");

        private static string NetcupCaPath
        {
            get => GetString("NetcupCaPath", Path.Combine(DeploymentRoot, "certificates", "RAV-Staging-DTLS-CA.crt"));
            set => SetString("NetcupCaPath", value);
        }

        private static string NetcupCommonName
        {
            get => GetString("NetcupCommonName", "rav-staging-server");
            set => SetString("NetcupCommonName", value);
        }

        private static Location SelectedEnvironment
        {
            get => (Location)EditorPrefs.GetInt(Pref + "Environment", EditorPrefs.GetInt(Pref + "Api", (int)Location.Local));
            set => EditorPrefs.SetInt(Pref + "Environment", (int)value);
        }

        private static bool LocalBuildChecked
        {
            get => EditorPrefs.GetBool(Pref + "LocalBuildChecked", true);
            set => EditorPrefs.SetBool(Pref + "LocalBuildChecked", value);
        }

        private static bool LocalStartChecked
        {
            get => EditorPrefs.GetBool(Pref + "LocalStartChecked", true);
            set => EditorPrefs.SetBool(Pref + "LocalStartChecked", value);
        }

        private static bool NetcupBuildChecked
        {
            get => EditorPrefs.GetBool(Pref + "NetcupBuildChecked", false);
            set => EditorPrefs.SetBool(Pref + "NetcupBuildChecked", value);
        }

        private static bool NetcupDeployChecked
        {
            get => EditorPrefs.GetBool(Pref + "NetcupDeployChecked", false);
            set => EditorPrefs.SetBool(Pref + "NetcupDeployChecked", value);
        }

        private static bool NetcupStartChecked
        {
            get => EditorPrefs.GetBool(Pref + "NetcupStartChecked", false);
            set => EditorPrefs.SetBool(Pref + "NetcupStartChecked", value);
        }

        private static int LocalDatabaseTunnelPort
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(Pref + "DatabaseTunnelPort", DefaultDatabaseTunnelPort), 1024, 65535);
        }

        private static string PreparedNetcupApiImage
        {
            get => EditorPrefs.GetString(Pref + "PreparedNetcupApiImage", string.Empty);
            set => EditorPrefs.SetString(Pref + "PreparedNetcupApiImage", value ?? string.Empty);
        }

        private static string PreparedLocalApiBuild
        {
            get => EditorPrefs.GetString(Pref + "PreparedLocalApiBuild", string.Empty);
            set => EditorPrefs.SetString(Pref + "PreparedLocalApiBuild", value ?? string.Empty);
        }

        private static string DeployedNetcupApiImage
        {
            get => EditorPrefs.GetString(Pref + "DeployedNetcupApiImage", string.Empty);
            set => EditorPrefs.SetString(Pref + "DeployedNetcupApiImage", value ?? string.Empty);
        }

        private static string PreparedNetcupServerRelease
        {
            get => EditorPrefs.GetString(Pref + "PreparedNetcupServerRelease", string.Empty);
            set => EditorPrefs.SetString(Pref + "PreparedNetcupServerRelease", value ?? string.Empty);
        }

        private static string DeployedNetcupServerRelease
        {
            get => EditorPrefs.GetString(Pref + "DeployedNetcupServerRelease", string.Empty);
            set => EditorPrefs.SetString(Pref + "DeployedNetcupServerRelease", value ?? string.Empty);
        }

        private static bool LocalApiUsesNetcupTunnel
        {
            get => EditorPrefs.GetBool(Pref + "LocalApiUsesNetcupTunnel", false);
            set => EditorPrefs.SetBool(Pref + "LocalApiUsesNetcupTunnel", value);
        }

        [MenuItem("RAV/Development Control Panel")]
        private static void OpenWindow()
        {
            RAVDevelopmentControlPanel window = GetWindow<RAVDevelopmentControlPanel>("RAV Control Panel");
            window.minSize = new Vector2(620f, 760f);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeHook()
        {
            EnsureRecentLogLoaded();
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= PumpOperation;
            EditorApplication.update += PumpOperation;
            ApplyConfiguredEnvironment("Editor startup");
        }

        private void OnEnable()
        {
            openWindow = this;
            EnsureRecentLogLoaded();
            autoScroll = EditorPrefs.GetBool(Pref + "AutoScrollLog", true);
            float savedLogHeight = EditorPrefs.GetFloat(Pref + "LiveLogHeight", 190f);
            liveLogHeight = Mathf.Clamp(savedLogHeight, 100f, 720f);
            EditorApplication.update -= PumpOperation;
            EditorApplication.update += PumpOperation;
            RefreshStatus();
        }

        private void OnDisable()
        {
            if (ReferenceEquals(openWindow, this))
            {
                openWindow = null;
            }
        }

        private void OnGUI()
        {
            EnsureLiveStyles();
            DrainPendingLogEntries();
            panelScroll = EditorGUILayout.BeginScrollView(panelScroll);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("RAV Development Control Panel", EditorStyles.boldLabel);
            DrawDatabaseStatus();
            DrawEnvironmentSelector();
            DrawComponentCard("API", SelectedEnvironment == Location.Local ? "Local · ASP.NET Core" : "Netcup · ASP.NET Core", apiStatus, false);
            DrawComponentCard("Dedicated Server", SelectedEnvironment == Location.Local ? "Local · Windows" : "Netcup · Linux x86-64 Mono", serverStatus, true);
            DrawCombinedOperations();
            DrawClientActions();
            using (new EditorGUI.DisabledScope(IsBusy))
            {
                if (GUILayout.Button("Refresh Status")) RefreshStatus();
            }

            DrawAdvancedSettings();
            DrawLiveOperationStatusAndLog();
            EditorGUILayout.EndScrollView();

            if (IsBusy)
            {
                SchedulePumpAfterGui();
                Repaint();
            }
        }

        private static void EnsureLiveStyles()
        {
            if (liveLogStyle == null)
            {
                liveLogStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { richText = false };
            }

            if (operationStatusStyle == null)
            {
                operationStatusStyle = new GUIStyle(EditorStyles.boldLabel) { richText = false };
            }
        }

        private void DrawDatabaseStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawStatusRow("PostgreSQL — Netcup", databaseStatus);
            EditorGUILayout.LabelField("Shared staging database for both modes", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private static bool DrawRunningButton(string label, bool isRunning, params GUILayoutOption[] options)
        {
            Color previousBackground = GUI.backgroundColor;
            try
            {
                if (isRunning)
                {
                    GUI.backgroundColor = RunningButtonColor;
                }

                return GUILayout.Button(label, options);
            }
            finally
            {
                GUI.backgroundColor = previousBackground;
            }
        }

        private static void InvalidateStatuses()
        {
            databaseStatus = ComponentStatus.Unknown;
            apiStatus = ComponentStatus.Unknown;
            serverStatus = ComponentStatus.Unknown;
        }

        private void DrawEnvironmentSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Environment", EditorStyles.miniBoldLabel);
            int selected = (int)SelectedEnvironment;
            using (new EditorGUI.DisabledScope(IsBusy || EditorApplication.isPlaying))
            {
                selected = GUILayout.Toolbar(selected, new[] { "Local", "Netcup" });
            }

            if (selected != (int)SelectedEnvironment)
            {
                SelectedEnvironment = (Location)selected;
                InvalidateStatuses();
                ApplyConfiguredEnvironment("Environment switched to " + SelectedEnvironment);
                RefreshStatus();
            }

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Exit Play Mode before switching environments or changing services.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentCard(string title, string subtitle, ComponentStatus status, bool server)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawStatusValue(status);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(IsBusy || EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Build"))
                {
                    StartOperation("Build " + title, server ? BuildServerRoutine() : BuildApiRoutine());
                }

                if (SelectedEnvironment == Location.Netcup && GUILayout.Button("Deploy"))
                {
                    StartOperation("Deploy " + title, server ? DeployServerRoutine() : DeployApiRoutine());
                }

                if (DrawRunningButton("Start", status == ComponentStatus.Ready))
                {
                    StartOperation("Start " + title, server ? StartServerRoutine(false) : StartApiRoutine());
                }

                if (GUILayout.Button("Stop"))
                {
                    StartOperation("Stop " + title, server ? StopServerRoutine() : StopApiRoutine());
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatusValue(ComponentStatus status)
        {
            Color previous = GUI.contentColor;
            try
            {
                GUI.contentColor = status == ComponentStatus.Ready
                    ? RunningButtonColor
                    : status == ComponentStatus.Failed ? GetLogColor(LiveLogLevel.Error) : previous;
                EditorGUILayout.LabelField(status.ToString(), EditorStyles.boldLabel, GUILayout.Width(74f));
            }
            finally
            {
                GUI.contentColor = previous;
            }
        }

        private static void DrawStatusRow(string label, ComponentStatus status)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawStatusValue(status);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCombinedOperations()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Combined Operations", EditorStyles.boldLabel);
            if (SelectedEnvironment == Location.Local)
            {
                LocalBuildChecked = EditorGUILayout.ToggleLeft("Build", LocalBuildChecked);
                LocalStartChecked = EditorGUILayout.ToggleLeft("Start", LocalStartChecked);
            }
            else
            {
                NetcupBuildChecked = EditorGUILayout.ToggleLeft("Build", NetcupBuildChecked);
                NetcupDeployChecked = EditorGUILayout.ToggleLeft("Deploy", NetcupDeployChecked);
                NetcupStartChecked = EditorGUILayout.ToggleLeft("Start", NetcupStartChecked);
            }

            using (new EditorGUI.DisabledScope(IsBusy || EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Execute API")) StartOperation("Execute API", ExecuteApiRoutine());
                if (GUILayout.Button("Execute Server")) StartOperation("Execute Server", ExecuteServerRoutine());
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Execute All", GUILayout.Height(28f))) StartOperation("Execute All", ExecuteAllRoutine());
            }

            using (new EditorGUI.DisabledScope(IsBusy))
            {
                if (GUILayout.Button("Stop All")) StartOperation("Stop All", StopSelectedRoutine());
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawClientActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Client", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(SelectedEnvironment == Location.Local ? "Local environment" : "Netcup environment", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(IsBusy || EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Build Client")) StartOperation("Build Client", BuildClientRoutine());
                if (GUILayout.Button("Launch Client")) StartOperation("Launch Client", LaunchClientRoutine());
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedSettings()
        {
            bool showAdvanced = EditorPrefs.GetBool(Pref + "ShowAdvanced", false);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
            EditorPrefs.SetBool(Pref + "ShowAdvanced", showAdvanced);
            if (!showAdvanced)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            ApiProjectRoot = EditorGUILayout.TextField("API project path", ApiProjectRoot);
            LocalApiUrl = EditorGUILayout.TextField("Local API URL", LocalApiUrl);
            NetcupApiUrl = EditorGUILayout.TextField("Netcup API URL", NetcupApiUrl);
            LocalSecurityPath = EditorGUILayout.TextField("Local security directory", LocalSecurityPath);
            LocalServerPort = EditorGUILayout.IntField("Local Dedicated Server port", LocalServerPort);
            NetcupServerHost = EditorGUILayout.TextField("Netcup server host", NetcupServerHost);
            NetcupServerPort = EditorGUILayout.IntField("Netcup server port", NetcupServerPort);
            ServerBuildRoot = DrawDirectoryField("Server build output root", ServerBuildRoot);
            ClientBuildRoot = DrawDirectoryField("Client build output root", ClientBuildRoot);
            SshTarget = EditorGUILayout.TextField("SSH target", SshTarget);
            SshPort = EditorGUILayout.IntField("SSH port", SshPort);
            SshKeyReference = EditorGUILayout.TextField("SSH key reference", SshKeyReference);
            EditorGUILayout.LabelField("SSH uses the configured key and strict host key checking.", EditorStyles.miniLabel);
            NetcupCaPath = EditorGUILayout.TextField("Netcup DTLS CA reference", NetcupCaPath);
            NetcupCommonName = EditorGUILayout.TextField("Netcup DTLS common name", NetcupCommonName);
            StartupTimeout = EditorGUILayout.IntField("Startup timeout (seconds)", StartupTimeout);
            EditorGUILayout.EndVertical();
        }

        private static string DrawDirectoryField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("Browse", GUILayout.Width(60f)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select " + label, value, string.Empty);
                if (!string.IsNullOrWhiteSpace(selected)) value = selected;
            }

            EditorGUILayout.EndHorizontal();
            return value;
        }

        private void DrawLiveOperationStatusAndLog()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Live Operation Status & Log", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.IsNullOrEmpty(operationName) ? "Operation: None" : "Operation: " + operationName,
                EditorStyles.label);

            string spinner = IsBusy ? GetActivityGlyph() : string.Empty;
            string statusText = operationStatus.ToString();
            EditorGUILayout.BeginHorizontal();
            GUIStyle statusStyle = operationStatusStyle ?? EditorStyles.boldLabel;
            statusStyle.normal.textColor = GetOperationStatusColor(operationStatus);
            EditorGUILayout.LabelField("Status: " + statusText + (IsBusy ? " " + spinner : string.Empty), statusStyle, GUILayout.Width(145f));
            double elapsed = IsBusy ? Math.Max(0d, EditorApplication.timeSinceStartup - operationStartedAt) : lastOperationElapsed;
            EditorGUILayout.LabelField("Elapsed: " + FormatElapsed(elapsed));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Stage: " + (currentStage > 0 ? currentStage.ToString(CultureInfo.InvariantCulture) : "0")
                + " of " + totalStages.ToString(CultureInfo.InvariantCulture)
                + " · " + completedStages.ToString(CultureInfo.InvariantCulture) + " done", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Current stage: " + operationPhase, EditorStyles.miniLabel);
            float progress = totalStages <= 0 ? 0f : Mathf.Clamp01((float)completedStages / totalStages);
            Rect progressRect = EditorGUILayout.GetControlRect(false, 18f);
            EditorGUI.ProgressBar(progressRect, progress, "Completed stages: " + completedStages + " / " + totalStages);

            EditorGUILayout.BeginHorizontal();
            bool nextAutoScroll = EditorGUILayout.ToggleLeft("Auto-scroll", autoScroll, GUILayout.Width(105f));
            if (nextAutoScroll != autoScroll)
            {
                autoScroll = nextAutoScroll;
                EditorPrefs.SetBool(Pref + "AutoScrollLog", autoScroll);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Log", GUILayout.Width(82f))) ClearLiveLog();
            using (new EditorGUI.DisabledScope(RecentLog.Count == 0))
            {
                if (GUILayout.Button("Copy Log", GUILayout.Width(78f))) CopyLiveLog();
            }

            if (GUILayout.Button("Open Full Log", GUILayout.Width(100f))) OpenFullLog();
            EditorGUILayout.EndHorizontal();

            float nextHeight = EditorGUILayout.Slider("Log height", liveLogHeight, 100f, 720f);
            if (!Mathf.Approximately(nextHeight, liveLogHeight))
            {
                liveLogHeight = nextHeight;
                EditorPrefs.SetFloat(Pref + "LiveLogHeight", liveLogHeight);
            }

            List<LiveLogEntry> snapshot = GetRecentLogSnapshot();
            if (autoScroll)
            {
                float contentWidth = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 48f);
                float contentHeight = 0f;
                GUIStyle measureStyle = liveLogStyle ?? EditorStyles.wordWrappedMiniLabel;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    contentHeight += measureStyle.CalcHeight(new GUIContent(FormatVisibleLogEntry(snapshot[i])), contentWidth)
                        + measureStyle.margin.vertical + 2f;
                }

                logScroll.y = Mathf.Max(0f, contentHeight + 8f - liveLogHeight);
            }

            logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(liveLogHeight));
            GUIStyle entryStyle = liveLogStyle ?? EditorStyles.wordWrappedMiniLabel;
            for (int i = 0; i < snapshot.Count; i++)
            {
                entryStyle.normal.textColor = GetLogColor(snapshot[i].level);
                EditorGUILayout.LabelField(FormatVisibleLogEntry(snapshot[i]), entryStyle);
            }

            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            string validation = ValidateSelection();
            string error;
            bool applied = false;
            error = string.Empty;
            if (string.IsNullOrEmpty(validation))
            {
                applied = TryApplyClientEnvironment(out error);
            }

            if (!string.IsNullOrEmpty(validation) || !applied)
            {
                lastError = string.IsNullOrEmpty(validation) ? error : validation;
                EditorApplication.isPlaying = false;
                AddLog("Play Mode blocked: " + lastError);
            }
        }

        private static bool ApplyConfiguredEnvironment(string reason)
        {
            bool standaloneOperation = !IsBusy;
            if (standaloneOperation)
            {
                BeginInstantOperation("Environment Switching", reason);
            }

            if (EditorApplication.isPlaying)
            {
                AddLog(reason + ": environment queued until Play Mode exits.");
                if (standaloneOperation) CompleteInstantOperation(false, "Exit Play Mode before switching environments.");
                return false;
            }

            string validation = ValidateSelection();
            if (!string.IsNullOrEmpty(validation))
            {
                lastError = validation;
                AddLog("Environment not applied: " + validation);
                if (standaloneOperation) CompleteInstantOperation(false, validation);
                return false;
            }

            if (!TryApplyClientEnvironment(out string error))
            {
                lastError = error;
                AddLog("Environment not applied: " + error);
                if (standaloneOperation) CompleteInstantOperation(false, error);
                return false;
            }

            lastError = string.Empty;
            AddLog(reason + ": explicit API and DTLS client settings applied; cached authentication and tickets cleared.");
            if (standaloneOperation)
            {
                CompleteInstantOperation(true);
                AddLog("Environment switching completed.", LiveLogLevel.Success);
            }
            return true;
        }

        private static bool TryApplyClientEnvironment(out string error)
        {
            error = string.Empty;
            string validation = ValidateSelection();
            if (!string.IsNullOrEmpty(validation))
            {
                error = validation;
                return false;
            }

            string apiBase = SelectedEnvironment == Location.Local ? LocalApiUrl : NetcupApiUrl;
            string commonName;
            string caPath;
            if (SelectedEnvironment == Location.Local)
            {
                string clientEnv = Path.Combine(LocalSecurityPath, "client-transport.env");
                if (!TryReadEnvironmentFile(clientEnv, out Dictionary<string, string> clientValues, out error))
                {
                    return false;
                }

                commonName = GetEnvironmentValue(clientValues, "RAV_UTP_SERVER_COMMON_NAME", "WOM_UTP_SERVER_COMMON_NAME");
                caPath = GetEnvironmentValue(clientValues, "RAV_UTP_CA_CERTIFICATE", "WOM_UTP_CA_CERTIFICATE");
            }
            else
            {
                commonName = NetcupCommonName;
                caPath = NetcupCaPath;
            }

            if (string.IsNullOrWhiteSpace(commonName) || string.IsNullOrWhiteSpace(caPath) || !File.Exists(caPath))
            {
                error = "The selected client DTLS common name or public CA certificate is missing. No insecure fallback is allowed.";
                return false;
            }

            if (!HttpLink.TrySetEditorApiBase(apiBase, out error))
            {
                return false;
            }

            Environment.SetEnvironmentVariable("RAV_UTP_SERVER_COMMON_NAME", commonName);
            Environment.SetEnvironmentVariable("RAV_UTP_CA_CERTIFICATE", caPath);
            Environment.SetEnvironmentVariable("RAV_UTP_SERVER_CERTIFICATE", null);
            Environment.SetEnvironmentVariable("RAV_UTP_SERVER_PRIVATE_KEY", null);
            Environment.SetEnvironmentVariable("RAV_SERVER_REGION", null);
            Environment.SetEnvironmentVariable("RAV_SERVER_ADDRESS", null);
            Environment.SetEnvironmentVariable("RAV_SERVER_PORT", null);
            Environment.SetEnvironmentVariable("RAV_SERVER_ID", null);
            Environment.SetEnvironmentVariable("RAV_SERVER_CREDENTIAL", null);
            Environment.SetEnvironmentVariable("DedicatedServers__Provisioned__0__ServerId", null);
            Environment.SetEnvironmentVariable("DedicatedServers__Provisioned__0__Credential", null);
            Environment.SetEnvironmentVariable("DATABASE_URL", null);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
            Environment.SetEnvironmentVariable("WOM_API_BASE", null);
            Environment.SetEnvironmentVariable("API_BASE", null);
            Environment.SetEnvironmentVariable("WOM_SERVER_REGION", null);
            Environment.SetEnvironmentVariable("WOM_SERVER_ADDRESS", null);
            Environment.SetEnvironmentVariable("WOM_SERVER_PORT", null);
            Environment.SetEnvironmentVariable("WOM_UTP_SERVER_CERTIFICATE", null);
            Environment.SetEnvironmentVariable("WOM_UTP_SERVER_PRIVATE_KEY", null);
            Environment.SetEnvironmentVariable("WOM_SERVER_ID", null);
            Environment.SetEnvironmentVariable("WOM_SERVER_CREDENTIAL", null);
            Environment.SetEnvironmentVariable("WOM_UTP_CA_CERTIFICATE", null);
            ClientAuthenticationState.Clear();
            return true;
        }

        private static string ValidateSelection()
        {
            if (!Uri.TryCreate(LocalApiUrl, UriKind.Absolute, out Uri localApi) || !localApi.IsLoopback || localApi.Scheme != Uri.UriSchemeHttps)
            {
                return "Local API must use an explicit HTTPS loopback URL. No production fallback is allowed.";
            }

            if (!Uri.TryCreate(NetcupApiUrl, UriKind.Absolute, out Uri netcupApi)
                || netcupApi.Scheme != Uri.UriSchemeHttps
                || netcupApi.Host.IndexOf("staging", StringComparison.OrdinalIgnoreCase) < 0
                || string.Equals(netcupApi.Host, "war-of-machines-api.onrender.com", StringComparison.OrdinalIgnoreCase))
            {
                return "Netcup API must use its HTTPS staging endpoint. Production fallback is blocked.";
            }

            return string.Empty;
        }

        private static void RefreshStatus()
        {
            if (!IsBusy)
            {
                StartOperation("Refresh Status", RefreshStatusRoutine());
            }
        }

        private static IEnumerator RefreshStatusRoutine()
        {
            BeginOperationStage("Checking API health, database readiness, and server heartbeat");
            if (!string.IsNullOrEmpty(ValidateSelection()))
            {
                databaseStatus = ComponentStatus.Unknown;
                apiStatus = ComponentStatus.Unknown;
                serverStatus = ComponentStatus.Unknown;
                AddLog("Status refresh blocked: " + ValidateSelection());
                yield break;
            }

            string apiBase = SelectedEnvironment == Location.Local ? LocalApiUrl : NetcupApiUrl;
            Task<bool> healthTask = CheckApiHealthAsync(apiBase);
            Task<bool> apiListenerTask = SelectedEnvironment == Location.Local ? CheckApiEndpointListeningAsync(apiBase) : null;
            Task<string> serversTask = GetServerStatusJsonAsync(apiBase);
            Task<bool> databaseTask = CheckNetcupDatabaseAsync();
            double statusDeadline = EditorApplication.timeSinceStartup + StatusRefreshTimeoutSeconds;
            while ((!healthTask.IsCompleted || (apiListenerTask != null && !apiListenerTask.IsCompleted)
                    || !serversTask.IsCompleted || !databaseTask.IsCompleted)
                   && EditorApplication.timeSinceStartup < statusDeadline)
            {
                SetOperationWaiting("Waiting for API, database, and server status checks...");
                yield return null;
            }

            if (!healthTask.IsCompleted || (apiListenerTask != null && !apiListenerTask.IsCompleted)
                || !serversTask.IsCompleted || !databaseTask.IsCompleted)
            {
                apiStatus = ComponentStatus.Failed;
                databaseStatus = ComponentStatus.Failed;
                serverStatus = ComponentStatus.Unknown;
                AddLog("Status refresh timed out after " + StatusRefreshTimeoutSeconds + " seconds; no services were started or stopped.");
                yield break;
            }

            bool apiReady = healthTask.Result;
            if (SelectedEnvironment == Location.Local
                && apiReady
                && (!IsManagedProcessRunning(LocalApiRole)
                    || !IsManagedProcessRunning(LocalDatabaseTunnelRole)
                    || !LocalApiUsesNetcupTunnel))
            {
                apiReady = false;
                AddLog("Local API health response is not trusted until its panel-managed Netcup tunnel is verified.", LiveLogLevel.Warning);
            }

            AddLog((SelectedEnvironment == Location.Netcup ? "Netcup" : "Local") + " API HTTPS health check "
                + (apiReady ? "passed." : "failed."), apiReady ? LiveLogLevel.Success : LiveLogLevel.Warning);
            apiStatus = apiReady
                ? ComponentStatus.Ready
                : apiListenerTask != null && !apiListenerTask.Result ? ComponentStatus.Stopped : ComponentStatus.Failed;
            databaseStatus = databaseTask.Result ? ComponentStatus.Ready : ComponentStatus.Failed;
            AddLog("Netcup PostgreSQL SSH health check " + (databaseTask.Result ? "passed." : "failed."),
                databaseTask.Result ? LiveLogLevel.Success : LiveLogLevel.Warning);

            bool registered = ServerStatusMatches(serversTask.Result, SelectedEnvironment == Location.Local ? "127.0.0.1" : NetcupServerHost,
                SelectedEnvironment == Location.Local ? LocalServerPort : NetcupServerPort);

            if (SelectedEnvironment == Location.Local)
            {
                serverStatus = apiStatus != ComponentStatus.Ready
                    ? ComponentStatus.Unknown
                    : registered
                    ? ComponentStatus.Ready
                    : IsManagedProcessRunning(LocalServerRole) ? ComponentStatus.Starting : ComponentStatus.Stopped;
            }
            else
            {
                serverStatus = registered
                    ? ComponentStatus.Ready
                    : apiReady ? ComponentStatus.Failed : ComponentStatus.Unknown;

                AddLog("Netcup Dedicated Server status is based on its API registration and fresh heartbeat; SSH/systemd checks are available in remote actions.");
            }

            AddLog(registered ? "Dedicated Server registration and heartbeat verified." : "No fresh Dedicated Server registration/heartbeat was found.",
                registered ? LiveLogLevel.Success : LiveLogLevel.Warning);
            AddLog("Status: Database " + databaseStatus + ", API " + apiStatus + ", Dedicated Server " + serverStatus + ".");
        }

        private static IEnumerator BuildApiRoutine()
        {
            if (SelectedEnvironment == Location.Netcup)
            {
                string repository = Directory.GetParent(ApiProjectRoot)?.FullName;
                string dockerfile = Path.Combine(repository ?? string.Empty, "Dockerfile");
                if (string.IsNullOrEmpty(repository) || !File.Exists(dockerfile))
                {
                    lastError = "API Dockerfile was not found in the configured RAV API repository.";
                    yield break;
                }

                string tag = "rav-api:staging-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                BeginOperationStage("Building the Netcup API image locally");
                AddLog("Building a staging-only API image; no remote service will change.", LiveLogLevel.Active);
                yield return EnsureDockerLinuxEngineRoutine();
                if (!string.IsNullOrEmpty(lastError)) yield break;

                Task<CommandResult> imageTask = RunProcessAsync("docker.exe", new[] { "build", "--file", dockerfile, "--tag", tag, repository }, repository, 900000, null);
                SetOperationWaiting("Waiting for the API container image build...");
                yield return imageTask;
                AddCommandOutput(imageTask.Result);
                if (!imageTask.Result.Success)
                {
                    lastError = "Netcup API image build failed. No remote service was changed.";
                    yield break;
                }

                Task<CommandResult> inspectTask = RunProcessAsync("docker.exe", new[] { "image", "inspect", tag }, repository, 30000, null);
                yield return inspectTask;
                if (!inspectTask.Result.Success)
                {
                    lastError = "Built Netcup API image could not be verified locally.";
                    yield break;
                }

                PreparedNetcupApiImage = tag;
                AddLog("Netcup API image built and verified: " + tag + ".", LiveLogLevel.Success);
                yield break;
            }

            string project = Path.Combine(ApiProjectRoot, "RavApi.csproj");
            if (!File.Exists(project))
            {
                lastError = "API project file was not found under the configured API project path.";
                yield break;
            }

            BeginOperationStage("Building API (.NET Debug; exact percentage unavailable)");
            string outputRoot = Path.Combine(LogsRoot, "LocalApiBuilds");
            string buildOutput = Path.Combine(outputRoot, "api-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture));
            string outputArgument = "-p:OutputPath=" + buildOutput + Path.DirectorySeparatorChar;
            AddLog("Building Local API into an isolated output folder; the existing API process and its files will not be overwritten.", LiveLogLevel.Active);
            Task<CommandResult> task = RunProcessAsync("dotnet.exe", new[] { "build", project, "--configuration", "Debug", "--nologo", "--verbosity", "minimal", "-p:UseAppHost=false", outputArgument }, ApiProjectRoot, 600000, null);
            SetOperationWaiting("Waiting for the Local API build...");
            yield return task;
            AddCommandOutput(task.Result);
            string builtAssembly = Path.Combine(buildOutput, "RavApi.dll");
            if (!task.Result.Success || !File.Exists(builtAssembly))
            {
                lastError = task.Result.TimedOut ? "API build timed out. See Open Full Log for details." : "API build failed. See Open Full Log for details.";
                yield break;
            }

            PreparedLocalApiBuild = builtAssembly;
            AddLog("Local API build passed.", LiveLogLevel.Success);
        }

        private static IEnumerator EnsureDockerLinuxEngineRoutine()
        {
            AddLog("Checking Docker Linux engine...", LiveLogLevel.Active);
            Task<CommandResult> check = RunProcessAsync("docker.exe", new[] { "info", "--format", "{{.OSType}}" }, ProjectRoot, 15000, null);
            SetOperationWaiting("Checking Docker Linux engine readiness...");
            yield return check;
            CommandResult lastCheck = check.Result;
            if (IsLinuxDockerEngineReady(lastCheck))
            {
                AddLog("Docker Linux engine is ready.", LiveLogLevel.Success);
                yield break;
            }

            string dockerDesktop = FindDockerDesktopExecutable();
            if (string.IsNullOrWhiteSpace(dockerDesktop))
            {
                AddCommandOutput(lastCheck);
                lastError = "Docker Linux engine is unavailable and Docker Desktop was not found. Install or start Docker Desktop, then retry the Netcup API build.";
                yield break;
            }

            AddLog("Docker Linux engine is stopped; starting the installed Docker Desktop and waiting for readiness.", LiveLogLevel.Warning);
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = dockerDesktop,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        lastError = "Docker Desktop could not be started. Start it manually and retry the Netcup API build.";
                        yield break;
                    }
                }
            }
            catch (Exception exception)
            {
                lastError = "Docker Desktop could not be started: " + Sanitize(exception.Message);
                yield break;
            }

            double deadline = EditorApplication.timeSinceStartup + DefaultStartupTimeout;
            double nextCheckAt = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (EditorApplication.timeSinceStartup < nextCheckAt)
                {
                    SetOperationWaiting("Waiting for Docker Desktop Linux engine...");
                    yield return null;
                    continue;
                }

                check = RunProcessAsync("docker.exe", new[] { "info", "--format", "{{.OSType}}" }, ProjectRoot, 15000, null);
                yield return check;
                lastCheck = check.Result;
                if (IsLinuxDockerEngineReady(lastCheck))
                {
                    AddLog("Docker Linux engine is ready.", LiveLogLevel.Success);
                    yield break;
                }

                nextCheckAt = EditorApplication.timeSinceStartup + 2.0;
            }

            AddCommandOutput(lastCheck);
            lastError = "Docker Desktop did not make its Linux engine ready within "
                + DefaultStartupTimeout.ToString(CultureInfo.InvariantCulture)
                + " seconds. See Open Full Log for the Docker diagnostic.";
        }

        private static bool IsLinuxDockerEngineReady(CommandResult result)
        {
            return result != null
                && result.Success
                && string.Equals(result.Output.Trim(), "linux", StringComparison.OrdinalIgnoreCase);
        }

        private static string FindDockerDesktopExecutable()
        {
            string localPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "DockerDesktop", "Docker Desktop.exe");
            string programFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe");
            string programFilesX86 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Docker", "Docker", "Docker Desktop.exe");
            string[] candidates = { localPrograms, programFiles, programFilesX86 };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i])) return candidates[i];
            }

            return string.Empty;
        }

        private static IEnumerator BuildServerRoutine()
        {
            if (EditorApplication.isPlaying)
            {
                lastError = "Exit Play Mode before building the Dedicated Server.";
                yield break;
            }

            try
            {
                if (SelectedEnvironment == Location.Local)
                {
                    BeginOperationStage("Building Windows Dedicated Server (Unity progress shown by Editor)");
                    string output = LocalNetworkBuild.BuildControlPanelServer(ServerBuildRoot);
                    AddLog("Local Windows Dedicated Server build passed: " + output, LiveLogLevel.Success);
                }
                else
                {
                    BeginOperationStage("Building Linux x86-64 Mono Dedicated Server (Unity progress shown by Editor)");
                    string output = LocalNetworkBuild.BuildControlPanelLinuxServer(ServerBuildRoot);
                    if (!ValidateLinuxServerBuild(output, out string validationError))
                    {
                        lastError = validationError;
                        yield break;
                    }

                    EditorPrefs.SetString(Pref + "LatestNetcupServerBuild", output);
                    AddLog("Netcup Linux Dedicated Server build passed: " + output, LiveLogLevel.Success);
                }
            }
            catch (Exception exception)
            {
                lastError = "Dedicated Server build failed: " + exception.Message;
            }
            yield return null;
        }

        private static IEnumerator ExecuteApiRoutine()
        {
            if (SelectedEnvironment == Location.Local)
            {
                if (LocalBuildChecked) { yield return BuildApiRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Local API build skipped — using the existing build.", LiveLogLevel.Warning);
                if (LocalStartChecked) yield return StartApiRoutine();
                else AddLog("Local API start skipped — Start is not selected.", LiveLogLevel.Warning);
            }
            else
            {
                if (NetcupBuildChecked) { yield return BuildApiRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Netcup API build skipped — using the last prepared image.", LiveLogLevel.Warning);
                if (NetcupDeployChecked) { yield return DeployApiRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Netcup API deploy skipped — Deploy is not selected.", LiveLogLevel.Warning);
                if (NetcupStartChecked) yield return StartApiRoutine();
                else AddLog("Netcup API start skipped — Start is not selected.", LiveLogLevel.Warning);
            }
        }

        private static IEnumerator ExecuteServerRoutine()
        {
            if (SelectedEnvironment == Location.Local)
            {
                if (LocalBuildChecked) { yield return BuildServerRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Local Dedicated Server build skipped — using the existing Windows build.", LiveLogLevel.Warning);
                if (LocalStartChecked) yield return StartServerRoutine(false);
                else AddLog("Local Dedicated Server start skipped — Start is not selected.", LiveLogLevel.Warning);
            }
            else
            {
                if (NetcupBuildChecked) { yield return BuildServerRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Netcup Dedicated Server build skipped — using the last prepared Linux build.", LiveLogLevel.Warning);
                if (NetcupDeployChecked) { yield return DeployServerRoutine(); if (!string.IsNullOrEmpty(lastError)) yield break; }
                else AddLog("Netcup Dedicated Server deploy skipped — Deploy is not selected.", LiveLogLevel.Warning);
                if (NetcupStartChecked) yield return StartServerRoutine(false);
                else AddLog("Netcup Dedicated Server start skipped — Start is not selected.", LiveLogLevel.Warning);
            }
        }

        private static IEnumerator ExecuteAllRoutine()
        {
            AddLog("Execute All will process API first, then Dedicated Server.", LiveLogLevel.Active);
            yield return ExecuteApiRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;
            yield return ExecuteServerRoutine();
        }

        private static bool ValidateLinuxServerBuild(string buildPath, out string error)
        {
            error = string.Empty;
            string[] required =
            {
                "RAVServer.x86_64",
                Path.Combine("RAVServer_Data", "globalgamemanagers"),
                Path.Combine("RAVServer_Data", "Managed", "Assembly-CSharp.dll"),
                Path.Combine("RAVServer_Data", "MonoBleedingEdge", "x86_64", "libmonobdwgc-2.0.so"),
                "UnityPlayer.so"
            };
            if (string.IsNullOrWhiteSpace(buildPath) || !Directory.Exists(buildPath))
            {
                error = "Linux Dedicated Server build output is missing.";
                return false;
            }

            for (int i = 0; i < required.Length; i++)
            {
                if (!File.Exists(Path.Combine(buildPath, required[i])))
                {
                    error = "Linux Dedicated Server build is incomplete: " + required[i] + " is missing.";
                    return false;
                }
            }

            return true;
        }

        private static IEnumerator DeployApiRoutine()
        {
            string tag = PreparedNetcupApiImage;
            if (string.IsNullOrWhiteSpace(tag) || !Regex.IsMatch(tag, @"^rav-api:staging-[A-Za-z0-9][A-Za-z0-9._-]{0,63}$"))
            {
                lastError = "Build the Netcup API image first; no staged image is available.";
                yield break;
            }

            string safeTag = tag.Substring("rav-api:staging-".Length);
            string archive = Path.Combine(LogsRoot, "rav-api-" + safeTag + ".tar");
            string script = Path.Combine(ApiDeploymentScriptsRoot, "activate-api-image.sh");
            if (!File.Exists(script))
            {
                lastError = "The API activation helper is missing from the existing staging deployment scripts.";
                yield break;
            }

            BeginOperationStage("Verifying and exporting the prepared Netcup API image");
            if (File.Exists(archive))
            {
                lastError = "The API image archive already exists; refusing to overwrite it.";
                yield break;
            }

            Task<CommandResult> save = RunProcessAsync("docker.exe", new[] { "save", "--output", archive, tag }, ProjectRoot, 180000, null);
            SetOperationWaiting("Waiting for the API image archive to be written...");
            yield return save;
            AddCommandOutput(save.Result);
            if (!save.Result.Success || !File.Exists(archive))
            {
                lastError = "Could not export the prepared Netcup API image.";
                yield break;
            }

            string incoming = "/home/ravadmin/rav-staging-incoming";
            string remoteArchive = incoming + "/" + Path.GetFileName(archive);
            string remoteScript = incoming + "/activate-api-image.sh";
            Task<CommandResult> directory = RunSshCommandAsync("install -d -m 0700 " + incoming);
            yield return directory;
            if (!directory.Result.Success) { lastError = "Could not prepare the restricted Netcup staging upload directory."; yield break; }

            BeginOperationStage("Transferring the API image and activation helper without starting services");
            Task<CommandResult> uploadImage = RunScpAsync(archive, remoteArchive);
            SetOperationWaiting("Transferring the API image to Netcup over verified SSH...");
            yield return uploadImage;
            if (!uploadImage.Result.Success) { AddCommandOutput(uploadImage.Result); lastError = "Netcup API image transfer failed."; yield break; }
            Task<CommandResult> uploadScript = RunScpAsync(script, remoteScript);
            yield return uploadScript;
            if (!uploadScript.Result.Success) { AddCommandOutput(uploadScript.Result); lastError = "Netcup API activation helper transfer failed."; yield break; }

            BeginOperationStage("Loading and verifying the staged API image on Netcup");
            string verify = "sudo -n docker load --input " + remoteArchive
                + " >/dev/null && sudo -n docker image inspect " + tag + " >/dev/null"
                + " && test -s " + remoteScript;
            Task<CommandResult> verifyTask = RunSshCommandAsync(verify);
            SetOperationWaiting("Waiting for remote image verification; active services remain unchanged...");
            yield return verifyTask;
            if (!verifyTask.Result.Success) { AddCommandOutput(verifyTask.Result); lastError = "Netcup did not verify the transferred API image."; yield break; }

            DeployedNetcupApiImage = tag;
            AddLog("Netcup API image is staged and verified. API, Caddy, and PostgreSQL were not restarted.", LiveLogLevel.Success);
        }

        private static IEnumerator DeployServerRoutine()
        {
            string buildPath = EditorPrefs.GetString(Pref + "LatestNetcupServerBuild", string.Empty);
            if (!ValidateLinuxServerBuild(buildPath, out string validationError)) { lastError = validationError; yield break; }
            string releaseId = "panel-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
            string archive = Path.Combine(LogsRoot, "RAVServer-" + releaseId + ".tar.gz");
            if (File.Exists(archive)) { lastError = "Server package already exists; refusing to overwrite it."; yield break; }

            BeginOperationStage("Packaging the verified Linux Dedicated Server build");
            string[] tarArgs = { "-czf", archive, "--exclude=./RAV_BurstDebugInformation_DoNotShip", "-C", buildPath, "." };
            Task<CommandResult> package = RunProcessAsync("tar.exe", tarArgs, buildPath, 900000, null);
            SetOperationWaiting("Packaging Linux server files; the active Netcup release remains unchanged...");
            yield return package;
            AddCommandOutput(package.Result);
            if (!package.Result.Success || !File.Exists(archive)) { lastError = "Linux Dedicated Server package creation failed."; yield break; }

            string incoming = "/home/ravadmin/rav-staging-incoming";
            string remoteArchive = incoming + "/" + Path.GetFileName(archive);
            Task<string> hashTask = Task.Run(() => ComputeSha256(archive));
            SetOperationWaiting("Calculating the archive checksum without blocking the Unity Editor...");
            yield return hashTask;
            string hash = hashTask.Result;
            Task<CommandResult> directory = RunSshCommandAsync("install -d -m 0700 " + incoming);
            yield return directory;
            if (!directory.Result.Success) { lastError = "Could not prepare the restricted Netcup server upload directory."; yield break; }

            BeginOperationStage("Transferring the Linux server archive without activating it");
            Task<CommandResult> upload = RunScpAsync(archive, remoteArchive);
            SetOperationWaiting("Transferring the Linux Dedicated Server to Netcup over verified SSH...");
            yield return upload;
            if (!upload.Result.Success) { AddCommandOutput(upload.Result); lastError = "Netcup Dedicated Server transfer failed."; yield break; }

            BeginOperationStage("Verifying the staged server archive on Netcup");
            string verify = "test \"$(sha256sum " + remoteArchive + " | cut -d ' ' -f1)\" = " + hash
                + " && tar -tzf " + remoteArchive + " | grep -Eq '(^|/)RAVServer\\.x86_64$'"
                + " && tar -tzf " + remoteArchive + " | grep -Eq '(^|/)UnityPlayer\\.so$'"
                + " && tar -tzf " + remoteArchive + " | grep -Eq '(^|/)RAVServer_Data/globalgamemanagers$'";
            Task<CommandResult> verifyTask = RunSshCommandAsync(verify);
            SetOperationWaiting("Waiting for remote checksum and build-content verification...");
            yield return verifyTask;
            if (!verifyTask.Result.Success) { AddCommandOutput(verifyTask.Result); lastError = "Netcup did not verify the transferred Linux server archive."; yield break; }

            PreparedNetcupServerRelease = releaseId;
            DeployedNetcupServerRelease = releaseId;
            EditorPrefs.SetString(Pref + "NetcupServerArchive", remoteArchive);
            AddLog("Netcup Linux server release " + releaseId + " is staged and verified; no service was activated or restarted.", LiveLogLevel.Success);
        }

        private static IEnumerator BuildClientRoutine()
        {
            if (EditorApplication.isPlaying)
            {
                lastError = "Exit Play Mode before building the Client.";
                yield break;
            }

            string validation = ValidateSelection();
            if (!string.IsNullOrEmpty(validation))
            {
                lastError = validation;
                yield break;
            }

            try
            {
                BeginOperationStage("Building Client (Unity build percentage unavailable)");
                string output = LocalNetworkBuild.BuildControlPanelClient(ClientBuildRoot);
                AddLog("Windows Client build passed: " + output);
            }
            catch (Exception exception)
            {
                lastError = "Windows Client build failed: " + exception.Message;
            }
            yield return null;
        }

        private static IEnumerator LaunchClientRoutine()
        {
            BeginOperationStage("Applying Client API and DTLS settings");
            if (EditorApplication.isPlaying)
            {
                lastError = "Exit Play Mode before launching a standalone Client.";
                yield break;
            }

            if (!TryApplyClientEnvironment("Launch Client", out Dictionary<string, string> environment, out string error))
            {
                lastError = error;
                yield break;
            }

            string executable = FindLatestBuild(ClientBuildRoot, "RAVClient.exe");
            if (string.IsNullOrEmpty(executable))
            {
                lastError = "No Windows Client build was found. Click Build Client first.";
                yield break;
            }

            string clientLog = GetLogPath("local-client");
            string clientArguments = JoinWindowsArguments(new[] { "-apiBase", SelectedEnvironment == Location.Local ? LocalApiUrl : NetcupApiUrl, "-logFile", clientLog });
            BeginOperationStage("Launching the standalone Client");
            Task<ManagedProcessInfo> task = StartManagedProcessAsync(executable, clientArguments, Path.GetDirectoryName(executable), "client", environment, false, clientLog + ".stdout.log", clientLog + ".stderr.log");
            yield return task;
            if (task.Result == null)
            {
                lastError = "Standalone Client did not start. See the operation log.";
                yield break;
            }

            SaveManagedProcess(LocalClientRole, task.Result, clientLog, clientLog + ".stderr.log");
            AddLog("Standalone Client launched with the selected explicit API and public DTLS trust settings.");
        }

        private static IEnumerator StartApiRoutine()
        {
            if (SelectedEnvironment == Location.Netcup)
            {
                yield return StartNetcupApiRoutine();
                yield break;
            }

            yield return EnsureLocalSecurityRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;
            yield return EnsureNetcupDatabaseTunnelRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;

            if (IsManagedProcessRunning(LocalApiRole) && !LocalApiUsesNetcupTunnel)
            {
                AddLog("Restarting the panel-owned Local API to apply the verified Netcup database tunnel configuration.", LiveLogLevel.Warning);
                yield return StopManagedProcessRoutine(LocalApiRole);
                if (!string.IsNullOrEmpty(lastError)) yield break;
            }

            BeginOperationStage("Checking Local API health against Netcup PostgreSQL");
            Task<bool> healthTask = CheckApiHealthAsync(LocalApiUrl);
            yield return healthTask;
            if (healthTask.Result)
            {
                if (IsManagedProcessRunning(LocalApiRole))
                {
                    apiStatus = ComponentStatus.Ready;
                    AddLog("Local API is already healthy and owned by this panel; duplicate startup was skipped.", LiveLogLevel.Success);
                }
                else
                {
                    apiStatus = ComponentStatus.Failed;
                    lastError = "A Local API is already answering on the configured URL but is not owned by this panel. It was left unchanged to avoid using the wrong database.";
                }

                yield break;
            }

            if (IsManagedProcessRunning(LocalApiRole))
            {
                apiStatus = ComponentStatus.Starting;
                AddLog("Local API process is already running; waiting for health instead of starting a duplicate.", LiveLogLevel.Warning);
                yield return WaitForApiReadyRoutine(LocalApiUrl);
                yield break;
            }

            Task<bool> localListener = CheckApiEndpointListeningAsync(LocalApiUrl);
            yield return localListener;
            if (localListener.Result)
            {
                apiStatus = ComponentStatus.Failed;
                lastError = "The Local API port is occupied by an unowned process. It was left unchanged.";
                yield break;
            }

            string apiProject = Path.Combine(ApiProjectRoot, "RavApi.csproj");
            string apiAssembly = PreparedLocalApiBuild;
            string latestIsolatedBuild = FindLatestBuild(Path.Combine(DevelopmentControlPanelRoot, "LocalApiBuilds"), "RavApi.dll");
            if (!string.IsNullOrWhiteSpace(latestIsolatedBuild)
                && (!File.Exists(apiAssembly)
                    || File.GetLastWriteTimeUtc(latestIsolatedBuild) > File.GetLastWriteTimeUtc(apiAssembly)))
            {
                apiAssembly = latestIsolatedBuild;
                PreparedLocalApiBuild = apiAssembly;
                AddLog("Using the newest isolated Local API build; no rebuild was requested.", LiveLogLevel.Warning);
            }

            if (string.IsNullOrWhiteSpace(apiAssembly) || !File.Exists(apiAssembly))
            {
                apiAssembly = Path.Combine(ApiProjectRoot, "bin", "Debug", "net10.0", "RavApi.dll");
                if (File.Exists(apiAssembly)) AddLog("Using the existing Local API build; no rebuild was requested.", LiveLogLevel.Warning);
            }

            if (!File.Exists(apiProject) || !File.Exists(apiAssembly))
            {
                lastError = "Local API build output is missing. Build the API first or enable Build API before start.";
                yield break;
            }

            if (!TryReadEnvironmentFile(Path.Combine(LocalSecurityPath, "local-security.env"), out Dictionary<string, string> environment, out string error))
            {
                lastError = error;
                yield break;
            }

            BeginOperationStage("Loading the staging database credential from the encrypted vault");
            Task<CommandResult> vaultTask = ReadVaultDatabaseUrlAsync();
            SetOperationWaiting("Reading the encrypted database URL without displaying or logging it...");
            yield return vaultTask;
            if (!vaultTask.Result.Success || !TryDecodeVaultDatabaseUrl(vaultTask.Result.Output, out string databaseUrl))
            {
                lastError = "The rav_staging database connection is unavailable in the encrypted secure vault.";
                yield break;
            }

            if (!TryValidateStagingDatabaseUrl(databaseUrl))
            {
                lastError = "The secure vault database connection is not the verified rav_staging TLS connection. Startup is blocked.";
                yield break;
            }

            if (!HasEnvironmentValue(environment, "DedicatedServers__Provisioned__0__ServerId")
                || !HasEnvironmentValue(environment, "DedicatedServers__Provisioned__0__Credential"))
            {
                lastError = "Local API Dedicated Server registration credentials are missing from the local security configuration.";
                yield break;
            }

            RemoveEnvironmentValues(environment,
                "RAV_SERVER_ID",
                "RAV_SERVER_CREDENTIAL",
                "RAV_SERVER_REGION",
                "RAV_SERVER_ADDRESS",
                "RAV_SERVER_PORT",
                "WOM_SERVER_ID",
                "WOM_SERVER_CREDENTIAL",
                "WOM_SERVER_REGION",
                "WOM_SERVER_ADDRESS",
                "WOM_SERVER_PORT",
                "RAV_UTP_SERVER_CERTIFICATE",
                "RAV_UTP_SERVER_PRIVATE_KEY",
                "RAV_UTP_SERVER_COMMON_NAME",
                "RAV_UTP_CA_CERTIFICATE",
                "WOM_UTP_SERVER_CERTIFICATE",
                "WOM_UTP_SERVER_PRIVATE_KEY",
                "WOM_UTP_SERVER_COMMON_NAME",
                "WOM_UTP_CA_CERTIFICATE",
                "RAV_DATABASE_TUNNEL_LOCAL_PORT",
                "RAV_DATABASE_ROOT_CERTIFICATE",
                "RAV_DATABASE_TLS_SERVER_NAME",
                "Database__ApplyMigrationsOnStartup",
                "Database__RunSeedOnStartup",
                "Database__SeedDevelopmentFixtures",
                "ConnectionStrings__DefaultConnection");
            environment["DATABASE_URL"] = databaseUrl;
            environment["RAV_DATABASE_TUNNEL_LOCAL_PORT"] = LocalDatabaseTunnelPort.ToString(CultureInfo.InvariantCulture);
            environment["RAV_DATABASE_ROOT_CERTIFICATE"] = PostgresCaPath;
            environment["RAV_DATABASE_TLS_SERVER_NAME"] = "postgres";
            environment["Database__ApplyMigrationsOnStartup"] = "false";
            environment["Database__RunSeedOnStartup"] = "false";
            environment["Database__SeedDevelopmentFixtures"] = "false";
            environment["RAV_API_BASE"] = LocalApiUrl;
            environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            environment["DOTNET_ENVIRONMENT"] = "Development";
            environment["ASPNETCORE_URLS"] = "https://localhost:7216;http://localhost:5220";
            string[] args = { apiAssembly };
            string apiLog = GetLogPath("local-api");
            BeginOperationStage("Starting Local API with migrations and fixtures disabled");
            Task<ManagedProcessInfo> startTask = StartManagedProcessAsync("dotnet.exe", JoinWindowsArguments(args), ApiProjectRoot, "api", environment, true, apiLog + ".out.log", apiLog + ".err.log");
            yield return startTask;
            environment.Remove("DATABASE_URL");
            databaseUrl = null;
            if (startTask.Result == null)
            {
                lastError = "Local API launch failed. See the operation log.";
                apiStatus = ComponentStatus.Failed;
                yield break;
            }

            SaveManagedProcess(LocalApiRole, startTask.Result, apiLog + ".out.log", apiLog + ".err.log");
            LocalApiUsesNetcupTunnel = true;
            apiStatus = ComponentStatus.Starting;
            yield return WaitForApiReadyRoutine(LocalApiUrl);
            databaseUrl = null;
        }

        private static IEnumerator StartNetcupApiRoutine()
        {
            BeginOperationStage("Checking Netcup API health over verified HTTPS");
            Task<bool> health = CheckApiHealthAsync(NetcupApiUrl);
            SetOperationWaiting("Waiting for Netcup API readiness...");
            yield return health;
            apiStatus = health.Result ? ComponentStatus.Ready : ComponentStatus.Stopped;

            string image = DeployedNetcupApiImage;
            if (!string.IsNullOrWhiteSpace(image))
            {
                string releaseId = image.Substring("rav-api:staging-".Length);
                if (!EditorUtility.DisplayDialog("Activate Netcup API Release", "Activate the staged API image " + releaseId + " on Netcup? Only the API container will be updated. PostgreSQL and Caddy remain running; the existing API rollback script is used if readiness fails.", "Activate API", "Cancel"))
                {
                    AddLog("Netcup API activation cancelled; the staged image remains ready.", LiveLogLevel.Warning);
                    yield break;
                }

                BeginOperationStage("Activating the staged Netcup API image with rollback protection");
                string script = "/home/ravadmin/rav-staging-incoming/activate-api-image.sh";
                Task<CommandResult> activation = RunSshCommandAsync("sudo -n bash " + script + " " + image);
                SetOperationWaiting("Waiting for API activation and HTTPS readiness; PostgreSQL will remain unchanged...");
                yield return activation;
                AddCommandOutput(activation.Result);
                if (!activation.Result.Success)
                {
                    apiStatus = ComponentStatus.Failed;
                    lastError = "Netcup API activation failed. The existing rollback script was invoked by the activation helper.";
                    yield break;
                }

                AddLog("Netcup API release activated; PostgreSQL and Caddy were not restarted.", LiveLogLevel.Success);
            }
            else if (health.Result)
            {
                AddLog("Netcup API is already healthy; startup was skipped.", LiveLogLevel.Success);
                yield break;
            }
            else
            {
                BeginOperationStage("Starting only the existing Netcup API container");
                string remote = "sudo -n docker compose --project-name rav-staging --project-directory /opt/rav-staging --env-file /etc/rav-staging/staging.env --file /opt/rav-staging/compose.yaml up -d --no-build --no-deps --wait api";
                Task<CommandResult> start = RunSshCommandAsync(remote);
                SetOperationWaiting("Waiting for the existing API image to start; database and proxy are not targeted...");
                yield return start;
                if (!start.Result.Success) { AddCommandOutput(start.Result); lastError = "Netcup API could not be started from its current prepared image."; yield break; }
            }

            Task<bool> finalHealth = CheckApiHealthAsync(NetcupApiUrl);
            SetOperationWaiting("Waiting for Netcup API liveness and readiness...");
            yield return finalHealth;
            apiStatus = finalHealth.Result ? ComponentStatus.Ready : ComponentStatus.Failed;
            if (!finalHealth.Result) lastError = "Netcup API did not become ready after startup.";
            else AddLog("Netcup API is healthy over HTTPS.", LiveLogLevel.Success);
        }

        private static IEnumerator EnsureNetcupDatabaseTunnelRoutine()
        {
            BeginOperationStage("Checking private Netcup PostgreSQL and local SSH tunnel");
            if (IsManagedProcessRunning(LocalDatabaseTunnelRole))
            {
                Task<bool> existingListener = CheckTcpPortListeningAsync(LocalDatabaseTunnelPort);
                yield return existingListener;
                if (existingListener.Result)
                {
                    AddLog("Protected Netcup PostgreSQL SSH tunnel is already running.", LiveLogLevel.Success);
                    yield break;
                }

                yield return StopManagedProcessRoutine(LocalDatabaseTunnelRole);
                if (!string.IsNullOrEmpty(lastError)) yield break;
            }

            Task<bool> databaseHealth = CheckNetcupDatabaseAsync();
            SetOperationWaiting("Checking Netcup PostgreSQL through verified SSH...");
            yield return databaseHealth;
            if (!databaseHealth.Result)
            {
                databaseStatus = ComponentStatus.Failed;
                lastError = "Netcup PostgreSQL is not healthy or SSH access failed; Local API was not started.";
                yield break;
            }

            Task<bool> tunnelPortOpen = CheckTcpPortListeningAsync(LocalDatabaseTunnelPort);
            yield return tunnelPortOpen;
            if (tunnelPortOpen.Result)
            {
                lastError = "Local SSH tunnel port is already occupied by an unowned process. It was left unchanged.";
                yield break;
            }

            Task<CommandResult> addressTask = RunSshCommandAsync("sudo -n docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' rav-staging-postgres-1");
            SetOperationWaiting("Resolving the private PostgreSQL container address over SSH...");
            yield return addressTask;
            string containerAddress = addressTask.Result.Success
                ? addressTask.Result.Output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                : string.Empty;
            if (!System.Net.IPAddress.TryParse(containerAddress, out System.Net.IPAddress parsedAddress)
                || parsedAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                || parsedAddress.ToString().StartsWith("127.", StringComparison.Ordinal))
            {
                lastError = "Could not resolve a private PostgreSQL container address; no public database route was attempted.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(SshKeyReference) || !File.Exists(SshKeyReference))
            {
                lastError = "The configured Netcup SSH key is missing; Local API was not started.";
                yield break;
            }

            BeginOperationStage("Starting the loopback-only SSH tunnel to private PostgreSQL");
            string forward = "127.0.0.1:" + LocalDatabaseTunnelPort.ToString(CultureInfo.InvariantCulture) + ":" + parsedAddress + ":5432";
            string[] arguments = { "-N", "-T", "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", "-o", "ConnectTimeout=5", "-o", "ExitOnForwardFailure=yes", "-o", "ServerAliveInterval=30", "-o", "ServerAliveCountMax=3", "-p", SshPort.ToString(CultureInfo.InvariantCulture), "-i", SshKeyReference, "-L", forward, SshTarget };
            string tunnelLog = GetLogPath("local-postgres-ssh-tunnel");
            Task<ManagedProcessInfo> tunnel = StartManagedProcessAsync("ssh.exe", JoinWindowsArguments(arguments), DeploymentRoot, "database-tunnel", null, true, tunnelLog, tunnelLog + ".stderr.log");
            SetOperationWaiting("Waiting for the SSH tunnel listener...");
            yield return tunnel;
            if (tunnel.Result == null)
            {
                lastError = "Could not start the protected SSH tunnel to Netcup PostgreSQL.";
                yield break;
            }

            SaveManagedProcess(LocalDatabaseTunnelRole, tunnel.Result, tunnelLog, tunnelLog + ".stderr.log");
            double deadline = EditorApplication.timeSinceStartup + 15;
            bool listening = false;
            while (EditorApplication.timeSinceStartup < deadline && IsManagedProcessRunning(LocalDatabaseTunnelRole))
            {
                Task<bool> probe = CheckTcpPortListeningAsync(LocalDatabaseTunnelPort);
                yield return probe;
                if (probe.Result) { listening = true; break; }
                yield return null;
            }

            if (!listening)
            {
                yield return StopManagedProcessRoutine(LocalDatabaseTunnelRole);
                lastError = "The Netcup PostgreSQL SSH tunnel did not open its loopback listener.";
                yield break;
            }

            AddLog("Protected PostgreSQL tunnel is listening on loopback port " + LocalDatabaseTunnelPort + ". PostgreSQL remains private.", LiveLogLevel.Success);
        }

        private static IEnumerator WaitForApiReadyRoutine(string apiBase)
        {
            double deadline = EditorApplication.timeSinceStartup + StartupTimeout;
            SetOperationWaiting("Waiting for API health readiness...");
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Task<bool> task = CheckApiHealthAsync(apiBase);
                yield return task;
                if (task.Result)
                {
                    apiStatus = ComponentStatus.Ready;
                    AddLog("API is ready — /health/live and /health/ready returned HTTP success.", LiveLogLevel.Success);
                    yield break;
                }

                if (!IsManagedProcessRunning(LocalApiRole)) break;
                SetOperationWaiting("Waiting for API health readiness...");
                yield return null;
            }

            apiStatus = ComponentStatus.Failed;
            lastError = "Local API did not become ready before the configured timeout.";
        }

        private static IEnumerator StopApiRoutine()
        {
            if (SelectedEnvironment == Location.Netcup)
            {
                if (!EditorUtility.DisplayDialog("Stop Netcup API", "Stop only the Netcup API container? PostgreSQL and Caddy will remain running.", "Stop API", "Cancel"))
                {
                    AddLog("Netcup API stop cancelled; no service changed.", LiveLogLevel.Warning);
                    yield break;
                }

                BeginOperationStage("Stopping only the Netcup API container");
                string remote = "sudo -n docker compose --project-name rav-staging --project-directory /opt/rav-staging --env-file /etc/rav-staging/staging.env --file /opt/rav-staging/compose.yaml stop --timeout 20 api";
                Task<CommandResult> task = RunSshCommandAsync(remote);
                SetOperationWaiting("Waiting for the Netcup API container to stop...");
                yield return task;
                if (!task.Result.Success) { AddCommandOutput(task.Result); lastError = "Netcup API could not be stopped."; yield break; }
                apiStatus = ComponentStatus.Stopped;
                AddLog("Netcup API stopped; PostgreSQL and Caddy were left running.", LiveLogLevel.Success);
                yield break;
            }

            BeginOperationStage("Stopping the Local API process");
            bool ownedApi = IsManagedProcessRunning(LocalApiRole);
            yield return StopManagedProcessRoutine(LocalApiRole);
            if (!string.IsNullOrEmpty(lastError)) yield break;
            if (ownedApi)
            {
                yield return StopManagedProcessRoutine(LocalDatabaseTunnelRole);
                if (!string.IsNullOrEmpty(lastError)) yield break;
                LocalApiUsesNetcupTunnel = false;
                AddLog("The Local API and its loopback-only database tunnel stopped; Netcup PostgreSQL remains running.");
            }
            else
            {
                LocalApiUsesNetcupTunnel = false;
                AddLog("No panel-owned Local API was stopped; any unowned tunnel was left unchanged.", LiveLogLevel.Warning);
            }

            apiStatus = ComponentStatus.Stopped;
            AddLog("Local API stop completed.", LiveLogLevel.Success);
        }

        private static IEnumerator StartServerRoutine(bool buildFirst)
        {
            if (SelectedEnvironment == Location.Netcup)
            {
                BeginOperationStage("Checking Netcup API readiness before server start");
                Task<bool> apiHealth = CheckApiHealthAsync(NetcupApiUrl);
                SetOperationWaiting("Waiting for Netcup API liveness and readiness...");
                yield return apiHealth;
                if (!apiHealth.Result)
                {
                    apiStatus = ComponentStatus.Failed;
                    lastError = "Netcup API is not ready; Dedicated Server was not started.";
                    yield break;
                }

                string release = DeployedNetcupServerRelease;
                if (!string.IsNullOrWhiteSpace(release))
                {
                    string archive = EditorPrefs.GetString(Pref + "NetcupServerArchive", string.Empty);
                    if (!Regex.IsMatch(release, @"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")
                        || !archive.StartsWith("/home/ravadmin/rav-staging-incoming/", StringComparison.Ordinal))
                    {
                        lastError = "The staged Netcup server release metadata is invalid; no service was changed.";
                        yield break;
                    }

                    if (!EditorUtility.DisplayDialog("Activate Netcup Server Release", "Activate staged Linux Dedicated Server release " + release + "? The active server will restart; the existing release rollback mechanism remains enabled.", "Activate Server", "Cancel"))
                    {
                        AddLog("Netcup server activation cancelled; the verified release remains staged.", LiveLogLevel.Warning);
                        yield break;
                    }

                    BeginOperationStage("Activating the staged Linux Dedicated Server release with rollback protection");
                    string healthUrl = NetcupApiUrl.TrimEnd('/') + "/health/ready";
                    string remoteDeploy = "sudo -n /opt/rav-staging/scripts/deploy-unity-release.sh " + release + " " + archive + " "
                        + NetcupServerPort.ToString(CultureInfo.InvariantCulture) + " " + healthUrl;
                    Task<CommandResult> deploy = RunSshCommandAsync(remoteDeploy);
                    SetOperationWaiting("Waiting for release activation, UDP bind, and remote health checks...");
                    yield return deploy;
                    AddCommandOutput(deploy.Result);
                    if (!deploy.Result.Success)
                    {
                        serverStatus = ComponentStatus.Failed;
                        lastError = "Netcup server activation failed; the existing deployment script attempted rollback.";
                        yield break;
                    }

                    DeployedNetcupServerRelease = string.Empty;
                }
                else
                {
                    BeginOperationStage("Starting the existing Netcup Dedicated Server release");
                    string remoteStart = "if ! sudo -n systemctl is-active --quiet rav-staging-dedicated-server.service; then sudo -n systemctl start rav-staging-dedicated-server.service; fi"
                        + " && sudo -n /opt/rav-staging/scripts/health-check-unity.sh "
                        + NetcupServerPort.ToString(CultureInfo.InvariantCulture) + " " + NetcupApiUrl.TrimEnd('/') + "/health/ready";
                    Task<CommandResult> start = RunSshCommandAsync(remoteStart);
                    SetOperationWaiting("Waiting for the prepared Netcup server to start without rebuilding...");
                    yield return start;
                    if (!start.Result.Success) { AddCommandOutput(start.Result); lastError = "Netcup Dedicated Server could not start from its current release."; yield break; }
                }

                yield return WaitForServerReadyRoutine(NetcupApiUrl, NetcupServerHost, NetcupServerPort);
                yield break;
            }

            yield return EnsureLocalSecurityRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;
            yield return EnsureNetcupDatabaseTunnelRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;
            if (!IsManagedProcessRunning(LocalApiRole)
                || !IsManagedProcessRunning(LocalDatabaseTunnelRole)
                || !LocalApiUsesNetcupTunnel)
            {
                lastError = "The Local Dedicated Server requires a panel-managed Local API connected to the verified Netcup PostgreSQL tunnel.";
                yield break;
            }

            BeginOperationStage("Starting the Dedicated Server and verifying registration/heartbeat");
            Task<bool> apiTask = CheckApiHealthAsync(LocalApiUrl);
            SetOperationWaiting("Waiting for local API readiness before server startup...");
            yield return apiTask;
            if (!apiTask.Result)
            {
                lastError = "Start the Local API and PostgreSQL before starting the local Dedicated Server.";
                yield break;
            }

            if (buildFirst)
            {
                yield return BuildServerRoutine();
                if (!string.IsNullOrEmpty(lastError)) yield break;
            }

            if (IsManagedProcessRunning(LocalServerRole))
            {
                serverStatus = ComponentStatus.Starting;
                AddLog("Dedicated Server is already running; waiting for its heartbeat instead of launching a duplicate.", LiveLogLevel.Warning);
                yield return WaitForServerReadyRoutine(LocalApiUrl, "127.0.0.1", LocalServerPort);
                yield break;
            }

            string executable = FindLatestBuild(ServerBuildRoot, "RAVServer.exe");
            if (string.IsNullOrEmpty(executable))
            {
                lastError = "No local Windows Dedicated Server build was found. Build it first.";
                yield break;
            }

            if (!IsUdpPortAvailable(LocalServerPort))
            {
                lastError = "Local UDP port " + LocalServerPort + " is already occupied by a process not owned by this panel.";
                serverStatus = ComponentStatus.Unknown;
                yield break;
            }

            if (!TryReadEnvironmentFile(Path.Combine(LocalSecurityPath, "local-security.env"), out Dictionary<string, string> environment, out string error))
            {
                lastError = error;
                yield break;
            }

            string certificatePath = GetEnvironmentValue(environment, "RAV_UTP_SERVER_CERTIFICATE", "WOM_UTP_SERVER_CERTIFICATE");
            string privateKeyPath = GetEnvironmentValue(environment, "RAV_UTP_SERVER_PRIVATE_KEY", "WOM_UTP_SERVER_PRIVATE_KEY");
            if (!HasEnvironmentValue(environment, "RAV_SERVER_ID")
                || !HasEnvironmentValue(environment, "RAV_SERVER_CREDENTIAL")
                || string.IsNullOrWhiteSpace(certificatePath)
                || !File.Exists(certificatePath)
                || string.IsNullOrWhiteSpace(privateKeyPath)
                || !File.Exists(privateKeyPath))
            {
                lastError = "Local Dedicated Server identity or DTLS certificate files are missing. Startup is blocked.";
                yield break;
            }

            RemoveEnvironmentValues(environment,
                "DedicatedServers__Provisioned__0__ServerId",
                "DedicatedServers__Provisioned__0__Credential");
            environment["RAV_API_BASE"] = LocalApiUrl;
            environment["RAV_SERVER_PORT"] = LocalServerPort.ToString(CultureInfo.InvariantCulture);
            environment["RAV_SERVER_ADDRESS"] = "127.0.0.1";
            string log = GetLogPath("local-server");
            string args = JoinWindowsArguments(new[] { "-batchmode", "-nographics", "-serverPort", LocalServerPort.ToString(CultureInfo.InvariantCulture), "-serverAddress", "127.0.0.1", "-apiBase", LocalApiUrl, "-logFile", "-" });
            Task<ManagedProcessInfo> startTask = StartManagedProcessAsync(executable, args, Path.GetDirectoryName(executable), "server", environment, true, log, log + ".stderr.log");
            yield return startTask;
            if (startTask.Result == null)
            {
                lastError = "Local Dedicated Server process did not start.";
                serverStatus = ComponentStatus.Failed;
                yield break;
            }

            SaveManagedProcess(LocalServerRole, startTask.Result, log, log + ".stderr.log");
            serverStatus = ComponentStatus.Starting;
            yield return WaitForServerReadyRoutine(LocalApiUrl, "127.0.0.1", LocalServerPort);
        }

        private static IEnumerator StopServerRoutine()
        {
            if (SelectedEnvironment == Location.Local)
            {
                BeginOperationStage("Stopping the panel-owned Local Dedicated Server");
                yield return StopManagedProcessRoutine(LocalServerRole);
                if (string.IsNullOrEmpty(lastError)) serverStatus = ComponentStatus.Stopped;
                yield break;
            }

            if (!EditorUtility.DisplayDialog("Stop Netcup Dedicated Server", "Stop only rav-staging-dedicated-server.service? The API and PostgreSQL will remain running.", "Stop Server", "Cancel"))
            {
                AddLog("Netcup Dedicated Server stop cancelled; no service changed.", LiveLogLevel.Warning);
                yield break;
            }

            BeginOperationStage("Stopping only the Netcup Dedicated Server systemd service");
            Task<CommandResult> task = RunSshCommandAsync("sudo -n systemctl stop rav-staging-dedicated-server.service");
            SetOperationWaiting("Waiting for the Netcup Dedicated Server to stop...");
            yield return task;
            if (!task.Result.Success) { AddCommandOutput(task.Result); lastError = "Netcup Dedicated Server could not be stopped."; yield break; }
            serverStatus = ComponentStatus.Stopped;
            AddLog("Netcup Dedicated Server stopped; API and PostgreSQL were left running.", LiveLogLevel.Success);
        }

        private static IEnumerator WaitForServerReadyRoutine(string apiBase, string host, int port)
        {
            double deadline = EditorApplication.timeSinceStartup + StartupTimeout;
            double processMetadataGraceDeadline = EditorApplication.timeSinceStartup + 5;
            SetOperationWaiting("Waiting for Dedicated Server registration and heartbeat...");
            while (EditorApplication.timeSinceStartup < deadline)
            {
                // Windows may briefly deny access to MainModule while a newly launched Unity
                // player is still initializing. Give its process identity a short grace period
                // and let the API heartbeat confirm readiness before treating that race as exit.
                if (SelectedEnvironment == Location.Local
                    && !IsManagedProcessRunning(LocalServerRole)
                    && EditorApplication.timeSinceStartup >= processMetadataGraceDeadline)
                {
                    serverStatus = ComponentStatus.Failed;
                    lastError = "Local Dedicated Server exited during startup. Open Logs for details.";
                    yield break;
                }

                Task<string> task = GetServerStatusJsonAsync(apiBase);
                yield return task;
                if (ServerStatusMatches(task.Result, host, port))
                {
                    serverStatus = ComponentStatus.Ready;
                    AddLog("Dedicated Server registration and fresh heartbeat received on UDP " + port + ".", LiveLogLevel.Success);
                    yield break;
                }

                SetOperationWaiting("Waiting for Dedicated Server registration and heartbeat...");
                yield return null;
            }

            serverStatus = ComponentStatus.Failed;
            lastError = "Dedicated Server registration/heartbeat did not become ready before timeout.";
            if (SelectedEnvironment == Location.Local)
            {
                AddLog("Stopping only the Local Dedicated Server process started by this panel.", LiveLogLevel.Warning);
                yield return StopManagedProcessRoutine(LocalServerRole);
            }
        }

        private static IEnumerator StopSelectedRoutine()
        {
            if (SelectedEnvironment == Location.Netcup)
            {
                if (!EditorUtility.DisplayDialog("Stop All Netcup Services", "Stop only the Netcup API and Dedicated Server? PostgreSQL and Caddy remain running.", "Stop Services", "Cancel"))
                {
                    AddLog("Netcup Stop All cancelled; no services changed.", LiveLogLevel.Warning);
                    yield break;
                }

                BeginOperationStage("Stopping the Netcup Dedicated Server");
                Task<CommandResult> serverStop = RunSshCommandAsync("sudo -n systemctl stop rav-staging-dedicated-server.service");
                yield return serverStop;
                if (!serverStop.Result.Success) { AddCommandOutput(serverStop.Result); lastError = "Netcup Dedicated Server stop failed; API stop was skipped."; yield break; }
                serverStatus = ComponentStatus.Stopped;

                BeginOperationStage("Stopping the Netcup API container");
                Task<CommandResult> apiStop = RunSshCommandAsync("sudo -n docker compose --project-name rav-staging --project-directory /opt/rav-staging --env-file /etc/rav-staging/staging.env --file /opt/rav-staging/compose.yaml stop --timeout 20 api");
                yield return apiStop;
                if (!apiStop.Result.Success) { AddCommandOutput(apiStop.Result); lastError = "Netcup API stop failed. PostgreSQL was not targeted."; yield break; }
                apiStatus = ComponentStatus.Stopped;
                AddLog("Netcup API and Dedicated Server stopped; PostgreSQL and Caddy remain running.", LiveLogLevel.Success);
                yield break;
            }

            BeginOperationStage("Stopping the Local Dedicated Server");
            yield return StopManagedProcessRoutine(LocalServerRole);
            if (!string.IsNullOrEmpty(lastError)) yield break;
            yield return StopApiRoutine();
            if (!string.IsNullOrEmpty(lastError)) yield break;

            serverStatus = ComponentStatus.Stopped;
            if (IsManagedProcessRunning(LocalApiRole)) apiStatus = ComponentStatus.Ready;
            else apiStatus = ComponentStatus.Stopped;
            AddLog("Local API and Dedicated Server stop sequence completed; Netcup PostgreSQL was left running.", LiveLogLevel.Success);
            yield return RefreshStatusRoutine();
        }

        private static IEnumerator StopManagedProcessRoutine(string role)
        {
            BeginOperationStage("Stopping " + role + " process");
            if (!TryGetManagedProcess(role, out ManagedProcessInfo record))
            {
                ClearManagedProcess(role);
                AddLog(role + " is not a live process owned by this panel; left unchanged.");
                yield break;
            }

            Task<CommandResult> graceful = RunProcessAsync("taskkill.exe", new[] { "/PID", record.Pid.ToString(CultureInfo.InvariantCulture), "/T" }, ProjectRoot, 10000, null);
            SetOperationWaiting("Waiting for " + role + " to stop gracefully...");
            yield return graceful;
            double deadline = EditorApplication.timeSinceStartup + 8;
            while (EditorApplication.timeSinceStartup < deadline && IsManagedProcessRunning(role))
            {
                yield return null;
            }

            if (IsManagedProcessRunning(role))
            {
                AddLog(role + " did not stop gracefully; force-stopping only its recorded PID tree.");
                Task<CommandResult> force = RunProcessAsync("taskkill.exe", new[] { "/F", "/PID", record.Pid.ToString(CultureInfo.InvariantCulture), "/T" }, ProjectRoot, 10000, null);
                yield return force;
            }

            if (IsManagedProcessRunning(role))
            {
                lastError = "Could not stop the panel-owned " + role + " process tree.";
                yield break;
            }

            ClearManagedProcess(role);
            AddLog(role + " stopped by recorded process ID.");
        }

        private static IEnumerator EnsureLocalSecurityRoutine()
        {
            if (securityPreparedThisOperation)
            {
                AddLog("Local HTTPS/DTLS files were already verified for this operation; repeated ACL work was skipped.");
                yield break;
            }

            BeginOperationStage("Checking local HTTPS and DTLS development credentials");
            string initializer = Path.Combine(ApiProjectRoot, "tools", "Initialize-LocalSecurityEnvironment.ps1");
            string envPath = Path.Combine(LocalSecurityPath, "local-security.env");
            string clientPath = Path.Combine(LocalSecurityPath, "client-transport.env");
            string certificatePath = Path.Combine(LocalSecurityPath, "server-cert.pem");
            string privateKeyPath = Path.Combine(LocalSecurityPath, "server-cert.key");
            if (!File.Exists(envPath) || !File.Exists(clientPath) || !File.Exists(certificatePath) || !File.Exists(privateKeyPath))
            {
                if (!File.Exists(initializer))
                {
                    lastError = "Local security configuration is missing and the existing initializer was not found.";
                    yield break;
                }

                AddLog("Preparing missing local HTTPS/DTLS development credentials with the existing initializer.");
                Task<CommandResult> init = RunProcessAsync("powershell.exe", new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", initializer, "-OutputDirectory", LocalSecurityPath }, ApiProjectRoot, 120000, null);
                yield return init;
                AddCommandOutput(init.Result);
                if (!init.Result.Success)
                {
                    lastError = "Local security initializer failed. No secret values were written to the panel log.";
                    yield break;
                }
            }

            string currentUser = Environment.UserDomainName + "\\" + Environment.UserName;
            string[] directoryArguments = { "/inheritance:r", "/grant:r", currentUser + ":(OI)(CI)F", "*S-1-5-18:(OI)(CI)F", "*S-1-5-32-544:(OI)(CI)F" };
            List<string> directories = new List<string> { LocalSecurityPath };
            directories.AddRange(Directory.GetDirectories(LocalSecurityPath, "*", SearchOption.AllDirectories));
            foreach (string directory in directories)
            {
                string[] arguments = new string[directoryArguments.Length + 1];
                arguments[0] = directory;
                Array.Copy(directoryArguments, 0, arguments, 1, directoryArguments.Length);
                Task<CommandResult> directoryAcl = RunProcessAsync("icacls.exe", arguments, ProjectRoot, 30000, null);
                yield return directoryAcl;
                if (directoryAcl.Result.ExitCode != 0)
                {
                    lastError = "Could not restrict a local security directory. Local server/API startup was blocked.";
                    yield break;
                }
            }

            string[] fileArguments = { "/inheritance:r", "/grant:r", currentUser + ":F", "*S-1-5-18:F", "*S-1-5-32-544:F" };
            foreach (string file in Directory.GetFiles(LocalSecurityPath, "*", SearchOption.AllDirectories))
            {
                string[] arguments = new string[fileArguments.Length + 1];
                arguments[0] = file;
                Array.Copy(fileArguments, 0, arguments, 1, fileArguments.Length);
                Task<CommandResult> fileAcl = RunProcessAsync("icacls.exe", arguments, ProjectRoot, 30000, null);
                yield return fileAcl;
                if (fileAcl.Result.ExitCode != 0)
                {
                    lastError = "Could not restrict a local security file. Local server/API startup was blocked.";
                    yield break;
                }
            }

            AddLog("Local security file permissions are restricted to this user, SYSTEM, and Administrators.");
            securityPreparedThisOperation = true;
        }

        private static void StartOperation(string name, IEnumerator routine)
        {
            if (IsBusy)
            {
                return;
            }

            lastError = string.Empty;
            operationName = name;
            operationPhase = "Preparing " + name + "...";
            operationStatus = OperationStatus.Running;
            currentStage = 1;
            completedStages = 0;
            totalStages = GetOperationStageCount(name);
            stageStarted = false;
            operationStartedAt = EditorApplication.timeSinceStartup;
            lastOperationElapsed = 0d;
            lastWaitingMessage = string.Empty;
            securityPreparedThisOperation = false;
            activeOperation = routine;
            operationStack.Clear();
            operationStack.Push(routine);
            waitingTask = null;
            AddLog(name + " started.", LiveLogLevel.Active);
        }

        private static void PumpOperation()
        {
            DrainPendingLogEntries();
            if (activeOperation == null)
            {
                return;
            }

            if (waitingTask != null)
            {
                if (!waitingTask.IsCompleted)
                {
                    if (operationStatus != OperationStatus.Waiting)
                    {
                        SetOperationWaiting("Waiting for " + operationPhase + "...");
                    }
                    return;
                }

                waitingTask = null;
                if (operationStatus == OperationStatus.Waiting)
                {
                    operationStatus = OperationStatus.Running;
                }
            }

            try
            {
                if (operationStack.Count == 0)
                {
                    activeOperation = null;
                    CompleteOperation();
                    return;
                }

                IEnumerator current = operationStack.Peek();
                if (!current.MoveNext())
                {
                    operationStack.Pop();
                    if (operationStack.Count == 0)
                    {
                        activeOperation = null;
                        CompleteOperation();
                    }

                    return;
                }

                IEnumerator nested = current.Current as IEnumerator;
                if (nested != null)
                {
                    operationStack.Push(nested);
                    return;
                }

                waitingTask = current.Current as Task;
            }
            catch (Exception exception)
            {
                lastError = Sanitize(exception.Message);
                AddLog("Operation failed: " + lastError, LiveLogLevel.Error);
                activeOperation = null;
                operationStack.Clear();
                waitingTask = null;
                CompleteOperation();
            }
        }

        private static int GetOperationStageCount(string name)
        {
            switch (name)
            {
                case "Execute API":
                    return SelectedEnvironment == Location.Local ? (LocalBuildChecked ? 2 : 1) : (NetcupBuildChecked ? 1 : 0) + (NetcupDeployChecked ? 2 : 0) + (NetcupStartChecked ? 2 : 0);
                case "Execute Server":
                    return SelectedEnvironment == Location.Local ? (LocalBuildChecked ? 2 : 1) : (NetcupBuildChecked ? 1 : 0) + (NetcupDeployChecked ? 2 : 0) + (NetcupStartChecked ? 2 : 0);
                case "Execute All":
                    return Mathf.Max(1, GetOperationStageCount("Execute API") + GetOperationStageCount("Execute Server"));
                case "Stop All":
                    return SelectedEnvironment == Location.Local ? 5 : 2;
                case "Build API":
                    return 2;
                case "Deploy API":
                case "Deploy Dedicated Server":
                    return 4;
                case "Start API":
                    return SelectedEnvironment == Location.Local ? 6 : 3;
                case "Start Dedicated Server":
                    return SelectedEnvironment == Location.Local ? 4 : 3;
                case "Stop API":
                case "Stop Dedicated Server":
                    return 2;
                case "Launch Client":
                    return 2;
                case "Refresh Status":
                    return 1;
                default:
                    return 1;
            }
        }

        private static void BeginOperationStage(string phase)
        {
            if (activeOperation == null)
            {
                return;
            }

            if (stageStarted && currentStage > completedStages)
            {
                completedStages = currentStage;
            }

            if (!stageStarted)
            {
                stageStarted = true;
                currentStage = 1;
            }
            else
            {
                currentStage = completedStages + 1;
            }

            if (currentStage > totalStages)
            {
                totalStages = currentStage;
            }

            operationPhase = phase;
            operationStatus = OperationStatus.Running;
            lastWaitingMessage = string.Empty;
            AddLog("Starting " + phase + "...", LiveLogLevel.Active);
        }

        private static void SetOperationWaiting(string message)
        {
            if (activeOperation == null)
            {
                return;
            }

            operationStatus = OperationStatus.Waiting;
            if (!string.Equals(lastWaitingMessage, message, StringComparison.Ordinal))
            {
                lastWaitingMessage = message;
                AddLog(message, LiveLogLevel.Waiting);
            }
        }

        private static void CompleteOperation()
        {
            lastOperationElapsed = Math.Max(0d, EditorApplication.timeSinceStartup - operationStartedAt);
            bool succeeded = string.IsNullOrEmpty(lastError);
            if (succeeded)
            {
                completedStages = totalStages;
                currentStage = totalStages;
                operationStatus = OperationStatus.Completed;
                operationPhase = "Operation completed.";
                AddLog(operationName + " completed.", LiveLogLevel.Success);
            }
            else
            {
                operationStatus = OperationStatus.Failed;
                operationPhase = lastError;
                AddLog(operationName + " failed: " + lastError, LiveLogLevel.Error);
            }
        }

        private static void BeginInstantOperation(string name, string phase)
        {
            operationName = name;
            operationPhase = phase;
            operationStatus = OperationStatus.Running;
            currentStage = 1;
            completedStages = 0;
            totalStages = 1;
            operationStartedAt = EditorApplication.timeSinceStartup;
            lastOperationElapsed = 0d;
            lastError = string.Empty;
            AddLog("Starting " + name + "...", LiveLogLevel.Active);
            AddLog("Starting " + phase + "...", LiveLogLevel.Active);
        }

        private static void CompleteInstantOperation(bool succeeded, string error = null)
        {
            lastOperationElapsed = Math.Max(0d, EditorApplication.timeSinceStartup - operationStartedAt);
            if (succeeded)
            {
                completedStages = totalStages;
                currentStage = totalStages;
                operationStatus = OperationStatus.Completed;
                operationPhase = "Operation completed.";
            }
            else
            {
                lastError = error ?? "The operation could not be completed.";
                operationStatus = OperationStatus.Failed;
                operationPhase = lastError;
            }
        }

        private static void SchedulePumpAfterGui()
        {
            if (pumpScheduled)
            {
                return;
            }

            pumpScheduled = true;
            EditorApplication.delayCall += PumpOperationAfterGui;
        }

        private static void PumpOperationAfterGui()
        {
            pumpScheduled = false;
            PumpOperation();
        }

        private static async Task<bool> CheckNetcupDatabaseAsync()
        {
            CommandResult result = await RunSshCommandAsync(
                "sudo -n docker exec rav-staging-postgres-1 pg_isready -U rav_staging_runtime -d rav_staging").ConfigureAwait(false);
            return result.Success;
        }

        private static Task<bool> CheckTcpPortListeningAsync(int port)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork))
                    {
                        Task connect = client.ConnectAsync(System.Net.IPAddress.Loopback, port);
                        if (Task.WhenAny(connect, Task.Delay(750)).GetAwaiter().GetResult() != connect) return false;
                        connect.GetAwaiter().GetResult();
                        return client.Connected;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private static Task<CommandResult> ReadVaultDatabaseUrlAsync()
        {
            string helper = Path.Combine(HelperScriptsRoot, "Get-DeploymentSecretBase64.ps1");
            if (!File.Exists(helper))
            {
                return Task.FromResult(new CommandResult { ExitCode = 1, Error = "Secure-vault helper is missing." });
            }

            // The deployment vault is DPAPI protected by PowerShell 7. Windows PowerShell 5.1
            // cannot decrypt this vault on this workstation, so use the installed PowerShell Core.
            return RunProcessAsync("pwsh.exe", new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", helper, "-Name", "DatabaseUrl" }, HelperScriptsRoot, 15000, null);
        }

        private static bool TryDecodeVaultDatabaseUrl(string base64, out string value)
        {
            value = string.Empty;
            try
            {
                string encoded = (base64 ?? string.Empty).Trim();
                if (encoded.Length == 0) return false;
                value = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }

        private static bool TryValidateStagingDatabaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string normalized = value.Replace(" ", string.Empty).ToLowerInvariant();
            return normalized.Contains("host=postgres;")
                && normalized.Contains("database=rav_staging;")
                && normalized.Contains("sslmode=verifyfull;");
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static Task<bool> CheckApiHealthAsync(string apiBase)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (HttpClient client = CreateApiHttpClient(apiBase))
                    {
                        Task<HttpResponseMessage> liveTask = client.GetAsync(apiBase.TrimEnd('/') + "/health/live");
                        Task<HttpResponseMessage> readyTask = client.GetAsync(apiBase.TrimEnd('/') + "/health/ready");
                        Task.WhenAll(liveTask, readyTask).GetAwaiter().GetResult();
                        HttpResponseMessage live = liveTask.Result;
                        HttpResponseMessage ready = readyTask.Result;
                        if (!live.IsSuccessStatusCode) return false;
                        return ready.IsSuccessStatusCode;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private static Task<bool> CheckApiEndpointListeningAsync(string apiBase)
        {
            return Task.Run(() =>
            {
                if (!Uri.TryCreate(apiBase, UriKind.Absolute, out Uri apiUri) || !apiUri.IsLoopback || apiUri.Port <= 0)
                {
                    return false;
                }

                try
                {
                    using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
                    {
                        Task connectTask = client.ConnectAsync(apiUri.Host, apiUri.Port);
                        Task completed = Task.WhenAny(connectTask, Task.Delay(1500)).GetAwaiter().GetResult();
                        if (completed != connectTask)
                        {
                            return false;
                        }

                        connectTask.GetAwaiter().GetResult();
                        return client.Connected;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private static Task<string> GetServerStatusJsonAsync(string apiBase)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (HttpClient client = CreateApiHttpClient(apiBase))
                    {
                        HttpResponseMessage response = client.GetAsync(apiBase.TrimEnd('/') + "/unity-server/status").GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode) return string.Empty;
                        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
                catch
                {
                    return string.Empty;
                }
            });
        }

        private static HttpClient CreateApiHttpClient(string apiBase)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (Uri.TryCreate(apiBase, UriKind.Absolute, out Uri apiUri) && apiUri.IsLoopback)
            {
                // Local health/status probes must connect directly to loopback. Some Windows
                // proxy auto-configurations incorrectly route localhost through the proxy.
                handler.UseProxy = false;
            }

            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) };
        }

        private static bool ServerStatusMatches(string json, string host, int port)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                ApiServerStatus response = JsonUtility.FromJson<ApiServerStatus>(json);
                if (response?.servers == null) return false;
                for (int i = 0; i < response.servers.Length; i++)
                {
                    ApiServerItem server = response.servers[i];
                    if (server == null || !server.isOnline || server.port != port || !AddressMatches(server.address, host)) continue;
                    if (!DateTimeOffset.TryParse(server.lastHeartbeatUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset heartbeat))
                    {
                        continue;
                    }

                    TimeSpan heartbeatAge = DateTimeOffset.UtcNow - heartbeat;
                    if (heartbeatAge > TimeSpan.FromSeconds(18) || heartbeatAge < TimeSpan.FromMinutes(-2))
                    {
                        continue;
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool AddressMatches(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected)) return false;
            return string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase)
                   || (string.Equals(expected, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                       && string.Equals(actual, "localhost", StringComparison.OrdinalIgnoreCase));
        }

        private static Task<CommandResult> RunSshCommandAsync(string remoteCommand)
        {
            List<string> args = new List<string> { "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", "-o", "ConnectTimeout=5", "-p", SshPort.ToString(CultureInfo.InvariantCulture) };
            if (!string.IsNullOrWhiteSpace(SshKeyReference))
            {
                if (!File.Exists(SshKeyReference))
                {
                    return Task.FromResult(new CommandResult { ExitCode = 1, Error = "Configured SSH key reference does not exist." });
                }

                args.Add("-i");
                args.Add(SshKeyReference);
            }

            args.Add(SshTarget);
            args.Add(remoteCommand);
            return RunProcessAsync("ssh.exe", args.ToArray(), DeploymentRoot, 30000, null);
        }

        private static Task<CommandResult> RunScpAsync(string localFile, string remotePath)
        {
            if (string.IsNullOrWhiteSpace(SshKeyReference) || !File.Exists(SshKeyReference))
            {
                return Task.FromResult(new CommandResult { ExitCode = 1, Error = "Configured SSH key reference does not exist." });
            }

            string destination = SshTarget + ":" + remotePath;
            string[] args = { "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes", "-o", "ConnectTimeout=5", "-P", SshPort.ToString(CultureInfo.InvariantCulture), "-i", SshKeyReference, localFile, destination };
            return RunProcessAsync("scp.exe", args, DeploymentRoot, 600000, null);
        }

        private static Task<CommandResult> RunProcessAsync(string executable, string[] arguments, string workingDirectory, int timeoutMilliseconds, IDictionary<string, string> environment)
        {
            return Task.Run(() => RunProcess(executable, arguments, workingDirectory, timeoutMilliseconds, environment));
        }

        private static CommandResult RunProcess(string executable, string[] arguments, string workingDirectory, int timeoutMilliseconds, IDictionary<string, string> environment)
        {
            CommandResult result = new CommandResult();
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = JoinWindowsArguments(arguments),
                    WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (environment != null)
                {
                    RemoveInheritedEnvironmentOverrides(info.EnvironmentVariables);

                    foreach (KeyValuePair<string, string> pair in environment)
                    {
                        info.EnvironmentVariables[pair.Key] = pair.Value;
                    }
                }

                using (Process process = new Process { StartInfo = info })
                {
                    StringBuilder outputBuilder = new StringBuilder();
                    StringBuilder errorBuilder = new StringBuilder();
                    object outputLock = new object();
                    bool outputClosed = false;
                    bool errorClosed = false;
                    process.OutputDataReceived += (_, eventArgs) =>
                    {
                        lock (outputLock)
                        {
                            if (eventArgs.Data == null) outputClosed = true;
                            else outputBuilder.AppendLine(eventArgs.Data);
                            Monitor.PulseAll(outputLock);
                        }
                    };
                    process.ErrorDataReceived += (_, eventArgs) =>
                    {
                        lock (outputLock)
                        {
                            if (eventArgs.Data == null) errorClosed = true;
                            else errorBuilder.AppendLine(eventArgs.Data);
                            Monitor.PulseAll(outputLock);
                        }
                    };

                    if (!process.Start())
                    {
                        result.ExitCode = 1;
                        result.Error = "Process did not start.";
                        return result;
                    }

                    info.EnvironmentVariables.Clear();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        result.TimedOut = true;
                        try { process.Kill(); } catch { }
                        process.WaitForExit(5000);
                    }

                    DateTime outputDeadline = DateTime.UtcNow.AddSeconds(2);
                    bool streamsClosed;
                    lock (outputLock)
                    {
                        while ((!outputClosed || !errorClosed) && DateTime.UtcNow < outputDeadline)
                        {
                            int remaining = (int)Math.Max(1, (outputDeadline - DateTime.UtcNow).TotalMilliseconds);
                            Monitor.Wait(outputLock, remaining);
                        }

                        streamsClosed = outputClosed && errorClosed;
                        result.Output = Sanitize(outputBuilder.ToString());
                        result.Error = Sanitize(errorBuilder.ToString());
                    }

                    if (!streamsClosed)
                    {
                        // Some command-line tools leave redirected handles open in helper processes.
                        // Never let inherited stdout/stderr handles keep a panel operation busy.
                        try { process.CancelOutputRead(); } catch { }
                        try { process.CancelErrorRead(); } catch { }
                        result.Error = string.IsNullOrEmpty(result.Error)
                            ? "Process output handles remained open after exit; captured output returned without waiting for child helpers."
                            : result.Error + Environment.NewLine + "Process output handles remained open after exit; captured output may be partial.";
                    }

                    try { result.ExitCode = process.ExitCode; }
                    catch
                    {
                        result.ExitCode = 1;
                        if (string.IsNullOrEmpty(result.Error)) result.Error = "Process did not exit within the bounded wait.";
                    }
                }
            }
            catch (Exception exception)
            {
                result.ExitCode = 1;
                result.Error = Sanitize(exception.Message);
            }

            return result;
        }

        private static Task<ManagedProcessInfo> StartManagedProcessAsync(string executable, string arguments, string workingDirectory, string logName, IDictionary<string, string> environment, bool hidden, string stdout = null, string stderr = null)
        {
            return Task.Run(() =>
            {
                Process process = null;
                try
                {
                    Directory.CreateDirectory(LogsRoot);
                    string stdoutPath = stdout ?? GetLogPath(logName + ".stdout");
                    string stderrPath = stderr ?? GetLogPath(logName + ".stderr");
                    Directory.CreateDirectory(Path.GetDirectoryName(stdoutPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(stderrPath));
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = hidden,
                        WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
                        RedirectStandardOutput = !string.IsNullOrWhiteSpace(stdoutPath),
                        RedirectStandardError = !string.IsNullOrWhiteSpace(stderrPath)
                    };

                    if (environment != null)
                    {
                        RemoveInheritedEnvironmentOverrides(startInfo.EnvironmentVariables);

                        foreach (KeyValuePair<string, string> pair in environment)
                        {
                            startInfo.EnvironmentVariables[pair.Key] = pair.Value;
                        }
                    }

                    process = new Process { StartInfo = startInfo };
                    if (!process.Start()) return null;
                    startInfo.EnvironmentVariables.Clear();

                    ManagedProcessInfo info = new ManagedProcessInfo
                    {
                        Pid = process.Id,
                        StartTicks = process.StartTime.ToUniversalTime().Ticks,
                        ExecutablePath = GetStartedProcessPath(process, executable)
                    };

                    List<Task> outputTasks = new List<Task>();
                    if (startInfo.RedirectStandardOutput)
                    {
                        outputTasks.Add(CopyProcessStreamToFileAsync(process.StandardOutput.BaseStream, stdoutPath));
                    }

                    if (startInfo.RedirectStandardError)
                    {
                        outputTasks.Add(CopyProcessStreamToFileAsync(process.StandardError.BaseStream, stderrPath));
                    }

                    if (outputTasks.Count > 0)
                    {
                        Task.WhenAll(outputTasks.ToArray()).ContinueWith(_ => process.Dispose(), TaskScheduler.Default);
                    }
                    else
                    {
                        process.Dispose();
                    }

                    return info;
                }
                catch
                {
                    if (process != null)
                    {
                        try { process.Dispose(); } catch { }
                    }

                    return null;
                }
            });
        }

        private static string GetStartedProcessPath(Process process, string fallbackExecutable)
        {
            try
            {
                string path = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(path)) return Path.GetFullPath(path);
            }
            catch
            {
                // Fall through to a local PATH lookup for processes whose module path is not exposed.
            }

            if (Path.IsPathRooted(fallbackExecutable)) return Path.GetFullPath(fallbackExecutable);
            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] directories = pathValue.Split(Path.PathSeparator);
            for (int i = 0; i < directories.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(directories[i])) continue;
                string candidate = Path.Combine(directories[i].Trim(), fallbackExecutable);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }

            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, fallbackExecutable));
        }

        private static Task CopyProcessStreamToFileAsync(Stream source, string outputPath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (StreamReader reader = new StreamReader(source, Encoding.UTF8, true, 4096, false))
                    using (StreamWriter writer = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                        {
                            await writer.WriteLineAsync(Sanitize(line, 0)).ConfigureAwait(false);
                        }
                    }
                }
                catch
                {
                    // Log capture must never hold up service startup or shutdown.
                }
            });
        }

        private static string FindLatestBuild(string root, string executableName)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return string.Empty;
            string latest = string.Empty;
            DateTime latestWrite = DateTime.MinValue;
            string[] directories = Directory.GetDirectories(root);
            for (int i = 0; i < directories.Length; i++)
            {
                string candidate = Path.Combine(directories[i], executableName);
                if (!File.Exists(candidate)) continue;
                DateTime write = File.GetLastWriteTimeUtc(candidate);
                if (write > latestWrite)
                {
                    latestWrite = write;
                    latest = candidate;
                }
            }

            return latest;
        }

        private static bool IsUdpPortAvailable(int port)
        {
            try
            {
                using (System.Net.Sockets.UdpClient client = new System.Net.Sockets.UdpClient(System.Net.Sockets.AddressFamily.InterNetwork))
                {
                    client.Client.ExclusiveAddressUse = true;
                    client.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadEnvironmentFile(string path, out Dictionary<string, string> values, out string error)
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;
            if (!File.Exists(path))
            {
                error = "Required local security configuration is missing: " + Path.GetFileName(path) + ".";
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    string name = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim().Trim('"');
                    values[name] = value;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = "Could not read local security configuration: " + Sanitize(exception.Message);
                return false;
            }
        }

        private static bool TryApplyClientEnvironment(string reason, out Dictionary<string, string> environment, out string error)
        {
            environment = null;
            if (!TryApplyClientEnvironment(out error)) return false;
            string apiBase = SelectedEnvironment == Location.Local ? LocalApiUrl : NetcupApiUrl;
            string commonName = SelectedEnvironment == Location.Local ? string.Empty : NetcupCommonName;
            string caPath = SelectedEnvironment == Location.Local ? string.Empty : NetcupCaPath;
            if (SelectedEnvironment == Location.Local)
            {
                if (!TryReadEnvironmentFile(Path.Combine(LocalSecurityPath, "client-transport.env"), out Dictionary<string, string> values, out error))
                {
                    return false;
                }

                commonName = GetEnvironmentValue(values, "RAV_UTP_SERVER_COMMON_NAME", "WOM_UTP_SERVER_COMMON_NAME");
                caPath = GetEnvironmentValue(values, "RAV_UTP_CA_CERTIFICATE", "WOM_UTP_CA_CERTIFICATE");
            }

            environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["RAV_API_BASE"] = apiBase,
                ["RAV_UTP_SERVER_COMMON_NAME"] = commonName,
                ["RAV_UTP_CA_CERTIFICATE"] = caPath
            };
            AddLog(reason + ": explicit client environment prepared; cached authentication and tickets cleared.");
            return true;
        }

        private static string GetEnvironmentValue(IDictionary<string, string> values, string name, string legacyName)
        {
            if (values == null) return string.Empty;
            if (values.TryGetValue(name, out string value) && !string.IsNullOrWhiteSpace(value)) return value;
            if (values.TryGetValue(legacyName, out value)) return value;
            return string.Empty;
        }

        private static bool HasEnvironmentValue(IDictionary<string, string> values, string name)
        {
            return values != null
                && values.TryGetValue(name, out string value)
                && !string.IsNullOrWhiteSpace(value);
        }

        private static void RemoveEnvironmentValues(IDictionary<string, string> values, params string[] names)
        {
            if (values == null || names == null)
            {
                return;
            }

            for (int i = 0; i < names.Length; i++)
            {
                values.Remove(names[i]);
            }
        }

        private static void RemoveInheritedEnvironmentOverrides(System.Collections.Specialized.StringDictionary values)
        {
            for (int i = 0; i < IsolatedEnvironmentNames.Length; i++)
            {
                values.Remove(IsolatedEnvironmentNames[i]);
            }
        }

        private static void SaveManagedProcess(string role, ManagedProcessInfo process, string stdout, string stderr)
        {
            EditorPrefs.SetInt(Pref + role + ".Pid", process.Pid);
            EditorPrefs.SetString(Pref + role + ".Ticks", process.StartTicks.ToString(CultureInfo.InvariantCulture));
            EditorPrefs.SetString(Pref + role + ".Path", process.ExecutablePath ?? string.Empty);
            EditorPrefs.SetString(Pref + role + ".Stdout", stdout ?? string.Empty);
            EditorPrefs.SetString(Pref + role + ".Stderr", stderr ?? string.Empty);
        }

        private static bool TryGetManagedProcess(string role, out ManagedProcessInfo info)
        {
            info = null;
            int pid = EditorPrefs.GetInt(Pref + role + ".Pid", 0);
            if (pid <= 0 || !long.TryParse(EditorPrefs.GetString(Pref + role + ".Ticks", string.Empty), out long ticks)) return false;
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != ticks) return false;
                    string executable = process.MainModule.FileName;
                    string expected = EditorPrefs.GetString(Pref + role + ".Path", string.Empty);
                    if (!string.Equals(Path.GetFullPath(executable), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase)) return false;
                    info = new ManagedProcessInfo { Pid = pid, StartTicks = ticks, ExecutablePath = expected };
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool IsManagedProcessRunning(string role)
        {
            return TryGetManagedProcess(role, out _);
        }

        private static void ClearManagedProcess(string role)
        {
            EditorPrefs.DeleteKey(Pref + role + ".Pid");
            EditorPrefs.DeleteKey(Pref + role + ".Ticks");
            EditorPrefs.DeleteKey(Pref + role + ".Path");
            EditorPrefs.DeleteKey(Pref + role + ".Stdout");
            EditorPrefs.DeleteKey(Pref + role + ".Stderr");
            if (role == LocalApiRole) LocalApiUsesNetcupTunnel = false;
        }

        private static void AddCommandOutput(CommandResult result)
        {
            if (result == null)
            {
                return;
            }

            AppendCommandDetails("STDOUT", result.Output);
            AppendCommandDetails("STDERR", result.Error);
            if (result.TimedOut)
            {
                AddLog("External command timed out. See Open Full Log for details.", LiveLogLevel.Warning);
            }
            else if (!result.Success)
            {
                AddLog("External command failed with exit code " + result.ExitCode + ". See Open Full Log for details.", LiveLogLevel.Error);
            }
        }

        private static void AddLog(string message)
        {
            AddLog(message, ClassifyLogLevel(message));
        }

        private static void AddLog(string message, LiveLogLevel level)
        {
            string safe = Sanitize(message);
            if (string.IsNullOrWhiteSpace(safe))
            {
                return;
            }

            string[] lines = safe.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                PendingLogEntries.Enqueue(new LiveLogEntry
                {
                    timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    message = lines[i],
                    level = level
                });
            }
        }

        private static LiveLogLevel ClassifyLogLevel(string message)
        {
            string lower = (message ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("failed") || lower.Contains("error") || lower.Contains("exception")
                || lower.Contains("blocked") || lower.Contains("could not") || lower.Contains("timed out")
                || lower.Contains("did not become") || lower.Contains("missing"))
            {
                return LiveLogLevel.Error;
            }

            if (lower.Contains("waiting") || lower.Contains("wait for") || lower.Contains("retrying"))
            {
                return LiveLogLevel.Waiting;
            }

            if (lower.Contains("skipped") || lower.Contains("cancelled") || lower.Contains("left unchanged")
                || lower.Contains("unavailable") || lower.Contains("not started by this panel"))
            {
                return LiveLogLevel.Warning;
            }

            if (lower.Contains("started") || lower.Contains("starting") || lower.Contains("building")
                || lower.Contains("launching") || lower.Contains("checking") || lower.Contains("restarting"))
            {
                return LiveLogLevel.Active;
            }

            if (lower.Contains("completed") || lower.Contains("passed") || lower.Contains("healthy")
                || lower.Contains("ready") || lower.Contains("returned success") || lower.Contains("heartbeat"))
            {
                return LiveLogLevel.Success;
            }

            return LiveLogLevel.Info;
        }

        private static void EnsureRecentLogLoaded()
        {
            if (logLoaded)
            {
                return;
            }

            logLoaded = true;
            try
            {
                if (!File.Exists(LiveLogPath))
                {
                    return;
                }

                string[] lines = File.ReadAllLines(LiveLogPath);
                int firstLine = Mathf.Max(0, lines.Length - MaxRecentLogEntries);
                for (int i = firstLine; i < lines.Length; i++)
                {
                    LiveLogEntry entry = ParsePersistedLogEntry(lines[i]);
                    if (entry != null)
                    {
                        RecentLog.Add(entry);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not load recent RAV panel logs: " + Sanitize(exception.Message));
            }
        }

        private static LiveLogEntry ParsePersistedLogEntry(string line)
        {
            Match match = Regex.Match(line ?? string.Empty, @"^\[(?<time>[^\]]+)\] \[(?<level>[^\]]+)\] (?<message>.*)$");
            if (!match.Success)
            {
                return null;
            }

            LiveLogLevel level;
            if (!Enum.TryParse(match.Groups["level"].Value, true, out level))
            {
                level = LiveLogLevel.Info;
            }

            return new LiveLogEntry
            {
                timestamp = match.Groups["time"].Value,
                message = match.Groups["message"].Value,
                level = level
            };
        }

        private static void DrainPendingLogEntries()
        {
            bool added = false;
            LiveLogEntry entry;
            while (PendingLogEntries.TryDequeue(out entry))
            {
                RecentLog.Add(entry);
                while (RecentLog.Count > MaxRecentLogEntries)
                {
                    RecentLog.RemoveAt(0);
                }

                string persisted = FormatPersistedLogEntry(entry) + Environment.NewLine;
                AppendTextSafely(LiveLogPath, persisted);
                AppendTextSafely(FullLogPath, persisted);
                added = true;
            }

            if (added && openWindow != null)
            {
                openWindow.Repaint();
            }
        }

        private static string FormatPersistedLogEntry(LiveLogEntry entry)
        {
            return "[" + entry.timestamp + "] [" + entry.level.ToString().ToUpperInvariant() + "] " + entry.message;
        }

        private static string FormatVisibleLogEntry(LiveLogEntry entry)
        {
            return "[" + entry.timestamp + "] " + entry.message;
        }

        private static List<LiveLogEntry> GetRecentLogSnapshot()
        {
            return new List<LiveLogEntry>(RecentLog);
        }

        private static Color GetLogColor(LiveLogLevel level)
        {
            switch (level)
            {
                case LiveLogLevel.Active: return new Color(0.38f, 0.78f, 1f);
                case LiveLogLevel.Waiting:
                case LiveLogLevel.Warning: return new Color(1f, 0.82f, 0.33f);
                case LiveLogLevel.Success: return new Color(0.47f, 0.86f, 0.56f);
                case LiveLogLevel.Error: return new Color(1f, 0.4f, 0.38f);
                default: return new Color(0.82f, 0.84f, 0.87f);
            }
        }

        private static Color GetOperationStatusColor(OperationStatus status)
        {
            switch (status)
            {
                case OperationStatus.Running: return GetLogColor(LiveLogLevel.Active);
                case OperationStatus.Waiting: return GetLogColor(LiveLogLevel.Waiting);
                case OperationStatus.Failed: return GetLogColor(LiveLogLevel.Error);
                default: return GetLogColor(LiveLogLevel.Success);
            }
        }

        private static void ClearLiveLog()
        {
            while (PendingLogEntries.TryDequeue(out _)) { }
            RecentLog.Clear();
            logScroll = Vector2.zero;
            try
            {
                Directory.CreateDirectory(LogsRoot);
                File.WriteAllText(LiveLogPath, string.Empty);
                File.WriteAllText(FullLogPath, string.Empty);
            }
            catch (Exception exception)
            {
                AddLog("Could not clear the panel log files: " + Sanitize(exception.Message), LiveLogLevel.Error);
            }

            AddLog("Live log cleared.", LiveLogLevel.Info);
        }

        private static void CopyLiveLog()
        {
            GUIUtility.systemCopyBuffer = string.Join(Environment.NewLine, GetRecentLogSnapshot().ConvertAll(FormatVisibleLogEntry));
            AddLog("Recent live log copied to the clipboard.", LiveLogLevel.Info);
        }

        private static void OpenFullLog()
        {
            try
            {
                Directory.CreateDirectory(LogsRoot);
                if (!File.Exists(FullLogPath))
                {
                    File.WriteAllText(FullLogPath, string.Empty);
                }

                Process.Start(new ProcessStartInfo { FileName = FullLogPath, UseShellExecute = true });
                AddLog("Opened the full operation log.", LiveLogLevel.Info);
            }
            catch (Exception exception)
            {
                AddLog("Could not open the full log: " + Sanitize(exception.Message), LiveLogLevel.Error);
            }
        }

        private static void AppendCommandDetails(string label, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            string sanitizedContent = Sanitize(content, 0);
            string[] lines = sanitizedContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                AppendTextSafely(FullLogPath, "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                    + "] [" + label + "] " + lines[i] + Environment.NewLine);
            }
        }

        private static void AppendTextSafely(string path, string content)
        {
            try
            {
                lock (LogFileLock)
                {
                    Directory.CreateDirectory(LogsRoot);
                    File.AppendAllText(path, content, new UTF8Encoding(false));
                    FileInfo info = new FileInfo(path);
                    if (info.Length > MaxFullLogBytes)
                    {
                        string[] lines = File.ReadAllLines(path);
                        int keepFrom = Mathf.Max(0, lines.Length - MaxRecentLogEntries * 4);
                    File.WriteAllLines(path, keepFrom > 0 ? lines.Skip(keepFrom).ToArray() : lines, new UTF8Encoding(false));
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not persist RAV panel log: " + Sanitize(exception.Message));
            }
        }

        private static string GetActivityGlyph()
        {
            string[] glyphs = { "|", "/", "—", "\\" };
            int index = (int)(EditorApplication.timeSinceStartup * 8d) % glyphs.Length;
            return glyphs[index];
        }

        private static string FormatElapsed(double seconds)
        {
            TimeSpan elapsed = TimeSpan.FromSeconds(seconds);
            return elapsed.TotalHours >= 1d
                ? elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                : elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }

        private static string Sanitize(string value)
        {
            return Sanitize(value, 4000);
        }

        private static string Sanitize(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string safe = Regex.Replace(value,
                @"(?i)(password|credential|token|secret|jwt(?:__key)?|database_url|authorization|api[_-]?key|client[_-]?secret|signing[_-]?key|private[_-]?key)\s*[:=]\s*(""[^""]*""|'[^']*'|[^\s,;]+)",
                "$1=[REDACTED]");
            safe = Regex.Replace(safe, @"(?i)\b(postgres(?:ql)?|mysql|mongodb(?:\+srv)?|redis)://[^/\s:@]+:[^@\s/]+@", "$1://[REDACTED]@");
            safe = Regex.Replace(safe, @"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*", "Bearer [REDACTED]");
            safe = Regex.Replace(safe, @"(?is)-----BEGIN [^-]*PRIVATE KEY-----.*?-----END [^-]*PRIVATE KEY-----", "[PRIVATE KEY REDACTED]");
            return maximumLength > 0 && safe.Length > maximumLength ? safe.Substring(0, maximumLength) + "…" : safe;
        }

        private static string GetLogPath(string name)
        {
            Directory.CreateDirectory(LogsRoot);
            return Path.Combine(LogsRoot, name + ".log");
        }

        private static string JoinWindowsArguments(IList<string> arguments)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(QuoteWindowsArgument(arguments[i]));
            }

            return builder.ToString();
        }

        private static string QuoteWindowsArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument)) return "\"\"";
            if (argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0) return argument;
            StringBuilder builder = new StringBuilder("\"");
            int slashes = 0;
            for (int i = 0; i < argument.Length; i++)
            {
                char character = argument[i];
                if (character == '\\')
                {
                    slashes++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', slashes * 2 + 1);
                    builder.Append('"');
                    slashes = 0;
                    continue;
                }

                builder.Append('\\', slashes);
                slashes = 0;
                builder.Append(character);
            }

            builder.Append('\\', slashes * 2);
            builder.Append('"');
            return builder.ToString();
        }

        private static string GetString(string key, string defaultValue)
        {
            return EditorPrefs.GetString(Pref + key, defaultValue);
        }

        private static void SetString(string key, string value)
        {
            EditorPrefs.SetString(Pref + key, value ?? string.Empty);
        }

        private static bool IsBusy => activeOperation != null;
    }
}
#endif
