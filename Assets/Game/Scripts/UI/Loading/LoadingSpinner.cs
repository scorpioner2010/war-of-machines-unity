using UnityEngine;

namespace Game.Scripts.UI.Loading
{
    public class LoadingSpinner : MonoBehaviour
    {
        public float rotationSpeed = -600;
        private RectTransform _rectTransform;
        private void Awake() => _rectTransform = GetComponent<RectTransform>();
        private void Update()
        {
            _rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }
}