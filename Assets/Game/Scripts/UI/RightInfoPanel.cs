using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Scripts.API;
using Game.Scripts.Core.Resources;
using Game.Scripts.MenuController;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.UI.Tree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public sealed class RightInfoPanel : MonoBehaviour
    {
        [Serializable]
        private struct PanelPlacement
        {
            public RectTransform parent;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public int siblingIndex;
        }

        [Header("Header")]
        [SerializeField] private TMP_Text vehicleName;
        [SerializeField] private TMP_Text faction;
        [SerializeField] private Image vehicleImage;
        [SerializeField] private Image levelIcon;
        [SerializeField] private Sprite[] levelSprites;
        [SerializeField] private TMP_Text progression;

        [Header("Survivability")]
        [SerializeField] private TMP_Text hitPoints;
        [SerializeField] private TMP_Text viewRange;
        [SerializeField] private TMP_Text turretArmor;
        [SerializeField] private TMP_Text hullArmor;

        [Header("Mobility")]
        [SerializeField] private TMP_Text topSpeed;
        [SerializeField] private TMP_Text acceleration;
        [SerializeField] private TMP_Text enginePower;
        [SerializeField] private TMP_Text hullTraverseSpeed;
        [SerializeField] private TMP_Text turretTraverseSpeed;

        [Header("Firepower")]
        [SerializeField] private TMP_Text damage;
        [SerializeField] private TMP_Text penetration;
        [SerializeField] private TMP_Text reloadTime;
        [SerializeField] private TMP_Text averageDps;
        [SerializeField] private TMP_Text ammoCapacity;
        [SerializeField] private TMP_Text projectileSpeed;
        [SerializeField] private TMP_Text accuracy;
        [SerializeField] private TMP_Text aimTime;

        [Header("Actions")]
        [SerializeField] private Button battleButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button researchButton;

        [Header("Menu placement")]
        [SerializeField] private PanelPlacement mainMenuPlacement;
        [SerializeField] private PanelPlacement developmentTreePlacement;
        [SerializeField] private RectTransform[] mainMenuContentElements;
        [SerializeField] private Vector2 mainMenuContentOffset = new Vector2(18.04f, 73f);

        private readonly Dictionary<RectTransform, Vector2> _baseContentPositions =
            new Dictionary<RectTransform, Vector2>();

        private VehicleSlotVehicleData _mainMenuVehicle;
        private VehicleResearchProgress _developmentProgress;
        private string _mainMenuExperience = string.Empty;
        private Action<VehicleResearchProgress> _buyAction;
        private Action<VehicleResearchProgress> _researchAction;
        private Context _context;
        private bool _isInitialized;

        private enum Context
        {
            Hidden,
            MainMenu,
            DevelopmentTree,
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            CacheBaseContentPositions();

            if (buyButton != null)
            {
                buyButton.onClick.AddListener(BuySelectedVehicle);
            }

            if (researchButton != null)
            {
                researchButton.onClick.AddListener(ResearchSelectedVehicle);
            }

            _isInitialized = true;
        }

        public void ApplyMenuContext(MenuType type)
        {
            Initialize();
            HandleMenuEnabled(type);
        }

        public void ShowMainMenuVehicle(VehicleSlotVehicleData vehicle)
        {
            Initialize();
            _mainMenuVehicle = vehicle;
            if (_context == Context.MainMenu)
            {
                RenderMainMenu();
            }
        }

        public void SetMainMenuExperience(string value)
        {
            Initialize();
            _mainMenuExperience = value;
            if (_context == Context.MainMenu)
            {
                SetMainMenuText(progression, value);
            }
        }

        public void ShowDevelopment(
            VehicleResearchProgress progress,
            Action<VehicleResearchProgress> buyAction,
            Action<VehicleResearchProgress> researchAction)
        {
            Initialize();
            _developmentProgress = progress;
            _buyAction = buyAction;
            _researchAction = researchAction;

            if (_context == Context.DevelopmentTree)
            {
                RenderDevelopmentTree();
            }
        }

        public void Clear()
        {
            Initialize();
            _developmentProgress = null;
            _buyAction = null;
            _researchAction = null;

            if (_context == Context.DevelopmentTree)
            {
                RenderDevelopmentTree();
            }
        }

        private void HandleMenuEnabled(MenuType type)
        {
            if (type == MenuType.MainMenu)
            {
                ApplyContext(Context.MainMenu, mainMenuPlacement);
                return;
            }

            if (type == MenuType.DevelopmentTree)
            {
                ApplyContext(Context.DevelopmentTree, developmentTreePlacement);
                return;
            }

            _context = Context.Hidden;
            gameObject.SetActive(false);
        }

        private void ApplyContext(Context context, PanelPlacement placement)
        {
            _context = context;
            RectTransform panel = (RectTransform)transform;
            panel.SetParent(placement.parent, false);
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.localRotation = Quaternion.identity;
            panel.localScale = Vector3.one;
            panel.anchoredPosition = placement.anchoredPosition;
            panel.sizeDelta = placement.sizeDelta;
            panel.SetSiblingIndex(Mathf.Clamp(placement.siblingIndex, 0, placement.parent.childCount - 1));

            ApplyContentOffset(context == Context.MainMenu);
            SetContextButtons(context);

            if (context == Context.MainMenu)
            {
                RenderMainMenu();
            }
            else
            {
                RenderDevelopmentTree();
            }

            gameObject.SetActive(true);
        }

        private void CacheBaseContentPositions()
        {
            _baseContentPositions.Clear();
            if (mainMenuContentElements == null)
            {
                return;
            }

            for (int i = 0; i < mainMenuContentElements.Length; i++)
            {
                RectTransform element = mainMenuContentElements[i];
                if (element != null && !_baseContentPositions.ContainsKey(element))
                {
                    _baseContentPositions.Add(element, element.anchoredPosition);
                }
            }
        }

        private void ApplyContentOffset(bool isMainMenu)
        {
            Vector2 offset = isMainMenu ? mainMenuContentOffset : Vector2.zero;
            foreach (KeyValuePair<RectTransform, Vector2> pair in _baseContentPositions)
            {
                pair.Key.anchoredPosition = pair.Value + offset;
            }
        }

        private void SetContextButtons(Context context)
        {
            if (battleButton != null)
            {
                battleButton.gameObject.SetActive(context == Context.MainMenu);
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(context == Context.DevelopmentTree);
            }

            if (researchButton != null)
            {
                researchButton.gameObject.SetActive(context == Context.DevelopmentTree);
            }

            if (vehicleImage != null && context == Context.MainMenu)
            {
                vehicleImage.enabled = false;
                vehicleImage.gameObject.SetActive(false);
            }
        }

        private void RenderMainMenu()
        {
            VehicleSlotVehicleData vehicle = _mainMenuVehicle;
            SetMainMenuText(vehicleName, vehicle?.Name);
            SetMainMenuText(faction, vehicle?.FactionName);
            SetLevelIcon(vehicle != null ? vehicle.Level : 0);
            SetMainMenuText(hitPoints, FormatInt(vehicle?.Hp ?? 0));
            SetMainMenuText(turretArmor, vehicle?.TurretArmor);
            SetMainMenuText(hullArmor, vehicle?.HullArmor);
            SetMainMenuText(viewRange, FormatFloat(vehicle?.ViewRange ?? 0f));
            SetMainMenuText(topSpeed, FormatFloat(vehicle?.Speed ?? 0f));
            SetMainMenuText(acceleration, FormatFloat(vehicle?.Acceleration ?? 0f));
            SetMainMenuText(enginePower, FormatFloat(vehicle?.EnginePower ?? 0f));
            SetMainMenuText(hullTraverseSpeed, FormatFloat(vehicle?.TraverseSpeed ?? 0f));
            SetMainMenuText(turretTraverseSpeed, FormatFloat(vehicle?.TurretTraverseSpeed ?? 0f));
            SetMainMenuText(damage, FormatRange(vehicle?.DamageMin ?? 0f, vehicle?.DamageMax ?? 0f));
            SetMainMenuText(penetration, FormatInt(vehicle?.Penetration ?? 0));
            SetMainMenuText(projectileSpeed, FormatFloat(vehicle?.ShellSpeed ?? 0f));
            SetMainMenuText(ammoCapacity, FormatInt(vehicle?.ShellsCount ?? 0));
            SetMainMenuText(reloadTime, FormatFloat(vehicle?.ReloadTime ?? 0f));
            SetMainMenuText(accuracy, FormatFloat(vehicle?.Accuracy ?? 0f));
            SetMainMenuText(aimTime, FormatFloat(vehicle?.AimTime ?? 0f));
            SetMainMenuText(averageDps, FormatAverageDps(vehicle));
            SetMainMenuText(progression, _mainMenuExperience);

            if (vehicleImage != null)
            {
                vehicleImage.enabled = false;
                vehicleImage.gameObject.SetActive(false);
            }
        }

        private void RenderDevelopmentTree()
        {
            VehicleResearchProgress progress = _developmentProgress;
            if (progress == null || progress.Node == null)
            {
                ClearDevelopmentFields();
                return;
            }

            VehicleNode node = progress.Node;
            VehicleLite vehicle = progress.Vehicle;

            SetText(vehicleName, node.name);
            SetText(faction, vehicle != null && !string.IsNullOrWhiteSpace(vehicle.factionName)
                ? vehicle.factionName
                : node.factionCode);
            SetVehicleImage(node.code, !progress.IsVisible);
            SetLevelIcon(node.level);
            SetText(progression, BuildProgressionText(progress));
            SetText(hitPoints, vehicle != null ? vehicle.hp.ToString() : string.Empty);
            SetText(viewRange, FormatFloat(vehicle != null ? vehicle.viewRange : node.viewRange));
            SetText(turretArmor, vehicle != null ? vehicle.turretArmor : string.Empty);
            SetText(hullArmor, vehicle != null ? vehicle.hullArmor : string.Empty);
            SetText(topSpeed, FormatFloat(vehicle != null ? vehicle.speed : 0f));
            SetText(acceleration, FormatFloat(vehicle != null ? vehicle.acceleration : 0f));
            SetText(enginePower, FormatFloat(vehicle != null ? vehicle.enginePower : node.enginePower));
            SetText(hullTraverseSpeed, FormatFloat(vehicle != null ? vehicle.traverseSpeed : 0f));
            SetText(turretTraverseSpeed, FormatFloat(vehicle != null ? vehicle.turretTraverseSpeed : 0f));
            SetText(damage, BuildDamageText(vehicle, node));
            SetText(penetration, vehicle != null ? vehicle.penetration.ToString() : string.Empty);
            SetText(reloadTime, FormatFloat(vehicle != null ? vehicle.reloadTime : 0f));
            SetText(averageDps, FormatFloat(CalculateAverageDps(vehicle, node)));
            SetText(ammoCapacity, (vehicle != null ? vehicle.shellsCount : node.shellsCount).ToString());
            SetText(projectileSpeed, FormatFloat(vehicle != null ? vehicle.shellSpeed : node.shellSpeed));
            SetText(accuracy, FormatFloat(vehicle != null ? vehicle.accuracy : 0f));
            SetText(aimTime, FormatFloat(vehicle != null ? vehicle.aimTime : 0f));
            SetButtonStates(progress);
        }

        private void ClearDevelopmentFields()
        {
            SetText(vehicleName, string.Empty);
            SetText(faction, string.Empty);
            SetVehicleImage(null, false);
            SetLevelIcon(0);
            SetText(progression, string.Empty);
            SetText(hitPoints, string.Empty);
            SetText(viewRange, string.Empty);
            SetText(turretArmor, string.Empty);
            SetText(hullArmor, string.Empty);
            SetText(topSpeed, string.Empty);
            SetText(acceleration, string.Empty);
            SetText(enginePower, string.Empty);
            SetText(hullTraverseSpeed, string.Empty);
            SetText(turretTraverseSpeed, string.Empty);
            SetText(damage, string.Empty);
            SetText(penetration, string.Empty);
            SetText(reloadTime, string.Empty);
            SetText(averageDps, string.Empty);
            SetText(ammoCapacity, string.Empty);
            SetText(projectileSpeed, string.Empty);
            SetText(accuracy, string.Empty);
            SetText(aimTime, string.Empty);

            if (buyButton != null)
            {
                buyButton.interactable = false;
            }

            if (researchButton != null)
            {
                researchButton.interactable = false;
            }
        }

        private void SetButtonStates(VehicleResearchProgress progress)
        {
            if (buyButton != null)
            {
                buyButton.interactable = progress.Status == VehicleResearchStatus.AvailableToBuy;
            }

            if (researchButton != null)
            {
                researchButton.interactable = progress.Status == VehicleResearchStatus.AvailableToResearch;
            }
        }

        private void BuySelectedVehicle()
        {
            if (_developmentProgress != null
                && _developmentProgress.Status == VehicleResearchStatus.AvailableToBuy)
            {
                _buyAction?.Invoke(_developmentProgress);
            }
        }

        private void ResearchSelectedVehicle()
        {
            if (_developmentProgress != null
                && _developmentProgress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                _researchAction?.Invoke(_developmentProgress);
            }
        }

        private void SetVehicleImage(string vehicleCode, bool unavailable)
        {
            if (vehicleImage == null)
            {
                return;
            }

            Sprite icon = string.IsNullOrWhiteSpace(vehicleCode)
                ? null
                : GameResourceManager.GetIcon(vehicleCode);

            bool hasIcon = icon != null;
            vehicleImage.enabled = hasIcon;
            vehicleImage.gameObject.SetActive(hasIcon && _context == Context.DevelopmentTree);
            vehicleImage.sprite = icon;
            vehicleImage.color = unavailable ? Color.black : Color.white;
        }

        private void SetLevelIcon(int vehicleLevel)
        {
            if (levelIcon == null)
            {
                return;
            }

            int spriteIndex = vehicleLevel - 1;
            bool hasIcon = levelSprites != null
                && spriteIndex >= 0
                && spriteIndex < levelSprites.Length
                && levelSprites[spriteIndex] != null;

            levelIcon.enabled = hasIcon;
            levelIcon.sprite = hasIcon ? levelSprites[spriteIndex] : null;
        }

        private static string BuildProgressionText(VehicleResearchProgress progress)
        {
            if (progress.Status == VehicleResearchStatus.Owned)
            {
                return "Owned";
            }

            if (progress.Status == VehicleResearchStatus.AvailableToBuy)
            {
                return "Purchase: " + progress.PurchaseCost;
            }

            if (progress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                return "Research: " + progress.RequiredXp + " XP";
            }

            if (progress.Status == VehicleResearchStatus.LockedByResearch)
            {
                return "XP " + progress.CurrentXp + "/" + progress.RequiredXp;
            }

            return "Unavailable";
        }

        private static string BuildDamageText(VehicleLite vehicle, VehicleNode node)
        {
            float damageMin = vehicle != null ? vehicle.damageMin : node.damageMin;
            float damageMax = vehicle != null ? vehicle.damageMax : node.damageMax;

            if (damageMin > 0f || damageMax > 0f)
            {
                return FormatFloat(damageMin) + " - " + FormatFloat(damageMax);
            }

            return vehicle != null ? vehicle.damage.ToString() : string.Empty;
        }

        private static float CalculateAverageDps(VehicleLite vehicle, VehicleNode node)
        {
            float reloadTime = vehicle != null ? vehicle.reloadTime : 0f;
            if (reloadTime <= 0f)
            {
                return 0f;
            }

            float damageMin = vehicle != null ? vehicle.damageMin : node.damageMin;
            float damageMax = vehicle != null ? vehicle.damageMax : node.damageMax;
            float damage = vehicle != null ? vehicle.damage : 0f;
            float averageDamage = damageMin > 0f || damageMax > 0f
                ? (damageMin + damageMax) * 0.5f
                : damage;

            return averageDamage / reloadTime;
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void SetMainMenuText(TMP_Text text, string value)
        {
            SetText(text, string.IsNullOrWhiteSpace(value) ? "-" : value);
        }

        private void OnDestroy()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuySelectedVehicle);
            }

            if (researchButton != null)
            {
                researchButton.onClick.RemoveListener(ResearchSelectedVehicle);
            }
        }
    }
}
