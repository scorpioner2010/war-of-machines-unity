using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Server;
using UnityEngine;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.AI.WaypointGraph
{
    public sealed class BotCombatController
    {
        private const int RaycastBufferSize = 64;
        private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[RaycastBufferSize];

        private VehicleRoot _vehicleRoot;
        private ServerRoom _room;
        private BotNavigator _navigator;
        private VehicleRoot _targetRoot;
        private VehicleRoot _navigationTargetRoot;
        private Vector3 _targetAimOffset;
        private Vector3 _lastTargetPosition;
        private Vector3 _targetVelocity;
        private float _nextThinkTime;
        private float _nextScanTime;
        private float _targetAcquiredTime;
        private float _fireAllowedTime;
        private float _lastTargetVisibleTime;
        private float _lastTargetSampleTime;
        private bool _hasTargetPositionSample;
        private bool _isInitialized;

        public VehicleRoot TargetRoot => _targetRoot;

        public void Initialize(VehicleRoot vehicleRoot, ServerRoom room, BotNavigator navigator)
        {
            _vehicleRoot = vehicleRoot;
            _room = room;
            _navigator = navigator;
            _targetRoot = null;
            _navigationTargetRoot = null;
            _targetVelocity = Vector3.zero;
            _hasTargetPositionSample = false;
            _isInitialized = true;

            float now = Time.time;
            BotCombatSettings settings = ServerSettings.GetBotCombat();
            _nextThinkTime = now + Random.Range(0f, settings.thinkInterval);
            _nextScanTime = now + Random.Range(0f, settings.targetScanInterval);
        }

        public void Stop()
        {
            ClearCombatInput();
            ReleaseNavigationControl();
            ClearTarget();
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
                    ClearCombatInput();
                    ReleaseNavigationControl();
                    ClearTarget();
                    return;
                }

                if (now < _nextThinkTime)
                {
                    return;
                }

                _nextThinkTime = now + settings.thinkInterval;

                if (!IsCurrentTargetValid(settings))
                {
                    ClearTarget();
                }

                if (_targetRoot == null || now >= _nextScanTime)
                {
                    ScanForTarget(settings, now);
                }

                if (_targetRoot == null)
                {
                    ClearCombatInput();
                    ReleaseNavigationControl();
                    return;
                }

                TickTargetVelocity(settings, now);

                Vector3 visibleAimPoint = ResolveTargetAimPoint(_targetRoot, settings, true);
                bool hasLineOfFire = HasLineOfFire(visibleAimPoint, _targetRoot, settings);
                bool shouldHoldPosition = settings.holdPositionWithLineOfFire && hasLineOfFire;
                UpdateNavigationForCombat(settings, shouldHoldPosition);

                if (hasLineOfFire)
                {
                    _lastTargetVisibleTime = now;
                }
                else if (now - _lastTargetVisibleTime > settings.lostSightForgetSeconds)
                {
                    ClearCombatInput();
                    ReleaseNavigationControl();
                    ClearTarget();
                    return;
                }

                Vector3 aimPoint = ApplyTargetLead(visibleAimPoint, settings);
                Vector3 aimForward = ResolveAimForward(aimPoint);
                VehicleAimInputResult aimResult = SolveAim(aimPoint, aimForward);
                if (!aimResult.HasState)
                {
                    ClearCombatInput();
                    return;
                }

                bool shoot = hasLineOfFire && CanShootAtTarget(aimResult, aimPoint, settings, now);
                Vector2 move = shouldHoldPosition ? Vector2.zero : _vehicleRoot.inputManager.Move;
                ApplyCombatInput(aimResult, shoot, move);

                if (shoot && _vehicleRoot.weaponReloadController != null)
                {
                    _vehicleRoot.weaponReloadController.ServerTryFireAuthoritative();
                }
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

        private bool IsCurrentTargetValid(BotCombatSettings settings)
        {
            if (!IsEnemyTarget(_targetRoot))
            {
                return false;
            }

            float forgetDistance = GetAcquireDistance(settings) * settings.forgetTargetDistanceMultiplier;
            Vector3 delta = GetMovePosition(_targetRoot) - GetMovePosition(_vehicleRoot);
            delta.y = 0f;
            return delta.sqrMagnitude <= forgetDistance * forgetDistance;
        }

        private void ScanForTarget(BotCombatSettings settings, float now)
        {
            _nextScanTime = now + settings.targetScanInterval;

            if (_room == null)
            {
                return;
            }

            var players = _room.GetPlayers();
            if (players == null || players.Count <= 1)
            {
                return;
            }

            float acquireDistance = GetAcquireDistance(settings);
            float acquireDistanceSqr = acquireDistance * acquireDistance;
            Vector3 selfPosition = GetMovePosition(_vehicleRoot);
            VehicleRoot bestTarget = null;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayer player = players[i];
                if (player == null || player.leftBattle)
                {
                    continue;
                }

                VehicleRoot candidate = player.playerRoot;
                if (!IsEnemyTarget(candidate))
                {
                    continue;
                }

                Vector3 delta = GetMovePosition(candidate) - selfPosition;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;
                if (distanceSqr > acquireDistanceSqr)
                {
                    continue;
                }

                Vector3 candidateAimPoint = ResolveTargetAimPoint(candidate, settings, false);
                if (settings.requireLineOfSightToAcquire && !HasLineOfFire(candidateAimPoint, candidate, settings))
                {
                    continue;
                }

                float score = distanceSqr;
                if (candidate == _targetRoot)
                {
                    score *= 0.75f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != null && bestTarget != _targetRoot)
            {
                SetTarget(bestTarget, settings, now);
            }
        }

        private void SetTarget(VehicleRoot targetRoot, BotCombatSettings settings, float now)
        {
            _targetRoot = targetRoot;
            _navigationTargetRoot = null;
            _targetVelocity = Vector3.zero;
            _lastTargetPosition = GetMovePosition(targetRoot);
            _lastTargetSampleTime = now;
            _hasTargetPositionSample = true;
            _targetAcquiredTime = now;
            _lastTargetVisibleTime = now;
            _fireAllowedTime = now + Random.Range(settings.reactionDelayMin, settings.reactionDelayMax);
            _targetAimOffset = BuildAimOffset(settings);
        }

        private void ClearTarget()
        {
            _targetRoot = null;
            _targetVelocity = Vector3.zero;
            _targetAimOffset = Vector3.zero;
            _hasTargetPositionSample = false;
            _targetAcquiredTime = 0f;
            _fireAllowedTime = 0f;
            _lastTargetVisibleTime = 0f;
        }

        private void UpdateNavigationForCombat(BotCombatSettings settings, bool holdPosition)
        {
            if (_navigator == null)
            {
                return;
            }

            _navigator.SetMovementSuppressed(holdPosition);
            if (holdPosition)
            {
                ClearNavigationTarget();
                return;
            }

            SyncNavigationTarget(settings);
        }

        private void SyncNavigationTarget(BotCombatSettings settings)
        {
            if (!settings.moveTowardTarget || _navigator == null)
            {
                return;
            }

            if (_navigationTargetRoot == _targetRoot)
            {
                return;
            }

            _navigationTargetRoot = _targetRoot;
            _navigator.SetTarget(GetMoveTransform(_targetRoot));
        }

        private void ClearNavigationTarget()
        {
            if (_navigator != null && _navigationTargetRoot != null)
            {
                _navigator.SetTarget(null);
            }

            _navigationTargetRoot = null;
        }

        private void ReleaseNavigationControl()
        {
            if (_navigator != null)
            {
                _navigator.SetMovementSuppressed(false);
            }

            ClearNavigationTarget();
        }

        private void TickTargetVelocity(BotCombatSettings settings, float now)
        {
            if (_targetRoot == null)
            {
                _targetVelocity = Vector3.zero;
                _hasTargetPositionSample = false;
                return;
            }

            Vector3 targetPosition = GetMovePosition(_targetRoot);
            if (!_hasTargetPositionSample)
            {
                _lastTargetPosition = targetPosition;
                _lastTargetSampleTime = now;
                _targetVelocity = Vector3.zero;
                _hasTargetPositionSample = true;
                return;
            }

            if (now - _lastTargetSampleTime < settings.targetVelocitySampleInterval)
            {
                return;
            }

            float dt = Mathf.Max(0.001f, now - _lastTargetSampleTime);
            Vector3 velocity = (targetPosition - _lastTargetPosition) / dt;
            velocity.y = 0f;
            _targetVelocity = velocity;
            _lastTargetPosition = targetPosition;
            _lastTargetSampleTime = now;
        }

        private VehicleAimInputResult SolveAim(Vector3 aimPoint, Vector3 aimForward)
        {
            return VehicleAimInputSolver.SolveForAimPoint(
                _vehicleRoot,
                aimPoint,
                aimForward,
                _vehicleRoot.robotHullRotation.CurrentLocalYaw,
                _vehicleRoot.weaponAimAtCamera.CurrentLocalPitch);
        }

        private void ApplyCombatInput(VehicleAimInputResult aimResult, bool shoot, Vector2 move)
        {
            if (_vehicleRoot == null || _vehicleRoot.inputManager == null)
            {
                return;
            }

            VehicleServerInput input = VehicleServerInput.Combat(
                move,
                shoot,
                false,
                aimResult.YawDeg,
                aimResult.PitchDeg,
                aimResult.CameraAimPoint,
                aimResult.CameraAimForward);

            _vehicleRoot.inputManager.ServerSetExternalInput(input, true);
        }

        private void ClearCombatInput()
        {
            if (_vehicleRoot == null || _vehicleRoot.inputManager == null || !_vehicleRoot.inputManager.IsServerInitialized)
            {
                return;
            }

            VehicleServerInput input = VehicleServerInput.Movement(_vehicleRoot.inputManager.Move);
            _vehicleRoot.inputManager.ServerSetExternalInput(input, true);
        }

        private bool CanShootAtTarget(
            VehicleAimInputResult aimResult,
            Vector3 aimPoint,
            BotCombatSettings settings,
            float now)
        {
            if (now - _targetAcquiredTime < settings.minTargetHoldBeforeFire || now < _fireAllowedTime)
            {
                return false;
            }

            if (_vehicleRoot.weaponReloadController == null || !_vehicleRoot.weaponReloadController.ServerCanFire)
            {
                return false;
            }

            if (_vehicleRoot.shooterNet == null)
            {
                return false;
            }

            if (!IsAimAligned(aimResult, aimPoint, settings))
            {
                return false;
            }

            return IsDispersionReady(settings);
        }

        private bool IsAimAligned(VehicleAimInputResult aimResult, Vector3 aimPoint, BotCombatSettings settings)
        {
            VehicleTurretRotationController turret = _vehicleRoot.robotHullRotation;
            WeaponAimController weaponAim = _vehicleRoot.weaponAimAtCamera;

            float yawError = Mathf.Abs(Mathf.DeltaAngle(turret.CurrentLocalYaw, aimResult.YawDeg));
            if (yawError > settings.maxAimYawErrorDeg)
            {
                return false;
            }

            float pitchError = Mathf.Abs(Mathf.DeltaAngle(weaponAim.CurrentLocalPitch, aimResult.PitchDeg));
            if (pitchError > settings.maxAimPitchErrorDeg)
            {
                return false;
            }

            Vector3 origin = GetAimOrigin();
            Vector3 desiredDirection = aimPoint - origin;
            if (!IsFinite(desiredDirection) || desiredDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            desiredDirection.Normalize();
            Vector3 muzzleForward = weaponAim.GetLogicalAimForwardWorld();
            if (!IsFinite(muzzleForward) || muzzleForward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            muzzleForward.Normalize();
            return Vector3.Angle(muzzleForward, desiredDirection) <= settings.maxMuzzleAimErrorDeg;
        }

        private bool IsDispersionReady(BotCombatSettings settings)
        {
            return true;
        }

        private Vector3 ResolveTargetAimPoint(VehicleRoot targetRoot, BotCombatSettings settings, bool includeOffset)
        {
            if (targetRoot == null)
            {
                return Vector3.zero;
            }

            Bounds bounds;
            Vector3 point;
            if (settings.preferTurretAimPoint
                && (TryGetBoundsFromColliders(targetRoot.turretColliders, out bounds)
                    || TryGetBoundsFromArmorMaps(targetRoot.armorMaps, ArmorMap.ArmorZone.Turret, out bounds)))
            {
                point = bounds.center;
            }
            else if ((targetRoot.health != null && TryGetBoundsFromColliders(targetRoot.health.colliders, out bounds))
                     || TryGetBoundsFromArmorMaps(targetRoot.armorMaps, ArmorMap.ArmorZone.Auto, out bounds))
            {
                point = bounds.center;
            }
            else if (targetRoot.robotHullRotation != null)
            {
                point = targetRoot.robotHullRotation.transform.position;
            }
            else
            {
                point = targetRoot.transform.position + Vector3.up * settings.fallbackTargetHeight;
            }

            if (includeOffset)
            {
                Transform reference = GetMoveTransform(targetRoot);
                point += reference.right * _targetAimOffset.x;
                point += Vector3.up * _targetAimOffset.y;
                point += reference.forward * _targetAimOffset.z;
            }

            return point;
        }

        private Vector3 ApplyTargetLead(Vector3 aimPoint, BotCombatSettings settings)
        {
            if (!settings.leadMovingTargets || settings.leadPredictionMultiplier <= 0f)
            {
                return aimPoint;
            }

            if (_targetVelocity.sqrMagnitude <= 0.0001f)
            {
                return aimPoint;
            }

            float shellSpeed = GetShellSpeed();
            if (shellSpeed <= 0.001f)
            {
                return aimPoint;
            }

            Vector3 origin = GetAimOrigin();
            float distance = (aimPoint - origin).magnitude;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.001f)
            {
                return aimPoint;
            }

            float leadSeconds = Mathf.Clamp(distance / shellSpeed, 0f, settings.maxLeadSeconds);
            return aimPoint + _targetVelocity * leadSeconds * settings.leadPredictionMultiplier;
        }

        private bool HasLineOfFire(Vector3 targetPoint, VehicleRoot expectedTarget, BotCombatSettings settings)
        {
            Vector3 origin = GetAimOrigin();
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            if (!IsFinite(direction) || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.001f)
            {
                return false;
            }

            direction /= distance;
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                RaycastBuffer,
                distance + 0.25f,
                settings.lineOfSightMask,
                QueryTriggerInteraction.Ignore);

            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = RaycastBuffer[i].collider;
                if (hitCollider == null || IsUnderRoot(hitCollider.transform, _vehicleRoot.transform))
                {
                    continue;
                }

                float hitDistance = RaycastBuffer[i].distance;
                if (hitDistance < bestDistance)
                {
                    bestDistance = hitDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return true;
            }

            Collider bestCollider = RaycastBuffer[bestIndex].collider;
            if (VehicleColliderRegistry.TryGetRoot(bestCollider, out VehicleRoot hitRoot))
            {
                return hitRoot == expectedTarget;
            }

            return expectedTarget != null && IsUnderRoot(bestCollider.transform, expectedTarget.transform);
        }

        private bool TryGetBoundsFromColliders(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            if (colliders == null)
            {
                return false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private bool TryGetBoundsFromArmorMaps(ArmorMap[] armorMaps, ArmorMap.ArmorZone requiredZone, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            if (armorMaps == null)
            {
                return false;
            }

            for (int i = 0; i < armorMaps.Length; i++)
            {
                ArmorMap armorMap = armorMaps[i];
                if (armorMap == null)
                {
                    continue;
                }

                if (requiredZone != ArmorMap.ArmorZone.Auto && armorMap.ResolvedArmorZone != requiredZone)
                {
                    continue;
                }

                Collider targetCollider = armorMap.armorCollider;
                if (!IsUsableCollider(targetCollider))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private bool IsEnemyTarget(VehicleRoot targetRoot)
        {
            if (targetRoot == null || targetRoot == _vehicleRoot)
            {
                return false;
            }

            if (targetRoot.health != null && targetRoot.health.IsDead)
            {
                return false;
            }

            if (_vehicleRoot == null || _vehicleRoot.characterInit == null || targetRoot.characterInit == null)
            {
                return true;
            }

            MatchTeam localTeam = _vehicleRoot.characterInit.Team.Value;
            MatchTeam targetTeam = targetRoot.characterInit.Team.Value;
            return !MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam);
        }

        private Vector3 ResolveAimForward(Vector3 aimPoint)
        {
            Vector3 origin = GetAimOrigin();
            Vector3 forward = aimPoint - origin;
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                forward = _vehicleRoot.transform.forward;
            }

            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private Vector3 GetAimOrigin()
        {
            if (_vehicleRoot != null && _vehicleRoot.shooterNet != null && _vehicleRoot.shooterNet.muzzleTransform != null)
            {
                return _vehicleRoot.shooterNet.muzzleTransform.position;
            }

            if (_vehicleRoot != null && _vehicleRoot.weaponAimAtCamera != null && _vehicleRoot.weaponAimAtCamera.gun != null)
            {
                return _vehicleRoot.weaponAimAtCamera.gun.position;
            }

            return _vehicleRoot != null ? _vehicleRoot.transform.position : Vector3.zero;
        }

        private float GetShellSpeed()
        {
            if (_vehicleRoot != null && _vehicleRoot.shooterNet != null)
            {
                return Mathf.Max(0f, _vehicleRoot.shooterNet.projectileSpeed);
            }

            if (_vehicleRoot != null && _vehicleRoot.HasRuntimeStats)
            {
                return VehicleRuntimeStats.ResolveShellSpeed(_vehicleRoot.RuntimeStats.ShellSpeed);
            }

            return VehicleRuntimeStats.DefaultShellSpeed;
        }

        private float GetAcquireDistance(BotCombatSettings settings)
        {
            float viewRange = _vehicleRoot != null && _vehicleRoot.HasRuntimeStats
                ? VehicleRuntimeStats.ResolveViewRange(_vehicleRoot.RuntimeStats.ViewRange)
                : VehicleRuntimeStats.DefaultViewRange;

            float statDistance = viewRange * settings.viewRangeMultiplier;
            float floorDistance = settings.maxAcquireDistance * 0.65f;
            return Mathf.Clamp(Mathf.Max(statDistance, floorDistance), 1f, settings.maxAcquireDistance);
        }

        private static Vector3 BuildAimOffset(BotCombatSettings settings)
        {
            float radius = Mathf.Max(0f, settings.randomAimRadius);
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            return new Vector3(
                Random.Range(-radius, radius),
                Random.Range(-radius * 0.5f, radius * 0.5f),
                Random.Range(-radius, radius));
        }

        private static bool IsUsableCollider(Collider targetCollider)
        {
            return targetCollider != null
                   && targetCollider.enabled
                   && targetCollider.gameObject.activeInHierarchy;
        }

        private static Vector3 GetMovePosition(VehicleRoot root)
        {
            Transform moveTransform = GetMoveTransform(root);
            return moveTransform != null ? moveTransform.position : Vector3.zero;
        }

        private static Transform GetMoveTransform(VehicleRoot root)
        {
            if (root != null && root.objectMover != null)
            {
                return root.objectMover.transform;
            }

            return root != null ? root.transform : null;
        }

        private static bool IsUnderRoot(Transform transform, Transform root)
        {
            if (transform == null || root == null)
            {
                return false;
            }

            Transform current = transform;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                   && !float.IsNaN(value.y)
                   && !float.IsNaN(value.z)
                   && !float.IsInfinity(value.x)
                   && !float.IsInfinity(value.y)
                   && !float.IsInfinity(value.z);
        }
    }
}
