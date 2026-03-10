using Game.Scripts.Gameplay.Robots;
using UnityEngine;

public class ArmorTest : MonoBehaviour
{
    public Camera cam;
    public LayerMask hitMask;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, hitMask))
            {
                ArmorMap map = hit.collider.GetComponentInParent<ArmorMap>();
                if (map != null)
                {
                    map.TryGetArmorLoS(hit, ray.direction, 5f, out var baseMm, out var losMm);
                }
            }
        }
    }
}
