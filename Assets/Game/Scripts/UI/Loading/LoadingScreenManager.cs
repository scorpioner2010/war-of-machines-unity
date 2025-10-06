using Cysharp.Threading.Tasks;
using FishNet.Managing.Scened;
using FishNet.Object;
using Game.Scripts.MenuController;
using Game.Scripts.UI.MainMenu;

namespace Game.Scripts.UI.Loading 
{
    public class LoadingScreenManager : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            SceneManager.OnLoadEnd += OnGameplayLoadEnd;
            SceneManager.OnLoadStart += OnGameplayLoadStart;
        }
        
        private void OnGameplayLoadStart(SceneLoadStartEventArgs obj)
        {
            ShowLoading();
        }

        private async void OnGameplayLoadEnd(SceneLoadEndEventArgs obj)
        {
            RobotView.Despawn();
            HideLoading();
            await UniTask.Delay(1000);
            MainMenu.MainMenu.In.SetActive(false);
        }

        public static void ShowLoading()
        {
            MenuManager.OpenMenu(MenuType.LoadScreen);
        }

        public static async void HideLoading()
        {
            await UniTask.Delay(1000);
            MenuManager.CloseMenu(MenuType.LoadScreen);
        }
    }
}