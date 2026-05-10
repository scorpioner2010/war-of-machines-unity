using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class BoodyNormalRotator : MonoBehaviour
    {
        public float raycastLength = 2f;
        public float surfaceCheckRadius = 1f;
        public int raysCount = 8;
        public float alignmentPercentage = 50f;
        public LayerMask surfaceLayer;
        public float lerpSpeed = 10f;

        private void Update()
        {
            AlignWithGround();
        }

        private void AlignWithGround()
        {
            Vector3 averageNormal = Vector3.zero;
            int validHits = 0;

            for (int i = 0; i < raysCount; i++)
            {
                float angle = (i / (float)raysCount) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * surfaceCheckRadius;
                Vector3 rayOrigin = transform.position + offset;

                if (Physics.Raycast(rayOrigin, -Vector3.up, out RaycastHit hit, raycastLength, surfaceLayer))
                {
                    averageNormal += hit.normal;
                    validHits++;
                }
            }

            if (validHits > 0)
            {
                averageNormal /= validHits;
            }
            else
            {
                return;
            }

            Vector3 flatUp = Vector3.up;

            Vector3 finalNormal = Vector3.Lerp(flatUp, averageNormal, alignmentPercentage / 100f).normalized;

            Vector3 forwardDirection = transform.parent ? transform.parent.forward : transform.forward;
            forwardDirection = Vector3.ProjectOnPlane(forwardDirection, finalNormal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, finalNormal);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        }
    }
}
