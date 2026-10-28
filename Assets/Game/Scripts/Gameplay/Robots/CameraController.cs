using System;
using UnityEngine;
using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Settings;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-100)]
    public class CameraController : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int DefaultZoomStep = 0;
        private const float DeathSpectatorIntroSeconds = 3f;

        private enum DeathSpectatorPhase
        {
            Inactive,
            KillerIntro,
            DeathWreck,
            AllyFollow
        }

        private static CameraController _activeDeathSpectator;

        public VehicleRoot vehicleRoot;
        public Transform rig;
        public CameraCollisionController collisionController;

        private float _X;
        private float _Y;
        private float _targetDistance;
        private float _currentDistance;
        private int _currentZoomStep = DefaultZoomStep;
        private int _lastNonSniperZoomStep = DefaultZoomStep;
        private bool _sniperUiApplied;
        private bool _initialized;
        private DeathSpectatorPhase _deathSpectatorPhase;
        private float _deathSpectatorIntroEndTime;
        private int _pendingKillerObjectId = -1;
        private MatchTeam _spectatorTeam = MatchTeam.None;
        private VehicleRoot _spectatorTarget;
        private VehicleRoot _spectatorCanvasTarget;
        private VehicleRoot _spectatorVisibilityTarget;
        private Transform _deathWreckRig;
        private bool _deathFallbackPoseCaptured;
        private Vector3 _deathFallbackPosition;
        private GameplayRuntimeSettings _runtimeSettings = GameplayRuntimeSettings.Default;

        public bool IsSniperModeActive => IsSniperStep(_currentZoomStep);
        public CameraZoomProfile CurrentZoomProfile => GetZoomProfile(_currentZoomStep);
        public int CurrentZoomStep => _currentZoomStep;
        public bool IsDeathSpectatorActive => _deathSpectatorPhase != DeathSpectatorPhase.Inactive;
        public bool IsDeathSpectatorWreck => _deathSpectatorPhase == DeathSpectatorPhase.DeathWreck;
        public static CameraController ActiveDeathSpectator => _activeDeathSpectator;
        public VehicleRoot SpectatorTarget => _spectatorTarget;

        public event Action<VehicleRoot> SpectatorTargetChanged;
        public static event Action<CameraController> DeathSpectatorCameraChanged;

        public void SetVehicleRoot(VehicleRoot root)
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnClientDeath -= HandleClientDeath;
            }

            vehicleRoot = root;
            CacheSpectatorTeam();
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnClientDeath += HandleClientDeath;
            }
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            vehicleRoot = context.Root;
            CacheSpectatorTeam();
            if (context.IsOwner && !context.IsMenu)
            {
                Init();
            }
        }

        public void Init()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            Vector3 angles = transform.eulerAngles;
            _X = angles.y;
            _Y = angles.x;

            if (CameraSync.In != null)
            {
                CameraSync.In.target = transform;

            }

            _currentDistance = GetZoomProfile(DefaultZoomStep).cameraDistance;
            _currentZoomStep = DefaultZoomStep;
            _lastNonSniperZoomStep = DefaultZoomStep;
            ApplyZoomStep(DefaultZoomStep, true);
            _initialized = true;
            ApplyRuntimeZoomOverride(true);
            ApplyCameraTransform(true);
            SyncGameplayCameraTransform();
        }

        public void DetachFromVehicleForDeath()
        {
            _deathWreckRig = rig;
            Transform fallbackSource = _deathWreckRig != null ? _deathWreckRig : transform;
            _deathFallbackPosition = fallbackSource.position;
            _deathFallbackPoseCaptured = true;
            if (_deathWreckRig != null && _deathWreckRig != transform)
            {
                _deathWreckRig.SetParent(null, true);
            }

            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
        }

        private void OnDestroy()
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnClientDeath -= HandleClientDeath;
            }

            if (_deathSpectatorPhase != DeathSpectatorPhase.Inactive)
            {
                EndDeathSpectator();
            }

            if (_activeDeathSpectator == this)
            {
                _activeDeathSpectator = null;
                DeathSpectatorCameraChanged?.Invoke(null);
            }
        }

        public void RefreshSniperCameraPose()
        {
            if (!_initialized || !IsSniperModeActive)
            {
                return;
            }

            ApplyCameraTransform(true);
            SyncGameplayCameraTransform();
        }

        public float GetAimUiZoom01()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            CameraZoomProfile defaultProfile = GetZoomProfile(DefaultZoomStep);
            int sniperStep = GetSniperZoomStep();
            int zoomEndpointStep = sniperStep > DefaultZoomStep
                ? sniperStep
                : GetMaxForwardZoomStep();
            if (zoomEndpointStep <= DefaultZoomStep)
            {
                return IsSniperModeActive ? 1f : 0f;
            }

            if (IsSniperModeActive)
            {
                return 1f;
            }

            CameraZoomProfile sniperProfile = GetZoomProfile(zoomEndpointStep);
            float normalDistance = Mathf.Max(0.01f, defaultProfile.cameraDistance);
            float sniperDistance = Mathf.Max(0.01f, sniperProfile.cameraDistance);
            float minDistance = Mathf.Min(normalDistance, sniperDistance);
            float maxDistance = Mathf.Max(normalDistance, sniperDistance);
            if (maxDistance <= minDistance + 0.0001f)
            {
                return 0f;
            }

            CameraZoomProfile currentProfile = GetZoomProfile(_currentZoomStep);
            float currentDistance = Mathf.Clamp(currentProfile.cameraDistance, minDistance, maxDistance);
            float distance01 = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
            return Mathf.Clamp01(1f - distance01);
        }

        private void Update()
        {
            using (ProfileScope.Measure("Camera.Controller.Update", DiagnosticsCategories.Render))
            {
                if (!_initialized || rig == null)
                {
                    return;
                }

                if (IsDeathSpectatorActive)
                {
                    UpdateDeathSpectator();
                    if (!VehicleInputController.IsGameplayInputBlockedByUi)
                    {
                        UpdateDeathSpectatorInputs();
                    }

                    ApplyCameraTransform(false);
                    SyncGameplayCameraTransform();
                    UpdateFov();
                    return;
                }

                bool runtimeZoomOverrideActive = ApplyRuntimeZoomOverride(false);
                bool inputBlocked = VehicleInputController.IsGameplayInputBlockedByUi;
                if (!inputBlocked)
                {
                    if (!runtimeZoomOverrideActive)
                    {
                        UpdateAimInputs();
                    }

                    UpdateOrbitInputs();
                }

                ApplyCameraTransform(false);
                SyncGameplayCameraTransform();
                UpdateFov();
            }
        }

        private void SyncGameplayCameraTransform()
        {
            if (CameraSync.In != null && CameraSync.In.target == transform)
            {
                CameraSync.In.SyncToTarget();
            }
        }

        private void ApplyCameraTransform(bool immediate)
        {
            Transform cameraRig = GetCameraRig();
            if (cameraRig == null)
            {
                return;
            }

            bool useDeathFallbackPivot = IsDeathSpectatorActive
                                         && _deathSpectatorPhase == DeathSpectatorPhase.DeathWreck
                                         && cameraRig == transform
                                         && _deathFallbackPoseCaptured;
            Vector3 pivotPosition = useDeathFallbackPivot ? _deathFallbackPosition : cameraRig.position;
            _targetDistance = GetDistanceForZoomStep(_currentZoomStep);
            Quaternion rotation = Quaternion.Euler(_Y, _X, 0);
            if (IsSniperModeActive && TryGetSniperCameraPosition(rotation, out Vector3 sniperPosition))
            {
                _currentDistance = _targetDistance;
                transform.rotation = rotation;
                transform.position = sniperPosition;
                ResolveCameraCollision(pivotPosition);
                return;
            }

            if (immediate)
            {
                _currentDistance = _targetDistance;
            }
            else
            {
                _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                float cameraT = 1f - Mathf.Exp(-Mathf.Max(0.01f, _runtimeSettings.cameraDistanceLerpSpeed) * Time.deltaTime);
                _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, cameraT);
            }

            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -_currentDistance) + pivotPosition;

            transform.rotation = rotation;
            transform.position = position;
            ResolveCameraCollision(pivotPosition);
        }

        private void ResolveCameraCollision(Vector3 pivotPosition)
        {
            if (collisionController == null)
            {
                return;
            }

            collisionController.Resolve(pivotPosition);
        }

        private void HandleClientDeath(int killerObjectId)
        {
            if (!_initialized || vehicleRoot == null || !vehicleRoot.IsOwner || vehicleRoot.IsMenu)
            {
                return;
            }

            DetachFromVehicleForDeath();
            _deathSpectatorPhase = DeathSpectatorPhase.KillerIntro;
            _deathSpectatorIntroEndTime = Time.unscaledTime + DeathSpectatorIntroSeconds;
            _pendingKillerObjectId = killerObjectId;
            CacheSpectatorTeam();
            SpectatorTeamContext.Capture(_spectatorTeam);
            RegisterActiveDeathSpectator();
            ClearSpectatorTarget();

            if (IsSniperModeActive)
            {
                ApplyZoomStep(DefaultZoomStep, true);
            }

            TryResolveKillerTarget();
        }

        private void UpdateDeathSpectator()
        {
            if (_deathSpectatorPhase == DeathSpectatorPhase.KillerIntro)
            {
                TryResolveKillerTarget();
                if (_pendingKillerObjectId < 0 && !IsLiveBattleVehicle(_spectatorTarget))
                {
                    EnterDeathWreck();
                    return;
                }

                if (Time.unscaledTime >= _deathSpectatorIntroEndTime)
                {
                    EnterDeathWreck();
                }

                return;
            }

            if (_deathSpectatorPhase == DeathSpectatorPhase.AllyFollow
                && !IsLiveBattleVehicle(_spectatorTarget))
            {
                EnterDeathWreck();
            }
        }

        private void TryResolveKillerTarget()
        {
            if (_pendingKillerObjectId < 0
                || !VehicleColliderRegistry.TryGetRoot(_pendingKillerObjectId, out VehicleRoot killerRoot)
                || !IsLiveBattleVehicle(killerRoot))
            {
                return;
            }

            _pendingKillerObjectId = -1;
            SetSpectatorTarget(killerRoot, DeathSpectatorPhase.KillerIntro, true);
        }

        private void CycleSpectatorTargets()
        {
            if (_deathSpectatorPhase == DeathSpectatorPhase.KillerIntro)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                CycleSpectatorTarget(1);
            }

            if (Input.GetMouseButtonDown(1))
            {
                CycleSpectatorTarget(-1);
            }
        }

        private void CycleSpectatorTarget(int direction)
        {
            int count = VehicleRoot.ActiveVehicleCount;
            if (count <= 0)
            {
                return;
            }

            int currentIndex = FindActiveVehicleIndex(_spectatorTarget);
            int startIndex = currentIndex >= 0
                ? currentIndex
                : direction > 0 ? -1 : count;
            for (int offset = 1; offset <= count; offset++)
            {
                int candidateIndex = WrapIndex(startIndex + direction * offset, count);
                VehicleRoot candidate = VehicleRoot.GetActiveVehicle(candidateIndex);
                if (!IsSpectatorCandidate(candidate))
                {
                    continue;
                }

                SetSpectatorTarget(candidate, DeathSpectatorPhase.AllyFollow, false);
                return;
            }
        }

        private static int FindActiveVehicleIndex(VehicleRoot target)
        {
            if (target == null)
            {
                return -1;
            }

            int count = VehicleRoot.ActiveVehicleCount;
            for (int vehicleIndex = 0; vehicleIndex < count; vehicleIndex++)
            {
                if (VehicleRoot.GetActiveVehicle(vehicleIndex) == target)
                {
                    return vehicleIndex;
                }
            }

            return -1;
        }

        private static int WrapIndex(int index, int count)
        {
            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static bool IsLiveBattleVehicle(VehicleRoot candidate)
        {
            return candidate != null
                   && !candidate.IsMenu
                   && candidate.health != null
                   && candidate.networkObject != null
                   && candidate.networkObject.IsSpawned
                   && !candidate.health.IsDead;
        }

        private bool IsSpectatorCandidate(VehicleRoot candidate)
        {
            if (!IsLiveBattleVehicle(candidate))
            {
                return false;
            }

            CacheSpectatorTeam();
            if (!MatchTeamUtility.IsAssigned(_spectatorTeam) || candidate.characterInit == null)
            {
                return false;
            }

            return MatchTeamUtility.AreSameAssignedTeam(
                _spectatorTeam,
                candidate.characterInit.Team.Value);
        }

        private void CacheSpectatorTeam()
        {
            if (vehicleRoot == null || vehicleRoot.characterInit == null)
            {
                return;
            }

            MatchTeam team = vehicleRoot.characterInit.Team.Value;
            if (MatchTeamUtility.IsAssigned(team))
            {
                _spectatorTeam = team;
            }
        }

        private bool IsAllowedSpectatorTarget(VehicleRoot target, bool allowAutomaticKillerTarget)
        {
            if (allowAutomaticKillerTarget)
            {
                return IsLiveBattleVehicle(target);
            }

            return IsSpectatorCandidate(target);
        }

        private void SetSpectatorTarget(
            VehicleRoot target,
            DeathSpectatorPhase phase,
            bool allowAutomaticKillerTarget)
        {
            if (target == null || !IsAllowedSpectatorTarget(target, allowAutomaticKillerTarget))
            {
                EnterDeathWreck();
                return;
            }

            if (_spectatorCanvasTarget != null && _spectatorCanvasTarget != target
                && _spectatorCanvasTarget.vehicleHUD != null)
            {
                _spectatorCanvasTarget.vehicleHUD.SetWorldCanvasHidden(false);
            }

            UpdateSpectatorVisibilityOverride(phase == DeathSpectatorPhase.KillerIntro ? target : null);
            _spectatorTarget = target;
            _spectatorCanvasTarget = target;
            _deathSpectatorPhase = phase;
            if (target.vehicleHUD != null)
            {
                target.vehicleHUD.SetWorldCanvasHidden(true);
            }

            SpectatorTargetChanged?.Invoke(target);

            Transform targetRig = GetTargetRig(target);
            if (targetRig == null)
            {
                return;
            }

            Vector3 forward = targetRig.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                _X = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }
        }

        private void EnterDeathWreck()
        {
            _pendingKillerObjectId = -1;
            ClearSpectatorTarget();
            _deathSpectatorPhase = DeathSpectatorPhase.DeathWreck;
        }

        private void ClearSpectatorTarget()
        {
            if (_spectatorCanvasTarget != null && _spectatorCanvasTarget.vehicleHUD != null)
            {
                _spectatorCanvasTarget.vehicleHUD.SetWorldCanvasHidden(false);
            }

            UpdateSpectatorVisibilityOverride(null);
            bool changed = _spectatorTarget != null || _spectatorCanvasTarget != null;
            _spectatorTarget = null;
            _spectatorCanvasTarget = null;
            if (changed)
            {
                SpectatorTargetChanged?.Invoke(null);
            }
        }

        private void UpdateSpectatorVisibilityOverride(VehicleRoot target)
        {
            if (_spectatorVisibilityTarget != null && _spectatorVisibilityTarget != target
                && _spectatorVisibilityTarget.clientVisibility != null)
            {
                _spectatorVisibilityTarget.clientVisibility.SetSpectatorVisibility(false);
            }

            _spectatorVisibilityTarget = target;
            if (target != null && target.clientVisibility != null)
            {
                target.clientVisibility.SetSpectatorVisibility(true);
            }
        }

        private void RegisterActiveDeathSpectator()
        {
            if (_activeDeathSpectator == this)
            {
                return;
            }

            _activeDeathSpectator = this;
            DeathSpectatorCameraChanged?.Invoke(this);
        }

        private void EndDeathSpectator()
        {
            ClearSpectatorTarget();
            _pendingKillerObjectId = -1;
            _deathSpectatorPhase = DeathSpectatorPhase.Inactive;
            SpectatorTeamContext.Clear();
            ReleaseDeathWreckRig();
        }

        private void ReleaseDeathWreckRig()
        {
            Transform deathWreckRig = _deathWreckRig;
            _deathWreckRig = null;
            _deathFallbackPoseCaptured = false;
            _deathFallbackPosition = default;
            if (deathWreckRig != null && deathWreckRig != transform)
            {
                Destroy(deathWreckRig.gameObject);
            }
        }

        private Transform GetCameraRig()
        {
            if (IsDeathSpectatorActive)
            {
                if (_deathSpectatorPhase == DeathSpectatorPhase.KillerIntro
                    || _deathSpectatorPhase == DeathSpectatorPhase.AllyFollow)
                {
                    if (IsLiveBattleVehicle(_spectatorTarget))
                    {
                        Transform spectatorRig = GetTargetRig(_spectatorTarget);
                        if (spectatorRig != null)
                        {
                            return spectatorRig;
                        }
                    }
                }

                return _deathWreckRig != null ? _deathWreckRig : transform;
            }

            return rig;
        }

        private static Transform GetTargetRig(VehicleRoot target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.cameraController != null && target.cameraController.rig != null)
            {
                return target.cameraController.rig;
            }

            return target.transform;
        }

        private bool TryGetSniperCameraPosition(Quaternion cameraRotation, out Vector3 position)
        {
            position = default;

            Transform anchor = GetSniperCameraAnchor();
            if (anchor == null)
            {
                return false;
            }

            Vector3 forward = cameraRotation * Vector3.forward;
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            forward.Normalize();

            Vector3 up = cameraRotation * Vector3.up;
            if (!IsFinite(up) || up.sqrMagnitude <= 0.000001f)
            {
                up = Vector3.up;
            }
            else
            {
                up.Normalize();
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            float profileDistance = GetDistanceForZoomStep(_currentZoomStep);
            position = anchor.position
                       + forward * (Mathf.Max(0f, _runtimeSettings.cameraSniperForwardOffset) + profileDistance)
                       + up * _runtimeSettings.cameraSniperVerticalOffset;
            return IsFinite(position);
        }

        private Transform GetSniperCameraAnchor()
        {
            if (vehicleRoot == null)
            {
                return rig;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            if (_runtimeSettings.cameraSniperFromMuzzle
                && vehicleRoot.shooterNet != null
                && vehicleRoot.shooterNet.muzzleTransform != null)
            {
                return vehicleRoot.shooterNet.muzzleTransform;
            }

            if (vehicleRoot.weaponAimAtCamera != null && vehicleRoot.weaponAimAtCamera.gun != null)
            {
                return vehicleRoot.weaponAimAtCamera.gun;
            }

            return rig;
        }

        private void UpdateAimInputs()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                ToggleSniper();
            }

            int scrollDirection = GetScrollDirection();
            if (scrollDirection > 0)
            {
                ZoomIn();
            }
            else if (scrollDirection < 0)
            {
                ZoomOut();
            }
        }

        private bool ApplyRuntimeZoomOverride(bool immediate)
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (!_runtimeSettings.runtimeZoomOverrideEnabled)
            {
                return false;
            }

            int runtimeZoomIndex = _runtimeSettings.runtimeZoomIndex;
            if (FindZoomProfile(_runtimeSettings.cameraZoomProfiles, runtimeZoomIndex) == null)
            {
                return false;
            }

            if (_currentZoomStep != runtimeZoomIndex)
            {
                ApplyZoomStep(runtimeZoomIndex, immediate);
            }

            return true;
        }

        private void UpdateOrbitInputs()
        {
            float mouseSensitivity = GetMouseSensitivity();
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            _X += Input.GetAxis("Mouse X") * _runtimeSettings.cameraHorizontalSpeed * mouseSensitivity * 0.02f;
            _Y -= Input.GetAxis("Mouse Y") * _runtimeSettings.cameraVerticalSpeed * mouseSensitivity * 0.02f;
            _Y = Mathf.Clamp(_Y, _runtimeSettings.cameraMinPitch, _runtimeSettings.cameraMaxPitch);
        }

        private void UpdateDeathSpectatorInputs()
        {
            UpdateOrbitInputs();
            CycleSpectatorTargets();

            int scrollDirection = GetScrollDirection();
            if (scrollDirection < 0)
            {
                ZoomOut();
            }
            else if (scrollDirection > 0 && _currentZoomStep < DefaultZoomStep)
            {
                ZoomIn();
            }
        }

        private float GetMouseSensitivity()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                return ClientGameplaySettings.SniperMouseSensitivity;
            }

            return ClientGameplaySettings.GameplayMouseSensitivity;
        }

        private int GetScrollDirection()
        {
            float scroll = Input.mouseScrollDelta.y;
            float legacyScroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(legacyScroll) > Mathf.Abs(scroll))
            {
                scroll = legacyScroll;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (Mathf.Abs(scroll) <= Mathf.Max(0.0001f, _runtimeSettings.cameraScrollDeadZone))
            {
                return 0;
            }

            if (scroll > 0f)
            {
                return 1;
            }

            return -1;
        }

        private void ToggleSniper()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                ExitSniper();
                return;
            }

            EnterSniper();
        }

        private void ZoomIn()
        {
            int maxForwardZoomStep = GetMaxForwardZoomStep();
            if (_currentZoomStep < maxForwardZoomStep)
            {
                ApplyZoomStep(_currentZoomStep + 1, false);
            }
        }

        private void ZoomOut()
        {
            int minBackwardZoomStep = -GetMaxBackwardZoomStep();
            if (_currentZoomStep > minBackwardZoomStep)
            {
                ApplyZoomStep(_currentZoomStep - 1, false);
            }
        }

        private void EnterSniper()
        {
            int sniperStep = GetSniperZoomStep();
            if (sniperStep <= DefaultZoomStep)
            {
                return;
            }

            if (!IsSniperStep(_currentZoomStep))
            {
                _lastNonSniperZoomStep = _currentZoomStep;
            }

            ApplyZoomStep(sniperStep, false);
        }

        private void AlignSniperCameraToCurrentGunAim(int sniperStep)
        {
            if (!TryGetCurrentGunAimPoint(out Vector3 aimPoint))
            {
                return;
            }

            Transform anchor = GetSniperCameraAnchor();
            if (anchor == null)
            {
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (sniperStep <= DefaultZoomStep)
            {
                return;
            }

            Vector3 origin = anchor.position;
            float sniperProfileDistance = GetDistanceForZoomStep(sniperStep);
            float yaw = _X;
            float pitch = _Y;

            for (int i = 0; i < 2; i++)
            {
                Vector3 direction = aimPoint - origin;
                if (!TryGetCameraAngles(direction, out yaw, out pitch))
                {
                    return;
                }

                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 forward = rotation * Vector3.forward;
                Vector3 up = rotation * Vector3.up;
                origin = anchor.position
                         + forward * (Mathf.Max(0f, _runtimeSettings.cameraSniperForwardOffset) + sniperProfileDistance)
                         + up * _runtimeSettings.cameraSniperVerticalOffset;
            }

            _X = yaw;
            _Y = pitch;
        }

        private bool TryGetCurrentGunAimPoint(out Vector3 aimPoint)
        {
            aimPoint = default;

            WeaponAimController weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim == null)
            {
                return false;
            }

            Vector3 origin = weaponAim.gun != null ? weaponAim.gun.position : transform.position;
            if (IsUsableAimPoint(weaponAim.CurrentAimPoint, origin))
            {
                aimPoint = weaponAim.CurrentAimPoint;
                return true;
            }

            if (IsUsableAimPoint(weaponAim.DesiredAimPoint, origin))
            {
                aimPoint = weaponAim.DesiredAimPoint;
                return true;
            }

            if (weaponAim.gun == null)
            {
                return false;
            }

            Vector3 forward = weaponAim.GetLogicalAimForwardWorld();
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            forward.Normalize();
            aimPoint = weaponAim.gun.position + forward * Mathf.Max(0.25f, weaponAim.maxAimDistance);
            return IsFinite(aimPoint);
        }

        private static bool IsUsableAimPoint(Vector3 aimPoint, Vector3 origin)
        {
            return IsFinite(aimPoint) && (aimPoint - origin).sqrMagnitude > 0.000001f;
        }

        private bool TryGetCameraAngles(Vector3 direction, out float yaw, out float pitch)
        {
            yaw = _X;
            pitch = _Y;

            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 euler = rotation.eulerAngles;

            yaw = euler.y;
            pitch = Mathf.Clamp(NormalizeSignedAngle(euler.x), _runtimeSettings.cameraMinPitch, _runtimeSettings.cameraMaxPitch);
            return true;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        private void ExitSniper()
        {
            int returnStep = _lastNonSniperZoomStep;
            if (returnStep < -GetMaxBackwardZoomStep()
                || returnStep > GetMaxForwardZoomStep()
                || IsSniperStep(returnStep))
            {
                returnStep = DefaultZoomStep;
            }

            ApplyZoomStep(returnStep, false);
        }

        private void AlignOrbitCameraToCurrentGunAim()
        {
            if (rig == null || !TryGetCurrentGunAimPoint(out Vector3 aimPoint))
            {
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (TryGetCameraAngles(aimPoint - rig.position, out float yaw, out float pitch))
            {
                _X = yaw;
                _Y = pitch;
            }
        }

        private void ApplyZoomStep(int zoomStep, bool immediate)
        {
            int maxForwardZoomStep = GetMaxForwardZoomStep();
            if (zoomStep > maxForwardZoomStep)
            {
                zoomStep = maxForwardZoomStep;
            }
            else if (zoomStep < -GetMaxBackwardZoomStep())
            {
                zoomStep = -GetMaxBackwardZoomStep();
            }

            bool wasSniperStep = IsSniperStep(_currentZoomStep);
            bool isSniperStep = IsSniperStep(zoomStep);
            if (isSniperStep && !wasSniperStep)
            {
                AlignSniperCameraToCurrentGunAim(zoomStep);
            }
            else if (!isSniperStep && wasSniperStep)
            {
                AlignOrbitCameraToCurrentGunAim();
            }

            _currentZoomStep = zoomStep;
            if (!IsSniperStep(zoomStep))
            {
                _lastNonSniperZoomStep = zoomStep;
            }

            if (GameplayGUI.In != null)
            {
                GameplayGUI.In.SetZoomLevel(_currentZoomStep);
            }

            _targetDistance = GetDistanceForZoomStep(zoomStep);
            if (immediate)
            {
                _currentDistance = _targetDistance;
                ApplyCameraFov(GetFovForZoomStep(zoomStep), true);
            }

            ApplySniperUi(isSniperStep, immediate);
        }

        private int GetMaxForwardZoomStep()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            int maxForwardStep = 0;
            CameraZoomProfile[] profiles = _runtimeSettings.cameraZoomProfiles;
            if (profiles == null)
            {
                return maxForwardStep;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && profiles[i].zoomIndex > maxForwardStep)
                {
                    maxForwardStep = profiles[i].zoomIndex;
                }
            }

            return maxForwardStep;
        }

        private int GetMaxBackwardZoomStep()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            int maxBackwardStep = 0;
            CameraZoomProfile[] profiles = _runtimeSettings.cameraZoomProfiles;
            if (profiles == null)
            {
                return maxBackwardStep;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && profiles[i].zoomIndex < 0)
                {
                    maxBackwardStep = Mathf.Max(maxBackwardStep, -profiles[i].zoomIndex);
                }
            }

            return maxBackwardStep;
        }

        private int GetSniperZoomStep()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            CameraZoomProfile[] profiles = _runtimeSettings.cameraZoomProfiles;
            if (profiles == null)
            {
                return -1;
            }

            int sniperStep = -1;
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null || profiles[i].zoomIndex <= DefaultZoomStep || !profiles[i].showReticle)
                {
                    continue;
                }

                if (sniperStep < 0 || profiles[i].zoomIndex < sniperStep)
                {
                    sniperStep = profiles[i].zoomIndex;
                }
            }

            return sniperStep;
        }

        private bool IsSniperStep(int zoomStep)
        {
            if (zoomStep <= DefaultZoomStep)
            {
                return false;
            }

            CameraZoomProfile profile = GetZoomProfile(zoomStep);
            return profile != null && profile.showReticle;
        }

        private CameraZoomProfile GetZoomProfile(int zoomStep)
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            CameraZoomProfile profile = FindZoomProfile(_runtimeSettings.cameraZoomProfiles, zoomStep);
            if (profile != null)
            {
                return profile;
            }

            profile = FindZoomProfile(GameplayRuntimeSettings.Default.cameraZoomProfiles, zoomStep);
            if (profile != null)
            {
                return profile;
            }

            return FindZoomProfile(GameplayRuntimeSettings.Default.cameraZoomProfiles, DefaultZoomStep)
                   ?? new CameraZoomProfile();
        }

        private static CameraZoomProfile FindZoomProfile(CameraZoomProfile[] profiles, int zoomIndex)
        {
            if (profiles == null)
            {
                return null;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && profiles[i].zoomIndex == zoomIndex)
                {
                    return profiles[i];
                }
            }

            return null;
        }

        private float GetDistanceForZoomStep(int zoomStep)
        {
            return Mathf.Max(0.01f, GetZoomProfile(zoomStep).cameraDistance);
        }

        private float GetFovForZoomStep(int zoomStep)
        {
            return Mathf.Clamp(GetZoomProfile(zoomStep).fieldOfView, 1f, 179f);
        }

        private void UpdateFov()
        {
            if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
            {
                return;
            }

            ApplyCameraFov(GetFovForZoomStep(_currentZoomStep), false);
        }

        private void ApplyCameraFov(float targetFov, bool immediate)
        {
            if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
            {
                return;
            }

            Camera cam = CameraSync.In.gameplayCamera;
            if (immediate)
            {
                cam.fieldOfView = targetFov;
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, _runtimeSettings.cameraFovLerpSpeed) * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, t);
        }

        private void ApplySniperUi(bool enabled, bool force)
        {
            if (!force && _sniperUiApplied == enabled)
            {
                return;
            }

            _sniperUiApplied = enabled;

            GunCrosshair crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            if (crosshair != null)
            {
                crosshair.SetSniperMode(enabled);
            }

            SniperScopeOverlay.SetActiveScreen(enabled);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                   && !float.IsNaN(value.y)
                   && !float.IsNaN(value.z)
                   && !float.IsInfinity(value.x)
                   && !float.IsInfinity(value.y)
                   && !float.IsInfinity(value.z);
        }
    }
}
