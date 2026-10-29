using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class QuestJournalUI : UIBase
{
    [Inject]
    PlayerProgressService progress;
    public TMP_FontAsset Font;
    PlayerQuestInteraction interaction;
    RectTransform panel, rows;
    TMP_Text heading, description, objectives, rewards, destination, status;
    Button track;
    readonly Button[] tabs = new Button[3];
    readonly Button[] categoryTabs = new Button[6];
    UIListView dataRows;
    string selected;
    int filter, category;
    protected override void OnInit()
    {
        UIView.Panel(transform, "Shade");
        panel = UIView.Panel(transform, "Journal");
        UIView.Text(panel, "Title", "冒 险 手 札").color = new Color(.87f, .74f, .49f);
        UIView.Button(panel, "Close", "关闭 [J]", () => interaction?.Close());
        string[] labels =
        {
            "全部任务",
            "进行中",
            "已提交"
        };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            tabs[i] = UIView.Button(panel, "Filter:" + i, labels[i], () =>
            {
                filter = index;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
        }

        string[] categories =
        {
            "全部",
            "主线",
            "支线",
            "职业",
            "特殊",
            "委托"
        };
        for (int i = 0; i < categories.Length; i++)
        {
            int index = i;
            categoryTabs[i] = UIView.Button(panel, "Category:" + i, categories[i], () =>
            {
                category = index;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
        }

        var viewport = UIView.Rect(panel, "Viewport");
        viewport.gameObject.GetComponent<Image>().color = W.Card;
        rows = UIView.Rect(viewport, "Rows");
        var scroll = viewport.gameObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = rows;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        heading = UIView.Text(panel, "QuestTitle", "");
        description = UIView.Text(panel, "Description", "");
        objectives = UIView.Text(panel, "Objectives", "");
        rewards = UIView.Text(panel, "Rewards", "");
        destination = UIView.Text(panel, "Destination", "");
        track = UIView.Button(panel, "Track", "追踪任务", Track);
        status = UIView.Text(panel, "Status", "");
        BuildCollectionTabs();
        BuildChapterUI();
        dataRows = rows.GetComponent<UIListView>();
        BindCodexTabs();
    }

    public override void OnOpen(params object[] args)
    {
        progress.Changed -= Refresh;
        CloseAbandonQuest();
        interaction = args.Length > 0 ? args[0] as PlayerQuestInteraction : null;
        filter = category = journalPage = 0;
        selected = progress.TrackedQuest?.Id;
        status.text = "任务追踪显示方向与距离；成就可手动领奖，图鉴记录每个角色的冒险。";
        progress.Changed += Refresh;
        Refresh();
    }

    public override void OnClose()
    {
        CloseAbandonQuest();
        progress.Changed -= Refresh;
        var previous = interaction;
        interaction = null;
        previous?.OnUiClosed();
    }

    void OnDestroy()
    {
        if (progress != null)
            progress.Changed -= Refresh;
    }

    public void Select(string id)
    {
        selected = id;
        Refresh();
    }

    void Refresh()
    {
        if (panel == null)
            return;
        dataRows.Begin();
        RefreshCollectionTabs();
        if (journalPage != 0)
        {
            RefreshCollectionRows();
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
            tabs[i].image.color = filter == i ? W.Accent : W.Card;
        for (int i = 0; i < categoryTabs.Length; i++)
            categoryTabs[i].image.color = category == i ? W.Accent : W.Card;
        var quests = progress.Quests.Where(q => (category == 0 || (int)q.Category == category - 1) && (filter == 0 || (filter == 1 ? progress.GetQuestStatus(q.Id) == QuestStatus.Active || progress.GetQuestStatus(q.Id) == QuestStatus.Ready : progress.GetQuestStatus(q.Id) == QuestStatus.Rewarded))).ToArray();
        if (!quests.Any(q => q.Id == selected))
            selected = quests.FirstOrDefault()?.Id;
        foreach (var q in quests)
        {
            string id = q.Id;
            var row = dataRows.Add(q.CategoryLabel + " · " + q.Title, StatusLabel(progress.GetQuestStatus(id)) + (progress.TrackedQuest == q ? " · 追踪中" : ""));
            row.SetSelected(selected == id);
            row.Select(() => Select(id));
        }

        if (quests.Length == 0)
            dataRows.Add("此分类暂无任务", "可向执事、商人、探险家或本职业导师询问。");
        var chosen = progress.FindQuest(selected);
        track.gameObject.SetActive(chosen != null);
        if (chosen == null)
        {
            abandonQuest.gameObject.SetActive(false);
            heading.text = "暂无任务";
            description.text = objectives.text = rewards.text = destination.text = "";
            return;
        }

        var state = progress.GetQuestStatus(selected);
        heading.text = $"{chosen.CategoryLabel} · {chosen.Title} · {StatusLabel(state)}";
        description.text = (chosen.Chapter.Length > 0 ? chosen.Chapter + "\n" : "") + chosen.Description;
        objectives.text = "目标\n" + progress.QuestObjectivesText(selected);
        rewards.text = "提交奖励\n" + progress.QuestRewardText(selected);
        destination.text = "接取 / 提交：" + progress.QuestGiverText(selected) + "\n" + (state == QuestStatus.Ready ? "目标已完成，请返回主城交谈提交。" : progress.QuestRequirementText(selected));
        track.interactable = state == QuestStatus.Active || state == QuestStatus.Ready;
        abandonQuest.gameObject.SetActive(track.interactable);
        track.GetComponentInChildren<TMP_Text>().text = progress.TrackedQuest == chosen ? "取消追踪" : "追踪任务";
    }

    void Track()
    {
        progress.TryTrackQuest(progress.TrackedQuest?.Id == selected ? "" : selected, out string message);
        status.text = message;
    }

    public static string StatusLabel(QuestStatus status) => status switch
    {
        QuestStatus.Available => "可接取",
        QuestStatus.Active => "进行中",
        QuestStatus.Ready => "可提交",
        QuestStatus.Rewarded => "已提交",
        QuestStatus.Abandoned => "已放弃 · 可重接",
        _ => "未解锁"
    };
}
