using TMPro;
using Game.Scripts.Client;
using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GunCrosshair : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform canvasRect;
        public RectTransform crosshair;
        public RectTransform serverCrosshair;
        public Image  fillImage;
        public TMP_Text reloadText; 
        public TMP_Text ammoLeftText;
        public TMP_Text aimStatusText;
        public TMP_Text penetrationStatusText;
        [SerializeField] private GameObject regularHudRoot;
        [SerializeField] private GameObject sniperOverlay;
        [SerializeField] private GameObject sniperReticleOnlyRoot;

        [Header("Faction Reticles")]
        [SerializeField] private GameObject ironAllianceCrosshairRoot;
        [SerializeField] private GameObject novaSyndicateCrosshairRoot;
        [SerializeField] private RectTransform ironAllianceReticle;
        [SerializeField] private RectTransform novaSyndicateReticle;
        [SerializeField] private RectTransform ironAllianceServerReticle;
        [SerializeField] private RectTransform novaSyndicateServerReticle;

        [Header("Faction Weapon Parameters")]
        [SerializeField] private Image ironAllianceFillImage;
        [SerializeField] private Image novaSyndicateFillImage;
        [SerializeField] private TMP_Text ironAllianceReloadText;
        [SerializeField] private TMP_Text novaSyndicateReloadText;
        [SerializeField] private TMP_Text ironAllianceAmmoLeftText;
        [SerializeField] private TMP_Text novaSyndicateAmmoLeftText;
        [SerializeField] private TMP_Text ironAllianceAimStatusText;
        [SerializeField] private TMP_Text novaSyndicateAimStatusText;
        [SerializeField] private TMP_Text ironAlliancePenetrationStatusText;
        [SerializeField] private TMP_Text novaSyndicatePenetrationStatusText;

        [Header("Nova Charge Indicators")]
        [SerializeField] private Image[] novaChargeLightElements = System.Array.Empty<Image>();

        private Vector2 _crosshairBaseSize;
        private Vector2 _serverCrosshairBaseSize;
        private bool _crosshairBaseSizeCached;
        private bool _serverCrosshairBaseSizeCached;
        private float _crosshairDiameter;
        private float _serverCrosshairDiameter;
        private bool _crosshairDiameterCached;
        private bool _serverCrosshairDiameterCached;
        private GameObject[] _sniperHiddenObjects;
        private bool[] _sniperHiddenObjectStates;
        private Transform _sniperHiddenRoot;
        private int _sniperHiddenRootChildCount = -1;
        private bool _sniperModeApplied;
        private readonly char[] _aimStatusBuffer = new char[48];
        private int _lastAimStatusPercent = int.MinValue;
        private bool _lastAimStatusReady;
        private bool _hasAimStatus;
        private string _lastAimStatusPrefix;
        private string _lastAimReadyText;
        private float _lastChargeProgress = -1f;
        private bool _serverCrosshairEnabled;
        private CameraController _spectatorCameraController;
        private VehicleRoot _spectatorTarget;
        private bool _activeFactionIsNova;
        private float _spectatorReloadRemainAtSync;
        private float _spectatorReloadSyncTime;
        private bool _spectatorReloadSyncInitialized;
        private bool _spectatorReloading;
        private int _spectatorAmmo = int.MinValue;
        private int _spectatorReloadTenths = int.MinValue;
        private bool _spectatorReloadStateInitialized;
        private Image _ironAllianceFillFallback;
        private TMP_Text _ironAllianceReloadFallback;
        private TMP_Text _ironAllianceAmmoLeftFallback;
        private TMP_Text _ironAllianceAimStatusFallback;
        private TMP_Text _ironAlliancePenetrationStatusFallback;
        private const string DefaultPenetrationStatusText = "PENETRATION STATUS";
        public const string PenetrationStatusGlyph = "〰〰〰";
        private string _lastPenetrationStatus = DefaultPenetrationStatusText;
        private const float ChargeFillSmoothingSpeed = 14f;

        private void Awake()
        {
            _ironAllianceFillFallback = fillImage;
            _ironAllianceReloadFallback = reloadText;
            _ironAllianceAmmoLeftFallback = ammoLeftText;
            _ironAllianceAimStatusFallback = aimStatusText;
            _ironAlliancePenetrationStatusFallback = penetrationStatusText;
            ResolveCanvasReference();
            ApplyPenetrationStatusToActiveTarget();
        }

        private void OnEnable()
        {
            Singleton<GunCrosshair>.Register(this);
            ResolveCanvasReference();
            ApplyPenetrationStatusToActiveTarget();
            ClientGameplaySettings.ServerCrosshairChanged += OnServerCrosshairSettingChanged;
            ApplyServerCrosshairSetting(ClientGameplaySettings.ServerCrosshairEnabled);
            ApplySniperPresentation(false);
            VehicleRoot.LocalPlayerVehicleChanged += OnLocalPlayerVehicleChanged;
            BindSpectatorCamera(VehicleRoot.LocalPlayerVehicle);
        }

        private void OnDisable()
        {
            if (_sniperModeApplied)
            {
                ApplySniperPresentation(false);
            }

            ClientGameplaySettings.ServerCrosshairChanged -= OnServerCrosshairSettingChanged;
            VehicleRoot.LocalPlayerVehicleChanged -= OnLocalPlayerVehicleChanged;
            UnbindSpectatorCamera();
            Singleton<GunCrosshair>.Unregister(this);
        }

        private void LateUpdate()
        {
            EnsureSpectatorCameraBinding();
            if (_spectatorCameraController == null || !_spectatorCameraController.IsDeathSpectatorActive)
            {
                return;
            }

            VehicleRoot target = _spectatorCameraController.SpectatorTarget;
            if (_spectatorTarget != target)
            {
                BindSpectatorTarget(target);
            }

            ApplySpectatorWeaponHud();
            ApplyFactionCrosshairVisibility();
        }

        private void OnLocalPlayerVehicleChanged(VehicleRoot vehicle)
        {
            BindSpectatorCamera(vehicle);
        }

        private void EnsureSpectatorCameraBinding()
        {
            VehicleRoot localVehicle = VehicleRoot.LocalPlayerVehicle;
            CameraController nextController = localVehicle != null ? localVehicle.cameraController : null;
            if (_spectatorCameraController != nextController)
            {
                BindSpectatorCamera(localVehicle);
            }
        }

        private void BindSpectatorCamera(VehicleRoot localVehicle)
        {
            CameraController nextController = localVehicle != null ? localVehicle.cameraController : null;
            if (_spectatorCameraController == nextController)
            {
                return;
            }

            UnbindSpectatorCamera();
            _spectatorCameraController = nextController;
            if (_spectatorCameraController == null)
            {
                return;
            }

            _spectatorCameraController.SpectatorTargetChanged += OnSpectatorTargetChanged;
            if (_spectatorCameraController.IsDeathSpectatorActive)
            {
                BindSpectatorTarget(_spectatorCameraController.SpectatorTarget);
            }
        }

        private void UnbindSpectatorCamera()
        {
            if (_spectatorCameraController != null)
            {
                _spectatorCameraController.SpectatorTargetChanged -= OnSpectatorTargetChanged;
            }

            _spectatorCameraController = null;
            BindSpectatorTarget(null);
        }

        private void OnSpectatorTargetChanged(VehicleRoot target)
        {
            BindSpectatorTarget(target);
        }

        private void BindSpectatorTarget(VehicleRoot target)
        {
            _spectatorTarget = target;
            _spectatorReloadSyncInitialized = false;
            _spectatorReloading = false;
            _spectatorAmmo = int.MinValue;
            _spectatorReloadTenths = int.MinValue;
            _spectatorReloadStateInitialized = false;
            ApplyFactionCrosshairVisibility();
        }

        private void ApplySpectatorWeaponHud()
        {
            ApplyFactionCrosshairVisibility();
            if (_spectatorTarget == null
                || _spectatorTarget.weaponReloadController == null
                || _spectatorTarget.shooterNet == null)
            {
                return;
            }

            WeaponReloadController reloadController = _spectatorTarget.weaponReloadController;
            NetworkWeaponShooter shooter = _spectatorTarget.shooterNet;
            bool factionChanged = SetFaction(reloadController.ClientFaction);
            int ammoLeft = reloadController.ClientAmmoLeft;
            bool isReloading = reloadController.ClientIsReloading;
            float reloadRemain = GetSpectatorReloadRemain(reloadController, isReloading);
            SetWeaponChargeProgress(GetSpectatorReloadProgress(reloadController, ammoLeft, isReloading, reloadRemain));
            ApplySpectatorReloadText(ammoLeft, isReloading, reloadRemain, factionChanged);
            ApplySpectatorAmmo(ammoLeft);

            float minDispersion = shooter.MinDispersionDeg;
            float maxDispersion = Mathf.Max(minDispersion, shooter.MaxDispersionDeg);
            float currentDispersion = shooter.ClientCurrentDispersionDeg;
            SetAimStatus(currentDispersion, minDispersion, maxDispersion);

            GunDispersionGlobalSettings globalDispersion = RemoteServerSettings.GunDispersion;
            float uiZoom01 = _spectatorCameraController.GetAimUiZoom01();
            CameraZoomProfile zoomProfile = _spectatorCameraController.CurrentZoomProfile;
            float aimingCircleDiameter = zoomProfile != null ? zoomProfile.aimingCircleDiameter : float.NaN;
            float diameter = globalDispersion.GetUiDiameter(
                currentDispersion,
                minDispersion,
                uiZoom01,
                aimingCircleDiameter);
            SetAimingDiameters(diameter, diameter, globalDispersion.uiDiameterLerpSpeed);
        }

        private float GetSpectatorReloadRemain(WeaponReloadController reloadController, bool isReloading)
        {
            float syncedRemain = reloadController.ClientReloadRemain;
            if (!_spectatorReloadSyncInitialized
                || _spectatorReloading != isReloading
                || Mathf.Abs(syncedRemain - _spectatorReloadRemainAtSync) > 0.05f)
            {
                _spectatorReloadRemainAtSync = syncedRemain;
                _spectatorReloadSyncTime = Time.unscaledTime;
                _spectatorReloadSyncInitialized = true;
                _spectatorReloading = isReloading;
            }

            if (!isReloading)
            {
                return 0f;
            }

            float elapsed = Mathf.Max(0f, Time.unscaledTime - _spectatorReloadSyncTime);
            return Mathf.Max(0f, _spectatorReloadRemainAtSync - elapsed);
        }

        private static float GetSpectatorReloadProgress(
            WeaponReloadController reloadController,
            int ammoLeft,
            bool isReloading,
            float reloadRemain)
        {
            if (!isReloading)
            {
                return ammoLeft > 0 ? 1f : 0f;
            }

            return Mathf.Clamp01(1f - reloadRemain / reloadController.ClientReloadTime);
        }

        private void ApplySpectatorAmmo(int ammoLeft)
        {
            if (_spectatorAmmo == ammoLeft || ammoLeftText == null)
            {
                return;
            }

            _spectatorAmmo = ammoLeft;
            ammoLeftText.SetText("{0}", ammoLeft);
        }

        private void ApplySpectatorReloadText(int ammoLeft, bool isReloading, float reloadRemain, bool force)
        {
            if (reloadText == null)
            {
                return;
            }

            int reloadTenths = isReloading ? Mathf.CeilToInt(Mathf.Max(0f, reloadRemain) * 10f) : 0;
            if (!force
                && _spectatorReloadStateInitialized
                && _spectatorReloading == isReloading
                && _spectatorReloadTenths == reloadTenths
                && ((ammoLeft > 0) == (_spectatorAmmo > 0)))
            {
                return;
            }

            _spectatorReloadStateInitialized = true;
            _spectatorReloadTenths = reloadTenths;
            if (ammoLeft <= 0 && !isReloading)
            {
                reloadText.text = "EMPTY";
                return;
            }

            if (!isReloading)
            {
                reloadText.text = "READY";
                return;
            }

            reloadText.SetText("{0:0.0}s", reloadTenths * 0.1f);
        }

        public void SetAimingDiameters(float localDiameter, float serverDiameter)
        {
            SetAimingDiameters(localDiameter, serverDiameter, 0f);
        }

        public void SetAimingDiameters(float localDiameter, float serverDiameter, float smoothingSpeed)
        {
            ApplyAimingDiameter(
                crosshair,
                localDiameter,
                smoothingSpeed,
                ref _crosshairBaseSize,
                ref _crosshairBaseSizeCached,
                ref _crosshairDiameter,
                ref _crosshairDiameterCached);
            ApplyAimingDiameter(
                serverCrosshair,
                serverDiameter,
                smoothingSpeed,
                ref _serverCrosshairBaseSize,
                ref _serverCrosshairBaseSizeCached,
                ref _serverCrosshairDiameter,
                ref _serverCrosshairDiameterCached);
        }

        public bool SetFaction(RobotFaction faction)
        {
            bool useNovaReticle = faction == RobotFaction.NovaSyndicate;
            _activeFactionIsNova = useNovaReticle;
            RectTransform nextReticle = useNovaReticle ? novaSyndicateReticle : ironAllianceReticle;
            RectTransform nextServerReticle = useNovaReticle
                ? novaSyndicateServerReticle
                : ironAllianceServerReticle;
            Image nextFillImage = useNovaReticle ? novaSyndicateFillImage : ironAllianceFillImage;
            TMP_Text nextReloadText = useNovaReticle ? novaSyndicateReloadText : ironAllianceReloadText;
            TMP_Text nextAmmoLeftText = useNovaReticle ? novaSyndicateAmmoLeftText : ironAllianceAmmoLeftText;
            TMP_Text nextAimStatusText = useNovaReticle ? novaSyndicateAimStatusText : ironAllianceAimStatusText;
            TMP_Text nextPenetrationStatusText = useNovaReticle
                ? novaSyndicatePenetrationStatusText
                : ironAlliancePenetrationStatusText;

            if (nextReticle == null)
            {
                nextReticle = useNovaReticle ? null : crosshair;
            }

            if (nextServerReticle == null)
            {
                nextServerReticle = useNovaReticle ? null : serverCrosshair;
            }

            if (!useNovaReticle)
            {
                nextFillImage ??= _ironAllianceFillFallback;
                nextReloadText ??= _ironAllianceReloadFallback;
                nextAmmoLeftText ??= _ironAllianceAmmoLeftFallback;
                nextAimStatusText ??= _ironAllianceAimStatusFallback;
                nextPenetrationStatusText ??= _ironAlliancePenetrationStatusFallback;
            }

            ApplyFactionCrosshairVisibility();

            bool bindingsChanged = crosshair != nextReticle
                                   || serverCrosshair != nextServerReticle
                                   || fillImage != nextFillImage
                                   || reloadText != nextReloadText
                                   || ammoLeftText != nextAmmoLeftText
                                   || aimStatusText != nextAimStatusText
                                   || penetrationStatusText != nextPenetrationStatusText;
            if (!bindingsChanged)
            {
                return false;
            }

            crosshair = nextReticle;
            serverCrosshair = nextServerReticle;
            fillImage = nextFillImage;
            reloadText = nextReloadText;
            ammoLeftText = nextAmmoLeftText;
            aimStatusText = nextAimStatusText;
            penetrationStatusText = nextPenetrationStatusText;
            ApplyServerCrosshairSetting(_serverCrosshairEnabled);
            _crosshairBaseSizeCached = false;
            _serverCrosshairBaseSizeCached = false;
            _crosshairDiameterCached = false;
            _serverCrosshairDiameterCached = false;
            _lastChargeProgress = -1f;
            _hasAimStatus = false;
            _lastAimStatusReady = false;
            _lastAimStatusPercent = int.MinValue;
            _lastAimStatusPrefix = null;
            _lastAimReadyText = null;
            ApplyPenetrationStatusToActiveTarget();
            if (_sniperModeApplied)
            {
                ClearSniperHiddenObjects();
                ApplySniperHudVisibility(true);
            }

            return true;
        }

        public Transform PenetrationStatusTextTransform => penetrationStatusText != null
            ? penetrationStatusText.transform
            : null;

        public void SetPenetrationStatus(string status)
        {
            _lastPenetrationStatus = string.IsNullOrWhiteSpace(status)
                ? DefaultPenetrationStatusText
                : status;
            ApplyPenetrationStatusToActiveTarget();
        }

        private void ApplyPenetrationStatusToActiveTarget()
        {
            if (penetrationStatusText != null && penetrationStatusText.text != _lastPenetrationStatus)
            {
                penetrationStatusText.text = _lastPenetrationStatus;
            }
        }

        public void SetWeaponChargeProgress(float progress)
        {
            float safeProgress = Mathf.Clamp01(progress);
            if (safeProgress >= 0.999f)
            {
                safeProgress = 1f;
            }

            ApplyChargeFill(safeProgress);

            if (Mathf.Abs(_lastChargeProgress - safeProgress) <= 0.001f)
            {
                return;
            }

            _lastChargeProgress = safeProgress;
            if (novaChargeLightElements == null)
            {
                return;
            }

            for (int i = 0; i < novaChargeLightElements.Length; i++)
            {
                Image lightElement = novaChargeLightElements[i];
                if (lightElement == null)
                {
                    continue;
                }

                Color color = lightElement.color;
                color.a = safeProgress;
                lightElement.color = color;
            }
        }

        private void ApplyChargeFill(float targetProgress)
        {
            if (fillImage == null)
            {
                return;
            }

            float currentProgress = Mathf.Clamp01(fillImage.fillAmount);
            if (targetProgress <= currentProgress)
            {
                fillImage.fillAmount = targetProgress;
                return;
            }

            float smoothing = 1f - Mathf.Exp(-ChargeFillSmoothingSpeed * Time.unscaledDeltaTime);
            float smoothedProgress = Mathf.Lerp(currentProgress, targetProgress, smoothing);
            fillImage.fillAmount = Mathf.Abs(smoothedProgress - targetProgress) <= 0.001f
                ? targetProgress
                : smoothedProgress;
        }

        private static void ApplyAimingDiameter(
            RectTransform target,
            float diameter,
            float smoothingSpeed,
            ref Vector2 baseSize,
            ref bool baseSizeCached,
            ref float currentDiameter,
            ref bool currentDiameterCached)
        {
            if (target == null)
            {
                return;
            }

            if (!baseSizeCached)
            {
                baseSize = target.sizeDelta;
                baseSizeCached = true;
            }

            float safeDiameter = Mathf.Max(1f, diameter);
            if (!currentDiameterCached || smoothingSpeed <= 0f)
            {
                currentDiameter = safeDiameter;
                currentDiameterCached = true;
            }
            else
            {
                float speed = Mathf.Max(0f, smoothingSpeed);
                float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
                currentDiameter = Mathf.Lerp(currentDiameter, safeDiameter, t);
                if (Mathf.Abs(currentDiameter - safeDiameter) <= 0.1f)
                {
                    currentDiameter = safeDiameter;
                }
            }

            if (baseSize.x > 0f && baseSize.y > 0f)
            {
                target.sizeDelta = new Vector2(currentDiameter, currentDiameter);
                return;
            }

            float baseDiameter = Mathf.Max(1f, Mathf.Max(Mathf.Abs(baseSize.x), Mathf.Abs(baseSize.y)));
            float scale = currentDiameter / baseDiameter;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnServerCrosshairSettingChanged(bool enabled)
        {
            ApplyServerCrosshairSetting(enabled);
        }

        private void ApplyServerCrosshairSetting(bool enabled)
        {
            _serverCrosshairEnabled = enabled;
            if (serverCrosshair != null)
            {
                serverCrosshair.gameObject.SetActive(enabled);
            }
        }

        public void SetAimStatus(float currentDispersionDeg, float minDispersionDeg, float maxDispersionDeg)
        {
            if (aimStatusText == null)
            {
                return;
            }

            float min = Mathf.Max(0f, minDispersionDeg);
            float max = Mathf.Max(min + 0.0001f, maxDispersionDeg);
            float t = Mathf.InverseLerp(max, min, Mathf.Clamp(currentDispersionDeg, min, max));
            int percent = Mathf.RoundToInt(t * 100f);
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();

            if (percent >= 99)
            {
                if (!_hasAimStatus || !_lastAimStatusReady || _lastAimReadyText != settings.aimReadyText)
                {
                    aimStatusText.text = settings.aimReadyText;
                    _hasAimStatus = true;
                    _lastAimStatusReady = true;
                    _lastAimStatusPercent = percent;
                    _lastAimReadyText = settings.aimReadyText;
                }

                if (aimStatusText.color != settings.aimReadyColor)
                {
                    aimStatusText.color = settings.aimReadyColor;
                }
                return;
            }

            string prefix = settings.aimProgressTextPrefix;
            if (!_hasAimStatus || _lastAimStatusReady || _lastAimStatusPercent != percent || _lastAimStatusPrefix != prefix)
            {
                SetAimProgressText(prefix, percent);
                _hasAimStatus = true;
                _lastAimStatusReady = false;
                _lastAimStatusPercent = percent;
                _lastAimStatusPrefix = prefix;
            }

            if (aimStatusText.color != settings.aimProgressColor)
            {
                aimStatusText.color = settings.aimProgressColor;
            }
        }

        private void SetAimProgressText(string prefix, int percent)
        {
            if (aimStatusText == null)
            {
                return;
            }

            int length = 0;
            if (!string.IsNullOrEmpty(prefix))
            {
                int prefixLimit = Mathf.Min(prefix.Length, _aimStatusBuffer.Length - 5);
                for (int i = 0; i < prefixLimit; i++)
                {
                    _aimStatusBuffer[length] = prefix[i];
                    length++;
                }
            }

            AppendPositiveInt(_aimStatusBuffer, ref length, Mathf.Clamp(percent, 0, 100));
            if (length < _aimStatusBuffer.Length)
            {
                _aimStatusBuffer[length] = '%';
                length++;
            }

            aimStatusText.SetCharArray(_aimStatusBuffer, 0, length);
        }

        private static void AppendPositiveInt(char[] buffer, ref int length, int value)
        {
            if (buffer == null || length >= buffer.Length)
            {
                return;
            }

            if (value >= 100 && length + 3 <= buffer.Length)
            {
                buffer[length] = (char)('0' + value / 100);
                length++;
                value %= 100;
                buffer[length] = (char)('0' + value / 10);
                length++;
                buffer[length] = (char)('0' + value % 10);
                length++;
                return;
            }

            if (value >= 10 && length + 2 <= buffer.Length)
            {
                buffer[length] = (char)('0' + value / 10);
                length++;
                buffer[length] = (char)('0' + value % 10);
                length++;
                return;
            }

            if (length < buffer.Length)
            {
                buffer[length] = (char)('0' + value);
                length++;
            }
        }

        public void SetSniperMode(bool enabled)
        {
            ApplySniperPresentation(enabled);
        }

        private void ApplySniperPresentation(bool sniperMode)
        {
            if (_sniperModeApplied == sniperMode)
            {
                ApplySniperPersistentElements(sniperMode);
                return;
            }

            _sniperModeApplied = sniperMode;
            ApplySniperHudVisibility(sniperMode);
            ApplySniperPersistentElements(sniperMode);
        }

        private void ApplySniperPersistentElements(bool sniperMode)
        {
            if (sniperOverlay != null)
            {
                sniperOverlay.SetActive(sniperMode);
            }

            if (sniperReticleOnlyRoot != null)
            {
                sniperReticleOnlyRoot.SetActive(sniperMode);
            }

            if (crosshair != null)
            {
                crosshair.gameObject.SetActive(true);
            }

            ApplyFactionCrosshairVisibility();
        }

        private void ApplyFactionCrosshairVisibility()
        {
            bool hideCrosshair = ShouldHideSpectatorCrosshair();
            if (hideCrosshair)
            {
                if (ironAllianceCrosshairRoot != null)
                {
                    ironAllianceCrosshairRoot.SetActive(false);
                }

                if (novaSyndicateCrosshairRoot != null)
                {
                    novaSyndicateCrosshairRoot.SetActive(false);
                }

                return;
            }

            if (ironAllianceCrosshairRoot != null)
            {
                bool shouldEnableIronAlliance = !_activeFactionIsNova;
                if (ironAllianceCrosshairRoot.activeSelf != shouldEnableIronAlliance)
                {
                    ironAllianceCrosshairRoot.SetActive(shouldEnableIronAlliance);
                }
            }

            if (novaSyndicateCrosshairRoot != null)
            {
                bool shouldEnableNovaSyndicate = _activeFactionIsNova;
                if (novaSyndicateCrosshairRoot.activeSelf != shouldEnableNovaSyndicate)
                {
                    novaSyndicateCrosshairRoot.SetActive(shouldEnableNovaSyndicate);
                }
            }
        }

        private bool ShouldHideSpectatorCrosshair()
        {
            if (_spectatorCameraController == null
                || !_spectatorCameraController.IsDeathSpectatorActive
                || _spectatorTarget == null
                || _spectatorTarget != _spectatorCameraController.vehicleRoot
                || _spectatorTarget.health == null)
            {
                return false;
            }

            return ShouldHideSpectatorCrosshairState(
                _spectatorCameraController.IsDeathSpectatorActive,
                _spectatorTarget == _spectatorCameraController.vehicleRoot,
                _spectatorTarget.health.IsDead);
        }

        private static bool ShouldHideSpectatorCrosshairState(
            bool deathSpectatorActive,
            bool isLocalTarget,
            bool targetIsDead)
        {
            return deathSpectatorActive && isLocalTarget && targetIsDead;
        }

        private void ApplySniperHudVisibility(bool sniperMode)
        {
            if (!sniperMode)
            {
                RestoreSniperHiddenObjects();
                ClearSniperHiddenObjects();
                return;
            }

            Transform root = GetSniperHiddenRoot();
            if (root == null)
            {
                return;
            }

            EnsureSniperHiddenObjects(root);
            if (_sniperHiddenObjects == null || _sniperHiddenObjectStates == null)
            {
                return;
            }

            CaptureSniperHiddenObjectStates();

            for (int i = 0; i < _sniperHiddenObjects.Length; i++)
            {
                GameObject target = _sniperHiddenObjects[i];
                if (target == null)
                {
                    continue;
                }

                target.SetActive(false);
            }
        }

        private Transform GetSniperHiddenRoot()
        {
            if (regularHudRoot != null)
            {
                return regularHudRoot.transform;
            }

            if (crosshair == null || crosshair.parent == null)
            {
                return null;
            }

            return crosshair.parent.parent;
        }

        private void EnsureSniperHiddenObjects(Transform root)
        {
            if (_sniperHiddenObjects != null && _sniperHiddenRoot == root && _sniperHiddenRootChildCount == root.childCount)
            {
                return;
            }

            _sniperHiddenRoot = root;
            _sniperHiddenRootChildCount = root.childCount;

            int count = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!ShouldKeepVisibleInSniper(child))
                {
                    count++;
                }
            }

            _sniperHiddenObjects = new GameObject[count];
            _sniperHiddenObjectStates = new bool[count];

            int index = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (ShouldKeepVisibleInSniper(child))
                {
                    continue;
                }

                _sniperHiddenObjects[index] = child.gameObject;
                index++;
            }
        }

        private void CaptureSniperHiddenObjectStates()
        {
            if (_sniperHiddenObjects == null || _sniperHiddenObjectStates == null)
            {
                return;
            }

            for (int i = 0; i < _sniperHiddenObjects.Length; i++)
            {
                GameObject target = _sniperHiddenObjects[i];
                _sniperHiddenObjectStates[i] = target != null && target.activeSelf;
            }
        }

        private void RestoreSniperHiddenObjects()
        {
            if (_sniperHiddenObjects == null || _sniperHiddenObjectStates == null)
            {
                return;
            }

            for (int i = 0; i < _sniperHiddenObjects.Length; i++)
            {
                GameObject target = _sniperHiddenObjects[i];
                if (target == null)
                {
                    continue;
                }

                target.SetActive(_sniperHiddenObjectStates[i]);
            }
        }

        private void ClearSniperHiddenObjects()
        {
            _sniperHiddenObjects = null;
            _sniperHiddenObjectStates = null;
            _sniperHiddenRoot = null;
            _sniperHiddenRootChildCount = -1;
        }

        private bool ShouldKeepVisibleInSniper(Transform child)
        {
            if (child == null)
            {
                return true;
            }

            Transform crosshairGroup = crosshair != null ? crosshair.parent : null;
            if (IsSameOrParentOf(child, crosshairGroup))
            {
                return true;
            }

            if (IsSameOrParentOf(child, serverCrosshair))
            {
                return true;
            }

            if (IsSameOrParentOf(child, fillImage != null ? fillImage.transform : null))
            {
                return true;
            }

            if (IsSameOrParentOf(child, reloadText != null ? reloadText.transform : null))
            {
                return true;
            }

            if (IsSameOrParentOf(child, ammoLeftText != null ? ammoLeftText.transform : null))
            {
                return true;
            }

            Transform aimStatusTransform = aimStatusText != null ? aimStatusText.transform : null;
            if (IsSameOrParentOf(child, aimStatusTransform))
            {
                return true;
            }

            Transform penetrationStatusTransform = penetrationStatusText != null
                ? penetrationStatusText.transform
                : null;
            if (IsSameOrParentOf(child, penetrationStatusTransform))
            {
                return true;
            }

            Transform overlayTransform = sniperOverlay != null ? sniperOverlay.transform : null;
            if (IsSameOrParentOf(child, overlayTransform))
            {
                return true;
            }

            Transform reticleRootTransform = sniperReticleOnlyRoot != null ? sniperReticleOnlyRoot.transform : null;
            if (IsSameOrParentOf(child, reticleRootTransform))
            {
                return true;
            }

            if (GameplayGUI.In != null && IsSameOrParentOf(child, GameplayGUI.In.ShotResultTextTransform))
            {
                return true;
            }

            return child.name == "SniperScopeOverlay" || child.name == "SniperScopeOverlayController";
        }

        public Canvas ResolveCanvasReference(Canvas preferredCanvas = null)
        {
            if (preferredCanvas != null)
            {
                canvas = preferredCanvas;
            }

            if (canvas != null)
            {
                RectTransform resolvedCanvasRect = canvas.transform as RectTransform;
                if (resolvedCanvasRect != null)
                {
                    canvasRect = resolvedCanvasRect;
                }

                return canvas;
            }

            if (canvasRect == null)
            {
                canvasRect = ResolveRootRectTransform();
            }

            return canvas;
        }

        private RectTransform ResolveRootRectTransform()
        {
            RectTransform lastRect = null;
            Transform current = transform;
            while (current != null)
            {
                RectTransform currentRect = current as RectTransform;
                if (currentRect != null)
                {
                    lastRect = currentRect;
                }

                current = current.parent;
            }

            return lastRect;
        }

        private static bool IsSameOrParentOf(Transform possibleParent, Transform target)
        {
            if (possibleParent == null || target == null)
            {
                return false;
            }

            Transform current = target;
            while (current != null)
            {
                if (current == possibleParent)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
