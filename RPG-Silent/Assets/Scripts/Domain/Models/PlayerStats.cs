using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 玩家核心数据模型。纯 C# 类，无 Unity 依赖，可直接单元测试。
    /// </summary>
    public class PlayerStats : IPlayerStatsReader
    {
        public const int LevelCap = 50;
        private readonly int baseMaxHealth;
        private readonly float baseMaxEnergy;
        public int Level { get; private set; } = 1;
        public int ExpInLevel => Level >= LevelCap ? 0 : Exp - ExpAtLevel(Level);
        public int ExpToNextLevel => Level >= LevelCap ? 0 : Level * 100;
        public static int ExpAtLevel(int level) => (level - 1) * level * 50;
        public int MaxHealth      { get; private set; }
        public int CurrentHealth  { get; private set; }
        public int Gold           { get; private set; }
        public int Exp            { get; private set; }
        public bool IsDead        => CurrentHealth <= 0;

        public float MaxEnergy     { get; private set; }
        public float CurrentEnergy { get; private set; }

        public event Action<int, int>     OnHealthChanged;
        public event Action<int>          OnGoldChanged;
        public event Action<int>          OnExpChanged;
        public event Action<float, float> OnEnergyChanged;
        // One notification after the whole reward transaction; HP/energy are transient.
        public event Action OnProgressChanged;

        public PlayerStats(int maxHealth, float maxEnergy = 100f)
        {
            if (maxHealth <= 0 || maxHealth > int.MaxValue - LevelCap * 10 ||
                float.IsNaN(maxEnergy) || float.IsInfinity(maxEnergy) || maxEnergy <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            baseMaxHealth = maxHealth;
            baseMaxEnergy = maxEnergy;
            MaxHealth     = maxHealth;
            CurrentHealth = maxHealth;
            MaxEnergy     = maxEnergy;
            CurrentEnergy = maxEnergy;
        }

        public void ResetForNewGame()
        {
            Level = 1;
            MaxHealth = baseMaxHealth;
            MaxEnergy = baseMaxEnergy;
            CurrentHealth = MaxHealth;
            CurrentEnergy = MaxEnergy;
            Gold = 0;
            Exp = 0;
            Refresh();
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;
            CurrentHealth = System.Math.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void RestoreVitals()
        {
            CurrentHealth = MaxHealth;
            CurrentEnergy = MaxEnergy;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHealth = (int)Math.Min(MaxHealth, (long)CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void AddGold(int amount) => AddReward(amount, 0);
        public void AddExp(int amount) => AddReward(0, amount);

        public void AddReward(int gold, int exp)
        {
            int newGold = (int)Math.Min(int.MaxValue, (long)Gold + Math.Max(0, gold));
            int newExp = (int)Math.Min(int.MaxValue, (long)Exp + Math.Max(0, exp));
            if (newGold == Gold && newExp == Exp) return;
            bool alive = !IsDead;
            int oldHealthMax = MaxHealth;
            float oldEnergyMax = MaxEnergy;
            Gold = newGold;
            Exp = newExp;
            RecalculateLevel();
            // Preserve missing HP/energy. Earning EXP must not resurrect a dead player.
            if (alive) CurrentHealth += MaxHealth - oldHealthMax;
            CurrentEnergy += MaxEnergy - oldEnergyMax;
            Refresh();
            OnProgressChanged?.Invoke();
        }

        public void RestoreProgress(int gold, int totalExp)
        {
            if (gold < 0 || totalExp < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            Gold = gold;
            Exp = totalExp;
            RecalculateLevel();
            CurrentHealth = MaxHealth;
            CurrentEnergy = MaxEnergy;
            Refresh();
        }

        private void RecalculateLevel()
        {
            Level = 1;
            while (Level < LevelCap && Exp >= ExpAtLevel(Level + 1)) ++Level;
            MaxHealth = baseMaxHealth + (Level - 1) * 10;
            MaxEnergy = baseMaxEnergy + (Level - 1) * 5f;
        }

        public bool TrySpendEnergy(float cost)
        {
            if (cost <= 0f) return true;
            if (CurrentEnergy < cost) return false;

            CurrentEnergy -= cost;
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
            return true;
        }

        public void RecoverEnergy(float amount)
        {
            if (amount <= 0f || CurrentEnergy >= MaxEnergy) return;

            CurrentEnergy = System.Math.Min(MaxEnergy, CurrentEnergy + amount);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        }

        public void Refresh()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnGoldChanged?.Invoke(Gold);
            OnExpChanged?.Invoke(Exp);
            OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        }
    }
}
