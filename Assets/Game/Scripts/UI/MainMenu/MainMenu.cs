using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.UI.Tree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button customGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button developmentTreeButton;

        [SerializeField] private TMP_Text user;
        [SerializeField] private TMP_Text bolts;
        [SerializeField] private TMP_Text freeXp;
        [SerializeField] private TMP_Text xp;
        [SerializeField] private TMP_Text adamant;
        [SerializeField] private TMP_Text mmr;
        
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
            
            developmentTreeButton.onClick.AddListener(()=>
            {
                MenuManager.OpenMenu(MenuType.DevelopmentTree);
                DevelopmentTree tree = Singleton<DevelopmentTree>.CurrentOrNull;
                if (tree != null)
                {
                    tree.Init();
                }
            });

            MenuManager.OnEnable += type =>
            {
                if (type == MenuType.MainMenu)
                {
                    ProfileServer.UpdateProfile();
                }
            };
        }

        public void UpdatePlayerInfo(PlayerProfile profile)
        {
            user.text = profile.username;
            mmr.text = profile.mmr.ToString();
            bolts.text = profile.bolts.ToString();
            adamant.text = profile.adamant.ToString();
            freeXp.text = profile.freeXp.ToString();
            
            OwnedVehicleDto active = profile.GetSelected();

            if (active != null)
            {
                xp.text = active.xp.ToString();
            }
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
    }
}
