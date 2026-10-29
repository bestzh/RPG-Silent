using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class DungeonRunService
{
    LongChallengeRun longChallenge;
    bool longRetryPrepared;
    public bool IsLongChallenge => LongChallengeRules.IsLong((int)Challenge);
    public LongChallengeRun LongChallenge => longChallenge;
    public bool LongChallengeSavePending => longChallenge != null && longChallenge.NeedsSave;
    public string LongChallengeSummary => longChallenge != null ? longChallenge.Summary : FailureReason ?? "正在准备挑战";
    bool ConfigureLongChallenge(out string error)
    {
        error = null;
        if (!IsLongChallenge) return true;
        var token = progress.ChallengeCareer.current;
        if (!token.HasValue || token.id != DungeonLaunchContext.LongChallengeId || (int)token.mode != (int)Challenge || token.dungeonId != Dungeon.Id ||
            token.difficulty != (int)Difficulty || token.status != ChallengeAttemptStatus.Active || token.clearedStage != 0)
        { error = "长期挑战入口记录不匹配，请返回主城重新选择。"; return false; }
        var arena = Resources.Load<LongChallengeArena>("Challenges/Arena" + Dungeon.Id);
        if (arena == null) { error = "未找到挑战区域配置。"; return false; }
        if (arena.DungeonId != Dungeon.Id || !arena.ValidateArena(out error)) return false;
        encounter?.Stop();
        var root = new GameObject("Long challenge run"); longChallenge = root.AddComponent<LongChallengeRun>();
        longChallenge.Initialize(this, progress, stats, player, difficultyTier, arena, token);
        return true;
    }
    public void SetLongChallengeRest(bool resting)
    {
        if (disposed || State != DungeonRunState.Running) return;
        input.SetGameplayInputBlocked(this, resting); ui.SetRaycastEnabled("UI/MainUI", !resting);
        if (resting) { player?.LockForDungeonResult(); cursor.EnterUICursor(); }
        else if (input.GameplayInputEnabled) cursor.EnterGameplayCursor();
        Changed?.Invoke();
    }
    public void ReleaseLongRound(IEnumerable<EnemyController> actors)
    {
        foreach (var enemy in actors)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied; enemy.Changed -= OnBossChanged;
                remaining.Remove(enemy); enemies.Remove(enemy); if (Boss == enemy) Boss = null;
                enemy.gameObject.SetActive(false); Object.Destroy(enemy.gameObject);
            }
            else enemies.Remove(enemy);
        }
    }
    public void ContinueLongChallenge() => longChallenge?.Continue();
    public bool RetryLongChallengeSave() => longChallenge == null || longChallenge.RetrySave();
    public void EndLongChallenge() => longChallenge?.Retire();
    public void NotifyLongChallenge() => Changed?.Invoke();
    public void FinishLongChallenge(bool success, string reason = null) => Finish(success, reason);
    bool PrepareLongChallengeRetry(out string error)
    {
        error = null;
        if (!IsLongChallenge || longRetryPrepared) return true;
        var token = longChallenge != null ? longChallenge.Attempt : progress.ChallengeCareer.current;
        if (!progress.TryRestartLongChallenge(token, out error)) return false;
        DungeonLaunchContext.SetLongChallenge(progress.ChallengeCareer.current.id); longRetryPrepared = true; return true;
    }
}
