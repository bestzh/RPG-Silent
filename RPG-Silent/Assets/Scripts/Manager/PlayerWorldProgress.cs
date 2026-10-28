using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerProgressService
{
    WorldProgressData world = new();
    readonly Dictionary<string, TownDialogueDefinition> stories = new();
    readonly Dictionary<string, EquipmentRecipe> recipes = new();
    readonly Dictionary<string, AchievementDefinition> achievements = new();
    readonly Queue<string> notices = new();
    readonly Dictionary<string, int[]> observedObjectives = new();
    readonly HashSet<string> observedReady = new(), observedAchievements = new();
    AdventureCatalog codex;
    readonly System.Random forgeRandom = new();
    public WorldProgressData World => world.Copy();
    public IEnumerable<EquipmentRecipe> Recipes => recipes.Values.OrderBy(x => x.Result.Rarity).ThenBy(x => x.Id);
    public IEnumerable<AchievementDefinition> Achievements => achievements.Values.OrderBy(x => x.Kind).ThenBy(x => x.Required);
    public int CompletedRuns => world.completedRuns;
    public float TownHour => world.townHour;
    public int TownDay => world.townDay;
    public string TownTimeLabel => $"第 {world.townDay} 天  {(int)world.townHour:00}:{(int)(world.townHour % 1 * 60):00}";
    public string StoryChoice(string id) => world.Choice(id);
    public int NpcAffinity(TownNpcRole role) => world.Affinity((int)role);
    public TownDialogueDefinition DialogueFor(TownNpc npc) => npc == null ? null : stories.Values.FirstOrDefault(x => x.Role == npc.Role);
    public bool QuestWasCompleted(string id) => journal.entries.Any(x => x.questId == id && (x.rewarded || x.completedCount > 0));
    public string TakeNotice() => notices.Count > 0 ? notices.Dequeue() : null;
    void Notify(string text) { if (notices.Count >= 12) notices.Dequeue(); notices.Enqueue(text); }
    void LoadWorldCatalog()
    {
        foreach (var s in Resources.LoadAll<TownDialogueDefinition>("Adventure"))
        {
            if (string.IsNullOrEmpty(s.Id) || !stories.TryAdd(s.Id, s) || s.Pages == null || s.Pages.Length < 2 || s.Pages.Any(string.IsNullOrEmpty) ||
                s.Choices == null || s.Choices.Length < 2 || !Enum.IsDefined(typeof(TownNpcRole), s.Role) ||
                s.Choices.Any(x => x == null || string.IsNullOrEmpty(x.Id) || string.IsNullOrEmpty(x.Label) || string.IsNullOrEmpty(x.Outcome) || x.Gold < 0 || x.Experience < 0 || x.Affinity < 0 || x.Affinity > 100) ||
                s.Choices.Select(x => x.Id).Distinct().Count() != s.Choices.Length) throw new InvalidOperationException("剧情配置无效：" + s.name);
        }
        foreach (var r in Resources.LoadAll<EquipmentRecipe>("Adventure"))
            if (string.IsNullOrEmpty(r.Id) || !recipes.TryAdd(r.Id, r) || r.Result == null || r.Result.IsStackable || FindItem(r.Result.Id) != r.Result ||
                r.Gold < 1 || r.ForgeShards < 1 || r.CrystalShards < 0 || r.UnlockQuestId == null || r.UnlockQuestId.Length > 0 && FindQuest(r.UnlockQuestId) == null)
                throw new InvalidOperationException("配方配置无效：" + r.name);
        foreach (var a in Resources.LoadAll<AchievementDefinition>("Adventure"))
            if (string.IsNullOrEmpty(a.Id) || !achievements.TryAdd(a.Id, a) || !Enum.IsDefined(typeof(CollectionKind), a.Kind) || a.Required < 1 || a.Gold < 0 || a.Experience < 0 || a.ForgeShards < 0)
                throw new InvalidOperationException("成就配置无效：" + a.name);
        codex = Resources.Load<AdventureCatalog>("Adventure/Codex");
        foreach (var q in questCatalog.Values)
            if (q.RequiredStoryId == null || q.RequiredChoiceId == null || (q.RequiredStoryId.Length == 0 ? q.RequiredChoiceId.Length > 0 :
                !stories.TryGetValue(q.RequiredStoryId, out var s) || !s.Choices.Any(x => x.Id == q.RequiredChoiceId)))
                throw new InvalidOperationException("任务剧情前置无效：" + q.name);
    }
    bool ValidWorldContent(WorldProgressData d) => d.choices.All(x => stories.TryGetValue(x.storyId, out var s) && s.Choices.Any(c => c.Id == x.choiceId)) &&
        d.claimedAchievements.All(achievements.ContainsKey);
    void SeedWorldFeedback()
    {
        notices.Clear(); observedObjectives.Clear(); observedReady.Clear(); observedAchievements.Clear();
        foreach (var e in journal.entries.Where(x => !x.rewarded)) observedObjectives[e.questId] = ObjectiveCounts(e.questId);
        foreach (var q in Quests) if (GetQuestStatus(q.Id) == QuestStatus.Ready) observedReady.Add(q.Id);
        foreach (var a in Achievements) if (AchievementReady(a.Id)) observedAchievements.Add(a.Id);
    }
    public int[] ObjectiveCounts(string id)
    {
        var q = FindQuest(id); var e = journal.entries.FirstOrDefault(x => x.questId == id);
        return q == null ? Array.Empty<int>() : q.Objectives.Select((o, i) => e?.rewarded == true ? o.Required : ObjectiveCount(q, e, i)).ToArray();
    }
    void ObserveWorldFeedback()
    {
        if (!attached) return;
        foreach (var e in journal.entries.Where(x => !x.rewarded))
        {
            var q = FindQuest(e.questId); var now = ObjectiveCounts(q.Id);
            if (observedObjectives.TryGetValue(q.Id, out var before))
                for (int i = 0; i < now.Length; i++)
                    if (before[i] < q.Objectives[i].Required && now[i] == q.Objectives[i].Required)
                        Notify("目标完成 · " + q.Objectives[i].Label + (HasUnsavedChanges ? "\n进度暂未保存，请重试保存。" : ""));
            observedObjectives[q.Id] = now;
            if (GetQuestStatus(q.Id) == QuestStatus.Ready)
            { if (observedReady.Add(q.Id)) Notify("可交付 · " + q.Title + "\n返回" + QuestGiverText(q.Id)); }
            else observedReady.Remove(q.Id);
        }
        foreach (var a in Achievements)
            if (AchievementReady(a.Id) && observedAchievements.Add(a.Id)) Notify("成就达成 · " + a.Title + "\n打开 J 手札，在成就页领取奖励。");
    }
    void CollectInventoryDiscoveries(PlayerProgressData d)
    {
        foreach (var item in d.inventory.bag.Concat(d.inventory.overflow))
            if (FindItem(item.itemId) is { IsStackable: false }) d.world.Record(CollectionKind.Equipment, item.itemId, true);
    }
    bool AtTownNpc(TownNpc npc, TownNpcRole role)
    {
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        return CanManageInventory && player != null && !player.CombatAllowed && npc != null && npc.Role == role && npc.isActiveAndEnabled &&
            npc.gameObject.scene == SceneManager.GetActiveScene() && Vector3.Distance(player.transform.position, npc.transform.position) <= npc.InteractionRadius;
    }
    public bool TryChooseDialogue(string storyId, string choiceId, TownNpc npc, out string message)
    {
        message = "请在对应 NPC 面前交谈。";
        if (!stories.TryGetValue(storyId ?? "", out var story) || !AtTownNpc(npc, story.Role)) return false;
        var choice = story.Choices.FirstOrDefault(x => x.Id == choiceId);
        if (choice == null || world.Choice(storyId).Length > 0) { message = "这段故事已有决定，可以回顾但不能重复领取。"; return false; }
        var data = Capture();
        data.world.choices = data.world.choices.Append(new StoryChoiceData { storyId = storyId, choiceId = choiceId }).ToArray();
        data.world.AddAffinity((int)story.Role, choice.Affinity);
        bool ok = CommitQuest(data, choice.Gold, choice.Experience, choice.Outcome, out message);
        if (ok) Notify($"决定已保存 · {choice.Label}\n金币 +{choice.Gold} · 经验 +{choice.Experience} · 好感 +{choice.Affinity}");
        return ok;
    }
    public void RecordEnemyDiscovery(EnemyController enemy)
    {
        if (!attached || enemy == null || enemy.IsCompanion || enemy.Definition == null) return;
        world.Record(enemy.IsBoss ? CollectionKind.Boss : CollectionKind.Enemy, enemy.Definition.name);
        if (enemy.IsElite && !enemy.IsBoss) world.Record(CollectionKind.Elite, enemy.Definition.name);
        Save();
    }
    public void RecordExplorationDiscovery(ExplorationNode node)
    {
        if (!attached || node == null || string.IsNullOrEmpty(node.PersistentId)) return;
        if (node.Kind == ExplorationKind.HiddenPassage) world.Record(CollectionKind.Secret, node.PersistentId, true);
        else if (node.Kind == ExplorationKind.Chest) world.Record(CollectionKind.Chest, node.PersistentId);
        else return;
        Save();
    }
    public void RecordCompletedRun(int dungeonId)
    {
        if (!attached || dungeonId != 1001 && dungeonId != 1002) return;
        world.completedRuns = Math.Min(1000000, world.completedRuns + 1);
        world.Record(CollectionKind.Dungeon, dungeonId.ToString()); Save();
    }
    public void SetTownTime(float hour, int day)
    {
        if (!attached || float.IsNaN(hour) || float.IsInfinity(hour)) return;
        world.townHour = Mathf.Repeat(hour, 24); world.townDay = Mathf.Clamp(day, 1, 1000000);
    }
    public bool RecipeUnlocked(EquipmentRecipe r) => r != null && recipes.TryGetValue(r.Id, out var known) && known == r && (r.UnlockQuestId.Length == 0 || QuestWasCompleted(r.UnlockQuestId));
    public bool TryCraftEquipment(string recipeId, TownNpc npc, out string message)
    {
        message = "请在主城铁匠处选择已解锁的配方。";
        if (!AtTownNpc(npc, TownNpcRole.Blacksmith) || !recipes.TryGetValue(recipeId ?? "", out var recipe) || !RecipeUnlocked(recipe)) return false;
        var d = Capture();
        if (stats.Gold < recipe.Gold) { message = "金币不足。"; return false; }
        if (!d.inventory.TrySpendMaterial("forge-shard", recipe.ForgeShards) || recipe.CrystalShards > 0 && !d.inventory.TrySpendMaterial("crystal-shard", recipe.CrystalShards))
        { message = "背包内制作材料不足。"; return false; }
        if (!d.inventory.AddRewards(new[] { EquipmentInstances.Create(recipe.Result, forgeRandom) })) { message = "背包和待领取已满，请整理后制作。"; return false; }
        d.world.Record(CollectionKind.Crafted, recipe.Result.Id);
        bool ok = CommitPurchase(d, recipe.Gold, "已锻造 " + recipe.Result.DisplayName + "。满包时进入待领取。", out message);
        if (ok) Notify(message); return ok;
    }
    public int ReforgeGold(string key) => FindItem(key) == null ? 0 : 100 + 75 * (int)FindItem(key).Rarity;
    public int ReforgeShards(string key) => FindItem(key) == null ? 0 : 2 + 2 * (int)FindItem(key).Rarity;
    public string ReforgeQuote(string key)
    {
        var e = inventory.Find(key); return e == null ? "" : JsonUtility.ToJson(e);
    }
    public bool TryReforge(string key, string expected, TownNpc npc, out string message)
    {
        message = "请在主城铁匠处选择背包内的装备。";
        if (!AtTownNpc(npc, TownNpcRole.Blacksmith)) return false;
        var entry = inventory.Find(key); var item = FindItem(key);
        if (entry == null || !entry.IsInstance || entry.legacyReserve || item == null || item.IsStackable || item.Rarity == ItemRarity.Common) return false;
        if (entry.isLocked) { message = "装备已锁定，请先在背包解除锁定。"; return false; }
        if (expected != ReforgeQuote(key)) { message = "装备状态已变化，请重新确认。"; return false; }
        int gold = ReforgeGold(key), shards = ReforgeShards(key);
        if (stats.Gold < gold) { message = "金币不足。"; return false; }
        var data = Capture();
        if (!data.inventory.TrySpendMaterial("forge-shard", shards) || !data.inventory.TrySpendMaterial("crystal-shard", 1)) { message = "需要足够锻造碎片和晶核碎片 ×1。"; return false; }
        data.inventory.Find(key).affixes = EquipmentInstances.Create(item, forgeRandom).affixes;
        bool ok = CommitPurchase(data, gold, "已重铸 " + item.DisplayName + " 的全部随机词条。强化、宝石和穿戴状态保留。", out message);
        if (ok) Notify(message); return ok;
    }
    public int AchievementCount(string id) => achievements.TryGetValue(id ?? "", out var a) ? Math.Min(a.Required, world.Count(a.Kind, a.Distinct)) : 0;
    public bool AchievementClaimed(string id) => world.claimedAchievements.Contains(id);
    public bool AchievementReady(string id) => achievements.TryGetValue(id ?? "", out var a) && !AchievementClaimed(id) && AchievementCount(id) >= a.Required;
    public bool TryClaimAchievement(string id, out string message)
    {
        message = "成就尚未完成或已领取奖励。";
        if (!CanManageInventory || !AchievementReady(id)) return false;
        var a = achievements[id]; var d = Capture();
        if (a.ForgeShards > 0 && !d.inventory.AddRewards(new[] { new ItemStackData { itemId = "forge-shard", count = a.ForgeShards } })) { message = "奖励存储已满，请先整理背包。"; return false; }
        d.world.claimedAchievements = d.world.claimedAchievements.Append(id).ToArray();
        bool ok = CommitQuest(d, a.Gold, a.Experience, "已领取成就奖励：" + a.Title, out message);
        if (ok) Notify($"成就奖励 · {a.Title}\n金币 +{a.Gold} · 经验 +{a.Experience} · 锻造碎片 +{a.ForgeShards}"); return ok;
    }
    public IEnumerable<CollectionRecord> CodexEntries(CollectionKind kind)
    {
        IEnumerable<string> ids = kind == CollectionKind.Equipment ? Items.Where(x => !x.IsStackable).Select(x => x.Id) :
            kind == CollectionKind.Secret ? codex?.Secrets.Select(x => x.Id) ?? Enumerable.Empty<string>() :
            codex?.Enemies.Where(x => x != null && (x.Behavior == EnemyBehaviorStyle.Boss) == (kind == CollectionKind.Boss)).Select(x => x.name) ?? Enumerable.Empty<string>();
        return ids.Select(id => world.collection.FirstOrDefault(x => x.kind == kind && x.id == id)?.Copy() ?? new CollectionRecord { kind = kind, id = id })
            .OrderByDescending(x => x.count > 0).ThenBy(x => x.id);
    }
    public string CodexDescription(CollectionRecord e)
    {
        if (e.count == 0) return e.kind == CollectionKind.Secret ? "未发现区域 · 在副本中调查机关与隐藏支路" : e.kind == CollectionKind.Equipment ? "未收集装备 · 拾取或锻造后收录" : "未击败敌人 · 击败后解锁资料";
        if (e.kind == CollectionKind.Equipment) { var item = FindItem(e.id); return $"{item?.DisplayName} · {item?.RarityLabel} · Lv.{item?.RequiredLevel}\n{item?.Description}"; }
        if (e.kind == CollectionKind.Secret) return (codex?.Secrets.FirstOrDefault(x => x.Id == e.id)?.Label ?? e.id) + "\n已探索 · 可在每趟副本重新开启";
        var enemy = codex?.Enemies.FirstOrDefault(x => x.name == e.id);
        return $"{enemy?.DisplayName ?? e.id} · 击败 {e.count} 次\n基础生命 {enemy?.Health} · {(e.kind == CollectionKind.Boss ? "首领 / 阶段战斗" : enemy?.Behavior == EnemyBehaviorStyle.Ranged ? "远程敌人" : "近战敌人")}";
    }
}
