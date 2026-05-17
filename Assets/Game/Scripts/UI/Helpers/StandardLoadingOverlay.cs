using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.UI.Helpers
{
    public class StandardLoadingOverlay : MonoBehaviour
    {
        private static StandardLoadingOverlay _in;
        public static bool IsOpen { get; private set; }
        
        public GameObject standardLoading;
        
        private void Awake()
        {
            _in = this;
            Hide();
        }
        
        public static void Show()
        {
            SetStandardLoadingActive(true);
        }

        public static void Hide()
        {
            SetStandardLoadingActive(false);
        }

        public static async UniTask WaitLoading()
        {
            while (IsOpen)
            {
                await UniTask.DelayFrame(1);
            }
        }

        private static void SetStandardLoadingActive(bool active)
        {
            if (_in == null || _in.standardLoading == null)
            {
                IsOpen = false;
                return;
            }

            _in.standardLoading.SetActive(active);
            if (active)
            {
                _in.standardLoading.transform.SetAsLastSibling();
            }

            IsOpen = active;
        }
    }
}
