using System;
using UnityEngine;

namespace Game.Scripts.UI.Settings
{
    public static class ClientGameplaySettings
    {
        private const string ServerCrosshairKey = "Gameplay.ServerCrosshairEnabled";

        public static event Action<bool> ServerCrosshairChanged;

        public static bool ServerCrosshairEnabled
        {
            get
            {
                return PlayerPrefs.GetInt(ServerCrosshairKey, 0) == 1;
            }
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
    }
}
