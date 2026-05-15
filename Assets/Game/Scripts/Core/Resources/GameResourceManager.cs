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
            return _instance.registry.GetPrefab(code);
        }

        public static Sprite GetIcon(string code)
        {
            return _instance.registry.GetIcon(code);
        }
    }
}
