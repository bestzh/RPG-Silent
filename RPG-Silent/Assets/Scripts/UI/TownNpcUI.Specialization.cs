using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    bool specializationPage, specializationReset;
    Button specializationEntry, specializationConfirm;
    RectTransform specializationShade, specializationWindow;
    TMP_Text specializationReview;
    SpecializationDirection specializationChoice;
    long specializationRevision;
    string specializationExpected;
    string SpecializationStateKey => JsonUtility.ToJson(progress.Specialization) + ":" + progress.Gold + ":" + progress.Level + ":" + progress.ArmorUpgradeMaterials + ":" + progress.CurrentClass.Id + ":" + progress.CanChooseSpecialization(SpecializationDirection.Summon);

    void BuildSpecialization()
    {
        specializationEntry = UIView.Button(panel, "SpecializationPage", "职业专精", () =>
        {
            questPage = supplyPage = advancementPage = false;
            ResetWorldPages();
            specializationPage = true;
            advanceConfirmation = null;
            rows.anchoredPosition = Vector2.zero;
            status.text = specializationPage ? "只可保留一个方向；可在此升级或付费重置。原职业被动、转职和已学技能继续生效。" : "新技能仍向对应导师学习；下方显示当前与下一级的具体变化。";
            Refresh();
        });
        specializationShade = UIView.Panel(transform, "SpecializationShade");
        specializationWindow = UIView.Panel(specializationShade, "SpecializationConfirmation");
        UIView.Text(specializationWindow, "Heading", "确认专精投入 / 重置");
        specializationReview = UIView.Text(specializationWindow, "Review", "");
        specializationConfirm = UIView.Button(specializationWindow, "ConfirmSpecialization", "确认", ConfirmSpecialization);
        UIView.Button(specializationWindow, "CancelSpecialization", "取消", CloseSpecialization);
        CloseSpecialization();
    }

    void CloseSpecialization()
    {
        specializationExpected = null;
        if (specializationShade != null)
            specializationShade.gameObject.SetActive(false);
    }

    void RefreshSpecialization()
    {
        bool visible = interaction.Conversation.Role == TownNpcRole.Mentor && !questPage && !supplyPage && !ExtraNpcPage;
        specializationEntry.gameObject.SetActive(visible);
        specializationEntry.GetComponentInChildren<TMP_Text>().text = "职业专精";
        if (visible && specializationPage)
            details.text = $"{progress.CurrentClass.DisplayName} · Lv.{progress.Level} · 金币 {progress.Gold} · 锻造碎片 {progress.ArmorUpgradeMaterials}\n当前职业被动继续生效；专精互斥，最高 3 级。滚动查看全部方向及重置规则。";
        if (!visible || !specializationShade.gameObject.activeSelf)
            return;
        if (specializationExpected != SpecializationStateKey)
        {
            CloseSpecialization();
            status.text = "专精、职业或资源已变化，已取消旧确认，请重新核对。";
        }
    }

    float AddSpecializationRows(float y)
    {
        var current = progress.Specialization;
        AddRow("原职业被动", ClassCombatRules.Description(progress.CurrentClass.FamilyId));
        AddRow("四个专精方向互斥", "只可保留一个方向，最高三级；已学技能、转职和职业被动继续生效。");
        foreach (var direction in new[]
        {
            SpecializationDirection.BasicAttack,
            SpecializationDirection.Skill,
            SpecializationDirection.Defense,
            SpecializationDirection.Summon
        }

        )
        {
            var choice = direction;
            int rank = current.direction == choice ? current.rank : 0, next = rank + 1;
            bool exclusive = current.rank > 0 && current.direction != choice, available = progress.CanChooseSpecialization(choice);
            string requirement = !available ? "仅御兽师向导师学会兽灵召唤后开放。" : exclusive ? "与当前方向互斥，需先重置。" : rank >= 3 ? "已经达到成长上限。" : $"下一级：Lv.{SpecializationRules.RequiredLevel(next)} · {SpecializationRules.GoldCost(next)} 金币 · 锻造碎片 ×{SpecializationRules.MaterialCost(next)}";
            var row = AddRow(SpecializationRules.Name(choice) + (rank > 0 ? $" · {rank}/3 级" : " · 未投入"), "当前：" + SpecializationRules.Effect(choice, rank) + "\n" + (rank < 3 ? "下一级：" + SpecializationRules.Effect(choice, next) : "当前效果继续生效。") + "\n" + requirement);
            row.SetSelected(rank > 0);
            row.Action(0, rank >= 3 ? "已满级" : rank == 0 ? "选择方向" : "提升一级", () => OpenSpecialization(choice, false), available && !exclusive && rank < 3 && progress.Level >= SpecializationRules.RequiredLevel(next) && progress.Gold >= SpecializationRules.GoldCost(next) && progress.ArmorUpgradeMaterials >= SpecializationRules.MaterialCost(next));
        }

        int refund = SpecializationRules.RefundGold(current.investedGold);
        AddRow("重置专精", $"已投入 {current.investedGold} 金币 / 锻造碎片 ×{current.spentMaterials}\n返还金币 {refund}（80%），另需支付 {SpecializationRules.ResetFee} 金币，材料不返还。\n只清除专精，保留技能、槽位、转职、装备与任务。").Action(0, "查看重置与返还", () => OpenSpecialization(SpecializationDirection.None, true), current.rank > 0 && progress.Gold >= SpecializationRules.ResetFee);
        return 0;
    }

    void OpenSpecialization(SpecializationDirection direction, bool reset)
    {
        if (!Validate(TownNpcRole.Mentor))
            return;
        var current = progress.Specialization;
        specializationChoice = direction;
        specializationReset = reset;
        specializationRevision = current.revision;
        specializationExpected = SpecializationStateKey;
        int next = current.rank + 1;
        specializationReview.text = reset ? $"当前：{SpecializationRules.Name(current.direction)} {current.rank} 级\n已投入 {current.investedGold} 金币、锻造碎片 ×{current.spentMaterials}。\n\n返还金币 {SpecializationRules.RefundGold(current.investedGold)}（80%）；支付费用 {SpecializationRules.ResetFee} 金币。\n锻造碎片不返还；必须先持有重置费用。\n重置后金币：{(long)progress.Gold - SpecializationRules.ResetFee + SpecializationRules.RefundGold(current.investedGold)}\n\n只清除专精；职业、技能槽、转职和装备保持。" : $"{SpecializationRules.Name(direction)} · {current.rank} → {next} 级\n当前：{SpecializationRules.Effect(direction, current.rank)}\n下一级：{SpecializationRules.Effect(direction, next)}\n\n需要 Lv.{SpecializationRules.RequiredLevel(next)}；消耗 {SpecializationRules.GoldCost(next)} 金币、锻造碎片 ×{SpecializationRules.MaterialCost(next)}。\n其余方向将互斥；改选需要先付费重置。\n取消不会扣除资源，保存失败保持全部投入前状态。";
        specializationConfirm.interactable = !reset || (long)progress.Gold - SpecializationRules.ResetFee + SpecializationRules.RefundGold(current.investedGold) <= int.MaxValue;
        specializationShade.gameObject.SetActive(true);
        specializationShade.SetAsLastSibling();
        RefreshSpecialization();
    }

    void ConfirmSpecialization()
    {
        if (!Validate(TownNpcRole.Mentor))
        {
            CloseSpecialization();
            return;
        }

        if (specializationExpected != SpecializationStateKey)
        {
            CloseSpecialization();
            status.text = "状态已变化，请重新确认。";
            return;
        }

        bool reset = specializationReset;
        var choice = specializationChoice;
        long revision = specializationRevision;
        CloseSpecialization();
        string message;
        if (reset)
            progress.TryResetSpecialization(revision, out message);
        else
            progress.TryTrainSpecialization(choice, revision, out message);
        status.text = message;
        Refresh();
    }

    string SkillGrowthDescription(SkillData skill, int rank, int cap)
    {
        float specializationDamage = SpecializationRules.SkillDamage(progress.SpecializationRank(SpecializationDirection.Skill));
        string At(int value) => skill.SummonPrefab != null ? $"兽灵伤害倍率 {(1 + (value - 1) * .2f) * progress.SpecializationSummonDamage:0.00} · 持续 {skill.SummonDuration + (value - 1) * 3 + progress.SpecializationSummonDuration:0.#} 秒" : $"技能伤害倍率 {(1 + (value - 1) * skill.DamagePerRank) * specializationDamage:0.00}（另计职业、装备与被动）";
        return (rank == 0 ? "当前：尚未学习" : "当前：" + At(rank)) + "\n" + (rank < cap ? "下一级：" + At(rank + 1) : "已达到当前职业上限") + $"\n实际能量消耗 {progress.SkillEnergyCost(skill):0.##} · 冷却 {skill.CooldownTime:0.#} 秒";
    }
}
