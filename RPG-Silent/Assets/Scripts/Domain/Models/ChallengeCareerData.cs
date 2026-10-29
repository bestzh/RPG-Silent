using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    // Values match appended DungeonChallenge modes; existing serialized mode values stay unchanged.
    public enum LongChallengeMode { BossRush = 4, Gauntlet = 5 }
    public enum ChallengeAttemptStatus { None, Active, Resting, Completed, Failed, Retired }
    [Serializable] public sealed class ChallengeAttemptData
    {
        public string id = "";
        public long sequence;
        public LongChallengeMode mode;
        public int dungeonId, difficulty, clearedStage, awardedGold, firstGold;
        public long score, combatMilliseconds;
        public ChallengeAttemptStatus status;
        public bool HasValue => !string.IsNullOrEmpty(id);
        public ChallengeAttemptData Copy() => (ChallengeAttemptData)MemberwiseClone();
        public bool SameAttempt(ChallengeAttemptData other) => other != null && id == other.id && sequence == other.sequence && mode == other.mode && dungeonId == other.dungeonId && difficulty == other.difficulty;
    }
    [Serializable] public sealed class ChallengeRecordData
    {
        public LongChallengeMode mode;
        public int dungeonId, difficulty, highestStage, claimedMilestones, fullClears;
        public long bestScore, bestMilliseconds;
        public bool firstClearClaimed;
        public ChallengeRecordData Copy() => (ChallengeRecordData)MemberwiseClone();
        public string Key => (int)mode + ":" + dungeonId + ":" + difficulty;
    }
    [Serializable] public sealed class ChallengeCareerData
    {
        public long revision;
        public ChallengeAttemptData current = new();
        public ChallengeRecordData[] records = Array.Empty<ChallengeRecordData>();
        public ChallengeCareerData Copy() => new() { revision = revision, current = current.Copy(), records = records.Select(r => r.Copy()).ToArray() };
        public ChallengeRecordData Find(LongChallengeMode mode, int dungeon, int difficulty) => records.FirstOrDefault(r => r.mode == mode && r.dungeonId == dungeon && r.difficulty == difficulty)?.Copy() ??
            new ChallengeRecordData { mode = mode, dungeonId = dungeon, difficulty = difficulty };
        public static bool ValidKey(LongChallengeMode mode, int dungeon, int difficulty) => Enum.IsDefined(typeof(LongChallengeMode), mode) && (dungeon == 1001 || dungeon == 1002) && difficulty >= 1 && difficulty <= 5;
        public static void Validate(ChallengeCareerData data)
        {
            if (data == null || data.revision < 0 || data.current == null || data.records == null || data.records.Length > 20 || data.records.Any(r => r == null) ||
                data.records.Select(r => r.Key).Distinct().Count() != data.records.Length) throw new InvalidDataException("长期挑战记录无效或重复。");
            foreach (var r in data.records)
            {
                if (!ValidKey(r.mode, r.dungeonId, r.difficulty) || r.highestStage < 1 || r.highestStage > LongChallengeRules.StageLimit(r.mode) ||
                    r.bestMilliseconds < r.highestStage || r.bestMilliseconds > (long)r.highestStage * LongChallengeRules.RoundMilliseconds ||
                    r.bestScore < LongChallengeRules.MinimumScore(r.highestStage, r.difficulty) || r.bestScore > LongChallengeRules.MaximumScore(r.highestStage, r.difficulty) ||
                    r.claimedMilestones != (r.mode == LongChallengeMode.Gauntlet ? r.highestStage / 5 : 0) ||
                    r.fullClears < 0 || r.fullClears > 1000000 || r.firstClearClaimed != (r.fullClears > 0) || r.firstClearClaimed != (r.highestStage == LongChallengeRules.StageLimit(r.mode)))
                    throw new InvalidDataException("长期挑战成绩或首次奖励标记不一致。");
            }
            var a = data.current;
            if (a.id == null || !Enum.IsDefined(typeof(ChallengeAttemptStatus), a.status)) throw new InvalidDataException("挑战尝试状态无效。");
            if (!a.HasValue)
            {
                if (data.revision != 0 || data.records.Length != 0 || a.sequence != 0 || (int)a.mode != 0 || a.dungeonId != 0 || a.difficulty != 0 || a.clearedStage != 0 ||
                    a.awardedGold != 0 || a.firstGold != 0 || a.score != 0 || a.combatMilliseconds != 0 || a.status != ChallengeAttemptStatus.None) throw new InvalidDataException("空挑战记录含有残留数据。");
                return;
            }
            if (!Guid.TryParseExact(a.id, "N", out _) || a.sequence < 1 || a.sequence != data.revision || !ValidKey(a.mode, a.dungeonId, a.difficulty) || a.clearedStage < 0 ||
                a.clearedStage > LongChallengeRules.StageLimit(a.mode) || a.awardedGold < 0 || a.awardedGold > 50000000 || a.firstGold < 0 || a.firstGold > a.awardedGold ||
                a.score < LongChallengeRules.MinimumScore(a.clearedStage, a.difficulty) || a.score > LongChallengeRules.MaximumScore(a.clearedStage, a.difficulty) ||
                a.combatMilliseconds < a.clearedStage || a.combatMilliseconds > (long)a.clearedStage * LongChallengeRules.RoundMilliseconds || a.status == ChallengeAttemptStatus.None ||
                (a.status == ChallengeAttemptStatus.Completed) != (a.clearedStage == LongChallengeRules.StageLimit(a.mode)) ||
                a.status == ChallengeAttemptStatus.Resting && a.clearedStage == 0 || a.clearedStage == 0 && (a.awardedGold != 0 || a.firstGold != 0) ||
                a.clearedStage > 0 && !data.records.Any(r => r.mode == a.mode && r.dungeonId == a.dungeonId && r.difficulty == a.difficulty && r.highestStage >= a.clearedStage))
                throw new InvalidDataException("挑战尝试、成绩或已提交奖励无效。");
        }
    }
    public readonly struct ChallengePayout
    {
        public readonly int Repeat, First;
        public int Total => Repeat + First;
        public ChallengePayout(int repeat, int first) { Repeat = repeat; First = first; }
    }
    public static class LongChallengeRules
    {
        public const int RoundMilliseconds = 600000;
        public static bool IsLong(int mode) => mode == 4 || mode == 5;
        public static int StageLimit(LongChallengeMode mode) => mode == LongChallengeMode.BossRush ? 6 : mode == LongChallengeMode.Gauntlet ? 1000 : 0;
        public static string Label(LongChallengeMode mode) => mode == LongChallengeMode.BossRush ? "首领连战" : "千层试炼";
        public static bool ValidStage(LongChallengeMode mode, int stage) => stage >= 1 && stage <= StageLimit(mode);
        public static double HealthScale(LongChallengeMode mode, int stage)
        {
            if (!ValidStage(mode, stage)) throw new ArgumentOutOfRangeException(nameof(stage));
            double n = stage - 1; return mode == LongChallengeMode.BossRush ? 1 + .18 * n : 1 + .12 * n + .002 * n * n;
        }
        public static double DamageScale(LongChallengeMode mode, int stage)
        {
            if (!ValidStage(mode, stage)) throw new ArgumentOutOfRangeException(nameof(stage));
            return 1 + (mode == LongChallengeMode.BossRush ? .12 : .08) * (stage - 1);
        }
        public static bool BossStage(LongChallengeMode mode, int stage) => ValidStage(mode, stage) && (mode == LongChallengeMode.BossRush || stage % 5 == 0);
        public static long StageScore(int stage, int difficulty, int milliseconds)
        {
            if (stage < 1 || stage > 1000 || difficulty < 1 || difficulty > 5 || milliseconds < 1 || milliseconds > RoundMilliseconds) throw new ArgumentOutOfRangeException();
            return (1000L * stage + (RoundMilliseconds - milliseconds) / 1000) * difficulty;
        }
        public static long MinimumScore(int stage, int difficulty) => 500L * stage * (stage + 1) * difficulty;
        public static long MaximumScore(int stage, int difficulty) => MinimumScore(stage, difficulty) + 599L * stage * difficulty;
        public static ChallengePayout Reward(LongChallengeMode mode, int stage, int difficulty, ChallengeRecordData record)
        {
            if (!ValidStage(mode, stage) || difficulty < 1 || difficulty > 5 || record == null) throw new ArgumentOutOfRangeException();
            int repeat = mode == LongChallengeMode.BossRush ? 25 * stage * difficulty : 15 * (1 + (stage - 1) / 5) * difficulty;
            int first = mode == LongChallengeMode.Gauntlet && stage % 5 == 0 && record.claimedMilestones < stage / 5 ? 100 * difficulty : 0;
            if (stage == StageLimit(mode)) { repeat += 100 * difficulty; if (!record.firstClearClaimed) first += 400 * difficulty; }
            return new ChallengePayout(repeat, first);
        }
        public static string Duration(long milliseconds) => $"{milliseconds / 3600000:00}:{milliseconds / 60000 % 60:00}:{milliseconds / 1000 % 60:00}.{milliseconds % 1000:000}";
        public static string RecordSummary(ChallengeRecordData r) => r.highestStage == 0 ? "本角色尚无此地图／难度成绩。" :
            $"最高 {r.highestStage} 轮 · 最佳 {r.bestScore:N0} 分\n到达第 {r.highestStage} 轮最快 {Duration(r.bestMilliseconds)} · 完整通关 {r.fullClears} 次";
        public static string Preview(LongChallengeMode mode, int difficulty, ChallengeRecordData record) =>
            (mode == LongChallengeMode.BossRush ? "6轮首领连战：两位现有首领交替。第n轮生命×(1+0.18(n−1))，攻击×(1+0.12(n−1))。\n每轮重复奖励 25 × 轮数 × 难度；通关另 +100 × 难度。首次通关另 +400 × 难度。" :
                "最高1000层，每5层挑战首领，其余层轮换近远程与精英。\n第n层生命倍率 1+0.12(n−1)+0.002(n−1)²，攻击 1+0.08(n−1)。\n每层重复奖励 15 × (1+整除((层数−1),5)) × 难度；每5层首次里程碑另 +100 × 难度。最终通关另 +100，首次另 +400，均乘难度。") +
            $"\n本次难度系数 {difficulty}，金币与经验同额；每轮10分钟，休整不计时。离场超5秒结束。\n清轮立即保存；休整可继续或结束，成功回复25%生命／能量。每轮刷新技能冷却并清除召唤／临时状态。\n失败、退出或读档保留已保存奖励及纪录；重开从第1轮开始，首次标记不重置。不掉普通战利品，不推进普通副本击杀／通关目标。\n{RecordSummary(record)}";
    }
}
