using Game.Scripts.MenuController;
using UnityEngine;

namespace Game.Scripts.UI
{
    public sealed class RightInfoPanelHost : MonoBehaviour
    {
        [SerializeField] private RightInfoPanel rightInfoPanel;

        private void Awake()
        {
            if (rightInfoPanel == null)
            {
                return;
            }

            rightInfoPanel.Initialize();
            MenuManager.OnEnable += HandleMenuEnabled;
            HandleMenuEnabled(MenuManager.CurrentType);
        }

        private void OnDestroy()
        {
            MenuManager.OnEnable -= HandleMenuEnabled;
        }

        private void HandleMenuEnabled(MenuType type)
        {
            rightInfoPanel?.ApplyMenuContext(type);
        }
    }
}
