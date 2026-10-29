using System;
using System.IO;

namespace RPGSilent.Domain
{
    public enum SpecializationDirection { None, BasicAttack, Skill, Defense, Summon }

    [Serializable]
    public sealed class SpecializationData
    {
        public SpecializationDirection direction;
        public int rank;
        // Survives reset so an old confirmation cannot purchase/refund twice.
        public long revision;
        public int investedGold;
        public int spentMaterials;
        public SpecializationData Copy() => (SpecializationData)MemberwiseClone();

        public static void Validate(SpecializationData data)
        {
            if (data == null || !Enum.IsDefined(typeof(SpecializationDirection), data.direction) ||
                data.rank < 0 || data.rank > SpecializationRules.MaxRank || data.revision < data.rank ||
                (data.direction == SpecializationDirection.None) != (data.rank == 0) ||
                data.investedGold != SpecializationRules.TotalGold(data.rank) ||
                data.spentMaterials != SpecializationRules.TotalMaterials(data.rank))
                throw new InvalidDataException("专精方向、投入记录或等级无效，原存档已保留。");
        }
    }

    /// <summary>Versioned growth rules. Existing class passives remain in force.</summary>
    public static class SpecializationRules
    {
        public const int MaxRank = 3;
        public const int ResetFee = 100;
        public static int RequiredLevel(int next) => ValidRank(next) ? next * 6 : 0;
        public static int GoldCost(int next) => ValidRank(next) ? next * 150 : 0;
        public static int MaterialCost(int next) => ValidRank(next) ? next * 2 : 0;
        public static int TotalGold(int rank) => ValidRank(rank) ? rank * (rank + 1) / 2 * 150 : 0;
        public static int TotalMaterials(int rank) => ValidRank(rank) ? rank * (rank + 1) : 0;
        public static int RefundGold(int investedGold) => (int)(Math.Max(0, (long)investedGold) * 80 / 100);
        static bool ValidRank(int rank) => rank >= 1 && rank <= MaxRank;
        static int Rank(int rank) => ValidRank(rank) ? rank : 0;
        public static float BasicDamage(int rank) => 1f + Rank(rank) * .06f;
        public static float BasicStagger(int rank) => 1f + Rank(rank) * .08f;
        public static float SkillDamage(int rank) => 1f + Rank(rank) * .06f;
        public static float SkillEnergy(int rank) => 1f - Rank(rank) * .05f;
        public static float BlockEnergy(int rank) => 1f - Rank(rank) * .1f;
        public static float ParryBonus(int rank) => Rank(rank) * .02f;
        public static float SummonDamage(int rank) => 1f + Rank(rank) * .08f;
        public static float SummonDuration(int rank) => Rank(rank) * 2f;
        public static string Name(SpecializationDirection direction) => direction switch
        {
            SpecializationDirection.BasicAttack => "连武专精",
            SpecializationDirection.Skill => "凝神专精",
            SpecializationDirection.Defense => "坚守专精",
            SpecializationDirection.Summon => "驭灵专精",
            _ => "未选择专精"
        };
        public static string Effect(SpecializationDirection direction, int rank)
        {
            int r = Rank(rank);
            return direction switch
            {
                SpecializationDirection.BasicAttack => $"普攻伤害 +{r * 6}% · 普攻硬直倍率 +{r * 8}%（含连段、蓄力、反击）",
                SpecializationDirection.Skill => $"已学技能伤害 +{r * 6}% · 施放能量消耗 -{r * 5}%（召唤物独立计算）",
                SpecializationDirection.Defense => $"普通格挡耗能 -{r * 10}% · 精准格挡窗口 {(.18f + r * .02f):0.00} 秒（不改变闪避与格挡承伤）",
                SpecializationDirection.Summon => $"兽灵伤害 +{r * 8}% · 召唤持续时间 +{r * 2} 秒（仅当前兽灵，与技能等级及装备乘算）",
                _ => "沿用原职业被动，尚无额外专精加成。"
            };
        }
    }
}
