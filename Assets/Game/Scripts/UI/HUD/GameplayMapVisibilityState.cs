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

        public static event Action Changed;
        public static int Version => _version;
        public static int Count => Entries.Count;

        public static GameplayMapVisibilityEntry GetEntry(int index)
        {
            return Entries[index];
        }

        public static void Apply(
            int version,
            int count,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws)
        {
            int safeCount = GetSafeCount(count, objectIds, relations, positions, yaws);
            int signature = BuildSignature(safeCount, objectIds, relations, positions, yaws);
            if (signature == _signature)
            {
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

                Entries.Add(new GameplayMapVisibilityEntry
                {
                    ObjectId = objectIds[i],
                    Relation = relation,
                    Position = positions[i],
                    Yaw = yaws[i]
                });
            }

            _signature = signature;
            _version++;
            Changed?.Invoke();
        }

        public static void Clear()
        {
            if (Entries.Count == 0 && _signature == 0)
            {
                return;
            }

            Entries.Clear();
            _signature = 0;
            _version++;
            Changed?.Invoke();
        }

        private static int BuildSignature(
            int count,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws)
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

        private static int GetSafeCount(
            int requestedCount,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws)
        {
            if (requestedCount <= 0 || objectIds == null || relations == null || positions == null || yaws == null)
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

            return MapVehicleVisibilityRelation.Hidden;
        }
    }

    public struct GameplayMapVisibilityEntry
    {
        public int ObjectId;
        public MapVehicleVisibilityRelation Relation;
        public Vector3 Position;
        public float Yaw;
    }
}
