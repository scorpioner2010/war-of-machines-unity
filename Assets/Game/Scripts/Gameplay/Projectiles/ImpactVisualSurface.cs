using Game.Scripts.Gameplay.Robots;
using UnityEngine;

public sealed class ImpactVisualSurface : MonoBehaviour
{
    [Header("Physics hit source")]
    [SerializeField] private Collider physicsCollider;

    [Header("Visible surface")]
    [SerializeField] private MeshCollider[] visualMeshColliders = System.Array.Empty<MeshCollider>();
    [SerializeField] private Renderer[] visualRenderers = System.Array.Empty<Renderer>();

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    private void Register()
    {
        VehicleColliderRegistry.RegisterImpactVisualSurface(
            physicsCollider,
            visualMeshColliders,
            visualRenderers);
    }

    private void Unregister()
    {
        VehicleColliderRegistry.UnregisterImpactVisualSurface(physicsCollider);
    }
}
