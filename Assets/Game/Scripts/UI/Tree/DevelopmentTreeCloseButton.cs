using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.UI.Tree
{
    public sealed class DevelopmentTreeCloseButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private DevelopmentTree developmentTree;

        private bool _isClosing;

        private void OnEnable()
        {
            _isClosing = false;
        }

        private void Update()
        {
            if (_isClosing || developmentTree == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            RectTransform buttonRect = transform as RectTransform;
            if (buttonRect == null || !RectTransformUtility.RectangleContainsScreenPoint(buttonRect, Input.mousePosition))
            {
                return;
            }

            Close();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Close();
        }

        private void Close()
        {
            if (_isClosing || developmentTree == null)
            {
                return;
            }

            _isClosing = true;
            developmentTree.ReturnToMainMenu();
        }
    }
}
