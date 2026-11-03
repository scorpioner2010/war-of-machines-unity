using UnityEngine;

public sealed class TransportEngineAudio : MonoBehaviour
{
    private const float MinimumContinuousLoopDuration = 1f;

    [SerializeField] private float volumeFadeSpeed = 3.5f;
    [SerializeField] private float pitchSmoothing = 4.5f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 65f;

    private TransportAudioProfile profile;
    private AudioSource idleSource;
    private AudioSource runSource;
    private AudioSource highSource;
    private AudioSource eventSource;
    private bool isActive;
    private bool wasMoving;
    private float activityWeight;
    private float rpm01;
    private float previousThrottle;
    private float nextAccelerationEvent;
    private float nextStopEvent;

    public string DisplayName => profile == null ? "Unknown" : profile.displayName;
    public float Rpm01 => rpm01;
    public float Rpm => Mathf.Lerp(650f, 2_450f, rpm01);
    public bool IsActive => isActive;

    public void Initialize(TransportAudioProfile audioProfile)
    {
        profile = audioProfile;
        idleSource = CreateLoopSource("Engine Idle", profile == null ? null : profile.idleLoop);
        runSource = CreateLoopSource("Engine Run", profile == null ? null : profile.runLoop);
        highSource = CreateLoopSource("Engine High", profile == null ? null : profile.highLoop);
        eventSource = CreateEventSource();
        PlayLoop(idleSource);
        PlayLoop(runSource);
        PlayLoop(highSource);
    }

    public void SetActive(bool active)
    {
        if (isActive == active)
        {
            return;
        }

        isActive = active;
        previousThrottle = 0f;
        wasMoving = false;

        if (active)
        {
            PlayLoop(idleSource);
            PlayLoop(runSource);
            PlayLoop(highSource);
            if (profile != null && profile.startClip != null && eventSource != null)
            {
                eventSource.PlayOneShot(profile.startClip);
            }

            nextAccelerationEvent = Time.time + 0.8f;
            nextStopEvent = Time.time + 0.25f;
        }
    }

    public void Tick(float throttle01, float normalizedSpeed)
    {
        if (profile == null)
        {
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float demand = isActive ? Mathf.Clamp01(Mathf.Max(throttle01, normalizedSpeed * 0.72f)) : 0f;
        float targetRpm = isActive ? Mathf.Clamp01(Mathf.Max(demand, normalizedSpeed * 0.55f)) : 0f;
        rpm01 = Mathf.MoveTowards(rpm01, targetRpm, deltaTime * 1.8f);
        activityWeight = Mathf.MoveTowards(activityWeight, isActive ? 1f : 0f, deltaTime * volumeFadeSpeed);

        float idleWeight = 1f - Mathf.SmoothStep(0.04f, 0.52f, rpm01);
        float runWeight = Mathf.SmoothStep(0.16f, 0.68f, rpm01) * (1f - Mathf.SmoothStep(0.78f, 1f, rpm01) * 0.3f);
        float highWeight = Mathf.SmoothStep(0.58f, 0.98f, rpm01);
        if (highSource == null)
        {
            runWeight = Mathf.Max(runWeight, highWeight);
        }

        SetLayer(idleSource, idleWeight * activityWeight * profile.masterVolume, Mathf.Lerp(0.88f, 1.06f, rpm01) * profile.pitchMultiplier, deltaTime);
        SetLayer(runSource, runWeight * activityWeight * profile.masterVolume, Mathf.Lerp(0.82f, 1.12f, rpm01) * profile.pitchMultiplier, deltaTime);
        SetLayer(highSource, highWeight * activityWeight * profile.masterVolume, Mathf.Lerp(0.76f, 1.15f, rpm01) * profile.pitchMultiplier, deltaTime);

        if (isActive)
        {
            if (normalizedSpeed > 0.08f)
            {
                wasMoving = true;
            }

            if (throttle01 > 0.72f && previousThrottle < 0.45f && Time.time >= nextAccelerationEvent)
            {
                PlayRandomAccelerationClip();
                nextAccelerationEvent = Time.time + 1.1f;
            }

            if (wasMoving && normalizedSpeed < 0.035f && throttle01 < 0.05f && Time.time >= nextStopEvent)
            {
                if (profile.stopClip != null && eventSource != null)
                {
                    eventSource.PlayOneShot(profile.stopClip);
                }

                wasMoving = false;
                nextStopEvent = Time.time + 1.5f;
            }
        }

        previousThrottle = throttle01;
    }

    private AudioSource CreateLoopSource(string sourceName, AudioClip clip)
    {
        if (clip == null || clip.length < MinimumContinuousLoopDuration)
        {
            return null;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.name = sourceName;
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        return source;
    }

    private AudioSource CreateEventSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.name = "Engine Events";
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.volume = profile == null ? 1f : profile.masterVolume;
        return source;
    }

    private static void PlayLoop(AudioSource source)
    {
        if (source != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    private void SetLayer(AudioSource source, float targetVolume, float targetPitch, float deltaTime)
    {
        if (source == null)
        {
            return;
        }

        PlayLoop(source);
        source.volume = Mathf.MoveTowards(source.volume, Mathf.Clamp01(targetVolume), deltaTime * volumeFadeSpeed);
        source.pitch = Mathf.MoveTowards(source.pitch, Mathf.Clamp(targetPitch, 0.1f, 3f), deltaTime * pitchSmoothing);
    }

    private void PlayRandomAccelerationClip()
    {
        if (eventSource == null || profile == null || profile.accelerationClips == null || profile.accelerationClips.Length == 0)
        {
            return;
        }

        AudioClip clip = profile.accelerationClips[Random.Range(0, profile.accelerationClips.Length)];
        if (clip != null)
        {
            eventSource.PlayOneShot(clip);
        }
    }
}
