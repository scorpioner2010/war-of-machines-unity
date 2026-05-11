using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class WeaponReloadController : NetworkBehaviour, IVehicleRootAware, IVehicleInitializable, IVehicleStatsConsumer
    {
        public VehicleRoot vehicleRoot;

        public float reloadTime = 2f;
        public int totalAmmo = 10;

        public UnityEngine.Events.UnityEvent onShot;

        private GunCrosshair _crosshair;

        private readonly SyncVar<int> _ammoLeft = new();
        private readonly SyncVar<bool> _isReloading = new();
        private readonly SyncVar<float> _reloadRemain = new();

        private float _serverTimer;
        private bool _initialized;

        private float _clientReloadRemain;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats != null && stats.ReloadTime > 0f)
            {
                reloadTime = stats.ReloadTime;
            }
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsOwner && !context.IsMenu)
            {
                Init();
            }
        }

        public override void OnStartServer()
        {
            _ammoLeft.Value = totalAmmo;
            _isReloading.Value = false;
            _reloadRemain.Value = 0f;
        }

        private void Update()
        {
            if (IsOwner && _clientReloadRemain > 0f)
            {
                _clientReloadRemain -= Time.deltaTime;
            }

            if (IsServerInitialized && _isReloading.Value)
            {
                float dt = Time.deltaTime;
                _serverTimer -= dt;
                if (_serverTimer < 0f)
                {
                    _serverTimer = 0f;
                }

                _reloadRemain.Value = _serverTimer;

                if (_serverTimer <= 0f)
                {
                    _isReloading.Value = false;
                    _reloadRemain.Value = 0f;
                }
            }
        }

        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            _crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            _initialized = true;

            ApplyHud();
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            if (IsOwner)
            {
                ApplyHud();

                bool localGate = (_clientReloadRemain > 0f);

                if (!localGate && !_isReloading.Value && _ammoLeft.Value > 0 && vehicleRoot.inputManager.Shoot)
                {
                    _clientReloadRemain = Mathf.Max(_clientReloadRemain, reloadTime);
                    vehicleRoot.shooterNet.PredictAndRequest();

                    RequestFireServerRpc();
                }
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestFireServerRpc(NetworkConnection sender = null)
        {
            if (!IsServerInitialized || sender == null)
            {
                return;
            }

            if (_isReloading.Value || _ammoLeft.Value <= 0)
            {
                return;
            }

            _ammoLeft.Value = Mathf.Max(0, _ammoLeft.Value - 1);
            StartServerReloadTimer();

            FireApprovedTargetRpc(sender);
            onShot?.Invoke();
        }

        [TargetRpc]
        private void FireApprovedTargetRpc(NetworkConnection conn)
        {
            ApplyHud();
        }

        private void StartServerReloadTimer()
        {
            _isReloading.Value = true;
            _serverTimer = Mathf.Max(0.01f, reloadTime);
            _reloadRemain.Value = _serverTimer;
        }

        private void ApplyHud()
        {
            if (_crosshair == null)
            {
                return;
            }

            int ammoLeft = _ammoLeft.Value;
            bool isReloading = _isReloading.Value;
            float reloadRemain = _reloadRemain.Value;

            if (_crosshair.ammoLeftText != null)
            {
                _crosshair.ammoLeftText.text = Mathf.Max(0, ammoLeft).ToString();
            }

            if (ammoLeft <= 0 && !isReloading)
            {
                if (_crosshair.fillImage != null)
                {
                    _crosshair.fillImage.fillAmount = 0f;
                }
                if (_crosshair.reloadText != null)
                {
                    _crosshair.reloadText.text = "EMPTY";
                }
                return;
            }

            if (isReloading)
            {
                float t = reloadTime > 0.0001f
                    ? Mathf.Clamp01(1f - (reloadRemain / reloadTime))
                    : 1f;

                if (_crosshair.fillImage != null)
                {
                    _crosshair.fillImage.fillAmount = t;
                }
                if (_crosshair.reloadText != null)
                {
                    _crosshair.reloadText.text = $"{Mathf.Max(0f, reloadRemain):0.0}s";
                }
                return;
            }

            if (_crosshair.fillImage != null)
            {
                _crosshair.fillImage.fillAmount = 1f;
            }
            if (_crosshair.reloadText != null)
            {
                _crosshair.reloadText.text = "READY";
            }
        }
    }
}
