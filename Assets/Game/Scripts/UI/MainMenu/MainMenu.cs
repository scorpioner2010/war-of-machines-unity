using System.Globalization;
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
        [SerializeField] private TMP_Text xp;
        [SerializeField] private TMP_Text adamant;
        [SerializeField] private TMP_Text mmr;

        [Header("Selected vehicle")]
        [SerializeField] private TMP_Text selectedVehicleName;
        [SerializeField] private Image selectedVehicleTierIcon;
        [SerializeField] private Sprite[] tierSprites;
        [SerializeField] private TMP_Text selectedVehicleFaction;
        [SerializeField] private TMP_Text selectedVehicleHitPoints;
        [SerializeField] private TMP_Text selectedVehicleTurretArmor;
        [SerializeField] private TMP_Text selectedVehicleHullArmor;
        [SerializeField] private TMP_Text selectedVehicleViewRange;
        [SerializeField] private TMP_Text selectedVehicleSpeed;
        [SerializeField] private TMP_Text selectedVehicleAcceleration;
        [SerializeField] private TMP_Text selectedVehicleTraverseSpeed;
        [SerializeField] private TMP_Text selectedVehicleTurretTraverseSpeed;
        [SerializeField] private TMP_Text selectedVehicleDamage;
        [SerializeField] private TMP_Text selectedVehiclePenetration;
        [SerializeField] private TMP_Text selectedVehicleShellSpeed;
        [SerializeField] private TMP_Text selectedVehicleShellsCount;
        [SerializeField] private TMP_Text selectedVehicleReloadTime;
        [SerializeField] private TMP_Text selectedVehicleAccuracy;
        [SerializeField] private TMP_Text selectedVehicleAimTime;
        [SerializeField] private TMP_Text selectedVehicleAverageDps;
        
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
                SetText(xp, string.Empty);
                return;
            }

            SetText(user, profile.username);
            SetText(mmr, profile.mmr.ToString(CultureInfo.InvariantCulture));
            SetText(bolts, profile.bolts.ToString(CultureInfo.InvariantCulture));
            SetText(adamant, profile.adamant.ToString(CultureInfo.InvariantCulture));
            SetText(freeXp, profile.freeXp.ToString(CultureInfo.InvariantCulture));
            
            OwnedVehicleDto active = profile.GetSelected();
            SetText(xp, active != null ? active.xp.ToString(CultureInfo.InvariantCulture) : string.Empty);
        }

        public void UpdateVehicleInfo(VehicleSlotVehicleData vehicle)
        {
            SetText(selectedVehicleName, vehicle?.Name);
            SetText(selectedVehicleFaction, vehicle?.FactionName);
            SetTierIcon(vehicle != null ? vehicle.Level : 0);
            SetText(selectedVehicleHitPoints, FormatInt(vehicle?.Hp ?? 0));
            SetText(selectedVehicleTurretArmor, vehicle?.TurretArmor);
            SetText(selectedVehicleHullArmor, vehicle?.HullArmor);
            SetText(selectedVehicleViewRange, FormatFloat(vehicle?.ViewRange ?? 0f));
            SetText(selectedVehicleSpeed, FormatFloat(vehicle?.Speed ?? 0f));
            SetText(selectedVehicleAcceleration, FormatFloat(vehicle?.Acceleration ?? 0f));
            SetText(selectedVehicleTraverseSpeed, FormatFloat(vehicle?.TraverseSpeed ?? 0f));
            SetText(selectedVehicleTurretTraverseSpeed, FormatFloat(vehicle?.TurretTraverseSpeed ?? 0f));
            SetText(selectedVehicleDamage, FormatRange(vehicle?.DamageMin ?? 0f, vehicle?.DamageMax ?? 0f));
            SetText(selectedVehiclePenetration, FormatInt(vehicle?.Penetration ?? 0));
            SetText(selectedVehicleShellSpeed, FormatFloat(vehicle?.ShellSpeed ?? 0f));
            SetText(selectedVehicleShellsCount, FormatInt(vehicle?.ShellsCount ?? 0));
            SetText(selectedVehicleReloadTime, FormatFloat(vehicle?.ReloadTime ?? 0f));
            SetText(selectedVehicleAccuracy, FormatFloat(vehicle?.Accuracy ?? 0f));
            SetText(selectedVehicleAimTime, FormatFloat(vehicle?.AimTime ?? 0f));
            SetText(selectedVehicleAverageDps, FormatAverageDps(vehicle));
        }

        private void SetTierIcon(int level)
        {
            if (selectedVehicleTierIcon == null)
            {
                return;
            }

            int tierIndex = level - 1;
            bool hasTierSprite = tierSprites != null
                && tierIndex >= 0
                && tierIndex < tierSprites.Length
                && tierSprites[tierIndex] != null;

            selectedVehicleTierIcon.enabled = hasTierSprite;
            selectedVehicleTierIcon.sprite = hasTierSprite ? tierSprites[tierIndex] : null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
            }
        }

        private static string FormatInt(int value)
        {
            return value > 0 ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string FormatFloat(float value)
        {
            return value > 0f ? value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string FormatRange(float min, float max)
        {
            if (min <= 0f && max <= 0f)
            {
                return string.Empty;
            }

            if (Mathf.Approximately(min, max))
            {
                return min.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return min.ToString("0.##", CultureInfo.InvariantCulture)
                + "-"
                + max.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatAverageDps(VehicleSlotVehicleData vehicle)
        {
            if (vehicle == null || vehicle.ReloadTime <= 0f)
            {
                return string.Empty;
            }

            float averageDamage = (vehicle.DamageMin + vehicle.DamageMax) * 0.5f;
            if (averageDamage <= 0f)
            {
                return string.Empty;
            }

            return (averageDamage / vehicle.ReloadTime).ToString("0.##", CultureInfo.InvariantCulture);
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
