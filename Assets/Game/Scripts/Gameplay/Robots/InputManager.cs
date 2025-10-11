using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class InputManager : NetworkBehaviour
    {
        public TankRoot tankRoot;

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

        private float _turretYawLocal;
        private float _gunPitchLocal;

        private const float YawPitchSendDeadzoneDeg = 0.3f;

        // 🔒 прапор блокування керування (меню, катсцена тощо)
        private bool _controlsBlocked;

        public static bool Escape => UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        /// <summary>
        /// Увімк/вимк блокування керування роботом (рух, стрільба, дії).
        /// Escape не блокується (UI).
        /// </summary>
        public void SetControlsBlocked(bool blocked)
        {
            _controlsBlocked = blocked;
        }

        public bool IsControlsBlocked => _controlsBlocked;

        public Vector2 Move
        {
            get
            {
                if (IsServer)
                {
                    return _moveServer;
                }
                if (IsOwner)
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
                if (IsServer)
                {
                    return _shootServer;
                }
                if (IsOwner)
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
                if (IsServer)
                {
                    return _actionServer;
                }
                if (IsOwner)
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
                if (IsServer) return _moveServer;
                if (IsOwner) return _moveLocal;
                return _animMove.Value;
            }
        }

        public bool AnimShoot
        {
            get
            {
                if (IsServer) return _shootServer;
                if (IsOwner) return _shootLocal;
                return _animShoot.Value;
            }
        }

        public bool AnimAction
        {
            get
            {
                if (IsServer) return _actionServer;
                if (IsOwner) return _actionLocal;
                return _animAction.Value;
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            // --- Зчитування локального інпуту ---
            float x = 0f;
            float y = 0f;
            if (!_controlsBlocked) // рух блокується, Escape НЕ блокуємо (використовуйте InputManager.Escape у вашому UI)
            {
                if (Input.GetKey(KeyCode.A)) x = -1f;
                else if (Input.GetKey(KeyCode.D)) x = 1f;
                if (Input.GetKey(KeyCode.W)) y = 1f;
                else if (Input.GetKey(KeyCode.S)) y = -1f;
            }

            bool newShoot = !_controlsBlocked && Input.GetMouseButton(0);
            bool newAction = !_controlsBlocked && Input.GetKey(KeyCode.Space);

            _moveLocal = new Vector2(x, y);
            _shootLocal = newShoot;
            _actionLocal = newAction;

            // --- Обчислення yaw/pitch ---
            float yawDeg, pitchDeg;

            if (_controlsBlocked)
            {
                // Під час блокування не оновлюємо цільові кути: фіксуємо попередні
                yawDeg = DequantizeAngle01(_lastSentYawQ);
                pitchDeg = DequantizeAngle01(_lastSentPitchQ);
            }
            else
            {
                ComputeLocalYawPitch(out yawDeg, out pitchDeg);
            }

            short yawQ = QuantizeAngle01(yawDeg);
            short pitchQ = QuantizeAngle01(pitchDeg);

            float lastYawDeg = DequantizeAngle01(_lastSentYawQ);
            float lastPitchDeg = DequantizeAngle01(_lastSentPitchQ);

            bool yawBeyond = Mathf.Abs(Mathf.DeltaAngle(yawDeg, lastYawDeg)) >= YawPitchSendDeadzoneDeg;
            bool pitchBeyond = Mathf.Abs(Mathf.DeltaAngle(pitchDeg, lastPitchDeg)) >= YawPitchSendDeadzoneDeg;

            if (!yawBeyond) yawQ = _lastSentYawQ;
            if (!pitchBeyond) pitchQ = _lastSentPitchQ;

            bool changed =
                (_lastSentMove - _moveLocal).sqrMagnitude > 0.0001f ||
                _lastSentShoot != newShoot ||
                _lastSentAction != newAction ||
                _lastSentYawQ != yawQ ||
                _lastSentPitchQ != pitchQ;

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
                    pitchQ
                );

                _lastSentMove = _moveLocal;
                _lastSentShoot = newShoot;
                _lastSentAction = newAction;
                _lastSentYawQ = yawQ;
                _lastSentPitchQ = pitchQ;
                _nextSendTime = Time.unscaledTime + SendInterval;
            }
        }

        private void LateUpdate()
        {
            // клієнтський prediction обертання відключено; все крутить сервер
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

            float yawDeg = DequantizeAngle01(yawQ);
            float pitchDeg = DequantizeAngle01(pitchQ);

            tankRoot.robotHullRotation.SetTargetYawServer(yawDeg);
            tankRoot.weaponAimAtCamera.SetTargetPitchServer(pitchDeg);
        }

        private void ComputeLocalYawPitch(out float yawDeg, out float pitchDeg)
        {
            yawDeg = 0f;
            pitchDeg = 0f;

            Transform chassis = tankRoot.objectMover.transform;
            Vector3 chassisFwd = chassis.forward; chassisFwd.y = 0f;

            Transform camTr = CameraSync.In != null ? CameraSync.In.transform : null;
            if (camTr == null)
            {
                _turretYawLocal = 0f;
                _gunPitchLocal = 0f;
                return;
            }

            Vector3 camFwdFlat = camTr.forward; camFwdFlat.y = 0f;

            if (chassisFwd.sqrMagnitude > 1e-6f && camFwdFlat.sqrMagnitude > 1e-6f)
            {
                chassisFwd.Normalize();
                camFwdFlat.Normalize();
                _turretYawLocal = Vector3.SignedAngle(chassisFwd, camFwdFlat, Vector3.up);

                float maxLocalYaw = tankRoot.robotHullRotation.maxLocalYaw;
                if (maxLocalYaw > 0f)
                {
                    float half = Mathf.Abs(maxLocalYaw);
                    _turretYawLocal = Mathf.Clamp(_turretYawLocal, -half, half);
                }
            }

            Transform gun = tankRoot.weaponAimAtCamera.gun;
            WeaponAimAtCamera.Axis pitchAxis = tankRoot.weaponAimAtCamera.localPitchAxis;
            WeaponAimAtCamera.Axis fwdAxis = tankRoot.weaponAimAtCamera.localForwardAxis;

            Ray camRay = new Ray(camTr.position, camTr.forward);
            float maxDist = tankRoot.weaponAimAtCamera.maxAimDistance;
            Vector3 cameraAimPoint = camTr.position + camTr.forward * maxDist;
            if (Physics.Raycast(camRay, out RaycastHit camHit, maxDist, tankRoot.weaponAimAtCamera.aimMask, QueryTriggerInteraction.Ignore))
            {
                cameraAimPoint = camHit.point;
            }

            Vector3 rightPitchWorld = ToWorldAxis(gun, pitchAxis);
            Vector3 fwdWorld = ToWorldAxis(gun, fwdAxis);
            Vector3 dirWorld = (cameraAimPoint - gun.position).normalized;

            Vector3 fwdProj = Vector3.ProjectOnPlane(fwdWorld, rightPitchWorld).normalized;
            Vector3 dirProj = Vector3.ProjectOnPlane(dirWorld, rightPitchWorld).normalized;

            float deltaPitch = Vector3.SignedAngle(fwdProj, dirProj, rightPitchWorld);
            Vector3 localPitchAxisVec = AxisToVector(pitchAxis);
            float currentPitch = ExtractSignedAngleAroundLocalAxis(gun.localRotation, tankRoot.weaponAimAtCamera.InitialLocalRotation, localPitchAxisVec);

            _gunPitchLocal = Mathf.Clamp(
                currentPitch + deltaPitch + tankRoot.weaponAimAtCamera.pitchOffset,
                tankRoot.weaponAimAtCamera.minPitch,
                tankRoot.weaponAimAtCamera.maxPitch
            );

            yawDeg = _turretYawLocal;
            pitchDeg = _gunPitchLocal;
        }

        private static short QuantizeAngle01(float deg)
        {
            float clamped = Mathf.Clamp(deg, -180f, 180f);
            return (short)Mathf.RoundToInt(clamped * 10f);
        }

        private static float DequantizeAngle01(short q)
        {
            return q / 10f;
        }

        private static Vector3 AxisToVector(WeaponAimAtCamera.Axis a)
        {
            if (a == WeaponAimAtCamera.Axis.X) return Vector3.right;
            if (a == WeaponAimAtCamera.Axis.Y) return Vector3.up;
            return Vector3.forward;
        }

        private static Vector3 ToWorldAxis(Transform t, WeaponAimAtCamera.Axis a)
        {
            if (a == WeaponAimAtCamera.Axis.X) return t.right;
            if (a == WeaponAimAtCamera.Axis.Y) return t.up;
            return t.forward;
        }

        private static float ExtractSignedAngleAroundLocalAxis(Quaternion currentLocal, Quaternion baseLocal, Vector3 localAxis)
        {
            Quaternion delta = Quaternion.Inverse(baseLocal) * currentLocal;
            Vector3 ortho = Mathf.Abs(localAxis.x) < 0.5f ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 1f, 0f);
            Vector3 a = Vector3.ProjectOnPlane(ortho, localAxis).normalized;
            Vector3 b = Vector3.ProjectOnPlane(delta * a, localAxis).normalized;
            if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) return 0f;
            return Vector3.SignedAngle(a, b, localAxis.normalized);
        }
    }
}
