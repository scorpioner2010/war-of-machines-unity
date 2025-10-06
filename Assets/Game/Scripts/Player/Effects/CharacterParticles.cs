using UnityEngine;

namespace Game.Scripts.Player.Effects
{
    public class CharacterParticles : MonoBehaviour
    {
        public static CharacterParticles In;
        
        public ParticleSystem bloodPrefab;
        public ParticleSystem hitPrefab;
        public ParticleSystem revolverShoot;
        

        private void Awake() => In = this;

        public void BloodEffectPlay(Vector3 spawnPosition) => Play(spawnPosition, bloodPrefab);
        public void HitEffectPlay(Vector3 spawnPosition) => Play(spawnPosition, hitPrefab);
        public void RevolverShootEffectPlay(Vector3 spawnPosition) => Play(spawnPosition, revolverShoot);
        
        private ParticleSystem Play(Vector3 spawnPosition, ParticleSystem prefab)
        {
            ParticleSystem hit = Instantiate(prefab, null, true);
            hit.transform.position = spawnPosition;
            hit.Play();
            Destroy(hit.gameObject, 2);
            return hit;
        }

        public void PlatTransform(Vector3 spawnPosition, Transform spawnTransform, ParticleSystem prefab)
        {
            ParticleSystem hit = Play(spawnPosition, prefab);
            hit.transform.parent = spawnTransform;
        }
    }
}