using Game.Scripts.Server;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class RobotMovementMotor : MonoBehaviour
    {
        [Header("Body")]
        public RobotMovementBody movementBody;

        [Header("Speed")]
        [FormerlySerializedAs("maxSpeed")]
        public float maxForwardSpeed = 10f;
        public float maxReverseSpeed = 5f;
        public float turnSpeed = 120f;
        public float rotateSpeed = 2f;
        public float acceleration = 30f;
        public float brakeDeceleration = 90f;
        public float naturalDeceleration = 20f;
        public float turnAcceleration = 360f;
        [Range(0f, 0.25f)] public float inputDeadZone = 0.02f;

        [Header("Grounding")]
        public float gravity = 25f;
        public float maxFallSpeed = 50f;
        public float groundCheckDistance = 0.2f;
        public float groundSnapDistance = 0.65f;
        public float groundHeightLerpSpeed = 17.5f;
        public bool useAlignedGroundProbeDirection = true;
        [Range(0f, 1f)] public float groundProbeAlignmentBlend = 1f;
        public float smallPitBridgeMaxDrop = 0.35f;
        public float maxSlopeAngle = 45f;
        public bool debugGrounding;

        [Header("Optimization")]
        public bool useDirectionalEdgeGroundProbe = true;
        public bool useEdgeProbeOnlyWhenMoving = true;
        public float minEdgeProbeMotion = 0.06f;
        public float minCollisionCastDistance = 0.03f;

        [Header("Steep Slope Sliding")]
        public bool slideOnSteepSlopes = true;
        public float steepSlopeSlideAcceleration = 12f;
        public float steepSlopeMaxSlideSpeed = 7f;
        public float steepSlopeSlideDamping = 5f;
        public float steepSlopeUphillControl = 0.15f;

        [Header("Slope Alignment")]
        public Transform slopeAlignmentRoot;
        public bool alignToGround = true;
        public float slopeAlignmentStrength = 1f;
        public float slopeAlignmentSpeed = 6f;
        public float maxSlopeAlignmentAngle = 55f;

        [Header("Collision")]
        public bool allowCollisionSlide = true;

        [System.NonSerialized] public float currentForwardSpeed;
        [System.NonSerialized] public float currentTurnSpeed;
        [System.NonSerialized] public float verticalVelocity;
        [System.NonSerialized] public Vector3 velocity;
        [System.NonSerialized] public float movementInput;
        [System.NonSerialized] public float turnInput;
        [System.NonSerialized] public bool isGrounded;
        [System.NonSerialized] public bool isSlidingOnSteepSlope;
        [System.NonSerialized] public Vector3 groundPoint;
        [System.NonSerialized] public Vector3 groundNormal = Vector3.up;
        [System.NonSerialized] public float slopeAngle;

        private Transform _cachedTransform;
        private bool _useRuntimeTraverseSpeed;
        private float _runtimeTraverseSpeedDegPerSecond;
        private bool _hasMotorYaw;
        private float _motorYaw;
        private Vector3 _yawForward = Vector3.forward;
        private Vector3 _slopeSlideVelocity;
        private Vector3 _probeUp = Vector3.up;

        private float _maxForwardSpeed;
        private float _maxReverseSpeed;
        private float _acceleration;
        private float _brakeDeceleration;
        private float _naturalDeceleration;
        private float _turnSpeed;
        private float _turnAcceleration;
        private float _gravity;
        private float _maxFallSpeed;
        private float _groundCheckDistance;
        private float _groundSnapDistance;
        private float _groundProbeDistance;
        private float _groundHeightLerpSpeed;
        private float _smallPitBridgeMaxDrop;
        private float _maxSlopeAngle;
        private float _maxSlopeDot;
        private bool _slideOnSteepSlopes;
        private float _steepSlopeSlideAcceleration;
        private float _steepSlopeMaxSlideSpeed;
        private float _steepSlopeSlideDamping;
        private float _steepSlopeUphillControl;
        private float _slopeAlignmentStrength;
        private float _slopeAlignmentSpeed;
        private float _maxSlopeAlignmentAngle;
        private bool _wallSlideEnabled;

        public float MotorYaw => _hasMotorYaw ? _motorYaw : transform.eulerAngles.y;

        private void Awake()
        {
            EnsureReady();
        }

        private void OnEnable()
        {
            EnsureReady();
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                return;
            }

            if (stats.Speed > 0f)
            {
                maxForwardSpeed = stats.Speed;
            }

            if (stats.Acceleration > 0f)
            {
                acceleration = stats.Acceleration;
            }

            if (stats.TraverseSpeed > 0f)
            {
                _runtimeTraverseSpeedDegPerSecond = stats.TraverseSpeed;
                _useRuntimeTraverseSpeed = true;
            }
        }

        public void Tick(Vector2 input, RobotMovementGlobalSettings settings, float dt, bool isLegged)
        {
            if (dt <= 0f)
            {
                return;
            }

            EnsureReady();
            settings ??= RobotMovementGlobalSettings.Default;
            RefreshRuntimeValues(settings, isLegged, dt);

            movementInput = ApplyInputDeadZone(input.y);
            turnInput = ApplyInputDeadZone(input.x);

            Vector3 startPosition = _cachedTransform.position;
            bool wasGrounded = isGrounded;
            GroundProbe startGround = ProbeGround(startPosition, _groundProbeDistance, Vector3.zero);

            ApplyGroundState(startGround, startPosition, wasGrounded);
            UpdateTurn(dt);
            UpdateForwardSpeed(dt);

            Vector3 horizontalMotion = BuildHorizontalMotion(startGround, dt);
            horizontalMotion += UpdateSteepSlopeSlide(startGround, startPosition, wasGrounded, dt);
            horizontalMotion = ResolveCollision(startPosition, horizontalMotion);

            float verticalMotion = UpdateVerticalVelocity(startGround, startPosition, wasGrounded, dt);
            Vector3 nextPosition = startPosition + horizontalMotion + Vector3.up * verticalMotion;

            GroundProbe finalGround = ShouldProbeFinalGround(horizontalMotion, verticalMotion)
                ? ProbeGround(nextPosition, _groundProbeDistance + Mathf.Abs(verticalMotion), horizontalMotion)
                : startGround;

            if (TryResolveFinalGround(finalGround, wasGrounded, dt, ref nextPosition))
            {
                verticalVelocity = 0f;
            }

            _cachedTransform.position = nextPosition;
            ApplyMotorRotation((isGrounded || isSlidingOnSteepSlope) ? groundNormal : Vector3.up, dt);
            velocity = (nextPosition - startPosition) / dt;

            if (debugGrounding)
            {
                DrawGroundDebug(nextPosition, finalGround);
            }
        }

        private void EnsureReady()
        {
            if (_cachedTransform == null)
            {
                _cachedTransform = transform;
            }

            if (movementBody == null)
            {
                movementBody = GetComponent<RobotMovementBody>();
                if (movementBody == null)
                {
                    movementBody = gameObject.AddComponent<RobotMovementBody>();
                }
            }

            EnsureMotorYawInitialized();
        }

        private void RefreshRuntimeValues(RobotMovementGlobalSettings settings, bool isLegged, float dt)
        {
            _maxForwardSpeed = maxForwardSpeed > 0f
                ? maxForwardSpeed
                : Mathf.Max(0f, settings.fallbackMaxSpeed);
            _maxReverseSpeed = maxReverseSpeed > 0f
                ? maxReverseSpeed
                : _maxForwardSpeed * 0.5f;

            float baseAcceleration = acceleration > 0f
                ? acceleration
                : Mathf.Max(0.01f, settings.fallbackAcceleration);
            float accelerationMultiplier = settings.GetAccelerationMultiplier(isLegged);
            float brakingMultiplier = settings.GetBrakingMultiplier(isLegged);
            _acceleration = Mathf.Max(0.01f, baseAcceleration * accelerationMultiplier);

            float brakeBase = brakeDeceleration > 0f
                ? brakeDeceleration
                : baseAcceleration * Mathf.Max(1f, settings.stoppingAccelerationMultiplier);
            _brakeDeceleration = Mathf.Max(0.01f, brakeBase * brakingMultiplier);
            _naturalDeceleration = naturalDeceleration > 0f
                ? Mathf.Max(naturalDeceleration, baseAcceleration)
                : _acceleration;

            _turnSpeed = ResolveTurnSpeed(settings, dt);
            _turnAcceleration = turnAcceleration > 0f
                ? turnAcceleration
                : Mathf.Max(1f, _turnSpeed * 5f);

            _gravity = settings.gravity > 0f
                ? settings.gravity
                : Mathf.Max(0.01f, gravity);
            _maxFallSpeed = settings.maxFallSpeed > 0f
                ? settings.maxFallSpeed
                : Mathf.Max(0.01f, maxFallSpeed);
            _groundCheckDistance = settings.groundCheckDistance > 0f
                ? settings.groundCheckDistance
                : Mathf.Max(0.01f, groundCheckDistance);
            _groundSnapDistance = settings.groundSnapDistance > 0f
                ? settings.groundSnapDistance
                : Mathf.Max(0.01f, groundSnapDistance);
            _groundProbeDistance = Mathf.Max(_groundCheckDistance, _groundSnapDistance);
            _groundHeightLerpSpeed = settings.groundHeightLerpSpeed >= 0f
                ? settings.groundHeightLerpSpeed
                : Mathf.Max(0f, groundHeightLerpSpeed);
            _smallPitBridgeMaxDrop = settings.smallPitBridgeMaxDrop >= 0f
                ? settings.smallPitBridgeMaxDrop
                : Mathf.Max(0f, smallPitBridgeMaxDrop);

            _maxSlopeAngle = settings.maxSlopeAngle > 0f
                ? Mathf.Clamp(settings.maxSlopeAngle, 0.01f, 89f)
                : Mathf.Clamp(maxSlopeAngle, 0.01f, 89f);
            _maxSlopeDot = Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad);

            _slideOnSteepSlopes = settings.slideOnSteepSlopes && slideOnSteepSlopes;
            _steepSlopeSlideAcceleration = settings.steepSlopeSlideAcceleration >= 0f
                ? settings.steepSlopeSlideAcceleration
                : Mathf.Max(0f, steepSlopeSlideAcceleration);
            _steepSlopeMaxSlideSpeed = settings.steepSlopeMaxSlideSpeed >= 0f
                ? settings.steepSlopeMaxSlideSpeed
                : Mathf.Max(0f, steepSlopeMaxSlideSpeed);
            _steepSlopeSlideDamping = settings.steepSlopeSlideDamping >= 0f
                ? settings.steepSlopeSlideDamping
                : Mathf.Max(0f, steepSlopeSlideDamping);
            _steepSlopeUphillControl = settings.steepSlopeUphillControl >= 0f
                ? Mathf.Clamp01(settings.steepSlopeUphillControl)
                : Mathf.Clamp01(steepSlopeUphillControl);

            _slopeAlignmentStrength = settings.slopeAlignmentStrength >= 0f
                ? settings.slopeAlignmentStrength
                : Mathf.Max(0f, slopeAlignmentStrength);
            _slopeAlignmentSpeed = settings.slopeAlignmentSpeed >= 0f
                ? settings.slopeAlignmentSpeed
                : Mathf.Max(0f, slopeAlignmentSpeed);
            _maxSlopeAlignmentAngle = settings.maxSlopeAlignmentAngle >= 0f
                ? Mathf.Clamp(settings.maxSlopeAlignmentAngle, 0f, 85f)
                : Mathf.Clamp(maxSlopeAlignmentAngle, 0f, 85f);
            _wallSlideEnabled = settings.wallSlideEnabled;
            _probeUp = BuildProbeUp();
        }

        private float ResolveTurnSpeed(RobotMovementGlobalSettings settings, float dt)
        {
            if (_useRuntimeTraverseSpeed)
            {
                return Mathf.Max(0f, _runtimeTraverseSpeedDegPerSecond);
            }

            if (turnSpeed > 0f)
            {
                return turnSpeed;
            }

            if (settings.fallbackTraverseSpeedDegPerSecond > 0f)
            {
                return settings.fallbackTraverseSpeedDegPerSecond;
            }

            return rotateSpeed / Mathf.Max(dt, 0.0001f);
        }

        private float ApplyInputDeadZone(float value)
        {
            float clamped = Mathf.Clamp(value, -1f, 1f);
            float deadZone = Mathf.Clamp(inputDeadZone, 0f, 0.25f);
            if (Mathf.Abs(clamped) <= deadZone)
            {
                return 0f;
            }

            return clamped;
        }

        private void UpdateTurn(float dt)
        {
            float targetTurnSpeed = turnInput * _turnSpeed;
            float turnRate = _turnAcceleration;

            if (Mathf.Abs(turnInput) <= 0.001f)
            {
                turnRate *= 1.35f;
            }
            else if (currentTurnSpeed * targetTurnSpeed < -0.01f)
            {
                turnRate *= 1.8f;
            }

            currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurnSpeed, turnRate * dt);
            if (Mathf.Abs(currentTurnSpeed) <= 0.001f)
            {
                currentTurnSpeed = 0f;
                return;
            }

            _motorYaw = Mathf.Repeat(_motorYaw + currentTurnSpeed * dt, 360f);
            UpdateYawForward();
        }

        private void UpdateForwardSpeed(float dt)
        {
            float targetSpeed = GetTargetForwardSpeed();
            float changeRate;
            if (Mathf.Abs(movementInput) <= 0.001f)
            {
                changeRate = _naturalDeceleration;
            }
            else if (IsBraking(targetSpeed))
            {
                changeRate = _brakeDeceleration;
            }
            else
            {
                changeRate = _acceleration;
            }

            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, changeRate * dt);
            currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, -_maxReverseSpeed, _maxForwardSpeed);
            if (Mathf.Abs(currentForwardSpeed) <= 0.001f)
            {
                currentForwardSpeed = 0f;
            }
        }

        private float GetTargetForwardSpeed()
        {
            if (movementInput > 0f)
            {
                return movementInput * _maxForwardSpeed;
            }

            if (movementInput < 0f)
            {
                return movementInput * _maxReverseSpeed;
            }

            return 0f;
        }

        private Vector3 BuildHorizontalMotion(GroundProbe ground, float dt)
        {
            if (Mathf.Abs(currentForwardSpeed) <= 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 direction = _yawForward;
            if (ground.Hit && !IsSteep(ground))
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(direction, ground.Normal);
                slopeDirection.y = 0f;
                if (slopeDirection.sqrMagnitude > 0.000001f)
                {
                    direction = slopeDirection.normalized;
                }
            }

            Vector3 motion = direction * (currentForwardSpeed * dt);
            motion.y = 0f;
            if (ground.Hit && IsSteep(ground))
            {
                motion = RemoveUphillMotion(motion, ground.Normal);
            }

            return motion;
        }

        private Vector3 UpdateSteepSlopeSlide(GroundProbe ground, Vector3 position, bool wasGrounded, float dt)
        {
            if (!_slideOnSteepSlopes || !ground.Hit || !IsSteep(ground) || !CanUseGround(ground, position, wasGrounded))
            {
                DecaySlopeSlideVelocity(dt);
                return Vector3.zero;
            }

            Vector3 downhill = GetSlopeDownhill(ground.Normal);
            if (downhill.sqrMagnitude <= 0.000001f)
            {
                DecaySlopeSlideVelocity(dt);
                return Vector3.zero;
            }

            float slope01 = Mathf.InverseLerp(_maxSlopeAngle, 89f, ground.SlopeAngle);
            _slopeSlideVelocity += downhill * (_steepSlopeSlideAcceleration * Mathf.Clamp01(slope01) * dt);

            float maxSlideSpeedSqr = _steepSlopeMaxSlideSpeed * _steepSlopeMaxSlideSpeed;
            if (_steepSlopeMaxSlideSpeed > 0f && _slopeSlideVelocity.sqrMagnitude > maxSlideSpeedSqr)
            {
                _slopeSlideVelocity = _slopeSlideVelocity.normalized * _steepSlopeMaxSlideSpeed;
            }

            return _slopeSlideVelocity * dt;
        }

        private void DecaySlopeSlideVelocity(float dt)
        {
            if (_slopeSlideVelocity.sqrMagnitude <= 0.000001f)
            {
                _slopeSlideVelocity = Vector3.zero;
                return;
            }

            if (_steepSlopeSlideDamping <= 0f)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-_steepSlopeSlideDamping * dt);
            _slopeSlideVelocity = Vector3.Lerp(_slopeSlideVelocity, Vector3.zero, t);
            if (_slopeSlideVelocity.sqrMagnitude <= 0.000001f)
            {
                _slopeSlideVelocity = Vector3.zero;
            }
        }

        private float UpdateVerticalVelocity(GroundProbe ground, Vector3 position, bool wasGrounded, float dt)
        {
            if (ground.Hit && CanUseGround(ground, position, wasGrounded) && (!IsSteep(ground) || _slideOnSteepSlopes))
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = 0f;
                }

                return 0f;
            }

            verticalVelocity -= _gravity * dt;
            if (verticalVelocity < -_maxFallSpeed)
            {
                verticalVelocity = -_maxFallSpeed;
            }

            return verticalVelocity * dt;
        }

        private GroundProbe ProbeGround(Vector3 position, float distance, Vector3 motion)
        {
            bool hasCenter = TryProbeGroundSingle(position, position, distance, 0f, out GroundProbe probe);
            if (!hasCenter)
            {
                probe = CreateEmptyGroundProbe(position);
            }

            if (!ShouldUseEdgeProbe(motion))
            {
                return probe;
            }

            Vector3 edgeDirection = GetEdgeProbeDirection(motion);
            float edgeOffset = GetEdgeProbeOffset(edgeDirection);
            if (edgeOffset <= 0.001f)
            {
                return probe;
            }

            if (!TryProbeGroundSingle(position, position + edgeDirection * edgeOffset, distance, edgeOffset * edgeOffset, out GroundProbe edgeProbe))
            {
                return probe;
            }

            if (!hasCenter || ShouldPreferEdgeProbe(probe, edgeProbe))
            {
                return edgeProbe;
            }

            if (!IsSteep(probe) && !IsSteep(edgeProbe))
            {
                Vector3 alignmentNormal = probe.Normal + edgeProbe.Normal;
                if (alignmentNormal.sqrMagnitude > 0.000001f)
                {
                    alignmentNormal.Normalize();
                    probe.AlignmentNormal = alignmentNormal;
                }
            }

            return probe;
        }

        private bool ShouldUseEdgeProbe(Vector3 motion)
        {
            if (!useDirectionalEdgeGroundProbe || movementBody == null || movementBody.groundMask.value == 0)
            {
                return false;
            }

            if (!useEdgeProbeOnlyWhenMoving)
            {
                return true;
            }

            float threshold = Mathf.Max(0.05f, minEdgeProbeMotion);
            return motion.sqrMagnitude >= threshold * threshold;
        }

        private bool TryProbeGroundSingle(Vector3 centerPosition, Vector3 samplePosition, float distance, float sampleOffsetSqr, out GroundProbe probe)
        {
            probe = CreateEmptyGroundProbe(centerPosition);
            if (movementBody == null || movementBody.groundMask.value == 0)
            {
                return false;
            }

            float radius = movementBody.GroundProbeRadius;
            Vector3 origin = samplePosition + _probeUp * radius;
            float castDistance = Mathf.Max(0.01f, movementBody.BodyHeightOffset + radius + distance);
            if (!Physics.SphereCast(
                    origin,
                    radius,
                    -_probeUp,
                    out RaycastHit hit,
                    castDistance,
                    movementBody.groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 normal = hit.normal.sqrMagnitude > 0.000001f ? hit.normal.normalized : Vector3.up;
            float groundY = GetGroundYAtPoint(centerPosition, hit.point, normal);
            Vector3 groundPosition = new Vector3(centerPosition.x, groundY, centerPosition.z);
            float desiredY = groundY + movementBody.BodyHeightOffset;

            probe = new GroundProbe
            {
                Hit = true,
                Point = groundPosition,
                Normal = normal,
                AlignmentNormal = normal,
                DesiredY = desiredY,
                DistanceToDesiredHeight = centerPosition.y - desiredY,
                SampleOffsetSqr = sampleOffsetSqr,
                SlopeAngle = Vector3.Angle(normal, Vector3.up)
            };
            return true;
        }

        private bool ShouldPreferEdgeProbe(GroundProbe center, GroundProbe edge)
        {
            bool centerSteep = IsSteep(center);
            bool edgeSteep = IsSteep(edge);
            if (centerSteep && !edgeSteep)
            {
                return true;
            }

            if (!centerSteep && edgeSteep)
            {
                return false;
            }

            return edge.DesiredY > center.DesiredY + 0.025f;
        }

        private Vector3 GetEdgeProbeDirection(Vector3 motion)
        {
            Vector3 flatMotion = new Vector3(motion.x, 0f, motion.z);
            if (flatMotion.sqrMagnitude > 0.000001f)
            {
                return flatMotion.normalized;
            }

            return _yawForward;
        }

        private float GetEdgeProbeOffset(Vector3 edgeDirection)
        {
            float forwardDot = Mathf.Abs(Vector3.Dot(edgeDirection, _yawForward));
            if (forwardDot >= 0.707f)
            {
                return movementBody.GroundProbeForwardOffset;
            }

            return movementBody.GroundProbeSideOffset;
        }

        private static float GetGroundYAtPoint(Vector3 worldPoint, Vector3 hitPoint, Vector3 normal)
        {
            if (Mathf.Abs(normal.y) <= 0.0001f)
            {
                return hitPoint.y;
            }

            float dx = worldPoint.x - hitPoint.x;
            float dz = worldPoint.z - hitPoint.z;
            return hitPoint.y - (normal.x * dx + normal.z * dz) / normal.y;
        }

        private bool ShouldProbeFinalGround(Vector3 horizontalMotion, float verticalMotion)
        {
            return horizontalMotion.sqrMagnitude > 0.0000005f || Mathf.Abs(verticalMotion) > 0.0001f || !isGrounded;
        }

        private void ApplyGroundState(GroundProbe ground, Vector3 position, bool wasGrounded)
        {
            if (!ground.Hit)
            {
                SetAirborneGroundState(position);
                return;
            }

            groundPoint = ground.Point;
            groundNormal = GetProbeAlignmentNormal(ground);
            slopeAngle = ground.SlopeAngle;

            bool canUseGround = CanUseGround(ground, position, wasGrounded);
            bool steep = IsSteep(ground);
            isGrounded = canUseGround && !steep;
            isSlidingOnSteepSlope = canUseGround && steep && _slideOnSteepSlopes;
        }

        private bool TryResolveFinalGround(GroundProbe finalGround, bool wasGrounded, float dt, ref Vector3 position)
        {
            if (!finalGround.Hit)
            {
                SetAirborneGroundState(position);
                return false;
            }

            groundPoint = finalGround.Point;
            groundNormal = GetProbeAlignmentNormal(finalGround);
            slopeAngle = finalGround.SlopeAngle;

            if (!CanUseGround(finalGround, position, wasGrounded))
            {
                isGrounded = false;
                isSlidingOnSteepSlope = false;
                return false;
            }

            bool steep = IsSteep(finalGround);
            if (steep && !_slideOnSteepSlopes)
            {
                isGrounded = false;
                isSlidingOnSteepSlope = false;
                return false;
            }

            ApplyGroundHeight(finalGround, dt, ref position);
            isGrounded = !steep;
            isSlidingOnSteepSlope = steep;
            return true;
        }

        private void SetAirborneGroundState(Vector3 position)
        {
            groundPoint = position;
            groundNormal = Vector3.up;
            slopeAngle = 0f;
            isGrounded = false;
            isSlidingOnSteepSlope = false;
        }

        private void ApplyGroundHeight(GroundProbe ground, float dt, ref Vector3 position)
        {
            float targetY = ground.DesiredY;
            if (_groundHeightLerpSpeed <= 0f)
            {
                position.y = targetY;
                return;
            }

            float heightSpeed = targetY >= position.y
                ? _groundHeightLerpSpeed * 1.75f
                : _groundHeightLerpSpeed;
            float t = 1f - Mathf.Exp(-heightSpeed * dt);
            position.y = Mathf.Lerp(position.y, targetY, t);

            float maxAllowedError = Mathf.Max(0.01f, _groundCheckDistance * 0.5f);
            if (targetY > position.y && targetY - position.y > maxAllowedError)
            {
                position.y = targetY - maxAllowedError;
            }
        }

        private bool CanUseGround(GroundProbe ground, Vector3 position, bool wasGrounded)
        {
            if (!ground.Hit)
            {
                return false;
            }

            float distanceToDesired = position.y - ground.DesiredY;
            float lowerTolerance = wasGrounded
                ? -(_groundCheckDistance + _smallPitBridgeMaxDrop)
                : -_groundCheckDistance;
            float upperTolerance = wasGrounded
                ? _groundSnapDistance + _smallPitBridgeMaxDrop
                : _groundSnapDistance;

            return distanceToDesired >= lowerTolerance && distanceToDesired <= upperTolerance;
        }

        private Vector3 ResolveCollision(Vector3 startPosition, Vector3 motion)
        {
            Vector3 flatMotion = new Vector3(motion.x, 0f, motion.z);
            float flatDistance = flatMotion.magnitude;
            float collisionThreshold = Mathf.Max(0.01f, minCollisionCastDistance);
            if (flatDistance <= collisionThreshold || movementBody == null || movementBody.collisionMask.value == 0)
            {
                return motion;
            }

            Vector3 direction = flatMotion / flatDistance;
            movementBody.GetCollisionCapsule(startPosition, out Vector3 pointA, out Vector3 pointB, out float radius);
            float skin = movementBody.CollisionSkinWidth;
            if (!Physics.CapsuleCast(
                    pointA,
                    pointB,
                    radius,
                    direction,
                    out RaycastHit hit,
                    flatDistance + skin,
                    movementBody.collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return motion;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - skin);
            if (allowedDistance <= 0.0001f)
            {
                currentForwardSpeed = 0f;
                return TryBuildCollisionSlide(startPosition, Vector3.zero, flatMotion, hit.normal, skin);
            }

            float ratio = Mathf.Clamp01(allowedDistance / flatDistance);
            if (ratio < 0.999f)
            {
                currentForwardSpeed *= ratio;
            }

            Vector3 allowedMotion = motion * ratio;
            Vector3 remainingMotion = flatMotion * (1f - ratio);
            return TryBuildCollisionSlide(startPosition, allowedMotion, remainingMotion, hit.normal, skin);
        }

        private Vector3 TryBuildCollisionSlide(Vector3 startPosition, Vector3 allowedMotion, Vector3 remainingMotion, Vector3 hitNormal, float skin)
        {
            if (!allowCollisionSlide || !_wallSlideEnabled)
            {
                return allowedMotion;
            }

            Vector3 slide = Vector3.ProjectOnPlane(remainingMotion, hitNormal);
            slide.y = 0f;
            float slideDistance = slide.magnitude;
            float collisionThreshold = Mathf.Max(0.01f, minCollisionCastDistance);
            if (slideDistance <= collisionThreshold)
            {
                return allowedMotion;
            }

            Vector3 slideDirection = slide / slideDistance;
            movementBody.GetCollisionCapsule(startPosition + allowedMotion, out Vector3 pointA, out Vector3 pointB, out float radius);
            if (!Physics.CapsuleCast(
                    pointA,
                    pointB,
                    radius,
                    slideDirection,
                    out RaycastHit slideHit,
                    slideDistance + skin,
                    movementBody.collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                return allowedMotion + slide;
            }

            float allowedSlideDistance = Mathf.Max(0f, slideHit.distance - skin);
            if (allowedSlideDistance <= 0.0001f)
            {
                return allowedMotion;
            }

            return allowedMotion + slideDirection * Mathf.Min(slideDistance, allowedSlideDistance);
        }

        private void ApplyMotorRotation(Vector3 targetGroundNormal, float dt)
        {
            Transform alignmentRoot = slopeAlignmentRoot != null ? slopeAlignmentRoot : _cachedTransform;
            Vector3 alignmentNormal = GetSlopeAlignmentNormal(targetGroundNormal);
            Vector3 forward = Vector3.ProjectOnPlane(_yawForward, alignmentNormal);
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = _yawForward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, alignmentNormal);
            if (_slopeAlignmentSpeed <= 0f)
            {
                alignmentRoot.rotation = targetRotation;
                return;
            }

            float t = 1f - Mathf.Exp(-_slopeAlignmentSpeed * dt);
            alignmentRoot.rotation = Quaternion.Slerp(alignmentRoot.rotation, targetRotation, t);
        }

        private Vector3 GetSlopeAlignmentNormal(Vector3 normal)
        {
            if (!alignToGround || normal.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            normal.Normalize();
            float groundAngle = Vector3.Angle(Vector3.up, normal);
            if (groundAngle <= 0.001f)
            {
                return Vector3.up;
            }

            Vector3 axis = Vector3.Cross(Vector3.up, normal);
            if (axis.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            float targetAngle = Mathf.Min(_maxSlopeAlignmentAngle, groundAngle * _slopeAlignmentStrength);
            return Quaternion.AngleAxis(targetAngle, axis.normalized) * Vector3.up;
        }

        private Vector3 BuildProbeUp()
        {
            if (!useAlignedGroundProbeDirection)
            {
                return Vector3.up;
            }

            Transform alignmentRoot = slopeAlignmentRoot != null ? slopeAlignmentRoot : _cachedTransform;
            Vector3 up = alignmentRoot != null ? alignmentRoot.up : Vector3.up;
            if (up.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            up.Normalize();
            float blend = Mathf.Clamp01(groundProbeAlignmentBlend);
            if (blend < 0.999f)
            {
                up = Vector3.Slerp(Vector3.up, up, blend);
                if (up.sqrMagnitude <= 0.000001f)
                {
                    return Vector3.up;
                }

                up.Normalize();
            }

            float angle = Vector3.Angle(Vector3.up, up);
            if (angle <= _maxSlopeAlignmentAngle)
            {
                return up;
            }

            Vector3 axis = Vector3.Cross(Vector3.up, up);
            if (axis.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            return Quaternion.AngleAxis(_maxSlopeAlignmentAngle, axis.normalized) * Vector3.up;
        }

        private Vector3 GetProbeAlignmentNormal(GroundProbe ground)
        {
            if (ground.AlignmentNormal.sqrMagnitude > 0.000001f)
            {
                return ground.AlignmentNormal.normalized;
            }

            if (ground.Normal.sqrMagnitude > 0.000001f)
            {
                return ground.Normal.normalized;
            }

            return Vector3.up;
        }

        private void DrawGroundDebug(Vector3 position, GroundProbe ground)
        {
            Color color = Color.red;
            if (ground.Hit)
            {
                color = IsSteep(ground) ? Color.yellow : Color.green;
                Debug.DrawLine(position, ground.Point, color, Time.fixedDeltaTime);
                Debug.DrawRay(ground.Point, ground.Normal * 0.5f, color, Time.fixedDeltaTime);
                return;
            }

            Debug.DrawRay(position, -_probeUp * Mathf.Max(0.1f, movementBody.BodyHeightOffset + _groundProbeDistance), color, Time.fixedDeltaTime);
        }

        private bool IsSteep(GroundProbe ground)
        {
            return ground.Hit && ground.Normal.y < _maxSlopeDot;
        }

        private static Vector3 GetSlopeDownhill(Vector3 normal)
        {
            Vector3 downhill = new Vector3(normal.x, 0f, normal.z);
            if (downhill.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            return downhill.normalized;
        }

        private Vector3 RemoveUphillMotion(Vector3 motion, Vector3 normal)
        {
            if (motion.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            Vector3 downhill = GetSlopeDownhill(normal);
            if (downhill.sqrMagnitude <= 0.000001f)
            {
                return motion;
            }

            float uphillAmount = Vector3.Dot(motion, -downhill);
            if (uphillAmount <= 0f)
            {
                return motion;
            }

            float removeAmount = uphillAmount * Mathf.Clamp01(1f - _steepSlopeUphillControl);
            Vector3 adjusted = motion + downhill * removeAmount;
            if (adjusted.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            return adjusted;
        }

        private bool IsBraking(float targetSpeed)
        {
            if (Mathf.Abs(currentForwardSpeed) <= 0.001f)
            {
                return false;
            }

            if (Mathf.Abs(targetSpeed) <= 0.001f)
            {
                return true;
            }

            bool directionChanged = Mathf.Sign(currentForwardSpeed) != Mathf.Sign(targetSpeed);
            bool reducingSpeed = Mathf.Abs(targetSpeed) < Mathf.Abs(currentForwardSpeed);
            return directionChanged || reducingSpeed;
        }

        private void EnsureMotorYawInitialized()
        {
            if (_hasMotorYaw)
            {
                return;
            }

            _motorYaw = _cachedTransform != null ? _cachedTransform.eulerAngles.y : transform.eulerAngles.y;
            _hasMotorYaw = true;
            UpdateYawForward();
        }

        private void UpdateYawForward()
        {
            _yawForward = Quaternion.Euler(0f, _motorYaw, 0f) * Vector3.forward;
        }

        private GroundProbe CreateEmptyGroundProbe(Vector3 position)
        {
            return new GroundProbe
            {
                Hit = false,
                Point = position,
                Normal = Vector3.up,
                AlignmentNormal = Vector3.up,
                DesiredY = position.y,
                DistanceToDesiredHeight = float.PositiveInfinity,
                SampleOffsetSqr = float.PositiveInfinity,
                SlopeAngle = 0f
            };
        }

        private struct GroundProbe
        {
            public bool Hit;
            public Vector3 Point;
            public Vector3 Normal;
            public Vector3 AlignmentNormal;
            public float DesiredY;
            public float DistanceToDesiredHeight;
            public float SampleOffsetSqr;
            public float SlopeAngle;
        }
    }
}
