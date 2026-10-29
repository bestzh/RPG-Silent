using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct QuestNavigation
{
    public Transform Target;
    public string Label;
    public float Distance;
    public bool HasTarget => Target != null;
}
public sealed partial class PlayerProgressService
{
    DungeonDatabase navigationDatabase;
    public QuestNavigation QuestDirection(PlayerController player)
    {
        if (player == null || TrackedQuest == null) return new QuestNavigation { Label = "在 J 手札中追踪已接任务" };
        var q = TrackedQuest;
        if (GetQuestStatus(q.Id) == QuestStatus.Ready) return NpcDestination(player, q.Giver, "交付 · " + QuestGiverText(q.Id));
        var counts = ObjectiveCounts(q.Id);
        int index = -1;
        for (int i = 0; i < counts.Length; i++) if (counts[i] < q.Objectives[i].Required) { index = i; break; }
        if (index < 0) return new QuestNavigation { Label = "目标已完成，请返回委托人" };
        var o = q.Objectives[index];
        if (player.CombatAllowed && o.IsDungeonCounter && RPGSilent.Domain.LongChallengeRules.IsLong((int)DungeonLaunchContext.Challenge))
            return new QuestNavigation { Label = "长期挑战不推进此目标；返回主城选择正式副本" };
        bool exploration = o.Kind == QuestObjectiveKind.OpenChest || o.Kind == QuestObjectiveKind.DiscoverSecret;
        if (player.CombatAllowed && exploration && DungeonLaunchContext.Challenge == DungeonChallenge.BossTrial)
            return new QuestNavigation { Label = "首领试炼不开放探索节点；回城选择普通探索或限时挑战" };
        if (o.Kind == QuestObjectiveKind.TalkToNpc) return NpcDestination(player, o.NpcRole, o.Label);
        if (o.Kind == QuestObjectiveKind.LearnSkills || o.Kind == QuestObjectiveKind.AdvanceClass) return NpcDestination(player, TownNpcRole.Mentor, o.Label);
        if (navigationDatabase == null) navigationDatabase = Resources.Load<DungeonDatabase>("DungeonDatabase");
        int dungeon = o.DungeonId > 0 ? o.DungeonId : o.ItemId == "crystal-shard" ? 1002 : 1001;
        var destination = navigationDatabase?.GetById(dungeon);
        string mode = o.Kind == QuestObjectiveKind.CompleteTimed ? " · 选择限时挑战" : o.Kind == QuestObjectiveKind.CompleteBossTrial ? " · 选择首领试炼" : exploration ? " · 选择普通探索" : "";
        if (!player.CombatAllowed)
        {
            var portal = PortalTrigger.Active.FirstOrDefault(x => x != null && x.gameObject.scene == SceneManager.GetActiveScene());
            return Destination(player, portal?.transform, "传送门 → " + destination?.DisplayName + mode);
        }
        if (destination == null || !destination.MatchesScene(SceneManager.GetActiveScene().name))
            return new QuestNavigation { Label = "返回主城，前往" + destination?.DisplayName + mode };
        Transform target = null;
        string label = o.Label;
        if (o.Kind == QuestObjectiveKind.ProfessionTrial)
            label = $"职业考验 · 本趟 {ProfessionTrialCount(o.Trial, player.CombatRecord)}/{o.TrialRequired} · 仍需成功通关";
        if (o.Kind == QuestObjectiveKind.OpenChest || o.Kind == QuestObjectiveKind.DiscoverSecret || o.Kind == QuestObjectiveKind.DefeatElite)
        {
            var kind = o.Kind == QuestObjectiveKind.OpenChest ? ExplorationKind.Chest : o.Kind == QuestObjectiveKind.DiscoverSecret ? ExplorationKind.HiddenPassage : ExplorationKind.EliteEvent;
            var node = ExplorationNode.Active.Where(x => x != null && !x.Used && x.Kind == kind && (!x.Hidden || x.Discovered))
                .OrderBy(x => (x.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
            if (node != null) { target = node.transform; label = node.DisplayName; }
            else if (kind == ExplorationKind.HiddenPassage) label = "探索附近机关，靠近后发现隐藏支路";
            else if (kind == ExplorationKind.Chest) label = "本趟宝箱已开启，重进副本继续探索";
        }
        if (target == null && o.Kind != QuestObjectiveKind.DiscoverSecret && o.Kind != QuestObjectiveKind.OpenChest)
        {
            var enemy = EnemyController.ActiveEnemies.Where(x => x != null && !x.IsDead && !x.IsCompanion && !x.CombatLocked &&
                (o.Kind != QuestObjectiveKind.DefeatElite || x.IsElite && !x.IsBoss))
                .OrderBy(x => (x.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
            if (enemy != null) { target = enemy.transform; if (o.Kind != QuestObjectiveKind.ProfessionTrial) label = enemy.DisplayName; }
            else label = o.Kind == QuestObjectiveKind.DefeatElite ? "本趟精英已清理，重进副本继续悬赏" : "继续副本目标，或重进副本收集";
        }
        return Destination(player, target, label + mode);
    }
    QuestNavigation NpcDestination(PlayerController player, TownNpcRole role, string label)
    {
        var npc = TownNpc.Active.FirstOrDefault(x => x != null && x.Role == role &&
            (role != TownNpcRole.Mentor || x.Profession?.Id == CurrentClass?.FamilyId));
        return Destination(player, npc?.transform, npc == null ? "完成副本后返回主城 · " + label : label);
    }
    static QuestNavigation Destination(PlayerController player, Transform target, string label) => new()
    { Target = target, Label = label, Distance = target == null ? 0 : Vector3.Distance(player.transform.position, target.position) };
}
