using System;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum DungeonRunState { Inactive, Running, Succeeded, Failed, Leaving }

/// <summary>场景作用域内的一次副本；敌人先记账，通关时一次结算，卸载即释放。</summary>
public sealed class DungeonRunService : IDisposable
{
    public const string UiKey = "UI/DungeonUI";
    private readonly PlayerStats stats;
    private readonly IPlayerInputActions input;
    private readonly ICursorService cursor;
    private readonly IUIService ui;
    private readonly ISceneLoader loader;
    private readonly PlayerSessionService session;
    private readonly HashSet<EnemyController> remaining = new();
    private readonly List<EnemyController> enemies = new();
    private PlayerController player;
    private bool disposed;
    private int pendingGold, pendingExp;
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
    public bool HasResult => settledState == DungeonRunState.Succeeded || settledState == DungeonRunState.Failed;
    public bool Succeeded => settledState == DungeonRunState.Succeeded;
    public event Action Changed;

    public DungeonRunService(PlayerStats stats, IPlayerInputActions input, ICursorService cursor,
        IUIService ui, ISceneLoader loader, PlayerSessionService session)
    {
        this.stats = stats; this.input = input; this.cursor = cursor;
        this.ui = ui; this.loader = loader; this.session = session;
    }

    public void Initialize(Scene scene)
    {
        if (disposed || State != DungeonRunState.Inactive || !DungeonLaunchContext.HasValue) return;
        var database = Resources.Load<DungeonDatabase>("DungeonDatabase");
        Dungeon = database != null ? database.GetById(DungeonLaunchContext.DungeonId) : null;
        if (Dungeon == null || !Dungeon.MatchesScene(scene.name)) return;
        Difficulty = DungeonLaunchContext.Difficulty;
        var tier = Dungeon.GetDifficultyTier(Difficulty);
        difficultyTier = tier;
        player = session.PlayerRig != null ? session.PlayerRig.GetComponentInChildren<PlayerController>(true) : null;
        State = DungeonRunState.Running;
        ui.OpenUI(UiKey);
        if (!Dungeon.IsEnabled || tier == null || player == null)
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
        pendingGold += enemy.RewardGold;
        pendingExp += enemy.RewardExp;
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
        FailureReason = reason;
        encounter?.Stop();
        stats.OnHealthChanged -= OnHealthChanged;
        input.SetGameplayInputBlocked(this, true);
        cursor.EnterUICursor();
        player?.LockForDungeonResult();
        foreach (var enemy in enemies)
            if (enemy != null) enemy.LockForDungeonResult();
        if (success)
        {
            AwardedGold = pendingGold;
            AwardedExp = pendingExp;
            stats.AddReward(AwardedGold, AwardedExp);
        }
        ui.SetRaycastEnabled("UI/MainUI", false);
        Changed?.Invoke();
    }

    public void Retry() => Leave(Dungeon?.SceneKey);
    public void ReturnToMain() => Leave("Scenes/Main");

    private void Leave(string sceneKey)
    {
        if (disposed || !HasResult || State == DungeonRunState.Leaving || loader.IsLoading ||
            ui.IsUIOpen("UI/LoadingUI") || string.IsNullOrEmpty(sceneKey)) return;
        State = DungeonRunState.Leaving;
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
        Changed = null;
        input.SetGameplayInputBlocked(this, false);
    }
}
