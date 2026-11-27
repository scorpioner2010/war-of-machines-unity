using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAV.Scripts.UI.Helpers
{
    public class StandardLoadingOverlay : MonoBehaviour
    {
        private static StandardLoadingOverlay _in;
        private static readonly HashSet<LoadingRequest> ActiveRequests = new HashSet<LoadingRequest>();
        private static bool _sceneLoadingSuppressed;

        public static bool IsOpen { get; private set; }

        public GameObject standardLoading;

        public sealed class LoadingRequest : IDisposable
        {
            private bool _ended;

            internal LoadingRequest()
            {
            }

            public bool IsActive => !_ended;

            public void End()
            {
                if (_ended)
                {
                    return;
                }

                _ended = true;
                EndRequest(this);
            }

            public void Dispose()
            {
                End();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            ActiveRequests.Clear();
            _in = null;
            _sceneLoadingSuppressed = false;
            IsOpen = false;
        }

        private void Awake()
        {
            _in = this;
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (_in == this)
            {
                _in = null;
                IsOpen = ActiveRequests.Count > 0 && !_sceneLoadingSuppressed;
            }
        }

        public static LoadingRequest BeginRequest()
        {
            LoadingRequest request = new LoadingRequest();
            ActiveRequests.Add(request);
            RefreshVisibility();
            return request;
        }

        public static void SetSceneLoadingSuppressed(bool suppressed)
        {
            _sceneLoadingSuppressed = suppressed;
            RefreshVisibility();
        }

        private static void EndRequest(LoadingRequest request)
        {
            ActiveRequests.Remove(request);
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
            bool active = ActiveRequests.Count > 0 && !_sceneLoadingSuppressed;
            IsOpen = active;
            if (_in == null || _in.standardLoading == null)
            {
                return;
            }

            _in.standardLoading.SetActive(active);
            if (active)
            {
                _in.standardLoading.transform.SetAsLastSibling();
            }
        }
    }
}
