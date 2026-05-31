using CartoonFX;
using UnityEngine;

public sealed class PooledImpactFx : MonoBehaviour
{
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
            for (int i = 0; i < cartoonFxEffects.Length; i++)
            {
                CFXR_Effect effect = cartoonFxEffects[i];
                if (effect != null)
                {
                    effect.ResetState();
                }
            }
        }

        _releaseTime = Time.unscaledTime + lifetime + 0.05f;
        _playing = true;
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
