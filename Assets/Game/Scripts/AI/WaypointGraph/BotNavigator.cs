using System.Collections.Generic;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class BotNavigator : MonoBehaviour
    {
        private const float FailedRepathBackoffSeconds = 2f;
        private const float MinimumPivotProgressDegrees = 4f;
        private const int ProximityDirectionCount = 8;
        private const int ProximityVerticalRayCount = 3;
        private const int ProximityRaycastBufferSize = 32;
        private const string DeathDebrisLayerName = "Chassis";
        private static int _deathDebrisLayer = int.MinValue;
        private static readonly Vector3[] ProximityDirections =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            new Vector3(1f, 0f, 1f).normalized,
            new Vector3(-1f, 0f, 1f).normalized,
            new Vector3(1f, 0f, -1f).normalized,
            new Vector3(-1f, 0f, -1f).normalized
        };

        [SerializeField] private Transform target;
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly List<int> _path = new List<int>(32);
        private readonly RaycastHit[] _proximityRaycastBuffer = new RaycastHit[ProximityRaycastBufferSize];
        private VehicleRoot _vehicleRoot;
        private ServerRoom _room;
        private WaypointGraphRuntime _graph;
        private WaypointAStarPathfinder _pathfinder;
        private IBotInputReceiver _inputReceiver;
        private int _pathIndex;
        private int _destinationNodeId = -1;
        private Vector3 _targetPosition;
        private Vector3 _pathTargetPosition;
        private Vector3 _lastProgressPosition;
        private float _lastProgressYaw;
        private float _nextTickTime;
        private float _nextRepathTime;
        private float _nextStuckCheckTime;
        private float _unstickUntilTime;
        private float _nextFallbackInputChangeTime;
        private float _nextProximityScanTime;
        private float _smoothedTurnInput;
        private float _lastSteeringSampleTime;
        private float _turnBias = 1f;
        private Vector3 _desiredTravelDirection;
        private Vector3 _proximityAvoidance;
        private int[] _nodeVisitCounts = System.Array.Empty<int>();
        private int _lastVisitedNodeId = -1;
        private float _lastDesiredTravelDirectionTime;
        private bool _hasExplicitTarget;
        private bool _hasExplicitPositionTarget;
        private bool _hasDesiredTravelDirection;
        private bool _isPivotTurning;
        private bool _isUnsticking;
        private bool _isInitialized;
        private bool _movementSuppressed;

        private void Awake()
        {
            _turnBias = Random.value < 0.5f ? -1f : 1f;
        }

        public void Initialize(VehicleRoot vehicleRoot, ServerRoom room, WaypointGraphRuntime graph)
        {
            _vehicleRoot = vehicleRoot;
            _room = room;
            _graph = graph;
            _pathfinder = graph != null ? new WaypointAStarPathfinder(graph) : null;
            _inputReceiver = vehicleRoot != null ? vehicleRoot.inputManager as IBotInputReceiver : null;
            _isInitialized = true;

            float now = Time.time;
            BotWanderSettings settings = ServerSettings.GetBotWander();
            _nextTickTime = now + Random.Range(0f, settings.thinkInterval);
            _nextRepathTime = now + Random.Range(0f, settings.repathCooldown);
            _nextStuckCheckTime = now + Random.Range(0.1f, settings.stuckCheckInterval);
            _nextProximityScanTime = now + Random.Range(0f, settings.proximityScanInterval);
            ResetSteeringSmoothing();
            _proximityAvoidance = Vector3.zero;
            InitializeExplorationMemory();
            _lastProgressPosition = GetMovePosition();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _hasExplicitPositionTarget = false;
            _hasExplicitTarget = target != null;
            ResetSteeringSmoothing();
            ResetProgressTracking();
            ClearPath();
            _nextRepathTime = 0f;
        }

        public void SetTargetPosition(Vector3 position)
        {
            target = null;
            _targetPosition = position;
            _hasExplicitPositionTarget = true;
            _hasExplicitTarget = true;
            ResetSteeringSmoothing();
            ResetProgressTracking();
            ClearPath();
            _nextRepathTime = 0f;
        }

        public void UpdateTargetPosition(Vector3 position)
        {
            target = null;
            _targetPosition = position;
            _hasExplicitPositionTarget = true;
            _hasExplicitTarget = true;
            ClearPath();
            _nextRepathTime = 0f;
        }

        public void SetMovementSuppressed(bool suppressed)
        {
            if (_movementSuppressed == suppressed)
            {
                return;
            }

            _movementSuppressed = suppressed;
            ResetProgressTracking();
            if (_movementSuppressed)
            {
                _isPivotTurning = false;
                ResetSteeringSmoothing();
                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
            }
        }

        public bool TryGetDesiredTravelDirection(out Vector3 direction, float maxAgeSeconds)
        {
            direction = _desiredTravelDirection;
            if (!_hasDesiredTravelDirection)
            {
                return false;
            }

            if (maxAgeSeconds > 0f && Time.time - _lastDesiredTravelDirectionTime > maxAgeSeconds)
            {
                return false;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        public void Stop()
        {
            _movementSuppressed = false;
            _isPivotTurning = false;
            ResetSteeringSmoothing();
            ClearDesiredTravelDirection();
            ApplyInput(0f, 0f);
            ClearPath();
            enabled = false;
        }

        private void FixedUpdate()
        {
            if (!_isInitialized || !IsServerReady())
            {
                return;
            }

            using (ProfileScope.Measure("Server.BotNavigator.FixedUpdate", DiagnosticsCategories.Ai))
            {
                if (_vehicleRoot.health != null && _vehicleRoot.health.IsDead)
                {
                    Stop();
                    return;
                }

                float now = Time.time;
                BotWanderSettings settings = ServerSettings.GetBotWander();
                RefreshProximityAvoidance(GetMovePosition(), settings, now);

                if (_movementSuppressed)
                {
                    if (!TryApplyProximityEscape(settings))
                    {
                        ApplyInput(0f, 0f);
                    }

                    return;
                }

                if (now < _nextTickTime)
                {
                    return;
                }

                _nextTickTime = now + settings.thinkInterval;
                TrackVisitedNode();

                if (_graph == null || !_graph.IsBuilt || _pathfinder == null)
                {
                    TickFallbackWander(settings, now);
                    return;
                }

                if (_isUnsticking)
                {
                    if (now < _unstickUntilTime)
                    {
                        return;
                    }

                    _isUnsticking = false;
                    ClearPath();
                }

                if (_path.Count > 0 && now >= _nextStuckCheckTime && TryStartUnstick(settings, now))
                {
                    return;
                }

                if (target != null)
                {
                    _targetPosition = target.position;
                    _hasExplicitTarget = true;
                }
                else if (!_hasExplicitPositionTarget)
                {
                    _hasExplicitTarget = false;
                }

                if (_hasExplicitTarget && _path.Count > 0 && HasTargetMovedEnough(settings))
                {
                    ClearPath();
                }

                if (_path.Count == 0 || _pathIndex >= _path.Count)
                {
                    using (ProfileScope.Measure("Server.BotNavigator.Repath", DiagnosticsCategories.Ai))
                    {
                        Repath(settings, now);
                    }
                }

                FollowPath(settings, now);
            }
        }

        private bool IsServerReady()
        {
            return _vehicleRoot != null
                   && _vehicleRoot.IsServerInitialized
                   && _vehicleRoot.inputManager != null
                   && _inputReceiver != null;
        }

        private void Repath(BotWanderSettings settings, float now)
        {
            if (now < _nextRepathTime)
            {
                return;
            }

            _nextRepathTime = now + settings.repathCooldown;

            int startNodeId = _graph.FindNearestNode(GetMovePosition());
            int goalNodeId = _hasExplicitTarget
                ? _graph.FindNearestNode(_targetPosition)
                : PickRandomDestinationNode(startNodeId, settings);

            if (startNodeId < 0 || goalNodeId < 0)
            {
                ClearPath();
                _nextRepathTime = now + Mathf.Max(settings.repathCooldown, FailedRepathBackoffSeconds);
                return;
            }

            if (!_pathfinder.FindPath(startNodeId, goalNodeId, _path))
            {
                ClearPath();
                _nextRepathTime = now + Mathf.Max(settings.repathCooldown, FailedRepathBackoffSeconds);
                return;
            }

            _destinationNodeId = goalNodeId;
            _pathTargetPosition = _hasExplicitTarget ? _targetPosition : _graph.GetNodePosition(goalNodeId);
            _pathIndex = _path.Count > 1 ? 1 : 0;
        }

        private bool HasTargetMovedEnough(BotWanderSettings settings)
        {
            Vector3 moved = _targetPosition - _pathTargetPosition;
            moved.y = 0f;
            return moved.sqrMagnitude >= settings.targetRepathDistance * settings.targetRepathDistance;
        }

        private int PickRandomDestinationNode(int startNodeId, BotWanderSettings settings)
        {
            int fallbackNodeId = _graph.GetRandomNodeId();
            if (startNodeId < 0 || !settings.exploreUnvisitedNodes || _nodeVisitCounts.Length != _graph.NodeCount)
            {
                return fallbackNodeId;
            }

            Vector3 startPosition = _graph.GetNodePosition(startNodeId);
            float minDistanceSqr = settings.minDestinationDistance * settings.minDestinationDistance;

            int destinationNodeId = FindExplorationDestination(
                startNodeId,
                startPosition,
                minDistanceSqr,
                settings,
                onlyUnvisited: true,
                requireMinimumDistance: true);
            if (destinationNodeId >= 0)
            {
                return destinationNodeId;
            }

            destinationNodeId = FindExplorationDestination(
                startNodeId,
                startPosition,
                minDistanceSqr,
                settings,
                onlyUnvisited: false,
                requireMinimumDistance: true);
            if (destinationNodeId >= 0)
            {
                return destinationNodeId;
            }

            destinationNodeId = FindExplorationDestination(
                startNodeId,
                startPosition,
                minDistanceSqr,
                settings,
                onlyUnvisited: true,
                requireMinimumDistance: false);
            return destinationNodeId >= 0 ? destinationNodeId : fallbackNodeId;
        }

        private int FindExplorationDestination(
            int startNodeId,
            Vector3 startPosition,
            float minDistanceSqr,
            BotWanderSettings settings,
            bool onlyUnvisited,
            bool requireMinimumDistance)
        {
            int bestNodeId = -1;
            float bestScore = float.PositiveInfinity;
            for (int nodeId = 0; nodeId < _graph.NodeCount; nodeId++)
            {
                if (nodeId == startNodeId)
                {
                    continue;
                }

                int visitCount = _nodeVisitCounts[nodeId];
                if (onlyUnvisited && visitCount > 0)
                {
                    continue;
                }

                Vector3 delta = _graph.GetNodePosition(nodeId) - startPosition;
                delta.y = 0f;
                bool isFarEnough = delta.sqrMagnitude >= minDistanceSqr;
                if (requireMinimumDistance && !isFarEnough)
                {
                    continue;
                }

                float score = visitCount * settings.explorationVisitedPenalty;
                if (!isFarEnough)
                {
                    score += settings.explorationVisitedPenalty;
                }

                score += Random.value * Mathf.Max(1f, settings.destinationPickAttempts);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNodeId = nodeId;
                }
            }

            return bestNodeId;
        }

        private void FollowPath(BotWanderSettings settings, float now)
        {
            if (_path.Count == 0)
            {
                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
                return;
            }

            Vector3 position = GetMovePosition();
            if (_pathIndex >= _path.Count)
            {
                if (TryFollowExplicitTargetPosition(position, settings))
                {
                    return;
                }

                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
                return;
            }

            AdvancePathIndex(position, settings);

            if (_pathIndex >= _path.Count)
            {
                if (TryFollowExplicitTargetPosition(position, settings))
                {
                    return;
                }

                ClearPath();
                Repath(settings, now);
                return;
            }

            Vector3 waypointPosition = _graph.GetNodePosition(_path[_pathIndex]);
            Vector3 toWaypoint = waypointPosition - position;
            toWaypoint.y = 0f;
            float waypointDistance = toWaypoint.magnitude;
            if (waypointDistance <= 0.001f)
            {
                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
                return;
            }

            Vector3 desiredDirection = toWaypoint / waypointDistance;
            RefreshProximityAvoidance(position, settings, now);

            Vector3 dynamicAvoidance = CalculateDynamicAvoidance(position, settings);
            Vector3 adjustedDirection = desiredDirection
                                      + dynamicAvoidance * settings.dynamicAvoidanceWeight
                                      + _proximityAvoidance * settings.proximityAvoidanceWeight;
            adjustedDirection.y = 0f;
            if (adjustedDirection.sqrMagnitude > 0.0001f)
            {
                desiredDirection = adjustedDirection.normalized;
            }

            SetDesiredTravelDirection(desiredDirection);
            ApplyDirectionInput(desiredDirection, waypointDistance, settings);
        }

        private bool TryFollowExplicitTargetPosition(Vector3 position, BotWanderSettings settings)
        {
            if (!_hasExplicitTarget)
            {
                return false;
            }

            Vector3 toTarget = _targetPosition - position;
            toTarget.y = 0f;
            float targetDistance = toTarget.magnitude;
            if (targetDistance <= Mathf.Max(settings.waypointReachDistance, 0.05f))
            {
                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
                return true;
            }

            Vector3 desiredDirection = toTarget / targetDistance;
            RefreshProximityAvoidance(position, settings, Time.time);
            Vector3 dynamicAvoidance = CalculateDynamicAvoidance(position, settings);
            Vector3 adjustedDirection = desiredDirection
                                      + dynamicAvoidance * settings.dynamicAvoidanceWeight
                                      + _proximityAvoidance * settings.proximityAvoidanceWeight;
            adjustedDirection.y = 0f;
            if (adjustedDirection.sqrMagnitude > 0.0001f)
            {
                desiredDirection = adjustedDirection.normalized;
            }

            SetDesiredTravelDirection(desiredDirection);
            ApplyDirectionInput(desiredDirection, targetDistance, settings);
            return true;
        }

        private void AdvancePathIndex(Vector3 position, BotWanderSettings settings)
        {
            float reachDistanceSqr = settings.waypointReachDistance * settings.waypointReachDistance;
            float passDistance = Mathf.Max(settings.waypointReachDistance, settings.waypointPassDistance);
            float passDistanceSqr = passDistance * passDistance;

            while (_pathIndex < _path.Count)
            {
                Vector3 waypointPosition = _graph.GetNodePosition(_path[_pathIndex]);
                if (!ShouldAdvanceWaypoint(position, waypointPosition, reachDistanceSqr, passDistanceSqr, settings))
                {
                    return;
                }

                _pathIndex++;
                _isPivotTurning = false;
            }
        }

        private bool ShouldAdvanceWaypoint(
            Vector3 position,
            Vector3 waypointPosition,
            float reachDistanceSqr,
            float passDistanceSqr,
            BotWanderSettings settings)
        {
            Vector3 delta = waypointPosition - position;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr <= reachDistanceSqr)
            {
                return true;
            }

            if (distanceSqr > passDistanceSqr)
            {
                return false;
            }

            if (HasMovedPastPathSegment(position, waypointPosition))
            {
                return true;
            }

            Transform moveTransform = GetMoveTransform();
            if (moveTransform == null || delta.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 forward = moveTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float angle = Mathf.Abs(Vector3.SignedAngle(forward.normalized, delta.normalized, Vector3.up));
            return angle >= settings.waypointPassedAngle;
        }

        private bool HasMovedPastPathSegment(Vector3 position, Vector3 waypointPosition)
        {
            if (_pathIndex <= 0 || _pathIndex >= _path.Count)
            {
                return false;
            }

            Vector3 previousPosition = _graph.GetNodePosition(_path[_pathIndex - 1]);
            Vector3 segment = waypointPosition - previousPosition;
            segment.y = 0f;
            float segmentSqr = segment.sqrMagnitude;
            if (segmentSqr <= 0.0001f)
            {
                return false;
            }

            Vector3 fromPrevious = position - previousPosition;
            fromPrevious.y = 0f;
            return Vector3.Dot(fromPrevious, segment) > segmentSqr;
        }

        private Vector3 CalculateDynamicAvoidance(Vector3 position, BotWanderSettings settings)
        {
            if (_room == null || settings.dynamicAvoidanceRadius <= 0f || settings.dynamicAvoidanceWeight <= 0f)
            {
                return Vector3.zero;
            }

            List<LobbyPlayer> players = _room.GetPlayers();
            if (players == null || players.Count <= 1)
            {
                return Vector3.zero;
            }

            float radiusSqr = settings.dynamicAvoidanceRadius * settings.dynamicAvoidanceRadius;
            Vector3 avoidance = Vector3.zero;
            int count = 0;

            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayer player = players[i];
                if (player == null || player.playerRoot == null || player.playerRoot == _vehicleRoot)
                {
                    continue;
                }

                Vector3 otherPosition = GetRootPosition(player.playerRoot);
                Vector3 away = position - otherPosition;
                away.y = 0f;

                float distanceSqr = away.sqrMagnitude;
                if (distanceSqr <= 0.0001f || distanceSqr > radiusSqr)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(distanceSqr);
                float strength = 1f - Mathf.Clamp01(distance / settings.dynamicAvoidanceRadius);
                avoidance += away / distance * strength;
                count++;
            }

            if (count == 0 || avoidance.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return avoidance.normalized;
        }

        private bool TryStartUnstick(BotWanderSettings settings, float now)
        {
            _nextStuckCheckTime = now + settings.stuckCheckInterval;

            if (!_hasDesiredTravelDirection)
            {
                ResetProgressTracking();
                return false;
            }

            Vector3 position = GetMovePosition();
            Vector3 moved = position - _lastProgressPosition;
            moved.y = 0f;
            float currentYaw = GetMoveYaw();
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(_lastProgressYaw, currentYaw));
            _lastProgressPosition = position;
            _lastProgressYaw = currentYaw;

            float stuckDistanceSqr = settings.stuckDistance * settings.stuckDistance;
            if (moved.sqrMagnitude >= stuckDistanceSqr
                || HasTurnProgress(yawDelta, MinimumPivotProgressDegrees))
            {
                return false;
            }

            _isUnsticking = true;
            _unstickUntilTime = now + settings.unstickDuration;
            _turnBias = -_turnBias;
            ClearPath();
            SetDesiredTravelDirection(BuildInputTravelDirection(settings.unstickReverseInput, _turnBias * settings.unstickTurnInput));
            ApplyInput(settings.unstickReverseInput, _turnBias * settings.unstickTurnInput);
            return true;
        }

        private static bool HasTurnProgress(float yawDelta, float minimumProgressDegrees)
        {
            return yawDelta >= Mathf.Max(0f, minimumProgressDegrees);
        }

        private void ResetProgressTracking()
        {
            _lastProgressPosition = GetMovePosition();
            _lastProgressYaw = GetMoveYaw();
            _nextStuckCheckTime = Time.time + Mathf.Max(0.1f, ServerSettings.GetBotWander().stuckCheckInterval);
            _isUnsticking = false;
        }

        private float GetMoveYaw()
        {
            Transform moveTransform = GetMoveTransform();
            return moveTransform != null ? moveTransform.eulerAngles.y : 0f;
        }

        private void ApplyDirectionInput(Vector3 desiredDirection, float waypointDistance, BotWanderSettings settings)
        {
            Transform moveTransform = GetMoveTransform();
            if (moveTransform == null)
            {
                ApplyInput(0f, 0f);
                return;
            }

            float angle = Vector3.SignedAngle(moveTransform.forward, desiredDirection, Vector3.up);
            float absAngle = Mathf.Abs(angle);
            float desiredTurn = ResolveSteeringTurn(
                angle,
                settings.turnFullInputAngle,
                settings.turnDeadZoneAngle);
            float turn = SmoothSteeringTurn(
                _smoothedTurnInput,
                desiredTurn,
                settings.turnInputSmoothing,
                GetSteeringDeltaTime(settings));
            _smoothedTurnInput = turn;
            float forward = settings.forwardInput;

            if (_isPivotTurning)
            {
                if (absAngle <= settings.turnInPlaceExitAngle)
                {
                    _isPivotTurning = false;
                }
            }
            else if (absAngle >= settings.turnInPlaceEnterAngle)
            {
                _isPivotTurning = true;
            }

            if (_isPivotTurning || absAngle >= settings.stopTurnAngle)
            {
                forward = 0f;
            }
            else if (absAngle >= settings.slowTurnAngle)
            {
                forward = settings.slowForwardInput;
            }

            if (forward > 0f
                && settings.waypointApproachSlowDistance > settings.waypointReachDistance
                && waypointDistance <= settings.waypointApproachSlowDistance)
            {
                float span = settings.waypointApproachSlowDistance - settings.waypointReachDistance;
                float t = Mathf.Clamp01((waypointDistance - settings.waypointReachDistance) / span);
                float approachForward = Mathf.Lerp(settings.slowForwardInput, settings.forwardInput, t);
                forward = Mathf.Min(forward, approachForward);
            }

            ApplyInput(forward, turn);
        }

        private float GetSteeringDeltaTime(BotWanderSettings settings)
        {
            float now = Time.time;
            float deltaTime = _lastSteeringSampleTime > 0f
                ? now - _lastSteeringSampleTime
                : settings.thinkInterval;
            _lastSteeringSampleTime = now;
            return Mathf.Clamp(deltaTime, 0.01f, 0.5f);
        }

        private static float ResolveSteeringTurn(float angle, float fullInputAngle, float deadZoneAngle)
        {
            float absoluteAngle = Mathf.Abs(angle);
            if (absoluteAngle <= Mathf.Max(0f, deadZoneAngle))
            {
                return 0f;
            }

            return Mathf.Clamp(angle / Mathf.Max(1f, fullInputAngle), -1f, 1f);
        }

        private static float SmoothSteeringTurn(
            float currentInput,
            float desiredInput,
            float smoothingSpeed,
            float deltaTime)
        {
            if (smoothingSpeed <= 0f || deltaTime <= 0f)
            {
                return desiredInput;
            }

            float blend = 1f - Mathf.Exp(-smoothingSpeed * deltaTime);
            float smoothedInput = Mathf.Lerp(currentInput, desiredInput, blend);
            return Mathf.Abs(smoothedInput) < 0.025f ? 0f : smoothedInput;
        }

        private void ResetSteeringSmoothing()
        {
            _smoothedTurnInput = 0f;
            _lastSteeringSampleTime = 0f;
        }

        private void TickFallbackWander(BotWanderSettings settings, float now)
        {
            RefreshProximityAvoidance(GetMovePosition(), settings, now);
            if (_proximityAvoidance.sqrMagnitude > 0.0001f)
            {
                Transform moveTransform = GetMoveTransform();
                if (moveTransform != null)
                {
                    Vector3 desiredDirection = moveTransform.forward + _proximityAvoidance * settings.proximityAvoidanceWeight;
                    desiredDirection.y = 0f;
                    if (desiredDirection.sqrMagnitude > 0.0001f)
                    {
                        desiredDirection.Normalize();
                        SetDesiredTravelDirection(desiredDirection);
                        ApplyDirectionInput(desiredDirection, settings.proximityRayDistance, settings);
                        _nextFallbackInputChangeTime = now + Mathf.Min(0.35f, settings.proximityScanInterval);
                        return;
                    }
                }
            }

            if (now < _nextFallbackInputChangeTime)
            {
                return;
            }

            float forward = settings.forwardInput;
            if (Random.value < settings.idleChance)
            {
                forward = 0f;
            }

            float turn = Random.Range(-settings.maxGentleTurnInput, settings.maxGentleTurnInput);
            if (Random.value < settings.strongTurnChance)
            {
                turn = (Random.value < 0.5f ? -1f : 1f) * settings.strongTurnInput;
            }

            SetDesiredTravelDirection(BuildInputTravelDirection(forward, turn));
            ApplyInput(forward, turn);
            _nextFallbackInputChangeTime = now + Random.Range(settings.minMoveDuration, settings.maxMoveDuration);
        }

        private bool TryApplyProximityEscape(BotWanderSettings settings)
        {
            if (_proximityAvoidance.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Transform moveTransform = GetMoveTransform();
            if (moveTransform == null)
            {
                return false;
            }

            Vector3 desiredDirection = moveTransform.forward
                                     + _proximityAvoidance * settings.proximityAvoidanceWeight;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            desiredDirection.Normalize();
            SetDesiredTravelDirection(desiredDirection);
            ApplyDirectionInput(desiredDirection, settings.proximityRayDistance, settings);
            return true;
        }

        private void ApplyInput(float forward, float turn)
        {
            if (_inputReceiver == null)
            {
                return;
            }

            forward = ClampInput(forward);
            turn = ClampInput(turn);
            _inputReceiver.ApplyBotInput(forward, turn);
        }

        private Vector3 GetMovePosition()
        {
            return GetRootPosition(_vehicleRoot);
        }

        private static Vector3 GetRootPosition(VehicleRoot root)
        {
            if (root != null && root.objectMover != null)
            {
                return root.objectMover.transform.position;
            }

            if (root != null)
            {
                return root.transform.position;
            }

            return Vector3.zero;
        }

        private Transform GetMoveTransform()
        {
            if (_vehicleRoot != null && _vehicleRoot.objectMover != null)
            {
                return _vehicleRoot.objectMover.transform;
            }

            return _vehicleRoot != null ? _vehicleRoot.transform : transform;
        }

        private Vector3 BuildInputTravelDirection(float forward, float turn)
        {
            Transform moveTransform = GetMoveTransform();
            if (moveTransform == null)
            {
                return Vector3.zero;
            }

            Vector3 direction = Vector3.zero;
            if (Mathf.Abs(forward) > 0.025f)
            {
                direction += moveTransform.forward * Mathf.Sign(forward);
            }

            if (Mathf.Abs(turn) > 0.025f)
            {
                direction += moveTransform.right * Mathf.Sign(turn) * 0.35f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = moveTransform.forward;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private void SetDesiredTravelDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                ClearDesiredTravelDirection();
                return;
            }

            _desiredTravelDirection = direction.normalized;
            _lastDesiredTravelDirectionTime = Time.time;
            _hasDesiredTravelDirection = true;
        }

        private void ClearDesiredTravelDirection()
        {
            _desiredTravelDirection = Vector3.zero;
            _lastDesiredTravelDirectionTime = 0f;
            _hasDesiredTravelDirection = false;
        }

        private float ClampInput(float value)
        {
            value = Mathf.Clamp(value, -1f, 1f);
            if (Mathf.Abs(value) < 0.025f)
            {
                return 0f;
            }

            return value;
        }

        private void ClearPath()
        {
            _path.Clear();
            _pathIndex = 0;
            _destinationNodeId = -1;
            _isPivotTurning = false;
        }

        private void InitializeExplorationMemory()
        {
            _lastVisitedNodeId = -1;
            if (_graph == null || !_graph.IsBuilt || _graph.NodeCount <= 0)
            {
                _nodeVisitCounts = System.Array.Empty<int>();
                return;
            }

            _nodeVisitCounts = new int[_graph.NodeCount];
            TrackVisitedNode();
        }

        private void TrackVisitedNode()
        {
            if (_graph == null || !_graph.IsBuilt || _nodeVisitCounts.Length != _graph.NodeCount)
            {
                return;
            }

            int nodeId = _graph.FindNearestNode(GetMovePosition());
            if (nodeId < 0 || nodeId == _lastVisitedNodeId)
            {
                return;
            }

            _nodeVisitCounts[nodeId]++;
            _lastVisitedNodeId = nodeId;
        }

        private void RefreshProximityAvoidance(Vector3 position, BotWanderSettings settings, float now)
        {
            if (now < _nextProximityScanTime)
            {
                return;
            }

            _nextProximityScanTime = now + Mathf.Max(0.1f, settings.proximityScanInterval);
            _proximityAvoidance = Vector3.zero;
            if (_vehicleRoot == null
                || settings.proximityRayDistance <= 0f
                || settings.proximityAvoidanceWeight <= 0f)
            {
                return;
            }

            Transform moveTransform = GetMoveTransform();
            if (moveTransform == null)
            {
                return;
            }

            Vector3 forward = moveTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            Vector3 totalAvoidance = Vector3.zero;
            LayerMask proximityMask = GetProximityMask(settings.proximityMask);
            for (int directionIndex = 0; directionIndex < ProximityDirectionCount; directionIndex++)
            {
                Vector3 direction = moveTransform.TransformDirection(ProximityDirections[directionIndex]);
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                direction.Normalize();
                for (int heightIndex = 0; heightIndex < ProximityVerticalRayCount; heightIndex++)
                {
                    float heightOffset = settings.proximityRayHeight
                                         + (heightIndex - 1) * settings.proximityRayVerticalSpacing;
                    Vector3 origin = position + Vector3.up * Mathf.Max(0.05f, heightOffset);
                    if (!TryGetNearestRobotAlongRay(
                            origin,
                            direction,
                            settings.proximityRayDistance,
                            proximityMask,
                            out VehicleRoot otherRoot,
                            out Vector3 obstaclePosition,
                            out float hitDistance))
                    {
                        continue;
                    }

                    Vector3 away = position - obstaclePosition;
                    away.y = 0f;
                    if (away.sqrMagnitude <= 0.0001f)
                    {
                        away = -direction;
                    }
                    else
                    {
                        away.Normalize();
                    }

                    float strength = 1f - Mathf.Clamp01(hitDistance / settings.proximityRayDistance);
                    float forwardDot = Vector3.Dot(forward, direction);
                    if (forwardDot > 0.25f)
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, direction);
                        side.y = 0f;
                        if (side.sqrMagnitude > 0.0001f)
                        {
                            side.Normalize();
                            totalAvoidance += (side * _turnBias + away * 0.35f) * strength * (0.75f + forwardDot);
                        }
                    }
                    else
                    {
                        totalAvoidance += away * strength;
                    }
                }
            }

            totalAvoidance.y = 0f;
            if (totalAvoidance.sqrMagnitude > 0.0001f)
            {
                _proximityAvoidance = totalAvoidance.normalized;
            }
        }

        private bool TryGetNearestRobotAlongRay(
            Vector3 origin,
            Vector3 direction,
            float distance,
            LayerMask mask,
            out VehicleRoot otherRoot,
            out Vector3 obstaclePosition,
            out float hitDistance)
        {
            otherRoot = null;
            obstaclePosition = default;
            hitDistance = float.PositiveInfinity;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _proximityRaycastBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float nearestColliderDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _proximityRaycastBuffer[i].collider;
                if (collider == null || BotCombatUtility.IsUnderRoot(collider.transform, _vehicleRoot.transform))
                {
                    continue;
                }

                float currentDistance = _proximityRaycastBuffer[i].distance;
                if (currentDistance < nearestColliderDistance)
                {
                    nearestColliderDistance = currentDistance;
                }

                if (VehicleColliderRegistry.TryGetRoot(collider, out VehicleRoot candidateRoot))
                {
                    if (candidateRoot == null || candidateRoot == _vehicleRoot)
                    {
                        continue;
                    }

                    Vector3 candidatePosition = BotCombatUtility.GetMovePosition(candidateRoot);
                    if (currentDistance < hitDistance)
                    {
                        otherRoot = candidateRoot;
                        obstaclePosition = candidatePosition;
                        hitDistance = currentDistance;
                    }

                    continue;
                }

                if (!IsDeathDebrisCollider(collider))
                {
                    continue;
                }

                Vector3 debrisPosition = _proximityRaycastBuffer[i].point;
                if ((debrisPosition - origin).sqrMagnitude <= 0.0001f)
                {
                    debrisPosition = collider.bounds.center;
                }

                if (currentDistance < hitDistance)
                {
                    otherRoot = null;
                    obstaclePosition = debrisPosition;
                    hitDistance = currentDistance;
                }
            }

            if (hitDistance == float.PositiveInfinity || hitDistance > nearestColliderDistance + 0.05f)
            {
                otherRoot = null;
                obstaclePosition = default;
                hitDistance = 0f;
                return false;
            }

            return true;
        }

        private static LayerMask GetProximityMask(LayerMask configuredMask)
        {
            if (_deathDebrisLayer == int.MinValue)
            {
                _deathDebrisLayer = LayerMask.NameToLayer(DeathDebrisLayerName);
            }

            if (_deathDebrisLayer < 0)
            {
                return configuredMask;
            }

            configuredMask.value |= 1 << _deathDebrisLayer;
            return configuredMask;
        }

        private static bool IsDeathDebrisCollider(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (_deathDebrisLayer == int.MinValue)
            {
                _deathDebrisLayer = LayerMask.NameToLayer(DeathDebrisLayerName);
            }

            return _deathDebrisLayer >= 0
                   && collider.gameObject.layer == _deathDebrisLayer
                   && collider.attachedRigidbody != null
                   && collider.transform.parent == null;
        }

        private void OnDrawGizmosSelected()
        {
            using (ProfileScope.Measure("Gizmos.BotNavigator.OnDrawGizmosSelected", DiagnosticsCategories.Editor))
            {
                if (!drawDebugGizmos || _graph == null || _path.Count == 0)
                {
                    return;
                }

                Gizmos.color = Color.blue;
                for (int i = 0; i < _path.Count - 1; i++)
                {
                    Gizmos.DrawLine(_graph.GetNodePosition(_path[i]), _graph.GetNodePosition(_path[i + 1]));
                }

                if (_pathIndex >= 0 && _pathIndex < _path.Count)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(_graph.GetNodePosition(_path[_pathIndex]), 0.6f);
                }

                if (_destinationNodeId >= 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(_graph.GetNodePosition(_destinationNodeId), 0.75f);
                }
            }
        }
    }
}
