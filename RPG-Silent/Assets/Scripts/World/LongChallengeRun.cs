using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public enum LongChallengePhase { Fighting, PendingSave, Resting, Finished }

/// <summary>One attempt, generated one round at a time. A cleared round commits before recovery or continuation.</summary>
public sealed class LongChallengeRun : MonoBehaviour
{
    DungeonRunService owner;
    PlayerProgressService progress;
    PlayerStats stats;
    PlayerController player;
    DungeonDatabase.DifficultyTier baseTier;
    LongChallengeArena arena;
    ChallengeAttemptData token;
    readonly List<EnemyController> actors = new();
    readonly HashSet<EnemyController> alive = new();
    EnemyCombatGroup group;
    double roundStarted, outsideAt = -1;
    float nextRefresh;
    int pendingMilliseconds;
    bool pendingStage, pendingEnd, retired, stopped;
    public LongChallengePhase Phase { get; private set; }
    public DungeonDatabase.DifficultyTier EnemyTier { get; private set; }
    public ChallengeAttemptData Attempt => token.Copy();
    public int Stage => token.clearedStage + (Phase == LongChallengePhase.Fighting || pendingStage ? 1 : 0);
    public bool NeedsSave => pendingStage || pendingEnd;
    public string SaveError { get; private set; }
    public ChallengePayout LastPayout { get; private set; }
    public IReadOnlyList<EnemyController> Actors => actors;
    public double RemainingSeconds => Phase == LongChallengePhase.Fighting ? Math.Max(0, 600 - (Time.timeAsDouble - roundStarted)) : 0;
    public string Objective => Phase == LongChallengePhase.Fighting ? $"第{Stage}/{LongChallengeRules.StageLimit(token.mode)}轮 · 剩余 {Math.Ceiling(RemainingSeconds)}秒 · 距中心 {(player != null ? Vector3.Distance(player.transform.position, arena.Center) : 0):0}/{arena.Radius:0}米" +
        (outsideAt >= 0 ? $" · {Math.Ceiling(Math.Max(0, 5 - (Time.timeAsDouble - outsideAt)))}秒内返回区域" : "") : Phase == LongChallengePhase.Resting ? "本轮已保存 · 选择继续或结束" : NeedsSave ? "本轮尚未保存 · 请重试" : "挑战已结束";
    public string Summary
    {
        get
        {
            var record = progress.ChallengeRecord(token.mode, token.dungeonId, token.difficulty);
            return $"{LongChallengeRules.Label(token.mode)} · 已完成 {token.clearedStage}/{LongChallengeRules.StageLimit(token.mode)}轮\n" +
                $"本次 {token.score:N0} 分 · 战斗用时 {LongChallengeRules.Duration(token.combatMilliseconds)}\n" +
                $"已保存奖励 {token.awardedGold} 金币 / {token.awardedGold} 经验（首次 {token.firstGold}）\n" +
                (LastPayout.Total > 0 ? $"上一轮：重复 {LastPayout.Repeat}，首次 {LastPayout.First} 金币／经验\n" : "") + LongChallengeRules.RecordSummary(record) +
                (NeedsSave ? "\n尚未保存：" + SaveError : !string.IsNullOrEmpty(SaveError) ? "\n继续失败：" + SaveError : "") +
                "\n每轮10分钟，休整不计时；重开从第1轮开始，首次奖励标记保留。";
        }
    }
    public void Initialize(DungeonRunService run, PlayerProgressService saves, PlayerStats vitals, PlayerController actor,
        DungeonDatabase.DifficultyTier tier, LongChallengeArena layout, ChallengeAttemptData attempt)
    {
        owner = run; progress = saves; stats = vitals; player = actor; baseTier = tier; arena = layout; token = attempt.Copy();
        StartRound();
    }
    void StartRound()
    {
        owner.ReleaseLongRound(actors); actors.Clear(); alive.Clear(); group?.Stop(); group = new EnemyCombatGroup(new EnemyGroupSettings());
        player.ResetForScene(arena.PlayerSpawn + Vector3.up * .08f, Quaternion.LookRotation(arena.BossSpawn - arena.PlayerSpawn));
        Phase = LongChallengePhase.Fighting; roundStarted = Time.timeAsDouble; outsideAt = -1;
        int stage = token.clearedStage + 1;
        EnemyTier = new DungeonDatabase.DifficultyTier { Difficulty = baseTier.Difficulty,
            EnemyHealthMultiplier = baseTier.EnemyHealthMultiplier * (float)LongChallengeRules.HealthScale(token.mode, stage),
            EnemyDamageMultiplier = baseTier.EnemyDamageMultiplier * (float)LongChallengeRules.DamageScale(token.mode, stage), RewardMultiplier = 0 };
        if (LongChallengeRules.BossStage(token.mode, stage))
        {
            int bossIndex = token.mode == LongChallengeMode.BossRush ? (stage - 1) % 2 : (stage / 5 - 1) % 2;
            Spawn(arena.Bosses[bossIndex], arena.BossSpawn, EnemyEliteTrait.None);
        }
        else
        {
            Spawn(stage % 2 == 1 ? arena.Melee : arena.Ranged, arena.GroupSpawns[0], (EnemyEliteTrait)(1 + (stage - 1) % 3));
            Spawn(stage % 2 == 1 ? arena.Ranged : arena.Melee, arena.GroupSpawns[1], stage >= 6 ? EnemyEliteTrait.Mender : EnemyEliteTrait.None);
            if (stage >= 3) Spawn(arena.Melee, arena.GroupSpawns[2], stage >= 11 ? EnemyEliteTrait.Frenzy : EnemyEliteTrait.None);
        }
        owner.SetLongChallengeRest(false); owner.NotifyLongChallenge();
    }
    void Spawn(EnemyController prefab, Vector3 point, EnemyEliteTrait trait)
    {
        var enemy = Instantiate(prefab, point, Quaternion.LookRotation(arena.PlayerSpawn - point), transform);
        if (trait != EnemyEliteTrait.None) enemy.TryConfigureEliteTrait(trait);
        if (!enemy.IsBoss) enemy.BindCombatGroup(group);
        actors.Add(enemy); alive.Add(enemy); owner.RegisterOptionalEnemy(enemy);
    }
    public void EnemyDefeated(EnemyController enemy)
    {
        if (stopped || Phase != LongChallengePhase.Fighting || !alive.Remove(enemy) || alive.Count != 0) return;
        double elapsed = (Time.timeAsDouble - roundStarted) * 1000;
        if (stats.IsDead || elapsed > LongChallengeRules.RoundMilliseconds)
        { owner.FinishLongChallenge(false, stats.IsDead ? "角色已倒下。" : "本轮超过10分钟，挑战失败。"); return; }
        pendingMilliseconds = Math.Max(1, (int)Math.Ceiling(elapsed));
        pendingStage = true; Phase = LongChallengePhase.PendingSave; group?.Stop(); owner.SetLongChallengeRest(true); RetrySave();
    }
    public bool RetrySave()
    {
        if (pendingStage)
        {
            if (!progress.TryCheckpointLongChallenge(token, token.clearedStage + 1, pendingMilliseconds, out var payout, out var error))
            { SaveError = error; owner.NotifyLongChallenge(); return false; }
            pendingStage = false; token = progress.ChallengeCareer.current; LastPayout = payout; SaveError = null;
            stats.Heal(Math.Max(1, stats.MaxHealth / 4)); stats.RecoverEnergy(stats.MaxEnergy * .25f);
            if (token.status == ChallengeAttemptStatus.Completed)
            { Phase = LongChallengePhase.Finished; owner.FinishLongChallenge(true); }
            else { Phase = LongChallengePhase.Resting; owner.NotifyLongChallenge(); }
        }
        if (pendingEnd)
        {
            if (!progress.TryEndLongChallenge(token, retired, out var error)) { SaveError = error; owner.NotifyLongChallenge(); return false; }
            token = progress.ChallengeCareer.current; pendingEnd = false; SaveError = null; owner.NotifyLongChallenge();
        }
        return true;
    }
    public void Continue()
    {
        if (stopped || Phase != LongChallengePhase.Resting || stats.IsDead || owner.State != DungeonRunState.Running) return;
        if (!progress.TryResumeLongChallenge(token, out var error)) { SaveError = error; owner.NotifyLongChallenge(); return; }
        token = progress.ChallengeCareer.current; SaveError = null; StartRound();
    }
    public void Retire()
    {
        if (stopped || owner.State != DungeonRunState.Running || !RetrySave() || Phase == LongChallengePhase.Finished) return;
        retired = true; owner.FinishLongChallenge(false, "主动结束挑战；已保存的奖励与纪录保留。");
    }
    public void OnRunFinished(bool success)
    {
        Phase = LongChallengePhase.Finished; group?.Stop();
        foreach (var enemy in actors) if (enemy != null && !enemy.IsDead) enemy.LockForDungeonResult();
        if (!success && token.status != ChallengeAttemptStatus.Completed)
        { pendingEnd = true; RetrySave(); }
    }
    void Update()
    {
        if (stopped || Phase != LongChallengePhase.Fighting || owner.State != DungeonRunState.Running) return;
        // Session teardown can destroy the persistent rig before the asynchronous scene unload.
        // Keep the saved checkpoint; stop scene actors rather than touching the old controller.
        if (player == null || !player.isActiveAndEnabled) { Stop(); return; }
        if (RemainingSeconds <= 0) { owner.FinishLongChallenge(false, "本轮超过10分钟，挑战失败。"); return; }
        if (Vector3.Distance(player.transform.position, arena.Center) > arena.Radius)
        {
            if (outsideAt < 0) outsideAt = Time.timeAsDouble;
            if (Time.timeAsDouble - outsideAt > 5) { owner.FinishLongChallenge(false, "离开挑战区域超过5秒，挑战结束。"); return; }
        }
        else outsideAt = -1;
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .2f; owner.NotifyLongChallenge(); }
    }
    public void Stop()
    {
        if (stopped) return; stopped = true; group?.Stop();
        foreach (var enemy in actors) if (enemy != null && !enemy.IsDead) enemy.LockForDungeonResult();
        alive.Clear();
    }
    void OnDestroy() => Stop();
}
