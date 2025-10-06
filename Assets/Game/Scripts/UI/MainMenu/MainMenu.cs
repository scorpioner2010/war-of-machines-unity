using Game.Scripts.API.ServerManagers;
using Game.Scripts.Audio;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private Button customGameButton;
        [SerializeField] private Button settingsButton;
        
        [SerializeField] private GameObject customGamePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_Text user;
        
        [SerializeField] private GameObject[] mainMenuObjects;

        public static MainMenu In;
        
        private void Awake()
        {
            In = this;
            
            customGameButton.onClick.AddListener(()=>
            {
                MenuManager.OpenMenu(MenuType.CustomLobby);
            });
            
            settingsButton.onClick.AddListener(()=>
            {
                MenuManager.OpenMenu(MenuType.Settings);
            });
            
            mainMenu.gameObject.OnEnableAsObservable().Subscribe(OnEnableMenu).AddTo(this);
        }

        private void OnEnableMenu<T>(T obj)
        {
            IPlayerClientInfo player = ServiceLocator.Get<IPlayerClientInfo>();
            user.text = player.Profile.username;
        }

        public void SetActive(bool isActive)
        {
            foreach (GameObject obj in mainMenuObjects)
            {
                obj?.SetActive(isActive);
            }
            
            if (isActive)
            {
                MenuManager.OpenMenu(MenuType.MainMenu);
            }
            else
            {
                MenuManager.OpenMenu(MenuType.GameplayHUD);
            }
        }

        private void Exit()
        {
            Application.Quit();
        }
    }
}