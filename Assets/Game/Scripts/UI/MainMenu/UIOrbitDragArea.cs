using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.UI.MainMenu
{
    public class UIOrbitDragArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public bool IsDragging { get; private set; }
        public Vector2 ConsumeDelta()
        {
            Vector2 d = _delta;
            _delta = Vector2.zero;
            return d;
        }

        private Vector2 _delta;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsDragging = true;
                _delta = Vector2.zero;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsDragging = false;
                _delta = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsDragging && eventData.button == PointerEventData.InputButton.Left)
            {
                _delta += eventData.delta;
            }
        }
    }
}