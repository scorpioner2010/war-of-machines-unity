using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Lobby
{
    public enum EndGameResult
    {
        Win = 0,
        Lose = 1,
        Draw = 2
    }

    public class EndGameUI : MonoBehaviour
    {
        private const string WinText = "WIN";
        private const string LoseText = "LOSE";
        private const string DrawText = "DRAW";

        private static EndGameUI _in;

        [SerializeField] private TMP_Text status;
        [SerializeField] private Button okButton;

        private void Awake()
        {
            _in = this;

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
                }
                else if (result == EndGameResult.Lose)
                {
                    status.text = LoseText;
                }
                else
                {
                    status.text = DrawText;
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
