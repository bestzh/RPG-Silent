using System.Linq;
using RPGSilent.Domain;

public sealed partial class PlayerProgressService
{
    SpecializationData specialization = new();
    public SpecializationData Specialization => specialization.Copy();
    public int SpecializationRank(SpecializationDirection direction) => specialization.direction == direction ? specialization.rank : 0;
    public bool CanChooseSpecialization(SpecializationDirection direction) => direction switch
    {
        SpecializationDirection.BasicAttack or SpecializationDirection.Skill or SpecializationDirection.Defense => CurrentClass != null,
        SpecializationDirection.Summon => CurrentClass?.Id == "beastmaster" && LearnedSkills.Any(s => s.SummonPrefab != null && CanStudy(s)),
        _ => false
    };
    bool ValidSpecializationContent(PlayerProgressData data) => data.specialization.direction != SpecializationDirection.Summon ||
        data.classId == "beastmaster" && data.learnedSkills.Any(e => catalog.TryGetValue(e.skillId, out var s) && s.SummonPrefab != null && classes[data.classId].Lessons.Contains(s));

    public float SpecializationBasicDamage => SpecializationRules.BasicDamage(SpecializationRank(SpecializationDirection.BasicAttack));
    public float SpecializationBasicStagger => SpecializationRules.BasicStagger(SpecializationRank(SpecializationDirection.BasicAttack));
    public float SpecializationBlockEnergy => SpecializationRules.BlockEnergy(SpecializationRank(SpecializationDirection.Defense));
    public float SpecializationParryBonus => SpecializationRules.ParryBonus(SpecializationRank(SpecializationDirection.Defense));
    public float SpecializationSummonDamage => SpecializationRules.SummonDamage(SpecializationRank(SpecializationDirection.Summon));
    public float SpecializationSummonDuration => SpecializationRules.SummonDuration(SpecializationRank(SpecializationDirection.Summon));
    public float SkillEnergyCost(SkillData skill) => skill == null ? 0 : skill.EnergyCost * SpecializationRules.SkillEnergy(SpecializationRank(SpecializationDirection.Skill));

    public bool TryTrainSpecialization(SpecializationDirection direction, long expectedRevision, out string message)
    {
        message = "请在主城对应职业导师处选择专精。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        if (specialization.revision != expectedRevision || specialization.revision == long.MaxValue)
        { message = "专精记录已变化或达到记录上限，请重新选择。"; return false; }
        if (!CanChooseSpecialization(direction)) { message = "该方向尚未开放；驭灵需要转职御兽师并向导师学会召唤。"; return false; }
        if (specialization.rank > 0 && specialization.direction != direction) { message = "专精方向互斥，请先重置当前专精。"; return false; }
        if (specialization.rank >= SpecializationRules.MaxRank) { message = "专精已达到 3 级上限。"; return false; }
        int next = specialization.rank + 1, gold = SpecializationRules.GoldCost(next), materials = SpecializationRules.MaterialCost(next);
        if (Level < SpecializationRules.RequiredLevel(next)) { message = $"专精 {next} 级需要角色 Lv.{SpecializationRules.RequiredLevel(next)}。"; return false; }
        var candidate = Capture();
        if (!candidate.inventory.TrySpendMaterial(ArmorUpgradeMaterialId, materials)) { message = $"背包内需要锻造碎片 ×{materials}。"; return false; }
        candidate.specialization = new SpecializationData { direction = direction, rank = next, revision = specialization.revision + 1,
            investedGold = specialization.investedGold + gold, spentMaterials = specialization.spentMaterials + materials };
        return CommitPurchase(candidate, gold, $"{SpecializationRules.Name(direction)}提升至 {next} 级；消耗 {gold} 金币、锻造碎片 ×{materials}。", out message);
    }

    public bool TryResetSpecialization(long expectedRevision, out string message)
    {
        message = "请在主城对应职业导师处重置专精。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        if (specialization.revision != expectedRevision || specialization.revision == long.MaxValue)
        { message = "专精记录已变化或达到记录上限，请重新确认。"; return false; }
        if (specialization.rank == 0) { message = "尚未投入专精，无需重置。"; return false; }
        var candidate = Capture();
        int refund = SpecializationRules.RefundGold(specialization.investedGold);
        candidate.specialization = new SpecializationData { revision = specialization.revision + 1 };
        return CommitPurchase(candidate, SpecializationRules.ResetFee,
            $"专精已重置：返还 {refund} 金币，支付 {SpecializationRules.ResetFee} 金币；已投入锻造碎片不返还。技能和转职保持。", out message, refund);
    }
}
