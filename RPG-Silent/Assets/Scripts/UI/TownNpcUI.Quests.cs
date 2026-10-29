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
        serviceTab = UIView.Button(panel, "Services", "成长服务", () => SelectQuestPage(false));
        questTab = UIView.Button(panel, "Quests", "任务委托", () => SelectQuestPage(true));
        supplyTab = UIView.Button(panel, "Supplies", "战斗补给", () =>
        {
            supplyPage = true;
            questPage = storyPage = craftingPage = false;
            rows.anchoredPosition = Vector2.zero;
            status.text = "购买后在背包配置药品快捷栏；Q / R / F 使用，所有药品共享 8 秒冷却。";
            Refresh();
        });
        questCategoryButton = UIView.Button(panel, "QuestCategory", "全部类别", () =>
        {
            questCategory = (questCategory + 1) % 6;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
    }

    void SelectQuestPage(bool enabled)
    {
        advancementPage = false;
        questPage = enabled;
        rows.anchoredPosition = Vector2.zero;
        specializationPage = false;
        CloseSpecialization();
        supplyPage = false;
        storyPage = craftingPage = false;
        status.text = enabled ? "接取后再前往副本；目标达成后返回这里提交。J 手札可确认放弃；重接清空目标，剧情和历史奖励保留。" : "请选择成长服务。";
        Refresh();
    }

    void RefreshQuestTabs()
    {
        serviceTab.image.color = questPage || supplyPage ? W.Card : W.Accent;
        serviceTab.gameObject.SetActive(interaction.Conversation.Role <= TownNpcRole.Blacksmith);
        questCategoryButton.gameObject.SetActive(questPage);
        questCategoryButton.GetComponentInChildren<TMP_Text>().text = new[]
        {
            "全部类别",
            "主线",
            "支线",
            "职业",
            "特殊",
            "重复委托"
        }[questCategory] + "  >";
        supplyTab.gameObject.SetActive(interaction.Conversation.Role == TownNpcRole.Blacksmith);
        supplyTab.image.color = supplyPage ? W.Accent : W.Card;
        questTab.image.color = questPage ? W.Accent : W.Card;
        int ready = progress.Quests.Count(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && progress.GetQuestStatus(q.Id) == QuestStatus.Ready);
        int available = progress.Quests.Count(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && (progress.GetQuestStatus(q.Id) == QuestStatus.Available || progress.GetQuestStatus(q.Id) == QuestStatus.Abandoned));
        questTab.GetComponentInChildren<TMP_Text>().text = $"任务 · 可接 {available} / 可交 {ready}";
        if (questPage)
            details.text = $"{progress.CurrentClass.DisplayName} · Lv.{progress.Level} · 金币 {progress.Gold} · 好感 {progress.NpcAffinity(interaction.Conversation.Role)}\n按 J 追踪任务；重复委托交付后，需再通关一趟副本刷新。";
    }

    float AddQuestRows(float y)
    {
        var quests = progress.Quests.Where(q => progress.QuestMatchesNpc(q.Id, interaction.Conversation) && (questCategory == 0 || (int)q.Category == questCategory - 1)).ToArray();
        if (quests.Length == 0)
        {
            AddRow("当前类别没有委托", "可切换类别，或向其他主城 NPC 询问。");
            return 0;
        }

        foreach (var q in quests)
        {
            string id = q.Id;
            var state = progress.GetQuestStatus(id);
            var row = AddRow($"{q.CategoryLabel} · {q.Title} · {QuestJournalUI.StatusLabel(state)}", (q.Chapter.Length > 0 ? q.Chapter + "\n" : "") + q.Description + "\n" + progress.QuestObjectivesText(id) + "\n奖励：" + progress.QuestRewardText(id) + "\n" + progress.QuestRequirementText(id));
            row.Title.color = state == QuestStatus.Ready ? new Color(.87f, .74f, .49f) : Color.white;
            row.Action(0, state == QuestStatus.Abandoned ? "重新接取任务" : state == QuestStatus.Available ? "接取任务" : state == QuestStatus.Ready ? "提交并领取奖励" : QuestJournalUI.StatusLabel(state), () =>
            {
                if (!Validate(q.Giver))
                    return;
                string message;
                if (state == QuestStatus.Available || state == QuestStatus.Abandoned)
                    progress.TryAcceptQuest(id, interaction.Conversation, out message);
                else
                    progress.TryTurnInQuest(id, interaction.Conversation, out message);
                status.text = message;
                Refresh();
            }, state == QuestStatus.Available || state == QuestStatus.Abandoned || state == QuestStatus.Ready);
            row.Action(1, progress.TrackedQuest == q ? "取消追踪" : "追踪任务", () =>
            {
                if (Validate(q.Giver))
                {
                    progress.TryTrackQuest(progress.TrackedQuest == q ? "" : id, out string message);
                    status.text = message;
                }
            }, state == QuestStatus.Active || state == QuestStatus.Ready);
        }

        return 0;
    }
}
