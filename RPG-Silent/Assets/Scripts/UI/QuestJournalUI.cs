using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class QuestJournalUI : UIBase
{
    [Inject] PlayerProgressService progress;
    public TMP_FontAsset Font;
    PlayerQuestInteraction interaction;
    RectTransform panel, rows;
    TMP_Text heading, description, objectives, rewards, destination, status;
    Button track;
    readonly Button[] tabs = new Button[3];
    readonly Button[] categoryTabs = new Button[6];
    string selected;
    int filter, category;
    protected override void OnInit()
    {
        W.Panel(transform, "Shade", new Color(.015f, .025f, .04f, .94f));
        panel = W.CenterPanel(transform, "Journal", 1220, 800);
        W.Text(panel, Font, "Title", "冒 险 手 札", 32, 22, 700, 50, 34).color = new Color(.87f, .74f, .49f);
        W.Button(panel, Font, "Close", "关闭 [J]", 1040, 26, 148, 42, () => interaction?.Close());
        string[] labels = { "全部任务", "进行中", "已提交" };
        for (int i = 0; i < 3; i++) { int index = i; tabs[i] = W.Button(panel, Font, "Filter:" + i, labels[i], 32 + i * 132, 100, 122, 42, () => { filter = index; rows.anchoredPosition = Vector2.zero; Refresh(); }); }
        string[] categories = { "全部", "主线", "支线", "职业", "特殊", "委托" };
        for (int i = 0; i < categories.Length; i++) { int index = i; categoryTabs[i] = W.Button(panel, Font, "Category:" + i, categories[i], 32 + i * 65, 154, 61, 40, () => { category = index; rows.anchoredPosition = Vector2.zero; Refresh(); }); }
        var viewport = W.Rect(panel, "Viewport", 32, 208, 386, 502);
        viewport.gameObject.AddComponent<Image>().color = W.Card; viewport.gameObject.AddComponent<RectMask2D>();
        rows = W.Rect(viewport, "Rows", 0, 0, 386, 502);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = rows;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        heading = W.Text(panel, Font, "QuestTitle", "", 452, 100, 736, 48, 28);
        description = W.Text(panel, Font, "Description", "", 452, 168, 736, 108, 23);
        objectives = W.Text(panel, Font, "Objectives", "", 452, 294, 736, 132, 24);
        rewards = W.Text(panel, Font, "Rewards", "", 452, 442, 736, 108, 22);
        destination = W.Text(panel, Font, "Destination", "", 452, 560, 736, 90, 22);
        track = W.Button(panel, Font, "Track", "追踪任务", 452, 666, 280, 44, Track);
        status = W.Text(panel, Font, "Status", "", 32, 736, 1156, 44, 20);
        BuildCollectionTabs();
    }
    public override void OnOpen(params object[] args)
    {
        progress.Changed -= Refresh; interaction = args.Length > 0 ? args[0] as PlayerQuestInteraction : null;
        filter = category = journalPage = 0; selected = progress.TrackedQuest?.Id; status.text = "任务追踪显示方向与距离；成就可手动领奖，图鉴记录每个角色的冒险。";
        progress.Changed += Refresh; Refresh();
    }
    public override void OnClose() { progress.Changed -= Refresh; var previous = interaction; interaction = null; previous?.OnUiClosed(); }
    void OnDestroy() { if (progress != null) progress.Changed -= Refresh; }
    void Update()
    {
        if (panel == null) return;
        var size = ((RectTransform)transform).rect.size; panel.localScale = Vector3.one * Mathf.Min(1, size.x / 1260, size.y / 840);
    }
    public void Select(string id) { selected = id; Refresh(); }
    void Refresh()
    {
        if (panel == null) return;
        foreach (Transform child in rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        RefreshCollectionTabs();
        if (journalPage != 0) { RefreshCollectionRows(); return; }
        for (int i = 0; i < tabs.Length; i++) tabs[i].image.color = filter == i ? W.Accent : W.Card;
        for (int i = 0; i < categoryTabs.Length; i++) categoryTabs[i].image.color = category == i ? W.Accent : W.Card;
        var quests = progress.Quests.Where(q => (category == 0 || (int)q.Category == category - 1) &&
            (filter == 0 || (filter == 1 ? progress.GetQuestStatus(q.Id) == QuestStatus.Active || progress.GetQuestStatus(q.Id) == QuestStatus.Ready : progress.GetQuestStatus(q.Id) == QuestStatus.Rewarded))).ToArray();
        if (!quests.Any(q => q.Id == selected)) selected = quests.FirstOrDefault()?.Id;
        float y = 6;
        foreach (var q in quests)
        {
            string id = q.Id; var b = W.Button(rows, Font, "Quest:" + id, $"{q.CategoryLabel} · {q.Title}\n{StatusLabel(progress.GetQuestStatus(id))}{(progress.TrackedQuest == q ? " · 追踪中" : "")}", 6, y, 374, 80, () => Select(id));
            b.image.color = selected == id ? W.Accent : W.Ink; y += 88;
        }
        if (quests.Length == 0) W.Text(rows, Font, "Empty", "此分类暂无任务。\n可向执事、商人、探险家或本职业导师询问。", 18, 20, 350, 120, 23);
        rows.sizeDelta = new Vector2(386, Mathf.Max(502, y));
        var chosen = progress.FindQuest(selected); track.gameObject.SetActive(chosen != null);
        if (chosen == null) { heading.text = "暂无任务"; description.text = objectives.text = rewards.text = destination.text = ""; return; }
        var state = progress.GetQuestStatus(selected);
        heading.text = $"{chosen.CategoryLabel} · {chosen.Title} · {StatusLabel(state)}";
        description.text = chosen.Description;
        objectives.text = "目标\n" + progress.QuestObjectivesText(selected);
        rewards.text = "提交奖励\n" + progress.QuestRewardText(selected);
        destination.text = "接取 / 提交：" + progress.QuestGiverText(selected) + "\n" + (state == QuestStatus.Ready ? "目标已完成，请返回主城交谈提交。" : progress.QuestRequirementText(selected));
        track.interactable = state == QuestStatus.Active || state == QuestStatus.Ready;
        track.GetComponentInChildren<TMP_Text>().text = progress.TrackedQuest == chosen ? "取消追踪" : "追踪任务";
    }
    void Track()
    {
        progress.TryTrackQuest(progress.TrackedQuest?.Id == selected ? "" : selected, out string message); status.text = message;
    }
    public static string StatusLabel(QuestStatus status) => status switch
    { QuestStatus.Available => "可接取", QuestStatus.Active => "进行中", QuestStatus.Ready => "可提交", QuestStatus.Rewarded => "已提交", _ => "未解锁" };
}
