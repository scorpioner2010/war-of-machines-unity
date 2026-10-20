using System.Collections.Generic;
using CartoonFX;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class PooledImpactFx : MonoBehaviour
{
    [System.Serializable]
    public sealed class ImpactFxProfile
    {
        [Header("Visual Effects")]
        public ParticleSystem[] particles = System.Array.Empty<ParticleSystem>();
        public CFXR_Effect[] cartoonFxEffects = System.Array.Empty<CFXR_Effect>();

        [Header("Impact Projectors")]
        public DecalProjector impactDecal;
        public DecalProjector impactGlowDecal;
        public MeshRenderer impactGlowHaloRenderer;

        [Header("Impact Glow")]
        [Tooltip("Shared multiplier for the crack decal and the broad halo. Values above 1 are allowed.")]
        [Min(0f)] public float impactGlowStrength = 1f;
        [Tooltip("Additional multiplier for the broad halo Bloom source only.")]
        [Min(0f)] public float impactGlowHaloStrength = 1f;
        [Tooltip("HDR emission color of the broad halo. Its RGB values are the actual HDR input sent to Bloom.")]
        [ColorUsage(true, true)] public Color impactGlowHaloEmissionColor = new Color(4f, 0.25f, 0.02f, 1f);
        [Tooltip("Base tint and alpha of the broad halo. Lower alpha reduces the visible soft disk without changing the crack decal.")]
        [ColorUsage(true, false)] public Color impactGlowHaloBaseColor = Color.white;
        [Tooltip("Size of the broad halo relative to the regular impact decal.")]
        [Min(0.01f)] public float impactGlowHaloSizeMultiplier = 2f;
        [Tooltip("Emission multiplier for the thin crack detail only.")]
        [Min(0f)] public float impactGlowDecalEmissionStrength = 1f;
        [Tooltip("Unscaled time used to fade the halo and crack glow after impact. The regular impact remains persistent.")]
        [Min(0f)] public float impactGlowFadeDuration = 2f;

        [Header("Impact Decal")]
        [Min(0.01f)] public float impactDecalWidth = 1.2f;
        [Min(0.01f)] public float impactDecalHeight = 1.2f;
        [Min(0.01f)] public float impactDecalProjectionDepth = 0.4f;
        [Tooltip("Signed world-space correction from the physics hit point. Positive moves toward the hit normal; negative moves inward toward a deeper visual mesh.")]
        public float impactDecalSurfaceOffset = 0.003f;
    }

    private const string ImpactGlowEmissionStrengthPropertyName = "Emission_Strength";
    private const string ImpactGlowHaloBaseColorPropertyName = "_BaseColor";
    private const string ImpactGlowHaloEmissionColorPropertyName = "_EmissionColor";
    private const uint WorldImpactRenderingLayerMask = 1u;
    private const int FirstImpactRenderingLayerBit = 2;
    private const int LastImpactRenderingLayerBit = 7;
    private const float CameraShakeFullStrengthDistance = 10f;
    private const float CameraShakeMaxDistance = 90f;
    private const float CameraShakeStrengthScale = 0.3f;
    private const float CameraShakeFullStrengthDistanceSqr =
        CameraShakeFullStrengthDistance * CameraShakeFullStrengthDistance;
    private const float CameraShakeMaxDistanceSqr = CameraShakeMaxDistance * CameraShakeMaxDistance;

    private static readonly Dictionary<VehicleRoot, ImpactLayerState> ImpactLayerStates =
        new Dictionary<VehicleRoot, ImpactLayerState>(16);
    private static readonly int ImpactGlowEmissionStrengthPropertyId =
        Shader.PropertyToID(ImpactGlowEmissionStrengthPropertyName);
    private static readonly int ImpactGlowHaloBaseColorPropertyId =
        Shader.PropertyToID(ImpactGlowHaloBaseColorPropertyName);
    private static readonly int ImpactGlowHaloEmissionColorPropertyId =
        Shader.PropertyToID(ImpactGlowHaloEmissionColorPropertyName);
    private static uint ActiveImpactRenderingLayerMask;

    [Header("Robot Impact Profile")]
    public ImpactFxProfile robotImpactProfile = new ImpactFxProfile();
    [Header("Other Object Impact Profile")]
    public ImpactFxProfile otherObjectImpactProfile = new ImpactFxProfile();

    private float _fallbackLifetime;
    private float _releaseTime;
    private float _impactGlowFadeStartTime;
    private float _impactGlowFadeEndTime;
    private bool _impactGlowPlaying;
    private bool _playing;
    private ImpactFxProfile _activeProfile;
    private Material _impactGlowMaterialInstance;
    private Material _impactGlowSourceMaterial;
    private DecalProjector _impactGlowMaterialProjector;
    private Material _impactGlowHaloMaterialInstance;
    private Material _impactGlowHaloSourceMaterial;
    private MeshRenderer _impactGlowHaloMaterialRenderer;
    private float _baseImpactGlowEmissionStrength = 1f;
    private VehicleRoot _impactTargetRoot;
    private uint _impactRenderingLayerMask;

    public void Initialize(float fallbackLifetime)
    {
        _fallbackLifetime = Mathf.Max(0.1f, fallbackLifetime);
        DisableExternalAutoDestroy();
    }

    public void Play()
    {
        PlayInternal(GetOtherObjectImpactProfile());
    }

    public void PlayImpact(
        Vector3 impactPoint,
        Vector3 impactNormal,
        VehicleRoot impactTarget = null,
        Collider impactCollider = null)
    {
        ImpactFxProfile profile = impactTarget != null
            ? GetRobotImpactProfile()
            : GetOtherObjectImpactProfile();
        PlayInternal(profile);
        if (profile.impactDecal == null
            && profile.impactGlowDecal == null
            && profile.impactGlowHaloRenderer == null)
        {
            return;
        }

        _impactRenderingLayerMask = 0u;
        if (impactTarget != null
            && VehicleColliderRegistry.TryGetImpactRenderers(impactTarget, out List<Renderer> targetRenderers))
        {
            TryAcquireImpactLayer(impactTarget, targetRenderers);
        }

        Vector3 safeNormal = impactNormal.sqrMagnitude > 0.000001f
            ? impactNormal.normalized
            : Vector3.up;
        if (impactTarget != null
            && VehicleColliderRegistry.TryGetImpactVisualSurface(
                impactTarget,
                impactPoint,
                safeNormal,
                out Vector3 visualSurfacePosition,
                out Vector3 visualSurfaceNormal))
        {
            impactPoint = visualSurfacePosition;
            safeNormal = visualSurfaceNormal;
        }
        else if (impactTarget == null
            && VehicleColliderRegistry.TryGetImpactVisualSurface(
                impactCollider,
                impactPoint,
                safeNormal,
                out visualSurfacePosition,
                out visualSurfaceNormal))
        {
            impactPoint = visualSurfacePosition;
            safeNormal = visualSurfaceNormal;
        }

        Vector3 up = Vector3.Cross(safeNormal, Vector3.up);
        if (up.sqrMagnitude <= 0.000001f)
        {
            up = Vector3.Cross(safeNormal, Vector3.right);
        }

        Quaternion rotation = Quaternion.LookRotation(-safeNormal, up.normalized);
        rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), safeNormal) * rotation;

        float surfaceOffset = profile.impactDecalSurfaceOffset;
        if (float.IsNaN(surfaceOffset) || float.IsInfinity(surfaceOffset))
        {
            surfaceOffset = 0f;
        }

        Vector3 surfacePosition = impactPoint + safeNormal * surfaceOffset;
        transform.position += safeNormal * surfaceOffset;
        float projectionDepth = Mathf.Max(0.01f, profile.impactDecalProjectionDepth);
        uint renderingLayerMask = _impactRenderingLayerMask != 0
            ? _impactRenderingLayerMask
            : WorldImpactRenderingLayerMask;
        EnsureImpactGlowMaterial();
        EnsureImpactGlowHaloMaterial();
        ApplyImpactGlowStrength();
        ConfigureImpactDecal(
            profile.impactDecal,
            surfacePosition,
            rotation,
            projectionDepth,
            renderingLayerMask);
        ConfigureImpactDecal(
            profile.impactGlowDecal,
            surfacePosition,
            rotation,
            projectionDepth,
            renderingLayerMask);
        ConfigureImpactGlowHalo(surfacePosition, rotation, renderingLayerMask);
        if (profile.impactGlowDecal != null)
        {
            profile.impactGlowDecal.fadeFactor = profile.impactGlowStrength > 0f ? 1f : 0f;
        }

        float now = Time.unscaledTime;
        float fadeDuration = Mathf.Max(0f, profile.impactGlowFadeDuration);
        _impactGlowFadeStartTime = now;
        _impactGlowFadeEndTime = now + fadeDuration;
        _impactGlowPlaying = profile.impactGlowDecal != null || profile.impactGlowHaloRenderer != null;
        if (profile.impactDecal != null)
        {
            _releaseTime = float.PositiveInfinity;
        }
        else
        {
            _releaseTime = Mathf.Max(_releaseTime, _impactGlowFadeEndTime + 0.05f);
        }
    }

    private void PlayInternal(ImpactFxProfile profile)
    {
        ResetImpactDecal();
        _activeProfile = profile;
        if (_activeProfile == null)
        {
            _activeProfile = GetOtherObjectImpactProfile();
        }

        DisableExternalAutoDestroy();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        float lifetime = _fallbackLifetime;
        ParticleSystem[] impactParticles = _activeProfile.particles;
        if (impactParticles == null)
        {
            impactParticles = System.Array.Empty<ParticleSystem>();
        }

        for (int i = 0; i < impactParticles.Length; i++)
        {
            ParticleSystem particle = impactParticles[i];
            if (particle == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particle.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            particle.Clear(true);
            particle.Play(true);
        }

        CFXR_Effect[] impactCartoonFxEffects = _activeProfile.cartoonFxEffects;
        if (impactCartoonFxEffects != null)
        {
            bool cameraShakeEnabled = ClientGameplaySettings.CameraShakeEnabled;
            Camera gameplayCamera = cameraShakeEnabled ? GetGameplayCamera() : null;
            float cameraShakeStrengthMultiplier = cameraShakeEnabled
                ? GetCameraShakeStrengthMultiplier(gameplayCamera) * CameraShakeStrengthScale
                : 0f;
            for (int i = 0; i < impactCartoonFxEffects.Length; i++)
            {
                CFXR_Effect effect = impactCartoonFxEffects[i];
                if (effect != null)
                {
                    effect.ResetState();
                    if (effect.cameraShake != null)
                    {
                        effect.cameraShake.SetStrengthMultiplier(cameraShakeStrengthMultiplier);
                        if (cameraShakeStrengthMultiplier > 0f && gameplayCamera != null)
                        {
                            effect.cameraShake.SetCamera(gameplayCamera);
                        }
                    }
                }
            }
        }

        _releaseTime = Time.unscaledTime + lifetime + 0.05f;
        _playing = true;
    }

    private ImpactFxProfile GetRobotImpactProfile()
    {
        if (robotImpactProfile == null)
        {
            robotImpactProfile = new ImpactFxProfile();
        }

        return robotImpactProfile;
    }

    private ImpactFxProfile GetOtherObjectImpactProfile()
    {
        if (otherObjectImpactProfile == null)
        {
            otherObjectImpactProfile = new ImpactFxProfile();
        }

        return otherObjectImpactProfile;
    }

    private static Camera GetGameplayCamera()
    {
        CameraSync cameraSync = CameraSync.In;
        return cameraSync != null ? cameraSync.gameplayCamera : null;
    }

    private float GetCameraShakeStrengthMultiplier(Camera gameplayCamera)
    {
        if (gameplayCamera == null)
        {
            return 1f;
        }

        Vector3 cameraOffset = gameplayCamera.transform.position - transform.position;
        float distanceSqr = cameraOffset.sqrMagnitude;
        if (distanceSqr <= CameraShakeFullStrengthDistanceSqr)
        {
            return 1f;
        }

        if (distanceSqr >= CameraShakeMaxDistanceSqr)
        {
            return 0f;
        }

        float distance = Mathf.Sqrt(distanceSqr);
        float fade = (distance - CameraShakeFullStrengthDistance) /
                     (CameraShakeMaxDistance - CameraShakeFullStrengthDistance);
        float smoothFade = fade * fade * (3f - 2f * fade);
        return 1f - smoothFade;
    }

    public void PrepareForPoolRelease()
    {
        if (this == null)
        {
            return;
        }

        _playing = false;
        ResetImpactDecal();
        StopParticles(robotImpactProfile);
        StopParticles(otherObjectImpactProfile);
        _activeProfile = null;

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ReleaseImpactLayer();
        ProjectileRuntimePool.NotifyImpactFxDestroyed(this);
        if (_impactGlowMaterialInstance != null)
        {
            Destroy(_impactGlowMaterialInstance);
            _impactGlowMaterialInstance = null;
        }

        _impactGlowSourceMaterial = null;
        _impactGlowMaterialProjector = null;
        if (_impactGlowHaloMaterialInstance != null)
        {
            Destroy(_impactGlowHaloMaterialInstance);
            _impactGlowHaloMaterialInstance = null;
        }

        _impactGlowHaloSourceMaterial = null;
        _impactGlowHaloMaterialRenderer = null;
    }

    private void DisableExternalAutoDestroy()
    {
        DisableExternalAutoDestroy(robotImpactProfile);
        DisableExternalAutoDestroy(otherObjectImpactProfile);
    }

    private static void DisableExternalAutoDestroy(ImpactFxProfile profile)
    {
        if (profile == null || profile.cartoonFxEffects == null)
        {
            return;
        }

        for (int i = 0; i < profile.cartoonFxEffects.Length; i++)
        {
            CFXR_Effect effect = profile.cartoonFxEffects[i];
            if (effect != null)
            {
                effect.clearBehavior = CFXR_Effect.ClearBehavior.None;
            }
        }
    }

    private static void StopParticles(ImpactFxProfile profile)
    {
        if (profile == null || profile.particles == null)
        {
            return;
        }

        for (int i = 0; i < profile.particles.Length; i++)
        {
            ParticleSystem particle = profile.particles[i];
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void Update()
    {
        UpdateImpactGlowFade();

        if (!_playing || Time.unscaledTime < _releaseTime)
        {
            return;
        }

        if (!ProjectileRuntimePool.ReleaseImpactFx(this))
        {
            Destroy(gameObject);
        }
    }

    private void ResetImpactDecal()
    {
        ReleaseImpactLayer();
        _impactGlowPlaying = false;
        _impactGlowFadeStartTime = 0f;
        _impactGlowFadeEndTime = 0f;
        ResetImpactProfileVisuals(robotImpactProfile);
        ResetImpactProfileVisuals(otherObjectImpactProfile);
    }

    private static void ResetImpactProfileVisuals(ImpactFxProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        if (profile.impactDecal != null)
        {
            ResetImpactDecalProjector(profile.impactDecal);
        }

        if (profile.impactGlowDecal != null)
        {
            ResetImpactDecalProjector(profile.impactGlowDecal);
        }

        if (profile.impactGlowHaloRenderer != null)
        {
            profile.impactGlowHaloRenderer.enabled = false;
            profile.impactGlowHaloRenderer.renderingLayerMask = WorldImpactRenderingLayerMask;
            if (profile.impactGlowHaloRenderer.gameObject.activeSelf)
            {
                profile.impactGlowHaloRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateImpactGlowFade()
    {
        ImpactFxProfile profile = _activeProfile;
        if (!_impactGlowPlaying
            || profile == null
            || (profile.impactGlowDecal == null && profile.impactGlowHaloRenderer == null))
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now < _impactGlowFadeStartTime)
        {
            return;
        }

        float fadeDuration = _impactGlowFadeEndTime - _impactGlowFadeStartTime;
        if (fadeDuration <= 0.0001f)
        {
            SetImpactGlowFade(0f);
            _impactGlowPlaying = false;
            return;
        }

        float fade = Mathf.Clamp01((now - _impactGlowFadeStartTime) / fadeDuration);
        float smoothFade = fade * fade * (3f - 2f * fade);
        SetImpactGlowFade(1f - smoothFade);
        if (fade >= 1f)
        {
            _impactGlowPlaying = false;
        }
    }

    private void ConfigureImpactDecal(
        DecalProjector projector,
        Vector3 surfacePosition,
        Quaternion rotation,
        float projectionDepth,
        uint renderingLayerMask)
    {
        if (projector == null)
        {
            return;
        }

        Transform decalTransform = projector.transform;
        decalTransform.SetPositionAndRotation(surfacePosition, rotation);
        ImpactFxProfile profile = _activeProfile;
        if (profile == null)
        {
            return;
        }

        projector.size = new Vector3(
            Mathf.Max(0.01f, profile.impactDecalWidth),
            Mathf.Max(0.01f, profile.impactDecalHeight),
            projectionDepth);
        projector.pivot = Vector3.zero;
        projector.renderingLayerMask = renderingLayerMask;
        projector.fadeFactor = 1f;
        projector.gameObject.SetActive(true);
    }

    private static void ResetImpactDecalProjector(DecalProjector projector)
    {
        projector.renderingLayerMask = WorldImpactRenderingLayerMask;
        projector.fadeFactor = 0f;
        if (projector.gameObject.activeSelf)
        {
            projector.gameObject.SetActive(false);
        }
    }

    private void SetImpactGlowFade(float fadeFactor)
    {
        ImpactFxProfile profile = _activeProfile;
        if (profile == null)
        {
            return;
        }

        float normalizedFade = profile.impactGlowStrength > 0f ? Mathf.Clamp01(fadeFactor) : 0f;
        if (profile.impactGlowDecal != null)
        {
            profile.impactGlowDecal.fadeFactor = normalizedFade;
        }

        if (_impactGlowMaterialInstance != null
            && _impactGlowMaterialProjector == profile.impactGlowDecal
            && _impactGlowMaterialInstance.HasProperty(ImpactGlowEmissionStrengthPropertyId))
        {
            _impactGlowMaterialInstance.SetFloat(
                ImpactGlowEmissionStrengthPropertyId,
                _baseImpactGlowEmissionStrength
                * Mathf.Max(0f, profile.impactGlowStrength)
                * Mathf.Max(0f, profile.impactGlowDecalEmissionStrength)
                * normalizedFade);
        }

        SetImpactGlowHaloFade(normalizedFade);
    }

    private void EnsureImpactGlowMaterial()
    {
        ImpactFxProfile profile = _activeProfile;
        if (profile == null || profile.impactGlowDecal == null)
        {
            return;
        }

        DecalProjector projector = profile.impactGlowDecal;
        Material configuredMaterial = projector.material;
        Material sourceMaterial = configuredMaterial == _impactGlowMaterialInstance
            && _impactGlowMaterialProjector == projector
            ? _impactGlowSourceMaterial
            : configuredMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        if (_impactGlowMaterialInstance != null
            && _impactGlowMaterialProjector == projector
            && _impactGlowSourceMaterial == sourceMaterial)
        {
            return;
        }

        if (_impactGlowMaterialInstance != null)
        {
            Destroy(_impactGlowMaterialInstance);
        }

        _impactGlowMaterialInstance = new Material(sourceMaterial);
        _impactGlowMaterialInstance.name = sourceMaterial.name + " (Runtime)";
        _impactGlowSourceMaterial = sourceMaterial;
        _impactGlowMaterialProjector = projector;
        projector.material = _impactGlowMaterialInstance;
    }

    private void EnsureImpactGlowHaloMaterial()
    {
        ImpactFxProfile profile = _activeProfile;
        if (profile == null || profile.impactGlowHaloRenderer == null)
        {
            return;
        }

        MeshRenderer renderer = profile.impactGlowHaloRenderer;
        Material configuredMaterial = renderer.sharedMaterial;
        Material sourceMaterial = configuredMaterial == _impactGlowHaloMaterialInstance
            && _impactGlowHaloMaterialRenderer == renderer
            ? _impactGlowHaloSourceMaterial
            : configuredMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        if (_impactGlowHaloMaterialInstance != null
            && _impactGlowHaloMaterialRenderer == renderer
            && _impactGlowHaloSourceMaterial == sourceMaterial)
        {
            return;
        }

        if (_impactGlowHaloMaterialInstance != null)
        {
            Destroy(_impactGlowHaloMaterialInstance);
        }

        _impactGlowHaloMaterialInstance = new Material(sourceMaterial);
        _impactGlowHaloMaterialInstance.name = sourceMaterial.name + " (Runtime)";
        _impactGlowHaloSourceMaterial = sourceMaterial;
        _impactGlowHaloMaterialRenderer = renderer;
        renderer.sharedMaterial = _impactGlowHaloMaterialInstance;
    }

    private void ApplyImpactGlowStrength()
    {
        ImpactFxProfile profile = _activeProfile;
        if (profile == null)
        {
            return;
        }

        EnsureImpactGlowMaterial();
        if (_impactGlowMaterialInstance != null
            && _impactGlowMaterialProjector == profile.impactGlowDecal
            && _impactGlowMaterialInstance.HasProperty(ImpactGlowEmissionStrengthPropertyId))
        {
            _baseImpactGlowEmissionStrength = _impactGlowSourceMaterial != null
                && _impactGlowSourceMaterial.HasProperty(ImpactGlowEmissionStrengthPropertyId)
                ? _impactGlowSourceMaterial.GetFloat(ImpactGlowEmissionStrengthPropertyId)
                : 1f;
            if (float.IsNaN(_baseImpactGlowEmissionStrength)
                || float.IsInfinity(_baseImpactGlowEmissionStrength)
                || _baseImpactGlowEmissionStrength < 0f)
            {
                _baseImpactGlowEmissionStrength = 1f;
            }

            _impactGlowMaterialInstance.SetFloat(
                ImpactGlowEmissionStrengthPropertyId,
                _baseImpactGlowEmissionStrength
                * Mathf.Max(0f, profile.impactGlowStrength)
                * Mathf.Max(0f, profile.impactGlowDecalEmissionStrength));
        }

        EnsureImpactGlowHaloMaterial();
        if (_impactGlowHaloMaterialInstance == null)
        {
            return;
        }

        if (_impactGlowHaloMaterialRenderer != profile.impactGlowHaloRenderer)
        {
            return;
        }

        float strength = Mathf.Max(0f, profile.impactGlowStrength)
            * Mathf.Max(0f, profile.impactGlowHaloStrength);
        _impactGlowHaloMaterialInstance.SetColor(
            ImpactGlowHaloBaseColorPropertyId,
            profile.impactGlowHaloBaseColor);
        _impactGlowHaloMaterialInstance.SetColor(
            ImpactGlowHaloEmissionColorPropertyId,
            profile.impactGlowHaloEmissionColor * strength);
    }

    private void SetImpactGlowHaloFade(float fadeFactor)
    {
        ImpactFxProfile profile = _activeProfile;
        if (_impactGlowHaloMaterialInstance == null
            || profile == null
            || _impactGlowHaloMaterialRenderer != profile.impactGlowHaloRenderer)
        {
            return;
        }

        float normalizedFade = Mathf.Clamp01(fadeFactor);
        float strength = Mathf.Max(0f, profile.impactGlowStrength)
            * Mathf.Max(0f, profile.impactGlowHaloStrength)
            * normalizedFade;
        Color baseColor = profile.impactGlowHaloBaseColor * normalizedFade;
        Color emissionColor = profile.impactGlowHaloEmissionColor * strength;
        _impactGlowHaloMaterialInstance.SetColor(ImpactGlowHaloBaseColorPropertyId, baseColor);
        _impactGlowHaloMaterialInstance.SetColor(ImpactGlowHaloEmissionColorPropertyId, emissionColor);
        if (profile.impactGlowHaloRenderer != null && fadeFactor <= 0f)
        {
            profile.impactGlowHaloRenderer.enabled = false;
            if (profile.impactGlowHaloRenderer.gameObject.activeSelf)
            {
                profile.impactGlowHaloRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void ConfigureImpactGlowHalo(
        Vector3 surfacePosition,
        Quaternion rotation,
        uint renderingLayerMask)
    {
        ImpactFxProfile profile = _activeProfile;
        if (profile == null || profile.impactGlowHaloRenderer == null)
        {
            return;
        }

        Transform haloTransform = profile.impactGlowHaloRenderer.transform;
        haloTransform.SetPositionAndRotation(surfacePosition, rotation);
        haloTransform.localScale = new Vector3(
            Mathf.Max(0.01f, profile.impactDecalWidth)
            * Mathf.Max(0.01f, profile.impactGlowHaloSizeMultiplier),
            Mathf.Max(0.01f, profile.impactDecalHeight)
            * Mathf.Max(0.01f, profile.impactGlowHaloSizeMultiplier),
            1f);
        profile.impactGlowHaloRenderer.renderingLayerMask = renderingLayerMask;
        bool haloEnabled = profile.impactGlowStrength > 0f && profile.impactGlowHaloStrength > 0f;
        profile.impactGlowHaloRenderer.enabled = haloEnabled;
        if (haloEnabled && !profile.impactGlowHaloRenderer.gameObject.activeSelf)
        {
            profile.impactGlowHaloRenderer.gameObject.SetActive(true);
        }
    }

    private bool TryAcquireImpactLayer(VehicleRoot targetRoot, List<Renderer> targetRenderers)
    {
        if (targetRoot == null || targetRenderers == null || targetRenderers.Count == 0)
        {
            return false;
        }

        if (ImpactLayerStates.TryGetValue(targetRoot, out ImpactLayerState existingState))
        {
            existingState.ReferenceCount++;
            _impactTargetRoot = targetRoot;
            _impactRenderingLayerMask = existingState.LayerMask;
            return true;
        }

        uint freeLayerMask = FindFreeImpactLayerMask();
        if (freeLayerMask == 0u)
        {
            return false;
        }

        ImpactLayerState state = new ImpactLayerState(freeLayerMask);
        for (int i = 0; i < targetRenderers.Count; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            state.Renderers.Add(renderer);
            state.OriginalMasks.Add(renderer.renderingLayerMask);
            renderer.renderingLayerMask |= freeLayerMask;
        }

        if (state.Renderers.Count == 0)
        {
            return false;
        }

        state.ReferenceCount = 1;
        ImpactLayerStates.Add(targetRoot, state);
        ActiveImpactRenderingLayerMask |= freeLayerMask;
        _impactTargetRoot = targetRoot;
        _impactRenderingLayerMask = freeLayerMask;
        return true;
    }

    private void ReleaseImpactLayer()
    {
        if (_impactTargetRoot == null || _impactRenderingLayerMask == 0u)
        {
            _impactTargetRoot = null;
            _impactRenderingLayerMask = 0u;
            return;
        }

        if (ImpactLayerStates.TryGetValue(_impactTargetRoot, out ImpactLayerState state))
        {
            state.ReferenceCount--;
            if (state.ReferenceCount <= 0)
            {
                for (int i = 0; i < state.Renderers.Count; i++)
                {
                    Renderer renderer = state.Renderers[i];
                    if (renderer != null)
                    {
                        renderer.renderingLayerMask = state.OriginalMasks[i];
                    }
                }

                ImpactLayerStates.Remove(_impactTargetRoot);
                ActiveImpactRenderingLayerMask &= ~state.LayerMask;
            }
        }

        _impactTargetRoot = null;
        _impactRenderingLayerMask = 0u;
    }

    private static uint FindFreeImpactLayerMask()
    {
        for (int bit = FirstImpactRenderingLayerBit; bit <= LastImpactRenderingLayerBit; bit++)
        {
            uint layerMask = 1u << bit;
            if ((ActiveImpactRenderingLayerMask & layerMask) == 0u)
            {
                return layerMask;
            }
        }

        return 0u;
    }

    private sealed class ImpactLayerState
    {
        public readonly uint LayerMask;
        public readonly List<Renderer> Renderers = new List<Renderer>(16);
        public readonly List<uint> OriginalMasks = new List<uint>(16);
        public int ReferenceCount;

        public ImpactLayerState(uint layerMask)
        {
            LayerMask = layerMask;
        }
    }
}
