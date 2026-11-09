using Game.Scripts.Audio;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleRoot))]
    public sealed class RobotEngineAudio : MonoBehaviour, IVehicleRootAware
    {
        private const float MovingThreshold = 0.08f;
        private const float StoppedThreshold = 0.035f;

        [SerializeField] private string robotCode;
        [SerializeField] private float volumeFadeSpeed = 3.5f;
        [SerializeField] private float pitchSmoothing = 4.5f;

        [SerializeField] private VehicleRoot _vehicleRoot;
        private AudioEngineProfile _profile;
        private string _resolvedRobotCode;
        private AudioPreset _idleAudio;
        private AudioPreset _runAudio;
        private AudioPreset _highAudio;
        private Transform _cachedObjectMover;
        private Transform _cachedAudioAnchor;
        private float _activityWeight;
        private float _rpm01;
        private float _previousThrottle;
        private float _nextAccelerationEvent;
        private float _nextStopEvent;
        private bool _wasMoving;
        private bool _hasStarted;
        private bool _missingProfileWarningShown;

        public void SetVehicleRoot(VehicleRoot vehicleRoot)
        {
            _vehicleRoot = vehicleRoot;
            _cachedObjectMover = null;
            _cachedAudioAnchor = null;
        }

        private void Update()
        {
            if (_vehicleRoot == null
                || !_vehicleRoot.IsClientInitialized
                || (_vehicleRoot.health != null && _vehicleRoot.health.IsDead))
            {
                StopPlayback();
                return;
            }

            if (!TryResolveProfile())
            {
                StopPlayback();
                return;
            }

            EnsureLoop(ref _idleAudio, _profile.idleLoop);
            EnsureLoop(ref _runAudio, _profile.runLoop);
            EnsureLoop(ref _highAudio, _profile.highLoop);
            PlayStartClipIfNeeded();

            Vector2 movement = _vehicleRoot.inputManager != null
                ? _vehicleRoot.inputManager.AnimMove
                : Vector2.zero;
            float throttle01 = Mathf.Clamp01(Mathf.Abs(movement.y));
            float normalizedSpeed = Mathf.Clamp01(movement.magnitude);
            Tick(throttle01, normalizedSpeed);
        }

        private bool TryResolveProfile()
        {
            string nextRobotCode = ResolveRobotCode();
            if (string.IsNullOrWhiteSpace(nextRobotCode))
            {
                return false;
            }

            if (_profile != null && string.Equals(_resolvedRobotCode, nextRobotCode, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            StopPlayback();
            _resolvedRobotCode = nextRobotCode;
            _profile = SoundCaller.GetEngineProfile(nextRobotCode);
            if (_profile == null)
            {
                if (!_missingProfileWarningShown)
                {
                    Debug.LogWarning(
                        $"Engine audio profile was not found for robot '{nextRobotCode}'.",
                        this);
                    _missingProfileWarningShown = true;
                }

                return false;
            }

            _missingProfileWarningShown = false;
            return true;
        }

        private string ResolveRobotCode()
        {
            if (_vehicleRoot != null
                && _vehicleRoot.RuntimeStats != null
                && !string.IsNullOrWhiteSpace(_vehicleRoot.RuntimeStats.Code))
            {
                return _vehicleRoot.RuntimeStats.Code;
            }

            return robotCode;
        }

        private void EnsureLoop(ref AudioPreset audio, AudionElement element)
        {
            if (!HasClip(element))
            {
                audio?.Stop();
                audio = null;
                return;
            }

            if (audio != null && audio.IsBusy)
            {
                AttachToAudioAnchor(audio);
                return;
            }

            audio = SoundCaller.PlayLoopedSfx(element, GetAudioAnchor());
            audio?.SetPlaybackVolume(0f);
        }

        private void PlayStartClipIfNeeded()
        {
            if (_hasStarted || _profile == null)
            {
                return;
            }

            AudioPreset startAudio = PlayProfileSfx(_profile.startClip);
            bool hasLoop = _idleAudio != null || _runAudio != null || _highAudio != null;
            if (startAudio != null || hasLoop || !HasClip(_profile.startClip))
            {
                _hasStarted = true;
                _nextAccelerationEvent = Time.time + 0.8f;
                _nextStopEvent = Time.time + 0.25f;
            }
        }

        private void Tick(float throttle01, float normalizedSpeed)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float demand = Mathf.Clamp01(Mathf.Max(throttle01, normalizedSpeed * 0.72f));
            float targetRpm = Mathf.Clamp01(Mathf.Max(demand, normalizedSpeed * 0.55f));
            _rpm01 = Mathf.MoveTowards(_rpm01, targetRpm, deltaTime * 1.8f);
            _activityWeight = Mathf.MoveTowards(
                _activityWeight,
                1f,
                deltaTime * Mathf.Max(0.01f, volumeFadeSpeed));

            float idleWeight = 1f - Mathf.SmoothStep(0.04f, 0.52f, _rpm01);
            float runWeight = Mathf.SmoothStep(0.16f, 0.68f, _rpm01)
                              * (1f - Mathf.SmoothStep(0.78f, 1f, _rpm01) * 0.3f);
            float highWeight = Mathf.SmoothStep(0.58f, 0.98f, _rpm01);
            if (_highAudio == null)
            {
                runWeight = Mathf.Max(runWeight, highWeight);
            }

            float masterVolume = SoundCaller.GetEngineMasterVolume();
            float pitchMultiplier = Mathf.Clamp(_profile.pitchMultiplier, 0.1f, 3f);
            SetLayer(
                _idleAudio,
                idleWeight * _activityWeight * masterVolume,
                Mathf.Lerp(0.88f, 1.06f, _rpm01) * pitchMultiplier,
                deltaTime);
            SetLayer(
                _runAudio,
                runWeight * _activityWeight * masterVolume,
                Mathf.Lerp(0.82f, 1.12f, _rpm01) * pitchMultiplier,
                deltaTime);
            SetLayer(
                _highAudio,
                highWeight * _activityWeight * masterVolume,
                Mathf.Lerp(0.76f, 1.15f, _rpm01) * pitchMultiplier,
                deltaTime);

            if (normalizedSpeed > MovingThreshold)
            {
                _wasMoving = true;
            }

            if (throttle01 > 0.72f
                && _previousThrottle < 0.45f
                && Time.time >= _nextAccelerationEvent)
            {
                PlayRandomAccelerationClip();
                _nextAccelerationEvent = Time.time + 1.1f;
            }

            if (_wasMoving
                && normalizedSpeed < StoppedThreshold
                && throttle01 < 0.05f
                && Time.time >= _nextStopEvent)
            {
                if (HasClip(_profile.stopClip))
                {
                    PlayProfileSfx(_profile.stopClip);
                }

                _wasMoving = false;
                _nextStopEvent = Time.time + 1.5f;
            }

            _previousThrottle = throttle01;
        }

        private void SetLayer(
            AudioPreset audio,
            float targetVolume,
            float targetPitch,
            float deltaTime)
        {
            if (audio == null)
            {
                return;
            }

            audio.SetPlaybackVolume(Mathf.MoveTowards(
                GetCurrentPlaybackVolume(audio),
                Mathf.Clamp01(targetVolume),
                deltaTime * Mathf.Max(0.01f, volumeFadeSpeed)));
            audio.SetPitch(Mathf.MoveTowards(
                GetCurrentPitch(audio),
                Mathf.Clamp(targetPitch, 0.1f, 3f),
                deltaTime * Mathf.Max(0.01f, pitchSmoothing)));
        }

        private static float GetCurrentPlaybackVolume(AudioPreset audio)
        {
            return audio == null ? 0f : audio.PlaybackVolume;
        }

        private static float GetCurrentPitch(AudioPreset audio)
        {
            return audio == null ? 1f : audio.Pitch;
        }

        private void PlayRandomAccelerationClip()
        {
            if (_profile == null
                || _profile.accelerationClips == null
                || _profile.accelerationClips.Length == 0)
            {
                return;
            }

            AudionElement element = _profile.accelerationClips[
                Random.Range(0, _profile.accelerationClips.Length)];
            if (HasClip(element))
            {
                PlayProfileSfx(element);
            }
        }

        private AudioPreset PlayProfileSfx(AudionElement element)
        {
            if (_profile == null || !HasClip(element))
            {
                return null;
            }

            AudioPreset audio = SoundCaller.PlaySfx(element, GetAudioAnchor());
            audio?.SetPlaybackVolume(SoundCaller.GetEngineMasterVolume());
            return audio;
        }

        private Transform GetAudioAnchor()
        {
            Transform objectMover = _vehicleRoot != null && _vehicleRoot.objectMover != null
                ? _vehicleRoot.objectMover.transform
                : null;
            if (_cachedAudioAnchor == null || objectMover != _cachedObjectMover)
            {
                _cachedObjectMover = objectMover;
                _cachedAudioAnchor = objectMover != null ? objectMover : transform;
            }

            return _cachedAudioAnchor;
        }

        private void AttachToAudioAnchor(AudioPreset audio)
        {
            if (audio == null)
            {
                return;
            }

            Transform audioAnchor = GetAudioAnchor();
            if (audio.transform.parent == audioAnchor)
            {
                return;
            }

            audio.transform.SetParent(audioAnchor, false);
            audio.transform.localPosition = Vector3.zero;
        }

        private void StopPlayback()
        {
            _idleAudio?.Stop();
            _runAudio?.Stop();
            _highAudio?.Stop();
            _idleAudio = null;
            _runAudio = null;
            _highAudio = null;
            _activityWeight = 0f;
            _rpm01 = 0f;
            _previousThrottle = 0f;
            _wasMoving = false;
            _hasStarted = false;
        }

        private void OnDisable()
        {
            StopPlayback();
        }

        private void OnDestroy()
        {
            StopPlayback();
        }

        private static bool HasClip(AudionElement element)
        {
            if (element == null || element.clips == null)
            {
                return false;
            }

            for (int i = 0; i < element.clips.Length; i++)
            {
                if (element.clips[i] != null && element.clips[i].clip != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
