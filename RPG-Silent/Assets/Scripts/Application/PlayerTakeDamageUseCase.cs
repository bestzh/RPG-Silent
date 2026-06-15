using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Application
{
    public class PlayerTakeDamageUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerTakeDamageUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int damage)
        {
            if (damage <= 0) return;
            _stats.TakeDamage(damage);
            Debug.Log($"[Player] 受伤 -{damage} HP。当前: {_stats.CurrentHealth}/{_stats.MaxHealth}");
        }
    }
}
