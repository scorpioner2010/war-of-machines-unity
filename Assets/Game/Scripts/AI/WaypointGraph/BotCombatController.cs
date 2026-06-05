using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class BotCombatController
    {
        private readonly BotCombatState _state = new BotCombatState();
        private readonly BotTargetValidator _targetValidator = new BotTargetValidator();
        private readonly BotAimPointResolver _aimPointResolver = new BotAimPointResolver();
        private readonly BotLineOfFireChecker _lineOfFireChecker = new BotLineOfFireChecker();
        private readonly BotAimController _aimController = new BotAimController();
        private readonly BotTargetMotionTracker _motionTracker = new BotTargetMotionTracker();
        private readonly BotCombatInputWriter _inputWriter = new BotCombatInputWriter();
        private readonly BotCombatNavigationController _navigationController = new BotCombatNavigationController();
        private readonly BotFireDecision _fireDecision;
        private readonly BotIdleAimController _idleAimController;
        private readonly BotTargetScanner _targetScanner;

        private VehicleRoot _vehicleRoot;
        private ServerRoom _room;
        private BotNavigator _navigator;
        private bool _isInitialized;

        public BotCombatController()
        {
            _fireDecision = new BotFireDecision(_aimController);
            _idleAimController = new BotIdleAimController(_aimController, _inputWriter);
            _targetScanner = new BotTargetScanner(_targetValidator, _aimPointResolver, _lineOfFireChecker, _aimController);
        }

        public VehicleRoot TargetRoot => _state.TargetRoot;

        public void Initialize(VehicleRoot vehicleRoot, ServerRoom room, BotNavigator navigator)
        {
            _vehicleRoot = vehicleRoot;
            _room = room;
            _navigator = navigator;
            _navigationController.Initialize(navigator);
            _state.ClearTarget();
            _motionTracker.Reset();
            _isInitialized = true;

            float now = Time.time;
            BotCombatSettings settings = ServerSettings.GetBotCombat();
            _state.InitializeTimings(settings, now);
        }

        public void Stop()
        {
            _inputWriter.ClearCombatInput(_vehicleRoot);
            _navigationController.ReleaseControl();
            _state.ClearTarget();
            _motionTracker.Reset();
            _isInitialized = false;
        }

        public void Tick(float now)
        {
            if (!_isInitialized || !IsServerReady())
            {
                return;
            }

            using (ProfileScope.Measure("Server.BotCombat.Tick", DiagnosticsCategories.Ai))
            {
                if (_vehicleRoot.health != null && _vehicleRoot.health.IsDead)
                {
                    Stop();
                    return;
                }

                BotCombatSettings settings = ServerSettings.GetBotCombat();
                if (!settings.enabled)
                {
                    _inputWriter.ClearCombatInput(_vehicleRoot);
                    _navigationController.ReleaseControl();
                    _state.ClearTarget();
                    _motionTracker.Reset();
                    return;
                }

                if (now < _state.NextThinkTime)
                {
                    return;
                }

                _state.ScheduleNextThink(settings, now);
                RefreshCurrentTargetFromMap(now);

                if (_state.TargetRoot == null || now >= _state.NextScanTime)
                {
                    ScanForTarget(settings, now);
                }

                if (_state.TargetRoot == null)
                {
                    _navigationController.ReleaseControl();
                    _idleAimController.ApplyNoTargetTravelAim(_vehicleRoot, _navigator, settings);
                    return;
                }

                TickTarget(settings, now);
            }
        }

        private bool IsServerReady()
        {
            return _vehicleRoot != null
                   && _vehicleRoot.IsServerInitialized
                   && _vehicleRoot.inputManager != null
                   && _vehicleRoot.weaponAimAtCamera != null
                   && _vehicleRoot.robotHullRotation != null;
        }

        private void RefreshCurrentTargetFromMap(float now)
        {
            if (_state.TargetRoot == null)
            {
                return;
            }

            if (!_targetValidator.IsEnemyTarget(_vehicleRoot, _state.TargetRoot))
            {
                ClearTarget();
                return;
            }

            if (!_targetScanner.TryRefreshCurrentTarget(_vehicleRoot, _room, _state.TargetRoot, now, out MatchVisibleEnemy visibleEnemy))
            {
                ClearTarget();
                return;
            }

            _state.RefreshTargetMapVisibility(visibleEnemy.Position, visibleEnemy.IsDirectlySpotted);
        }

        private void ScanForTarget(BotCombatSettings settings, float now)
        {
            _state.ScheduleNextScan(settings, now);

            if (!_targetScanner.TryFindBestTarget(_vehicleRoot, _room, _state.TargetRoot, settings, now, out BotTargetCandidate candidate))
            {
                if (_state.TargetRoot == null)
                {
                    return;
                }

                ClearTarget();
                return;
            }

            if (candidate.Root == _state.TargetRoot)
            {
                _state.RefreshTargetMapVisibility(candidate.MapPosition, candidate.IsDirectlySpotted);
                return;
            }

            _state.SetTarget(candidate.Root, candidate.MapPosition, candidate.IsDirectlySpotted, settings, now);
            if (candidate.IsDirectlySpotted)
            {
                _motionTracker.Start(candidate.Root, now);
            }
            else
            {
                _motionTracker.Reset();
            }
        }

        private void TickTarget(BotCombatSettings settings, float now)
        {
            if (_state.TargetIsDirectlySpotted)
            {
                _motionTracker.Tick(_state.TargetRoot, settings, now);
            }
            else
            {
                _motionTracker.Reset();
            }

            Vector3 visibleAimPoint = _state.TargetIsDirectlySpotted
                ? _aimPointResolver.Resolve(_state.TargetRoot, settings, _state.TargetAimOffset)
                : _state.TargetMapPosition + Vector3.up * settings.fallbackTargetHeight;
            bool hasLineOfFire = _state.TargetIsDirectlySpotted
                                 && _lineOfFireChecker.HasLineOfFire(_vehicleRoot, visibleAimPoint, _state.TargetRoot, settings);
            bool shouldHoldPosition = settings.holdPositionWithLineOfFire && hasLineOfFire;
            _navigationController.UpdateForTarget(settings, _state.TargetRoot, _state.TargetMapPosition, shouldHoldPosition);

            Vector3 aimPoint = _state.TargetIsDirectlySpotted
                ? _motionTracker.ApplyTargetLead(_vehicleRoot, visibleAimPoint, settings)
                : visibleAimPoint;
            Vector3 aimForward = _aimController.ResolveAimForward(_vehicleRoot, aimPoint);
            VehicleAimInputResult aimResult = _aimController.SolveAim(_vehicleRoot, aimPoint, aimForward);
            if (!aimResult.HasState)
            {
                _inputWriter.ClearCombatInput(_vehicleRoot);
                return;
            }

            bool shoot = hasLineOfFire && _fireDecision.CanShootAtTarget(_vehicleRoot, _state, aimResult, aimPoint, settings, now);
            Vector2 move = shouldHoldPosition ? Vector2.zero : _vehicleRoot.inputManager.Move;
            _inputWriter.ApplyCombatInput(_vehicleRoot, aimResult, shoot, move);

            if (shoot && _vehicleRoot.weaponReloadController != null)
            {
                _vehicleRoot.weaponReloadController.ServerTryFireAuthoritative();
            }
        }

        private void ClearTarget()
        {
            _state.ClearTarget();
            _motionTracker.Reset();
            _navigationController.ReleaseControl();
            _inputWriter.ClearCombatInput(_vehicleRoot);
        }
    }
}
