using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.UI.Helpers
{
    public class Loading : MonoBehaviour
    {
        private static Loading _in;
        public static bool IsOpen { get; private set; }
        
        public GameObject standardLoading;
        
        private void Awake()
        {
            _in = this;
            Hide();
        }
        
        public static void Show()
        {
            _in.standardLoading.SetActive(true);
            IsOpen = true;
        }

        public static void Hide()
        {
            _in.standardLoading.SetActive(false);
            IsOpen = false;
        }

        public static async UniTask WaitLoading()
        {
            while (IsOpen)
            {
                await UniTask.DelayFrame(1);
            }
        }
    }
}