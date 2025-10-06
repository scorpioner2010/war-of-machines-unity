using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.MenuController
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager _in;

        [SerializeField] private List<MenuEntry> menus;
        private Dictionary<MenuType, Menu> _map;

        private void Awake()
        {
            _in = this;
            
            _map = new Dictionary<MenuType, Menu>();
            
            foreach (MenuEntry e in menus)
            {
                _map[e.type] = e.controller;
                e.controller.SetActive(false);
            }
        }

        public static void OpenMenu(MenuType type)
        {
            foreach (KeyValuePair<MenuType, Menu> kv in _in._map)
            {
                if (kv.Key == type)
                {
                    kv.Value.Open();
                }
                else
                {
                    kv.Value.CloseAsync().Forget();
                }
            }
        }

        public static void CloseMenu(MenuType type)
        {
            if (_in._map.ContainsKey(type))
            {
                _in._map[type].CloseAsync().Forget();
            }
        }

        [System.Serializable]
        public struct MenuEntry
        {
            public MenuType type;
            public Menu controller;
        }
    }
}