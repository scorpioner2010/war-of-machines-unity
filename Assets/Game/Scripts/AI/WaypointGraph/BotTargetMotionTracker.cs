using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotTargetMotionTracker
    {
        private Vector3 _lastTargetPosition;
        private Vector3 _targetVelocity;
        private float _lastTargetSampleTime;
        private bool _hasTargetPositionSample;

        public void Start(VehicleRoot targetRoot, float now)
        {
            _targetVelocity = Vector3.zero;
            _lastTargetPosition = BotCombatUtility.GetMovePosition(targetRoot);
            _lastTargetSampleTime = now;
            _hasTargetPositionSample = true;
        }

        public void Reset()
        {
            _targetVelocity = Vector3.zero;
            _lastTargetPosition = Vector3.zero;
            _lastTargetSampleTime = 0f;
            _hasTargetPositionSample = false;
        }

        public void Tick(VehicleRoot targetRoot, BotCombatSettings settings, float now)
        {
            if (targetRoot == null)
            {
                Reset();
                return;
            }

            Vector3 targetPosition = BotCombatUtility.GetMovePosition(targetRoot);
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

        public Vector3 ApplyTargetLead(VehicleRoot shooterRoot, Vector3 aimPoint, BotCombatSettings settings)
        {
            if (!settings.leadMovingTargets || settings.leadPredictionMultiplier <= 0f)
            {
                return aimPoint;
            }

            if (_targetVelocity.sqrMagnitude <= 0.0001f)
            {
                return aimPoint;
            }

            float shellSpeed = BotCombatUtility.GetShellSpeed(shooterRoot);
            if (shellSpeed <= 0.001f)
            {
                return aimPoint;
            }

            Vector3 origin = BotCombatUtility.GetAimOrigin(shooterRoot);
            float distance = (aimPoint - origin).magnitude;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0.001f)
            {
                return aimPoint;
            }

            float leadSeconds = Mathf.Clamp(distance / shellSpeed, 0f, settings.maxLeadSeconds);
            return aimPoint + _targetVelocity * leadSeconds * settings.leadPredictionMultiplier;
        }
    }
}
