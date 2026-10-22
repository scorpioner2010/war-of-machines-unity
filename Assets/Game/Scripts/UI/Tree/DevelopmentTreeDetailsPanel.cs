using System;
using Game.Scripts.API;
using Game.Scripts.Core.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public sealed class DevelopmentTreeDetailsPanel : MonoBehaviour
    {
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
        [SerializeField] private Button buyButton;
        [SerializeField] private Button researchButton;

        private Action<VehicleResearchProgress> _buyAction;
        private Action<VehicleResearchProgress> _researchAction;
        private VehicleResearchProgress _selectedProgress;

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(BuySelectedVehicle);
            }

            if (researchButton != null)
            {
                researchButton.onClick.AddListener(ResearchSelectedVehicle);
            }
        }

        public void Show(
            VehicleResearchProgress progress,
            Action<VehicleResearchProgress> buyAction,
            Action<VehicleResearchProgress> researchAction)
        {
            _selectedProgress = progress;
            _buyAction = buyAction;
            _researchAction = researchAction;

            if (progress == null || progress.Node == null)
            {
                Clear();
                return;
            }

            VehicleNode node = progress.Node;
            VehicleLite vehicle = progress.Vehicle;

            SetText(vehicleName, node.name);
            SetText(faction, vehicle != null && !string.IsNullOrWhiteSpace(vehicle.factionName)
                ? vehicle.factionName
                : node.factionCode);
            SetVehicleImage(node.code);
            SetLevelIcon(node.level);
            SetText(progression, BuildProgressionText(progress));

            SetText(hitPoints, vehicle != null ? vehicle.hp.ToString() : string.Empty);
            SetText(viewRange, FormatFloat(vehicle != null ? vehicle.viewRange : node.viewRange));
            SetText(turretArmor, vehicle != null ? vehicle.turretArmor : string.Empty);
            SetText(hullArmor, vehicle != null ? vehicle.hullArmor : string.Empty);

            SetText(topSpeed, FormatFloat(vehicle != null ? vehicle.speed : 0f));
            SetText(acceleration, FormatFloat(vehicle != null ? vehicle.acceleration : 0f));
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

        public void Clear()
        {
            _selectedProgress = null;
            _buyAction = null;
            _researchAction = null;

            SetText(vehicleName, string.Empty);
            SetText(faction, string.Empty);
            SetVehicleImage(null);
            SetLevelIcon(0);
            SetText(progression, string.Empty);
            SetText(hitPoints, string.Empty);
            SetText(viewRange, string.Empty);
            SetText(turretArmor, string.Empty);
            SetText(hullArmor, string.Empty);
            SetText(topSpeed, string.Empty);
            SetText(acceleration, string.Empty);
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
            if (_selectedProgress != null && _selectedProgress.Status == VehicleResearchStatus.AvailableToBuy)
            {
                _buyAction?.Invoke(_selectedProgress);
            }
        }

        private void ResearchSelectedVehicle()
        {
            if (_selectedProgress != null && _selectedProgress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                _researchAction?.Invoke(_selectedProgress);
            }
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

        private void SetVehicleImage(string vehicleCode)
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
            vehicleImage.gameObject.SetActive(hasIcon);
            vehicleImage.sprite = icon;
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static string FormatFloat(float value)
        {
            return value > 0f ? value.ToString("0.##") : string.Empty;
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
