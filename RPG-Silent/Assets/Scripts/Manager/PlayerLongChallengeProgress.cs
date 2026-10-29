using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    public ChallengeCareerData ChallengeCareer => world.challenges.Copy();
    public ChallengeRecordData ChallengeRecord(LongChallengeMode mode, int dungeon, int difficulty) => world.challenges.Find(mode, dungeon, difficulty);
    public bool TryBeginLongChallenge(LongChallengeMode mode, int dungeon, int difficulty, long expectedRevision, out string message)
    {
        message = "请在主城重新选择长期挑战。";
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        var database = Resources.Load<DungeonDatabase>("DungeonDatabase"); var entry = database != null ? database.GetById(dungeon) : null;
        if (!attached || !CanManageInventory || player == null || player.CombatAllowed || !ChallengeCareerData.ValidKey(mode, dungeon, difficulty) ||
            entry == null || !entry.IsEnabled || entry.IsTraining || entry.GetDifficultyTier((DungeonDifficulty)difficulty) == null) return false;
        if (expectedRevision != world.challenges.revision || expectedRevision == long.MaxValue) { message = "挑战记录已变化，请重新选择。"; return false; }
        return CommitNewLongAttempt(mode, dungeon, difficulty, out message);
    }
    bool CommitNewLongAttempt(LongChallengeMode mode, int dungeon, int difficulty, out string message)
    {
        var data = Capture(); data.world.challenges.revision++;
        data.world.challenges.current = new ChallengeAttemptData { id = Guid.NewGuid().ToString("N"), sequence = data.world.challenges.revision,
            mode = mode, dungeonId = dungeon, difficulty = difficulty, status = ChallengeAttemptStatus.Active };
        return CommitQuest(data, 0, 0, "新一轮挑战已保存，从第1轮开始。", out message);
    }
    bool CurrentLongAttempt(ChallengeAttemptData token) => attached && token != null && world.challenges.current.SameAttempt(token) &&
        world.challenges.current.clearedStage == token.clearedStage && DungeonLaunchContext.HasValue && LongChallengeRules.IsLong((int)DungeonLaunchContext.Challenge) &&
        DungeonLaunchContext.LongChallengeId == token.id && DungeonLaunchContext.DungeonId == token.dungeonId && (int)DungeonLaunchContext.Difficulty == token.difficulty &&
        (int)DungeonLaunchContext.Challenge == (int)token.mode;
    public bool TryCheckpointLongChallenge(ChallengeAttemptData token, int stage, int milliseconds, out ChallengePayout payout, out string message)
    {
        payout = default; message = "本轮已结算或尝试记录已变化，不能重复领取。";
        if (!CurrentLongAttempt(token) || world.challenges.current.status != ChallengeAttemptStatus.Active || !LongChallengeRules.ValidStage(token.mode, stage) ||
            stage != token.clearedStage + 1 || milliseconds < 1 || milliseconds > LongChallengeRules.RoundMilliseconds) return false;
        var data = Capture(); var career = data.world.challenges; var next = career.current;
        var record = career.Find(next.mode, next.dungeonId, next.difficulty);
        payout = LongChallengeRules.Reward(next.mode, stage, next.difficulty, record);
        next.clearedStage = stage; next.score += LongChallengeRules.StageScore(stage, next.difficulty, milliseconds);
        next.combatMilliseconds += milliseconds; next.awardedGold += payout.Total; next.firstGold += payout.First;
        bool completed = stage == LongChallengeRules.StageLimit(next.mode);
        next.status = completed ? ChallengeAttemptStatus.Completed : ChallengeAttemptStatus.Resting;
        if (stage > record.highestStage) { record.highestStage = stage; record.bestMilliseconds = next.combatMilliseconds; }
        else if (stage == record.highestStage && next.combatMilliseconds < record.bestMilliseconds) record.bestMilliseconds = next.combatMilliseconds;
        record.bestScore = Math.Max(record.bestScore, next.score);
        if (next.mode == LongChallengeMode.Gauntlet) record.claimedMilestones = Math.Max(record.claimedMilestones, stage / 5);
        if (completed) { record.firstClearClaimed = true; record.fullClears = Math.Min(1000000, record.fullClears + 1); }
        career.records = career.records.Where(r => r.Key != record.Key).Append(record).ToArray();
        return CommitQuest(data, payout.Total, payout.Total, $"第{stage}轮成绩与奖励已保存：重复 {payout.Repeat}，首次 {payout.First} 金币／经验。", out message);
    }
    public bool TryResumeLongChallenge(ChallengeAttemptData token, out string message)
    {
        message = "休整记录已变化，无法继续。";
        if (!CurrentLongAttempt(token) || world.challenges.current.status != ChallengeAttemptStatus.Resting) return false;
        var data = Capture(); data.world.challenges.current.status = ChallengeAttemptStatus.Active;
        return CommitQuest(data, 0, 0, "下一轮已准备。", out message);
    }
    public bool TryEndLongChallenge(ChallengeAttemptData token, bool retired, out string message)
    {
        message = "挑战已经结束或记录已变化。";
        if (!CurrentLongAttempt(token) || (world.challenges.current.status != ChallengeAttemptStatus.Active && world.challenges.current.status != ChallengeAttemptStatus.Resting)) return false;
        var data = Capture(); data.world.challenges.current.status = retired ? ChallengeAttemptStatus.Retired : ChallengeAttemptStatus.Failed;
        return CommitQuest(data, 0, 0, "挑战结束，已提交的奖励和纪录保留。", out message);
    }
    public bool TryRestartLongChallenge(ChallengeAttemptData token, out string message)
    {
        message = "挑战记录已变化，无法重新开始。";
        if (!CurrentLongAttempt(token) || world.challenges.revision == long.MaxValue ||
            world.challenges.current.status != ChallengeAttemptStatus.Completed && world.challenges.current.status != ChallengeAttemptStatus.Failed && world.challenges.current.status != ChallengeAttemptStatus.Retired) return false;
        return CommitNewLongAttempt(token.mode, token.dungeonId, token.difficulty, out message);
    }
}
