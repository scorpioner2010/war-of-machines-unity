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
        public int totalAmmo = VehicleRuntimeStats.DefaultShellsCount;

        public UnityEngine.Events.UnityEvent onShot;

        private GunCrosshair _crosshair;

        private readonly SyncVar<int> _ammoLeft = new();
        private readonly SyncVar<bool> _isReloading = new();
        private readonly SyncVar<float> _reloadRemain = new();

        private float _serverTimer;
        private bool _initialized;

        private float _clientReloadRemain;
        private float _nextHudResolveTime;
        private int _lastHudAmmo = int.MinValue;
        private ReloadHudState _lastHudState = ReloadHudState.Unknown;
        private int _lastReloadTenths = int.MinValue;

        private enum ReloadHudState : byte
        {
            Unknown = 0,
            Ready = 1,
            Reloading = 2,
            Empty = 3
        }

        public bool ServerCanFire
        {
            get
            {
                return IsServerInitialized && !_isReloading.Value && _ammoLeft.Value > 0;
            }
        }

        public int ServerAmmoLeft => _ammoLeft.Value;
        public bool ServerIsReloading => _isReloading.Value;
        public float ServerReloadRemain => _reloadRemain.Value;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                return;
            }

            if (stats.ReloadTime > 0f)
            {
                reloadTime = stats.ReloadTime;
            }

            ApplyTotalAmmo(stats.ShellsCount);
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

        private void ApplyTotalAmmo(int value)
        {
            int oldTotalAmmo = totalAmmo;
            int newTotalAmmo = VehicleRuntimeStats.ResolveShellsCount(value);
            if (newTotalAmmo == oldTotalAmmo)
            {
                return;
            }

            bool canResetServerAmmo = IsServerInitialized && _ammoLeft.Value == oldTotalAmmo;
            totalAmmo = newTotalAmmo;

            if (canResetServerAmmo)
            {
                _ammoLeft.Value = totalAmmo;
            }
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

            if (ShouldProcessServerBotFire())
            {
                ServerTryFireAuthoritative();
            }
        }

        public void Init()
        {
            if (_initialized)
            {
                return;
            }

            TryResolveCrosshair(true);
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

            if (!ServerCanFire)
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

        private bool ShouldProcessServerBotFire()
        {
            return IsServerInitialized
                   && vehicleRoot != null
                   && vehicleRoot.botBrain != null
                   && vehicleRoot.botBrain.IsRunning
                   && vehicleRoot.inputManager != null
                   && vehicleRoot.inputManager.Shoot;
        }

        [Server]
        public bool ServerTryFireAuthoritative()
        {
            if (!ServerCanFire)
            {
                return false;
            }

            if (vehicleRoot == null || vehicleRoot.shooterNet == null)
            {
                return false;
            }

            _ammoLeft.Value = Mathf.Max(0, _ammoLeft.Value - 1);
            StartServerReloadTimer();

            vehicleRoot.shooterNet.ServerFireAuthoritative();
            onShot?.Invoke();
            return true;
        }

        private void ApplyHud()
        {
            if (!TryResolveCrosshair(false))
            {
                return;
            }

            int ammoLeft = _ammoLeft.Value;
            bool isReloading = _isReloading.Value;
            float reloadRemain = _reloadRemain.Value;

            int safeAmmoLeft = Mathf.Max(0, ammoLeft);
            if (_crosshair.ammoLeftText != null && _lastHudAmmo != safeAmmoLeft)
            {
                _lastHudAmmo = safeAmmoLeft;
                _crosshair.ammoLeftText.SetText("{0}", safeAmmoLeft);
            }

            if (ammoLeft <= 0 && !isReloading)
            {
                if (_crosshair.fillImage != null)
                {
                    _crosshair.fillImage.fillAmount = 0f;
                }
                if (_crosshair.reloadText != null)
                {
                    ApplyReloadText(ReloadHudState.Empty, 0);
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
                    int reloadTenths = Mathf.CeilToInt(Mathf.Max(0f, reloadRemain) * 10f);
                    ApplyReloadText(ReloadHudState.Reloading, reloadTenths);
                }
                return;
            }

            if (_crosshair.fillImage != null)
            {
                _crosshair.fillImage.fillAmount = 1f;
            }
            if (_crosshair.reloadText != null)
            {
                ApplyReloadText(ReloadHudState.Ready, 0);
            }
        }

        private void ApplyReloadText(ReloadHudState state, int reloadTenths)
        {
            if (_crosshair == null || _crosshair.reloadText == null)
            {
                return;
            }

            if (_lastHudState == state && _lastReloadTenths == reloadTenths)
            {
                return;
            }

            _lastHudState = state;
            _lastReloadTenths = reloadTenths;

            if (state == ReloadHudState.Empty)
            {
                _crosshair.reloadText.text = "EMPTY";
                return;
            }

            if (state == ReloadHudState.Ready)
            {
                _crosshair.reloadText.text = "READY";
                return;
            }

            _crosshair.reloadText.SetText("{0:0.0}s", reloadTenths * 0.1f);
        }

        private bool TryResolveCrosshair(bool force)
        {
            if (_crosshair != null)
            {
                return true;
            }

            if (!force && Time.unscaledTime < _nextHudResolveTime)
            {
                return false;
            }

            _nextHudResolveTime = Time.unscaledTime + 0.25f;
            _crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            return _crosshair != null;
        }
    }
}
