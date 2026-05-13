using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class InputManager : NetworkBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;

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

        private const float SendInterval = 0.05f;
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

        private const float YawPitchSendDeadzoneDeg = 0.03f;
        private const float AimPointSendDeadzoneSqr = 0.02f * 0.02f;

        private bool _controlsBlocked;

        public static bool Escape => UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void SetControlsBlocked(bool blocked)
        {
            _controlsBlocked = blocked;
        }

        public bool IsControlsBlocked => _controlsBlocked;
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
                    return _moveLocal;
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
                    return _shootLocal;
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
                    return _actionLocal;
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
                    return _moveLocal;
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
                    return _shootLocal;
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
                    return _actionLocal;
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
            if (!_controlsBlocked)
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

            bool newShoot = !_controlsBlocked && Input.GetMouseButton(0);
            bool newAction = !_controlsBlocked && Input.GetKey(KeyCode.Space);

            _moveLocal = new Vector2(x, y);
            _shootLocal = newShoot;
            _actionLocal = newAction;

            float yawDeg, pitchDeg;
            Vector3 aimPoint;
            Vector3 aimForward;

            if (_controlsBlocked)
            {
                yawDeg = AngleQuantization.DequantizeAngle01(_lastSentYawQ);
                pitchDeg = AngleQuantization.DequantizeAngle01(_lastSentPitchQ);
                aimPoint = _lastAimPointLocal;
                aimForward = _lastAimForwardLocal;
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
                aimForward = CameraSync.In.transform.forward;
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

            bool yawBeyond = Mathf.Abs(Mathf.DeltaAngle(yawDeg, lastYawDeg)) >= YawPitchSendDeadzoneDeg;
            bool pitchBeyond = Mathf.Abs(Mathf.DeltaAngle(pitchDeg, lastPitchDeg)) >= YawPitchSendDeadzoneDeg;

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
                (_lastSentAimPoint - aimPoint).sqrMagnitude > AimPointSendDeadzoneSqr;

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
                _nextSendTime = Time.unscaledTime + SendInterval;
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

            _moveServer = new Vector2(moveX, moveY);
            _shootServer = shoot;
            _actionServer = action;

            _animMove.Value = _moveServer;
            _animShoot.Value = _shootServer;
            _animAction.Value = _actionServer;

            float yawDeg = AngleQuantization.DequantizeAngle01(yawQ);
            float pitchDeg = AngleQuantization.DequantizeAngle01(pitchQ);

            vehicleRoot.weaponAimAtCamera.SetDesiredAimPointServer(aimPoint, aimForward);
            vehicleRoot.robotHullRotation.SetTargetYawServer(yawDeg);
            vehicleRoot.weaponAimAtCamera.SetTargetPitchServer(pitchDeg);
        }

        private void ComputeLocalYawPitch(out float yawDeg, out float pitchDeg, out Vector3 cameraAimPoint, out Vector3 cameraAimForward)
        {
            Transform cameraTransform = CameraSync.In != null ? CameraSync.In.transform : null;
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
