using System;
#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
#endif

namespace Game.Scripts.Diagnostics
{
    public sealed class UnityFrameProfilerRecorder : IDisposable
    {
#if UNITY_2020_2_OR_NEWER
        private readonly bool _enabled;
        private readonly bool _enableGcAllocRecorder;
        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _renderThread;
        private ProfilerRecorder _gfxWaitForPresent;
        private ProfilerRecorder _gfxWaitForPresentOnGfxThread;
        private ProfilerRecorder _scriptRunBehaviourUpdate;
        private ProfilerRecorder _behaviourUpdate;
        private ProfilerRecorder _scriptRunBehaviourLateUpdate;
        private ProfilerRecorder _scriptRunBehaviourFixedUpdate;
        private ProfilerRecorder _gcAlloc;
        private ProfilerRecorder _cameraRender;
        private ProfilerRecorder _uiRendering;
        private ProfilerRecorder _canvasBuildBatch;

        public UnityFrameProfilerRecorder(DiagnosticsConfig config)
        {
            _enabled = config != null && config.EnableUnityProfilerRecorders;
            _enableGcAllocRecorder = _enabled && config.EnableUnityGcAllocRecorder;
            if (!_enabled)
            {
                return;
            }

            _mainThread = Start(ProfilerCategory.Internal, "Main Thread");
            _renderThread = Start(ProfilerCategory.Internal, "Render Thread");
            _gfxWaitForPresent = Start(ProfilerCategory.Render, "Gfx.WaitForPresent");
            _gfxWaitForPresentOnGfxThread = Start(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
            _scriptRunBehaviourUpdate = Start(ProfilerCategory.Scripts, "ScriptRunBehaviourUpdate");
            _behaviourUpdate = Start(ProfilerCategory.Scripts, "BehaviourUpdate");
            _scriptRunBehaviourLateUpdate = Start(ProfilerCategory.Scripts, "ScriptRunBehaviourLateUpdate");
            _scriptRunBehaviourFixedUpdate = Start(ProfilerCategory.Scripts, "FixedUpdate.ScriptRunBehaviourFixedUpdate");
            _cameraRender = Start(ProfilerCategory.Render, "Camera.Render");
            _uiRendering = Start(ProfilerCategory.Render, "UI.Rendering");
            _canvasBuildBatch = Start(ProfilerCategory.Render, "Canvas.BuildBatch");
            if (_enableGcAllocRecorder)
            {
                _gcAlloc = Start(ProfilerCategory.Memory, "GC.Alloc");
            }
        }

        public void Collect(DiagnosticsClientMetrics metrics)
        {
            if (!_enabled || metrics == null)
            {
                return;
            }

            metrics.MainThreadMs = ReadMilliseconds(_mainThread);
            metrics.RenderThreadMs = ReadMilliseconds(_renderThread);
            metrics.GfxWaitForPresentMs = FirstMilliseconds(_gfxWaitForPresent, _gfxWaitForPresentOnGfxThread);
            metrics.ScriptUpdateMs = ReadMilliseconds(_scriptRunBehaviourUpdate);
            metrics.BehaviourUpdateMs = FirstMilliseconds(_behaviourUpdate, _scriptRunBehaviourUpdate);
            metrics.LateUpdateMs = ReadMilliseconds(_scriptRunBehaviourLateUpdate);
            metrics.FixedUpdateMs = ReadMilliseconds(_scriptRunBehaviourFixedUpdate);
            metrics.CameraRenderMs = ReadMilliseconds(_cameraRender);
            metrics.UiRenderMs = FirstMilliseconds(_uiRendering, _canvasBuildBatch);
            metrics.GcAllocatedBytesInFrame = _enableGcAllocRecorder ? ReadBytes(_gcAlloc) : null;
        }

        private static ProfilerRecorder Start(ProfilerCategory category, string markerName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, markerName, 1, ProfilerRecorderOptions.SumAllSamplesInFrame);
            }
            catch
            {
                return default;
            }
        }

        private static double? FirstMilliseconds(ProfilerRecorder first, ProfilerRecorder second)
        {
            double? value = ReadMilliseconds(first);
            return value.HasValue ? value : ReadMilliseconds(second);
        }

        private static double? ReadMilliseconds(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count <= 0)
            {
                return null;
            }

            long value = recorder.LastValue;
            if (value <= 0)
            {
                return null;
            }

            return value / 1000000d;
        }

        private static long? ReadBytes(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count <= 0)
            {
                return null;
            }

            long value = recorder.LastValue;
            return value >= 0 ? value : null;
        }

        public void Dispose()
        {
            DisposeRecorder(ref _mainThread);
            DisposeRecorder(ref _renderThread);
            DisposeRecorder(ref _gfxWaitForPresent);
            DisposeRecorder(ref _gfxWaitForPresentOnGfxThread);
            DisposeRecorder(ref _scriptRunBehaviourUpdate);
            DisposeRecorder(ref _behaviourUpdate);
            DisposeRecorder(ref _scriptRunBehaviourLateUpdate);
            DisposeRecorder(ref _scriptRunBehaviourFixedUpdate);
            DisposeRecorder(ref _gcAlloc);
            DisposeRecorder(ref _cameraRender);
            DisposeRecorder(ref _uiRendering);
            DisposeRecorder(ref _canvasBuildBatch);
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }
#else
        public UnityFrameProfilerRecorder(DiagnosticsConfig config)
        {
        }

        public void Collect(DiagnosticsClientMetrics metrics)
        {
            // ProfilerRecorder is unavailable in older Unity versions. Fields stay null.
        }

        public void Dispose()
        {
        }
#endif
    }
}
