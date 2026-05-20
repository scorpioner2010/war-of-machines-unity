using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleHealth : NetworkBehaviour, IVehicleStatsConsumer
    {
        private const string DeathDebrisLayerName = "Chassis";
        private static int _deathDebrisLayer = int.MinValue;

        [Min(1f)] public float maxHealth = 100f;

        public Action<float, float, float> OnDamaged;
        public Action<float, float> OnHealthChanged;
        public Action<VehicleHealth> OnServerDeath;
        public UnityEvent onDeath;

        private readonly SyncVar<float> _hp = new();
        private readonly SyncVar<bool> _dead = new();

        public Collider[] colliders;
        private Collider[] _runtimeColliders;
        private bool _hasObservedHealth;
        private float _observedHealth;
        private bool _hasAppliedSyncHealth;
        private float _appliedSyncHealth;

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
            get
            {
                PullSyncHealthIfChanged();

                float max = MaxHealth;

                if (_dead.Value)
                {
                    return 0f;
                }

                if (_hasObservedHealth)
                {
                    return Mathf.Clamp(_observedHealth, 0f, max);
                }

                return max;
            }
        }

        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public bool IsDead => _dead.Value;

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null || stats.MaxHealth <= 0f)
            {
                return;
            }

            float oldMax = Mathf.Max(1f, maxHealth);
            maxHealth = Mathf.Max(1f, stats.MaxHealth);

            if (IsServerInitialized && !_dead.Value)
            {
                float ratio = _hp.Value > 0f ? Mathf.Clamp01(_hp.Value / oldMax) : 1f;
                _hp.Value = Mathf.Clamp(maxHealth * ratio, 1f, maxHealth);
                SetObservedHealth(_hp.Value, maxHealth, true);
            }
            else if (!_dead.Value)
            {
                if (_hasAppliedSyncHealth)
                {
                    SetObservedHealth(_appliedSyncHealth, maxHealth, true);
                    return;
                }

                if (_hp.Value > 0f)
                {
                    SetObservedHealth(_hp.Value, maxHealth, true);
                    return;
                }

                float sourceHealth = _hasObservedHealth
                    ? _observedHealth
                    : oldMax;
                float ratio = Mathf.Clamp01(sourceHealth / oldMax);
                SetObservedHealth(maxHealth * ratio, maxHealth);
            }
        }

        public override void OnStartServer()
        {
            _hp.Value = Mathf.Max(1f, maxHealth);
            _dead.Value = false;
            SetObservedHealth(_hp.Value, maxHealth, true);
        }

        public override void OnStartClient()
        {
            _hp.OnChange += OnHpSyncChanged;
            _dead.OnChange += OnDeadSyncChanged;
            if (_dead.Value)
            {
                SetObservedHealth(0f, maxHealth, true);
            }
            else if (_hp.Value > 0f)
            {
                SetObservedHealth(_hp.Value, maxHealth, true);
            }
            else if (!_hasObservedHealth)
            {
                SetObservedHealth(maxHealth, maxHealth);
            }

            SetCollidersEnabled(!_dead.Value);
        }

        public override void OnStopClient()
        {
            _hp.OnChange -= OnHpSyncChanged;
            _dead.OnChange -= OnDeadSyncChanged;
            base.OnStopClient();
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
            SetObservedHealth(_hp.Value, maxHealth, true);

            DamagedObserversRpc(dmg, _hp.Value, maxHealth);

            if (_hp.Value <= 0f)
            {
                _dead.Value = true;
                DeathServer();
            }
        }

        [Server]
        public void ServerKill()
        {
            if (!IsServerInitialized || _dead.Value)
            {
                return;
            }

            float old = _hp.Value;
            _hp.Value = 0f;
            SetObservedHealth(0f, maxHealth, true);
            DamagedObserversRpc(old, _hp.Value, maxHealth);
            _dead.Value = true;
            DeathServer();
        }

        [Server]
        private void DeathServer()
        {
            SetCollidersEnabled(false);
            OnServerDeath?.Invoke(this);
            DiedObserversRpc();
        }
        
        private void SetCollidersEnabled(bool v)
        {
            EnsureRuntimeColliders();
            if (_runtimeColliders == null)
            {
                return;
            }

            for (int i = 0; i < _runtimeColliders.Length; i++)
            {
                Collider targetCollider = _runtimeColliders[i];
                if (targetCollider != null)
                {
                    if (!v && IsDeathDebrisCollider(targetCollider))
                    {
                        continue;
                    }

                    targetCollider.enabled = v;
                }
            }
        }

        private void EnsureRuntimeColliders()
        {
            if (_runtimeColliders != null)
            {
                return;
            }

            if (colliders != null && colliders.Length > 0)
            {
                _runtimeColliders = colliders;
                return;
            }

            _runtimeColliders = GetComponentsInChildren<Collider>(true);
        }

        private static bool IsDeathDebrisCollider(Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            if (_deathDebrisLayer == int.MinValue)
            {
                _deathDebrisLayer = LayerMask.NameToLayer(DeathDebrisLayerName);
            }

            return _deathDebrisLayer >= 0
                   && targetCollider.gameObject.layer == _deathDebrisLayer
                   && targetCollider.attachedRigidbody != null
                   && targetCollider.transform.parent == null;
        }

        [ObserversRpc(BufferLast = false)]
        private void DamagedObserversRpc(float dmg, float newHp, float maxHp)
        {
            SetObservedHealth(newHp, maxHp, true);
            OnDamaged?.Invoke(dmg, newHp, maxHp);
        }

        [ObserversRpc(BufferLast = false)]
        private void DiedObserversRpc()
        {
            SetObservedHealth(0f, maxHealth, true);
            onDeath?.Invoke();
        }

        private void OnHpSyncChanged(float previous, float next, bool asServer)
        {
            if (_dead.Value)
            {
                SetObservedHealth(0f, maxHealth, true);
                return;
            }

            if (next > 0f)
            {
                SetObservedHealth(next, maxHealth, true);
            }
        }

        private void OnDeadSyncChanged(bool previous, bool next, bool asServer)
        {
            if (next)
            {
                SetObservedHealth(0f, maxHealth, true);
                SetCollidersEnabled(false);
            }
            else if (_hp.Value > 0f)
            {
                SetObservedHealth(_hp.Value, maxHealth, true);
                SetCollidersEnabled(true);
            }
        }

        private void SetObservedHealth(float currentHealth, float currentMaxHealth, bool fromSync = false)
        {
            maxHealth = Mathf.Max(1f, currentMaxHealth);
            _observedHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            _hasObservedHealth = true;

            if (fromSync)
            {
                _appliedSyncHealth = Mathf.Max(0f, currentHealth);
                _hasAppliedSyncHealth = true;
            }

            OnHealthChanged?.Invoke(_observedHealth, maxHealth);
        }

        private void PullSyncHealthIfChanged()
        {
            float max = MaxHealth;

            if (_dead.Value)
            {
                if (!_hasAppliedSyncHealth || !Mathf.Approximately(_appliedSyncHealth, 0f))
                {
                    _observedHealth = 0f;
                    _hasObservedHealth = true;
                    _appliedSyncHealth = 0f;
                    _hasAppliedSyncHealth = true;
                }

                return;
            }

            float syncHealth = _hp.Value;
            if (syncHealth <= 0f)
            {
                return;
            }

            if (_hasAppliedSyncHealth && Mathf.Approximately(syncHealth, _appliedSyncHealth))
            {
                return;
            }

            _observedHealth = Mathf.Clamp(syncHealth, 0f, max);
            _hasObservedHealth = true;
            _appliedSyncHealth = Mathf.Max(0f, syncHealth);
            _hasAppliedSyncHealth = true;
        }
    }
}
