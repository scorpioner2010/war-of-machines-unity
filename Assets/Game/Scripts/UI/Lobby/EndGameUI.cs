using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public class EndGameUI : MonoBehaviour
    {
        private const string WinText = "WIN";
        private const string LoseText = "LOSE";
        private const string DrawText = "DRAW";
        private static readonly Color WinColor = new Color(0.24f, 0.82f, 0.34f);
        private static readonly Color LoseColor = new Color(0.92f, 0.18f, 0.18f);
        private static readonly Color DrawColor = new Color(0.24f, 0.52f, 1f);

        private static EndGameUI _in;
        private static EndGameResult? _pendingResult;

        [SerializeField] private TMP_Text status;
        [SerializeField] private Button okButton;

        private void Awake()
        {
            _in = this;

            if (_pendingResult.HasValue)
            {
                SetResult(_pendingResult.Value);
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

        public static void Show(EndGameResult result)
        {
            _pendingResult = result;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_in == null)
            {
                return;
            }

            _in.SetResult(result);
        }

        private void SetResult(EndGameResult result)
        {
            if (status != null)
            {
                if (result == EndGameResult.Win)
                {
                    status.text = WinText;
                    status.color = WinColor;
                }
                else if (result == EndGameResult.Lose)
                {
                    status.text = LoseText;
                    status.color = LoseColor;
                }
                else
                {
                    status.text = DrawText;
                    status.color = DrawColor;
                }
            }
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
