using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleHUD : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        public VehicleRoot vehicleRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;
        public FloatingDamageText floatingTextPrefab;
        [SerializeField, Min(0)] private int floatingTextPoolPrewarmCount = 8;
        [SerializeField, Min(1)] private int floatingTextPoolMaxInactive = 32;

        private float _nextTeamColorRefreshTime;
        private bool _subscribedToLocalPlayer;
        private bool _subscribedToHealth;
        private Vector3 _baseLocalScale;
        private bool _hasBaseLocalScale;
        private bool _mapVisible = true;
        private bool _isDead;
        private GameplayRuntimeSettings _runtimeSettings;
        private string _fallbackDisplayName;

        public void SetVehicleRoot(VehicleRoot root)
        {
            if (vehicleRoot != root)
            {
                UnsubscribeFromHealth();
            }

            vehicleRoot = root;
            _nextTeamColorRefreshTime = 0f;
            TrySubscribeHealth();
            RefreshHpView();
            RefreshDisplayName();
            ApplyHpColor();
            ApplyRootVisibility();
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            RefreshDisplayName(stats);
        }

        private void Awake()
        {
            CaptureBaseLocalScale();
        }

        private void OnEnable()
        {
            CaptureBaseLocalScale();
            TrySubscribeHealth();
            SubscribeToLocalPlayerChange();
            PrewarmFloatingTextPool();
            _nextTeamColorRefreshTime = 0f;
            RefreshHpView();
            ApplyHpColor();
            ApplyRootVisibility();
        }

        private void OnDisable()
        {
            UnsubscribeFromLocalPlayerChange();
            ResetDistanceScale();
        }
        
        private void Start()
        {
            TrySubscribeHealth();
            RefreshHpView();
            ApplyHpColor();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLocalPlayerChange();
            UnsubscribeFromHealth();
        }

        private void TrySubscribeHealth()
        {
            if (_subscribedToHealth)
            {
                return;
            }

            if (vehicleRoot == null || vehicleRoot.health == null)
            {
                return;
            }

            vehicleRoot.health.OnDamaged += OnDamaged;
            vehicleRoot.health.OnHealthChanged += OnHealthChanged;
            vehicleRoot.health.onDeath.AddListener(OnDeath);
            _subscribedToHealth = true;
            RefreshHpView();
        }

        private void UnsubscribeFromHealth()
        {
            if (!_subscribedToHealth)
            {
                return;
            }

            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnDamaged -= OnDamaged;
                vehicleRoot.health.OnHealthChanged -= OnHealthChanged;
                vehicleRoot.health.onDeath.RemoveListener(OnDeath);
            }

            _subscribedToHealth = false;
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            OnHealthChanged(currentHealth, maxHealth);
            ShowFloatingText(damageAmount);
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            SetHpFill(currentHealth, maxHealth);
        }

        private void RefreshHpView()
        {
            if (vehicleRoot == null || vehicleRoot.health == null)
            {
                SetHpFill(1f, 1f);
                return;
            }

            SetHpFill(vehicleRoot.health.Current, vehicleRoot.health.MaxHealth);
        }

        private void SetHpFill(float currentHealth, float maxHealth)
        {
            if (hpView == null)
            {
                return;
            }

            hpView.fillAmount = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
        }

        private void OnDeath()
        {
            _isDead = true;
            ApplyRootVisibility();
        }
        
        private void ShowFloatingText(float dmg)
        {
            if (!ShouldDisplayRoot() || floatingTextPrefab == null)
            {
                return;
            }

            FloatingDamageText floatingText = FloatingDamageText.Rent(
                floatingTextPrefab,
                transform.position,
                Quaternion.identity,
                transform,
                floatingTextPoolMaxInactive);
            if (floatingText != null)
            {
                floatingText.SetDamage(Mathf.RoundToInt(dmg));
            }
        }

        public void SetNick(string nick)
        {
            _fallbackDisplayName = nick;
            RefreshDisplayName();

            ApplyHpColor();
            ApplyRootVisibility();
        }

        public void SetMapVisible(bool visible)
        {
            _mapVisible = visible;
            ApplyRootVisibility();
        }

        public void RefreshVisibility()
        {
            ApplyRootVisibility();
        }
        
        public void SetCamera(Camera cam)
        {
            _mainCamera = cam;
            AlignToCamera(cam);
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
            using (ProfileScope.Measure("Client.UI.VehicleHUD.LateUpdate", DiagnosticsCategories.Ui))
            {
                RefreshHpColorIfNeeded();

                Camera camera = ResolveCamera();
                AlignToCamera(camera);
            }
        }

        private void AlignToCamera(Camera camera)
        {
            if (camera != null)
            {
                transform.forward = camera.transform.forward;
            }

            ApplyDistanceScale(camera);
        }

        private void ApplyDistanceScale(Camera camera)
        {
            CaptureBaseLocalScale();

            if (camera == null)
            {
                ResetDistanceScale();
                return;
            }

            GameplayRuntimeSettings settings = GetRuntimeSettings();
            if (!settings.worldHpBarDistanceScaleEnabled)
            {
                ResetDistanceScale();
                return;
            }

            Transform cameraTransform = camera.transform;
            Vector3 offset = transform.position - cameraTransform.position;
            float distance = Mathf.Sqrt(offset.sqrMagnitude);
            float minDistance = settings.worldHpBarScaleMinDistance;
            float maxDistance = settings.worldHpBarScaleMaxDistance;
            if (maxDistance <= minDistance)
            {
                maxDistance = minDistance + 0.01f;
            }

            float minScale = settings.worldHpBarMinDistanceScale;
            float maxScale = settings.worldHpBarMaxDistanceScale;
            if (maxScale < minScale)
            {
                maxScale = minScale;
            }

            float distance01 = Mathf.InverseLerp(minDistance, maxDistance, distance);
            float scale = Mathf.Lerp(minScale, maxScale, distance01);
            transform.localScale = _baseLocalScale * scale;
        }

        private Camera ResolveCamera()
        {
            if (_mainCamera != null)
            {
                return _mainCamera;
            }

            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                _mainCamera = CameraSync.In.gameplayCamera;
                return _mainCamera;
            }

            return _mainCamera;
        }

        private void CaptureBaseLocalScale()
        {
            if (_hasBaseLocalScale)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _hasBaseLocalScale = true;
        }

        private void ResetDistanceScale()
        {
            if (!_hasBaseLocalScale)
            {
                return;
            }

            transform.localScale = _baseLocalScale;
        }

        private void PrewarmFloatingTextPool()
        {
            if (floatingTextPrefab == null || floatingTextPoolPrewarmCount <= 0)
            {
                return;
            }

            FloatingDamageText.Prewarm(floatingTextPrefab, floatingTextPoolPrewarmCount, floatingTextPoolMaxInactive);
        }

        private void RefreshHpColorIfNeeded()
        {
            if (Time.unscaledTime < _nextTeamColorRefreshTime)
            {
                return;
            }

            GameplayRuntimeSettings settings = RefreshRuntimeSettings();
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
            GameplayRuntimeSettings settings = GetRuntimeSettings();
            hpView.color = relation == VehicleHudRelation.Ally ? settings.alliedHpColor : settings.enemyHpColor;
        }

        private void RefreshDisplayName(VehicleRuntimeStats stats = null)
        {
            if (nickName == null)
            {
                return;
            }

            VehicleRuntimeStats resolvedStats = stats;
            if (resolvedStats == null && vehicleRoot != null)
            {
                resolvedStats = vehicleRoot.RuntimeStats;
            }

            if (resolvedStats != null)
            {
                if (!string.IsNullOrEmpty(resolvedStats.Name))
                {
                    nickName.text = resolvedStats.Name;
                    return;
                }

                if (!string.IsNullOrEmpty(resolvedStats.Code))
                {
                    nickName.text = resolvedStats.Code;
                    return;
                }
            }

            if (vehicleRoot != null && !string.IsNullOrEmpty(vehicleRoot.name))
            {
                nickName.text = vehicleRoot.name;
                return;
            }

            nickName.text = _fallbackDisplayName ?? string.Empty;
        }

        private GameplayRuntimeSettings GetRuntimeSettings()
        {
            if (_runtimeSettings == null)
            {
                return RefreshRuntimeSettings();
            }

            return _runtimeSettings;
        }

        private GameplayRuntimeSettings RefreshRuntimeSettings()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            return _runtimeSettings;
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
            return MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam)
                ? VehicleHudRelation.Ally
                : VehicleHudRelation.Enemy;
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
            ApplyRootVisibility();
        }

        private void ApplyRootVisibility()
        {
            gameObject.SetActive(ShouldDisplayRoot());
        }

        private bool ShouldDisplayRoot()
        {
            if (!_mapVisible || _isDead)
            {
                return false;
            }

            if (vehicleRoot == null || vehicleRoot.IsMenu)
            {
                return false;
            }

            return !vehicleRoot.IsOwner && vehicleRoot != VehicleRoot.LocalPlayerVehicle;
        }

        private enum VehicleHudRelation
        {
            Ally,
            Enemy
        }
    }
}
