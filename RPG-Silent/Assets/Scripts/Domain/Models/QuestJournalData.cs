using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class QuestProgressData
    {
        public string questId;
        public int[] counts = Array.Empty<int>();
        public bool rewarded;
        public bool abandoned;
        public int completedCount;
        public int lastTurnInRun;
        public QuestProgressData Copy() => new() { questId = questId, counts = (int[])counts.Clone(), rewarded = rewarded, abandoned = abandoned,
            completedCount = completedCount, lastTurnInRun = lastTurnInRun };
    }

    [Serializable]
    public sealed class QuestJournalData
    {
        public QuestProgressData[] entries = Array.Empty<QuestProgressData>();
        public string trackedId = string.Empty;
        public QuestJournalData Copy() => new() { entries = entries.Select(x => x.Copy()).ToArray(), trackedId = trackedId };
        public static void Validate(QuestJournalData data)
        {
            if (data == null || data.entries == null || data.entries.Length > 128 || data.trackedId == null || data.trackedId.Length > 128)
                throw new InvalidDataException("任务记录不完整。");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in data.entries)
                if (e == null || string.IsNullOrEmpty(e.questId) || e.questId.Length > 128 || !ids.Add(e.questId) ||
                    e.completedCount < 0 || e.completedCount > 1000000 || e.lastTurnInRun < 0 || e.lastTurnInRun > 1000000 ||
                    e.counts == null || e.counts.Length == 0 || e.counts.Length > 8 || e.counts.Any(x => x < 0 || x > 9999) ||
                    e.abandoned && (e.rewarded || e.counts.Any(x => x != 0)))
                    throw new InvalidDataException("任务进度无效或重复。");
            if (data.trackedId.Length > 0 && !data.entries.Any(x => x.questId == data.trackedId && !x.rewarded && !x.abandoned))
                throw new InvalidDataException("追踪的任务尚未接取或已经提交。");
        }
    }
}
