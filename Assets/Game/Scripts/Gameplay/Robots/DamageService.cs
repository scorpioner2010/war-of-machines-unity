using Game.Scripts.Networking.Lobby;

namespace Game.Scripts.Gameplay.Robots
{
    public static class DamageService
    {
        public static void ApplyVehicleShotDamage(VehicleRoot attackerRoot, VehicleRoot targetRoot, VehicleHealth targetHealth, float damage)
        {
            bool wasDead = targetHealth.IsDead;
            bool willKill = !wasDead && targetHealth.Current > 0f && targetHealth.Current - damage <= 0f;

            if (GameplaySpawner.In != null)
            {
                GameplaySpawner.In.RecordHitStats(attackerRoot, targetRoot, damage, willKill);
            }

            targetHealth.ServerApplyDamage(damage);
        }
    }
}
