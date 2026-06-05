using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.AI.WaypointGraph;
using Game.Scripts.Diagnostics;
using Game.Scripts.MenuController;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleInputController : NetworkBehaviour, IVehicleRootAware, IBotInputReceiver
    {
        private const float RightMouseAutoAimTapSeconds = 0.18f;
        private const int MaxCruiseSpeedLevel = 3;
        private const float CruiseSpeedStep = 1f / MaxCruiseSpeedLevel;

        public VehicleRoot vehicleRoot;
        public VehicleAutoAimController autoAimController;

        private Vector2 _moveServer;
        private bool _shootServer;
        private bool _actionServer;
        private int _lastInputSeqServer;

        private Vector2 _moveLocal;
        private bool _shootLocal;
        private bool _actionLocal;

        private readonly SyncVar<Vector2> _animMove = new(Vector2.zero);
        private readonly SyncVar<bool> _animShoot = new(false);
        private readonly SyncVar<bool> _animAction = new(false);

        private float _nextSendTime;

        private int _seq;
        private Vector2 _lastSentMove;
        private bool _lastSentShoot;
        private bool _lastSentAction;
        private short _lastSentYawQ;
        private short _lastSentPitchQ;
        private Vector3 _lastSentAimPoint;
        private Vector3 _lastAimPointLocal;
        private Vector3 _lastAimForwardLocal;

        private float _turretYawLocal;
        private float _gunPitchLocal;
        private bool _turretAimLockActive;
        private float _rightMouseDownTime;
        private float _lockedTurretYawLocal;
        private float _lockedGunPitchLocal;
        private Vector3 _lockedAimPointLocal;
        private Vector3 _lockedAimForwardLocal;
        private int _cruiseSpeedLevel;

        private bool _controlsBlocked;

        public static bool Escape => UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            autoAimController = root != null ? root.autoAimController : null;
        }

        public void SetControlsBlocked(bool blocked)
        {
            _controlsBlocked = blocked;
        }

        [Server]
        public void ApplyBotInput(float forward, float turn)
        {
            VehicleServerInput input = VehicleServerInput.Movement(new Vector2(turn, forward));
            input.Shoot = _shootServer;
            input.Action = _actionServer;
            ServerSetExternalInput(input, true);
        }

        [Server]
        public void ServerSetExternalInput(VehicleServerInput input)
        {
            ServerSetExternalInput(input, true);
        }

        [Server]
        public void ServerSetExternalInput(VehicleServerInput input, bool syncAnimation)
        {
            Vector2 move = input.Move;
            move.x = Mathf.Clamp(move.x, -1f, 1f);
            move.y = Mathf.Clamp(move.y, -1f, 1f);

            ApplyServerInput(move, input.Shoot, input.Action, syncAnimation);

            if (!input.HasAim || vehicleRoot == null)
            {
                return;
            }

            if (vehicleRoot.weaponAimAtCamera != null)
            {
                vehicleRoot.weaponAimAtCamera.SetDesiredAimPointServer(input.AimPoint, input.AimForward);
                vehicleRoot.weaponAimAtCamera.SetTargetPitchServer(input.TargetPitchDeg);
            }

            if (vehicleRoot.robotHullRotation != null)
            {
                vehicleRoot.robotHullRotation.SetTargetYawServer(input.TargetYawDeg);
            }
        }

        [Server]
        public void ServerClearExternalInput()
        {
            ApplyServerInput(Vector2.zero, false, false, true);
        }

        public bool IsControlsBlocked => _controlsBlocked;
        public static bool IsGameplayInputBlockedByUi
        {
            get
            {
                if (!MenuManager.IsReady)
                {
                    return false;
                }

                if (MenuManager.CurrentType == MenuType.GameplayPause || MenuManager.CurrentType == MenuType.EndGame)
                {
                    return true;
                }

                return MenuManager.CurrentType == MenuType.Settings && MenuManager.PreviousType == MenuType.GameplayPause;
            }
        }

        private bool IsLocalInputBlocked => _controlsBlocked || IsGameplayInputBlockedByUi;
        private bool HasLocalInput => IsOwner;

        public Vector2 Move
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _moveServer;
                }
                if (HasLocalInput)
                {
                    return IsLocalInputBlocked ? Vector2.zero : _moveLocal;
                }
                return Vector2.zero;
            }
        }

        public bool Shoot
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _shootServer;
                }
                if (HasLocalInput)
                {
                    return !IsLocalInputBlocked && _shootLocal;
                }
                return false;
            }
        }

        public bool Action
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _actionServer;
                }
                if (HasLocalInput)
                {
                    return !IsLocalInputBlocked && _actionLocal;
                }
                return false;
            }
        }

        public Vector2 AnimMove
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _moveServer;
                }
                if (HasLocalInput)
                {
                    return IsLocalInputBlocked ? Vector2.zero : _moveLocal;
                }
                return _animMove.Value;
            }
        }

        public bool AnimShoot
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _shootServer;
                }
                if (HasLocalInput)
                {
                    return !IsLocalInputBlocked && _shootLocal;
                }
                return _animShoot.Value;
            }
        }

        public bool AnimAction
        {
            get
            {
                if (IsServerInitialized)
                {
                    return _actionServer;
                }
                if (HasLocalInput)
                {
                    return !IsLocalInputBlocked && _actionLocal;
                }
                return _animAction.Value;
            }
        }

        private void Update()
        {
            if (!HasLocalInput)
            {
                return;
            }

            using (ProfileScope.Measure("Client.VehicleInput.Update", DiagnosticsCategories.Client))
            {
                bool blocked = IsLocalInputBlocked;
                ReadMovementInput(blocked, out float x, out float y);

                bool newShoot = !blocked && Input.GetMouseButton(0);
                bool newAction = !blocked && Input.GetKey(KeyCode.Space);
                ResolveAutoAimController();
                bool turretAimLocked = HandleRightMouseAimLock(blocked, out bool aimLockChanged);

                bool autoAimActive = false;
                Vector3 autoAimPoint = Vector3.zero;
                Vector3 autoAimForward = Vector3.zero;
                if (!blocked && !turretAimLocked && autoAimController != null)
                {
                    autoAimActive = autoAimController.TryGetAimTarget(out autoAimPoint, out autoAimForward);
                }

                _moveLocal = new Vector2(x, y);
                _shootLocal = newShoot;
                _actionLocal = newAction;

                float yawDeg, pitchDeg;
                Vector3 aimPoint;
                Vector3 aimForward;

                if (blocked)
                {
                    yawDeg = AngleQuantization.DequantizeAngle01(_lastSentYawQ);
                    pitchDeg = AngleQuantization.DequantizeAngle01(_lastSentPitchQ);
                    aimPoint = _lastAimPointLocal;
                    aimForward = _lastAimForwardLocal;
                }
                else if (turretAimLocked)
                {
                    yawDeg = _lockedTurretYawLocal;
                    pitchDeg = _lockedGunPitchLocal;
                    aimPoint = _lockedAimPointLocal;
                    aimForward = _lockedAimForwardLocal;
                    _turretYawLocal = yawDeg;
                    _gunPitchLocal = pitchDeg;
                    _lastAimPointLocal = aimPoint;
                    _lastAimForwardLocal = aimForward;
                }
                else if (autoAimActive)
                {
                    ComputeAutoAimYawPitch(autoAimPoint, autoAimForward, out yawDeg, out pitchDeg, out aimPoint, out aimForward);
                    _lastAimPointLocal = aimPoint;
                    _lastAimForwardLocal = aimForward;
                }
                else
                {
                    ComputeLocalYawPitch(out yawDeg, out pitchDeg, out aimPoint, out aimForward);
                    _lastAimPointLocal = aimPoint;
                    _lastAimForwardLocal = aimForward;
                }

                if (aimPoint == Vector3.zero && vehicleRoot.weaponAimAtCamera != null)
                {
                    aimPoint = vehicleRoot.weaponAimAtCamera.CurrentAimPoint;
                    _lastAimPointLocal = aimPoint;
                }
                if (aimForward == Vector3.zero && CameraSync.In != null)
                {
                    aimForward = CameraSync.In.GetAimForward();
                    _lastAimForwardLocal = aimForward;
                }

                if (vehicleRoot.weaponAimAtCamera != null)
                {
                    vehicleRoot.weaponAimAtCamera.SetDesiredAimPoint(aimPoint, aimForward);
                }
                ApplyLocalAimTargets(yawDeg, pitchDeg);

                short yawQ = AngleQuantization.QuantizeAngle01(yawDeg);
                short pitchQ = AngleQuantization.QuantizeAngle01(pitchDeg);

                float lastYawDeg = AngleQuantization.DequantizeAngle01(_lastSentYawQ);
                float lastPitchDeg = AngleQuantization.DequantizeAngle01(_lastSentPitchQ);
                VehicleInputSyncSettings inputSync = GetInputSyncSettings();
                float yawPitchDeadzone = Mathf.Max(0f, inputSync.yawPitchSendDeadzoneDeg);

                bool yawBeyond = Mathf.Abs(Mathf.DeltaAngle(yawDeg, lastYawDeg)) >= yawPitchDeadzone;
                bool pitchBeyond = Mathf.Abs(Mathf.DeltaAngle(pitchDeg, lastPitchDeg)) >= yawPitchDeadzone;

                if (!yawBeyond)
                {
                    yawQ = _lastSentYawQ;
                }
                if (!pitchBeyond)
                {
                    pitchQ = _lastSentPitchQ;
                }

                bool moveChanged = (_lastSentMove - _moveLocal).sqrMagnitude > 0.0001f;
                bool shootChanged = _lastSentShoot != newShoot;
                bool actionChanged = _lastSentAction != newAction;
                bool immediateChanged = moveChanged || shootChanged || actionChanged || aimLockChanged;
                bool sendDue = Time.unscaledTime >= _nextSendTime;

                if (sendDue || immediateChanged)
                {
                    _seq++;
                    ProfileScope.RecordEvent("RPC.SendControls", DiagnosticsCategories.Rpc);
                    DiagnosticsManager.RecordOutgoing("RPC.SendControls", 96);
                    SendControlsServerRpc(
                        _seq,
                        Mathf.Clamp(_moveLocal.x, -1f, 1f),
                        Mathf.Clamp(_moveLocal.y, -1f, 1f),
                        newShoot,
                        newAction,
                        yawQ,
                        pitchQ,
                        aimPoint,
                        aimForward
                    );

                    _lastSentMove = _moveLocal;
                    _lastSentShoot = newShoot;
                    _lastSentAction = newAction;
                    _lastSentYawQ = yawQ;
                    _lastSentPitchQ = pitchQ;
                    _lastSentAimPoint = aimPoint;
                    _nextSendTime = Time.unscaledTime + Mathf.Max(0.001f, inputSync.sendInterval);
                }
            }
        }

        private void LateUpdate()
        {
        }

        [ServerRpc(RequireOwnership = true, RunLocally = false)]
        private void SendControlsServerRpc(
            int seq,
            float moveX,
            float moveY,
            bool shoot,
            bool action,
            short yawQ,
            short pitchQ,
            Vector3 aimPoint,
            Vector3 aimForward,
            NetworkConnection sender = null)
        {
            using (ProfileScope.Measure("RPC.SendControls", DiagnosticsCategories.Rpc))
            {
                if (sender == null)
                {
                    return;
                }
                if (sender != base.Owner)
                {
                    return;
                }
                if (seq <= _lastInputSeqServer)
                {
                    return;
                }

                _lastInputSeqServer = seq;

                moveX = Mathf.Clamp(moveX, -1f, 1f);
                moveY = Mathf.Clamp(moveY, -1f, 1f);

                ApplyServerInput(new Vector2(moveX, moveY), shoot, action, true);

                float yawDeg = AngleQuantization.DequantizeAngle01(yawQ);
                float pitchDeg = AngleQuantization.DequantizeAngle01(pitchQ);

                vehicleRoot.weaponAimAtCamera.SetDesiredAimPointServer(aimPoint, aimForward);
                vehicleRoot.robotHullRotation.SetTargetYawServer(yawDeg);
                vehicleRoot.weaponAimAtCamera.SetTargetPitchServer(pitchDeg);
            }
        }

        private void ApplyServerInput(Vector2 move, bool shoot, bool action, bool syncAnimation)
        {
            _moveServer = move;
            _shootServer = shoot;
            _actionServer = action;

            if (syncAnimation)
            {
                _animMove.Value = _moveServer;
                _animShoot.Value = _shootServer;
                _animAction.Value = _actionServer;
            }
        }

        private void ReadMovementInput(bool blocked, out float x, out float y)
        {
            x = 0f;
            y = 0f;

            if (blocked)
            {
                ResetCruiseControl();
                return;
            }

            if (Input.GetKey(KeyCode.A))
            {
                x = -1f;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                x = 1f;
            }

            bool manualForward = Input.GetKey(KeyCode.W);
            bool manualReverse = Input.GetKey(KeyCode.S);
            if (manualForward || manualReverse)
            {
                ResetCruiseControl();

                if (manualForward)
                {
                    y = 1f;
                }
                else
                {
                    y = -1f;
                }

                return;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                ResetCruiseControl();
                return;
            }

            ApplyCruiseControlKeyPresses();
            y = GetCruiseControlInput();
        }

        private void ApplyCruiseControlKeyPresses()
        {
            bool forwardPressed = Input.GetKeyDown(KeyCode.R);
            bool reversePressed = Input.GetKeyDown(KeyCode.F);

            if (forwardPressed && reversePressed)
            {
                ResetCruiseControl();
                return;
            }

            if (forwardPressed)
            {
                IncreaseCruiseControl(1);
            }
            else if (reversePressed)
            {
                IncreaseCruiseControl(-1);
            }
        }

        private void IncreaseCruiseControl(int direction)
        {
            _cruiseSpeedLevel = Mathf.Clamp(_cruiseSpeedLevel + direction, -MaxCruiseSpeedLevel, MaxCruiseSpeedLevel);
        }

        private float GetCruiseControlInput()
        {
            if (_cruiseSpeedLevel == 0)
            {
                return 0f;
            }

            return Mathf.Clamp(_cruiseSpeedLevel * CruiseSpeedStep, -1f, 1f);
        }

        private void ResetCruiseControl()
        {
            _cruiseSpeedLevel = 0;
        }

        private bool HandleRightMouseAimLock(bool blocked, out bool changed)
        {
            changed = false;

            if (blocked)
            {
                if (_turretAimLockActive)
                {
                    _turretAimLockActive = false;
                    changed = true;
                }

                return false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                _rightMouseDownTime = Time.unscaledTime;
                CaptureTurretAimLock();
                _turretAimLockActive = true;
                changed = true;
            }

            if (_turretAimLockActive && !Input.GetMouseButton(1))
            {
                float holdTime = Time.unscaledTime - _rightMouseDownTime;
                _turretAimLockActive = false;
                changed = true;

                if (holdTime <= RightMouseAutoAimTapSeconds && autoAimController != null)
                {
                    autoAimController.ToggleFromCurrentView();
                }

                return false;
            }

            return _turretAimLockActive && Input.GetMouseButton(1);
        }

        private void CaptureTurretAimLock()
        {
            _lockedTurretYawLocal = _turretYawLocal;
            _lockedGunPitchLocal = _gunPitchLocal;

            if (vehicleRoot != null && vehicleRoot.robotHullRotation != null)
            {
                _lockedTurretYawLocal = vehicleRoot.robotHullRotation.CurrentLocalYaw;
            }

            if (vehicleRoot != null && vehicleRoot.weaponAimAtCamera != null)
            {
                _lockedGunPitchLocal = vehicleRoot.weaponAimAtCamera.CurrentLocalPitch;
            }

            _turretYawLocal = _lockedTurretYawLocal;
            _gunPitchLocal = _lockedGunPitchLocal;
            _lockedAimPointLocal = ResolveLockedAimPoint();
            _lockedAimForwardLocal = ResolveLockedAimForward();
            _lastAimPointLocal = _lockedAimPointLocal;
            _lastAimForwardLocal = _lockedAimForwardLocal;
        }

        private Vector3 ResolveLockedAimPoint()
        {
            WeaponAimController weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim != null)
            {
                Vector3 currentAimPoint = weaponAim.CurrentAimPoint;
                if (IsFinite(currentAimPoint) && currentAimPoint != Vector3.zero)
                {
                    return currentAimPoint;
                }

                Vector3 desiredAimPoint = weaponAim.DesiredAimPoint;
                if (IsFinite(desiredAimPoint) && desiredAimPoint != Vector3.zero)
                {
                    return desiredAimPoint;
                }
            }

            if (IsFinite(_lastAimPointLocal) && _lastAimPointLocal != Vector3.zero)
            {
                return _lastAimPointLocal;
            }

            Vector3 forward = ResolveLockedAimForward();
            float distance = weaponAim != null ? Mathf.Max(0.25f, weaponAim.maxAimDistance) : 500f;
            Transform origin = weaponAim != null && weaponAim.gun != null ? weaponAim.gun : transform;
            return origin.position + forward * distance;
        }

        private Vector3 ResolveLockedAimForward()
        {
            WeaponAimController weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim != null)
            {
                Vector3 logicalForward = weaponAim.GetLogicalAimForwardWorld();
                if (IsFinite(logicalForward) && logicalForward.sqrMagnitude > 0.000001f)
                {
                    return logicalForward.normalized;
                }

                if (weaponAim.gun != null && weaponAim.gun.forward.sqrMagnitude > 0.000001f)
                {
                    return weaponAim.gun.forward.normalized;
                }
            }

            if (IsFinite(_lastAimForwardLocal) && _lastAimForwardLocal.sqrMagnitude > 0.000001f)
            {
                return _lastAimForwardLocal.normalized;
            }

            if (CameraSync.In != null)
            {
                Vector3 cameraForward = CameraSync.In.GetAimForward();
                if (IsFinite(cameraForward) && cameraForward.sqrMagnitude > 0.000001f)
                {
                    return cameraForward.normalized;
                }
            }

            if (transform.forward.sqrMagnitude > 0.000001f)
            {
                return transform.forward.normalized;
            }

            return Vector3.forward;
        }

        private void ComputeLocalYawPitch(out float yawDeg, out float pitchDeg, out Vector3 cameraAimPoint, out Vector3 cameraAimForward)
        {
            Transform cameraTransform = CameraSync.In != null ? CameraSync.In.GetAimTransform() : null;
            VehicleAimInputResult result = VehicleAimInputSolver.Solve(
                vehicleRoot,
                cameraTransform,
                _turretYawLocal,
                _gunPitchLocal);

            if (result.HasState)
            {
                _turretYawLocal = result.YawDeg;
                _gunPitchLocal = result.PitchDeg;
            }

            yawDeg = result.YawDeg;
            pitchDeg = result.PitchDeg;
            cameraAimPoint = result.CameraAimPoint;
            cameraAimForward = result.CameraAimForward;
        }

        private void ComputeAutoAimYawPitch(
            Vector3 targetAimPoint,
            Vector3 targetAimForward,
            out float yawDeg,
            out float pitchDeg,
            out Vector3 cameraAimPoint,
            out Vector3 cameraAimForward)
        {
            VehicleAimInputResult result = VehicleAimInputSolver.SolveForAimPoint(
                vehicleRoot,
                targetAimPoint,
                targetAimForward,
                _turretYawLocal,
                _gunPitchLocal);

            if (result.HasState)
            {
                _turretYawLocal = result.YawDeg;
                _gunPitchLocal = result.PitchDeg;
                yawDeg = result.YawDeg;
                pitchDeg = result.PitchDeg;
                cameraAimPoint = result.CameraAimPoint;
                cameraAimForward = result.CameraAimForward;
                return;
            }

            ComputeLocalYawPitch(out yawDeg, out pitchDeg, out cameraAimPoint, out cameraAimForward);
        }

        private void ResolveAutoAimController()
        {
            if (autoAimController == null && vehicleRoot != null)
            {
                autoAimController = vehicleRoot.autoAimController;
            }
        }

        private VehicleInputSyncSettings GetInputSyncSettings()
        {
            if (IsServerInitialized)
            {
                return ServerSettings.GetVehicleInputSync();
            }

            if (RemoteServerSettings.IsLoaded)
            {
                return RemoteServerSettings.VehicleInputSync;
            }

            return VehicleInputSyncSettings.Default;
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

        private void ApplyLocalAimTargets(float yawDeg, float pitchDeg)
        {
            if (vehicleRoot == null)
            {
                return;
            }

            if (vehicleRoot.robotHullRotation != null)
            {
                vehicleRoot.robotHullRotation.SetTargetYaw(yawDeg);
            }
            if (vehicleRoot.weaponAimAtCamera != null)
            {
                vehicleRoot.weaponAimAtCamera.SetTargetPitch(pitchDeg);
            }
        }

    }
}
