using System.IO;
using UnityEngine;

namespace Game.Scripts.UI.Settings
{
    [System.Serializable]
    public class SettingsModel
    {
        public string Language = "English";

        public int FullScreenIndex = 0;
        public int ResolutionIndex = 0;
        public int QualityIndex = 2;
        public float Gamma = 1.0f;

        public float UiVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public float SfxVolume = 1.0f;

        public float MouseSensitivity = 1.0f;
        public float GameplayMouseSensitivity = ClientGameplaySettings.DefaultGameplayMouseSensitivity;
        public float SniperMouseSensitivity = ClientGameplaySettings.DefaultSniperMouseSensitivity;
        public bool InvertXAxis = false;
        public bool InvertYAxis = false;
        public string WalkKey = "WASD";
        public string AttackKey = "RMB";
        public bool ServerCrosshairEnabled = false;

        private static string configFileName = "settings.config";

        private static string ConfigFilePath
        {
            get
            {
                string exeFolder = Directory.GetParent(Application.dataPath)?.FullName;
                return Path.Combine(exeFolder, configFileName);
            }
        }
        
        public void Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                JsonUtility.FromJsonOverwrite(json, this);
            }
            else
            {
            }

            ValidateControls();
        }
        
        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(ConfigFilePath, json);
        }

        public void SaveGeneral()
        {
            Save();
        }

        public void SaveVideo()
        {
            Save();
        }

        public void SaveAudio()
        {
            Save();
        }

        public void SaveControls()
        {
            ValidateControls();
            Save();
        }

        private void ValidateControls()
        {
            GameplayMouseSensitivity = ClientGameplaySettings.ClampMouseSensitivity(
                GameplayMouseSensitivity,
                ClientGameplaySettings.DefaultGameplayMouseSensitivity);
            SniperMouseSensitivity = ClientGameplaySettings.ClampMouseSensitivity(
                SniperMouseSensitivity,
                ClientGameplaySettings.DefaultSniperMouseSensitivity);
        }
    }
}
