using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class GameResultUI : MonoBehaviour
    {
        private static GameResultUI _in;
        private static readonly Queue<ResultData> PendingResults = new Queue<ResultData>();
        private static ResultData? _current;
        private static bool _waitingForMainMenu;

        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button okButton;

        private bool _wired;

        private struct ResultData
        {
            public string RoomId;
            public string Result;
            public int Kills;
            public int Damage;
            public int XpEarned;
            public int Bolts;
            public int FreeXp;
            public int MmrDelta;
        }

        private void Awake()
        {
            _in = this;
            EnsureWired();
        }

        public static void Enqueue(string roomId, string result, int kills, int damage, int xpEarned, int bolts, int freeXp, int mmrDelta)
        {
            PendingResults.Enqueue(new ResultData
            {
                RoomId = roomId,
                Result = result,
                Kills = kills,
                Damage = damage,
                XpEarned = xpEarned,
                Bolts = bolts,
                FreeXp = freeXp,
                MmrDelta = mmrDelta
            });

            if (MenuManager.CurrentType != MenuType.MainMenu && MenuManager.CurrentType != MenuType.GameResult)
            {
                WaitForMainMenu();
                return;
            }

            GameResultUI ui = GetOrCreate();
            if (ui == null)
            {
                return;
            }

            ui.TryShowNext();
        }

        private static GameResultUI GetOrCreate()
        {
            if (_in != null)
            {
                return _in;
            }

            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null || obj.name != "GameResult" || !obj.scene.IsValid())
                {
                    continue;
                }

                GameResultUI ui = obj.GetComponent<GameResultUI>();
                if (ui == null)
                {
                    ui = obj.AddComponent<GameResultUI>();
                }

                _in = ui;
                ui.EnsureWired();
                return ui;
            }

            return null;
        }

        private static void WaitForMainMenu()
        {
            _waitingForMainMenu = true;
            MenuManager.OnEnable -= HandleMenuOpened;
            MenuManager.OnEnable += HandleMenuOpened;
        }

        private void EnsureWired()
        {
            if (_wired)
            {
                return;
            }

            if (summaryText == null)
            {
                TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0)
                {
                    summaryText = texts[0];
                }
            }

            if (okButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0)
                {
                    okButton = buttons[0];
                }
            }

            if (okButton != null)
            {
                okButton.onClick.RemoveListener(Close);
                okButton.onClick.AddListener(Close);
            }

            _wired = true;
        }

        private static void HandleMenuOpened(MenuType type)
        {
            if (type != MenuType.MainMenu || !_waitingForMainMenu)
            {
                return;
            }

            _waitingForMainMenu = false;
            MenuManager.OnEnable -= HandleMenuOpened;

            GameResultUI ui = GetOrCreate();
            if (ui != null)
            {
                ui.TryShowNext();
            }
        }

        private void TryShowNext()
        {
            if (_current.HasValue)
            {
                return;
            }

            if (PendingResults.Count == 0
                || (MenuManager.CurrentType != MenuType.MainMenu && MenuManager.CurrentType != MenuType.GameResult))
            {
                return;
            }

            EnsureWired();
            ResultData data = PendingResults.Dequeue();
            _current = data;

            if (summaryText != null)
            {
                summaryText.text =
                    $"Battle result: {FormatResult(data.Result)}\n" +
                    $"XP: {data.XpEarned}\n" +
                    $"Bolts: {data.Bolts}\n" +
                    $"Free XP: {data.FreeXp}\n" +
                    $"MMR: {FormatSigned(data.MmrDelta)}\n" +
                    $"Kills: {data.Kills}\n" +
                    $"Damage: {data.Damage}";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MenuManager.OpenMenu(MenuType.GameResult);
        }

        private void Close()
        {
            ResultData? closed = _current;
            _current = null;

            if (closed.HasValue && GameplaySpawner.In != null)
            {
                GameplaySpawner.In.ConfirmGameResultReceivedServerRpc(closed.Value.RoomId);
            }

            ProfileServer.UpdateProfile();

            if (PendingResults.Count > 0)
            {
                MenuManager.CloseMenu(MenuType.GameResult);
                ShowNextDelayed().Forget();
                return;
            }

            MenuManager.OpenMenu(MenuType.MainMenu);
        }

        private async UniTaskVoid ShowNextDelayed()
        {
            await UniTask.Delay(350);
            TryShowNext();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string FormatResult(string result)
        {
            if (result == "win")
            {
                return "WIN";
            }

            if (result == "draw")
            {
                return "DRAW";
            }

            return "LOSE";
        }

        private static string FormatSigned(int value)
        {
            if (value > 0)
            {
                return "+" + value;
            }

            return value.ToString();
        }
    }
}
