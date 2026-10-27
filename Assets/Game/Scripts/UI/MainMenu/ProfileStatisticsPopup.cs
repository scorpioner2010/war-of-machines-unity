using System.Collections.Generic;
using System.Globalization;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class ProfileStatisticsPopup : MonoBehaviour
    {
        public static ProfileStatisticsPopup In { get; private set; }

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text[] generalStatValues;
        [SerializeField] private ProfileVehicleRow vehicleRowTemplate;
        [SerializeField] private RectTransform vehicleRowContent;
        [SerializeField] private ScrollRect vehicleScrollRect;
        [SerializeField] private TMP_Text vehicleName;
        [SerializeField] private TMP_Text vehicleTier;
        [SerializeField] private TMP_Text[] vehicleStatValues;
        [SerializeField] private Image vehiclePreviewPlaceholder;
        [SerializeField] private TMP_Text recordLabel;
        [SerializeField] private Image winsBar;
        [FormerlySerializedAs("lossesBar")]
        [SerializeField] private Image recordBarBackground;
        [SerializeField] private TMP_Text winsLabel;
        [SerializeField] private TMP_Text lossesLabel;
        [SerializeField] private Button closeButton;

        private readonly List<ProfileVehicleRow> _vehicleRows = new List<ProfileVehicleRow>();

        private void Awake()
        {
            In = this;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (In == this)
            {
                In = null;
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowLoading();
            ProfileServer.RequestProfileStatistics();
        }

        public void Close()
        {
            ClearVehicleRows();
            gameObject.SetActive(false);
        }

        public void ShowLoading()
        {
            SetText(title, "PROFILE // LOADING...");
            SetGeneralValues("...");
            SetVehicleValues("...");
            SetText(recordLabel, "RECORD (LOADING)");
            SetText(winsLabel, "--% WINS");
            SetText(lossesLabel, "--% LOSSES");
            SetRecordBar(0f);
            SetText(vehicleName, "LOADING PLAYER DATA");
            SetText(vehicleTier, string.Empty);
            SetPreview(null);
            ClearVehicleRows();
        }

        public void ShowUnavailable(string message)
        {
            SetText(title, "PROFILE // OFFLINE");
            SetGeneralValues("-");
            SetVehicleValues("-");
            SetText(recordLabel, "NO SERVER HISTORY");
            SetText(winsLabel, "--% WINS");
            SetText(lossesLabel, "--% LOSSES");
            SetRecordBar(0f);
            SetText(vehicleName, message);
            SetText(vehicleTier, string.Empty);
            SetPreview(null);
            ClearVehicleRows();
        }

        public void ShowError(string message)
        {
            SetText(title, "PROFILE // ERROR");
            SetGeneralValues("-");
            SetVehicleValues("-");
            SetText(recordLabel, "STATISTICS UNAVAILABLE");
            SetText(winsLabel, "--% WINS");
            SetText(lossesLabel, "--% LOSSES");
            SetRecordBar(0f);
            SetText(vehicleName, string.IsNullOrWhiteSpace(message) ? "Unable to load server statistics." : message);
            SetText(vehicleTier, string.Empty);
            SetPreview(null);
            ClearVehicleRows();
        }

        public void ApplyStatistics(PlayerStatisticsDto statistics)
        {
            if (statistics == null)
            {
                ShowError("Empty statistics response.");
                return;
            }

            SetText(title, string.IsNullOrWhiteSpace(statistics.username)
                ? "PROFILE // LIVE"
                : "PROFILE // " + statistics.username.ToUpperInvariant());
            SetGeneralValue(0, statistics.battles.ToString(CultureInfo.InvariantCulture));
            SetGeneralValue(1, statistics.wins.ToString(CultureInfo.InvariantCulture));
            SetGeneralValue(2, statistics.losses.ToString(CultureInfo.InvariantCulture));
            SetGeneralValue(3, FormatPercent(statistics.winRate));
            SetGeneralValue(4, statistics.mmr.ToString("N0", CultureInfo.InvariantCulture));

            SetText(recordLabel, "SELECT A ROBOT");
            SetText(winsLabel, "--% WINS");
            SetText(lossesLabel, "--% LOSSES");
            SetRecordBar(0f);

            ClearVehicleRows();
            VehicleStatisticsDto[] vehicles = statistics.vehicles ?? new VehicleStatisticsDto[0];
            if (vehicles.Length == 0)
            {
                SetText(vehicleName, "NO BATTLE HISTORY");
                SetText(vehicleTier, string.Empty);
                SetVehicleValues("-");
                SetPreview(null);
                return;
            }

            if (vehicleRowTemplate == null || vehicleRowContent == null)
            {
                ShowError("Profile vehicle list is not configured.");
                return;
            }

            vehicleRowTemplate.gameObject.SetActive(false);
            VehicleStatisticsDto firstVehicle = null;
            for (int index = 0; index < vehicles.Length; index++)
            {
                VehicleStatisticsDto vehicle = vehicles[index];
                if (vehicle == null)
                {
                    continue;
                }

                ProfileVehicleRow row = Instantiate(vehicleRowTemplate, vehicleRowContent, false);
                row.Setup(vehicle, SelectVehicle, firstVehicle == null);
                row.gameObject.SetActive(true);
                _vehicleRows.Add(row);
                firstVehicle ??= vehicle;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(vehicleRowContent);
            ResetScrollPosition();

            if (firstVehicle != null)
            {
                SelectVehicle(firstVehicle);
            }
        }

        private void SelectVehicle(VehicleStatisticsDto vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            for (int index = 0; index < _vehicleRows.Count; index++)
            {
                _vehicleRows[index].SetSelected(_vehicleRows[index].VehicleId == vehicle.vehicleId);
            }

            SetSelectedVehicle(vehicle);
        }

        private void SetSelectedVehicle(VehicleStatisticsDto vehicle)
        {
            SetText(vehicleName, vehicle.name);
            SetText(vehicleTier, vehicle.tier > 0
                ? "TIER " + vehicle.tier.ToString(CultureInfo.InvariantCulture)
                : string.Empty);
            SetVehicleValue(0, vehicle.battles.ToString(CultureInfo.InvariantCulture));
            SetVehicleValue(1, vehicle.wins.ToString(CultureInfo.InvariantCulture));
            SetVehicleValue(2, FormatPercent(vehicle.winRate));
            SetVehicleValue(3, FormatNumber(vehicle.averageDamage));
            SetPreview(GameResourceManager.GetIcon(vehicle.code));
            SetSelectedRecord(vehicle);
        }

        private void SetSelectedRecord(VehicleStatisticsDto vehicle)
        {
            int wins = Mathf.Max(0, vehicle.wins);
            int losses = Mathf.Max(0, vehicle.losses);
            int decisiveBattles = wins + losses;

            SetText(recordLabel, "RECORD (" + vehicle.battles.ToString(CultureInfo.InvariantCulture) + " BATTLES)");
            if (decisiveBattles == 0)
            {
                SetText(winsLabel, "0% WINS");
                SetText(lossesLabel, "0% LOSSES");
                SetRecordBar(0f);
                return;
            }

            float winsFill = wins / (float)decisiveBattles;
            int winsPercent = Mathf.Clamp(Mathf.RoundToInt(winsFill * 100f), 0, 100);
            int lossesPercent = 100 - winsPercent;
            SetText(winsLabel, FormatWholePercent(winsPercent) + " WINS");
            SetText(lossesLabel, FormatWholePercent(lossesPercent) + " LOSSES");
            SetRecordBar(winsFill);
        }

        private void ClearVehicleRows()
        {
            for (int index = 0; index < _vehicleRows.Count; index++)
            {
                ProfileVehicleRow row = _vehicleRows[index];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _vehicleRows.Clear();

            if (vehicleRowTemplate != null)
            {
                vehicleRowTemplate.gameObject.SetActive(false);
            }

            ResetScrollPosition();
        }

        private void ResetScrollPosition()
        {
            if (vehicleScrollRect != null)
            {
                vehicleScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void SetGeneralValues(string value)
        {
            if (generalStatValues == null)
            {
                return;
            }

            for (int index = 0; index < generalStatValues.Length; index++)
            {
                SetText(generalStatValues[index], value);
            }
        }

        private void SetVehicleValues(string value)
        {
            if (vehicleStatValues == null)
            {
                return;
            }

            for (int index = 0; index < vehicleStatValues.Length; index++)
            {
                SetText(vehicleStatValues[index], value);
            }
        }

        private void SetGeneralValue(int index, string value)
        {
            if (generalStatValues != null && index >= 0 && index < generalStatValues.Length)
            {
                SetText(generalStatValues[index], value);
            }
        }

        private void SetVehicleValue(int index, string value)
        {
            if (vehicleStatValues != null && index >= 0 && index < vehicleStatValues.Length)
            {
                SetText(vehicleStatValues[index], value);
            }
        }

        private void SetRecordBar(float winsFill)
        {
            if (winsBar != null)
            {
                winsBar.fillAmount = Mathf.Clamp01(winsFill);
            }

            if (recordBarBackground != null)
            {
                recordBarBackground.fillAmount = 1f;
            }
        }

        private void SetPreview(Sprite sprite)
        {
            if (vehiclePreviewPlaceholder == null)
            {
                return;
            }

            vehiclePreviewPlaceholder.sprite = sprite;
            vehiclePreviewPlaceholder.enabled = sprite != null;
        }

        private static string FormatPercent(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatWholePercent(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
