using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class RobotFootAnimator : MonoBehaviour
    {
        public TankRoot playerRoot;
        
        public FootPlacer leftFoot;
        public FootPlacer rightFoot;
        
        public float inputThreshold = 0.01f;

        public float stepDistance = 1.1f;        // Дистанція переміщення стопи
        public float stepHeight = 0.5f;          // Підйом стопи під час кроку
        public float stepCycleDuration = 1f;     // Тривалість циклу ходьби

        public float turnLiftHeight = 0.2f;      // Підйом стопи при повороті
        public float turnStepDuration = 0.5f;    // Тривалість циклу поворотної анімації

        public float animTransitionSpeed = 5f;   // Швидкість переходу між анімаціями

        // Приватні змінні
        private float _walkPhase = 0f;            // Фаза ходьби [0,1)
        private float _turnTimer = 0f;            // Таймер циклу повороту
        private bool _isLeftTurningStep = true;   // Чи піднімається ліва стопа при повороті

        // Вагові коефіцієнти для анімацій (0 - нейтрально, 1 - повна анімація)
        private float _currentWalkWeight = 0f;
        private float _currentTurnWeight = 0f;
        
        private void Update()
        {
            Vector2 movementInput = playerRoot.inputManager.AnimMove;
        
            bool isWalking = Mathf.Abs(movementInput.y) > inputThreshold;
            bool isTurning = (!isWalking) && (Mathf.Abs(movementInput.x) > inputThreshold);
        
            if (isWalking)
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 1f, Time.deltaTime * animTransitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * animTransitionSpeed);
                // Оновлення фази ходьби (на основі напрямку вперед/назад)
                float direction = movementInput.y > 0f ? 1f : -1f;
                _walkPhase += direction * Time.deltaTime / stepCycleDuration;
                _walkPhase %= 1f;
                if (_walkPhase < 0f) _walkPhase += 1f;
            }
            else if (isTurning)
            {
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 0f, Time.deltaTime * animTransitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 1f, Time.deltaTime * animTransitionSpeed);
                // Оновлення таймера повороту
                _turnTimer += Time.deltaTime;
                if (_turnTimer >= turnStepDuration)
                {
                    _turnTimer -= turnStepDuration;
                    _isLeftTurningStep = !_isLeftTurningStep;
                }
            }
            else
            {
                // Немає вводу – повертаємось до нейтрального положення
                _currentWalkWeight = Mathf.Lerp(_currentWalkWeight, 0f, Time.deltaTime * animTransitionSpeed);
                _currentTurnWeight = Mathf.Lerp(_currentTurnWeight, 0f, Time.deltaTime * animTransitionSpeed);
            }

            // --- Обчислення анімації ходьби ---
            Vector3 leftWalkOffset = ComputeWalkOffset(_walkPhase);
            Vector3 rightWalkOffset = ComputeWalkOffset((_walkPhase + 0.5f) % 1f);
            float leftWalkBlend = ComputeWalkBlend(_walkPhase);
            float rightWalkBlend = ComputeWalkBlend((_walkPhase + 0.5f) % 1f);

            // --- Обчислення анімації повороту ---
            Vector3 turnOffset = new Vector3(0f, turnLiftHeight, 0f);
            float turnBlend = ComputeTurnBlend();  // спільний для обох, але застосовується до однієї стопи
            float leftTurnBlend = _isLeftTurningStep ? turnBlend : 1f;
            float rightTurnBlend = !_isLeftTurningStep ? turnBlend : 1f;
            Vector3 leftTurnOffset = turnOffset;
            Vector3 rightTurnOffset = turnOffset;

            // --- Остаточне змішування (нейтральний стан – offset = 0, blend = 1) ---
            // Формула: final = neutral*(1 - (walkW + turnW)) + walk*walkW + turn*turnW
            Vector3 neutralOffset = Vector3.zero;
            float neutralBlend = 1f; // повний контакт із землею

            Vector3 leftFinalOffset = neutralOffset * (1f - (_currentWalkWeight + _currentTurnWeight))
                                      + leftWalkOffset * _currentWalkWeight
                                      + leftTurnOffset * _currentTurnWeight;
            Vector3 rightFinalOffset = neutralOffset * (1f - (_currentWalkWeight + _currentTurnWeight))
                                       + rightWalkOffset * _currentWalkWeight
                                       + rightTurnOffset * _currentTurnWeight;

            float leftFinalBlend = (neutralBlend * (1f - (_currentWalkWeight + _currentTurnWeight))
                                    + leftWalkBlend * _currentWalkWeight
                                    + leftTurnBlend * _currentTurnWeight);
            float rightFinalBlend = (neutralBlend * (1f - (_currentWalkWeight + _currentTurnWeight))
                                     + rightWalkBlend * _currentWalkWeight
                                     + rightTurnBlend * _currentTurnWeight);

            // Передача зсувів до компонентів FootPlacer
            leftFoot.SetTargetOffset(leftFinalOffset, leftFinalBlend);
            rightFoot.SetTargetOffset(rightFinalOffset, rightFinalBlend);
        }

        // --- Функції для анімації ходьби ---
        private Vector3 ComputeWalkOffset(float phase)
        {
            float halfStep = stepDistance * 0.5f;
            float horizontal = 0f;
            float vertical = 0f;

            if (phase < 0.5f)
            {
                float t = phase / 0.5f;
                horizontal = Mathf.Lerp(halfStep, -halfStep, t);
            }
            else
            {
                float t = (phase - 0.5f) / 0.5f;
                horizontal = Mathf.Lerp(-halfStep, halfStep, t);
                vertical = Mathf.Sin(Mathf.PI * t) * stepHeight;
            }
            // Зсув: по вертикалі (Y) і вздовж осі Z (горизонтальний)
            return new Vector3(0f, vertical, horizontal);
        }

        private float ComputeWalkBlend(float phase)
        {
            if (phase < 0.45f)
                return 1f;
            else if (phase < 0.55f)
                return Mathf.Lerp(1f, 0f, (phase - 0.45f) / 0.1f);
            else if (phase < 0.85f)
                return 0f;
            else
                return Mathf.Lerp(0f, 1f, (phase - 0.85f) / 0.15f);
        }

        // --- Функція для анімації повороту ---
        private float ComputeTurnBlend()
        {
            float t = _turnTimer / turnStepDuration;
            return t < 0.5f ? Mathf.Lerp(1f, 0f, t * 2f) : Mathf.Lerp(0f, 1f, (t - 0.5f) * 2f);
        }
    }
}