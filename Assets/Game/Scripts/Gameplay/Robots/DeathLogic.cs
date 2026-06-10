using Game.Scripts.Gameplay.Robots;
using Game.Scripts.World.Maps;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathLogic : MonoBehaviour, IVehicleRootAware
{
    [System.Serializable]
    public sealed class DetachableVisual
    {
        public GameObject root;
        public Collider collider;
        public Rigidbody rigidbody;
        public Renderer renderer;
        public MeshCollider meshCollider;
    }

    private const string DeathDebrisLayerName = "Chassis";

    public DetachableVisual[] detachableVisuals = System.Array.Empty<DetachableVisual>();
    public Collider[] colliders;
    public Collider[] collidersToDisableOnDeath;
    public Rigidbody[] debrisRigidbodies;
    public Renderer[] debrisRenderers;
    public MeshCollider[] debrisMeshColliders;
    public VehicleRoot vehicleRoot;
    public GameObject[] forTurnOff;

    public void SetVehicleRoot(VehicleRoot root)
    {
        vehicleRoot = root;
    }

    private void Start()
    {
        if (vehicleRoot == null || vehicleRoot.health == null)
        {
            enabled = false;
            return;
        }

        vehicleRoot.health.onDeath.AddListener(Death);
    }

    private void OnDestroy()
    {
        if (vehicleRoot != null && vehicleRoot.health != null)
        {
            vehicleRoot.health.onDeath.RemoveListener(Death);
        }
    }

    [Button]
    private void TurnOffConvex()
    {
        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private void Death()
    {
        if (vehicleRoot != null && vehicleRoot.inputManager != null)
        {
            vehicleRoot.inputManager.SetControlsBlocked(true);
        }

        int debrisLayer = ResolveDeathDebrisLayer();
        DisableConfiguredColliders(collidersToDisableOnDeath);
        Scene mapScene = MapScopedObjectRegistry.ResolveMapScene(GetOwningScene());

        if (detachableVisuals != null && detachableVisuals.Length > 0)
        {
            DetachConfiguredVisuals(mapScene, debrisLayer);
        }
        else
        {
            DetachLegacyDebris(mapScene, debrisLayer);
        }

        if (forTurnOff == null)
        {
            return;
        }

        foreach (GameObject obj in forTurnOff)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    private void DetachConfiguredVisuals(Scene mapScene, int debrisLayer)
    {
        for (int i = 0; i < detachableVisuals.Length; i++)
        {
            DetachableVisual visual = detachableVisuals[i];
            if (visual == null || visual.root == null)
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has an unconfigured detachable visual at index {i}.", this);
                continue;
            }

            if (visual.collider == null || visual.rigidbody == null)
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has no collider or Rigidbody configured for {visual.root.name}.", this);
                continue;
            }

            if (!IsConfiguredForDynamicRigidbody(visual.collider, visual.meshCollider))
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has non-convex debris MeshCollider {visual.collider.name}. Configure it as convex in the prefab before using it with a dynamic Rigidbody.", this);
                continue;
            }

            visual.root.transform.SetParent(null, true);
            MapScopedObjectRegistry.Register(mapScene, visual.root);
            MapScopedObjectRegistry.MoveRootToScene(mapScene, visual.root);
            SetLayerRecursively(visual.root.transform, debrisLayer);

            visual.rigidbody.isKinematic = false;
            visual.collider.enabled = true;

            if (visual.renderer != null)
            {
                visual.renderer.enabled = true;
            }
        }
    }

    private void DetachLegacyDebris(Scene mapScene, int debrisLayer)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider coll = colliders[i];
            if (coll == null)
            {
                continue;
            }

            Rigidbody rigidbody = debrisRigidbodies != null && i < debrisRigidbodies.Length
                ? debrisRigidbodies[i]
                : null;
            if (rigidbody == null)
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has no configured debris Rigidbody for {coll.name}.", this);
                continue;
            }

            MeshCollider meshCollider = debrisMeshColliders != null && i < debrisMeshColliders.Length
                ? debrisMeshColliders[i]
                : null;
            if (!IsConfiguredForDynamicRigidbody(coll, meshCollider))
            {
                Debug.LogError($"{nameof(DeathLogic)} on {name} has non-convex debris MeshCollider {coll.name}. Configure it as convex in the prefab before using it with a dynamic Rigidbody.", this);
                continue;
            }

            GameObject debrisObject = coll.gameObject;
            coll.transform.SetParent(null, true);
            MapScopedObjectRegistry.Register(mapScene, debrisObject);
            MapScopedObjectRegistry.MoveRootToScene(mapScene, debrisObject);
            SetLayerRecursively(coll.transform, debrisLayer);

            rigidbody.isKinematic = false;
            coll.enabled = true;

            Renderer obj = debrisRenderers != null && i < debrisRenderers.Length
                ? debrisRenderers[i]
                : null;
            if (obj != null)
            {
                obj.enabled = true;
            }
        }
    }

    private Scene GetOwningScene()
    {
        if (vehicleRoot != null)
        {
            Scene vehicleScene = vehicleRoot.gameObject.scene;
            if (vehicleScene.IsValid())
            {
                return vehicleScene;
            }
        }

        return gameObject.scene;
    }

    private static void DisableConfiguredColliders(Collider[] configuredColliders)
    {
        if (configuredColliders == null)
        {
            return;
        }

        for (int i = 0; i < configuredColliders.Length; i++)
        {
            Collider collider = configuredColliders[i];
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private static int ResolveDeathDebrisLayer()
    {
        int layer = LayerMask.NameToLayer(DeathDebrisLayerName);
        if (layer >= 0)
        {
            return layer;
        }

        return LayerMask.NameToLayer("Ignore Raycast");
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private static bool IsConfiguredForDynamicRigidbody(Collider collider, MeshCollider configuredMeshCollider)
    {
        MeshCollider meshCollider = configuredMeshCollider;
        if (meshCollider == null)
        {
            meshCollider = collider as MeshCollider;
        }

        if (meshCollider == null)
        {
            return true;
        }

        return meshCollider.convex;
    }
}
