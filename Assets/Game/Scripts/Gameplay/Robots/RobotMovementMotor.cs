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
        public float minEdgeProbeMotion = 0.02f;
        public float minCollisionCastDistance = 0.01f;

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

        private bool _useRuntimeTraverseSpeed;
        private float _runtimeTraverseSpeedDegPerSecond;
        private bool _hasMotorYaw;
        private float _motorYaw;
        private Vector3 _slopeSlideVelocity;
        private RobotMovementGlobalSettings _activeSettings;
        private GroundProbe _lastGround;

        public float MotorYaw => _hasMotorYaw ? _motorYaw : transform.eulerAngles.y;

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

            EnsureBody();
            settings ??= RobotMovementGlobalSettings.Default;
            _activeSettings = settings;
            EnsureMotorYawInitialized();

            movementInput = Mathf.Clamp(input.y, -1f, 1f);
            turnInput = Mathf.Clamp(input.x, -1f, 1f);

            Vector3 startPosition = transform.position;
            bool wasGrounded = isGrounded;
            float probeDistance = GetGroundProbeDistance(settings);
            GroundProbe startGround = ProbeGround(startPosition, probeDistance, Vector3.zero);

            ApplyGroundState(startGround, startPosition, wasGrounded);
            UpdateTurn(settings, dt);
            UpdateForwardSpeed(settings, isLegged, dt);

            Vector3 horizontalMotion = BuildHorizontalMotion(startGround, dt);
            horizontalMotion += UpdateSteepSlopeSlide(startGround, startPosition, wasGrounded, dt);
            horizontalMotion = ResolveCollision(startPosition, horizontalMotion);
            float verticalMotion = UpdateVerticalVelocity(settings, startGround, startPosition, wasGrounded, dt);

            Vector3 nextPosition = startPosition + horizontalMotion + Vector3.up * verticalMotion;
            GroundProbe finalGround = horizontalMotion.sqrMagnitude <= 0.000001f && Mathf.Abs(verticalMotion) <= 0.000001f
                ? startGround
                : ProbeGround(nextPosition, probeDistance + Mathf.Abs(verticalMotion), horizontalMotion);
            if (TryResolveFinalGround(finalGround, wasGrounded, settings, dt, ref nextPosition))
            {
                verticalVelocity = 0f;
            }

            transform.position = nextPosition;
            ApplyMotorRotation((isGrounded || isSlidingOnSteepSlope) ? groundNormal : Vector3.up, dt);
            velocity = (nextPosition - startPosition) / dt;

            if (debugGrounding)
            {
                DrawGroundDebug(nextPosition, finalGround);
            }
        }

        private void EnsureBody()
        {
            if (movementBody != null)
            {
                return;
            }

            movementBody = GetComponent<RobotMovementBody>();
            if (movementBody == null)
            {
                movementBody = gameObject.AddComponent<RobotMovementBody>();
            }
        }

        private Vector3 BuildHorizontalMotion(GroundProbe ground, float dt)
        {
            float slopeLimit = GetMaxSlopeAngle();
            Vector3 direction = GetYawForward();
            if (ground.Hit && ground.SlopeAngle <= slopeLimit)
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(direction, ground.Normal);
                slopeDirection.y = 0f;
                if (slopeDirection.sqrMagnitude > 0.000001f)
                {
                    direction = slopeDirection.normalized;
                }
            }
            else if (ground.Hit && IsMovingUpTooSteepSlope(direction, ground.Normal))
            {
                direction = Vector3.Lerp(direction, GetSlopeDownhill(ground.Normal), Mathf.Clamp01(1f - GetSteepSlopeUphillControl()));
            }

            Vector3 motion = direction * (currentForwardSpeed * dt);
            motion.y = 0f;
            if (ground.Hit && ground.SlopeAngle > slopeLimit)
            {
                motion = RemoveUphillMotion(motion, ground.Normal);
            }

            return motion;
        }

        private void UpdateTurn(RobotMovementGlobalSettings settings, float dt)
        {
            float targetTurnSpeed = turnInput * GetTurnSpeed(settings);
            float turnRate = GetTurnAcceleration(settings);
            currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurnSpeed, turnRate * dt);

            if (Mathf.Abs(currentTurnSpeed) <= 0.001f)
            {
                currentTurnSpeed = 0f;
                return;
            }

            _motorYaw = Mathf.Repeat(_motorYaw + currentTurnSpeed * dt, 360f);
        }

        private void UpdateForwardSpeed(RobotMovementGlobalSettings settings, bool isLegged, float dt)
        {
            float targetSpeed = GetTargetForwardSpeed(settings);
            float changeRate;
            if (Mathf.Abs(movementInput) <= 0.001f)
            {
                changeRate = GetNaturalDeceleration(settings);
            }
            else if (IsBraking(targetSpeed))
            {
                changeRate = GetBrakeDeceleration(settings, isLegged);
            }
            else
            {
                changeRate = GetAcceleration(settings) * settings.GetAccelerationMultiplier(isLegged);
            }

            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, changeRate * dt);
            currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, -GetMaxReverseSpeed(settings), GetMaxForwardSpeed(settings));
        }

        private Vector3 UpdateSteepSlopeSlide(GroundProbe ground, Vector3 position, bool wasGrounded, float dt)
        {
            float slopeLimit = GetMaxSlopeAngle();
            if (!GetSlideOnSteepSlopes() || !ground.Hit || ground.SlopeAngle <= slopeLimit || !CanUseGround(ground, position, wasGrounded))
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

            float slope01 = Mathf.InverseLerp(slopeLimit, 89f, ground.SlopeAngle);
            float accelerationValue = GetSteepSlopeSlideAcceleration() * Mathf.Clamp01(slope01);
            _slopeSlideVelocity += downhill * (accelerationValue * dt);

            float maxSpeed = GetSteepSlopeMaxSlideSpeed();
            if (maxSpeed > 0f && _slopeSlideVelocity.magnitude > maxSpeed)
            {
                _slopeSlideVelocity = _slopeSlideVelocity.normalized * maxSpeed;
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

            float damping = GetSteepSlopeSlideDamping();
            if (damping <= 0f || dt <= 0f)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-damping * dt);
            _slopeSlideVelocity = Vector3.Lerp(_slopeSlideVelocity, Vector3.zero, t);
            if (_slopeSlideVelocity.sqrMagnitude <= 0.000001f)
            {
                _slopeSlideVelocity = Vector3.zero;
            }
        }

        private float UpdateVerticalVelocity(RobotMovementGlobalSettings settings, GroundProbe ground, Vector3 position, bool wasGrounded, float dt)
        {
            if (ground.Hit && CanUseGround(ground, position, wasGrounded) && (ground.SlopeAngle <= GetMaxSlopeAngle() || GetSlideOnSteepSlopes()))
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = 0f;
                }

                return 0f;
            }

            verticalVelocity -= GetGravity(settings) * dt;
            float fallSpeedLimit = GetMaxFallSpeed(settings);
            if (verticalVelocity < -fallSpeedLimit)
            {
                verticalVelocity = -fallSpeedLimit;
            }

            return verticalVelocity * dt;
        }

        private GroundProbe ProbeGround(Vector3 position, float distance, Vector3 motion)
        {
            GroundProbe probe = CreateEmptyGroundProbe(position);
            if (movementBody.groundMask.value == 0)
            {
                return probe;
            }

            bool hasCenter = TryProbeGroundSingle(position, position, distance, 0f, out probe);
            if (!ShouldUseEdgeProbe(motion))
            {
                UpdateCachedGround(probe);
                return probe;
            }

            Vector3 edgeDirection = GetEdgeProbeDirection(motion);
            float edgeOffset = GetEdgeProbeOffset(edgeDirection);
            if (edgeOffset <= 0.001f)
            {
                UpdateCachedGround(probe);
                return probe;
            }

            if (TryProbeGroundSingle(position, position + edgeDirection * edgeOffset, distance, edgeOffset * edgeOffset, out GroundProbe edgeProbe))
            {
                if (hasCenter)
                {
                    ApplyEdgeProbeNormal(ref probe, edgeProbe);
                }

                if (IsBetterGroundProbe(edgeProbe, probe))
                {
                    probe = edgeProbe;
                }
            }

            UpdateCachedGround(probe);
            return probe;
        }

        private bool ShouldUseEdgeProbe(Vector3 motion)
        {
            if (!useDirectionalEdgeGroundProbe)
            {
                return false;
            }

            if (!useEdgeProbeOnlyWhenMoving)
            {
                return true;
            }

            return motion.sqrMagnitude >= minEdgeProbeMotion * minEdgeProbeMotion || Mathf.Abs(currentForwardSpeed) > 0.05f;
        }

        private Vector3 GetEdgeProbeDirection(Vector3 motion)
        {
            Vector3 flatMotion = new Vector3(motion.x, 0f, motion.z);
            if (flatMotion.sqrMagnitude > 0.000001f)
            {
                return flatMotion.normalized;
            }

            return GetYawForward();
        }

        private float GetEdgeProbeOffset(Vector3 edgeDirection)
        {
            Vector3 forward = GetYawForward();
            float forwardDot = Mathf.Abs(Vector3.Dot(edgeDirection, forward));
            return forwardDot >= 0.707f
                ? movementBody.GroundProbeForwardOffset
                : movementBody.GroundProbeSideOffset;
        }

        private bool TryProbeGroundSingle(Vector3 centerPosition, Vector3 samplePosition, float distance, float sampleOffsetSqr, out GroundProbe probe)
        {
            probe = CreateEmptyGroundProbe(centerPosition);
            float radius = movementBody.GroundProbeRadius;
            Vector3 probeUp = GetGroundProbeUp();
            Vector3 origin = samplePosition + probeUp * radius;
            float castDistance = Mathf.Max(0.01f, movementBody.BodyHeightOffset + radius + distance);
            if (!Physics.SphereCast(
                    origin,
                    radius,
                    -probeUp,
                    out RaycastHit hit,
                    castDistance,
                    movementBody.groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 normal = hit.normal.sqrMagnitude > 0.000001f ? hit.normal.normalized : Vector3.up;
            Vector3 groundPoint = GetGroundPointUnderTiltedBody(centerPosition, hit.point, normal, probeUp);
            float desiredY = groundPoint.y + probeUp.y * movementBody.BodyHeightOffset;
            probe = new GroundProbe
            {
                Hit = true,
                Point = groundPoint,
                Normal = normal,
                AlignmentNormal = normal,
                ProbeUp = probeUp,
                DesiredY = desiredY,
                DistanceToDesiredHeight = centerPosition.y - desiredY,
                SampleOffsetSqr = sampleOffsetSqr,
                SampleGroundY = hit.point.y
            };
            probe.SlopeAngle = Vector3.Angle(probe.Normal, Vector3.up);
            return true;
        }

        private void ApplyEdgeProbeNormal(ref GroundProbe center, GroundProbe edge)
        {
            if (!center.Hit || !edge.Hit || center.SlopeAngle > GetMaxSlopeAngle())
            {
                return;
            }

            Vector3 normal = center.Normal + edge.Normal;
            if (normal.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            normal.Normalize();
            float angle = Vector3.Angle(normal, Vector3.up);
            if (angle <= GetMaxSlopeAlignmentAngle())
            {
                center.AlignmentNormal = normal;
            }
        }

        private Vector3 GetGroundPointUnderTiltedBody(Vector3 centerPosition, Vector3 hitPoint, Vector3 normal, Vector3 probeUp)
        {
            Vector3 lowerPoint = centerPosition - probeUp * movementBody.BodyHeightOffset;
            float supportY = GetGroundYAtPoint(lowerPoint, hitPoint, normal);
            return new Vector3(lowerPoint.x, supportY, lowerPoint.z);
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

        private bool IsBetterGroundProbe(GroundProbe candidate, GroundProbe current)
        {
            if (!candidate.Hit)
            {
                return false;
            }

            if (!current.Hit)
            {
                return true;
            }

            bool candidateUsable = candidate.SlopeAngle <= GetMaxSlopeAngle();
            bool currentUsable = current.SlopeAngle <= GetMaxSlopeAngle();
            if (candidateUsable && !currentUsable)
            {
                return true;
            }

            if (!candidateUsable && currentUsable)
            {
                return false;
            }

            bool candidateNeedsLift = candidate.DistanceToDesiredHeight < -0.001f;
            bool currentNeedsLift = current.DistanceToDesiredHeight < -0.001f;
            if (candidateUsable && currentUsable && candidateNeedsLift != currentNeedsLift)
            {
                return candidateNeedsLift;
            }

            float candidateDistance = Mathf.Abs(candidate.DistanceToDesiredHeight);
            float currentDistance = Mathf.Abs(current.DistanceToDesiredHeight);
            if (candidateDistance < currentDistance - 0.01f)
            {
                return true;
            }

            if (candidateDistance > currentDistance + 0.01f)
            {
                return false;
            }

            return candidate.SampleOffsetSqr < current.SampleOffsetSqr;
        }

        private void UpdateCachedGround(GroundProbe probe)
        {
            if (probe.Hit)
            {
                _lastGround = probe;
            }
        }

        private GroundProbe CreateEmptyGroundProbe(Vector3 position)
        {
            return new GroundProbe
            {
                Hit = false,
                Point = position,
                Normal = Vector3.up,
                AlignmentNormal = Vector3.up,
                ProbeUp = Vector3.up,
                SlopeAngle = 0f,
                DesiredY = position.y,
                DistanceToDesiredHeight = float.PositiveInfinity,
                SampleOffsetSqr = float.PositiveInfinity,
                SampleGroundY = position.y
            };
        }

        private void ApplyGroundState(GroundProbe ground, Vector3 position, bool wasGrounded)
        {
            if (ground.Hit)
            {
                groundPoint = ground.Point;
                groundNormal = GetProbeAlignmentNormal(ground);
                slopeAngle = ground.SlopeAngle;
                bool canUseGround = CanUseGround(ground, position, wasGrounded);
                isGrounded = ground.SlopeAngle <= GetMaxSlopeAngle() && canUseGround;
                isSlidingOnSteepSlope = !isGrounded && GetSlideOnSteepSlopes() && ground.SlopeAngle > GetMaxSlopeAngle() && canUseGround;
                return;
            }

            groundPoint = position;
            groundNormal = Vector3.up;
            slopeAngle = 0f;
            isGrounded = false;
            isSlidingOnSteepSlope = false;
        }

        private bool TryResolveFinalGround(GroundProbe finalGround, bool wasGrounded, RobotMovementGlobalSettings settings, float dt, ref Vector3 position)
        {
            if (!finalGround.Hit)
            {
                ApplyGroundState(finalGround, position, wasGrounded);
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

            bool steep = finalGround.SlopeAngle > GetMaxSlopeAngle();
            if (steep && !GetSlideOnSteepSlopes())
            {
                isGrounded = false;
                isSlidingOnSteepSlope = false;
                return false;
            }

            ApplyGroundHeight(finalGround, settings, dt, ref position);
            isGrounded = !steep;
            isSlidingOnSteepSlope = steep;
            return true;
        }

        private void ApplyGroundHeight(GroundProbe ground, RobotMovementGlobalSettings settings, float dt, ref Vector3 position)
        {
            float targetY = ground.DesiredY;
            float lerpSpeed = GetGroundHeightLerpSpeed(settings);
            if (lerpSpeed <= 0f || dt <= 0f)
            {
                position.y = targetY;
                return;
            }

            float t = 1f - Mathf.Exp(-lerpSpeed * dt);
            position.y = Mathf.Lerp(position.y, targetY, t);
            float maxAllowedError = Mathf.Max(0.01f, GetGroundCheckDistance(settings) * 0.5f);
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
            float checkDistance = GetGroundCheckDistance(_activeSettings);
            float snapDistance = Mathf.Max(checkDistance, GetGroundSnapDistance(_activeSettings));
            if (distanceToDesired >= -snapDistance && distanceToDesired <= snapDistance)
            {
                return true;
            }

            if (wasGrounded && distanceToDesired >= -(snapDistance + checkDistance) && distanceToDesired <= snapDistance + checkDistance)
            {
                return true;
            }

            if (_lastGround.Hit && GetSmallPitBridgeMaxDrop() > 0f && Mathf.Abs(distanceToDesired) <= snapDistance + GetSmallPitBridgeMaxDrop())
            {
                return true;
            }

            return false;
        }

        private Vector3 ResolveCollision(Vector3 startPosition, Vector3 motion)
        {
            Vector3 flatMotion = new Vector3(motion.x, 0f, motion.z);
            float flatDistance = flatMotion.magnitude;
            if (flatDistance <= minCollisionCastDistance || movementBody.collisionMask.value == 0)
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
            if (!allowCollisionSlide || !GetWallSlideEnabled())
            {
                return allowedMotion;
            }

            Vector3 slide = Vector3.ProjectOnPlane(remainingMotion, hitNormal);
            slide.y = 0f;
            float slideDistance = slide.magnitude;
            if (slideDistance <= 0.0001f)
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
            Transform alignmentRoot = slopeAlignmentRoot != null ? slopeAlignmentRoot : transform;
            Vector3 alignmentNormal = GetSlopeAlignmentNormal(targetGroundNormal);
            Vector3 forward = Vector3.ProjectOnPlane(GetYawForward(), alignmentNormal);
            if (forward.sqrMagnitude <= 0.000001f)
            {
                forward = GetYawForward();
            }

            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, alignmentNormal);
            float t = 1f;
            float alignmentSpeed = GetSlopeAlignmentSpeed();
            if (alignmentSpeed > 0f && dt > 0f)
            {
                t = 1f - Mathf.Exp(-alignmentSpeed * dt);
            }

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

            float targetAngle = Mathf.Min(GetMaxSlopeAlignmentAngle(), groundAngle * GetSlopeAlignmentStrength());
            return Quaternion.AngleAxis(targetAngle, axis.normalized) * Vector3.up;
        }

        private Vector3 GetGroundProbeUp()
        {
            if (!useAlignedGroundProbeDirection)
            {
                return Vector3.up;
            }

            Transform alignmentRoot = slopeAlignmentRoot != null ? slopeAlignmentRoot : transform;
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
            float maxAngle = Mathf.Clamp(GetMaxSlopeAlignmentAngle(), 0f, 85f);
            if (angle <= maxAngle)
            {
                return up;
            }

            Vector3 axis = Vector3.Cross(Vector3.up, up);
            if (axis.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            return Quaternion.AngleAxis(maxAngle, axis.normalized) * Vector3.up;
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
                color = ground.SlopeAngle <= GetMaxSlopeAngle() ? Color.green : Color.yellow;
                Debug.DrawLine(position, ground.Point, color, Time.fixedDeltaTime);
                Debug.DrawRay(ground.Point, ground.Normal * 0.5f, color, Time.fixedDeltaTime);
                return;
            }

            Debug.DrawRay(position, -GetGroundProbeUp() * Mathf.Max(0.1f, movementBody.BodyHeightOffset + GetGroundSnapDistance(_activeSettings)), color, Time.fixedDeltaTime);
        }

        private bool IsMovingUpTooSteepSlope(Vector3 flatDirection, Vector3 normal)
        {
            Vector3 downhill = GetSlopeDownhill(normal);
            if (downhill.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            return Vector3.Dot(flatDirection, downhill) < -0.05f;
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
            Vector3 flatMotion = new Vector3(motion.x, 0f, motion.z);
            if (flatMotion.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            Vector3 downhill = GetSlopeDownhill(normal);
            if (downhill.sqrMagnitude <= 0.000001f)
            {
                return motion;
            }

            float uphillAmount = Vector3.Dot(flatMotion, -downhill);
            if (uphillAmount <= 0f)
            {
                return motion;
            }

            float removeAmount = uphillAmount * Mathf.Clamp01(1f - GetSteepSlopeUphillControl());
            Vector3 adjusted = flatMotion + downhill * removeAmount;
            if (adjusted.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            return adjusted;
        }

        private Vector3 GetYawForward()
        {
            Quaternion yawRotation = Quaternion.Euler(0f, _motorYaw, 0f);
            return yawRotation * Vector3.forward;
        }

        private void EnsureMotorYawInitialized()
        {
            if (_hasMotorYaw)
            {
                return;
            }

            _motorYaw = transform.eulerAngles.y;
            _hasMotorYaw = true;
        }

        private float GetTargetForwardSpeed(RobotMovementGlobalSettings settings)
        {
            if (movementInput > 0f)
            {
                return movementInput * GetMaxForwardSpeed(settings);
            }

            if (movementInput < 0f)
            {
                return movementInput * GetMaxReverseSpeed(settings);
            }

            return 0f;
        }

        private float GetMaxForwardSpeed(RobotMovementGlobalSettings settings)
        {
            if (maxForwardSpeed > 0f)
            {
                return maxForwardSpeed;
            }

            return Mathf.Max(0f, settings.fallbackMaxSpeed);
        }

        private float GetMaxReverseSpeed(RobotMovementGlobalSettings settings)
        {
            if (maxReverseSpeed > 0f)
            {
                return maxReverseSpeed;
            }

            return GetMaxForwardSpeed(settings) * 0.5f;
        }

        private float GetAcceleration(RobotMovementGlobalSettings settings)
        {
            if (acceleration > 0f)
            {
                return acceleration;
            }

            return Mathf.Max(0.01f, settings.fallbackAcceleration);
        }

        private float GetBrakeDeceleration(RobotMovementGlobalSettings settings, bool isLegged)
        {
            float value = brakeDeceleration > 0f
                ? brakeDeceleration
                : GetAcceleration(settings) * Mathf.Max(1f, settings.stoppingAccelerationMultiplier);
            return Mathf.Max(0.01f, value * settings.GetBrakingMultiplier(isLegged));
        }

        private float GetNaturalDeceleration(RobotMovementGlobalSettings settings)
        {
            if (naturalDeceleration > 0f)
            {
                return naturalDeceleration;
            }

            return GetAcceleration(settings);
        }

        private float GetTurnSpeed(RobotMovementGlobalSettings settings)
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

            return rotateSpeed / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        }

        private float GetTurnAcceleration(RobotMovementGlobalSettings settings)
        {
            if (turnAcceleration > 0f)
            {
                return turnAcceleration;
            }

            return Mathf.Max(1f, GetTurnSpeed(settings) * 4f);
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

        private float GetGravity(RobotMovementGlobalSettings settings)
        {
            if (settings != null && settings.gravity > 0f)
            {
                return settings.gravity;
            }

            if (gravity > 0f)
            {
                return gravity;
            }

            return RobotMovementGlobalSettings.Default.gravity;
        }

        private float GetMaxFallSpeed(RobotMovementGlobalSettings settings)
        {
            if (settings != null && settings.maxFallSpeed > 0f)
            {
                return settings.maxFallSpeed;
            }

            if (maxFallSpeed > 0f)
            {
                return maxFallSpeed;
            }

            return RobotMovementGlobalSettings.Default.maxFallSpeed;
        }

        private float GetGroundProbeDistance(RobotMovementGlobalSettings settings)
        {
            return Mathf.Max(GetGroundCheckDistance(settings), GetGroundSnapDistance(settings));
        }

        private float GetGroundCheckDistance(RobotMovementGlobalSettings settings)
        {
            if (settings != null && settings.groundCheckDistance > 0f)
            {
                return settings.groundCheckDistance;
            }

            if (groundCheckDistance > 0f)
            {
                return groundCheckDistance;
            }

            return RobotMovementGlobalSettings.Default.groundCheckDistance;
        }

        private float GetGroundSnapDistance(RobotMovementGlobalSettings settings)
        {
            if (settings != null && settings.groundSnapDistance > 0f)
            {
                return settings.groundSnapDistance;
            }

            if (groundSnapDistance > 0f)
            {
                return groundSnapDistance;
            }

            if (settings != null && settings.groundedSnap > 0f)
            {
                return settings.groundedSnap;
            }

            return RobotMovementGlobalSettings.Default.groundSnapDistance;
        }

        private float GetGroundHeightLerpSpeed(RobotMovementGlobalSettings settings)
        {
            if (settings != null && settings.groundHeightLerpSpeed >= 0f)
            {
                return settings.groundHeightLerpSpeed;
            }

            return Mathf.Max(0f, groundHeightLerpSpeed);
        }

        private float GetSmallPitBridgeMaxDrop()
        {
            if (_activeSettings != null && _activeSettings.smallPitBridgeMaxDrop >= 0f)
            {
                return _activeSettings.smallPitBridgeMaxDrop;
            }

            return Mathf.Max(0f, smallPitBridgeMaxDrop);
        }

        private float GetMaxSlopeAngle()
        {
            if (_activeSettings != null && _activeSettings.maxSlopeAngle > 0f)
            {
                return Mathf.Clamp(_activeSettings.maxSlopeAngle, 0.01f, 89f);
            }

            if (maxSlopeAngle > 0f)
            {
                return Mathf.Clamp(maxSlopeAngle, 0.01f, 89f);
            }

            return RobotMovementGlobalSettings.Default.maxSlopeAngle;
        }

        private bool GetSlideOnSteepSlopes()
        {
            if (_activeSettings != null)
            {
                return _activeSettings.slideOnSteepSlopes;
            }

            return slideOnSteepSlopes;
        }

        private float GetSteepSlopeSlideAcceleration()
        {
            if (_activeSettings != null && _activeSettings.steepSlopeSlideAcceleration >= 0f)
            {
                return _activeSettings.steepSlopeSlideAcceleration;
            }

            return Mathf.Max(0f, steepSlopeSlideAcceleration);
        }

        private float GetSteepSlopeMaxSlideSpeed()
        {
            if (_activeSettings != null && _activeSettings.steepSlopeMaxSlideSpeed >= 0f)
            {
                return _activeSettings.steepSlopeMaxSlideSpeed;
            }

            return Mathf.Max(0f, steepSlopeMaxSlideSpeed);
        }

        private float GetSteepSlopeSlideDamping()
        {
            if (_activeSettings != null && _activeSettings.steepSlopeSlideDamping >= 0f)
            {
                return _activeSettings.steepSlopeSlideDamping;
            }

            return Mathf.Max(0f, steepSlopeSlideDamping);
        }

        private float GetSteepSlopeUphillControl()
        {
            if (_activeSettings != null && _activeSettings.steepSlopeUphillControl >= 0f)
            {
                return Mathf.Clamp01(_activeSettings.steepSlopeUphillControl);
            }

            return Mathf.Clamp01(steepSlopeUphillControl);
        }

        private float GetSlopeAlignmentStrength()
        {
            if (_activeSettings != null && _activeSettings.slopeAlignmentStrength >= 0f)
            {
                return _activeSettings.slopeAlignmentStrength;
            }

            return Mathf.Max(0f, slopeAlignmentStrength);
        }

        private float GetSlopeAlignmentSpeed()
        {
            if (_activeSettings != null && _activeSettings.slopeAlignmentSpeed >= 0f)
            {
                return _activeSettings.slopeAlignmentSpeed;
            }

            return Mathf.Max(0f, slopeAlignmentSpeed);
        }

        private float GetMaxSlopeAlignmentAngle()
        {
            if (_activeSettings != null && _activeSettings.maxSlopeAlignmentAngle >= 0f)
            {
                return Mathf.Clamp(_activeSettings.maxSlopeAlignmentAngle, 0f, 85f);
            }

            return Mathf.Clamp(maxSlopeAlignmentAngle, 0f, 85f);
        }

        private bool GetWallSlideEnabled()
        {
            if (_activeSettings != null)
            {
                return _activeSettings.wallSlideEnabled;
            }

            return true;
        }

        private struct GroundProbe
        {
            public bool Hit;
            public Vector3 Point;
            public Vector3 Normal;
            public Vector3 AlignmentNormal;
            public Vector3 ProbeUp;
            public float SlopeAngle;
            public float DesiredY;
            public float DistanceToDesiredHeight;
            public float SampleOffsetSqr;
            public float SampleGroundY;
        }
    }
}
