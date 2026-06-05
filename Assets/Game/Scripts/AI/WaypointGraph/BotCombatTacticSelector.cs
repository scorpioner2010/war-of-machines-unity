using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.AI.WaypointGraph
{
    internal enum BotCombatTactic
    {
        CloseMobileAssault,
        FiringPosition,
        PeekFromCover,
        KiteStrongTarget,
        FlankDistractedTarget,
        FinishWeakTarget,
        DefensiveAnchor
    }

    internal struct BotCombatTacticContext
    {
        public VehicleRoot SelfRoot;
        public VehicleRoot TargetRoot;
        public ServerRoom Room;
        public BotCombatSettings Settings;
        public Vector3 SelfPosition;
        public Vector3 SelfForward;
        public Vector3 SelfRight;
        public Vector3 TargetPosition;
        public Vector3 TargetForward;
        public Vector3 TargetRight;
        public bool TargetIsDirectlySpotted;
        public bool HasLineOfFire;
        public bool HasAimSolution;
        public bool CanFire;
        public bool TargetLookingAtSelf;
        public bool HasSideOrRearShot;
        public bool FriendlyBlocksFireLane;
        public float Distance;
        public float AimReadiness01;
        public float SelfHealth01;
        public float TargetHealth01;
        public float TargetHealth;
        public float ExpectedDamageMax;
        public float Now;
    }

    internal struct BotCombatTacticDecision
    {
        public BotCombatTactic Tactic;
        public Vector3 NavigationPosition;
        public bool HoldPosition;
        public bool AllowFire;
        public float RequiredAimReadiness01;
    }

    internal sealed class BotCombatTacticSelector
    {
        private const float EvaluationIntervalSeconds = 3f;
        private const float MinimumTacticDurationSeconds = 6f;
        private const float SwitchScoreMargin = 0.18f;
        private const float EmergencySwitchScoreMargin = 0.28f;
        private const float CurrentTacticStickiness = 0.12f;
        private const float NavigationRefreshIntervalSeconds = 0.75f;
        private const float NavigationRefreshDistance = 6f;
        private const float OccupiedPositionRadius = 9f;
        private const float FireLaneBlockerRadius = 5.5f;
        private const float FireLaneSideStepDistance = 18f;
        private const float FireLaneBackStepDistance = 8f;
        private const float CandidateMinorSideStep = 10f;
        private const float CandidateMajorSideStep = 20f;
        private const float CandidateBackStep = 12f;
        private const float CandidateForwardStep = 8f;

        private const float OccupiedPositionRadiusSqr = OccupiedPositionRadius * OccupiedPositionRadius;
        private const float FireLaneBlockerRadiusSqr = FireLaneBlockerRadius * FireLaneBlockerRadius;

        private const float CloseRange = 34f;
        private const float MediumRange = 105f;
        private const float FarRange = 180f;
        private const float PreferredFireMinRange = 48f;
        private const float PreferredFireMaxRange = 140f;
        private const float KiteDangerRange = 58f;
        private const float LowHealth01 = 0.35f;
        private const float CriticalHealth01 = 0.22f;
        private const float WeakTargetHealth01 = 0.28f;
        private const float LookingAwayRequiredAngle = 55f;

        private const float MovingFireReadiness = 0.55f;
        private const float KitingFireReadiness = 0.78f;
        private const float FlankFireReadiness = 0.85f;
        private const float PeekFireReadiness = 0.9f;
        private const float PrecisionFireReadiness = 0.95f;

        private BotCombatTactic _currentTactic;
        private readonly Vector3[] _navigationCandidates = new Vector3[13];
        private BotCombatTactic _cachedNavigationTactic;
        private Vector3 _cachedNavigationPosition;
        private float _nextEvaluationTime;
        private float _lastSwitchTime;
        private float _nextNavigationRefreshTime;
        private float _flankSide = 1f;
        private bool _hasTactic;
        private bool _hasCachedNavigationPosition;

        public void Reset()
        {
            _currentTactic = BotCombatTactic.FiringPosition;
            _cachedNavigationTactic = BotCombatTactic.FiringPosition;
            _cachedNavigationPosition = Vector3.zero;
            _nextEvaluationTime = 0f;
            _lastSwitchTime = 0f;
            _nextNavigationRefreshTime = 0f;
            _flankSide = 1f;
            _hasTactic = false;
            _hasCachedNavigationPosition = false;
        }

        public BotCombatTacticDecision Tick(BotCombatTacticContext context)
        {
            if (!_hasTactic)
            {
                SetTactic(ChooseBestTactic(context, out _), context);
            }
            else if (context.Now >= _nextEvaluationTime)
            {
                TrySwitchTactic(context);
                _nextEvaluationTime = context.Now + EvaluationIntervalSeconds;
            }

            return BuildDecision(context);
        }

        public static BotCombatTacticContext BuildContext(
            VehicleRoot selfRoot,
            VehicleRoot targetRoot,
            ServerRoom room,
            BotCombatSettings settings,
            Vector3 targetMapPosition,
            bool targetIsDirectlySpotted,
            bool hasLineOfFire,
            bool hasAimSolution,
            float aimReadiness01,
            float now)
        {
            Vector3 selfPosition = BotCombatUtility.GetMovePosition(selfRoot);
            Vector3 selfForward = ResolveForward(BotCombatUtility.GetMoveTransform(selfRoot));
            Vector3 selfRight = ResolveRight(selfForward);
            Vector3 targetPosition = targetMapPosition;

            if (targetIsDirectlySpotted)
            {
                targetPosition = BotCombatUtility.GetMovePosition(targetRoot);
            }

            Vector3 targetForward = targetIsDirectlySpotted
                ? ResolveForward(BotCombatUtility.GetMoveTransform(targetRoot))
                : ResolveDirection(targetPosition - selfPosition, selfForward);
            Vector3 targetRight = ResolveRight(targetForward);
            Vector3 toSelf = selfPosition - targetPosition;
            toSelf.y = 0f;

            float distance = toSelf.magnitude;
            Vector3 targetThreatForward = targetIsDirectlySpotted
                ? ResolveForward(targetRoot != null && targetRoot.robotHullRotation != null
                    ? targetRoot.robotHullRotation.transform
                    : BotCombatUtility.GetMoveTransform(targetRoot))
                : targetForward;

            bool targetLookingAtSelf = false;
            bool hasSideOrRearShot = false;
            if (distance > 0.001f)
            {
                Vector3 toSelfDirection = toSelf / distance;
                targetLookingAtSelf = Vector3.Angle(targetThreatForward, toSelfDirection) <= 38f;
                hasSideOrRearShot = Vector3.Dot(targetForward, toSelfDirection) < 0.5f;
            }

            float targetHealth = GetHealth(targetRoot);
            bool friendlyBlocksFireLane = HasFriendlyFireLaneBlocker(room, selfRoot, targetRoot, selfPosition, targetPosition);
            return new BotCombatTacticContext
            {
                SelfRoot = selfRoot,
                TargetRoot = targetRoot,
                Room = room,
                Settings = settings,
                SelfPosition = selfPosition,
                SelfForward = selfForward,
                SelfRight = selfRight,
                TargetPosition = targetPosition,
                TargetForward = targetForward,
                TargetRight = targetRight,
                TargetIsDirectlySpotted = targetIsDirectlySpotted,
                HasLineOfFire = hasLineOfFire,
                HasAimSolution = hasAimSolution,
                CanFire = selfRoot != null
                          && selfRoot.weaponReloadController != null
                          && selfRoot.weaponReloadController.ServerCanFire,
                TargetLookingAtSelf = targetLookingAtSelf,
                HasSideOrRearShot = hasSideOrRearShot,
                FriendlyBlocksFireLane = friendlyBlocksFireLane,
                Distance = distance,
                AimReadiness01 = Mathf.Clamp01(aimReadiness01),
                SelfHealth01 = GetHealth01(selfRoot),
                TargetHealth01 = GetHealth01(targetRoot),
                TargetHealth = targetHealth,
                ExpectedDamageMax = GetExpectedDamageMax(selfRoot),
                Now = now
            };
        }

        public static float GetAimReadiness01(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || vehicleRoot.shooterNet == null)
            {
                return 0f;
            }

            NetworkWeaponShooter shooter = vehicleRoot.shooterNet;
            float min = Mathf.Max(0f, shooter.MinDispersionDeg);
            float current = Mathf.Max(min, shooter.ServerCurrentDispersionDeg);
            float max = shooter.dispersion != null
                ? shooter.dispersion.MaxDispersion
                : current;

            if (max <= min + 0.0001f)
            {
                return 1f;
            }

            float unready01 = Mathf.InverseLerp(min, max, current);
            return 1f - Mathf.Clamp01(unready01);
        }

        private void TrySwitchTactic(BotCombatTacticContext context)
        {
            BotCombatTactic candidate = ChooseBestTactic(context, out float candidateScore);
            if (candidate == _currentTactic)
            {
                return;
            }

            float currentScore = ScoreTactic(_currentTactic, context) + CurrentTacticStickiness;
            bool minimumDurationElapsed = context.Now - _lastSwitchTime >= MinimumTacticDurationSeconds;
            float requiredMargin = minimumDurationElapsed ? SwitchScoreMargin : EmergencySwitchScoreMargin;
            if (candidateScore < currentScore + requiredMargin)
            {
                return;
            }

            SetTactic(candidate, context);
        }

        private BotCombatTactic ChooseBestTactic(BotCombatTacticContext context, out float bestScore)
        {
            BotCombatTactic bestTactic = BotCombatTactic.FiringPosition;
            bestScore = ScoreFiringPosition(context);

            TryUseBetterScore(BotCombatTactic.CloseMobileAssault, ScoreCloseMobileAssault(context), ref bestTactic, ref bestScore);
            TryUseBetterScore(BotCombatTactic.PeekFromCover, ScorePeekFromCover(context), ref bestTactic, ref bestScore);
            TryUseBetterScore(BotCombatTactic.KiteStrongTarget, ScoreKiteStrongTarget(context), ref bestTactic, ref bestScore);
            TryUseBetterScore(BotCombatTactic.FlankDistractedTarget, ScoreFlankDistractedTarget(context), ref bestTactic, ref bestScore);
            TryUseBetterScore(BotCombatTactic.FinishWeakTarget, ScoreFinishWeakTarget(context), ref bestTactic, ref bestScore);
            TryUseBetterScore(BotCombatTactic.DefensiveAnchor, ScoreDefensiveAnchor(context), ref bestTactic, ref bestScore);

            return bestTactic;
        }

        private static void TryUseBetterScore(BotCombatTactic tactic, float score, ref BotCombatTactic bestTactic, ref float bestScore)
        {
            if (score <= bestScore)
            {
                return;
            }

            bestTactic = tactic;
            bestScore = score;
        }

        private float ScoreTactic(BotCombatTactic tactic, BotCombatTacticContext context)
        {
            if (tactic == BotCombatTactic.CloseMobileAssault)
            {
                return ScoreCloseMobileAssault(context);
            }

            if (tactic == BotCombatTactic.PeekFromCover)
            {
                return ScorePeekFromCover(context);
            }

            if (tactic == BotCombatTactic.KiteStrongTarget)
            {
                return ScoreKiteStrongTarget(context);
            }

            if (tactic == BotCombatTactic.FlankDistractedTarget)
            {
                return ScoreFlankDistractedTarget(context);
            }

            if (tactic == BotCombatTactic.FinishWeakTarget)
            {
                return ScoreFinishWeakTarget(context);
            }

            if (tactic == BotCombatTactic.DefensiveAnchor)
            {
                return ScoreDefensiveAnchor(context);
            }

            return ScoreFiringPosition(context);
        }

        private static float ScoreCloseMobileAssault(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted || context.Distance > CloseRange)
            {
                return 0.05f;
            }

            float score = 0.35f + Mathf.InverseLerp(CloseRange, 6f, context.Distance) * 0.4f;
            if (context.SelfHealth01 >= LowHealth01)
            {
                score += 0.15f;
            }

            if (context.TargetHealth01 <= WeakTargetHealth01)
            {
                score += 0.2f;
            }

            if (!context.TargetLookingAtSelf)
            {
                score += 0.1f;
            }

            if (context.SelfHealth01 <= CriticalHealth01 && context.TargetHealth01 > context.SelfHealth01)
            {
                score -= 0.35f;
            }

            return score;
        }

        private static float ScoreFiringPosition(BotCombatTacticContext context)
        {
            float score = 0.22f;
            if (context.Distance >= PreferredFireMinRange && context.Distance <= PreferredFireMaxRange)
            {
                score += 0.32f;
            }
            else if (context.Distance > PreferredFireMaxRange && context.Distance <= FarRange)
            {
                score += 0.18f;
            }

            if (context.HasLineOfFire && context.HasAimSolution)
            {
                score += 0.28f;
            }

            if (context.AimReadiness01 >= PrecisionFireReadiness)
            {
                score += 0.08f;
            }

            if (context.FriendlyBlocksFireLane)
            {
                score -= 0.12f;
            }

            if (context.Distance < CloseRange)
            {
                score -= 0.22f;
            }

            return score;
        }

        private static float ScorePeekFromCover(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted || context.Distance < CloseRange || context.Distance > MediumRange)
            {
                return 0.04f;
            }

            float score = 0.24f;
            if (context.HasLineOfFire)
            {
                score += 0.28f;
            }

            if (!context.CanFire)
            {
                score += 0.14f;
            }

            if (context.SelfHealth01 <= LowHealth01)
            {
                score += 0.12f;
            }

            if (context.TargetLookingAtSelf)
            {
                score += 0.08f;
            }

            return score;
        }

        private static float ScoreKiteStrongTarget(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted || context.Distance > KiteDangerRange)
            {
                return 0.03f;
            }

            bool targetIsStronger = context.TargetHealth01 > context.SelfHealth01 + 0.15f;
            if (!targetIsStronger && context.SelfHealth01 > LowHealth01)
            {
                return 0.08f;
            }

            float score = 0.38f + Mathf.InverseLerp(KiteDangerRange, 8f, context.Distance) * 0.32f;
            if (targetIsStronger)
            {
                score += 0.18f;
            }

            if (context.SelfHealth01 <= LowHealth01)
            {
                score += 0.16f;
            }

            return score;
        }

        private static float ScoreFlankDistractedTarget(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted || context.Distance < CloseRange || context.Distance > MediumRange)
            {
                return 0.03f;
            }

            Vector3 toSelf = context.SelfPosition - context.TargetPosition;
            toSelf.y = 0f;
            if (toSelf.sqrMagnitude <= 0.0001f)
            {
                return 0.03f;
            }

            float targetAngleToSelf = Vector3.Angle(context.TargetForward, toSelf.normalized);
            if (context.TargetLookingAtSelf || targetAngleToSelf < LookingAwayRequiredAngle)
            {
                return 0.08f;
            }

            float score = 0.3f;
            if (targetAngleToSelf >= LookingAwayRequiredAngle)
            {
                score += 0.28f;
            }

            if (context.HasSideOrRearShot)
            {
                score += 0.16f;
            }

            if (context.SelfHealth01 >= LowHealth01)
            {
                score += 0.1f;
            }

            if (context.HasLineOfFire)
            {
                score += 0.08f;
            }

            return score;
        }

        private static float ScoreFinishWeakTarget(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted)
            {
                return 0.02f;
            }

            bool targetCanDieSoon = context.ExpectedDamageMax > 0f
                                    && context.TargetHealth > 0f
                                    && context.TargetHealth <= context.ExpectedDamageMax * 1.25f;
            if (!targetCanDieSoon && context.TargetHealth01 > WeakTargetHealth01)
            {
                return 0.07f;
            }

            float score = 0.42f;
            if (targetCanDieSoon)
            {
                score += 0.28f;
            }

            if (context.Distance <= MediumRange)
            {
                score += 0.12f;
            }

            if (context.HasLineOfFire)
            {
                score += 0.14f;
            }

            if (context.CanFire)
            {
                score += 0.08f;
            }

            if (context.SelfHealth01 <= CriticalHealth01 && !context.HasLineOfFire)
            {
                score -= 0.18f;
            }

            return score;
        }

        private static float ScoreDefensiveAnchor(BotCombatTacticContext context)
        {
            float score = 0.12f;
            if (context.SelfHealth01 <= LowHealth01)
            {
                score += 0.32f;
            }

            if (context.SelfHealth01 <= CriticalHealth01)
            {
                score += 0.18f;
            }

            if (context.Distance >= PreferredFireMinRange)
            {
                score += 0.16f;
            }

            if (context.HasLineOfFire && context.TargetLookingAtSelf)
            {
                score += 0.12f;
            }

            if (context.Distance < CloseRange)
            {
                score -= 0.2f;
            }

            return score;
        }

        private void SetTactic(BotCombatTactic tactic, BotCombatTacticContext context)
        {
            _currentTactic = tactic;
            _lastSwitchTime = context.Now;
            _nextEvaluationTime = context.Now + EvaluationIntervalSeconds;
            _hasTactic = true;
            _hasCachedNavigationPosition = false;
            _flankSide = ResolveFlankSide(context);
        }

        private BotCombatTacticDecision BuildDecision(BotCombatTacticContext context)
        {
            if (!context.TargetIsDirectlySpotted)
            {
                return CreateDecision(context, _currentTactic, context.TargetPosition, false, false, PrecisionFireReadiness);
            }

            if (_currentTactic == BotCombatTactic.CloseMobileAssault)
            {
                return BuildCloseMobileAssaultDecision(context);
            }

            if (_currentTactic == BotCombatTactic.PeekFromCover)
            {
                return BuildPeekFromCoverDecision(context);
            }

            if (_currentTactic == BotCombatTactic.KiteStrongTarget)
            {
                return BuildKiteStrongTargetDecision(context);
            }

            if (_currentTactic == BotCombatTactic.FlankDistractedTarget)
            {
                return BuildFlankDistractedTargetDecision(context);
            }

            if (_currentTactic == BotCombatTactic.FinishWeakTarget)
            {
                return BuildFinishWeakTargetDecision(context);
            }

            if (_currentTactic == BotCombatTactic.DefensiveAnchor)
            {
                return BuildDefensiveAnchorDecision(context);
            }

            return BuildFiringPositionDecision(context);
        }

        private BotCombatTacticDecision BuildCloseMobileAssaultDecision(BotCombatTacticContext context)
        {
            Vector3 navigationPosition = BuildRearSidePosition(context, 11f, 9f);
            if (context.Distance <= 18f)
            {
                Vector3 orbitDirection = BuildOrbitDirection(context);
                navigationPosition = context.TargetPosition + orbitDirection * 13f;
            }

            return CreateDecision(context, BotCombatTactic.CloseMobileAssault, navigationPosition, false, true, MovingFireReadiness);
        }

        private BotCombatTacticDecision BuildFiringPositionDecision(BotCombatTacticContext context)
        {
            bool hold = CanHoldPosition(context) && context.Distance >= PreferredFireMinRange;
            Vector3 navigationPosition = context.TargetPosition;
            if (!hold && context.Distance < PreferredFireMinRange)
            {
                navigationPosition = BuildRetreatPosition(context, 24f, 5f);
            }

            return CreateDecision(context, BotCombatTactic.FiringPosition, navigationPosition, hold, true, PrecisionFireReadiness);
        }

        private BotCombatTacticDecision BuildPeekFromCoverDecision(BotCombatTacticContext context)
        {
            bool hold = CanHoldPosition(context) && context.CanFire;
            Vector3 navigationPosition = context.TargetPosition;
            if (context.HasLineOfFire && !context.CanFire)
            {
                navigationPosition = BuildRetreatPosition(context, 15f, 8f);
            }
            else if (!context.HasLineOfFire)
            {
                navigationPosition = context.TargetPosition;
            }

            return CreateDecision(context, BotCombatTactic.PeekFromCover, navigationPosition, hold, true, PeekFireReadiness);
        }

        private BotCombatTacticDecision BuildKiteStrongTargetDecision(BotCombatTacticContext context)
        {
            Vector3 navigationPosition = BuildRetreatPosition(context, 34f, 12f);
            return CreateDecision(context, BotCombatTactic.KiteStrongTarget, navigationPosition, false, true, KitingFireReadiness);
        }

        private BotCombatTacticDecision BuildFlankDistractedTargetDecision(BotCombatTacticContext context)
        {
            Vector3 navigationPosition = BuildRearSidePosition(context, 17f, 14f);
            bool allowFire = context.HasSideOrRearShot || context.Distance <= CloseRange;
            return CreateDecision(context, BotCombatTactic.FlankDistractedTarget, navigationPosition, false, allowFire, FlankFireReadiness);
        }

        private BotCombatTacticDecision BuildFinishWeakTargetDecision(BotCombatTacticContext context)
        {
            Vector3 navigationPosition = context.Distance > 28f
                ? context.TargetPosition
                : BuildRearSidePosition(context, 9f, 8f);
            bool hold = CanHoldPosition(context) && context.Distance >= PreferredFireMinRange;
            float requiredReadiness = context.TargetHealth > 0f && context.TargetHealth <= context.ExpectedDamageMax * 1.25f
                ? MovingFireReadiness
                : KitingFireReadiness;

            return CreateDecision(context, BotCombatTactic.FinishWeakTarget, navigationPosition, hold, true, requiredReadiness);
        }

        private BotCombatTacticDecision BuildDefensiveAnchorDecision(BotCombatTacticContext context)
        {
            bool hold = CanHoldPosition(context) && context.Distance >= PreferredFireMinRange;
            Vector3 navigationPosition = hold || context.Distance >= PreferredFireMinRange
                ? context.TargetPosition
                : BuildRetreatPosition(context, 28f, 6f);

            return CreateDecision(context, BotCombatTactic.DefensiveAnchor, navigationPosition, hold, true, PrecisionFireReadiness);
        }

        private BotCombatTacticDecision CreateDecision(
            BotCombatTacticContext context,
            BotCombatTactic tactic,
            Vector3 desiredNavigationPosition,
            bool holdPosition,
            bool allowFire,
            float requiredAimReadiness01)
        {
            if (!BotCombatUtility.IsFinite(desiredNavigationPosition))
            {
                desiredNavigationPosition = context.TargetPosition;
            }

            if (holdPosition && context.FriendlyBlocksFireLane)
            {
                holdPosition = false;
            }

            if (context.FriendlyBlocksFireLane)
            {
                allowFire = false;
            }

            if (!holdPosition)
            {
                desiredNavigationPosition = SelectOpenNavigationPosition(context, desiredNavigationPosition);
            }

            Vector3 navigationPosition = holdPosition
                ? desiredNavigationPosition
                : GetStableNavigationPosition(tactic, desiredNavigationPosition, context.Now);

            return new BotCombatTacticDecision
            {
                Tactic = tactic,
                NavigationPosition = navigationPosition,
                HoldPosition = holdPosition,
                AllowFire = allowFire,
                RequiredAimReadiness01 = Mathf.Clamp01(requiredAimReadiness01)
            };
        }

        private Vector3 SelectOpenNavigationPosition(BotCombatTacticContext context, Vector3 desiredPosition)
        {
            int candidateCount = BuildNavigationCandidates(context, desiredPosition);
            if (candidateCount <= 0)
            {
                return desiredPosition;
            }

            Vector3 bestPosition = desiredPosition;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < candidateCount; i++)
            {
                Vector3 candidate = _navigationCandidates[i];
                float score = ScoreNavigationCandidate(context, candidate, desiredPosition);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate;
                }
            }

            return bestPosition;
        }

        private int BuildNavigationCandidates(BotCombatTacticContext context, Vector3 desiredPosition)
        {
            int count = 0;
            Vector3 targetDirection = ResolveDirection(context.TargetPosition - desiredPosition, context.TargetPosition - context.SelfPosition);
            Vector3 targetSide = ResolveRight(targetDirection);
            Vector3 selfToTargetDirection = ResolveDirection(context.TargetPosition - context.SelfPosition, context.SelfForward);
            Vector3 selfSide = ResolveRight(selfToTargetDirection);

            AddCandidate(ref count, desiredPosition);
            if (context.FriendlyBlocksFireLane)
            {
                AddCandidate(ref count, context.SelfPosition + selfSide * FireLaneSideStepDistance);
                AddCandidate(ref count, context.SelfPosition - selfSide * FireLaneSideStepDistance);
                AddCandidate(ref count, context.SelfPosition + selfSide * FireLaneSideStepDistance - selfToTargetDirection * FireLaneBackStepDistance);
                AddCandidate(ref count, context.SelfPosition - selfSide * FireLaneSideStepDistance - selfToTargetDirection * FireLaneBackStepDistance);
            }

            AddCandidate(ref count, desiredPosition + targetSide * CandidateMinorSideStep);
            AddCandidate(ref count, desiredPosition - targetSide * CandidateMinorSideStep);
            AddCandidate(ref count, desiredPosition + targetSide * CandidateMajorSideStep);
            AddCandidate(ref count, desiredPosition - targetSide * CandidateMajorSideStep);
            AddCandidate(ref count, desiredPosition - targetDirection * CandidateBackStep);
            AddCandidate(ref count, desiredPosition + targetDirection * CandidateForwardStep);
            AddCandidate(ref count, desiredPosition - targetDirection * CandidateBackStep + targetSide * CandidateMinorSideStep);
            AddCandidate(ref count, desiredPosition - targetDirection * CandidateBackStep - targetSide * CandidateMinorSideStep);

            return count;
        }

        private void AddCandidate(ref int count, Vector3 position)
        {
            if (count >= _navigationCandidates.Length)
            {
                return;
            }

            _navigationCandidates[count] = position;
            count++;
        }

        private static float ScoreNavigationCandidate(
            BotCombatTacticContext context,
            Vector3 candidate,
            Vector3 desiredPosition)
        {
            if (!BotCombatUtility.IsFinite(candidate))
            {
                return float.PositiveInfinity;
            }

            float score = HorizontalDistanceSqr(candidate, desiredPosition) * 0.015f;
            score += ScorePositionOccupancy(context, candidate);

            if (HasFriendlyFireLaneBlocker(context.Room, context.SelfRoot, context.TargetRoot, candidate, context.TargetPosition))
            {
                score += 10000f;
            }

            return score;
        }

        private static float ScorePositionOccupancy(BotCombatTacticContext context, Vector3 candidate)
        {
            if (context.Room == null)
            {
                return 0f;
            }

            List<LobbyPlayer> players = context.Room.GetPlayers();
            if (players == null)
            {
                return 0f;
            }

            float score = 0f;
            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayer player = players[i];
                VehicleRoot otherRoot = player != null ? player.playerRoot : null;
                if (otherRoot == null || otherRoot == context.SelfRoot)
                {
                    continue;
                }

                if (otherRoot.health != null && otherRoot.health.IsDead)
                {
                    continue;
                }

                Vector3 otherPosition = BotCombatUtility.GetMovePosition(otherRoot);
                float distanceSqr = HorizontalDistanceSqr(candidate, otherPosition);
                if (distanceSqr >= OccupiedPositionRadiusSqr)
                {
                    continue;
                }

                float occupancy01 = 1f - Mathf.Clamp01(distanceSqr / OccupiedPositionRadiusSqr);
                score += occupancy01 * 850f;
                if (IsFriendly(context.SelfRoot, otherRoot, player))
                {
                    score += occupancy01 * 250f;
                }
            }

            return score;
        }

        private Vector3 GetStableNavigationPosition(BotCombatTactic tactic, Vector3 desiredPosition, float now)
        {
            bool shouldRefresh = !_hasCachedNavigationPosition
                                 || _cachedNavigationTactic != tactic
                                 || now >= _nextNavigationRefreshTime
                                 || HasNavigationMovedEnough(desiredPosition);
            if (shouldRefresh)
            {
                _cachedNavigationTactic = tactic;
                _cachedNavigationPosition = desiredPosition;
                _nextNavigationRefreshTime = now + NavigationRefreshIntervalSeconds;
                _hasCachedNavigationPosition = true;
            }

            return _cachedNavigationPosition;
        }

        private bool HasNavigationMovedEnough(Vector3 desiredPosition)
        {
            Vector3 delta = desiredPosition - _cachedNavigationPosition;
            delta.y = 0f;
            return delta.sqrMagnitude >= NavigationRefreshDistance * NavigationRefreshDistance;
        }

        private static bool CanHoldPosition(BotCombatTacticContext context)
        {
            return context.Settings != null
                   && context.Settings.holdPositionWithLineOfFire
                   && context.HasLineOfFire;
        }

        private Vector3 BuildRearSidePosition(BotCombatTacticContext context, float rearDistance, float sideDistance)
        {
            Vector3 position = context.TargetPosition;
            position -= context.TargetForward * rearDistance;
            position += context.TargetRight * _flankSide * sideDistance;
            return position;
        }

        private Vector3 BuildRetreatPosition(BotCombatTacticContext context, float awayDistance, float sideDistance)
        {
            Vector3 away = context.SelfPosition - context.TargetPosition;
            away.y = 0f;
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = -context.TargetForward;
            }

            away.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, away);
            if (side.sqrMagnitude <= 0.0001f)
            {
                side = context.SelfRight;
            }

            side.Normalize();
            return context.SelfPosition + away * awayDistance + side * _flankSide * sideDistance;
        }

        private Vector3 BuildOrbitDirection(BotCombatTacticContext context)
        {
            Vector3 fromTarget = context.SelfPosition - context.TargetPosition;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude <= 0.0001f)
            {
                fromTarget = -context.TargetForward;
            }

            fromTarget.Normalize();
            Vector3 orbit = fromTarget + context.TargetRight * _flankSide * 0.85f;
            orbit.y = 0f;
            if (orbit.sqrMagnitude <= 0.0001f)
            {
                return fromTarget;
            }

            return orbit.normalized;
        }

        private static float ResolveFlankSide(BotCombatTacticContext context)
        {
            Vector3 toSelf = context.SelfPosition - context.TargetPosition;
            toSelf.y = 0f;
            if (toSelf.sqrMagnitude > 0.0001f)
            {
                float sideDot = Vector3.Dot(toSelf.normalized, context.TargetRight);
                if (Mathf.Abs(sideDot) > 0.15f)
                {
                    return sideDot >= 0f ? 1f : -1f;
                }
            }

            return Random.value < 0.5f ? -1f : 1f;
        }

        private static Vector3 ResolveForward(Transform transform)
        {
            if (transform == null)
            {
                return Vector3.forward;
            }

            return ResolveDirection(transform.forward, Vector3.forward);
        }

        private static Vector3 ResolveRight(Vector3 forward)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            right.y = 0f;
            if (right.sqrMagnitude <= 0.0001f)
            {
                return Vector3.right;
            }

            return right.normalized;
        }

        private static Vector3 ResolveDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (!BotCombatUtility.IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            if (!BotCombatUtility.IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        private static bool HasFriendlyFireLaneBlocker(
            ServerRoom room,
            VehicleRoot selfRoot,
            VehicleRoot targetRoot,
            Vector3 firePosition,
            Vector3 targetPosition)
        {
            if (room == null || selfRoot == null)
            {
                return false;
            }

            List<LobbyPlayer> players = room.GetPlayers();
            if (players == null)
            {
                return false;
            }

            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayer player = players[i];
                VehicleRoot otherRoot = player != null ? player.playerRoot : null;
                if (otherRoot == null || otherRoot == selfRoot || otherRoot == targetRoot)
                {
                    continue;
                }

                if (!IsFriendly(selfRoot, otherRoot, player))
                {
                    continue;
                }

                if (otherRoot.health != null && otherRoot.health.IsDead)
                {
                    continue;
                }

                Vector3 otherPosition = BotCombatUtility.GetMovePosition(otherRoot);
                if (IsPointNearSegment(firePosition, targetPosition, otherPosition, FireLaneBlockerRadiusSqr))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointNearSegment(Vector3 start, Vector3 end, Vector3 point, float radiusSqr)
        {
            Vector3 segment = end - start;
            segment.y = 0f;
            float segmentSqr = segment.sqrMagnitude;
            if (segmentSqr <= 0.0001f)
            {
                return false;
            }

            Vector3 toPoint = point - start;
            toPoint.y = 0f;
            float t = Vector3.Dot(toPoint, segment) / segmentSqr;
            if (t <= 0.05f || t >= 0.95f)
            {
                return false;
            }

            Vector3 closest = start + segment * t;
            return HorizontalDistanceSqr(point, closest) <= radiusSqr;
        }

        private static bool IsFriendly(VehicleRoot selfRoot, VehicleRoot otherRoot, LobbyPlayer otherPlayer)
        {
            MatchTeam selfTeam = ResolveTeam(selfRoot, null);
            MatchTeam otherTeam = ResolveTeam(otherRoot, otherPlayer);
            return MatchTeamUtility.AreSameAssignedTeam(selfTeam, otherTeam);
        }

        private static MatchTeam ResolveTeam(VehicleRoot root, LobbyPlayer player)
        {
            MatchTeam team = player != null ? player.team : MatchTeam.None;
            if (!MatchTeamUtility.IsAssigned(team) && root != null && root.characterInit != null)
            {
                team = root.characterInit.Team.Value;
            }

            return team;
        }

        private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private static float GetHealth(VehicleRoot root)
        {
            if (root == null || root.health == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, root.health.Current);
        }

        private static float GetHealth01(VehicleRoot root)
        {
            if (root == null || root.health == null)
            {
                return 1f;
            }

            return Mathf.Clamp01(root.health.Current / root.health.MaxHealth);
        }

        private static float GetExpectedDamageMax(VehicleRoot root)
        {
            if (root == null || root.shooterNet == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, root.shooterNet.damageMax);
        }
    }
}
