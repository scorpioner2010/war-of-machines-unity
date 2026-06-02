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

        [SerializeField] private Transform target;
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly List<int> _path = new List<int>(32);
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
        private float _nextTickTime;
        private float _nextRepathTime;
        private float _nextStuckCheckTime;
        private float _unstickUntilTime;
        private float _nextFallbackInputChangeTime;
        private float _turnBias = 1f;
        private Vector3 _desiredTravelDirection;
        private float _lastDesiredTravelDirectionTime;
        private bool _hasExplicitTarget;
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
            _lastProgressPosition = GetMovePosition();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _hasExplicitTarget = target != null;
            ClearPath();
        }

        public void SetTargetPosition(Vector3 position)
        {
            target = null;
            _targetPosition = position;
            _hasExplicitTarget = true;
            ClearPath();
        }

        public void SetMovementSuppressed(bool suppressed)
        {
            if (_movementSuppressed == suppressed)
            {
                return;
            }

            _movementSuppressed = suppressed;
            if (_movementSuppressed)
            {
                _isPivotTurning = false;
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

                if (_movementSuppressed)
                {
                    ApplyInput(0f, 0f);
                    return;
                }

                float now = Time.time;
                if (now < _nextTickTime)
                {
                    return;
                }

                BotWanderSettings settings = ServerSettings.GetBotWander();
                _nextTickTime = now + settings.thinkInterval;

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
            if (startNodeId < 0)
            {
                return fallbackNodeId;
            }

            Vector3 startPosition = _graph.GetNodePosition(startNodeId);
            float minDistanceSqr = settings.minDestinationDistance * settings.minDestinationDistance;

            for (int i = 0; i < settings.destinationPickAttempts; i++)
            {
                int nodeId = _graph.GetRandomNodeId();
                if (nodeId < 0)
                {
                    return fallbackNodeId;
                }

                Vector3 delta = _graph.GetNodePosition(nodeId) - startPosition;
                delta.y = 0f;
                if (delta.sqrMagnitude >= minDistanceSqr)
                {
                    return nodeId;
                }

                fallbackNodeId = nodeId;
            }

            return fallbackNodeId;
        }

        private void FollowPath(BotWanderSettings settings, float now)
        {
            if (_path.Count == 0 || _pathIndex >= _path.Count)
            {
                ClearDesiredTravelDirection();
                ApplyInput(0f, 0f);
                return;
            }

            Vector3 position = GetMovePosition();
            AdvancePathIndex(position, settings);

            if (_pathIndex >= _path.Count)
            {
                _hasExplicitTarget = target != null;
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

            Vector3 avoidance = CalculateDynamicAvoidance(position, settings);
            if (avoidance.sqrMagnitude > 0.0001f)
            {
                Vector3 adjustedDirection = desiredDirection + avoidance * settings.dynamicAvoidanceWeight;
                adjustedDirection.y = 0f;
                if (adjustedDirection.sqrMagnitude > 0.0001f)
                {
                    desiredDirection = adjustedDirection.normalized;
                }
            }

            SetDesiredTravelDirection(desiredDirection);
            ApplyDirectionInput(desiredDirection, waypointDistance, settings);
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

                if (player.playerRoot.health != null && player.playerRoot.health.IsDead)
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

            Vector3 position = GetMovePosition();
            Vector3 moved = position - _lastProgressPosition;
            moved.y = 0f;
            _lastProgressPosition = position;

            float stuckDistanceSqr = settings.stuckDistance * settings.stuckDistance;
            if (moved.sqrMagnitude >= stuckDistanceSqr)
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
            float turn = Mathf.Clamp(angle / settings.turnFullInputAngle, -1f, 1f);
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

        private void TickFallbackWander(BotWanderSettings settings, float now)
        {
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
