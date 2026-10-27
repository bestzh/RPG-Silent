using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed class TownNpcUI : UIBase
{
    [Inject] PlayerProgressService progress;
    public TMP_FontAsset Font;
    PlayerNpcInteraction interaction;
    RectTransform panel, rows;
    TMP_Text title, details, status, slotLabel;
    PlayerClassDefinition advanceConfirmation;
    int selectedSlot;
    protected override void OnInit()
    {
        W.Panel(transform, "Shade", new Color(0, 0, 0, .65f));
        panel = W.CenterPanel(transform, "Conversation", 1060, 740);
        title = W.Text(panel, Font, "Title", "", 32, 22, 880, 48, 32);
        details = W.Text(panel, Font, "Details", "", 32, 76, 980, 64);
        W.Button(panel, Font, "Close", "关闭", 928, 24, 100, 42, () => interaction?.Close());
        slotLabel = W.Text(panel, Font, "Slot", "", 32, 142, 200, 36);
        W.Button(panel, Font, "PreviousSlot", "上一槽", 236, 138, 106, 40, () => { selectedSlot = (selectedSlot + 9) % 10; Refresh(); });
        W.Button(panel, Font, "NextSlot", "下一槽", 352, 138, 106, 40, () => { selectedSlot = (selectedSlot + 1) % 10; Refresh(); });
        W.Button(panel, Font, "ClearSlot", "卸下该槽技能", 470, 138, 180, 40, ClearSlot);
        var viewport = W.Rect(panel, "Viewport", 32, 192, 996, 430);
        viewport.gameObject.AddComponent<Image>().color = W.Card; viewport.gameObject.AddComponent<RectMask2D>();
        rows = W.Rect(viewport, "Rows", 0, 0, 980, 430);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = rows;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
        status = W.Text(panel, Font, "Status", "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。", 32, 646, 996, 72, 21);
    }
    public override void OnOpen(params object[] args)
    {
        progress.Changed -= Refresh;
        interaction = args.Length > 0 ? args[0] as PlayerNpcInteraction : null;
        advanceConfirmation = null; selectedSlot = 0;
        status.text = interaction != null && interaction.Conversation != null && interaction.Conversation.Role == TownNpcRole.Blacksmith
            ? "强化必定成功。先装备要强化的武器，再与铁匠交谈。"
            : "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。";
        progress.Changed += Refresh; Refresh();
    }
    public override void OnClose() { progress.Changed -= Refresh; var previous = interaction; interaction = null; previous?.Close(); }
    void OnDestroy() { if (progress != null) progress.Changed -= Refresh; }
    bool Validate(TownNpcRole role)
    {
        if (interaction == null) return false;
        bool valid = interaction.ValidateConversation(role, out string message);
        if (!valid) status.text = message; return valid;
    }
    void Refresh()
    {
        if (panel == null || interaction == null || interaction.Conversation == null) return;
        foreach (Transform child in rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var npc = interaction.Conversation;
        bool mentor = npc.Role == TownNpcRole.Mentor;
        foreach (string name in new[] { "Slot", "PreviousSlot", "NextSlot", "ClearSlot" }) panel.Find(name).gameObject.SetActive(mentor);
        var viewport = rows.parent.GetComponent<RectTransform>();
        viewport.anchoredPosition = new Vector2(32, mentor ? -192 : -142);
        viewport.sizeDelta = new Vector2(996, mentor ? 430 : 480);
        title.text = npc.DisplayName;
        details.text = $"{progress.CurrentClass.DisplayName}  ·  Lv.{progress.Level}  ·  金币 {progress.Gold}\n" +
            (npc.Role == TownNpcRole.Blacksmith ? "强化每级增加 10% 普攻伤害，最高 +5。相同武器共享强化等级。" : "基础职业技能最高 3 级；转职后最高 5 级。滚动查看全部技能与转职分支。");
        slotLabel.text = $"装配目标：槽 {(selectedSlot + 1) % 10}";
        float y = 10;
        if (npc.Role == TownNpcRole.Blacksmith)
        {
            foreach (var weapon in Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Select(progress.EquippedWeapon).Where(w => w != null).Distinct())
            {
                var w = weapon; int rank = progress.GetWeaponLevel(w); int cost = progress.WeaponUpgradeCost(rank + 1);
                W.Text(rows, Font, w.name, $"{w.DisplayName}  +{rank}\n普攻伤害加成 {rank * 10}% → {Mathf.Min(5, rank + 1) * 10}%", 16, y, 620, 82, 23);
                var button = W.Button(rows, Font, "UpgradeWeapon", rank < 5 ? $"强化 · {cost} 金币" : "已满级", 710, y + 12, 240, 48, () =>
                { if (Validate(TownNpcRole.Blacksmith)) { progress.TryUpgradeWeapon(w, rank, out string message); status.text = message; Refresh(); } });
                button.interactable = rank < 5 && progress.Gold >= cost; y += 108;
            }
        }
        else if (npc.Profession != null && npc.Profession.Id == progress.CurrentClass.FamilyId)
        {
            foreach (var skill in progress.AvailableLessons.Concat(progress.LearnedSkills).Distinct())
            {
                var s = skill; int rank = progress.GetSkillRank(s); int next = rank + 1;
                int cap = progress.SkillRankCap(s); bool study = progress.CanStudy(s) && rank < cap;
                string requirement = study ? $"下一级：Lv.{s.LevelForRank(next)} / {s.CostForRank(next)} 金币" : "当前职业已达上限";
                W.Text(rows, Font, s.name, $"{s.SkillName}  {(rank == 0 ? "未学习" : $"{rank} 级")}\n{requirement}  ·  能量 {s.EnergyCost:0} / 冷却 {s.CooldownTime:0.#} 秒", 16, y, 580, 78, 21);
                var train = W.Button(rows, Font, "TrainSkill", rank == 0 ? "学习" : "升级", 610, y + 10, 150, 48, () =>
                { if (Validate(TownNpcRole.Mentor)) { progress.TryTrainSkill(s, rank, out string message); status.text = message; Refresh(); } });
                train.interactable = study && progress.Level >= s.LevelForRank(next) && progress.Gold >= s.CostForRank(next);
                var equip = W.Button(rows, Font, "EquipSkill", "装配", 780, y + 10, 170, 48, () =>
                { if (Validate(TownNpcRole.Mentor)) { progress.TryEquipLearnedSkill(s, selectedSlot, out string message); status.text = message; Refresh(); } });
                equip.interactable = rank > 0; y += 92;
            }
            foreach (var branch in progress.Classes.Where(c => c.BaseClass == progress.CurrentClass))
            {
                var c = branch;
                W.Text(rows, Font, c.Id, $"转职 · {c.DisplayName}\nLv.{c.RequiredLevel} / {c.AdvancementCost} 金币 · {c.Description}", 16, y, 670, 100, 21);
                var b = W.Button(rows, Font, "Advance", advanceConfirmation == c ? "确认转职" : "选择分支", 760, y + 12, 190, 50, () =>
                {
                    if (!Validate(TownNpcRole.Mentor)) return;
                    if (advanceConfirmation != c) { advanceConfirmation = c; status.text = $"确定转职为{c.DisplayName}？本角色无法改选另一分支。再次点击确认。"; Refresh(); return; }
                    progress.TryAdvanceClass(c, out string message); advanceConfirmation = null; status.text = message; Refresh();
                });
                b.interactable = progress.Level >= c.RequiredLevel && progress.Gold >= c.AdvancementCost; y += 118;
            }
        }
        else W.Text(rows, Font, "WrongMentor", $"这里是{npc.Profession?.DisplayName}导师。\n请寻找与你职业对应的导师。", 24, y, 920, 100, 26);
        rows.sizeDelta = new Vector2(980, Mathf.Max(430, y + 10));
    }
    void ClearSlot()
    {
        if (!Validate(TownNpcRole.Mentor)) return;
        progress.TryEquipLearnedSkill(null, selectedSlot, out string message); status.text = message; Refresh();
    }
}
