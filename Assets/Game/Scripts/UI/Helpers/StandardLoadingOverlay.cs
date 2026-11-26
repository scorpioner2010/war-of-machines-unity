using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAV.Scripts.UI.Helpers
{
    public class StandardLoadingOverlay : MonoBehaviour
    {
        private static StandardLoadingOverlay _in;
        private static int _pendingRequestCount;
        private static bool _manuallyVisible;

        public static bool IsOpen { get; private set; }
        
        public GameObject standardLoading;
        
        private void Awake()
        {
            _in = this;
            _manuallyVisible = false;
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (_in == this)
            {
                _in = null;
                IsOpen = false;
            }
        }
        
        public static void Show()
        {
            _manuallyVisible = true;
            RefreshVisibility();
        }

        public static void Hide()
        {
            _manuallyVisible = false;
            RefreshVisibility();
        }

        public static void BeginRequest()
        {
            _pendingRequestCount++;
            RefreshVisibility();
        }

        public static void EndRequest()
        {
            if (_pendingRequestCount > 0)
            {
                _pendingRequestCount--;
            }

            RefreshVisibility();
        }

        public static async UniTask WaitLoading()
        {
            while (IsOpen)
            {
                await UniTask.DelayFrame(1);
            }
        }

        private static void RefreshVisibility()
        {
            bool active = _manuallyVisible || _pendingRequestCount > 0;
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
