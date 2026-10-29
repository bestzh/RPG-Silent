using System;

namespace RPGSilent.Domain
{
    public enum EquipmentSpecialEffect { None, ParryEnergy, HeavyShock, SpellChain, SpiritBond }

    public static class EquipmentEffectRules
    {
        public const int EffectCount = 5;
        public const float SpiritDuration = 8f;
        public static bool Valid(EquipmentSpecialEffect effect) => (int)effect > 0 && (int)effect < EffectCount;
        public static bool ValidLevel(int level) => level == 1 || level == 2;
        public static int Level(ItemDefinition item) => item != null && Valid(item.SpecialEffect) &&
            (item.Kind is ItemKind.Weapon or ItemKind.Equipment) && (item.Rarity is ItemRarity.Legendary or ItemRarity.Mythic) ? item.Rarity == ItemRarity.Mythic ? 2 : 1 : 0;
        public static string Name(EquipmentSpecialEffect effect) => effect switch
        {
            EquipmentSpecialEffect.ParryEnergy => "精准回能", EquipmentSpecialEffect.HeavyShock => "重击冲击",
            EquipmentSpecialEffect.SpellChain => "技能连锁", EquipmentSpecialEffect.SpiritBond => "兽灵共鸣", _ => "无特殊效果"
        };
        public static float Cooldown(EquipmentSpecialEffect effect) => effect switch
        {
            EquipmentSpecialEffect.ParryEnergy => 6f, EquipmentSpecialEffect.HeavyShock => 8f,
            EquipmentSpecialEffect.SpellChain => 10f, EquipmentSpecialEffect.SpiritBond => 15f, _ => 0f
        };
        public static int ParryEnergy(int level) => level == 2 ? 18 : level == 1 ? 12 : 0;
        public static int TargetLimit(EquipmentSpecialEffect effect) => effect == EquipmentSpecialEffect.HeavyShock ? 3 : effect == EquipmentSpecialEffect.SpellChain ? 2 : 0;
        public static float Radius(EquipmentSpecialEffect effect) => effect == EquipmentSpecialEffect.HeavyShock ? 3.2f : effect == EquipmentSpecialEffect.SpellChain ? 6f : 0f;
        public static int Damage(int baseDamage, EquipmentSpecialEffect effect, int level)
        {
            if (baseDamage <= 0 || !ValidLevel(level)) return 0;
            int percent = effect switch
            {
                EquipmentSpecialEffect.HeavyShock => level == 2 ? 50 : 35,
                EquipmentSpecialEffect.SpellChain => level == 2 ? 35 : 25,
                EquipmentSpecialEffect.SpiritBond => level == 2 ? 140 : 125, _ => 0
            };
            return percent == 0 ? 0 : (int)Math.Min(int.MaxValue, Math.Max(1, ((long)baseDamage * percent + 50) / 100));
        }
        public static string Description(EquipmentSpecialEffect effect, int level)
        {
            if (!ValidLevel(level) || !Valid(effect)) return "这件装备没有特殊效果。";
            string rule = effect switch
            {
                EquipmentSpecialEffect.ParryEnergy => $"精准格挡成功时，额外恢复 {ParryEnergy(level)} 点能量。普通格挡和闪避不触发。",
                EquipmentSpecialEffect.HeavyShock => $"双手剑／斧的连段末击，或双手剑至少半蓄力攻击命中后，对目标周围 3.2 米内至多 3 名其他敌人造成该次实际伤害的 {(level == 2 ? 50 : 35)}%，冲击硬直系数 1.5。",
                EquipmentSpecialEffect.SpellChain => $"技能命中后，对目标周围 6 米内至多 2 名其他敌人造成该次实际伤害的 {(level == 2 ? 35 : 25)}%。普攻不触发。",
                EquipmentSpecialEffect.SpiritBond => $"成功召唤兽灵后，接下来 8 秒当前唯一兽灵伤害 +{(level == 2 ? 40 : 25)}%。仅御兽师的召唤生效；重复召唤不会刷新剩余强化时间。", _ => ""
            };
            return rule + $"\n冷却 {Cooldown(effect):0} 秒。" + (effect is EquipmentSpecialEffect.HeavyShock or EquipmentSpecialEffect.SpellChain ?
                "每次攻击至多触发一次，优先最近目标，不能穿墙；额外伤害不再触发装备、职业连锁或命中状态。" : "") +
                "\n同类只取最高品质，不叠加；仅当前武器和已穿戴防具饰品生效。卸下立即失效，再装备不刷新本场冷却。死亡、结算、切场景和读档清空临时状态。";
        }
    }
    // Scene-local timers are intentionally excluded from durable character progress.
    public sealed class EquipmentEffectState
    {
        readonly float[] readyAt = new float[EquipmentEffectRules.EffectCount];
        readonly float[] activeUntil = new float[EquipmentEffectRules.EffectCount];
        static bool ValidTime(float now) => !float.IsNaN(now) && !float.IsInfinity(now) && now >= 0;
        public float Remaining(EquipmentSpecialEffect effect, float now) => EquipmentEffectRules.Valid(effect) && ValidTime(now) ? Math.Max(0, readyAt[(int)effect] - now) : 0;
        public float ActiveRemaining(EquipmentSpecialEffect effect, float now) => EquipmentEffectRules.Valid(effect) && ValidTime(now) ? Math.Max(0, activeUntil[(int)effect] - now) : 0;
        public bool TryTrigger(EquipmentSpecialEffect effect, int level, float now)
        {
            if (!EquipmentEffectRules.Valid(effect) || !EquipmentEffectRules.ValidLevel(level) || !ValidTime(now) || Remaining(effect, now) > 0) return false;
            readyAt[(int)effect] = now + EquipmentEffectRules.Cooldown(effect);
            activeUntil[(int)effect] = effect == EquipmentSpecialEffect.SpiritBond ? now + EquipmentEffectRules.SpiritDuration : 0;
            return true;
        }
        public void Deactivate(EquipmentSpecialEffect effect) { if (EquipmentEffectRules.Valid(effect)) activeUntil[(int)effect] = 0; }
        public void Reset() { Array.Clear(readyAt, 0, readyAt.Length); Array.Clear(activeUntil, 0, activeUntil.Length); }
    }
}
