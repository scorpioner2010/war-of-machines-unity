using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.AI.WaypointGraph;
using Game.Scripts.MenuController;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleInputController : NetworkBehaviour, IVehicleRootAware, IBotInputReceiver
    {
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
            ServerSetExternalInput(VehicleServerInput.Movement(new Vector2(turn, forward)), true);
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

            float x = 0f;
            float y = 0f;
            bool blocked = IsLocalInputBlocked;
            if (!blocked)
            {
                if (Input.GetKey(KeyCode.A))
                {
                    x = -1f;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    x = 1f;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    y = 1f;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    y = -1f;
                }
            }

            bool newShoot = !blocked && Input.GetMouseButton(0);
            bool newAction = !blocked && Input.GetKey(KeyCode.Space);
            ResolveAutoAimController();
            if (!blocked && Input.GetMouseButtonDown(1) && autoAimController != null)
            {
                autoAimController.ToggleFromCurrentView();
            }

            bool autoAimActive = false;
            Vector3 autoAimPoint = Vector3.zero;
            Vector3 autoAimForward = Vector3.zero;
            if (!blocked && autoAimController != null)
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
            float aimPointDeadzoneSqr = inputSync.GetAimPointSendDeadzoneSqr();

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

            bool changed =
                (_lastSentMove - _moveLocal).sqrMagnitude > 0.0001f ||
                _lastSentShoot != newShoot ||
                _lastSentAction != newAction ||
                _lastSentYawQ != yawQ ||
                _lastSentPitchQ != pitchQ ||
                (_lastSentAimPoint - aimPoint).sqrMagnitude > aimPointDeadzoneSqr;

            if (Time.unscaledTime >= _nextSendTime || changed)
            {
                _seq++;
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
            if (autoAimController != null)
            {
                return;
            }

            if (vehicleRoot == null)
            {
                return;
            }

            autoAimController = vehicleRoot.autoAimController;
            if (autoAimController == null)
            {
                autoAimController = vehicleRoot.GetComponentInChildren<VehicleAutoAimController>(true);
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
