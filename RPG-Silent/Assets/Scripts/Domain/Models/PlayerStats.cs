using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 玩家核心数据模型。纯 C# 类，无 Unity 依赖，可直接单元测试。
    /// </summary>
    public class PlayerStats : IPlayerStatsReader
    {
        public int MaxHealth      { get; private set; }
        public int CurrentHealth  { get; private set; }
        public int Gold           { get; private set; }
        public int Exp            { get; private set; }
        public bool IsDead        => CurrentHealth <= 0;

        public event Action<int, int> OnHealthChanged;
        public event Action<int>      OnGoldChanged;
        public event Action<int>      OnExpChanged;

        public PlayerStats(int maxHealth)
        {
            MaxHealth     = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;
            CurrentHealth = System.Math.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHealth = System.Math.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            OnExpChanged?.Invoke(Exp);
        }

        public void Refresh()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnGoldChanged?.Invoke(Gold);
            OnExpChanged?.Invoke(Exp);
        }
    }
}
