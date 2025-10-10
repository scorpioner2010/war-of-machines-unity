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

        // The controller is a plain C# class (not a MonoBehaviour).
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
            // 1) Create the Model and Controller
            _model = new SettingsModel();
            _controller = new SettingsController(_model);

            // (Optional) If you have a post-processing volume for gamma
            // _controller.SetPostProcessingVolume(myVolume);

            // 2) Initialize each tab with the same controller
            GeneralTab.Initialize(_controller);
            VideoTab.Initialize(_controller);
            SoundTab.Initialize(_controller);
            ControlsTab.Initialize(_controller);

            // 3) Set initial data in each tab from the model
            GeneralTab.SetData(_model);
            VideoTab.SetData(_model);
            SoundTab.SetData(_model);
            ControlsTab.SetData(_model);

            // 4) Set up tab button listeners
            GeneralTabButton.onClick.AddListener(() => ShowTab(TabType.General));
            VideoTabButton.onClick.AddListener(() => ShowTab(TabType.Video));
            SoundTabButton.onClick.AddListener(() => ShowTab(TabType.Sounds));
            ControlsTabButton.onClick.AddListener(() => ShowTab(TabType.Controls));

            // 5) Assign Apply/Back button listeners
            ApplyButton.onClick.AddListener(OnApplyClicked);
            BackButton.onClick.AddListener(OnBackClicked);

            // Show the first tab by default
            ShowTab(TabType.Video);
        }

        private void OnApplyClicked()
        {
            _controller.ApplyChanges(_currentTab);
            //Debug.Log("Settings applied and saved.");
        }

        private void OnBackClicked()
        {
            // Hide or close the settings menu
            //gameObject.SetActive(false);
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