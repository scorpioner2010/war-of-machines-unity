using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class RobotMovementBody : MonoBehaviour
    {
        public const string MovementBodyTag = "MovementBody";

        [Header("Body Shape")]
        [Min(0f)] public float bodyHeightOffset = 0.9f;
        [Min(0.01f)] public float groundProbeRadius = 0.45f;
        [Min(0f)] public float groundProbeForwardOffset = 0.9f;
        [Min(0f)] public float groundProbeSideOffset = 0.55f;
        [Min(0.01f)] public float collisionCapsuleRadius = 0.5f;
        [Min(0.01f)] public float collisionCapsuleHeight = 1.3f;
        [Min(0f)] public float collisionSkinWidth = 0.05f;

        [Header("Layers")]
        public LayerMask groundMask = 1 << 3;
        public LayerMask collisionMask = 1 << 8;

        [Header("Debug")]
        public Color bodyColor = new Color(0.05f, 0.35f, 1f, 0.35f);

        public float GroundProbeForwardOffset => groundProbeForwardOffset > 0f
            ? groundProbeForwardOffset
            : Mathf.Max(0f, collisionCapsuleRadius * 1.5f);

        public float GroundProbeSideOffset => groundProbeSideOffset > 0f
            ? groundProbeSideOffset
            : Mathf.Max(0f, collisionCapsuleRadius);

        public float BodyHeightOffset => Mathf.Max(0f, bodyHeightOffset);
        public float GroundProbeRadius => Mathf.Max(0.01f, groundProbeRadius);
        public float CollisionSkinWidth => Mathf.Max(0.001f, collisionSkinWidth);

        public void GetCollisionCapsule(Vector3 position, out Vector3 pointA, out Vector3 pointB, out float radius)
        {
            radius = Mathf.Max(0.01f, collisionCapsuleRadius);
            float height = Mathf.Max(radius * 2f + 0.01f, collisionCapsuleHeight);
            float bottomY = position.y - BodyHeightOffset;
            pointA = new Vector3(position.x, bottomY + radius, position.z);
            pointB = new Vector3(position.x, bottomY + height - radius, position.z);
        }

        private void OnValidate()
        {
            bodyHeightOffset = Mathf.Max(0f, bodyHeightOffset);
            groundProbeRadius = Mathf.Max(0.01f, groundProbeRadius);
            groundProbeForwardOffset = Mathf.Max(0f, groundProbeForwardOffset);
            groundProbeSideOffset = Mathf.Max(0f, groundProbeSideOffset);
            collisionCapsuleRadius = Mathf.Max(0.01f, collisionCapsuleRadius);
            collisionCapsuleHeight = Mathf.Max(collisionCapsuleRadius * 2f + 0.01f, collisionCapsuleHeight);
            collisionSkinWidth = Mathf.Max(0f, collisionSkinWidth);
#if UNITY_EDITOR
            if (gameObject != null && gameObject.tag != MovementBodyTag)
            {
                gameObject.tag = MovementBodyTag;
            }
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = bodyColor;
            GetCollisionCapsule(transform.position, out Vector3 pointA, out Vector3 pointB, out float radius);
            Gizmos.DrawWireSphere(pointA, radius);
            Gizmos.DrawWireSphere(pointB, radius);
            Gizmos.DrawLine(pointA + Vector3.forward * radius, pointB + Vector3.forward * radius);
            Gizmos.DrawLine(pointA - Vector3.forward * radius, pointB - Vector3.forward * radius);
            Gizmos.DrawLine(pointA + Vector3.right * radius, pointB + Vector3.right * radius);
            Gizmos.DrawLine(pointA - Vector3.right * radius, pointB - Vector3.right * radius);

            Vector3 groundCenter = transform.position - transform.up * BodyHeightOffset;
            Gizmos.DrawWireCube(
                groundCenter,
                new Vector3(GroundProbeSideOffset * 2f, 0.03f, GroundProbeForwardOffset * 2f));
        }
    }
}
