using System.Collections.Generic;
using CartoonFX;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class PooledImpactFx : MonoBehaviour
{
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

    public ParticleSystem[] particles = System.Array.Empty<ParticleSystem>();
    public CFXR_Effect[] cartoonFxEffects = System.Array.Empty<CFXR_Effect>();
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
    [Min(0f)] public float impactDecalSurfaceOffset = 0.003f;

    private float _fallbackLifetime;
    private float _releaseTime;
    private float _impactGlowFadeStartTime;
    private float _impactGlowFadeEndTime;
    private bool _impactGlowPlaying;
    private bool _persistentImpactDecal;
    private bool _playing;
    private Material _impactGlowMaterialInstance;
    private Material _impactGlowSourceMaterial;
    private Material _impactGlowHaloMaterialInstance;
    private Material _impactGlowHaloSourceMaterial;
    private float _baseImpactGlowEmissionStrength = 1f;
    private VehicleRoot _impactTargetRoot;
    private uint _impactRenderingLayerMask;

    public void Initialize(float fallbackLifetime)
    {
        _fallbackLifetime = Mathf.Max(0.1f, fallbackLifetime);
        EnsureImpactGlowMaterial();
        EnsureImpactGlowHaloMaterial();
        DisableExternalAutoDestroy();
    }

    public void Play()
    {
        ResetImpactDecal();

        if (particles == null)
        {
            particles = System.Array.Empty<ParticleSystem>();
        }

        DisableExternalAutoDestroy();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        float lifetime = _fallbackLifetime;
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particle.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            particle.Clear(true);
            particle.Play(true);
        }

        if (cartoonFxEffects != null)
        {
            bool cameraShakeEnabled = ClientGameplaySettings.CameraShakeEnabled;
            Camera gameplayCamera = cameraShakeEnabled ? GetGameplayCamera() : null;
            float cameraShakeStrengthMultiplier = cameraShakeEnabled
                ? GetCameraShakeStrengthMultiplier(gameplayCamera) * CameraShakeStrengthScale
                : 0f;
            for (int i = 0; i < cartoonFxEffects.Length; i++)
            {
                CFXR_Effect effect = cartoonFxEffects[i];
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

    public void PlayImpact(Vector3 impactPoint, Vector3 impactNormal, VehicleRoot impactTarget = null)
    {
        Play();
        if (impactDecal == null && impactGlowDecal == null && impactGlowHaloRenderer == null)
        {
            return;
        }

        if (impactTarget != null
            && (!VehicleColliderRegistry.TryGetImpactRenderers(impactTarget, out List<Renderer> targetRenderers)
                || !TryAcquireImpactLayer(impactTarget, targetRenderers)))
        {
            return;
        }

        Vector3 safeNormal = impactNormal.sqrMagnitude > 0.000001f
            ? impactNormal.normalized
            : Vector3.up;
        Vector3 up = Vector3.Cross(safeNormal, Vector3.up);
        if (up.sqrMagnitude <= 0.000001f)
        {
            up = Vector3.Cross(safeNormal, Vector3.right);
        }

        Quaternion rotation = Quaternion.LookRotation(-safeNormal, up.normalized);
        rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), safeNormal) * rotation;

        Vector3 surfacePosition = impactPoint + safeNormal * Mathf.Max(0f, impactDecalSurfaceOffset);
        float projectionDepth = Mathf.Max(0.01f, impactDecalProjectionDepth);
        uint renderingLayerMask = _impactRenderingLayerMask != 0
            ? _impactRenderingLayerMask
            : WorldImpactRenderingLayerMask;
        EnsureImpactGlowMaterial();
        EnsureImpactGlowHaloMaterial();
        ApplyImpactGlowStrength();
        ConfigureImpactDecal(impactDecal, surfacePosition, rotation, projectionDepth, renderingLayerMask);
        ConfigureImpactDecal(impactGlowDecal, surfacePosition, rotation, projectionDepth, renderingLayerMask);
        ConfigureImpactGlowHalo(surfacePosition, rotation, renderingLayerMask);
        if (impactGlowDecal != null)
        {
            impactGlowDecal.fadeFactor = impactGlowStrength > 0f ? 1f : 0f;
        }

        float now = Time.unscaledTime;
        float fadeDuration = Mathf.Max(0f, impactGlowFadeDuration);
        _impactGlowFadeStartTime = now;
        _impactGlowFadeEndTime = now + fadeDuration;
        _impactGlowPlaying = impactGlowDecal != null || impactGlowHaloRenderer != null;
        _persistentImpactDecal = impactDecal != null;
        if (_persistentImpactDecal)
        {
            _releaseTime = float.PositiveInfinity;
        }
        else
        {
            _releaseTime = Mathf.Max(_releaseTime, _impactGlowFadeEndTime + 0.05f);
        }
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

        if (particles != null)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

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
        if (_impactGlowHaloMaterialInstance != null)
        {
            Destroy(_impactGlowHaloMaterialInstance);
            _impactGlowHaloMaterialInstance = null;
        }

        _impactGlowHaloSourceMaterial = null;
    }

    private void DisableExternalAutoDestroy()
    {
        if (cartoonFxEffects == null)
        {
            return;
        }

        for (int i = 0; i < cartoonFxEffects.Length; i++)
        {
            CFXR_Effect effect = cartoonFxEffects[i];
            if (effect != null)
            {
                effect.clearBehavior = CFXR_Effect.ClearBehavior.None;
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
        _persistentImpactDecal = false;
        _impactGlowFadeStartTime = 0f;
        _impactGlowFadeEndTime = 0f;
        if (impactDecal != null)
        {
            ResetImpactDecalProjector(impactDecal);
        }

        if (impactGlowDecal != null)
        {
            ResetImpactDecalProjector(impactGlowDecal);
        }

        if (impactGlowHaloRenderer != null)
        {
            impactGlowHaloRenderer.enabled = false;
            if (impactGlowHaloRenderer.gameObject.activeSelf)
            {
                impactGlowHaloRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateImpactGlowFade()
    {
        if (!_impactGlowPlaying || (impactGlowDecal == null && impactGlowHaloRenderer == null))
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
        projector.size = new Vector3(
            Mathf.Max(0.01f, impactDecalWidth),
            Mathf.Max(0.01f, impactDecalHeight),
            projectionDepth);
        projector.pivot = Vector3.zero;
        projector.renderingLayerMask = renderingLayerMask;
        projector.fadeFactor = 1f;
        projector.gameObject.SetActive(true);
    }

    private void ResetImpactDecalProjector(DecalProjector projector)
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
        float normalizedFade = impactGlowStrength > 0f ? Mathf.Clamp01(fadeFactor) : 0f;
        if (impactGlowDecal != null)
        {
            impactGlowDecal.fadeFactor = normalizedFade;
        }

        if (_impactGlowMaterialInstance != null
            && _impactGlowMaterialInstance.HasProperty(ImpactGlowEmissionStrengthPropertyId))
        {
            _impactGlowMaterialInstance.SetFloat(
                ImpactGlowEmissionStrengthPropertyId,
                _baseImpactGlowEmissionStrength
                * Mathf.Max(0f, impactGlowStrength)
                * Mathf.Max(0f, impactGlowDecalEmissionStrength)
                * normalizedFade);
        }

        SetImpactGlowHaloFade(normalizedFade);
    }

    private void EnsureImpactGlowMaterial()
    {
        if (impactGlowDecal == null)
        {
            return;
        }

        Material configuredMaterial = impactGlowDecal.material;
        Material sourceMaterial = configuredMaterial == _impactGlowMaterialInstance
            ? _impactGlowSourceMaterial
            : configuredMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        if (_impactGlowMaterialInstance != null && _impactGlowSourceMaterial == sourceMaterial)
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
        impactGlowDecal.material = _impactGlowMaterialInstance;
    }

    private void EnsureImpactGlowHaloMaterial()
    {
        if (impactGlowHaloRenderer == null)
        {
            return;
        }

        Material configuredMaterial = impactGlowHaloRenderer.sharedMaterial;
        Material sourceMaterial = configuredMaterial == _impactGlowHaloMaterialInstance
            ? _impactGlowHaloSourceMaterial
            : configuredMaterial;
        if (sourceMaterial == null)
        {
            return;
        }

        if (_impactGlowHaloMaterialInstance != null && _impactGlowHaloSourceMaterial == sourceMaterial)
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
        impactGlowHaloRenderer.sharedMaterial = _impactGlowHaloMaterialInstance;
    }

    private void ApplyImpactGlowStrength()
    {
        EnsureImpactGlowMaterial();
        if (_impactGlowMaterialInstance != null
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
                * Mathf.Max(0f, impactGlowStrength)
                * Mathf.Max(0f, impactGlowDecalEmissionStrength));
        }

        EnsureImpactGlowHaloMaterial();
        if (_impactGlowHaloMaterialInstance == null)
        {
            return;
        }

        float strength = Mathf.Max(0f, impactGlowStrength) * Mathf.Max(0f, impactGlowHaloStrength);
        _impactGlowHaloMaterialInstance.SetColor(
            ImpactGlowHaloBaseColorPropertyId,
            impactGlowHaloBaseColor);
        _impactGlowHaloMaterialInstance.SetColor(
            ImpactGlowHaloEmissionColorPropertyId,
            impactGlowHaloEmissionColor * strength);
    }

    private void SetImpactGlowHaloFade(float fadeFactor)
    {
        if (_impactGlowHaloMaterialInstance == null)
        {
            return;
        }

        float normalizedFade = Mathf.Clamp01(fadeFactor);
        float strength = Mathf.Max(0f, impactGlowStrength)
            * Mathf.Max(0f, impactGlowHaloStrength)
            * normalizedFade;
        Color baseColor = impactGlowHaloBaseColor * normalizedFade;
        Color emissionColor = impactGlowHaloEmissionColor * strength;
        _impactGlowHaloMaterialInstance.SetColor(ImpactGlowHaloBaseColorPropertyId, baseColor);
        _impactGlowHaloMaterialInstance.SetColor(ImpactGlowHaloEmissionColorPropertyId, emissionColor);
        if (impactGlowHaloRenderer != null && fadeFactor <= 0f)
        {
            impactGlowHaloRenderer.enabled = false;
            if (impactGlowHaloRenderer.gameObject.activeSelf)
            {
                impactGlowHaloRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void ConfigureImpactGlowHalo(
        Vector3 surfacePosition,
        Quaternion rotation,
        uint renderingLayerMask)
    {
        if (impactGlowHaloRenderer == null)
        {
            return;
        }

        Transform haloTransform = impactGlowHaloRenderer.transform;
        haloTransform.SetPositionAndRotation(surfacePosition, rotation);
        haloTransform.localScale = new Vector3(
            Mathf.Max(0.01f, impactDecalWidth) * Mathf.Max(0.01f, impactGlowHaloSizeMultiplier),
            Mathf.Max(0.01f, impactDecalHeight) * Mathf.Max(0.01f, impactGlowHaloSizeMultiplier),
            1f);
        impactGlowHaloRenderer.renderingLayerMask = renderingLayerMask;
        bool haloEnabled = impactGlowStrength > 0f && impactGlowHaloStrength > 0f;
        impactGlowHaloRenderer.enabled = haloEnabled;
        if (haloEnabled && !impactGlowHaloRenderer.gameObject.activeSelf)
        {
            impactGlowHaloRenderer.gameObject.SetActive(true);
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
