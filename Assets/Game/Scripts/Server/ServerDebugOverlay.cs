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
        private const int MaxRoomsShown = 8;
        private const int MaxStatusLines = 20;
        private const float StatusRefreshIntervalSeconds = 0.5f;
        private const float OverlayX = 10f;
        private const float OverlayY = 10f;
        private const float OverlayWidth = 430f;
        private const float TitleHeight = 24f;
        private const float LineHeight = 18f;
        private const float Padding = 8f;

        private static ServerDebugOverlay _instance;

        private readonly string[] _statusLines = new string[MaxStatusLines];
        private readonly bool[] _statusWarn = new bool[MaxStatusLines];

        private bool _visible = true;
        private bool _lastServerEditorContext;
        private int _statusLineCount;
        private float _nextStatusRefreshTime;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _warnStyle;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInEditor()
        {
            if (_instance != null)
            {
                return;
            }

            GameObject obj = new GameObject(nameof(ServerDebugOverlay));
            DontDestroyOnLoad(obj);
            _instance = obj.AddComponent<ServerDebugOverlay>();
        }
#endif

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            if (!Application.isEditor)
            {
                return;
            }

            _lastServerEditorContext = IsServerEditorContext();
            if (Input.GetKeyDown(KeyCode.F10) && _lastServerEditorContext)
            {
                _visible = !_visible;
                _nextStatusRefreshTime = 0f;
            }

            if (!_visible || !_lastServerEditorContext)
            {
                return;
            }

            if (Time.unscaledTime >= _nextStatusRefreshTime)
            {
                _nextStatusRefreshTime = Time.unscaledTime + StatusRefreshIntervalSeconds;
                RefreshStatusLines();
            }
        }

        private void OnGUI()
        {
            if (!Application.isEditor || !_visible || !_lastServerEditorContext)
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
                GUI.Label(new Rect(OverlayX + Padding, OverlayY + Padding, OverlayWidth - Padding * 2f, TitleHeight), "Server Debug", _titleStyle);

                float y = OverlayY + Padding + TitleHeight;
                for (int i = 0; i < _statusLineCount; i++)
                {
                    GUI.Label(
                        new Rect(OverlayX + Padding, y, OverlayWidth - Padding * 2f, LineHeight),
                        _statusLines[i],
                        _statusWarn[i] ? _warnStyle : _labelStyle);
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

            AddStatusLine("Scene: " + SceneManager.GetActiveScene().name, false);
            AddStatusLine("Role: " + GetRoleText(serverStarted, clientStarted), false);
            AddStatusLine("Server: " + GetStateText(serverStarted), !serverStarted);
            AddStatusLine("Client: " + GetStateText(clientStarted), false);
            AddStatusLine("Start status: " + StartServerButtons.LastServerStatus, !serverStarted);

            if (networkManager == null)
            {
                AddStatusLine("NetworkManager: missing", true);
                AddStatusLine("F10 - hide/show overlay", false);
                return;
            }

            AddStatusLine("Port: " + networkManager.TransportManager.Transport.GetPort(), false);
            AddStatusLine("Connected clients: " + networkManager.ServerManager.Clients.Count, false);

            int totalRooms = 0;
            int matchmakingRooms = 0;
            int activeBattles = 0;
            int finishedBattles = 0;

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
            }

            AddStatusLine("Rooms: " + totalRooms, false);
            AddStatusLine("Matchmaking: " + matchmakingRooms + " | Active battles: " + activeBattles + " | Finished: " + finishedBattles, false);
            AddStatusLine("Pending results: " + PendingBattleResults.GetPendingResultCount() + " for " + PendingBattleResults.GetPendingUserCount() + " users", false);

            int shown = 0;
            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                if (shown >= MaxRoomsShown)
                {
                    AddStatusLine("... more rooms not shown", false);
                    break;
                }

                AddStatusLine(FormatRoom(room), false);
                shown++;
            }

            AddStatusLine("F10 - hide/show overlay", false);
        }

        private void AddStatusLine(string line, bool warn)
        {
            if (_statusLineCount >= MaxStatusLines)
            {
                return;
            }

            _statusLines[_statusLineCount] = line;
            _statusWarn[_statusLineCount] = warn;
            _statusLineCount++;
        }

        private static bool IsServerEditorContext()
        {
            if (!Application.isEditor)
            {
                return false;
            }

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

        private static string FormatRoom(ServerRoom room)
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

            return "Room " + roomId
                   + " | " + state
                   + " | players " + room.PlayersCount() + "/" + room.maxPlayers
                   + " | map " + room.selectedLocation
                   + " | match " + room.matchId;
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
            return started ? "Started" : "Stopped";
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

            _warnStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(1f, 0.75f, 0.25f) }
            };
        }
    }
}
