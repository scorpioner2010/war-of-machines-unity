using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Client;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class VehicleHUD : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;
        public FloatingDamageText floatingTextPrefab;

        private float _nextTeamColorRefreshTime;
        private bool _subscribedToLocalPlayer;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            _nextTeamColorRefreshTime = 0f;
            ApplyHpColor();
        }

        private void OnEnable()
        {
            SubscribeToLocalPlayerChange();
            _nextTeamColorRefreshTime = 0f;
            ApplyHpColor();
        }

        private void OnDisable()
        {
            UnsubscribeFromLocalPlayerChange();
        }
        
        private void Start()
        {
            if (vehicleRoot == null || vehicleRoot.health == null)
            {
                enabled = false;
                return;
            }

            vehicleRoot.health.OnDamaged += OnDamaged;
            vehicleRoot.health.onDeath.AddListener(OnDeath);
            ApplyHpColor();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLocalPlayerChange();

            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnDamaged -= OnDamaged;
                vehicleRoot.health.onDeath.RemoveListener(OnDeath);
            }
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            float cur01 = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            if (hpView != null)
            {
                hpView.fillAmount = cur01;
            }

            ShowFloatingText(damageAmount);
        }

        private void OnDeath()
        {
            gameObject.SetActive(false);
        }
        
        private void ShowFloatingText(float dmg)
        {
            if (floatingTextPrefab == null)
            {
                return;
            }

            FloatingDamageText text = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
            string damage = Mathf.RoundToInt(dmg).ToString();
            text.SetText(damage);
        }

        public void SetNick(string nick)
        {
            if (nickName != null)
            {
                nickName.text = nick;
            }

            ApplyHpColor();
            gameObject.SetActive(true);
        }
        
        public void SetCamera(Camera cam)
        {
            _mainCamera = cam;
        }

        public void SetActiveView(bool active)
        {
            if (nickName != null)
            {
                nickName.gameObject.SetActive(active);
            }
        }
        
        private void LateUpdate()
        {
            RefreshHpColorIfNeeded();

            if (_mainCamera != null)
            {
                transform.forward = _mainCamera.transform.forward;
            }
        }

        private void RefreshHpColorIfNeeded()
        {
            if (Time.unscaledTime < _nextTeamColorRefreshTime)
            {
                return;
            }

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            _nextTeamColorRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, settings.hpTeamColorRefreshInterval);
            ApplyHpColor();
        }

        private void ApplyHpColor()
        {
            if (hpView == null)
            {
                return;
            }

            VehicleHudRelation relation = GetRelationToLocalPlayer();
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            hpView.color = relation == VehicleHudRelation.Ally ? settings.alliedHpColor : settings.enemyHpColor;
        }

        private VehicleHudRelation GetRelationToLocalPlayer()
        {
            VehicleRoot localPlayer = VehicleRoot.LocalPlayerVehicle;
            if (vehicleRoot == null || localPlayer == null)
            {
                return VehicleHudRelation.Enemy;
            }

            if (vehicleRoot == localPlayer)
            {
                return VehicleHudRelation.Ally;
            }

            if (vehicleRoot.characterInit == null || localPlayer.characterInit == null)
            {
                return VehicleHudRelation.Enemy;
            }

            MatchTeam localTeam = localPlayer.characterInit.Team.Value;
            MatchTeam targetTeam = vehicleRoot.characterInit.Team.Value;
            if (localTeam == MatchTeam.None || targetTeam == MatchTeam.None)
            {
                return VehicleHudRelation.Enemy;
            }

            return localTeam == targetTeam ? VehicleHudRelation.Ally : VehicleHudRelation.Enemy;
        }

        private void SubscribeToLocalPlayerChange()
        {
            if (_subscribedToLocalPlayer)
            {
                return;
            }

            VehicleRoot.LocalPlayerVehicleChanged += OnLocalPlayerVehicleChanged;
            _subscribedToLocalPlayer = true;
        }

        private void UnsubscribeFromLocalPlayerChange()
        {
            if (!_subscribedToLocalPlayer)
            {
                return;
            }

            VehicleRoot.LocalPlayerVehicleChanged -= OnLocalPlayerVehicleChanged;
            _subscribedToLocalPlayer = false;
        }

        private void OnLocalPlayerVehicleChanged(VehicleRoot vehicle)
        {
            _nextTeamColorRefreshTime = 0f;
            ApplyHpColor();
        }

        private enum VehicleHudRelation
        {
            Ally,
            Enemy
        }
    }
}
