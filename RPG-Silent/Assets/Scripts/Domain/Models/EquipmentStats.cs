using System;

namespace RPGSilent.Domain
{
    /// <summary>Shared effective values for gameplay, inventory comparisons and forge quotes.</summary>
    public static class EquipmentStats
    {
        public const int MaxArmorLevel = 5;
        public static int BaseValue(ItemDefinition item, ItemAffixStat stat) => item == null ? 0 : stat switch
        {
            ItemAffixStat.Health => item.Health,
            ItemAffixStat.Energy => item.Energy,
            ItemAffixStat.AttackPercent => item.AttackPercent,
            ItemAffixStat.DefensePercent => item.DefensePercent,
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };
        public static int UpgradeBonus(ItemDefinition item, int level, ItemAffixStat stat)
        {
            if (level < 0 || level > MaxArmorLevel) throw new ArgumentOutOfRangeException(nameof(level));
            if (item == null || item.Kind != ItemKind.Equipment) return 0;
            // Round the total bonus once, rather than compounding rounded previous levels.
            return (int)(((long)BaseValue(item, stat) * level + 9) / 10);
        }
        public static int Total(ItemDefinition item, ItemStackData instance, ItemAffixStat stat) =>
            BaseValue(item, stat) + UpgradeBonus(item, instance?.upgradeLevel ?? 0, stat) + (instance?.Bonus(stat) ?? 0);
    }
}
