using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotTargetScanner
    {
        private readonly List<MatchVisibleEnemy> _visibleEnemies = new List<MatchVisibleEnemy>(16);
        private readonly BotTargetValidator _targetValidator;
        private readonly BotAimPointResolver _aimPointResolver;
        private readonly BotLineOfFireChecker _lineOfFireChecker;
        private readonly BotAimController _aimController;

        public BotTargetScanner(
            BotTargetValidator targetValidator,
            BotAimPointResolver aimPointResolver,
            BotLineOfFireChecker lineOfFireChecker,
            BotAimController aimController)
        {
            _targetValidator = targetValidator;
            _aimPointResolver = aimPointResolver;
            _lineOfFireChecker = lineOfFireChecker;
            _aimController = aimController;
        }

        public bool TryRefreshCurrentTarget(
            VehicleRoot selfRoot,
            ServerRoom room,
            VehicleRoot targetRoot,
            float now,
            out MatchVisibleEnemy visibleEnemy)
        {
            visibleEnemy = default;
            if (selfRoot == null || room == null || targetRoot == null)
            {
                return false;
            }

            MatchVisibilityService visibility = PrepareVisibility(room, now);
            return visibility != null && visibility.TryGetVisibleEnemyFor(selfRoot, targetRoot, out visibleEnemy);
        }

        public bool TryFindBestTarget(
            VehicleRoot selfRoot,
            ServerRoom room,
            VehicleRoot currentTarget,
            BotCombatSettings settings,
            float now,
            out BotTargetCandidate bestCandidate)
        {
            bestCandidate = default;
            if (selfRoot == null || room == null)
            {
                return false;
            }

            MatchVisibilityService visibility = PrepareVisibility(room, now);
            if (visibility == null)
            {
                return false;
            }

            visibility.FillVisibleEnemiesFor(selfRoot, _visibleEnemies);
            bool found = false;
            for (int i = 0; i < _visibleEnemies.Count; i++)
            {
                MatchVisibleEnemy visibleEnemy = _visibleEnemies[i];
                VehicleRoot candidateRoot = visibleEnemy.Root;
                if (!_targetValidator.IsEnemyTarget(selfRoot, candidateRoot))
                {
                    continue;
                }

                BotTargetCandidate candidate = BuildCandidate(selfRoot, currentTarget, visibleEnemy, settings);
                if (!found || IsBetter(candidate, bestCandidate, settings))
                {
                    bestCandidate = candidate;
                    found = true;
                }
            }

            return found;
        }

        private MatchVisibilityService PrepareVisibility(ServerRoom room, float now)
        {
            if (room == null)
            {
                return null;
            }

            MatchVisibilityService visibility = room.Visibility;
            if (!visibility.IsRunning)
            {
                visibility.Start(room);
            }

            visibility.RefreshForBotQueries(now);
            return visibility;
        }

        private BotTargetCandidate BuildCandidate(
            VehicleRoot selfRoot,
            VehicleRoot currentTarget,
            MatchVisibleEnemy visibleEnemy,
            BotCombatSettings settings)
        {
            Vector3 selfPosition = BotCombatUtility.GetMovePosition(selfRoot);
            Vector3 delta = visibleEnemy.Position - selfPosition;
            delta.y = 0f;

            Vector3 aimPoint = visibleEnemy.IsDirectlySpotted
                ? _aimPointResolver.Resolve(visibleEnemy.Root, settings, Vector3.zero)
                : visibleEnemy.Position + Vector3.up * settings.fallbackTargetHeight;
            bool hasLineOfFire = false;
            bool hasAimSolution = false;
            float aimErrorDeg = float.PositiveInfinity;

            if (visibleEnemy.IsDirectlySpotted)
            {
                hasLineOfFire = _lineOfFireChecker.HasLineOfFire(selfRoot, aimPoint, visibleEnemy.Root, settings);
                Vector3 aimForward = _aimController.ResolveAimForward(selfRoot, aimPoint);
                VehicleAimInputResult aimResult = _aimController.SolveAim(selfRoot, aimPoint, aimForward);
                hasAimSolution = aimResult.HasState;
                if (hasAimSolution)
                {
                    aimErrorDeg = _aimController.EstimateAimErrorDeg(selfRoot, aimResult, aimPoint);
                }
            }

            return new BotTargetCandidate
            {
                Root = visibleEnemy.Root,
                MapPosition = visibleEnemy.Position,
                AimPoint = aimPoint,
                HasLineOfFire = hasLineOfFire,
                HasAimSolution = hasAimSolution,
                IsCurrentTarget = visibleEnemy.Root == currentTarget,
                IsDirectlySpotted = visibleEnemy.IsDirectlySpotted,
                AimErrorDeg = aimErrorDeg,
                DistanceSqr = delta.sqrMagnitude
            };
        }

        private static bool IsBetter(BotTargetCandidate candidate, BotTargetCandidate best, BotCombatSettings settings)
        {
            int candidatePriority = GetPriority(candidate, settings);
            int bestPriority = GetPriority(best, settings);
            if (candidatePriority != bestPriority)
            {
                return candidatePriority < bestPriority;
            }

            if (candidate.HasLineOfFire && best.HasLineOfFire)
            {
                float candidateAimError = ApplyCurrentTargetBonus(candidate.AimErrorDeg, candidate.IsCurrentTarget, 0.9f);
                float bestAimError = ApplyCurrentTargetBonus(best.AimErrorDeg, best.IsCurrentTarget, 0.9f);
                if (Mathf.Abs(candidateAimError - bestAimError) > 1f)
                {
                    return candidateAimError < bestAimError;
                }
            }

            float candidateDistance = ApplyCurrentTargetBonus(candidate.DistanceSqr, candidate.IsCurrentTarget, 0.75f);
            float bestDistance = ApplyCurrentTargetBonus(best.DistanceSqr, best.IsCurrentTarget, 0.75f);
            return candidateDistance < bestDistance;
        }

        private static int GetPriority(BotTargetCandidate candidate, BotCombatSettings settings)
        {
            if (!settings.requireLineOfSightToAcquire)
            {
                return candidate.HasAimSolution ? 0 : 1;
            }

            if (candidate.HasLineOfFire && candidate.HasAimSolution)
            {
                return 0;
            }

            if (candidate.HasLineOfFire)
            {
                return 1;
            }

            return 2;
        }

        private static float ApplyCurrentTargetBonus(float value, bool isCurrentTarget, float multiplier)
        {
            return isCurrentTarget ? value * multiplier : value;
        }
    }
}
