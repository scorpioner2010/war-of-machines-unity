using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.MenuController
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager _in;

        [SerializeField] private List<MenuEntry> menus;
        [SerializeField] private List<PrefabMenuEntry> prefabMenus;
        private Dictionary<MenuType, Menu> _map;
        public static Action<MenuType> OnDisable;
        public static Action<MenuType> OnEnable;
        public static MenuType CurrentType { get; private set; }
        public static MenuType PreviousType { get; private set; }
        public static bool IsReady => _in != null && _in._map != null;
        
        private void Awake()
        {
            _in = this;
            _map = new Dictionary<MenuType, Menu>();
            
            foreach (MenuEntry e in menus)
            {
                if (e.controller == null)
                {
                    continue;
                }

                _map[e.type] = e.controller;
                e.controller.SetActive(false);
            }

            SpawnPrefabMenus();
        }

        public static bool RegisterMenu(MenuType type, Menu controller)
        {
            if (!IsReady || controller == null)
            {
                return false;
            }

            _in._map[type] = controller;
            controller.SetActive(false);
            return true;
        }

        public static void OpenMenu(MenuType type)
        {
            if (!IsReady)
            {
                return;
            }

            if (CurrentType != type)
            {
                PreviousType = CurrentType;
            }

            CurrentType = type;
            OnEnable?.Invoke(type);
            foreach (KeyValuePair<MenuType, Menu> kv in _in._map)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                if (kv.Key == type)
                {
                    kv.Value.Open();
                }
                else if (ShouldCloseMenu(type, kv.Key))
                {
                    kv.Value.CloseAsync().Forget();
                }
            }
        }

        public static void CloseMenu(MenuType type)
        {
            if (!IsReady)
            {
                return;
            }

            OnDisable?.Invoke(type);
            if (_in._map.ContainsKey(type))
            {
                _in._map[type].CloseAsync().Forget();
            }
        }

        [Serializable]
        public struct MenuEntry
        {
            public MenuType type;
            public Menu controller;
        }

        [Serializable]
        public struct PrefabMenuEntry
        {
            public MenuType type;
            public Menu prefab;
            public Transform parent;
        }

        private static bool ShouldCloseMenu(MenuType openingType, MenuType candidateType)
        {
            if (candidateType == MenuType.GameplayHUD)
            {
                if (openingType == MenuType.GameplayPause)
                {
                    return false;
                }

                if (openingType == MenuType.Settings && PreviousType == MenuType.GameplayPause)
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnPrefabMenus()
        {
            if (prefabMenus == null)
            {
                return;
            }

            for (int i = 0; i < prefabMenus.Count; i++)
            {
                PrefabMenuEntry entry = prefabMenus[i];
                if (entry.prefab == null)
                {
                    continue;
                }

                Transform parent = entry.parent != null ? entry.parent : FindDefaultPrefabMenuParent();
                Menu controller = Instantiate(entry.prefab, parent, false);
                controller.name = entry.prefab.name;

                _map[entry.type] = controller;
                controller.SetActive(false);
            }
        }

        private Transform FindDefaultPrefabMenuParent()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas.transform;
            }

            return FindDefaultMenuParent();
        }

        private Transform FindDefaultMenuParent()
        {
            if (menus != null)
            {
                for (int i = 0; i < menus.Count; i++)
                {
                    Menu controller = menus[i].controller;
                    if (controller == null || controller.MenuParent == null)
                    {
                        continue;
                    }

                    return controller.MenuParent;
                }
            }

            return transform.parent;
        }
    }
}
