using System.Globalization;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.UI;
using Game.Scripts.UI.Tree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button developmentTreeButton;
        [SerializeField] private Button freeXpButton;
        [SerializeField] private FreeXpConversionPopup freeXpConversionPopup;
        [SerializeField] private Button profileButton;
        [SerializeField] private GameObject profileStatisticsPopup;
        [SerializeField] private ProfileStatisticsPopup profileStatisticsPopupView;

        [SerializeField] private TMP_Text user;
        [SerializeField] private TMP_Text bolts;
        [SerializeField] private TMP_Text freeXp;
        [SerializeField] private TMP_Text adamant;
        [SerializeField] private TMP_Text mmr;

        [SerializeField] private RightInfoPanel rightInfoPanel;
        
        [SerializeField] private GameObject[] mainMenuObjects;

        public static MainMenu In;
        
        private void Awake()
        {
            In = this;

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

            if (freeXpButton != null && freeXpConversionPopup != null)
            {
                freeXpButton.onClick.AddListener(freeXpConversionPopup.Show);
            }

            if (profileButton != null && profileStatisticsPopup != null)
            {
                profileButton.onClick.AddListener(ShowProfileStatisticsPopup);
            }

            MenuManager.OnEnable += type =>
            {
                if (type == MenuType.MainMenu)
                {
                    ProfileServer.UpdateProfile();
                }
            };
        }

        public void ShowProfileStatisticsPopup()
        {
            if (profileStatisticsPopup == null)
            {
                return;
            }

            profileStatisticsPopup.SetActive(true);
            profileStatisticsPopup.transform.SetAsLastSibling();
            if (profileStatisticsPopupView != null)
            {
                profileStatisticsPopupView.Open();
            }
            else
            {
                ProfileServer.RequestProfileStatistics();
            }
        }

        public void HideProfileStatisticsPopup()
        {
            if (profileStatisticsPopupView != null)
            {
                profileStatisticsPopupView.Close();
            }
            else if (profileStatisticsPopup != null)
            {
                profileStatisticsPopup.SetActive(false);
            }
        }

        public void UpdateProfileStatistics(PlayerStatisticsDto statistics)
        {
            profileStatisticsPopupView?.ApplyStatistics(statistics);
        }

        public void ShowProfileStatisticsError(string message)
        {
            profileStatisticsPopupView?.ShowError(message);
        }

        public void UpdatePlayerInfo(PlayerProfile profile)
        {
            if (profile == null)
            {
                SetText(user, string.Empty);
                SetText(mmr, string.Empty);
                SetText(bolts, string.Empty);
                SetText(adamant, string.Empty);
                SetText(freeXp, string.Empty);
                rightInfoPanel?.SetMainMenuExperience(string.Empty);
                return;
            }

            SetText(user, profile.username);
            SetText(mmr, profile.mmr.ToString(CultureInfo.InvariantCulture));
            SetText(bolts, profile.bolts.ToString(CultureInfo.InvariantCulture));
            SetText(adamant, profile.adamant.ToString(CultureInfo.InvariantCulture));
            SetText(freeXp, profile.freeXp.ToString(CultureInfo.InvariantCulture));
            
            OwnedVehicleDto active = profile.GetSelected();
            string experience = active != null ? active.xp.ToString(CultureInfo.InvariantCulture) : string.Empty;
            rightInfoPanel?.SetMainMenuExperience(experience);
        }

        public void UpdateVehicleInfo(VehicleSlotVehicleData vehicle)
        {
            rightInfoPanel?.ShowMainMenuVehicle(vehicle);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
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
