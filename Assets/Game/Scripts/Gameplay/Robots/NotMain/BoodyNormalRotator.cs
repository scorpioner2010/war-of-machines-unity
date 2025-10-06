using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class BoodyNormalRotator : MonoBehaviour
    {
        public float raycastLength = 2f; // Довжина променя
        public float surfaceCheckRadius = 1f; // Радіус обчислення поверхні
        public int raysCount = 8; // Кількість променів для усереднення
        public float alignmentPercentage = 50f; // Відсоток вирівнювання (0-100)
        public LayerMask surfaceLayer; // Шари для перевірки поверхні
        public float lerpSpeed = 10f; // Швидкість інтерполяції

        private void Update()
        {
            AlignWithGround();
        }

        private void AlignWithGround()
        {
            Vector3 averageNormal = Vector3.zero;
            int validHits = 0;

            // Створюємо кілька променів навколо об'єкта
            for (int i = 0; i < raysCount; i++)
            {
                float angle = (i / (float)raysCount) * Mathf.PI * 2f; // Рівномірний розподіл кутів
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * surfaceCheckRadius;
                Vector3 rayOrigin = transform.position + offset;

                if (Physics.Raycast(rayOrigin, -Vector3.up, out RaycastHit hit, raycastLength, surfaceLayer))
                {
                    averageNormal += hit.normal;
                    validHits++;
                }
            }

            // Якщо знайшли нормалі, то усереднюємо їх
            if (validHits > 0)
            {
                averageNormal /= validHits; // Усереднена нормаль
            }
            else
            {
                return; // Виходимо, якщо нічого не знайдено
            }

            // Отримуємо стандартний вектор "вгору" (тобто, якщо поверхня була б рівною)
            Vector3 flatUp = Vector3.up;

            // Вираховуємо фінальну нормаль між ідеальним "вгору" і середньостатистичною
            Vector3 finalNormal = Vector3.Lerp(flatUp, averageNormal, alignmentPercentage / 100f).normalized;

            // Визначаємо forward з parent, щоб об'єкт завжди дивився вперед
            Vector3 forwardDirection = transform.parent ? transform.parent.forward : transform.forward;
            // Перепроєктовуємо forwardDirection на площину, задану finalNormal
            forwardDirection = Vector3.ProjectOnPlane(forwardDirection, finalNormal).normalized;

            // Створюємо нову цільову ротацію
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, finalNormal);

            // Плавно застосовуємо ротацію
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        }
    }
}