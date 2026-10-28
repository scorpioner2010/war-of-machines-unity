using System;
using System.Collections.Generic;
using FishNet.Connection;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public enum MapVehicleVisibilityRelation : byte
    {
        Hidden = 0,
        Ally = 1,
        Enemy = 2,
        Destroyed = 3,
        EnemyLastKnown = 4
    }

    public interface IMatchVisibilityUpdateSink
    {
        void SendMapVisibility(
            NetworkConnection target,
            int version,
            int count,
            int[] objectIds,
            byte[] relations,
            Vector3[] positions,
            float[] yaws,
            float[] remainingVisibilitySeconds);
    }

    public sealed class MatchVisibilityService
    {
        private const int RaycastBufferSize = 96;
        private const int ArmorLayer = 6;
        private const int ChassisLayer = 7;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];

        private readonly List<VisibilityParticipant> _participants = new List<VisibilityParticipant>(16);
        private readonly List<MapVisibilityEntry> _entries = new List<MapVisibilityEntry>(16);
        private readonly Dictionary<long, CachedLineOfSight> _lineOfSightCache = new Dictionary<long, CachedLineOfSight>(64);
        private readonly List<long> _expiredLineOfSightKeys = new List<long>(64);
        private readonly TeamVisibilityState _teamA = new TeamVisibilityState(MatchTeam.TeamA);
        private readonly TeamVisibilityState _teamB = new TeamVisibilityState(MatchTeam.TeamB);
        private readonly MatchVisibilitySnapshotRecipients _snapshotRecipients =
            new MatchVisibilitySnapshotRecipients();

        private ServerRoom _room;
        private float _nextTickTime;
        private float _lastStateRefreshTime;
        private int _version;
        private int _spottingPairCursor;

        public bool IsRunning => _room != null;

        public void Start(ServerRoom room)
        {
            _room = room;
            _nextTickTime = 0f;
            _lastStateRefreshTime = float.NegativeInfinity;
            _version = 0;
            _teamA.Clear();
            _teamB.Clear();
            _participants.Clear();
            _entries.Clear();
            _lineOfSightCache.Clear();
            _expiredLineOfSightKeys.Clear();
            _spottingPairCursor = 0;
        }

        public void Stop()
        {
            _room = null;
            _nextTickTime = 0f;
            _lastStateRefreshTime = float.NegativeInfinity;
            _teamA.Clear();
            _teamB.Clear();
            _participants.Clear();
            _entries.Clear();
            _lineOfSightCache.Clear();
            _expiredLineOfSightKeys.Clear();
            _spottingPairCursor = 0;
        }

        public void Tick(float now, IMatchVisibilityUpdateSink updateSink)
        {
            if (_room == null || updateSink == null)
            {
                return;
            }

            MatchVisibilityGlobalSettings settings = ServerSettings.GetMatchVisibility();
            if (!settings.enabled)
            {
                SendEmptySnapshots(updateSink);
                _nextTickTime = now + Mathf.Max(0.05f, settings.tickInterval);
                return;
            }

            if (now < _nextTickTime)
            {
                return;
            }

            _nextTickTime = now + Mathf.Max(0.05f, settings.tickInterval);
            RefreshState(now, settings);

            using (ProfileScope.Measure("Server.Visibility.SendSnapshots", DiagnosticsCategories.Network))
            {
                SendSnapshots(updateSink);
            }
        }

        public void RefreshForBotQueries(float now)
        {
            if (_room == null)
            {
                return;
            }

            MatchVisibilityGlobalSettings settings = ServerSettings.GetMatchVisibility();
            float interval = Mathf.Max(0.05f, settings.tickInterval);
            if (now - _lastStateRefreshTime < interval)
            {
                return;
            }

            if (!settings.enabled)
            {
                ClearVisibilityState();
                _lastStateRefreshTime = now;
                return;
            }

            RefreshState(now, settings);
        }

        private void RefreshState(float now, MatchVisibilityGlobalSettings settings)
        {
            _version++;

            using (ProfileScope.Measure("Server.Visibility.RebuildParticipants", DiagnosticsCategories.Server))
            {
                RebuildParticipants(settings);
            }

            using (ProfileScope.Measure("Server.Visibility.UpdateTeamSpotting", DiagnosticsCategories.Server))
            {
                UpdateTeamSpotting(now, settings);
            }

            using (ProfileScope.Measure("Server.Visibility.BuildSnapshots", DiagnosticsCategories.Server))
            {
                BuildSnapshotForTeam(_teamA, now, settings);
                BuildSnapshotForTeam(_teamB, now, settings);
            }

            _lastStateRefreshTime = now;
        }

        private void ClearVisibilityState()
        {
            _teamA.Clear();
            _teamB.Clear();
            _participants.Clear();
            _entries.Clear();
            _lineOfSightCache.Clear();
            _expiredLineOfSightKeys.Clear();
        }

        public bool IsVisibleForTeam(MatchTeam viewerTeam, VehicleRoot targetRoot)
        {
            if (!MatchTeamUtility.IsAssigned(viewerTeam) || targetRoot == null || targetRoot.networkObject == null)
            {
                return false;
            }

            if (targetRoot.characterInit != null
                && MatchTeamUtility.AreSameAssignedTeam(viewerTeam, targetRoot.characterInit.Team.Value))
            {
                return true;
            }

            TeamVisibilityState teamState = GetTeamState(viewerTeam);
            if (teamState == null)
            {
                return false;
            }

            return teamState.IsVisible(targetRoot.networkObject.ObjectId, Time.time);
        }

        public bool TryGetVisibleEnemyFor(VehicleRoot viewerRoot, VehicleRoot targetRoot, out MatchVisibleEnemy visibleEnemy)
        {
            visibleEnemy = default;
            if (viewerRoot == null || viewerRoot.characterInit == null || targetRoot == null)
            {
                return false;
            }

            MatchTeam viewerTeam = viewerRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(viewerTeam))
            {
                return false;
            }

            TeamVisibilityState teamState = GetTeamState(viewerTeam);
            if (teamState == null)
            {
                return false;
            }

            float now = Time.time;
            for (int i = 0; i < _participants.Count; i++)
            {
                VisibilityParticipant participant = _participants[i];
                if (participant.Root != targetRoot
                    || participant.Root == viewerRoot
                    || participant.IsDead
                    || participant.Player == null
                    || participant.Player.leftBattle
                    || !MatchTeamUtility.AreOpposingAssignedTeams(viewerTeam, participant.Team))
                {
                    continue;
                }

                if (teamState.TryGetEnemyVisibility(
                        participant.ObjectId,
                        now,
                        participant.Position,
                        participant.Yaw,
                        out Vector3 position,
                        out float yaw))
                {
                    visibleEnemy = new MatchVisibleEnemy
                    {
                        Root = participant.Root,
                        Position = position,
                        Yaw = yaw,
                        IsDirectlySpotted = teamState.IsDirectlySpotted(participant.ObjectId)
                    };
                    return true;
                }
            }

            return false;
        }

        public void FillVisibleEnemiesFor(VehicleRoot viewerRoot, List<VehicleRoot> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (viewerRoot == null || viewerRoot.characterInit == null)
            {
                return;
            }

            MatchTeam viewerTeam = viewerRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(viewerTeam))
            {
                return;
            }

            TeamVisibilityState teamState = GetTeamState(viewerTeam);
            if (teamState == null)
            {
                return;
            }

            float now = Time.time;
            for (int i = 0; i < _participants.Count; i++)
            {
                VisibilityParticipant participant = _participants[i];
                if (participant.Root == null
                    || participant.Root == viewerRoot
                    || participant.IsDead
                    || participant.Player == null
                    || participant.Player.leftBattle
                    || !MatchTeamUtility.AreOpposingAssignedTeams(viewerTeam, participant.Team))
                {
                    continue;
                }

                if (teamState.IsVisible(participant.ObjectId, now))
                {
                    results.Add(participant.Root);
                }
            }
        }

        public void FillVisibleEnemiesFor(VehicleRoot viewerRoot, List<MatchVisibleEnemy> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (viewerRoot == null || viewerRoot.characterInit == null)
            {
                return;
            }

            MatchTeam viewerTeam = viewerRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(viewerTeam))
            {
                return;
            }

            TeamVisibilityState teamState = GetTeamState(viewerTeam);
            if (teamState == null)
            {
                return;
            }

            float now = Time.time;
            for (int i = 0; i < _participants.Count; i++)
            {
                VisibilityParticipant participant = _participants[i];
                if (participant.Root == null
                    || participant.Root == viewerRoot
                    || participant.IsDead
                    || participant.Player == null
                    || participant.Player.leftBattle
                    || !MatchTeamUtility.AreOpposingAssignedTeams(viewerTeam, participant.Team))
                {
                    continue;
                }

                if (!teamState.TryGetEnemyVisibility(
                        participant.ObjectId,
                        now,
                        participant.Position,
                        participant.Yaw,
                        out Vector3 position,
                        out float yaw))
                {
                    continue;
                }

                results.Add(new MatchVisibleEnemy
                {
                    Root = participant.Root,
                    Position = position,
                    Yaw = yaw,
                    IsDirectlySpotted = teamState.IsDirectlySpotted(participant.ObjectId)
                });
            }
        }

        private void RebuildParticipants(MatchVisibilityGlobalSettings settings)
        {
            _participants.Clear();
            if (_room == null)
            {
                return;
            }

            List<Player> players = _room.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null || player.leftBattle || player.playerRoot == null)
                {
                    continue;
                }

                VehicleRoot root = player.playerRoot;
                if (root.IsMenu || root.networkObject == null || !root.networkObject.IsSpawned)
                {
                    continue;
                }

                MatchTeam team = player.team;
                if (!MatchTeamUtility.IsAssigned(team) && root.characterInit != null)
                {
                    team = root.characterInit.Team.Value;
                }

                if (!MatchTeamUtility.IsAssigned(team))
                {
                    continue;
                }

                Transform trackedTransform = GetTrackedTransform(root);
                VisibilityParticipant participant = new VisibilityParticipant
                {
                    Player = player,
                    Connection = player.Connection,
                    Root = root,
                    Team = team,
                    IsDead = root.health != null && root.health.IsDead,
                    ObjectId = root.networkObject.ObjectId,
                    Position = trackedTransform.position,
                    Yaw = trackedTransform.eulerAngles.y,
                    ViewRange = ResolveViewRange(root, settings)
                };

                _participants.Add(participant);
            }
        }

        private void UpdateTeamSpotting(float now, MatchVisibilityGlobalSettings settings)
        {
            float memorySeconds = Mathf.Max(0f, settings.spottedMemorySeconds);
            _teamA.PruneSpotterLinks(_participants, now, memorySeconds);
            _teamB.PruneSpotterLinks(_participants, now, memorySeconds);
            _teamA.BeginFrame();
            _teamB.BeginFrame();

            int lineOfSightChecksRemaining = Mathf.Max(1, settings.maxLineOfSightChecksPerTick);
            int maintenanceBudget = Mathf.Max(1, lineOfSightChecksRemaining / 3);
            int maintenanceChecksRemaining = maintenanceBudget;
            int pairCursor = GetNormalizedPairCursor();
            UpdateTeamSpottingPass(
                now,
                settings,
                memorySeconds,
                true,
                ref maintenanceChecksRemaining,
                ref pairCursor);
            lineOfSightChecksRemaining -= maintenanceBudget - maintenanceChecksRemaining;
            UpdateTeamSpottingPass(
                now,
                settings,
                memorySeconds,
                false,
                ref lineOfSightChecksRemaining,
                ref pairCursor);
            _spottingPairCursor = pairCursor;

            _teamA.RemoveExpired(now);
            _teamB.RemoveExpired(now);
            RemoveStaleLineOfSightCache(now, settings);
        }

        private void UpdateTeamSpottingPass(
            float now,
            MatchVisibilityGlobalSettings settings,
            float memorySeconds,
            bool previouslySpottedTargets,
            ref int lineOfSightChecksRemaining,
            ref int pairCursor)
        {
            int participantCount = _participants.Count;
            if (participantCount <= 1)
            {
                return;
            }

            int pairSlotCount = participantCount * participantCount;
            for (int inspected = 0; inspected < pairSlotCount; inspected++)
            {
                int pairSlot = pairCursor;
                pairCursor = (pairCursor + 1) % pairSlotCount;
                int spotterIndex = pairSlot / participantCount;
                int targetIndex = pairSlot % participantCount;
                if (spotterIndex == targetIndex)
                {
                    continue;
                }

                VisibilityParticipant spotter = _participants[spotterIndex];
                if (spotter.IsDead)
                {
                    continue;
                }

                TeamVisibilityState spotterTeam = GetTeamState(spotter.Team);
                if (spotterTeam == null)
                {
                    continue;
                }

                VisibilityParticipant target = _participants[targetIndex];
                if (target.IsDead || !MatchTeamUtility.AreOpposingAssignedTeams(spotter.Team, target.Team))
                {
                    continue;
                }

                if (spotterTeam.WasDirectlySpotted(target.ObjectId) != previouslySpottedTargets)
                {
                    continue;
                }

                if (!previouslySpottedTargets && spotterTeam.IsDirectlySpotted(target.ObjectId))
                {
                    continue;
                }

                bool visible = CanSpot(spotter, target, settings, now, ref lineOfSightChecksRemaining);
                spotterTeam.SetSpotterVisibility(
                    target.ObjectId,
                    spotter.ObjectId,
                    visible,
                    target.Position,
                    target.Yaw,
                    now,
                    memorySeconds);

                if (lineOfSightChecksRemaining <= 0)
                {
                    return;
                }
            }
        }

        private int GetNormalizedPairCursor()
        {
            int participantCount = _participants.Count;
            if (participantCount <= 1)
            {
                return 0;
            }

            int pairSlotCount = participantCount * participantCount;
            return ((_spottingPairCursor % pairSlotCount) + pairSlotCount) % pairSlotCount;
        }

        private bool CanSpot(
            VisibilityParticipant spotter,
            VisibilityParticipant target,
            MatchVisibilityGlobalSettings settings,
            float now,
            ref int lineOfSightChecksRemaining)
        {
            Vector3 delta = target.Position - spotter.Position;
            float distanceSqr = delta.sqrMagnitude;
            float guaranteedRange = Mathf.Max(0f, settings.guaranteedDetectionRange);
            bool withinGuaranteedRange = guaranteedRange > 0f
                                         && distanceSqr <= guaranteedRange * guaranteedRange;
            float range = Mathf.Max(0f, spotter.ViewRange);
            bool withinViewRange = range > 0f && distanceSqr <= range * range;
            if (!withinGuaranteedRange && !withinViewRange)
            {
                return false;
            }

            if (!settings.requireLineOfSight)
            {
                return true;
            }

            Vector3 origin = spotter.Position + Vector3.up * Mathf.Max(0f, settings.spotterEyeHeight);
            return HasCachedLineOfSight(
                spotter.ObjectId,
                target.ObjectId,
                origin,
                target.Position,
                target.Yaw,
                settings,
                now,
                ref lineOfSightChecksRemaining);
        }

        private bool HasCachedLineOfSight(
            int spotterObjectId,
            int targetObjectId,
            Vector3 origin,
            Vector3 targetPosition,
            float targetYaw,
            MatchVisibilityGlobalSettings settings,
            float now,
            ref int lineOfSightChecksRemaining)
        {
            long key = BuildLineOfSightKey(spotterObjectId, targetObjectId);
            bool hasCached = _lineOfSightCache.TryGetValue(key, out CachedLineOfSight cached);
            float recheckSeconds = Mathf.Max(0.05f, settings.lineOfSightRecheckSeconds);
            if (hasCached && now - cached.CheckedAt < recheckSeconds)
            {
                return cached.Visible;
            }

            float probeHeight = Mathf.Max(0f, settings.targetProbeHeight);
            float probeRadius = Mathf.Max(0f, settings.targetProbeRadius);
            Vector3 targetRight = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.right;
            for (int probeIndex = 0; probeIndex < 3; probeIndex++)
            {
                if (lineOfSightChecksRemaining <= 0)
                {
                    return hasCached && cached.Visible;
                }

                Vector3 targetPoint = targetPosition + Vector3.up * probeHeight;
                if (probeIndex == 1)
                {
                    targetPoint += targetRight * probeRadius;
                }
                else if (probeIndex == 2)
                {
                    targetPoint -= targetRight * probeRadius;
                }

                lineOfSightChecksRemaining--;
                if (!HasLineOfSight(origin, targetPoint, settings.lineOfSightMask))
                {
                    continue;
                }

                _lineOfSightCache[key] = new CachedLineOfSight
                {
                    Visible = true,
                    CheckedAt = now
                };
                return true;
            }

            _lineOfSightCache[key] = new CachedLineOfSight
            {
                Visible = false,
                CheckedAt = now
            };
            return false;
        }

        private static bool HasLineOfSight(Vector3 origin, Vector3 targetPoint, LayerMask mask)
        {
            Vector3 delta = targetPoint - origin;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            Vector3 direction = delta / distance;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                RaycastBuffer,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float blockDistance = distance - 0.05f;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = RaycastBuffer[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (RaycastBuffer[i].distance >= blockDistance)
                {
                    continue;
                }

                int hitLayer = hitCollider.gameObject.layer;
                if (IsVehicleLayer(hitLayer))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void RemoveStaleLineOfSightCache(float now, MatchVisibilityGlobalSettings settings)
        {
            float staleSeconds = Mathf.Max(settings.lineOfSightRecheckSeconds * 4f, 1f);
            _expiredLineOfSightKeys.Clear();
            foreach (KeyValuePair<long, CachedLineOfSight> pair in _lineOfSightCache)
            {
                if (now - pair.Value.CheckedAt > staleSeconds)
                {
                    _expiredLineOfSightKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < _expiredLineOfSightKeys.Count; i++)
            {
                _lineOfSightCache.Remove(_expiredLineOfSightKeys[i]);
            }

            _expiredLineOfSightKeys.Clear();
        }

        private static long BuildLineOfSightKey(int spotterObjectId, int targetObjectId)
        {
            return ((long)spotterObjectId << 32) ^ (uint)targetObjectId;
        }

        private static bool IsVehicleLayer(int layer)
        {
            return layer == ArmorLayer || layer == ChassisLayer;
        }

        private void BuildSnapshotForTeam(
            TeamVisibilityState teamState,
            float now,
            MatchVisibilityGlobalSettings settings)
        {
            _entries.Clear();
            float directFallbackSeconds = Mathf.Max(0f, settings.spottedMemorySeconds)
                                          + Mathf.Max(0.05f, settings.tickInterval);

            for (int i = 0; i < _participants.Count; i++)
            {
                VisibilityParticipant participant = _participants[i];
                MapVehicleVisibilityRelation relation = ResolveRelationForTeam(
                    teamState,
                    participant,
                    now,
                    out Vector3 position,
                    out float yaw);
                if (relation == MapVehicleVisibilityRelation.Hidden)
                {
                    continue;
                }

                float remainingVisibilitySeconds = -1f;
                if (MatchTeamUtility.AreOpposingAssignedTeams(teamState.Team, participant.Team))
                {
                    remainingVisibilitySeconds = teamState.GetRemainingVisibilitySeconds(
                        participant.ObjectId,
                        now,
                        directFallbackSeconds);
                }

                _entries.Add(new MapVisibilityEntry
                {
                    ObjectId = participant.ObjectId,
                    Relation = relation,
                    Position = position,
                    Yaw = yaw,
                    RemainingVisibilitySeconds = remainingVisibilitySeconds
                });
            }

            teamState.SetSnapshot(_version, _entries);
        }

        private MapVehicleVisibilityRelation ResolveRelationForTeam(
            TeamVisibilityState teamState,
            VisibilityParticipant participant,
            float now,
            out Vector3 position,
            out float yaw)
        {
            position = participant.Position;
            yaw = participant.Yaw;
            MatchTeam viewerTeam = teamState.Team;
            if (MatchTeamUtility.AreSameAssignedTeam(viewerTeam, participant.Team))
            {
                return participant.IsDead
                    ? MapVehicleVisibilityRelation.Destroyed
                    : MapVehicleVisibilityRelation.Ally;
            }

            if (MatchTeamUtility.AreOpposingAssignedTeams(viewerTeam, participant.Team)
                && teamState.TryGetEnemyVisibility(participant.ObjectId, now, participant.Position, participant.Yaw, out position, out yaw))
            {
                if (participant.IsDead)
                {
                    return MapVehicleVisibilityRelation.Destroyed;
                }

                return teamState.IsDirectlySpotted(participant.ObjectId)
                    ? MapVehicleVisibilityRelation.Enemy
                    : MapVehicleVisibilityRelation.EnemyLastKnown;
            }

            return MapVehicleVisibilityRelation.Hidden;
        }

        private void SendSnapshots(IMatchVisibilityUpdateSink updateSink)
        {
            _snapshotRecipients.SendSnapshots(
                _room,
                updateSink,
                CreateSnapshot(_teamA),
                CreateSnapshot(_teamB));
        }

        private void SendEmptySnapshots(IMatchVisibilityUpdateSink updateSink)
        {
            _version++;
            _snapshotRecipients.SendEmptySnapshots(_room, updateSink, _version);
        }

        private static MatchVisibilitySnapshot CreateSnapshot(TeamVisibilityState teamState)
        {
            return new MatchVisibilitySnapshot
            {
                Version = teamState.Version,
                Count = teamState.Count,
                ObjectIds = teamState.ObjectIds,
                Relations = teamState.Relations,
                Positions = teamState.Positions,
                Yaws = teamState.Yaws,
                RemainingVisibilitySeconds = teamState.RemainingVisibilitySeconds
            };
        }

        private TeamVisibilityState GetTeamState(MatchTeam team)
        {
            if (team == MatchTeam.TeamA)
            {
                return _teamA;
            }

            if (team == MatchTeam.TeamB)
            {
                return _teamB;
            }

            return null;
        }

        private static Transform GetTrackedTransform(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot != null && vehicleRoot.objectMover != null)
            {
                return vehicleRoot.objectMover.transform;
            }

            return vehicleRoot != null ? vehicleRoot.transform : null;
        }

        private static float ResolveViewRange(VehicleRoot root, MatchVisibilityGlobalSettings settings)
        {
            float viewRange = settings.fallbackViewRange;
            if (root != null && root.HasRuntimeStats && root.RuntimeStats.ViewRange > 0f)
            {
                viewRange = root.RuntimeStats.ViewRange;
            }

            if (settings.maxViewRange > 0f)
            {
                viewRange = Mathf.Min(viewRange, settings.maxViewRange);
            }

            return Mathf.Max(0f, viewRange);
        }

        private struct VisibilityParticipant
        {
            public Player Player;
            public NetworkConnection Connection;
            public VehicleRoot Root;
            public MatchTeam Team;
            public bool IsDead;
            public int ObjectId;
            public Vector3 Position;
            public float Yaw;
            public float ViewRange;
        }

        private struct MapVisibilityEntry
        {
            public int ObjectId;
            public MapVehicleVisibilityRelation Relation;
            public Vector3 Position;
            public float Yaw;
            public float RemainingVisibilitySeconds;
        }

        private struct CachedLineOfSight
        {
            public bool Visible;
            public float CheckedAt;
        }

        private struct SpottedMemoryState
        {
            public float VisibleUntil;
            public Vector3 LastKnownPosition;
            public float LastKnownYaw;
        }

        private sealed class TeamVisibilityState
        {
            private readonly HashSet<int> _directlySpottedObjectIds = new HashSet<int>();
            private readonly HashSet<int> _previouslyDirectlySpottedObjectIds = new HashSet<int>();
            private readonly Dictionary<int, HashSet<int>> _directSpotterObjectIdsByTarget =
                new Dictionary<int, HashSet<int>>(16);
            private readonly Dictionary<int, SpottedMemoryState> _spottedMemory = new Dictionary<int, SpottedMemoryState>(16);
            private readonly List<int> _expiredObjectIds = new List<int>(16);
            private readonly List<int> _staleDirectTargetIds = new List<int>(16);
            private readonly List<int> _staleSpotterObjectIds = new List<int>(16);

            public readonly MatchTeam Team;
            public int Version;
            public int Count;
            public int[] ObjectIds = Array.Empty<int>();
            public byte[] Relations = Array.Empty<byte>();
            public Vector3[] Positions = Array.Empty<Vector3>();
            public float[] Yaws = Array.Empty<float>();
            public float[] RemainingVisibilitySeconds = Array.Empty<float>();

            public TeamVisibilityState(MatchTeam team)
            {
                Team = team;
            }

            public void BeginFrame()
            {
                _previouslyDirectlySpottedObjectIds.Clear();
                foreach (int objectId in _directlySpottedObjectIds)
                {
                    _previouslyDirectlySpottedObjectIds.Add(objectId);
                }
            }

            public void SetSpotterVisibility(
                int targetObjectId,
                int spotterObjectId,
                bool visible,
                Vector3 position,
                float yaw,
                float now,
                float memorySeconds)
            {
                if (visible)
                {
                    if (!_directSpotterObjectIdsByTarget.TryGetValue(
                            targetObjectId,
                            out HashSet<int> spotterObjectIds))
                    {
                        spotterObjectIds = new HashSet<int>();
                        _directSpotterObjectIdsByTarget.Add(targetObjectId, spotterObjectIds);
                    }

                    spotterObjectIds.Add(spotterObjectId);
                    _directlySpottedObjectIds.Add(targetObjectId);
                    _spottedMemory[targetObjectId] = new SpottedMemoryState
                    {
                        VisibleUntil = float.PositiveInfinity,
                        LastKnownPosition = position,
                        LastKnownYaw = yaw
                    };
                    return;
                }

                if (!_directSpotterObjectIdsByTarget.TryGetValue(
                        targetObjectId,
                        out HashSet<int> existingSpotterObjectIds)
                    || !existingSpotterObjectIds.Remove(spotterObjectId)
                    || existingSpotterObjectIds.Count > 0)
                {
                    return;
                }

                _directSpotterObjectIdsByTarget.Remove(targetObjectId);
                if (!_directlySpottedObjectIds.Remove(targetObjectId))
                {
                    return;
                }

                SetSpottedMemory(targetObjectId, now, memorySeconds);
            }

            public void PruneSpotterLinks(
                List<VisibilityParticipant> participants,
                float now,
                float memorySeconds)
            {
                _staleDirectTargetIds.Clear();
                foreach (KeyValuePair<int, HashSet<int>> targetPair in _directSpotterObjectIdsByTarget)
                {
                    if (!ContainsTarget(participants, targetPair.Key))
                    {
                        _staleDirectTargetIds.Add(targetPair.Key);
                        continue;
                    }

                    HashSet<int> spotterObjectIds = targetPair.Value;
                    _staleSpotterObjectIds.Clear();
                    foreach (int spotterObjectId in spotterObjectIds)
                    {
                        if (!ContainsLiveSpotter(participants, spotterObjectId))
                        {
                            _staleSpotterObjectIds.Add(spotterObjectId);
                        }
                    }

                    for (int i = 0; i < _staleSpotterObjectIds.Count; i++)
                    {
                        spotterObjectIds.Remove(_staleSpotterObjectIds[i]);
                    }

                    if (spotterObjectIds.Count == 0)
                    {
                        _staleDirectTargetIds.Add(targetPair.Key);
                    }
                }

                for (int i = 0; i < _staleDirectTargetIds.Count; i++)
                {
                    int targetObjectId = _staleDirectTargetIds[i];
                    _directSpotterObjectIdsByTarget.Remove(targetObjectId);
                    if (_directlySpottedObjectIds.Remove(targetObjectId))
                    {
                        if (ContainsTarget(participants, targetObjectId))
                        {
                            SetSpottedMemory(targetObjectId, now, memorySeconds);
                        }
                        else
                        {
                            _spottedMemory.Remove(targetObjectId);
                        }
                    }
                }

                _expiredObjectIds.Clear();
                foreach (KeyValuePair<int, SpottedMemoryState> memoryPair in _spottedMemory)
                {
                    if (!ContainsTarget(participants, memoryPair.Key))
                    {
                        _expiredObjectIds.Add(memoryPair.Key);
                    }
                }

                for (int i = 0; i < _expiredObjectIds.Count; i++)
                {
                    _spottedMemory.Remove(_expiredObjectIds[i]);
                }

                _staleDirectTargetIds.Clear();
                _staleSpotterObjectIds.Clear();
                _expiredObjectIds.Clear();
            }

            private bool ContainsTarget(List<VisibilityParticipant> participants, int objectId)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    VisibilityParticipant participant = participants[i];
                    if (participant.ObjectId == objectId
                        && participant.Player != null
                        && !participant.Player.leftBattle
                        && MatchTeamUtility.AreOpposingAssignedTeams(Team, participant.Team))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool ContainsLiveSpotter(List<VisibilityParticipant> participants, int objectId)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    VisibilityParticipant participant = participants[i];
                    if (participant.ObjectId != objectId
                        || participant.Team != Team
                        || participant.IsDead
                        || participant.Player == null
                        || participant.Player.leftBattle)
                    {
                        continue;
                    }

                    return true;
                }

                return false;
            }

            private void SetSpottedMemory(int objectId, float now, float memorySeconds)
            {
                if (!_spottedMemory.TryGetValue(objectId, out SpottedMemoryState state))
                {
                    state = new SpottedMemoryState();
                }

                state.VisibleUntil = now + memorySeconds;
                _spottedMemory[objectId] = state;
            }

            public bool WasDirectlySpotted(int objectId)
            {
                return _previouslyDirectlySpottedObjectIds.Contains(objectId);
            }

            public bool IsDirectlySpotted(int objectId)
            {
                return _directlySpottedObjectIds.Contains(objectId);
            }

            public float GetRemainingVisibilitySeconds(int objectId, float now, float directFallbackSeconds)
            {
                if (_directlySpottedObjectIds.Contains(objectId))
                {
                    return directFallbackSeconds;
                }

                if (_spottedMemory.TryGetValue(objectId, out SpottedMemoryState state))
                {
                    return Mathf.Max(0f, state.VisibleUntil - now);
                }

                return 0f;
            }

            public bool IsVisible(int objectId, float now)
            {
                if (_directlySpottedObjectIds.Contains(objectId))
                {
                    return true;
                }

                return _spottedMemory.TryGetValue(objectId, out SpottedMemoryState state)
                       && state.VisibleUntil > now;
            }

            public bool TryGetEnemyVisibility(
                int objectId,
                float now,
                Vector3 currentPosition,
                float currentYaw,
                out Vector3 position,
                out float yaw)
            {
                if (_directlySpottedObjectIds.Contains(objectId))
                {
                    position = currentPosition;
                    yaw = currentYaw;
                    return true;
                }

                if (_spottedMemory.TryGetValue(objectId, out SpottedMemoryState state) && state.VisibleUntil > now)
                {
                    position = state.LastKnownPosition;
                    yaw = state.LastKnownYaw;
                    return true;
                }

                position = default;
                yaw = 0f;
                return false;
            }

            public void RemoveExpired(float now)
            {
                if (_spottedMemory.Count == 0)
                {
                    return;
                }

                _expiredObjectIds.Clear();
                foreach (KeyValuePair<int, SpottedMemoryState> pair in _spottedMemory)
                {
                    if (pair.Value.VisibleUntil <= now && !_directlySpottedObjectIds.Contains(pair.Key))
                    {
                        _expiredObjectIds.Add(pair.Key);
                    }
                }

                for (int i = 0; i < _expiredObjectIds.Count; i++)
                {
                    _spottedMemory.Remove(_expiredObjectIds[i]);
                }

                _expiredObjectIds.Clear();
            }

            public void SetSnapshot(int version, List<MapVisibilityEntry> entries)
            {
                Version = version;
                Count = entries != null ? entries.Count : 0;
                EnsureSnapshotCapacity(Count);

                for (int i = 0; i < Count; i++)
                {
                    MapVisibilityEntry entry = entries[i];
                    ObjectIds[i] = entry.ObjectId;
                    Relations[i] = (byte)entry.Relation;
                    Positions[i] = entry.Position;
                    Yaws[i] = entry.Yaw;
                    RemainingVisibilitySeconds[i] = entry.RemainingVisibilitySeconds;
                }
            }

            public void Clear()
            {
                Version = 0;
                Count = 0;
                _directlySpottedObjectIds.Clear();
                _previouslyDirectlySpottedObjectIds.Clear();
                _directSpotterObjectIdsByTarget.Clear();
                _spottedMemory.Clear();
                _expiredObjectIds.Clear();
                _staleDirectTargetIds.Clear();
                _staleSpotterObjectIds.Clear();
                ObjectIds = Array.Empty<int>();
                Relations = Array.Empty<byte>();
                Positions = Array.Empty<Vector3>();
                Yaws = Array.Empty<float>();
                RemainingVisibilitySeconds = Array.Empty<float>();
            }

            private void EnsureSnapshotCapacity(int count)
            {
                if (ObjectIds.Length >= count)
                {
                    return;
                }

                ObjectIds = new int[count];
                Relations = new byte[count];
                Positions = new Vector3[count];
                Yaws = new float[count];
                RemainingVisibilitySeconds = new float[count];
            }
        }
    }

    public struct MatchVisibleEnemy
    {
        public VehicleRoot Root;
        public Vector3 Position;
        public float Yaw;
        public bool IsDirectlySpotted;
    }
}
