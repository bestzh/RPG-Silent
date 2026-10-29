using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class DungeonRunService
{
    DungeonExpeditionEvent expeditionEvent;
    ExpeditionData expeditionToken;
    bool expeditionRetryPrepared;
    public bool IsExpedition => Challenge == DungeonChallenge.Expedition;
    public bool ExpeditionSavePending => expeditionEvent != null && expeditionEvent.NeedsSave;
    public bool CanAbandonExpedition => State == DungeonRunState.Running && expeditionEvent != null && !expeditionEvent.Terminal;
    public string ExpeditionSummary => expeditionEvent != null ? expeditionEvent.Summary : "";
    public DungeonExpeditionEvent ExpeditionEvent => expeditionEvent;

    bool ConfigureExpedition(out string error)
    {
        error = null;
        if (!IsExpedition) return true;
        expeditionToken = progress.Expedition;
        if (!expeditionToken.HasValue || expeditionToken.id != DungeonLaunchContext.ExpeditionId || expeditionToken.dungeonId != Dungeon.Id ||
            expeditionToken.difficulty != (int)Difficulty || expeditionToken.status != ExpeditionEventStatus.Available || encounter == null)
        { error = "远征入口记录不匹配，请返回主城重新选择。"; return false; }
        var definition = Resources.Load<DungeonVariationDefinition>("Expeditions/Dungeon" + Dungeon.Id);
        var nodes = runScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ExplorationNode>(true)).ToArray();
        if (definition == null) { error = "未找到远征路线配置。"; return false; }
        if (!definition.ValidateRoute(Dungeon, nodes, out error)) return false;
        var plan = new ExpeditionPlan(expeditionToken);
        encounter.Waves = encounter.Waves.Select(w =>
        {
            var ordinary = w.Enemies.Where(s => s.Prefab.GetComponent<BossPhaseController>() == null).ToArray();
            int slot = 0;
            return new DungeonEncounter.Wave { Name = w.Name + " · " + plan.FormationLabel, Enemies = w.Enemies.Select(s =>
            {
                if (s.Prefab.GetComponent<BossPhaseController>() != null) return new DungeonEncounter.Spawn { Prefab = s.Prefab, Point = s.Point };
                int index = slot++;
                var prefab = plan.Formation == 1 ? (s.Prefab.Definition.Behavior == EnemyBehaviorStyle.Melee ? definition.Ranged : definition.Melee) :
                    plan.Formation == 2 ? (index + 1 == ordinary.Length && ordinary.Length > 1 ? definition.Ranged : definition.Melee) : s.Prefab;
                int[] traits = plan.TraitRotation == 0 ? new[] { 1, 3 } : plan.TraitRotation == 1 ? new[] { 2, 1 } : new[] { 3, 2 };
                return new DungeonEncounter.Spawn { Prefab = prefab, Point = s.Point, Trait = index < 2 ? (EnemyEliteTrait)traits[index] : EnemyEliteTrait.None };
            }).ToArray() };
        }).ToArray();
        for (int i = 0; i < definition.OptionalNodeIds.Length; i++)
            if ((plan.OptionalNodes & (1 << i)) == 0) nodes.Single(n => n.PersistentId == definition.OptionalNodeIds[i]).gameObject.SetActive(false);
        var root = new GameObject("Expedition event");
        expeditionEvent = root.AddComponent<DungeonExpeditionEvent>();
        expeditionEvent.Initialize(this, progress, player, expeditionToken, plan, definition);
        return true;
    }
    void TryCompleteRun()
    {
        if (State != DungeonRunState.Running || remaining.Count != 0 || encounter != null && !encounter.IsComplete) return;
        if (stats.IsDead) Finish(false, "角色已倒下。");
        else if (expeditionEvent == null || expeditionEvent.Settled) Finish(true);
        else Changed?.Invoke();
    }
    public bool TryStartExpeditionEvent(PlayerController actor) => actor == player && State == DungeonRunState.Running && expeditionEvent != null && expeditionEvent.TryStart();
    public void AbandonExpeditionEvent() { if (CanAbandonExpedition) expeditionEvent.Abandon(); }
    public bool RetryExpeditionSave() => expeditionEvent == null || expeditionEvent.RetrySave();
    public void ExpeditionChanged() { Changed?.Invoke(); TryCompleteRun(); }
    public void RemoveExpeditionEnemy(EnemyController enemy)
    {
        if (enemy == null) return;
        remaining.Remove(enemy); enemy.Died -= OnEnemyDied; enemy.LockForDungeonResult();
    }
    bool PrepareExpeditionRetry(out string message)
    {
        message = null;
        if (!IsExpedition || expeditionRetryPrepared) return true;
        if (!progress.TryRetryExpedition(expeditionToken, out message)) return false;
        expeditionToken = progress.Expedition; expeditionRetryPrepared = true; return true;
    }
}
