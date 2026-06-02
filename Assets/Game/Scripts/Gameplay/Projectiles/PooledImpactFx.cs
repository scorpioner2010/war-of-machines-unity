using CartoonFX;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Settings;
using UnityEngine;

public sealed class PooledImpactFx : MonoBehaviour
{
    private const float CameraShakeFullStrengthDistance = 10f;
    private const float CameraShakeMaxDistance = 90f;
    private const float CameraShakeFullStrengthDistanceSqr =
        CameraShakeFullStrengthDistance * CameraShakeFullStrengthDistance;
    private const float CameraShakeMaxDistanceSqr = CameraShakeMaxDistance * CameraShakeMaxDistance;

    public ParticleSystem[] particles = System.Array.Empty<ParticleSystem>();
    public CFXR_Effect[] cartoonFxEffects = System.Array.Empty<CFXR_Effect>();

    private float _fallbackLifetime;
    private float _releaseTime;
    private bool _playing;

    public void Initialize(float fallbackLifetime)
    {
        _fallbackLifetime = Mathf.Max(0.1f, fallbackLifetime);
        DisableExternalAutoDestroy();
    }

    public void Play()
    {
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
                ? GetCameraShakeStrengthMultiplier(gameplayCamera)
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
        _playing = false;

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
        if (!_playing || Time.unscaledTime < _releaseTime)
        {
            return;
        }

        if (!ProjectileRuntimePool.ReleaseImpactFx(this))
        {
            Destroy(gameObject);
        }
    }
}
