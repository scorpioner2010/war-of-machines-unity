using System;
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

        public Action<float, float, float> OnDamaged;
        public UnityEvent onDeath;

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
        
        public float Current => _hp.Value;
        public bool IsDead => _dead.Value;

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

        [Server]
        public void ServerApplyDamage(float dmg)
        {
            if (!IsServerInitialized || _dead.Value || dmg <= 0f)
            {
                return;
            }

            float old = _hp.Value;
            float newHp = Mathf.Max(0f, old - dmg);
            _hp.Value = newHp;

            DamagedObserversRpc(dmg, _hp.Value, maxHealth);

            if (_hp.Value <= 0f)
            {
                _dead.Value = true;
                DeathServer();
            }
        }

        [Server]
        private void DeathServer()
        {
            SetCollidersEnabled(false);
            DiedObserversRpc();
            TurnOffObject();
        }

        private void TurnOffObject()
        {
            gameObject.SetActive(false);
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
        private void DamagedObserversRpc(float dmg, float newHp, float maxHp)
        {
            OnDamaged?.Invoke(dmg, newHp, maxHp);
        }

        [ObserversRpc(BufferLast = false)]
        private void DiedObserversRpc()
        {
            onDeath?.Invoke();
        }
    }
}
