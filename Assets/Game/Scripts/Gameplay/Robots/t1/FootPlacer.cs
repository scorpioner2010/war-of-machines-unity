using UnityEngine;

namespace Game.Scripts.Gameplay.Robots.t1
{
    public class FootPlacer : MonoBehaviour 
    { 
        public LayerMask groundLayer;          // Шар, який представляє землю 
        public float groundCheckDistance = 1f;   // Дистанція для рейткасту 
        public float footOffset = 0.05f;         // Відступ стопи від землі 

        private Vector3 _neutralLocalPos;       // Нейтральна локальна позиція стопи 
        private Transform _parentTransform;     // Батьківський об’єкт (персонаж) 

        private void Start() 
        { 
            _parentTransform = transform.parent; 
            _neutralLocalPos = transform.localPosition; 
        } 

        // Отримуємо зсув стопи та blend-фактор (від 0 до 1) для коригування висоти стопи згідно землі 
        public void SetTargetOffset(Vector3 offset, float groundBlend) 
        {
            if (_parentTransform == null)
            {
                return;
            }
            
            // Обчислюємо бажану світову позицію (нейтральна позиція + зсув) 
            Vector3 targetWorldPos = _parentTransform.TransformPoint(_neutralLocalPos + offset); 
            // Рейткаст зверху вниз 
            Vector3 rayOrigin = targetWorldPos + Vector3.up * 0.5f; 
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) 
            {
                float groundY = hit.point.y;
                targetWorldPos.y = Mathf.Lerp(targetWorldPos.y, groundY + footOffset, groundBlend);
            }
            else
            {
                targetWorldPos.y = Mathf.Lerp(targetWorldPos.y, _parentTransform.position.y + footOffset, groundBlend);
            }
            // Перетворюємо позицію назад у локальні координати
            transform.localPosition = _parentTransform.InverseTransformPoint(targetWorldPos);
        }
    }
}