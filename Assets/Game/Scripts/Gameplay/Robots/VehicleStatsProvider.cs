using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class VehicleStatsProvider
    {
        private static VehicleRuntimeStats[] _cache;
        private static bool _loading;

        public static async UniTask PreloadAsync()
        {
            if (_cache != null)
            {
                return;
            }

            if (_loading)
            {
                while (_loading)
                {
                    await UniTask.DelayFrame(1);
                }

                return;
            }

            try
            {
                _loading = true;
                (bool isSuccess, string message, VehicleLite[] items) result = await VehiclesManager.GetAll();
                if (result.isSuccess && result.items != null)
                {
                    _cache = new VehicleRuntimeStats[result.items.Length];
                    for (int i = 0; i < result.items.Length; i++)
                    {
                        _cache[i] = VehicleRuntimeStats.FromVehicleLite(result.items[i]);
                    }
                }
                else
                {
                    Debug.LogWarning("Failed to load vehicle stats from API: " + result.message);
                    _cache = new VehicleRuntimeStats[0];
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to load vehicle stats from API: " + ex.Message);
                _cache = new VehicleRuntimeStats[0];
            }
            finally
            {
                _loading = false;
            }
        }

        public static async UniTask<VehicleRuntimeStats> GetAsync(int vehicleId, string code)
        {
            await PreloadAsync();

            VehicleRuntimeStats cached = FindCached(vehicleId, code);
            if (cached != null)
            {
                return cached.Clone();
            }

            VehicleRuntimeStats direct = await LoadDirectAsync(vehicleId, code);
            if (direct != null)
            {
                return direct;
            }

            Debug.LogWarning("Vehicle stats were not found. vehicleId=" + vehicleId + ", code=" + code);
            return null;
        }

        public static async UniTask<VehicleRuntimeStats[]> GetAllAsync()
        {
            await PreloadAsync();

            if (_cache == null || _cache.Length == 0)
            {
                return Array.Empty<VehicleRuntimeStats>();
            }

            VehicleRuntimeStats[] result = new VehicleRuntimeStats[_cache.Length];
            for (int i = 0; i < _cache.Length; i++)
            {
                result[i] = _cache[i] != null ? _cache[i].Clone() : null;
            }

            return result;
        }

        private static VehicleRuntimeStats FindCached(int vehicleId, string code)
        {
            if (_cache == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(code))
            {
                for (int i = 0; i < _cache.Length; i++)
                {
                    VehicleRuntimeStats stats = _cache[i];
                    if (stats != null && stats.Code == code)
                    {
                        return stats;
                    }
                }
            }

            if (vehicleId > 0)
            {
                for (int i = 0; i < _cache.Length; i++)
                {
                    VehicleRuntimeStats stats = _cache[i];
                    if (stats != null && stats.VehicleId == vehicleId)
                    {
                        return stats;
                    }
                }
            }

            return null;
        }

        private static async UniTask<VehicleRuntimeStats> LoadDirectAsync(int vehicleId, string code)
        {
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    (bool isSuccess, string message, VehicleLite item) byCode = await VehiclesManager.GetByCode(code);
                    if (byCode.isSuccess && byCode.item != null)
                    {
                        return VehicleRuntimeStats.FromVehicleLite(byCode.item);
                    }
                }

                if (vehicleId > 0)
                {
                    (bool isSuccess, string message, VehicleLite item) byId = await VehiclesManager.GetById(vehicleId);
                    if (byId.isSuccess && byId.item != null)
                    {
                        return VehicleRuntimeStats.FromVehicleLite(byId.item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to load direct vehicle stats from API: " + ex.Message);
            }

            return null;
        }
    }
}
