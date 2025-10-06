using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t2
{
    public class CaterpillarBoneAdjuster : MonoBehaviour
    {
        public float rayHeight = 1f;
        public float rayDistance = 2f;
        public LayerMask groundLayer;
        private float _lerpSpeed;
        public Vector3 positionOffset = Vector3.zero;
        private Vector3 _initialLocalPos;

        private void Start()
        {
            // Запам'ятовуємо початкову локальну позицію кістки
            _initialLocalPos = transform.localPosition;
        }

        private void Update()
        {
            // 1. Отримуємо базову глобальну позицію кістки з початкових локальних координат (якщо є батьківський об'єкт)
            Vector3 baselineGlobalPos = transform.parent 
                ? transform.parent.TransformPoint(_initialLocalPos) 
                : _initialLocalPos;

            // 2. Визначаємо точку запуску raycast – на rayHeight вище поточної глобальної позиції кістки
            Vector3 rayOrigin = transform.position + Vector3.up * rayHeight;
            Ray ray = new Ray(rayOrigin, Vector3.down);

            // 3. За замовчуванням бажана глобальна Y – це значення з базової позиції
            float targetGlobalY = baselineGlobalPos.y;

            // 4. Виконуємо raycast для визначення бажаної глобальної Y (точка зіткнення з землею)
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundLayer))
            {
                // Дозволяємо підйом тільки якщо Y точки зіткнення перевищує базову позицію
                float hitY = hit.point.y;
                if (hitY > baselineGlobalPos.y)
                {
                    targetGlobalY = hitY;
                }
            }

            // 5. Формуємо цільову глобальну позицію: базові глобальні X та Z, бажане Y
            Vector3 targetGlobalPos = new Vector3(baselineGlobalPos.x, targetGlobalY, baselineGlobalPos.z);
            // Додаємо додатковий офсет
            targetGlobalPos += positionOffset;

            // 6. Перетворюємо цільову глобальну позицію назад у локальні координати батьківського об'єкта (якщо є)
            Vector3 targetLocalPos = transform.parent 
                ? transform.parent.InverseTransformPoint(targetGlobalPos) 
                : targetGlobalPos;

            // 7. Лерпимо поточну локальну позицію до цільової позиції
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * _lerpSpeed);
        }
    }
}