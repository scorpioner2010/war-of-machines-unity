using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.SceneManagement;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.World.Spawns
{
    public class SpawnPoint : NetworkBehaviour
    {
        public readonly SyncVar<bool> IsNotFree = new (false);

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
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            
                if (mesh != null)
                {
                    mesh.enabled = false;
                }
        }

        public static SpawnPoint GetFreePoint(Scene scene)
        {
            return GetFreePoint(scene, MatchTeam.None);
        }

        public static SpawnPoint GetFreePoint(Scene scene, MatchTeam team)
        {
            List<SpawnPoint> allPoints = SceneObjectFinder.FindInScene<SpawnPoint>(scene);
            List<SpawnPoint> preferredPoints = new List<SpawnPoint>();
            List<SpawnPoint> fallbackPoints = new List<SpawnPoint>();

            foreach (SpawnPoint point in allPoints)
            {
                if (point == null || point.IsNotFree.Value)
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
                if (current.name.IndexOf("Red", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return MatchTeam.Red;
                }

                if (current.name.IndexOf("Blue", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return MatchTeam.Blue;
                }

                current = current.parent;
            }

            return MatchTeam.None;
        }
    }
}
