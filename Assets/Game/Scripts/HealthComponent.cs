using UnityEngine;

public class HealthComponent : MonoBehaviour//, IDamageable
{
    public int maxHP = 100;
    public int currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        // TODO: SyncVar/події смерті/ефекти
        if (currentHP == 0)
        {
            // TODO: Die()
        }
    }
}