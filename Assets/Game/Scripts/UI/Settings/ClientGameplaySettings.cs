using System;
using CartoonFX;
using UnityEngine;

namespace Game.Scripts.UI.Settings
{
    public static class ClientGameplaySettings
    {
        private const string ServerCrosshairKey = "Gameplay.ServerCrosshairEnabled";
        private const string ReticleOpacityKey = "Gameplay.ReticleOpacity";
        private const string GameplayMouseSensitivityKey = "Gameplay.MouseSensitivity";
        private const string SniperMouseSensitivityKey = "Gameplay.SniperMouseSensitivity";

        public const float DefaultGameplayMouseSensitivity = 0.7f;
        public const float DefaultSniperMouseSensitivity = 0.3f;
        public const float DefaultReticleOpacity = 1f;
        public const float MinMouseSensitivity = 0.05f;
        public const float MaxMouseSensitivity = 2f;

        public static event Action<bool> ServerCrosshairChanged;
        public static event Action<float> ReticleOpacityChanged;
        public static event Action<bool> CameraShakeChanged;

        private static bool _cameraShakeEnabled = true;
        private static bool? _cameraShakeOverride;

        public static bool ServerCrosshairEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(ServerCrosshairKey, 0) == 1;
            }
        }

        public static float ReticleOpacity
        {
            get
            {
                return ClampReticleOpacity(PlayerPrefs.GetFloat(ReticleOpacityKey, DefaultReticleOpacity));
            }
        }

        public static bool CameraShakeEnabled
        {
            get
            {
                if (_cameraShakeOverride.HasValue)
                {
                    return _cameraShakeOverride.Value;
                }

                return _cameraShakeEnabled;
            }
        }

        public static float GameplayMouseSensitivity
        {
            get
            {
                return ClampMouseSensitivity(
                    PlayerPrefs.GetFloat(GameplayMouseSensitivityKey, DefaultGameplayMouseSensitivity),
                    DefaultGameplayMouseSensitivity);
            }
        }

        public static float SniperMouseSensitivity
        {
            get
            {
                return ClampMouseSensitivity(
                    PlayerPrefs.GetFloat(SniperMouseSensitivityKey, DefaultSniperMouseSensitivity),
                    DefaultSniperMouseSensitivity);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyStoredCameraShakePreference()
        {
            SetCameraShakeEnabled(SettingsModel.LoadStoredCameraShakeEnabled(), false);
        }

        public static void SetServerCrosshairEnabled(bool enabled)
        {
            SetServerCrosshairEnabled(enabled, true);
        }

        public static void SetServerCrosshairEnabled(bool enabled, bool notify)
        {
            int value = enabled ? 1 : 0;
            if (PlayerPrefs.GetInt(ServerCrosshairKey, 0) == value)
            {
                return;
            }

            PlayerPrefs.SetInt(ServerCrosshairKey, value);
            PlayerPrefs.Save();

            if (notify)
            {
                ServerCrosshairChanged?.Invoke(enabled);
            }
        }

        public static void SetReticleOpacity(float value)
        {
            SetReticleOpacity(value, true);
        }

        public static void SetReticleOpacity(float value, bool notify)
        {
            float clampedValue = ClampReticleOpacity(value);
            float previousValue = ReticleOpacity;
            if (Mathf.Approximately(previousValue, clampedValue))
            {
                return;
            }

            PlayerPrefs.SetFloat(ReticleOpacityKey, clampedValue);
            PlayerPrefs.Save();

            if (notify)
            {
                ReticleOpacityChanged?.Invoke(clampedValue);
            }
        }

        public static void SetCameraShakeEnabled(bool enabled)
        {
            SetCameraShakeEnabled(enabled, true);
        }

        public static void SetCameraShakeEnabled(bool enabled, bool notify)
        {
            bool changed = _cameraShakeEnabled != enabled;
            _cameraShakeEnabled = enabled;

            ApplyCameraShakePreference(CameraShakeEnabled);

            if (changed && notify)
            {
                CameraShakeChanged?.Invoke(enabled);
            }
        }

        public static void SetCameraShakeOverride(bool? enabled)
        {
            _cameraShakeOverride = enabled;
            ApplyCameraShakePreference(CameraShakeEnabled);
        }

        public static void SetGameplayMouseSensitivity(float value)
        {
            PlayerPrefs.SetFloat(
                GameplayMouseSensitivityKey,
                ClampMouseSensitivity(value, DefaultGameplayMouseSensitivity));
            PlayerPrefs.Save();
        }

        public static void SetSniperMouseSensitivity(float value)
        {
            PlayerPrefs.SetFloat(
                SniperMouseSensitivityKey,
                ClampMouseSensitivity(value, DefaultSniperMouseSensitivity));
            PlayerPrefs.Save();
        }

        public static void SetMouseSensitivities(float gameplayValue, float sniperValue, bool save)
        {
            PlayerPrefs.SetFloat(
                GameplayMouseSensitivityKey,
                ClampMouseSensitivity(gameplayValue, DefaultGameplayMouseSensitivity));
            PlayerPrefs.SetFloat(
                SniperMouseSensitivityKey,
                ClampMouseSensitivity(sniperValue, DefaultSniperMouseSensitivity));

            if (save)
            {
                PlayerPrefs.Save();
            }
        }

        public static float ClampMouseSensitivity(float value)
        {
            return ClampMouseSensitivity(value, DefaultGameplayMouseSensitivity);
        }

        public static float ClampMouseSensitivity(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return Mathf.Clamp(fallback, MinMouseSensitivity, MaxMouseSensitivity);
            }

            return Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity);
        }

        public static float ClampReticleOpacity(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultReticleOpacity;
            }

            return Mathf.Clamp01(value);
        }

        private static void ApplyCameraShakePreference(bool enabled)
        {
            CFXR_Effect.GlobalDisableCameraShake = !enabled;
        }
    }
}
