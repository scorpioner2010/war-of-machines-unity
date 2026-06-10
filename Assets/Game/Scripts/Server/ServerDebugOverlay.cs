using System.Text;
using System.Collections.Generic;
using FishNet.Managing;
using Game.Scripts.Diagnostics;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Server
{
    public class ServerDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR
        private const int MaxRoomsShown = 8;
        private const int MaxStatusLines = 24;
        private const float StatusRefreshIntervalSeconds = 1f;
        private const float OverlayX = 10f;
        private const float OverlayY = 10f;
        private const float OverlayWidth = 430f;
        private const float TitleHeight = 24f;
        private const float LineHeight = 18f;
        private const float Padding = 8f;
        private const KeyCode ToggleKey = KeyCode.BackQuote;

        private static ServerDebugOverlay _instance;
        private static readonly GUIContent TitleContent = new GUIContent("Server Debug [`]");

        private readonly GUIContent[] _statusContents = new GUIContent[MaxStatusLines];
        private readonly OverlayLineState[] _statusStates = new OverlayLineState[MaxStatusLines];
        private readonly ServerRoom[] _roomsShown = new ServerRoom[MaxRoomsShown];
        private readonly StringBuilder _lineBuilder = new StringBuilder(192);

        private bool _visible = true;
        private bool _lastServerEditorContext;
        private int _statusLineCount;
        private float _nextStatusRefreshTime;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _workingStyle;
        private GUIStyle _notWorkingStyle;
        private GUIStyle _notInitializedStyle;

        private enum OverlayLineState
        {
            Info,
            Working,
            NotWorking,
            NotInitialized
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < _statusContents.Length; i++)
            {
                _statusContents[i] = new GUIContent();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                bool serverEditorContext = IsServerEditorContext();
                if (_visible || serverEditorContext)
                {
                    _visible = !_visible && serverEditorContext;
                    _lastServerEditorContext = serverEditorContext;
                    _nextStatusRefreshTime = 0f;
                }
            }

            if (!_visible)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextStatusRefreshTime)
            {
                return;
            }

            _nextStatusRefreshTime = now + StatusRefreshIntervalSeconds;
            _lastServerEditorContext = IsServerEditorContext();
            if (!_lastServerEditorContext)
            {
                _visible = false;
                ClearStatusLines();
                return;
            }

            RefreshStatusLines();
        }

        private void OnGUI()
        {
            if (!_visible || !_lastServerEditorContext)
            {
                return;
            }

            Event current = Event.current;
            if (current != null && current.type != EventType.Repaint)
            {
                return;
            }

            using (ProfileScope.Measure("OnGUI.ServerDebugOverlay", DiagnosticsCategories.Editor))
            {
                EnsureStyles();

                float height = Padding + TitleHeight + (_statusLineCount * LineHeight) + Padding;
                GUI.Box(new Rect(OverlayX, OverlayY, OverlayWidth, height), GUIContent.none);
                GUI.Label(new Rect(OverlayX + Padding, OverlayY + Padding, OverlayWidth - Padding * 2f, TitleHeight), TitleContent, _titleStyle);

                float y = OverlayY + Padding + TitleHeight;
                for (int i = 0; i < _statusLineCount; i++)
                {
                    GUI.Label(
                        new Rect(OverlayX + Padding, y, OverlayWidth - Padding * 2f, LineHeight),
                        _statusContents[i],
                        GetStyle(_statusStates[i]));
                    y += LineHeight;
                }
            }
        }

        private void RefreshStatusLines()
        {
            _statusLineCount = 0;

            NetworkManager networkManager = GetNetworkManager();
            bool serverStarted = networkManager != null && networkManager.IsServerStarted;
            bool clientStarted = networkManager != null && networkManager.IsClientStarted;

            AddValueLine("Scene", SceneManager.GetActiveScene().name, OverlayLineState.Info);
            AddValueLine("Role", GetRoleText(serverStarted, clientStarted), OverlayLineState.Info);

            if (networkManager == null)
            {
                AddStatusLine("NetworkManager: Not initialized", OverlayLineState.NotInitialized);
                AddStatusLine("Server: Not initialized", OverlayLineState.NotInitialized);
                AddStatusLine("Client: Not initialized", OverlayLineState.NotInitialized);
                AddStatusLine("Transport: Not initialized", OverlayLineState.NotInitialized);
                AddToggleHint();
                return;
            }

            AddStatusLine("NetworkManager: Initialized", OverlayLineState.Working);
            AddValueLine("Server", GetStateText(serverStarted), GetRunningState(serverStarted));
            OverlayLineState localClientState = clientStarted
                ? OverlayLineState.Working
                : serverStarted ? OverlayLineState.Info : OverlayLineState.NotWorking;
            AddValueLine("Local client (host only)", GetStateText(clientStarted), localClientState);
            AddValueLine("Start status", StartServerButtons.LastServerStatus, GetRunningState(serverStarted));

            bool transportInitialized = networkManager.TransportManager != null
                                        && networkManager.TransportManager.Transport != null;
            if (transportInitialized)
            {
                AddStatusLine("Transport: Initialized", OverlayLineState.Working);
                AddValueLine("Port", networkManager.TransportManager.Transport.GetPort(), OverlayLineState.Working);
            }
            else
            {
                AddStatusLine("Transport: Not initialized", OverlayLineState.NotInitialized);
            }

            int connectedClients = networkManager.ServerManager != null
                ? networkManager.ServerManager.Clients.Count
                : 0;
            AddValueLine("Connected clients", connectedClients, GetRunningState(serverStarted));

            int totalRooms = 0;
            int matchmakingRooms = 0;
            int activeBattles = 0;
            int finishedBattles = 0;
            int shownRooms = 0;

            for (int i = 0; i < _roomsShown.Length; i++)
            {
                _roomsShown[i] = null;
            }

            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                totalRooms++;
                if (!room.isInGame)
                {
                    matchmakingRooms++;
                }
                else if (room.isGameFinished)
                {
                    finishedBattles++;
                }
                else
                {
                    activeBattles++;
                }

                if (shownRooms < _roomsShown.Length)
                {
                    _roomsShown[shownRooms] = room;
                    shownRooms++;
                }
            }

            OverlayLineState serverState = GetRunningState(serverStarted);
            AddValueLine("Rooms", totalRooms, serverState);

            _lineBuilder.Clear();
            _lineBuilder.Append("Matchmaking: ").Append(matchmakingRooms)
                .Append(" | Active battles: ").Append(activeBattles)
                .Append(" | Finished: ").Append(finishedBattles);
            AddStatusLine(_lineBuilder.ToString(), serverState);

            _lineBuilder.Clear();
            _lineBuilder.Append("Pending results: ").Append(PendingBattleResults.GetPendingResultCount())
                .Append(" for ").Append(PendingBattleResults.GetPendingUserCount()).Append(" users");
            AddStatusLine(_lineBuilder.ToString(), serverState);

            for (int i = 0; i < shownRooms; i++)
            {
                AddRoomLine(_roomsShown[i]);
            }

            if (shownRooms < totalRooms)
            {
                AddStatusLine("... more rooms not shown", OverlayLineState.Info);
            }

            AddToggleHint();
        }

        private void ClearStatusLines()
        {
            for (int i = 0; i < _statusLineCount; i++)
            {
                _statusContents[i].text = string.Empty;
            }

            _statusLineCount = 0;
        }

        private void AddStatusLine(string line, OverlayLineState state)
        {
            if (_statusLineCount >= MaxStatusLines)
            {
                return;
            }

            _statusContents[_statusLineCount].text = line;
            _statusStates[_statusLineCount] = state;
            _statusLineCount++;
        }

        private void AddValueLine(string label, string value, OverlayLineState state)
        {
            _lineBuilder.Clear();
            _lineBuilder.Append(label).Append(": ").Append(value);
            AddStatusLine(_lineBuilder.ToString(), state);
        }

        private void AddValueLine(string label, int value, OverlayLineState state)
        {
            _lineBuilder.Clear();
            _lineBuilder.Append(label).Append(": ").Append(value);
            AddStatusLine(_lineBuilder.ToString(), state);
        }

        private void AddToggleHint()
        {
            AddStatusLine("` - hide/show overlay", OverlayLineState.Info);
        }

        private static bool IsServerEditorContext()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName == "VehicleTest")
            {
                return false;
            }

            if (activeSceneName == "Server")
            {
                return true;
            }

            NetworkManager networkManager = GetNetworkManager();
            return networkManager != null && networkManager.IsServerStarted;
        }

        private static NetworkManager GetNetworkManager()
        {
            IReadOnlyList<NetworkManager> instances = NetworkManager.Instances;
            if (instances == null || instances.Count == 0)
            {
                return null;
            }

            return instances[0];
        }

        private void AddRoomLine(ServerRoom room)
        {
            string roomId = string.IsNullOrEmpty(room.roomId) ? "no-id" : room.roomId;
            if (roomId.Length > 8)
            {
                roomId = roomId.Substring(0, 8);
            }

            string state = "Matchmaking";
            if (room.isGameFinished)
            {
                state = "Finished";
            }
            else if (room.isInGame)
            {
                state = "InGame";
            }

            _lineBuilder.Clear();
            _lineBuilder.Append("Room ").Append(roomId)
                .Append(" | ").Append(state)
                .Append(" | players ").Append(room.PlayersCount()).Append('/').Append(room.maxPlayers)
                .Append(" | map ").Append(room.selectedLocation)
                .Append(" | match ").Append(room.matchId);
            AddStatusLine(_lineBuilder.ToString(), OverlayLineState.Info);
        }

        private static string GetRoleText(bool serverStarted, bool clientStarted)
        {
            if (serverStarted && clientStarted)
            {
                return "Host";
            }

            if (serverStarted)
            {
                return "Server";
            }

            if (clientStarted)
            {
                return "Client";
            }

            return "Offline";
        }

        private static string GetStateText(bool started)
        {
            return started ? "Running" : "Stopped";
        }

        private static OverlayLineState GetRunningState(bool running)
        {
            return running ? OverlayLineState.Working : OverlayLineState.NotWorking;
        }

        private GUIStyle GetStyle(OverlayLineState state)
        {
            if (state == OverlayLineState.Working)
            {
                return _workingStyle;
            }

            if (state == OverlayLineState.NotWorking)
            {
                return _notWorkingStyle;
            }

            if (state == OverlayLineState.NotInitialized)
            {
                return _notInitializedStyle;
            }

            return _labelStyle;
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            _workingStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0.35f, 1f, 0.45f) }
            };

            _notWorkingStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(1f, 0.75f, 0.25f) }
            };

            _notInitializedStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
        }
#endif
    }
}
