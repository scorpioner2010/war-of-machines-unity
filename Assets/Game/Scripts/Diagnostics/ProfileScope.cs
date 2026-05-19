using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Diagnostics
{
    public static class ProfileScope
    {
        public static MeasureScope Measure(string name)
        {
            return Measure(name, DiagnosticsCategories.Unknown);
        }

        public static MeasureScope Measure(string name, string category)
        {
            DiagnosticsManager manager = DiagnosticsManager.Instance;
            if (manager == null || !manager.IsRunning)
            {
                return default;
            }

            return new MeasureScope(manager, name, category);
        }

        public static async Task MeasureAsync(string name, Func<Task> action)
        {
            await MeasureAsync(name, DiagnosticsCategories.Unknown, action);
        }

        public static async Task MeasureAsync(string name, string category, Func<Task> action)
        {
            if (action == null)
            {
                return;
            }

            MeasureScope scope = Measure(name, category);
            try
            {
                await action();
            }
            finally
            {
                scope.Dispose();
            }
        }

        public static async Task<T> MeasureAsync<T>(string name, Func<Task<T>> action)
        {
            return await MeasureAsync(name, DiagnosticsCategories.Unknown, action);
        }

        public static async Task<T> MeasureAsync<T>(string name, string category, Func<Task<T>> action)
        {
            if (action == null)
            {
                return default;
            }

            MeasureScope scope = Measure(name, category);
            try
            {
                return await action();
            }
            finally
            {
                scope.Dispose();
            }
        }

        public static async UniTask MeasureAsync(string name, Func<UniTask> action)
        {
            await MeasureAsync(name, DiagnosticsCategories.Unknown, action);
        }

        public static async UniTask MeasureAsync(string name, string category, Func<UniTask> action)
        {
            if (action == null)
            {
                return;
            }

            MeasureScope scope = Measure(name, category);
            try
            {
                await action();
            }
            finally
            {
                scope.Dispose();
            }
        }

        public static async UniTask<T> MeasureAsync<T>(string name, Func<UniTask<T>> action)
        {
            return await MeasureAsync(name, DiagnosticsCategories.Unknown, action);
        }

        public static async UniTask<T> MeasureAsync<T>(string name, string category, Func<UniTask<T>> action)
        {
            if (action == null)
            {
                return default;
            }

            MeasureScope scope = Measure(name, category);
            try
            {
                return await action();
            }
            finally
            {
                scope.Dispose();
            }
        }

        public static void RecordEvent(string name, string category)
        {
            RecordEvent(name, category, 1, 0d);
        }

        public static void RecordEvent(string name, string category, int count, double totalMs)
        {
            DiagnosticsManager manager = DiagnosticsManager.Instance;
            if (manager == null || !manager.IsRunning)
            {
                return;
            }

            manager.RecordEvent(name, category, count, totalMs);
        }

        public readonly struct MeasureScope : IDisposable
        {
            private readonly DiagnosticsManager _manager;
            private readonly string _name;
            private readonly string _category;
            private readonly long _startTimestamp;
            private readonly long _startAllocatedBytes;
            private readonly bool _trackAllocatedBytes;

            public MeasureScope(DiagnosticsManager manager, string name, string category)
            {
                _manager = manager;
                _name = name;
                _category = string.IsNullOrEmpty(category) ? DiagnosticsCategories.Unknown : category;
                _startTimestamp = Stopwatch.GetTimestamp();
                _trackAllocatedBytes = manager != null && manager.Config != null && manager.Config.TrackScopeAllocations;
                _startAllocatedBytes = _trackAllocatedBytes ? GC.GetAllocatedBytesForCurrentThread() : 0L;
            }

            public void Dispose()
            {
                if (_manager == null || string.IsNullOrEmpty(_name))
                {
                    return;
                }

                long elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
                double durationMs = elapsed * 1000d / Stopwatch.Frequency;
                long allocatedBytes = 0L;
                if (_trackAllocatedBytes)
                {
                    allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - _startAllocatedBytes;
                    if (allocatedBytes < 0)
                    {
                        allocatedBytes = 0;
                    }
                }

                _manager.RecordScope(_name, _category, durationMs, allocatedBytes);
            }
        }
    }

    public static class DiagnosticsRuntimeCounters
    {
        private static int _activeProjectiles;

        public static int ActiveProjectiles => _activeProjectiles;

        public static void RegisterProjectile()
        {
            _activeProjectiles++;
        }

        public static void UnregisterProjectile()
        {
            _activeProjectiles = Mathf.Max(0, _activeProjectiles - 1);
        }
    }
}
