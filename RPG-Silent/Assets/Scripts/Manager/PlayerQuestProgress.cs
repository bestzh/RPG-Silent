using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerProgressService
{
    readonly Dictionary<string, QuestDefinition> questCatalog = new();
    QuestJournalData journal = new();
    public QuestJournalData Journal => journal.Copy();
    public QuestDefinition TrackedQuest => FindQuest(journal.trackedId);
    public IEnumerable<QuestDefinition> Quests => questCatalog.Values.Where(q => q.FamilyId.Length == 0 || q.FamilyId == CurrentClass?.FamilyId).OrderBy(q => q.Order).ThenBy(q => q.Id);
    public QuestDefinition FindQuest(string id) => id != null && questCatalog.TryGetValue(id, out var q) ? q : null;
    public QuestProgressData QuestRecord(string id) => journal.entries.FirstOrDefault(e => e.questId == id)?.Copy();

    void LoadQuestCatalog()
    {
        var database = Resources.Load<DungeonDatabase>("DungeonDatabase");
        foreach (var q in Resources.LoadAll<QuestDefinition>("Quests"))
        {
            if (string.IsNullOrEmpty(q.Id) || !questCatalog.TryAdd(q.Id, q) || string.IsNullOrEmpty(q.Title) ||
                !Enum.IsDefined(typeof(QuestCategory), q.Category) || !Enum.IsDefined(typeof(TownNpcRole), q.Giver) ||
                q.FamilyId == null || (q.FamilyId.Length > 0 && (!classes.TryGetValue(q.FamilyId, out var family) || family.IsAdvanced)) ||
                q.PrerequisiteId == null || q.RequiredLevel < 1 || q.RequiredLevel > PlayerStats.LevelCap ||
                q.Objectives == null || q.Objectives.Length == 0 || q.Objectives.Length > 8 || q.RewardGold < 0 || q.RewardExp < 0 ||
                q.Rewards == null || q.Rewards.Any(x => !InventoryData.ValidItem(x) || x.IsInstance || x.legacyReserve || FindItem(x.itemId)?.Kind != ItemKind.Material))
                throw new InvalidOperationException("任务配置无效：" + q.name);
            foreach (var o in q.Objectives)
                if (o == null || !Enum.IsDefined(typeof(QuestObjectiveKind), o.Kind) || string.IsNullOrEmpty(o.Label) || o.Required < 1 || o.Required > 9999 ||
                    (o.IsDungeonCounter && (database == null || database.GetById(o.DungeonId) == null || database.GetById(o.DungeonId).IsTraining)) ||
                    (o.Kind == QuestObjectiveKind.TalkToNpc && (!Enum.IsDefined(typeof(TownNpcRole), o.NpcRole) || o.Required != 1)) ||
                    (o.Kind == QuestObjectiveKind.DeliverMaterial && FindItem(o.ItemId)?.Kind != ItemKind.Material) ||
                    (o.Kind == QuestObjectiveKind.ReachLevel && o.Required > PlayerStats.LevelCap) ||
                    (o.Kind == QuestObjectiveKind.AdvanceClass && o.Required != 1))
                    throw new InvalidOperationException("任务目标无效：" + q.name);
            if (q.Objectives.Where(o => o.Kind == QuestObjectiveKind.DeliverMaterial).GroupBy(o => o.ItemId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("任务交付材料重复：" + q.name);
        }
        foreach (var quest in questCatalog.Values)
        {
            var visited = new HashSet<string> { quest.Id }; var q = quest;
            while (q.PrerequisiteId.Length > 0)
            {
                if (!questCatalog.TryGetValue(q.PrerequisiteId, out var previous) || !visited.Add(previous.Id) ||
                    (previous.FamilyId.Length > 0 && previous.FamilyId != quest.FamilyId))
                    throw new InvalidOperationException("任务前置关系无效：" + quest.name);
                q = previous;
            }
        }
    }
    bool ValidQuestContent(QuestJournalData data, string savedClass)
    {
        return data.entries.All(e => questCatalog.TryGetValue(e.questId, out var q) &&
            (q.FamilyId.Length == 0 || q.FamilyId == classes[savedClass].FamilyId) && e.counts.Length == q.Objectives.Length &&
            e.counts.Select((value, i) => q.Objectives[i].IsCounter ? value <= q.Objectives[i].Required : value == 0).All(x => x) &&
            (q.PrerequisiteId.Length == 0 || data.entries.Any(p => p.questId == q.PrerequisiteId && p.rewarded)) &&
            (!e.rewarded || q.Objectives.Select((o, i) => !o.IsCounter || e.counts[i] == o.Required).All(x => x)));
    }
    public QuestStatus GetQuestStatus(string id)
    {
        var q = FindQuest(id);
        if (q == null || (q.FamilyId.Length > 0 && q.FamilyId != CurrentClass?.FamilyId)) return QuestStatus.Locked;
        var e = journal.entries.FirstOrDefault(x => x.questId == id);
        if (e != null) return e.rewarded ? QuestStatus.Rewarded : q.Objectives.Select((o, i) => ObjectiveCount(q, e, i) >= o.Required).All(x => x) ? QuestStatus.Ready : QuestStatus.Active;
        return stats.Level >= q.RequiredLevel && (q.PrerequisiteId.Length == 0 || journal.entries.Any(x => x.questId == q.PrerequisiteId && x.rewarded)) ? QuestStatus.Available : QuestStatus.Locked;
    }
    int ObjectiveCount(QuestDefinition q, QuestProgressData e, int i)
    {
        var o = q.Objectives[i];
        int value = o.IsCounter ? e?.counts[i] ?? 0 : o.Kind switch
        {
            QuestObjectiveKind.DeliverMaterial => inventory.Count(o.ItemId),
            QuestObjectiveKind.LearnSkills => learned.Keys.Count(id => AvailableLessons.Any(s => s.PersistentId == id)),
            QuestObjectiveKind.ReachLevel => stats.Level,
            QuestObjectiveKind.AdvanceClass => CurrentClass?.IsAdvanced == true ? 1 : 0,
            _ => 0
        };
        return Mathf.Min(value, o.Required);
    }
    public string QuestObjectivesText(string id)
    {
        var q = FindQuest(id); if (q == null) return string.Empty;
        var e = journal.entries.FirstOrDefault(x => x.questId == id);
        return string.Join("\n", q.Objectives.Select((o, i) => $"{o.Label}  {(e?.rewarded == true ? o.Required : ObjectiveCount(q, e, i))}/{o.Required}"));
    }
    public string QuestRewardText(string id)
    {
        var q = FindQuest(id); if (q == null) return string.Empty;
        return $"金币 {q.RewardGold} · 经验 {q.RewardExp}" + (q.Rewards.Length == 0 ? "" : "\n" + string.Join(" · ", q.Rewards.Select(x => $"{FindItem(x.itemId).DisplayName} ×{x.count}")));
    }
    public string QuestRequirementText(string id)
    {
        var q = FindQuest(id); if (q == null) return "任务资源不可用。";
        return $"接取等级 Lv.{q.RequiredLevel}" + (q.PrerequisiteId.Length == 0 ? "" : " · 先提交「" + FindQuest(q.PrerequisiteId)?.Title + "」");
    }
    public string QuestGiverText(string id) => FindQuest(id)?.Giver switch
    {
        TownNpcRole.Blacksmith => "主城铁匠",
        TownNpcRole.Steward => "广场执事 · 阿登",
        TownNpcRole.Scout => "探险家 · 米拉",
        TownNpcRole.Resident => "商人 · 罗莎",
        _ => $"主城{(CurrentClass?.BaseClass ?? CurrentClass)?.DisplayName}导师"
    };
    public string QuestMarkerFor(TownNpc npc)
    {
        bool available = false;
        foreach (var q in Quests)
        {
            if (!QuestMatchesNpc(q.Id, npc)) continue;
            var state = GetQuestStatus(q.Id);
            if (state == QuestStatus.Ready) return "?";
            if (state == QuestStatus.Available) available = true;
        }
        return available ? "!" : "";
    }
    public bool QuestMatchesNpc(string id, TownNpc npc)
    {
        var q = FindQuest(id);
        return q != null && npc != null && npc.Role == q.Giver && (q.FamilyId.Length == 0 || q.FamilyId == CurrentClass?.FamilyId) &&
            (npc.Role != TownNpcRole.Mentor || npc.Profession?.Id == CurrentClass?.FamilyId);
    }
    bool CanTalkQuest(string id, TownNpc npc, out string message)
    {
        message = "请靠近对应的主城 NPC 交谈。";
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        return CanManageInventory && player != null && !player.CombatAllowed && QuestMatchesNpc(id, npc) && npc.isActiveAndEnabled &&
            npc.gameObject.scene == SceneManager.GetActiveScene() && Vector3.Distance(player.transform.position, npc.transform.position) <= npc.InteractionRadius;
    }
    public bool TryAcceptQuest(string id, TownNpc npc, out string message)
    {
        if (!CanTalkQuest(id, npc, out message)) return false;
        if (GetQuestStatus(id) != QuestStatus.Available) { message = "任务已接取，或尚未满足前置条件。"; return false; }
        var data = Capture(); var q = FindQuest(id);
        data.quests.entries = data.quests.entries.Append(new QuestProgressData { questId = id, counts = new int[q.Objectives.Length] }).ToArray();
        data.quests.trackedId = id;
        return CommitQuest(data, 0, 0, "已接取并追踪「" + q.Title + "」。", out message);
    }
    public bool TryTrackQuest(string id, out string message)
    {
        message = "请选择已接取且未提交的任务。";
        if (!attached || stats.IsDead || id == null || (id.Length > 0 && !journal.entries.Any(e => e.questId == id && !e.rewarded))) return false;
        var data = Capture(); data.quests.trackedId = id;
        return CommitQuest(data, 0, 0, id.Length == 0 ? "已取消任务追踪。" : "已追踪「" + FindQuest(id).Title + "」。", out message);
    }
    public bool TryTurnInQuest(string id, TownNpc npc, out string message)
    {
        if (!CanTalkQuest(id, npc, out message)) return false;
        if (GetQuestStatus(id) != QuestStatus.Ready) { message = "目标尚未完成，或奖励已经领取。"; return false; }
        var q = FindQuest(id); var data = Capture();
        foreach (var o in q.Objectives.Where(x => x.Kind == QuestObjectiveKind.DeliverMaterial))
            if (!data.inventory.TrySpendMaterial(o.ItemId, o.Required)) { message = "交付材料不足，请先从待领取移入背包。"; return false; }
        if (!data.inventory.AddRewards(q.Rewards)) { message = "背包和待领取空间不足，请整理后再提交；材料尚未消耗。"; return false; }
        data.quests.entries.First(x => x.questId == id).rewarded = true;
        if (data.quests.trackedId == id) data.quests.trackedId = data.quests.entries.FirstOrDefault(x => !x.rewarded)?.questId ?? "";
        return CommitQuest(data, q.RewardGold, q.RewardExp, "已完成「" + q.Title + "」，奖励已保存。满包物品可在背包待领取页领取。", out message);
    }
    bool CommitQuest(PlayerProgressData data, int gold, int exp, string success, out string message)
    {
        data.gold = (int)Math.Min(int.MaxValue, (long)stats.Gold + gold); data.totalExp = (int)Math.Min(int.MaxValue, (long)stats.Exp + exp);
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        applying = true;
        try
        {
            journal = data.quests.Copy(); inventory = data.inventory.Copy(); stats.AddReward(gold, exp);
            newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false; StatusMessage = message = success;
        }
        finally { applying = false; }
        Changed?.Invoke(); return true;
    }
    // Called only after DungeonRunService removes a live enemy / settles a successful run.
    // Gameplay events remain in memory if saving fails, so retrying Save never loses or doubles them.
    public void RecordQuestDungeonEvent(int dungeonId, bool cleared)
    {
        RecordQuestActivity(cleared ? QuestObjectiveKind.ClearDungeon : QuestObjectiveKind.DefeatEnemies, dungeonId);
    }
    public void RecordQuestConversation(TownNpc npc)
    {
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        if (player == null || player.IsDead || player.CombatAllowed || npc == null || !npc.isActiveAndEnabled ||
            npc.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(player.transform.position, npc.transform.position) > npc.InteractionRadius ||
            npc.Role == TownNpcRole.Mentor && npc.Profession?.Id != CurrentClass?.FamilyId) return;
        RecordQuestActivity(QuestObjectiveKind.TalkToNpc, 0, npc.Role);
    }
    // Callers own event de-duplication: successful interaction, registered enemy death, settled challenge.
    public void RecordQuestActivity(QuestObjectiveKind kind, int dungeonId, TownNpcRole npcRole = TownNpcRole.Mentor)
    {
        if (!attached) return;
        bool changed = false;
        foreach (var e in journal.entries.Where(x => !x.rewarded))
        {
            var q = FindQuest(e.questId);
            if (kind == QuestObjectiveKind.ClearDungeon && q.Objectives.Any(o => o.Kind == QuestObjectiveKind.AdvanceClass) && CurrentClass?.IsAdvanced != true) continue;
            for (int i = 0; i < q.Objectives.Length; i++)
                if (q.Objectives[i].MatchesEvent(kind, dungeonId, npcRole) && e.counts[i] < q.Objectives[i].Required)
                { e.counts[i]++; changed = true; }
        }
        if (changed) Save();
    }
}
