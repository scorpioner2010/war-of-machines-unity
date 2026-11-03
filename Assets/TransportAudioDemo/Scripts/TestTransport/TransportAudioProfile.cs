using UnityEngine;

[CreateAssetMenu(fileName = "TransportAudioProfile", menuName = "TEST/Transport Audio Profile")]
public sealed class TransportAudioProfile : ScriptableObject
{
    public string displayName = "Transport";
    public AudioClip idleLoop;
    public AudioClip runLoop;
    public AudioClip highLoop;
    public AudioClip[] accelerationClips;
    public AudioClip startClip;
    public AudioClip stopClip;
    [Range(0.1f, 1.5f)] public float masterVolume = 0.9f;
    [Range(0.5f, 1.5f)] public float pitchMultiplier = 1f;
    [Min(1f)] public float maxSpeed = 12f;
    [Min(0.1f)] public float acceleration = 6f;
    [Min(0.1f)] public float braking = 9f;
    [Min(1f)] public float turnSpeed = 65f;
}
