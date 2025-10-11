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
        public readonly SyncVar<bool> IsNotFree = new (false);

        private async void MarkPoint()
        {
            IsNotFree.Value = true;
            await UniTask.Delay(5000);
            IsNotFree.Value = false;
        }

        private void Awake()
        {
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            
            if(mesh != null)
            {
                mesh.enabled = false;
            }
        }

        public static SpawnPoint GetFreePoint(Scene scene)
        {
            List<SpawnPoint> allPoints = GameplaySpawner.FindObjectsInScene<SpawnPoint>(scene);
            List<SpawnPoint> freePoints = new();

            foreach (SpawnPoint point in allPoints)
            {
                if (point.IsNotFree.Value == false)
                {
                    freePoints.Add(point);
                }
            }
            
            SpawnPoint random = freePoints.RandomElement();

            if (random != null)
            {
                random.MarkPoint();
            }
            
            return random;
        }
    }
}