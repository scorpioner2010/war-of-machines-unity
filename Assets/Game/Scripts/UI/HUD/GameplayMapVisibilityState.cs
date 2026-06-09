using System;
using System.Collections.Generic;
using Game.Scripts.Networking.Lobby;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public static class GameplayMapVisibilityState
    {
        private const float PositionQuantization = 0.5f;
        private const float YawQuantization = 2f;

        private static readonly List<GameplayMapVisibilityEntry> Entries = new List<GameplayMapVisibilityEntry>(16);
        private static int _version;
        private static int _signature;
        private static int _lastNetworkVersion = -1;

        public static event Action Changed;
        public static int Version => _version;
        public static int Count => Entries.Count;

        public static GameplayMapVisibilityEntry GetEntry(int index)
        {
            return Entries[index];
        }

        public static bool TryGetRelation(int objectId, out MapVehicleVisibilityRelation relation)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                GameplayMapVisibilityEntry entry = Entries[i];
                if (entry.ObjectId == objectId)
                {
                    relation = entry.Relation;
                    return true;
                }
            }

            relation = MapVehicleVisibilityRelation.Hidden;
            return false;
        }

        public static void Apply(
            int version,
            int count,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws,
            float[] remainingVisibilitySeconds,
            float snapshotAgeSeconds)
        {
            if (version <= _lastNetworkVersion)
            {
                return;
            }

            _lastNetworkVersion = version;
            int safeCount = GetSafeCount(
                count,
                objectIds,
                relations,
                positions,
                yaws,
                remainingVisibilitySeconds);
            float safeSnapshotAgeSeconds = Mathf.Max(0f, snapshotAgeSeconds);
            int signature = BuildSignature(
                safeCount,
                objectIds,
                relations,
                positions,
                yaws,
                remainingVisibilitySeconds,
                safeSnapshotAgeSeconds);
            float now = Time.unscaledTime;
            if (signature == _signature)
            {
                RefreshEntryExpirations(
                    safeCount,
                    objectIds,
                    relations,
                    remainingVisibilitySeconds,
                    safeSnapshotAgeSeconds,
                    now);
                return;
            }

            Entries.Clear();

            for (int i = 0; i < safeCount; i++)
            {
                MapVehicleVisibilityRelation relation = ToRelation(relations[i]);
                if (relation == MapVehicleVisibilityRelation.Hidden)
                {
                    continue;
                }

                if (IsExpiredAtReceive(remainingVisibilitySeconds[i], safeSnapshotAgeSeconds))
                {
                    continue;
                }

                Entries.Add(new GameplayMapVisibilityEntry
                {
                    ObjectId = objectIds[i],
                    Relation = relation,
                    Position = positions[i],
                    Yaw = yaws[i],
                    ExpiresAtClientTime = ResolveExpirationTime(
                        remainingVisibilitySeconds[i],
                        safeSnapshotAgeSeconds,
                        now)
                });
            }

            _signature = signature;
            _version++;
            Changed?.Invoke();
        }

        public static void Tick(float now)
        {
            bool changed = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                GameplayMapVisibilityEntry entry = Entries[i];
                if (entry.ExpiresAtClientTime > now)
                {
                    continue;
                }

                Entries.RemoveAt(i);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            _signature = BuildSignature(Entries);
            _version++;
            Changed?.Invoke();
        }

        public static void Clear()
        {
            bool changed = Entries.Count > 0 || _signature != 0;
            Entries.Clear();
            _signature = 0;
            _lastNetworkVersion = -1;
            if (!changed)
            {
                return;
            }

            _version++;
            Changed?.Invoke();
        }

        private static void RefreshEntryExpirations(
            int count,
            int[] objectIds,
            byte[] relations,
            float[] remainingVisibilitySeconds,
            float snapshotAgeSeconds,
            float now)
        {
            for (int i = 0; i < count; i++)
            {
                if (ToRelation(relations[i]) == MapVehicleVisibilityRelation.Hidden)
                {
                    continue;
                }

                if (IsExpiredAtReceive(remainingVisibilitySeconds[i], snapshotAgeSeconds))
                {
                    continue;
                }

                for (int entryIndex = 0; entryIndex < Entries.Count; entryIndex++)
                {
                    GameplayMapVisibilityEntry entry = Entries[entryIndex];
                    if (entry.ObjectId != objectIds[i])
                    {
                        continue;
                    }

                    entry.ExpiresAtClientTime = ResolveExpirationTime(
                        remainingVisibilitySeconds[i],
                        snapshotAgeSeconds,
                        now);
                    Entries[entryIndex] = entry;
                    break;
                }
            }
        }

        private static bool IsExpiredAtReceive(float remainingVisibilitySeconds, float snapshotAgeSeconds)
        {
            return remainingVisibilitySeconds >= 0f
                   && remainingVisibilitySeconds <= snapshotAgeSeconds;
        }

        private static float ResolveExpirationTime(
            float remainingVisibilitySeconds,
            float snapshotAgeSeconds,
            float now)
        {
            if (remainingVisibilitySeconds < 0f)
            {
                return float.PositiveInfinity;
            }

            return now + Mathf.Max(0f, remainingVisibilitySeconds - snapshotAgeSeconds);
        }

        private static int BuildSignature(
            int count,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws,
            float[] remainingVisibilitySeconds,
            float snapshotAgeSeconds)
        {
            unchecked
            {
                int signature = 17;
                int visibleCount = 0;
                for (int i = 0; i < count; i++)
                {
                    MapVehicleVisibilityRelation relation = ToRelation(relations[i]);
                    if (relation == MapVehicleVisibilityRelation.Hidden)
                    {
                        continue;
                    }

                    if (IsExpiredAtReceive(remainingVisibilitySeconds[i], snapshotAgeSeconds))
                    {
                        continue;
                    }

                    visibleCount++;
                    Vector3 position = positions[i];
                    int quantizedX = Mathf.RoundToInt(position.x / PositionQuantization);
                    int quantizedZ = Mathf.RoundToInt(position.z / PositionQuantization);
                    int quantizedYaw = Mathf.RoundToInt(yaws[i] / YawQuantization);
                    signature = (signature * 31) + objectIds[i];
                    signature = (signature * 31) + (int)relation;
                    signature = (signature * 31) + quantizedX;
                    signature = (signature * 31) + quantizedZ;
                    signature = (signature * 31) + quantizedYaw;
                }

                if (visibleCount == 0)
                {
                    return 0;
                }

                signature = (signature * 31) + visibleCount;
                return signature;
            }
        }

        private static int BuildSignature(List<GameplayMapVisibilityEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            unchecked
            {
                int signature = 17;
                for (int i = 0; i < entries.Count; i++)
                {
                    GameplayMapVisibilityEntry entry = entries[i];
                    int quantizedX = Mathf.RoundToInt(entry.Position.x / PositionQuantization);
                    int quantizedZ = Mathf.RoundToInt(entry.Position.z / PositionQuantization);
                    int quantizedYaw = Mathf.RoundToInt(entry.Yaw / YawQuantization);
                    signature = (signature * 31) + entry.ObjectId;
                    signature = (signature * 31) + (int)entry.Relation;
                    signature = (signature * 31) + quantizedX;
                    signature = (signature * 31) + quantizedZ;
                    signature = (signature * 31) + quantizedYaw;
                }

                signature = (signature * 31) + entries.Count;
                return signature;
            }
        }

        private static int GetSafeCount(
            int requestedCount,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws,
            float[] remainingVisibilitySeconds)
        {
            if (requestedCount <= 0
                || objectIds == null
                || relations == null
                || positions == null
                || yaws == null
                || remainingVisibilitySeconds == null)
            {
                return 0;
            }

            int count = Mathf.Min(requestedCount, objectIds.Length);
            if (relations.Length < count)
            {
                count = relations.Length;
            }
            if (positions.Length < count)
            {
                count = positions.Length;
            }
            if (yaws.Length < count)
            {
                count = yaws.Length;
            }
            if (remainingVisibilitySeconds.Length < count)
            {
                count = remainingVisibilitySeconds.Length;
            }

            return count;
        }

        private static MapVehicleVisibilityRelation ToRelation(byte value)
        {
            if (value == (byte)MapVehicleVisibilityRelation.Ally)
            {
                return MapVehicleVisibilityRelation.Ally;
            }

            if (value == (byte)MapVehicleVisibilityRelation.Enemy)
            {
                return MapVehicleVisibilityRelation.Enemy;
            }

            if (value == (byte)MapVehicleVisibilityRelation.Destroyed)
            {
                return MapVehicleVisibilityRelation.Destroyed;
            }

            if (value == (byte)MapVehicleVisibilityRelation.EnemyLastKnown)
            {
                return MapVehicleVisibilityRelation.EnemyLastKnown;
            }

            return MapVehicleVisibilityRelation.Hidden;
        }
    }

    public struct GameplayMapVisibilityEntry
    {
        public int ObjectId;
        public MapVehicleVisibilityRelation Relation;
        public Vector3 Position;
        public float Yaw;
        public float ExpiresAtClientTime;
    }
}
