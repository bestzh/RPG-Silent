using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Application
{
    public class PlayerHealUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerHealUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int amount)
        {
            if (amount <= 0) return;
            _stats.Heal(amount);
            Debug.Log($"[Player] 恢复 +{amount} HP。当前: {_stats.CurrentHealth}/{_stats.MaxHealth}");
        }
    }
}
