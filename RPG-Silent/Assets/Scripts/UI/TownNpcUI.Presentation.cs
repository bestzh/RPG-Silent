using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public sealed partial class TownNpcUI
{
    [Inject] private PlayerSkillAccess skillAccess;
    private SkillData selectedLesson;
    private UIConfirmationView serviceConfirmation;
    [SerializeField] Texture mentorBackground, forgeBackground;
    private RawImage serviceBackground;
    private Image serviceTint;
    private string confirmationViewKey;
    private bool advancementPage;
    private RectTransform lessonDetails;
    private TMP_Text lessonName, lessonGrowth, lessonRequirement;
    private Button lessonTrain, lessonEquip, advancementEntry;
    private readonly Button[] directSkillSlots = new Button[10];
    private readonly Image[] directSkillIcons = new Image[10];
    private readonly Image[] warehouseIcons = new Image[24];
    private TMP_Text warehouseCart;

    private void BindV5Presentation()
    {
        serviceConfirmation = UIView.Require<UIConfirmationView>(transform, "ServiceConfirmation");
        warehouseCart = UIView.Require<TMP_Text>(warehouseWindow, "WarehouseCart");
        for(int i=0;i<warehouseIcons.Length;i++)warehouseIcons[i]=UIView.Require<Image>(warehouseCells[i].transform,"Icon");
        serviceBackground = UIView.Require<RawImage>(panel, "Backdrop");
        serviceTint = UIView.Require<Image>(panel, "Atmosphere");
        dataRows = rows.GetComponent<UIListView>();
        lessonDetails = UIView.Rect(panel, "MentorDetails");
        lessonName = UIView.Text(lessonDetails, "LessonName", "");
        lessonGrowth = UIView.Text(lessonDetails, "LessonGrowth", "");
        lessonRequirement = UIView.Text(lessonDetails, "LessonRequirement", "");
        lessonTrain = UIView.Button(lessonDetails, "TrainSelected", "学习", TrainSelectedLesson);
        lessonEquip = UIView.Button(lessonDetails, "EquipSelected", "装配到所选位置", EquipSelectedLesson);
        advancementEntry = UIView.Button(panel, "AdvancementPage", "职业进阶", () =>
        {
            advancementPage = true; specializationPage = questPage = supplyPage = storyPage = craftingPage = false;
            advanceConfirmation = null; rows.anchoredPosition = Vector2.zero; Refresh();
        });
        for (int i = 0; i < directSkillSlots.Length; i++)
        {
            int index = i;
            directSkillSlots[i] = UIView.Button(lessonDetails, "SkillSlot" + i, i == 9 ? "0" : (i + 1).ToString(), () => { selectedSlot = index; Refresh(); });
            directSkillIcons[i] = UIView.Require<Image>(directSkillSlots[i].transform, "Icon");
        }
    }

    private void RefreshV5WarehouseItems()
    {
        for(int i=0;i<warehouseIcons.Length;i++)
        {
            int index=warehousePage*24+i;bool used=index<warehouseVisible.Length;
            var item=used?progress.FindItem(warehouseVisible[index].itemId):null;
            warehouseIcons[i].sprite=item?.Icon;warehouseIcons[i].enabled=item?.Icon!=null;
            if(!used)continue;
            var entry=warehouseVisible[index];
            var label=warehouseCells[i].GetComponentInChildren<TMP_Text>();
            bool protectedItem=warehouseDeposit&&!progress.CanWarehouseSelect(entry.Key,true,out _);
            label.text=item.DisplayName+(entry.IsInstance?$" +{entry.upgradeLevel}":$" ×{entry.count}")+"\n"+
                (warehouseSelection.ContainsKey(entry.Key)?"✓ 已选择":protectedItem?"已装备 / 方案保留":entry.isLocked?"锁定保留":item.RarityLabel);
        }
        var choices=WarehouseSelection;
        warehouseCart.text=$"本次清单 · {choices.Length} 项\n"+string.Join("\n",choices.Take(4).Select(x=>progress.FindItem(x.key).DisplayName+$" ×{x.count}"))+
            (choices.Length>4?$"\n另有 {choices.Length-4} 项，核对时查看全部。":"")+
            $"\n\n方向：{(warehouseDeposit?"背包 → 本角色仓库":"本角色仓库 → 背包")}";
    }

    private void RefreshV5Presentation(bool mentor, bool smith)
    {
        serviceBackground.texture = smith ? forgeBackground : mentorBackground;
        serviceTint.color = smith ? new Color(.12f,.14f,.12f,.88f) : new Color(.06f,.15f,.18f,.90f);
        bool lessonPage = mentor && !questPage && !supplyPage && !ExtraNpcPage && !specializationPage && !advancementPage;
        lessonDetails.gameObject.SetActive(lessonPage);
        advancementEntry.gameObject.SetActive(mentor);
        advancementEntry.image.color = advancementPage ? ProgressionUIWidgets.Accent : ProgressionUIWidgets.Card;
        specializationEntry.gameObject.SetActive(mentor);
        serviceTab.image.color=(lessonPage||smith&&!questPage&&!supplyPage&&!ExtraNpcPage)?ProgressionUIWidgets.Accent:ProgressionUIWidgets.Card;
        specializationEntry.image.color=specializationPage&&!questPage&&!supplyPage&&!ExtraNpcPage?ProgressionUIWidgets.Accent:ProgressionUIWidgets.Card;
        serviceTab.GetComponentInChildren<TMP_Text>().text = smith ? "成长与强化" : "技能研习";
        specializationEntry.GetComponentInChildren<TMP_Text>().text = "职业专精";
        foreach (string id in new[] { "PreviousSlot", "NextSlot", "Slot" }) UIView.Find(panel, id).gameObject.SetActive(false);
        UIView.Find(panel, "ClearSlot").gameObject.SetActive(lessonPage);
        for (int i = 0; i < directSkillSlots.Length; i++)
        {
            var equipped = skillAccess?.Current?.GetEquippedSkill(i);
            directSkillIcons[i].sprite = equipped?.Icon;
            directSkillIcons[i].enabled = equipped != null;
            directSkillSlots[i].image.color = selectedSlot == i ? ProgressionUIWidgets.Accent : ProgressionUIWidgets.Card;
        }
    }

    private void AddMentorLessons()
    {
        var lessons = progress.AvailableLessons.Concat(progress.LearnedSkills).Distinct().ToArray();
        if (!lessons.Contains(selectedLesson)) selectedLesson = lessons.FirstOrDefault();
        foreach (var entry in lessons)
        {
            var skill = entry; int rank = progress.GetSkillRank(skill);
            var row = AddRow(skill.SkillName, rank == 0 ? "尚未学习" : $"已学 {rank}/{progress.SkillRankCap(skill)} 级", skill.Icon);
            row.SetSelected(skill == selectedLesson);
            row.Select(() => { selectedLesson = skill; Refresh(); });
        }
        if (selectedLesson == null)
        {
            lessonName.text = "暂无可学习技能"; lessonGrowth.text = lessonRequirement.text = "";
            lessonTrain.interactable = lessonEquip.interactable = false; return;
        }
        int current = progress.GetSkillRank(selectedLesson), next = current + 1, cap = progress.SkillRankCap(selectedLesson);
        bool study = progress.CanStudy(selectedLesson) && current < cap;
        lessonName.text = selectedLesson.SkillName + (current == 0 ? " · 未学习" : $" · {current}/{cap} 级");
        lessonGrowth.text = SkillGrowthDescription(selectedLesson, current, cap);
        lessonRequirement.text = !progress.CanStudy(selectedLesson) ? "历史已学技能：可装配，当前导师不提供升级。" : study ?
            $"下一等级需要 Lv.{selectedLesson.LevelForRank(next)} · {selectedLesson.CostForRank(next)} 金币\n持有 {progress.Gold} 金币 · 自动装入首个空槽" : "已达到当前职业上限，仍可装配使用。";
        lessonTrain.GetComponentInChildren<TMP_Text>().text = current == 0 ? "学习技能" : $"升至 {next} 级";
        lessonTrain.interactable = study && progress.Level >= selectedLesson.LevelForRank(next) && progress.Gold >= selectedLesson.CostForRank(next);
        lessonEquip.interactable = current > 0;
        lessonEquip.GetComponentInChildren<TMP_Text>().text = "装配到位置 " + ((selectedSlot + 1) % 10);
    }

    private void TrainSelectedLesson()
    {
        if (selectedLesson == null || !Validate(TownNpcRole.Mentor)) return;
        progress.TryTrainSkill(selectedLesson, progress.GetSkillRank(selectedLesson), out string message);
        status.text = message; Refresh();
    }

    private void EquipSelectedLesson()
    {
        if (selectedLesson == null || !Validate(TownNpcRole.Mentor)) return;
        progress.TryEquipLearnedSkill(selectedLesson, selectedSlot, out string message);
        status.text = message; Refresh();
    }

    private void AddAdvancementRows()
    {
        var branches = progress.Classes.Where(c => c.BaseClass == progress.CurrentClass).ToArray();
        if (branches.Length == 0) { AddRow("当前职业 · " + progress.CurrentClass.DisplayName, "本角色的转职分支已经确定；已学技能和当前成长继续保留。"); return; }
        foreach (var entry in branches)
        {
            var choice = entry;
            var row = AddRow(choice.DisplayName, $"Lv.{choice.RequiredLevel} · {choice.AdvancementCost} 金币\n{choice.Description}\n确认后本角色无法改选另一分支。");
            row.SetSelected(advanceConfirmation == choice);
            row.Action(0, advanceConfirmation == choice ? "确认转职为" + choice.DisplayName : "选择分支", () =>
            {
                if (!Validate(TownNpcRole.Mentor)) return;
                if (advanceConfirmation != choice) { advanceConfirmation = choice; status.text = "请核对分支与消耗，再次确认后转职。"; Refresh(); return; }
                progress.TryAdvanceClass(choice, out string message); advanceConfirmation = null; status.text = message; Refresh();
            }, progress.Level >= choice.RequiredLevel && progress.Gold >= choice.AdvancementCost);
            if (advanceConfirmation == choice) row.Action(1, "重新考虑", () => { advanceConfirmation = null; Refresh(); });
        }
    }

    void PresentServiceConfirmation()
    {
        string key = advancementPage && advanceConfirmation != null ? "advance:" + advanceConfirmation.Id :
            storyPage && dialogueConfirmation != null ? "story:" + CurrentDialogue?.Id + ":" + dialogueConfirmation :
            craftingPage && reforgePage && reforgeTarget != null ? "reforge:" + reforgeTarget : null;
        if (key == null) { serviceConfirmation.Hide(); confirmationViewKey = null; return; }
        if (key == confirmationViewKey && serviceConfirmation.IsOpen) return;
        confirmationViewKey = key;
        if (advancementPage)
        {
            var choice = advanceConfirmation;
            serviceConfirmation.Show("确认转职为「" + choice.DisplayName + "」？",
                choice.Description + $"\n\n需要 Lv.{choice.RequiredLevel} · 消耗 {choice.AdvancementCost} 金币\n本角色确认后无法改选另一分支；已学技能与装备保留。",
                () => { advanceConfirmation = null; if (Validate(TownNpcRole.Mentor)) { progress.TryAdvanceClass(choice, out string message); status.text = message; } Refresh(); },
                () => { advanceConfirmation = null; Refresh(); });
        }
        else if (storyPage)
        {
            var story = CurrentDialogue; var choice = story.Choices.First(c => c.Id == dialogueConfirmation);
            serviceConfirmation.Show("确认这个决定？", choice.Label + "\n\n" + choice.Outcome + $"\n\n金币 +{choice.Gold} · 经验 +{choice.Experience} · 好感 +{choice.Affinity}\n决定将随角色保存。",
                () => { dialogueConfirmation = null; if (Validate(story.Role)) { progress.TryChooseDialogue(story.Id, choice.Id, interaction.Conversation, out string message); status.text = message; } Refresh(); },
                () => { dialogueConfirmation = null; Refresh(); });
        }
        else
        {
            string itemKey = reforgeTarget, quote = reforgeExpected; var entry = progress.FindInstance(itemKey); var item = progress.FindItem(itemKey);
            serviceConfirmation.Show("核对词条重铸", item.DisplayName + " +" + entry.upgradeLevel + "\n当前：" + Affixes(entry) +
                $"\n\n消耗 {progress.ReforgeGold(itemKey)} 金币 · 锻造碎片 ×{progress.ReforgeShards(itemKey)} · 晶核碎片 ×1\n将随机重铸全部词条，结果可能变弱。品质、强化、宝石与穿戴状态保留。",
                () => { reforgeTarget = reforgeExpected = null; if (Validate(TownNpcRole.Blacksmith)) { progress.TryReforge(itemKey, quote, interaction.Conversation, out string message); status.text = message; } Refresh(); },
                () => { reforgeTarget = reforgeExpected = null; Refresh(); });
        }
    }
}
