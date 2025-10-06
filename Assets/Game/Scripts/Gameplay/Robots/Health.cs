using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Game.Scripts.Gameplay.Robots
{
    public class Health : NetworkBehaviour
    {
        [Min(1f)] public float maxHealth = 100f;
        public bool destroyOnDeath = false;
        public float respawnDelay = -1f;

        public UnityEvent onDamaged;
        public UnityEvent onDeath;
        public UnityEvent onRespawn;

        private readonly SyncVar<float> _hp = new();
        private readonly SyncVar<bool> _dead = new();

        public Collider[] colliders;

        [Button]
        private void FindArmorColliders()
        {
            List<Collider> list = new List<Collider>();
            MeshCollider[] all = GetComponentsInChildren<MeshCollider>(true);
            int armorLayer = LayerMask.NameToLayer("Armor");

            foreach (MeshCollider c in all)
            {
                if (c.gameObject.layer == armorLayer && c.convex == false)
                {
                    list.Add(c);
                }
            }

            colliders = list.ToArray();
        }

        
        public float Current
        {
            get { return _hp.Value; }
        }

        public bool IsDead
        {
            get { return _dead.Value; }
        }

        private void Awake()
        {
            _hp.OnChange += OnHpChanged;
        }

        public override void OnStartServer()
        {
            _hp.Value = Mathf.Max(1f, maxHealth);
            _dead.Value = false;
        }

        public override void OnStartClient()
        {
            // Узгодити стан колайдерів для лейт-джойнерів
            SetCollidersEnabled(!_dead.Value);
        }

        private void OnHpChanged(float prev, float next, bool asServer)
        {
            if (!IsServer)
            {
                Debug.Log($"[Health:{name}][client] HP change {prev:0.##} -> {next:0.##}");
            }
        }

        [Server]
        public void ServerApplyDamage(float dmg)
        {
            if (!IsServer || _dead.Value || dmg <= 0f)
            {
                return;
            }

            float old = _hp.Value;
            float newHp = Mathf.Max(0f, old - dmg);
            _hp.Value = newHp;

            Debug.Log($"[Health:{name}] DAMAGE {dmg:0.##}  {old:0.##} -> {newHp:0.##}");

            DamagedObserversRpc(dmg, _hp.Value);

            if (_hp.Value <= 0f)
            {
                _dead.Value = true;
                Debug.Log($"[Health:{name}] DEAD");
                DeathServer();
            }
        }

        [Server]
        public void ServerHeal(float amount)
        {
            if (!IsServer || amount <= 0f || _dead.Value)
            {
                return;
            }

            float old = _hp.Value;
            float newHp = Mathf.Min(maxHealth, old + amount);
            _hp.Value = newHp;

            Debug.Log($"[Health:{name}] HEAL {amount:0.##}  {old:0.##} -> {newHp:0.##}");

            DamagedObserversRpc(-amount, _hp.Value);
        }

        [Server]
        private void DeathServer()
        {
            SetCollidersEnabled(false);
            DiedObserversRpc();
            TurnOffObject();

            if (destroyOnDeath)
            {
                Invoke(nameof(DestroyServerObject), 2f);
            }
            else if (respawnDelay >= 0f)
            {
                Invoke(nameof(RespawnServer), respawnDelay);
            }
        }

        private void TurnOffObject()
        {
            gameObject.SetActive(false);
        }

        [Server]
        private void DestroyServerObject()
        {
            Despawn();
        }

        [Server]
        private void RespawnServer()
        {
            float old = _hp.Value;
            _hp.Value = Mathf.Max(1f, maxHealth);
            _dead.Value = false;

            SetCollidersEnabled(true);
            Debug.Log($"[Health:{name}] RESPAWN  {old:0.##} -> {_hp.Value:0.##}");

            RespawnObserversRpc();
        }

        private void SetCollidersEnabled(bool v)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = v;
                }
            }
        }

        [ObserversRpc(BufferLast = false)]
        private void DamagedObserversRpc(float amount, float newHp)
        {
            onDamaged?.Invoke();
        }

        [ObserversRpc(BufferLast = false)]
        private void DiedObserversRpc()
        {
            onDeath?.Invoke();
        }

        [ObserversRpc(BufferLast = false)]
        private void RespawnObserversRpc()
        {
            onRespawn?.Invoke();
        }
    }
}
