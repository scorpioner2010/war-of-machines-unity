using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.World.Spawns
{
    public class SpawnPoint : NetworkBehaviour
    {
        private static readonly List<SpawnPoint> ActivePoints = new List<SpawnPoint>(64);

        public readonly SyncVar<bool> IsNotFree = new (false);
        [SerializeField] private MeshRenderer markerRenderer;

        private void ReserveTemporarily()
        {
            ReserveTemporarilyAsync().Forget();
        }

        private async UniTask ReserveTemporarilyAsync()
        {
            IsNotFree.Value = true;
            await UniTask.Delay(5000);
            IsNotFree.Value = false;
        }

        private void Awake()
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!ActivePoints.Contains(this))
            {
                ActivePoints.Add(this);
            }
        }

        private void OnDisable()
        {
            int index = ActivePoints.IndexOf(this);
            if (index >= 0)
            {
                ActivePoints.RemoveAt(index);
            }
        }

        public static SpawnPoint GetFreePoint(Scene scene)
        {
            return GetFreePoint(scene, MatchTeam.None);
        }

        public static SpawnPoint GetFreePoint(Scene scene, MatchTeam team)
        {
            List<SpawnPoint> preferredPoints = new List<SpawnPoint>();
            List<SpawnPoint> fallbackPoints = new List<SpawnPoint>();

            for (int i = 0; i < ActivePoints.Count; i++)
            {
                SpawnPoint point = ActivePoints[i];
                if (point == null || point.IsNotFree.Value)
                {
                    continue;
                }

                if (point.gameObject.scene != scene)
                {
                    continue;
                }

                fallbackPoints.Add(point);

                if (team == MatchTeam.None || point.BelongsToTeam(team))
                {
                    preferredPoints.Add(point);
                }
            }

            List<SpawnPoint> freePoints = preferredPoints.Count > 0 ? preferredPoints : fallbackPoints;
            if (freePoints.Count == 0)
            {
                return null;
            }
            
            SpawnPoint random = freePoints.RandomElement();

            if (random != null)
            {
                random.ReserveTemporarily();
            }
            
            return random;
        }

        private bool BelongsToTeam(MatchTeam team)
        {
            return GetTeamFromHierarchy() == team;
        }

        private MatchTeam GetTeamFromHierarchy()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name.IndexOf("TeamA", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return MatchTeam.TeamA;
                }

                if (current.name.IndexOf("TeamB", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return MatchTeam.TeamB;
                }

                current = current.parent;
            }

            return MatchTeam.None;
        }
    }
}
