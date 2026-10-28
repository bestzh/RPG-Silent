using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    public enum CollectionKind { Enemy, Boss, Equipment, Secret, Dungeon, Elite, Chest, Crafted, Commission }
    [Serializable] public sealed class StoryChoiceData { public string storyId, choiceId; }
    [Serializable] public sealed class NpcAffinityData { public int role, value; }
    [Serializable] public sealed class CollectionRecord
    {
        public CollectionKind kind;
        public string id;
        public int count;
        public CollectionRecord Copy() => new() { kind = kind, id = id, count = count };
    }
    [Serializable] public sealed class WorldProgressData
    {
        public StoryChoiceData[] choices = Array.Empty<StoryChoiceData>();
        public NpcAffinityData[] affinities = Array.Empty<NpcAffinityData>();
        public CollectionRecord[] collection = Array.Empty<CollectionRecord>();
        public string[] claimedAchievements = Array.Empty<string>();
        public int completedRuns;
        public float townHour = 8;
        public int townDay = 1;
        public WorldProgressData Copy() => new()
        {
            choices = choices.Select(x => new StoryChoiceData { storyId = x.storyId, choiceId = x.choiceId }).ToArray(),
            affinities = affinities.Select(x => new NpcAffinityData { role = x.role, value = x.value }).ToArray(),
            collection = collection.Select(x => x.Copy()).ToArray(), claimedAchievements = (string[])claimedAchievements.Clone(),
            completedRuns = completedRuns, townHour = townHour, townDay = townDay
        };
        public string Choice(string story) => choices.FirstOrDefault(x => x.storyId == story)?.choiceId ?? "";
        public int Affinity(int role) => affinities.FirstOrDefault(x => x.role == role)?.value ?? 0;
        public int Count(CollectionKind kind, bool distinct = false) => distinct ? collection.Count(x => x.kind == kind) :
            (int)Math.Min(int.MaxValue, collection.Where(x => x.kind == kind).Sum(x => (long)x.count));
        public void Record(CollectionKind kind, string id, bool unique = false)
        {
            var entry = collection.FirstOrDefault(x => x.kind == kind && x.id == id);
            if (entry != null) { if (!unique) entry.count = Math.Min(1000000, entry.count + 1); return; }
            if (collection.Length >= 512) return;
            collection = collection.Append(new CollectionRecord { kind = kind, id = id, count = 1 }).ToArray();
        }
        public void AddAffinity(int role, int amount)
        {
            var entry = affinities.FirstOrDefault(x => x.role == role);
            if (entry == null) { entry = new NpcAffinityData { role = role }; affinities = affinities.Append(entry).ToArray(); }
            entry.value = Math.Clamp(entry.value + amount, 0, 100);
        }
        static bool Id(string id) => !string.IsNullOrEmpty(id) && id.Length <= 128;
        public static void Validate(WorldProgressData d)
        {
            if (d == null || d.choices == null || d.affinities == null || d.collection == null || d.claimedAchievements == null ||
                d.choices.Length > 64 || d.affinities.Length > 5 || d.collection.Length > 512 || d.claimedAchievements.Length > 128 ||
                d.completedRuns < 0 || d.completedRuns > 1000000 || float.IsNaN(d.townHour) || d.townHour < 0 || d.townHour >= 24 || d.townDay < 1 || d.townDay > 1000000)
                throw new InvalidDataException("冒险记录不完整或超出范围。");
            if (d.choices.Any(x => x == null || !Id(x.storyId) || !Id(x.choiceId)) || d.choices.Select(x => x.storyId).Distinct().Count() != d.choices.Length ||
                d.affinities.Any(x => x == null || x.role < 0 || x.role > 4 || x.value < 0 || x.value > 100) || d.affinities.Select(x => x.role).Distinct().Count() != d.affinities.Length ||
                d.collection.Any(x => x == null || !Enum.IsDefined(typeof(CollectionKind), x.kind) || !Id(x.id) || x.count < 1 || x.count > 1000000) ||
                d.collection.Select(x => x.kind + ":" + x.id).Distinct().Count() != d.collection.Length ||
                d.claimedAchievements.Any(x => !Id(x)) || d.claimedAchievements.Distinct().Count() != d.claimedAchievements.Length)
                throw new InvalidDataException("剧情、好感或图鉴记录无效或重复。");
        }
    }
}
