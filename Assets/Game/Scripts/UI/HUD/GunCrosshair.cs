using TMPro;
using Game.Scripts.UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GunCrosshair : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform crosshair;
        public RectTransform serverCrosshair;
        public Image  fillImage;
        public TMP_Text reloadText; 
        public TMP_Text ammoLeftText;
        public TMP_Text aimStatusText;
        [SerializeField] private GameObject regularHudRoot;
        [SerializeField] private GameObject sniperOverlay;
        [SerializeField] private GameObject sniperReticleOnlyRoot;
        [SerializeField] private Color aimedColor = new Color(0.5f, 1f, 0.5f, 1f);
        [SerializeField] private Color aimingColor = new Color(1f, 0.86f, 0.2f, 1f);
        [SerializeField] private string aimedText = "AIM READY";
        [SerializeField] private string aimingTextPrefix = "AIM ";

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

        private void Awake()
        {
            ResolveCanvasReference();
        }

        private void OnEnable()
        {
            ResolveCanvasReference();
            ClientGameplaySettings.ServerCrosshairChanged += OnServerCrosshairSettingChanged;
            ApplyServerCrosshairSetting(ClientGameplaySettings.ServerCrosshairEnabled);
            ApplySniperPresentation(false);
        }

        private void OnDisable()
        {
            if (_sniperModeApplied)
            {
                ApplySniperPresentation(false);
            }

            ClientGameplaySettings.ServerCrosshairChanged -= OnServerCrosshairSettingChanged;
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

            if (percent >= 99)
            {
                aimStatusText.text = aimedText;
                aimStatusText.color = aimedColor;
                return;
            }

            aimStatusText.text = aimingTextPrefix + percent + "%";
            aimStatusText.color = aimingColor;
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

            return child.name == "SniperScopeOverlay";
        }

        public Canvas ResolveCanvasReference(Canvas preferredCanvas = null)
        {
            if (preferredCanvas != null)
            {
                canvas = preferredCanvas;
            }

            if (canvas != null)
            {
                return canvas;
            }

            canvas = GetComponentInParent<Canvas>();
            return canvas;
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
