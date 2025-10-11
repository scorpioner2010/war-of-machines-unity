using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class WeaponReloadController : NetworkBehaviour
    {
        public TankRoot tankRoot;

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

            if (IsServer && _isReloading.Value)
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

            _crosshair = Singleton<GunCrosshair>.Instance;
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

                if (!localGate && !_isReloading.Value && _ammoLeft.Value > 0 && tankRoot.inputManager.Shoot)
                {
                    // Миттєвий локальний візуал + запит на сервер через ShooterNet
                    _clientReloadRemain = Mathf.Max(_clientReloadRemain, reloadTime);
                    tankRoot.shooterNet.PredictAndRequest();

                    // Паралельно офіційний запит на сервер для списання патронів/таймера
                    RequestFireServerRpc();
                }
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestFireServerRpc(NetworkConnection sender = null)
        {
            if (!IsServer || sender == null)
            {
                return;
            }

            if (_isReloading.Value || _ammoLeft.Value <= 0)
            {
                return;
            }

            _ammoLeft.Value = Mathf.Max(0, _ammoLeft.Value - 1);
            StartServerReloadTimer();

            // Підтверджуємо власнику (без спавну візуалу тут!)
            FireApprovedTargetRpc(sender);
            onShot?.Invoke();
        }

        [TargetRpc]
        private void FireApprovedTargetRpc(NetworkConnection conn)
        {
            // НІЯКОГО Predict тут — уникнення дубля.
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

            _crosshair.ammoLeftText.text = Mathf.Max(0, _ammoLeft.Value).ToString();

            if (_ammoLeft.Value <= 0 && !_isReloading.Value)
            {
                _crosshair.fillImage.fillAmount = 0f;
                _crosshair.reloadText.text = "EMPTY";
                return;
            }

            if (_isReloading.Value)
            {
                float t = reloadTime > 0.0001f
                    ? Mathf.Clamp01(1f - (_reloadRemain.Value / reloadTime))
                    : 1f;

                _crosshair.fillImage.fillAmount = t;
                _crosshair.reloadText.text = $"{Mathf.Max(0f, _reloadRemain.Value):0.0}s";
                return;
            }

            _crosshair.fillImage.fillAmount = 1f;
            _crosshair.reloadText.text = "READY";
        }
    }
}
