using UnityEngine;

namespace Game.Scripts.UI.Helpers
{
    public class ClockwiseUiRotator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float rotationSpeed = 120f;

        private void Update()
        {
            transform.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
        }
    }
}
