using System.IO;
using UnityEngine;

namespace Game.Scripts.UI.Settings
{
    [System.Serializable]
    public class SettingsModel
    {
        // ------------------ GENERAL ------------------
        public string Language = "English";

        // ------------------ VIDEO --------------------
        public int FullScreenIndex = 0;
        public int ResolutionIndex = 0;   // Наприклад, 0 => 1920x1080, 1 => 1280x720, тощо.
        public int QualityIndex = 2;      // Наприклад, 0 => Low, 1 => Medium, 2 => High.
        public float Gamma = 1.0f;

        // ------------------ SOUND --------------------
        public float UiVolume = 1.0f;
        public float MusicVolume = 1.0f;
        public float SfxVolume = 1.0f;

        // ------------------ CONTROLS -----------------
        public float MouseSensitivity = 1.0f;
        public bool InvertXAxis = false;
        public bool InvertYAxis = false;
        public string WalkKey = "WASD";
        public string AttackKey = "RMB";

        // Ім'я файлу налаштувань.
        private static string configFileName = "settings.config";

        // Отримуємо шлях до файлу: для білд-версії exe знаходиться в папці, що на рівні з exe.
        private static string ConfigFilePath
        {
            get
            {
                // Для standalone збірки Application.dataPath повертає шлях до папки *_Data, тому беремо батьківську директорію
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
                // Файл не знайдено, використовується стандартне значення.
            }
        }
        
        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(ConfigFilePath, json);
            UnityEngine.Debug.Log("Settings applied and saved to file: " + ConfigFilePath);
        }

        // Якщо потрібно зберігати окремі розділи, можна викликати Save() після оновлення відповідних полів.

        public void SaveGeneral()
        {
            // Оновлюємо налаштування General і зберігаємо всі налаштування.
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
            Save();
        }
    }
}
