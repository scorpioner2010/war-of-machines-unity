using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEngine;

public class DeathLogic : MonoBehaviour
{
    public Collider[] colliders;
    public TankRoot tankRoot;
    public GameObject[] forTurnOff;

    private void Start()
    {
        tankRoot.health.onDeath.AddListener(Death);
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
            if (c == null) continue;
            if (c.gameObject.layer != armorLayer) continue;

            bool isConvex = true;

            if (c is MeshCollider mc)
                isConvex = mc.convex; // тільки якщо Convex = true

            if (isConvex)
                list.Add(c);
        }

        colliders = list.ToArray();
    }


    private void Death()
    {
        tankRoot.inputManager.SetControlsBlocked(true);
        
        foreach (Collider coll in colliders)
        {
            coll.transform.parent = null;
            coll.AddComponent<Rigidbody>();
            coll.enabled = true;

            if (coll.TryGetComponent(out MeshRenderer obj))
            {
                obj.enabled = true;
            }
        }

        foreach (GameObject obj in forTurnOff)
        {
            obj.SetActive(false);
        }
    }
}