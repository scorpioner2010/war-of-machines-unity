using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using NaughtyAttributes;
using UnityEngine;

public class DeathLogic : MonoBehaviour, IVehicleRootAware
{
    private const string DeathDebrisLayerName = "Chassis";

    public Collider[] colliders;
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

    [Button]
    private void FindArmorColliders()
    {
        List<Collider> list = new List<Collider>();
        Collider[] all = GetComponentsInChildren<Collider>(true);
        int armorLayer = LayerMask.NameToLayer("Armor");

        foreach (Collider c in all)
        {
            if (c == null)
            {
                continue;
            }

            if (c.gameObject.layer != armorLayer)
            {
                continue;
            }

            bool isConvex = true;

            if (c is MeshCollider mc)
            {
                isConvex = mc.convex;
            }

            if (isConvex)
            {
                list.Add(c);
            }
        }

        colliders = list.ToArray();
    }


    private void Death()
    {
        if (vehicleRoot != null && vehicleRoot.inputManager != null)
        {
            vehicleRoot.inputManager.SetControlsBlocked(true);
        }

        int debrisLayer = ResolveDeathDebrisLayer();
        foreach (Collider coll in colliders)
        {
            if (coll == null)
            {
                continue;
            }

            coll.transform.parent = null;
            SetLayerRecursively(coll.transform, debrisLayer);
            PrepareColliderForDynamicRigidbody(coll);

            if (!coll.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody = coll.gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = false;
            coll.enabled = true;

            if (coll.TryGetComponent(out MeshRenderer obj))
            {
                obj.enabled = true;
            }
        }

        foreach (GameObject obj in forTurnOff)
        {
            if (obj != null)
            {
                obj.SetActive(false);
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

    private static void PrepareColliderForDynamicRigidbody(Collider selectedCollider)
    {
        MeshCollider[] meshColliders = selectedCollider.GetComponents<MeshCollider>();

        for (int i = 0; i < meshColliders.Length; i++)
        {
            MeshCollider meshCollider = meshColliders[i];

            if (meshCollider == null)
            {
                continue;
            }

            if (meshCollider != selectedCollider)
            {
                meshCollider.enabled = false;
                continue;
            }

            if (!meshCollider.convex)
            {
                meshCollider.convex = true;
            }
        }
    }
}
