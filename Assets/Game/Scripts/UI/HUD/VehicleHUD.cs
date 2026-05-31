using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Diagnostics;
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
        [SerializeField, Min(0)] private int floatingTextPoolPrewarmCount = 8;
        [SerializeField, Min(1)] private int floatingTextPoolMaxInactive = 32;

        private float _nextTeamColorRefreshTime;
        private bool _subscribedToLocalPlayer;
        private bool _subscribedToHealth;
        private Vector3 _baseLocalScale;
        private bool _hasBaseLocalScale;
        private GameplayRuntimeSettings _runtimeSettings;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            _nextTeamColorRefreshTime = 0f;
            TrySubscribeHealth();
            ApplyHpColor();
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
            ApplyHpColor();
        }

        private void OnDisable()
        {
            UnsubscribeFromLocalPlayerChange();
            ResetDistanceScale();
        }
        
        private void Start()
        {
            TrySubscribeHealth();
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
            vehicleRoot.health.onDeath.AddListener(OnDeath);
            _subscribedToHealth = true;
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
                vehicleRoot.health.onDeath.RemoveListener(OnDeath);
            }

            _subscribedToHealth = false;
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
            using (ProfileScope.Measure("Client.UI.VehicleHUD.LateUpdate", DiagnosticsCategories.Ui))
            {
                RefreshHpColorIfNeeded();

                Camera camera = ResolveCamera();
                if (camera != null)
                {
                    transform.forward = camera.transform.forward;
                }

                ApplyDistanceScale(camera);
            }
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
        }

        private enum VehicleHudRelation
        {
            Ally,
            Enemy
        }
    }
}
