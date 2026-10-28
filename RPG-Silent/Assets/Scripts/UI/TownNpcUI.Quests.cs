using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    bool questPage;
    Button serviceTab, questTab, questCategoryButton;
    int questCategory;
    void BuildQuestTabs()
    {
        foreach (string name in new[] { "Slot", "PreviousSlot", "NextSlot", "ClearSlot", "WeaponForge", "ArmorForge", "GemForge", "ForgeMaterials" })
            panel.Find(name).GetComponent<RectTransform>().anchoredPosition += Vector2.down * 54;
        serviceTab = W.Button(panel, Font, "Services", "成长服务", 32, 138, 220, 40, () => SelectQuestPage(false));
        questTab = W.Button(panel, Font, "Quests", "任务委托", 268, 138, 300, 40, () => SelectQuestPage(true));
        supplyTab = W.Button(panel, Font, "Supplies", "战斗补给", 584, 138, 220, 40, () =>
        { supplyPage = true; questPage = false; rows.anchoredPosition = Vector2.zero; status.text = "购买后在背包配置药品快捷栏；Q / R / F 使用，所有药品共享 8 秒冷却。"; Refresh(); });
        questCategoryButton = W.Button(panel, Font, "QuestCategory", "全部类别", 818, 138, 210, 40, () =>
        { questCategory = (questCategory + 1) % 5; rows.anchoredPosition = Vector2.zero; Refresh(); });
    }
    void SelectQuestPage(bool enabled)
    {
        questPage = enabled; rows.anchoredPosition = Vector2.zero;
        supplyPage = false;
        status.text = enabled ? "接取后再前往副本；目标达成后返回这里提交。材料只在提交成功时扣除。" : "请选择成长服务。";
        Refresh();
    }
    void RefreshQuestTabs()
    {
        serviceTab.image.color = questPage || supplyPage ? W.Card : W.Accent;
        serviceTab.gameObject.SetActive(interaction.Conversation.Role <= TownNpcRole.Blacksmith);
        questCategoryButton.gameObject.SetActive(questPage);
        questCategoryButton.GetComponentInChildren<TMP_Text>().text = new[] { "全部类别", "主线", "支线", "职业", "特殊" }[questCategory] + "  >";
        supplyTab.gameObject.SetActive(interaction.Conversation.Role == TownNpcRole.Blacksmith);
        supplyTab.image.color = supplyPage ? W.Accent : W.Card;
        questTab.image.color = questPage ? W.Accent : W.Card;
        int ready = progress.Quests.Count(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && progress.GetQuestStatus(q.Id) == QuestStatus.Ready);
        int available = progress.Quests.Count(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && progress.GetQuestStatus(q.Id) == QuestStatus.Available);
        questTab.GetComponentInChildren<TMP_Text>().text = $"任务 · 可接 {available} / 可交 {ready}";
        if (questPage) details.text = $"{progress.CurrentClass.DisplayName} · Lv.{progress.Level} · 金币 {progress.Gold}\n按 J 查看任务手札；奖励领取后不可重复提交。";
    }
    float AddQuestRows(float y)
    {
        var quests = progress.Quests.Where(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && (questCategory == 0 || (int)q.Category == questCategory - 1)).ToArray();
        if (quests.Length == 0)
        { W.Text(rows, Font, "NoQuests", "当前类别没有委托，可切换类别，或前往执事、商人、探险家及本职业导师处询问。", 16, y, 920, 80, 23); return y + 90; }
        foreach (var q in quests)
        {
            string id = q.Id; var state = progress.GetQuestStatus(id);
            W.Text(rows, Font, "QuestTitle:" + id, $"{q.CategoryLabel} · {q.Title} · {QuestJournalUI.StatusLabel(state)}", 16, y, 674, 34, 24).color = state == QuestStatus.Ready ? new Color(.87f, .74f, .49f) : Color.white;
            W.Text(rows, Font, "QuestStory:" + id, q.Description, 16, y + 38, 674, 64, 20);
            W.Text(rows, Font, "QuestObjectives:" + id, progress.QuestObjectivesText(id), 16, y + 108, 674, 78, 21);
            W.Text(rows, Font, "QuestRewards:" + id, "奖励：" + progress.QuestRewardText(id), 16, y + 192, 674, 52, 20);
            W.Text(rows, Font, "QuestRequirements:" + id, progress.QuestRequirementText(id), 16, y + 250, 920, 28, 18);
            var action = W.Button(rows, Font, "QuestAction:" + id, state == QuestStatus.Available ? "接取任务" : state == QuestStatus.Ready ? "提交并领取奖励" : QuestJournalUI.StatusLabel(state), 710, y + 8, 240, 48, () =>
            {
                if (!Validate(q.Giver)) return;
                string message;
                if (state == QuestStatus.Available) progress.TryAcceptQuest(id, interaction.Conversation, out message);
                else progress.TryTurnInQuest(id, interaction.Conversation, out message);
                status.text = message; Refresh();
            });
            action.interactable = state == QuestStatus.Available || state == QuestStatus.Ready;
            var trackButton = W.Button(rows, Font, "QuestTrack:" + id, progress.TrackedQuest == q ? "取消追踪" : "追踪任务", 710, y + 72, 240, 44, () =>
            { if (Validate(q.Giver)) { progress.TryTrackQuest(progress.TrackedQuest == q ? "" : id, out string message); status.text = message; } });
            trackButton.interactable = state == QuestStatus.Active || state == QuestStatus.Ready;
            y += 294;
        }
        return y;
    }
}
