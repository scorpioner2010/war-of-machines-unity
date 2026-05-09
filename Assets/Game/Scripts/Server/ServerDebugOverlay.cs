using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Server
{
    public class ServerDebugOverlay : MonoBehaviour
    {
        private const int MaxRoomsShown = 8;
        private static ServerDebugOverlay _instance;

        private bool _visible = true;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _okStyle;
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
            if (!IsServerEditorContext())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!Application.isEditor || !_visible || !IsServerEditorContext())
            {
                return;
            }

            EnsureStyles();

            GUILayout.BeginArea(new Rect(10f, 10f, 430f, 360f), GUI.skin.box);
            GUILayout.Label("Server Debug", _titleStyle);
            GUILayout.Space(4f);

            DrawNetworkStatus();
            GUILayout.Space(6f);
            DrawRoomsStatus();
            GUILayout.Space(6f);
            GUILayout.Label("F10 - hide/show overlay", _labelStyle);

            GUILayout.EndArea();
        }

        private void DrawNetworkStatus()
        {
            NetworkManager networkManager = GetNetworkManager();
            bool serverStarted = networkManager != null && networkManager.IsServerStarted;
            bool clientStarted = networkManager != null && networkManager.IsClientStarted;

            GUILayout.Label("Scene: " + SceneManager.GetActiveScene().name, _labelStyle);
            GUILayout.Label("Role: " + GetRoleText(serverStarted, clientStarted), _labelStyle);
            GUILayout.Label("Server: " + GetStateText(serverStarted), serverStarted ? _okStyle : _warnStyle);
            GUILayout.Label("Client: " + GetStateText(clientStarted), clientStarted ? _okStyle : _labelStyle);
            GUILayout.Label("Start status: " + StartServerButtons.LastServerStatus, serverStarted ? _okStyle : _warnStyle);

            if (networkManager == null)
            {
                GUILayout.Label("NetworkManager: missing", _warnStyle);
                return;
            }

            GUILayout.Label("Port: " + networkManager.TransportManager.Transport.GetPort(), _labelStyle);
            GUILayout.Label("Connected clients: " + networkManager.ServerManager.Clients.Count, _labelStyle);
        }

        private static bool IsServerEditorContext()
        {
            if (!Application.isEditor)
            {
                return false;
            }

            if (SceneManager.GetActiveScene().name == "Server")
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

        private void DrawRoomsStatus()
        {
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

            GUILayout.Label("Rooms: " + totalRooms, _labelStyle);
            GUILayout.Label("Matchmaking: " + matchmakingRooms + " | Active battles: " + activeBattles + " | Finished: " + finishedBattles, _labelStyle);
            GUILayout.Label("Pending results: " + PendingBattleResults.GetPendingResultCount() + " for " + PendingBattleResults.GetPendingUserCount() + " users", _labelStyle);

            int shown = 0;
            foreach (ServerRoom room in LobbyRooms.Rooms.Values)
            {
                if (room == null)
                {
                    continue;
                }

                if (shown >= MaxRoomsShown)
                {
                    GUILayout.Label("... more rooms not shown", _labelStyle);
                    break;
                }

                GUILayout.Label(FormatRoom(room), _labelStyle);
                shown++;
            }
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

            _okStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0.35f, 1f, 0.45f) }
            };

            _warnStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(1f, 0.75f, 0.25f) }
            };
        }
    }
}
