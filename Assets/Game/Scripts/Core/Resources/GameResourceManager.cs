using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Core.Resources
{
    public class GameResourceManager : MonoBehaviour
    {
        private static GameResourceManager _instance;
        public RobotRegistry registry;

        private void Awake()
        {
            _instance = this;
        }

        public static VehicleRoot GetPrefab(string code)
        {
            if (_instance == null || _instance.registry == null)
            {
                return null;
            }

            return _instance.registry.GetPrefab(code);
        }

        public static string GetFirstVehicleCode()
        {
            if (_instance == null || _instance.registry == null)
            {
                return string.Empty;
            }

            return _instance.registry.GetFirstCode();
        }

        public static Sprite GetIcon(string code)
        {
            if (_instance == null || _instance.registry == null)
            {
                return null;
            }

            return _instance.registry.GetIcon(code);
        }
    }
}
