using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum DungeonRunState { Inactive, Running, Succeeded, Failed, Leaving }

/// <summary>Owns encounter outcome and scene-local drops; rewards commit only on manual pickup.</summary>
public sealed class DungeonRunService : IDisposable
{
    public const string UiKey = "UI/DungeonUI";
    private readonly PlayerStats stats;
    private readonly IPlayerInputActions input;
    private readonly ICursorService cursor;
    private readonly IUIService ui;
    private readonly ISceneLoader loader;
    private readonly PlayerSessionService session;
    private readonly PlayerProgressService progress;
    private readonly List<WorldLootPickup> drops = new();
    private readonly System.Random random = new();
    private DungeonLootTable lootTable;
    private LootPresentation lootPresentation;
    public bool ResultVisible { get; private set; }
    public int RemainingDrops => drops.Count(x => x != null && !x.IsCollected);
    public int CollectedItems { get; private set; }
    private readonly HashSet<EnemyController> remaining = new();
    private readonly List<EnemyController> enemies = new();
    private PlayerController player;
    private bool disposed;
    private DungeonRunState settledState;
    private DungeonEncounter encounter;
    private DungeonDatabase.DifficultyTier difficultyTier;
    public EnemyController Boss { get; private set; }
    public string Objective => encounter != null ? encounter.Objective : "清除敌人";

    public DungeonRunState State { get; private set; }
    public DungeonDatabase.Entry Dungeon { get; private set; }
    public DungeonDifficulty Difficulty { get; private set; }
    public int TotalEnemies { get; private set; }
    public int DefeatedEnemies { get; private set; }
    public int AwardedGold { get; private set; }
    public int AwardedExp { get; private set; }
    public string FailureReason { get; private set; }
    public string TravelError { get; private set; }
    public bool HasResult => settledState == DungeonRunState.Succeeded || settledState == DungeonRunState.Failed;
    public bool Succeeded => settledState == DungeonRunState.Succeeded;
    public event Action Changed;

    public DungeonRunService(PlayerStats stats, IPlayerInputActions input, ICursorService cursor,
        IUIService ui, ISceneLoader loader, PlayerSessionService session, PlayerProgressService progress)
    {
        this.stats = stats; this.input = input; this.cursor = cursor;
        this.ui = ui; this.loader = loader; this.session = session;
        this.progress = progress;
    }

    public void Initialize(Scene scene)
    {
        if (disposed || State != DungeonRunState.Inactive || !DungeonLaunchContext.HasValue) return;
        var database = Resources.Load<DungeonDatabase>("DungeonDatabase");
        Dungeon = database != null ? database.GetById(DungeonLaunchContext.DungeonId) : null;
        if (Dungeon == null || !Dungeon.MatchesScene(scene.name)) return;
        Difficulty = DungeonLaunchContext.Difficulty;
        lootTable = Resources.Load<DungeonLootTable>("Loot/Dungeon" + Dungeon.Id);
        lootPresentation = Resources.Load<LootPresentation>("Loot/Presentation");
        var tier = Dungeon.GetDifficultyTier(Difficulty);
        difficultyTier = tier;
        player = session.PlayerRig != null ? session.PlayerRig.GetComponentInChildren<PlayerController>(true) : null;
        State = DungeonRunState.Running;
        ui.OpenUI(UiKey);
        if (!Dungeon.IsEnabled || tier == null || player == null || lootTable == null || lootPresentation == null)
        {
            Finish(false, "副本配置不可用，请返回主城。");
            return;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            encounter = root.GetComponentInChildren<DungeonEncounter>();
            if (encounter != null) break;
        }
        stats.OnHealthChanged += OnHealthChanged;
        if (encounter != null)
        {
            TotalEnemies = encounter.TotalEnemies;
            encounter.Spawned += RegisterEnemy;
            encounter.Changed += OnEncounterChanged;
            encounter.Completed += OnEncounterCompleted;
            encounter.Failed += OnEncounterFailed;
            if (stats.IsDead) Finish(false, "角色已倒下。");
            else encounter.Begin();
            return;
        }

        foreach (var root in scene.GetRootGameObjects())
            foreach (var enemy in root.GetComponentsInChildren<EnemyController>())
            {
                // GetComponentsInChildren(false) 仍会返回非激活根节点自身的组件。
                if (!enemy.gameObject.activeInHierarchy || enemy.IsDead) continue;
                RegisterEnemy(enemy);
            }
        TotalEnemies = remaining.Count;
        if (TotalEnemies == 0) Finish(false, "未找到副本目标，请返回主城。");
        else if (stats.IsDead) Finish(false, "角色已倒下。");
        else Changed?.Invoke();
    }

    private void RegisterEnemy(EnemyController enemy)
    {
        if (disposed || State != DungeonRunState.Running || !remaining.Add(enemy)) return;
        enemy.ConfigureDungeon(difficultyTier);
        enemies.Add(enemy);
        enemy.Died += OnEnemyDied;
        if (enemy.IsBoss)
        {
            Boss = enemy;
            enemy.Changed += OnBossChanged;
        }
        Changed?.Invoke();
    }

    private void OnBossChanged(EnemyController enemy) => Changed?.Invoke();
    private void OnEncounterChanged() => Changed?.Invoke();
    private void OnEncounterFailed(string reason) => Finish(false, reason);
    private void OnEncounterCompleted()
    {
        if (State != DungeonRunState.Running) return;
        if (stats.IsDead) Finish(false, "角色已倒下。");
        else if (remaining.Count == 0) Finish(true);
    }

    private void OnEnemyDied(EnemyController enemy)
    {
        if (disposed || State != DungeonRunState.Running || !remaining.Remove(enemy)) return;
        enemy.Died -= OnEnemyDied;
        ++DefeatedEnemies;
        progress.RecordQuestDungeonEvent(Dungeon.Id, false);
        SpawnDrops(enemy);
        if (stats.IsDead) Finish(false, "角色已倒下。");
        else if (encounter == null && remaining.Count == 0) Finish(true);
        else Changed?.Invoke();
    }

    private void OnHealthChanged(int health, int maxHealth)
    {
        if (health <= 0 && State == DungeonRunState.Running) Finish(false, "角色已倒下。");
    }

    private void Finish(bool success, string reason = null)
    {
        if (disposed || State != DungeonRunState.Running) return;
        State = settledState = success ? DungeonRunState.Succeeded : DungeonRunState.Failed;
        if (success) progress.RecordQuestDungeonEvent(Dungeon.Id, true);
        player?.Statuses?.State.Clear();
        FailureReason = reason;
        encounter?.Stop();
        stats.OnHealthChanged -= OnHealthChanged;
        input.SetGameplayInputBlocked(this, true);
        cursor.EnterUICursor();
        ResultVisible = true;
        if (!success) player?.LockForDungeonResult();
        foreach (var enemy in enemies)
            if (enemy != null) enemy.LockForDungeonResult();
        ui.SetRaycastEnabled("UI/MainUI", false);
        Changed?.Invoke();
    }

    private void SpawnDrops(EnemyController enemy)
    {
        var items = lootTable.RollEnemy(Difficulty, enemy.IsBoss, random);
        for (int i = 0; i <= items.Length; i++)
        {
            var go = new GameObject("World loot"); SceneManager.MoveGameObjectToScene(go, enemy.gameObject.scene);
            Vector3 position = enemy.transform.position;
            Vector3 candidate = position + Quaternion.Euler(0, i * 137.5f, 0) * Vector3.forward * (i == 0 ? .25f : .9f);
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>(); UnityEngine.AI.NavMeshHit nav;
            if (agent != null && UnityEngine.AI.NavMesh.SamplePosition(candidate, out nav, 1f,
                new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = UnityEngine.AI.NavMesh.AllAreas }))
                position = nav.position;
            var ground = Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.normal.y > .6f && h.collider.GetComponentInParent<EnemyController>() == null && h.collider.GetComponentInParent<PlayerController>() == null)
                .OrderBy(h => h.distance).ToArray();
            if (ground.Length > 0) position = ground[0].point;
            go.transform.position = position + Vector3.up * .03f;
            var drop = go.AddComponent<WorldLootPickup>();
            drop.InitializeRolled(i == 0 ? null : progress.FindItem(items[i - 1].itemId), i == 0 ? null : items[i - 1],
                i == 0 ? enemy.RewardGold : 0, i == 0 ? enemy.RewardExp : 0, lootPresentation);
            drop.Collected += OnDropCollected; drops.Add(drop);
        }
    }
    void OnDropCollected(WorldLootPickup pickup)
    {
        AwardedGold += pickup.Gold; AwardedExp += pickup.Experience;
        if (pickup.Item != null) CollectedItems += pickup.Count;
        pickup.Collected -= OnDropCollected; Changed?.Invoke();
    }
    public void ContinueLooting()
    {
        if (!Succeeded || disposed || State == DungeonRunState.Leaving || stats.IsDead) return;
        ResultVisible = false; input.SetGameplayInputBlocked(this, false); ui.SetRaycastEnabled("UI/MainUI", true);
        if (input.GameplayInputEnabled) cursor.EnterGameplayCursor(); Changed?.Invoke();
    }
    public void ShowResult()
    {
        if (!HasResult || disposed || State == DungeonRunState.Leaving) return;
        ResultVisible = true; input.SetGameplayInputBlocked(this, true); cursor.EnterUICursor();
        ui.SetRaycastEnabled("UI/MainUI", false); Changed?.Invoke();
    }
    public void Retry() => Leave(Dungeon?.SceneKey);
    public void ReturnToMain() => Leave("Scenes/Main");

    private void Leave(string sceneKey)
    {
        if (disposed || !HasResult || State == DungeonRunState.Leaving || loader.IsLoading ||
            ui.IsUIOpen("UI/LoadingUI") || string.IsNullOrEmpty(sceneKey)) return;
        if (!progress.Save()) { TravelError = "进度尚未保存，无法离开。请检查存储后再次点击重试或返回。"; Changed?.Invoke(); return; }
        TravelError = string.Empty;
        State = DungeonRunState.Leaving;
        ResultVisible = true;
        input.SetGameplayInputBlocked(this, true); cursor.EnterUICursor();
        Changed?.Invoke();
        // 直到目的场景激活，才由出生器复活/清上下文；失败保留本次结果与奖励。
        ui.OpenUI("UI/LoadingUI", sceneKey, "UI/MainUI", (Action)(() =>
        {
            if (disposed) return;
            State = settledState;
            ui.SetRaycastEnabled("UI/MainUI", false);
            Changed?.Invoke();
        }), "返回结算");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (encounter != null)
        {
            encounter.Stop();
            encounter.Spawned -= RegisterEnemy;
            encounter.Changed -= OnEncounterChanged;
            encounter.Completed -= OnEncounterCompleted;
            encounter.Failed -= OnEncounterFailed;
        }
        stats.OnHealthChanged -= OnHealthChanged;
        foreach (var enemy in enemies)
            if (enemy != null) { enemy.Died -= OnEnemyDied; enemy.Changed -= OnBossChanged; }
        remaining.Clear();
        enemies.Clear();
        foreach (var pickup in drops) if (pickup != null) pickup.Collected -= OnDropCollected;
        drops.Clear();
        Changed = null;
        input.SetGameplayInputBlocked(this, false);
    }
}
