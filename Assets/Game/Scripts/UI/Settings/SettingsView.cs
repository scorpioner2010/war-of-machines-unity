using Game.Scripts.MenuController;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Settings
{
    public class SettingsView : MonoBehaviour
    {
        public GeneralTabView GeneralTab;
        public VideoTabView VideoTab;
        public SoundTabView SoundTab;
        public ControlsTabView ControlsTab;
        
        public Button GeneralTabButton;
        public Button VideoTabButton;
        public Button SoundTabButton;
        public Button ControlsTabButton;
        
        public Button ApplyButton;
        public Button BackButton;

        private SettingsController _controller;
        private SettingsModel _model;

        public enum TabType
        {
            General = 0,
            Video = 1, 
            Sounds = 2, 
            Controls = 3
        }
        
        private TabType _currentTab;

        private void Start()
        {
            _model = new SettingsModel();
            _controller = new SettingsController(_model);

            GeneralTab.Initialize(_controller);
            VideoTab.Initialize(_controller);
            SoundTab.Initialize(_controller);
            ControlsTab.Initialize(_controller);

            GeneralTab.SetData(_model);
            VideoTab.SetData(_model);
            SoundTab.SetData(_model);
            ControlsTab.SetData(_model);

            GeneralTabButton.onClick.AddListener(() => ShowTab(TabType.General));
            VideoTabButton.onClick.AddListener(() => ShowTab(TabType.Video));
            SoundTabButton.onClick.AddListener(() => ShowTab(TabType.Sounds));
            ControlsTabButton.onClick.AddListener(() => ShowTab(TabType.Controls));

            ApplyButton.onClick.AddListener(OnApplyClicked);
            BackButton.onClick.AddListener(OnBackClicked);

            ShowTab(TabType.Video);
        }

        private void OnApplyClicked()
        {
            _controller.ApplyChanges(_currentTab);
        }

        private void OnBackClicked()
        {
            if (MenuManager.PreviousType == MenuType.GameplayPause)
            {
                MenuManager.OpenMenu(MenuType.GameplayPause);
                return;
            }

            MenuManager.OpenMenu(MenuType.MainMenu);
        }
        
        private void ShowTab(TabType tab)
        {
            _currentTab = tab;

            GeneralTab.gameObject.SetActive(tab == TabType.General);
            VideoTab.gameObject.SetActive(tab == TabType.Video);
            SoundTab.gameObject.SetActive(tab == TabType.Sounds);
            ControlsTab.gameObject.SetActive(tab == TabType.Controls);
        }
    }
}
