using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    void ValidateNarrativeDependencies()
    {
        var visited = new System.Collections.Generic.Dictionary<string, byte>();
        Action<string> visit = null;
        visit = key =>
        {
            if (visited.TryGetValue(key, out byte state))
            { if (state == 1) throw new InvalidOperationException("任务与剧情形成循环依赖：" + key); return; }
            visited[key] = 1;
            if (key.StartsWith("quest:"))
            {
                var q = questCatalog[key.Substring(6)];
                if (q.PrerequisiteId.Length > 0) visit("quest:" + q.PrerequisiteId);
                if (q.RequiredStoryId.Length > 0) visit("story:" + q.RequiredStoryId);
            }
            else
            {
                var story = stories[key.Substring(6)];
                if (story.RequiredQuestId.Length > 0) visit("quest:" + story.RequiredQuestId);
            }
            visited[key] = 2;
        };
        foreach (var q in questCatalog.Keys) visit("quest:" + q);
        foreach (var story in stories.Keys) visit("story:" + story);
    }
    bool QuestClassMatches(QuestDefinition q, string classId) =>
        (q.FamilyId.Length == 0 || classId != null && classes.TryGetValue(classId, out var c) && q.FamilyId == c.FamilyId) &&
        (q.RequiredClassId.Length == 0 || q.RequiredClassId == classId);
    bool QuestUnlocked(QuestDefinition q) => stats.Level >= q.RequiredLevel && NpcAffinity(q.Giver) >= q.RequiredAffinity &&
        (q.PrerequisiteId.Length == 0 || QuestWasCompleted(q.PrerequisiteId)) &&
        (q.RequiredStoryId.Length == 0 || (q.RequiredChoiceId.Length == 0 ? world.Choice(q.RequiredStoryId).Length > 0 : world.Choice(q.RequiredStoryId) == q.RequiredChoiceId));
    public string QuestRulesText(string id)
    {
        var q = FindQuest(id); if (q == null) return "";
        return (q.Objectives.Any(o => o.Kind == QuestObjectiveKind.ProfessionTrial)
            ? "职业考验：同一趟正式副本中完成专属动作并成功通关；死亡、退出、重试或读档清空本趟动作，任务保留。训练与长期挑战不计。"
            : "普通目标跨趟保留；材料在成功提交时扣除。") +
            "\n放弃会清空未交付目标，已获物品保留；可回原委托人重新接取。已交付奖励和剧情决定不可重置。";
    }
    public string QuestAbandonKey(string id)
    {
        var entry = journal.entries.FirstOrDefault(e => e.questId == id && !e.rewarded && !e.abandoned);
        return entry == null ? null : profileId + ":" + JsonUtility.ToJson(entry);
    }
    public bool TryAbandonQuest(string id, string expectedKey, out string message)
    {
        message = "请选择进行中且未提交的任务。";
        if (!CanManageInventory || expectedKey == null || expectedKey != QuestAbandonKey(id))
        { message = "角色或任务进度已变化，请重新确认放弃。"; return false; }
        var data = Capture(); var entry = data.quests.entries.First(e => e.questId == id);
        entry.abandoned = true; entry.counts = new int[entry.counts.Length];
        if (data.quests.trackedId == id) data.quests.trackedId = data.quests.entries.FirstOrDefault(e => !e.rewarded && !e.abandoned)?.questId ?? "";
        bool ok = CommitQuest(data, 0, 0, "已放弃「" + FindQuest(id).Title + "」；目标已清空，已获物品和历史奖励保留，可回原委托人重接。", out message);
        if (ok) { observedObjectives.Remove(id); observedReady.Remove(id); Notify(message); }
        return ok;
    }
    public static int ProfessionTrialCount(ProfessionTrialKind kind, PlayerCombatRecord record) => record == null ? 0 : kind switch
    {
        ProfessionTrialKind.PerfectParry => record.PerfectParries,
        ProfessionTrialKind.SkillHits => record.SkillHits,
        ProfessionTrialKind.RangedHits => record.RangedHits,
        ProfessionTrialKind.HeavyHits => record.HeavyHits,
        ProfessionTrialKind.PerfectDodge => record.PerfectDodges,
        ProfessionTrialKind.CompanionHits => record.CompanionHits,
        _ => 0
    };
    public void RecordProfessionTrial(int dungeonId, PlayerCombatRecord record)
    {
        if (!attached || stats.IsDead || record == null) return;
        bool changed = false;
        foreach (var entry in journal.entries.Where(e => !e.rewarded && !e.abandoned))
        {
            var q = FindQuest(entry.questId);
            for (int i = 0; i < q.Objectives.Length; i++)
            {
                var o = q.Objectives[i];
                if (o.Kind != QuestObjectiveKind.ProfessionTrial || o.DungeonId != dungeonId || entry.counts[i] == o.Required ||
                    ProfessionTrialCount(o.Trial, record) < o.TrialRequired) continue;
                entry.counts[i] = o.Required; changed = true;
            }
        }
        if (changed) Save();
    }
    public string ProfessionTrialSummary(int dungeonId, PlayerCombatRecord record)
    {
        var q = Quests.Where(q => GetQuestStatus(q.Id) == QuestStatus.Active).OrderByDescending(q => q == TrackedQuest)
            .FirstOrDefault(q => q.Objectives.Any(o => o.Kind == QuestObjectiveKind.ProfessionTrial && o.DungeonId == dungeonId));
        if (q == null) return "";
        var o = q.Objectives.First(o => o.Kind == QuestObjectiveKind.ProfessionTrial && o.DungeonId == dungeonId);
        return $"职业考验 · {q.Title}：本趟 {Math.Min(o.TrialRequired, ProfessionTrialCount(o.Trial, record))}/{o.TrialRequired} · 成功通关后核验";
    }
}
