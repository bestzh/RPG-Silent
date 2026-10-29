using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>由副本服务显式启动，波次结束不等于整个副本结束。</summary>
public sealed class DungeonEncounter : MonoBehaviour
{
    [Serializable] public sealed class Spawn { public EnemyController Prefab; public Transform Point; public EnemyEliteTrait Trait; }
    [Serializable] public sealed class Wave { public string Name; public Spawn[] Enemies; }
    public Wave[] Waves;
    public float WaveDelay = 2.5f;
    public EnemyGroupSettings GroupCombat = new();
    public EnemyCombatGroup CombatGroup { get; private set; }
    private readonly HashSet<EnemyController> alive = new();
    private bool running;
    private int waveIndex = -1;
    public string Objective { get; private set; } = "准备战斗";
    public bool IsComplete { get; private set; }
    public int TotalEnemies
    {
        get { int total = 0; if (Waves != null) foreach (var wave in Waves) total += wave.Enemies?.Length ?? 0; return total; }
    }
    public event Action<EnemyController> Spawned;
    public event Action Changed;
    public event Action Completed;
    public event Action<string> Failed;

    public void Begin()
    {
        if (running || IsComplete) return;
        if (Waves == null || Waves.Length == 0) { Failed?.Invoke("关卡未配置敌人波次。"); return; }
        foreach (var wave in Waves)
        {
            if (wave.Enemies == null || wave.Enemies.Length == 0) { Failed?.Invoke("关卡存在空波次。"); return; }
            foreach (var spawn in wave.Enemies)
                if (spawn.Prefab == null || spawn.Point == null || !Enum.IsDefined(typeof(EnemyEliteTrait), spawn.Trait) ||
                    spawn.Trait != EnemyEliteTrait.None && spawn.Prefab.GetComponent<BossPhaseController>() != null)
                { Failed?.Invoke("关卡敌人、出生点或精英特性配置无效。"); return; }
        }
        if (DungeonLaunchContext.Challenge == DungeonChallenge.BossTrial)
        {
            var bosses = new List<Spawn>();
            foreach (var wave in Waves) foreach (var spawn in wave.Enemies)
                if (spawn.Prefab.GetComponent<BossPhaseController>() != null) bosses.Add(spawn);
            if (bosses.Count == 0) { Failed?.Invoke("本关未配置首领。"); return; }
            Waves = new[] { new Wave { Name = "首领试炼", Enemies = bosses.ToArray() } };
        }
        running = true;
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (!running) return;
        ++waveIndex;
        CombatGroup?.Stop();
        CombatGroup = new EnemyCombatGroup(GroupCombat ?? new EnemyGroupSettings());
        var wave = Waves[waveIndex];
        Objective = $"第 {waveIndex + 1}/{Waves.Length} 波 · {wave.Name}";
        foreach (var spawn in wave.Enemies)
        {
            var enemy = Instantiate(spawn.Prefab, spawn.Point.position, spawn.Point.rotation, transform);
            if (spawn.Trait != EnemyEliteTrait.None) enemy.TryConfigureEliteTrait(spawn.Trait);
            if (!enemy.IsBoss) enemy.BindCombatGroup(CombatGroup);
            alive.Add(enemy);
            enemy.Died += OnEnemyDied;
            Spawned?.Invoke(enemy);
        }
        Changed?.Invoke();
    }

    private void OnEnemyDied(EnemyController enemy)
    {
        enemy.Died -= OnEnemyDied;
        if (!running || !alive.Remove(enemy) || alive.Count != 0) return;
        if (waveIndex + 1 >= Waves.Length)
        {
            running = false;
            IsComplete = true;
            Objective = "关卡目标已完成";
            StartCoroutine(CompleteNextFrame());
        }
        else StartCoroutine(NextWave());
    }

    private IEnumerator NextWave()
    {
        Objective = "区域已清理，下一波即将出现";
        Changed?.Invoke();
        yield return new WaitForSeconds(WaveDelay);
        SpawnWave();
    }

    private IEnumerator CompleteNextFrame()
    {
        // 等本次死亡的所有监听者完成奖励记账。
        yield return null;
        Completed?.Invoke();
    }

    public void Stop()
    {
        running = false;
        StopAllCoroutines();
        CombatGroup?.Stop();
        foreach (var enemy in alive) if (enemy != null) enemy.Died -= OnEnemyDied;
        alive.Clear();
    }
    private void OnDisable() => Stop();
}
