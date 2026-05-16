using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class BotNavigator : MonoBehaviour
    {
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
        private bool _hasExplicitTarget;
        private bool _isUnsticking;
        private bool _isInitialized;

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

        public void Stop()
        {
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

            if (_vehicleRoot.health != null && _vehicleRoot.health.IsDead)
            {
                Stop();
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
                Repath(settings, now);
            }

            FollowPath(settings, now);
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
                return;
            }

            if (!_pathfinder.FindPath(startNodeId, goalNodeId, _path))
            {
                ClearPath();
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
            Vector3 desiredDirection = waypointPosition - position;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                ApplyInput(0f, 0f);
                return;
            }

            desiredDirection.Normalize();

            Vector3 avoidance = CalculateDynamicAvoidance(position, settings);
            if (avoidance.sqrMagnitude > 0.0001f)
            {
                desiredDirection = (desiredDirection + avoidance * settings.dynamicAvoidanceWeight).normalized;
            }

            ApplyDirectionInput(desiredDirection, settings);
        }

        private void AdvancePathIndex(Vector3 position, BotWanderSettings settings)
        {
            float reachDistanceSqr = settings.waypointReachDistance * settings.waypointReachDistance;

            while (_pathIndex < _path.Count)
            {
                Vector3 delta = _graph.GetNodePosition(_path[_pathIndex]) - position;
                delta.y = 0f;
                if (delta.sqrMagnitude > reachDistanceSqr)
                {
                    return;
                }

                _pathIndex++;
            }
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
            ApplyInput(settings.unstickReverseInput, _turnBias * settings.unstickTurnInput);
            return true;
        }

        private void ApplyDirectionInput(Vector3 desiredDirection, BotWanderSettings settings)
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

            if (absAngle >= settings.stopTurnAngle)
            {
                forward = 0f;
            }
            else if (absAngle >= settings.slowTurnAngle)
            {
                forward = settings.slowForwardInput;
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
        }

        private void OnDrawGizmosSelected()
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
