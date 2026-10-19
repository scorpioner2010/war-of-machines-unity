using System.Collections.Generic;
using CartoonFX;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class PooledImpactFx : MonoBehaviour
{
    private const uint DefaultRenderingLayerMask = 1u;
    private const int FirstImpactRenderingLayerBit = 1;
    private const int LastImpactRenderingLayerBit = 7;
    private const float CameraShakeFullStrengthDistance = 10f;
    private const float CameraShakeMaxDistance = 90f;
    private const float CameraShakeStrengthScale = 0.3f;
    private const float CameraShakeFullStrengthDistanceSqr =
        CameraShakeFullStrengthDistance * CameraShakeFullStrengthDistance;
    private const float CameraShakeMaxDistanceSqr = CameraShakeMaxDistance * CameraShakeMaxDistance;

    private static readonly Dictionary<VehicleRoot, ImpactLayerState> ImpactLayerStates =
        new Dictionary<VehicleRoot, ImpactLayerState>(16);
    private static uint ActiveImpactRenderingLayerMask;

    public ParticleSystem[] particles = System.Array.Empty<ParticleSystem>();
    public CFXR_Effect[] cartoonFxEffects = System.Array.Empty<CFXR_Effect>();
    public DecalProjector impactDecal;
    [Min(0.1f)] public float impactDecalLifetime = 30f;
    [Min(0f)] public float impactDecalFadeDuration = 5f;
    [Min(0.01f)] public float impactDecalWidth = 1.2f;
    [Min(0.01f)] public float impactDecalHeight = 1.2f;
    [Min(0.01f)] public float impactDecalProjectionDepth = 0.4f;
    [Min(0f)] public float impactDecalSurfaceOffset = 0.003f;

    private float _fallbackLifetime;
    private float _releaseTime;
    private float _impactDecalFadeStartTime;
    private float _impactDecalReleaseTime;
    private bool _impactDecalPlaying;
    private bool _playing;
    private VehicleRoot _impactTargetRoot;
    private uint _impactRenderingLayerMask;

    public void Initialize(float fallbackLifetime)
    {
        _fallbackLifetime = Mathf.Max(0.1f, fallbackLifetime);
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
        if (impactDecal == null)
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
        Transform decalTransform = impactDecal.transform;
        decalTransform.SetPositionAndRotation(surfacePosition, rotation);

        float projectionDepth = Mathf.Max(0.01f, impactDecalProjectionDepth);
        impactDecal.size = new Vector3(
            Mathf.Max(0.01f, impactDecalWidth),
            Mathf.Max(0.01f, impactDecalHeight),
            projectionDepth);
        impactDecal.pivot = Vector3.zero;
        impactDecal.renderingLayerMask = _impactRenderingLayerMask != 0
            ? _impactRenderingLayerMask
            : DefaultRenderingLayerMask;
        impactDecal.fadeFactor = 1f;
        impactDecal.gameObject.SetActive(true);

        float lifetime = Mathf.Max(0.1f, impactDecalLifetime);
        float fadeDuration = Mathf.Clamp(impactDecalFadeDuration, 0f, lifetime);
        _impactDecalFadeStartTime = Time.unscaledTime + lifetime - fadeDuration;
        _impactDecalReleaseTime = Time.unscaledTime + lifetime;
        _releaseTime = Mathf.Max(_releaseTime, _impactDecalReleaseTime + 0.05f);
        _impactDecalPlaying = true;
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
        UpdateImpactDecalFade();

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
        _impactDecalPlaying = false;
        _impactDecalFadeStartTime = 0f;
        _impactDecalReleaseTime = 0f;
        if (impactDecal != null)
        {
            impactDecal.renderingLayerMask = DefaultRenderingLayerMask;
            impactDecal.fadeFactor = 0f;
            if (impactDecal.gameObject.activeSelf)
            {
                impactDecal.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateImpactDecalFade()
    {
        if (!_impactDecalPlaying || impactDecal == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now < _impactDecalFadeStartTime)
        {
            return;
        }

        float fadeDuration = _impactDecalReleaseTime - _impactDecalFadeStartTime;
        if (fadeDuration <= 0.0001f)
        {
            impactDecal.fadeFactor = 0f;
            return;
        }

        float fade = Mathf.Clamp01((now - _impactDecalFadeStartTime) / fadeDuration);
        impactDecal.fadeFactor = 1f - fade;
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
