using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class EndGameUI : MonoBehaviour
    {
        private const string ModeText = "TEAM BATTLE";
        private const string GoldColor = "#D6B56A";
        private const string OliveColor = "#7C8568";
        private const string LabelColor = "#C1C2B4";
        private const string ValueColor = "#F1EEE2";
        private const string MutedColor = "#8E9583";
        private const string WinColor = "#86BD6A";
        private const string LoseColor = "#C86B5A";
        private const string DrawColor = "#83A7B8";
        private const string UnknownMapText = "UNKNOWN MAP";
        private const string VictoryText = "VICTORY";
        private const string DefeatText = "DEFEAT";
        private const string DrawText = "DRAW";

        private static EndGameUI _in;
        private static EndGameResult? _pendingResult;
        private static EndGameSnapshot? _pendingSnapshot;

        [SerializeField] private TMP_Text status;
        [SerializeField] private Button okButton;

        private struct EndGameSnapshot
        {
            public string MapName;
            public int DurationSeconds;
            public int TeamAScore;
            public int TeamBScore;
        }

        private void Awake()
        {
            _in = this;

            if (_pendingResult.HasValue)
            {
                SetResult(_pendingResult.Value, _pendingSnapshot ?? default(EndGameSnapshot));
            }

            if (okButton != null)
            {
                okButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        private void OnDestroy()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }

        public static void Show(
            EndGameResult result,
            string mapName,
            int durationSeconds,
            int teamAScore,
            int teamBScore)
        {
            EndGameSnapshot snapshot = new EndGameSnapshot
            {
                MapName = mapName,
                DurationSeconds = durationSeconds,
                TeamAScore = teamAScore,
                TeamBScore = teamBScore
            };

            _pendingResult = result;
            _pendingSnapshot = snapshot;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_in == null)
            {
                return;
            }

            _in.SetResult(result, snapshot);
        }

        private void SetResult(EndGameResult result, EndGameSnapshot snapshot)
        {
            if (status != null)
            {
                status.text = BuildSummary(result, snapshot);
                status.color = Color.white;
            }
        }

        private static string BuildSummary(EndGameResult result, EndGameSnapshot snapshot)
        {
            string outcome = FormatResult(result);
            string outcomeColor = GetResultColor(result);

            return
                $"<align=center><size=30><color={GoldColor}><b>BATTLE COMPLETE</b></color></size>\n" +
                $"<size=28><color={outcomeColor}><b>{outcome}</b></color></size>\n" +
                $"<size=10><color={OliveColor}>------------------------------</color></size>\n" +
                $"<size=11><color={MutedColor}>TACTICAL SNAPSHOT</color></size>\n" +
                BuildSnapshotLine("MAP", FormatMapName(snapshot.MapName)) + "\n" +
                BuildSnapshotLine("MODE", ModeText) + "\n" +
                BuildSnapshotLine("DURATION", FormatDuration(snapshot.DurationSeconds)) + "\n" +
                BuildSnapshotLine("TEAM SCORE", FormatTeamScore(snapshot.TeamAScore, snapshot.TeamBScore)) +
                "</align>";
        }

        private static string BuildSnapshotLine(string label, string value)
        {
            return $"<size=14><color={LabelColor}>{label}</color> <color={OliveColor}>:</color> <color={ValueColor}><b>{value}</b></color></size>";
        }

        private static string FormatResult(EndGameResult result)
        {
            if (result == EndGameResult.Win)
            {
                return VictoryText;
            }

            if (result == EndGameResult.Lose)
            {
                return DefeatText;
            }

            return DrawText;
        }

        private static string GetResultColor(EndGameResult result)
        {
            if (result == EndGameResult.Win)
            {
                return WinColor;
            }

            if (result == EndGameResult.Lose)
            {
                return LoseColor;
            }

            return DrawColor;
        }

        private static string FormatMapName(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UnknownMapText;
            }

            return mapName.Replace("_", " ").Replace("-", " ").ToUpperInvariant();
        }

        private static string FormatDuration(int durationSeconds)
        {
            int safeSeconds = Mathf.Max(0, durationSeconds);
            int minutes = safeSeconds / 60;
            int seconds = safeSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private static string FormatTeamScore(int teamAScore, int teamBScore)
        {
            return $"{Mathf.Max(0, teamAScore)}  -  {Mathf.Max(0, teamBScore)}";
        }

        private void ReturnToMainMenu()
        {
            MenuManager.CloseMenu(MenuType.EndGame);

            if (GameplaySpawner.In != null)
            {
                GameplaySpawner.In.ReturnToMainMenu();
            }
            else
            {
                MenuManager.OpenMenu(MenuType.MainMenu);
            }
        }
    }
}
