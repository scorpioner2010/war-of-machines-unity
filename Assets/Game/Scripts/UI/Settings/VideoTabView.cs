using System;
using System.Collections.Generic;
using Game.Scripts.Client;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class VideoTabView : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Dropdown FullScreenDropdown;
        public TMP_Dropdown ResolutionDropdown;
        public TMP_Dropdown QualityDropdown;
        public TMP_Dropdown FrameRateDropdown;
        public Toggle VerticalSyncToggle;
        public Slider GammaSlider;

        private SettingsController _controller;
        private bool _suppressEvents;
        private VideoSettingsState _appliedSettings;
        private bool _hasAppliedSettings;

        public event Action<bool> PendingChangesChanged;

        public bool HasPendingChanges
        {
            get
            {
                return _hasAppliedSettings && !GetCurrentSettings().Equals(_appliedSettings);
            }
        }

        public void Initialize(SettingsController controller)
        {
            _controller = controller;

            FullScreenDropdown.ClearOptions();
            List<string> fullScreenType = new List<string>
            {
                "FullScreen",
                "Windowed",
            };
            FullScreenDropdown.AddOptions(fullScreenType);

            VideoResolutionOptions.Refresh();
            ResolutionDropdown.ClearOptions();
            List<string> screenResolution = new List<string>(VideoResolutionOptions.Count);
            for (int i = 0; i < VideoResolutionOptions.Count; i++)
            {
                screenResolution.Add(VideoResolutionOptions.GetLabel(i));
            }
            
            ResolutionDropdown.AddOptions(screenResolution);
            
            QualityDropdown.ClearOptions();
            List<string> quality = new List<string>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                quality.Add(QualitySettings.names[i]);
            }
            
            QualityDropdown.AddOptions(quality);

            if (FrameRateDropdown != null)
            {
                FrameRateDropdown.ClearOptions();
                List<string> frameRateOptions = new List<string>(ClientFramePacingSettings.SupportedTargetFrameRateCount);
                for (int i = 0; i < ClientFramePacingSettings.SupportedTargetFrameRateCount; i++)
                {
                    frameRateOptions.Add(ClientFramePacingSettings.GetSupportedTargetFrameRate(i) + " FPS");
                }

                FrameRateDropdown.AddOptions(frameRateOptions);
            }
            
            FullScreenDropdown.onValueChanged.RemoveListener(OnFullScreenChanged);
            FullScreenDropdown.onValueChanged.AddListener(OnFullScreenChanged);
            ResolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            ResolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            QualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            QualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            GammaSlider.onValueChanged.RemoveListener(OnGammaChanged);
            GammaSlider.onValueChanged.AddListener(OnGammaChanged);
            if (FrameRateDropdown != null)
            {
                FrameRateDropdown.onValueChanged.RemoveListener(OnFrameRateChanged);
                FrameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
            }

            if (VerticalSyncToggle != null)
            {
                VerticalSyncToggle.onValueChanged.RemoveListener(OnVerticalSyncChanged);
                VerticalSyncToggle.onValueChanged.AddListener(OnVerticalSyncChanged);
            }
        }

        public void SetData(SettingsModel model)
        {
            _suppressEvents = true;

            int fullScreenIndex = model != null ? model.FullScreenIndex : 1;
            if (fullScreenIndex == 1)
            {
                FullScreenDropdown.SetValueWithoutNotify(0);
            }

            if (fullScreenIndex == 3)
            {
                FullScreenDropdown.SetValueWithoutNotify(1);
            }

            if (ResolutionDropdown != null)
            {
                ResolutionDropdown.SetValueWithoutNotify(VideoResolutionOptions.ClampIndex(model != null ? model.ResolutionIndex : 0));
                ResolutionDropdown.RefreshShownValue();
            }

            if (QualityDropdown != null)
            {
                int qualityIndex = model != null ? model.QualityIndex : QualitySettings.GetQualityLevel();
                QualityDropdown.SetValueWithoutNotify(Mathf.Clamp(qualityIndex, 0, QualityDropdown.options.Count - 1));
                QualityDropdown.RefreshShownValue();
            }

            if (GammaSlider != null)
            {
                GammaSlider.SetValueWithoutNotify(model != null ? model.Gamma : 1f);
            }

            int targetFrameRate = model != null
                ? model.TargetFrameRate
                : ClientFramePacingSettings.DefaultTargetFrameRate;
            bool verticalSyncEnabled = model != null && model.VerticalSyncEnabled;

            if (FrameRateDropdown != null)
            {
                FrameRateDropdown.SetValueWithoutNotify(GetFrameRateOptionIndex(targetFrameRate));
                FrameRateDropdown.RefreshShownValue();
            }

            if (VerticalSyncToggle != null)
            {
                VerticalSyncToggle.SetIsOnWithoutNotify(verticalSyncEnabled);
            }

            SetFrameRateDropdownInteractable(!verticalSyncEnabled);
            _suppressEvents = false;

            _appliedSettings = GetCurrentSettings();
            _hasAppliedSettings = true;
            NotifyPendingChangesChanged();
        }

        public void MarkChangesApplied()
        {
            _appliedSettings = GetCurrentSettings();
            _hasAppliedSettings = true;
            NotifyPendingChangesChanged();
        }
        
        private void OnFullScreenChanged(int index)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            if (index == 0)
            {
                _controller.HandleFullScreenChanged(1);
            }

            if (index == 1)
            {
                _controller.HandleFullScreenChanged(3);
            }

            NotifyPendingChangesChanged();
        }

        private void OnResolutionChanged(int index)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            _controller.HandleResolutionChanged(index);
            NotifyPendingChangesChanged();
        }

        private void OnQualityChanged(int index)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            _controller.HandleQualityChanged(index);
            NotifyPendingChangesChanged();
        }

        private void OnGammaChanged(float value)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            _controller.HandleGammaChanged(value);
            NotifyPendingChangesChanged();
        }

        private void OnFrameRateChanged(int index)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            _controller.HandleTargetFrameRateChanged(GetFrameRateOptionValue(index));
            NotifyPendingChangesChanged();
        }

        private void OnVerticalSyncChanged(bool isOn)
        {
            if (_suppressEvents || _controller == null)
            {
                return;
            }

            SetFrameRateDropdownInteractable(!isOn);
            _controller.HandleVerticalSyncChanged(isOn);
            NotifyPendingChangesChanged();
        }

        private void SetFrameRateDropdownInteractable(bool interactable)
        {
            if (FrameRateDropdown != null)
            {
                FrameRateDropdown.interactable = interactable;
            }
        }

        private void NotifyPendingChangesChanged()
        {
            PendingChangesChanged?.Invoke(HasPendingChanges);
        }

        private VideoSettingsState GetCurrentSettings()
        {
            int fullScreenIndex = FullScreenDropdown != null && FullScreenDropdown.value == 1 ? 3 : 1;
            int resolutionIndex = ResolutionDropdown != null
                ? VideoResolutionOptions.ClampIndex(ResolutionDropdown.value)
                : 0;
            int qualityIndex = QualityDropdown != null ? QualityDropdown.value : 0;
            int targetFrameRate = FrameRateDropdown != null
                ? GetFrameRateOptionValue(FrameRateDropdown.value)
                : ClientFramePacingSettings.DefaultTargetFrameRate;
            float gamma = GammaSlider != null ? GammaSlider.value : 1f;
            bool verticalSyncEnabled = VerticalSyncToggle != null && VerticalSyncToggle.isOn;

            return new VideoSettingsState(
                fullScreenIndex,
                resolutionIndex,
                qualityIndex,
                targetFrameRate,
                gamma,
                verticalSyncEnabled);
        }

        private struct VideoSettingsState
        {
            private readonly int _fullScreenIndex;
            private readonly int _resolutionIndex;
            private readonly int _qualityIndex;
            private readonly int _targetFrameRate;
            private readonly float _gamma;
            private readonly bool _verticalSyncEnabled;

            public VideoSettingsState(
                int fullScreenIndex,
                int resolutionIndex,
                int qualityIndex,
                int targetFrameRate,
                float gamma,
                bool verticalSyncEnabled)
            {
                _fullScreenIndex = fullScreenIndex;
                _resolutionIndex = resolutionIndex;
                _qualityIndex = qualityIndex;
                _targetFrameRate = targetFrameRate;
                _gamma = gamma;
                _verticalSyncEnabled = verticalSyncEnabled;
            }

            public bool Equals(VideoSettingsState other)
            {
                return _fullScreenIndex == other._fullScreenIndex
                    && _resolutionIndex == other._resolutionIndex
                    && _qualityIndex == other._qualityIndex
                    && _targetFrameRate == other._targetFrameRate
                    && Mathf.Approximately(_gamma, other._gamma)
                    && _verticalSyncEnabled == other._verticalSyncEnabled;
            }
        }

        private static int GetFrameRateOptionIndex(int targetFrameRate)
        {
            int safeTargetFrameRate = ClientFramePacingSettings.ClampTargetFrameRate(targetFrameRate);
            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < ClientFramePacingSettings.SupportedTargetFrameRateCount; i++)
            {
                int option = ClientFramePacingSettings.GetSupportedTargetFrameRate(i);
                int distance = Mathf.Abs(option - safeTargetFrameRate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static int GetFrameRateOptionValue(int index)
        {
            return ClientFramePacingSettings.GetSupportedTargetFrameRate(index);
        }

    }

    internal static class VideoResolutionOptions
    {
        private static readonly int[] AllowedWidths =
        {
            1280,
            1600,
            1920,
            2560,
            3840
        };

        private static readonly int[] AllowedHeights =
        {
            720,
            900,
            1080,
            1440,
            2160
        };

        private static readonly int[] AllowedRefreshRates =
        {
            60,
            144,
            240
        };

        private static readonly List<Resolution> Resolutions = new List<Resolution>(16);
        private static readonly List<string> Labels = new List<string>(16);

        public static int Count
        {
            get
            {
                return Resolutions.Count;
            }
        }

        public static void Refresh()
        {
            Resolutions.Clear();
            Labels.Clear();

            Resolution[] available = Screen.resolutions;
            for (int sizeIndex = 0; sizeIndex < AllowedWidths.Length; sizeIndex++)
            {
                int width = AllowedWidths[sizeIndex];
                int height = AllowedHeights[sizeIndex];
                for (int refreshIndex = 0; refreshIndex < AllowedRefreshRates.Length; refreshIndex++)
                {
                    if (TryFindMode(available, width, height, AllowedRefreshRates[refreshIndex], out Resolution resolution))
                    {
                        AddMode(resolution);
                    }
                }
            }

            if (Resolutions.Count == 0)
            {
                AddMode(Screen.currentResolution);
            }
        }

        public static int ClampIndex(int index)
        {
            if (Resolutions.Count == 0)
            {
                Refresh();
            }

            if (Resolutions.Count == 0)
            {
                return 0;
            }

            return Mathf.Clamp(index, 0, Resolutions.Count - 1);
        }

        public static Resolution GetResolution(int index)
        {
            if (Resolutions.Count == 0)
            {
                Refresh();
            }

            if (Resolutions.Count == 0)
            {
                return Screen.currentResolution;
            }

            return Resolutions[ClampIndex(index)];
        }

        public static string GetLabel(int index)
        {
            if (Labels.Count == 0)
            {
                Refresh();
            }

            if (Labels.Count == 0)
            {
                return "Current resolution";
            }

            return Labels[ClampIndex(index)];
        }

        private static bool TryFindMode(Resolution[] available, int width, int height, int refreshRate, out Resolution selected)
        {
            selected = default;
            int bestDistance = int.MaxValue;
            bool found = false;
            for (int i = 0; i < available.Length; i++)
            {
                Resolution candidate = available[i];
                if (candidate.width != width || candidate.height != height)
                {
                    continue;
                }

                int candidateRefreshRate = GetRefreshRate(candidate);
                int distance = Mathf.Abs(candidateRefreshRate - refreshRate);
                if (distance > 1 || distance >= bestDistance)
                {
                    continue;
                }

                selected = candidate;
                bestDistance = distance;
                found = true;
            }

            return found;
        }

        private static void AddMode(Resolution resolution)
        {
            if (resolution.width <= 0 || resolution.height <= 0)
            {
                return;
            }

            int refreshRate = GetRefreshRate(resolution);
            for (int i = 0; i < Resolutions.Count; i++)
            {
                Resolution existing = Resolutions[i];
                if (existing.width == resolution.width
                    && existing.height == resolution.height
                    && GetRefreshRate(existing) == refreshRate)
                {
                    return;
                }
            }

            Resolutions.Add(resolution);
            Labels.Add(resolution.width + " x " + resolution.height + " @ " + refreshRate + " Hz");
        }

        private static int GetRefreshRate(Resolution resolution)
        {
            RefreshRate refreshRate = resolution.refreshRateRatio;
            if (refreshRate.denominator > 0)
            {
                return Mathf.RoundToInt((float)((double)refreshRate.numerator / refreshRate.denominator));
            }

#pragma warning disable CS0618
            return resolution.refreshRate;
#pragma warning restore CS0618
        }
    }
}
