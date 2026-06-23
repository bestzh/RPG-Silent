using System;
using System.Collections.Generic;
using UnityEngine;

public enum DungeonDifficulty
{
    Easy      = 1,
    Normal    = 2,
    Hard      = 3,
    Nightmare = 4,
    Hell      = 5
}

/// <summary>
/// 副本总表：在单个 ScriptableObject 中配置所有副本数据。
/// 全局共五档难度（简单/普通/困难/噩梦/地狱），每个副本可自由配置开放其中任意几档，共用同一场景。
/// 创建方式：右键 → Create → RPG → Dungeon Database
/// </summary>
[CreateAssetMenu(fileName = "DungeonDatabase", menuName = "RPG/Dungeon Database")]
public class DungeonDatabase : ScriptableObject
{
    public static readonly DungeonDifficulty[] AllDifficulties =
    {
        DungeonDifficulty.Easy,
        DungeonDifficulty.Normal,
        DungeonDifficulty.Hard,
        DungeonDifficulty.Nightmare,
        DungeonDifficulty.Hell
    };

    [Serializable]
    public class DifficultyTier
    {
        [Tooltip("难度档位")]
        public DungeonDifficulty Difficulty = DungeonDifficulty.Normal;

        [Tooltip("该难度下的建议等级")]
        [Min(1)]
        public int RecommendedLevel = 1;

        [Tooltip("该难度下的最低进入等级，0 表示无限制")]
        [Min(0)]
        public int MinLevel;

        [Tooltip("关闭后该难度档位不可选")]
        public bool IsEnabled = true;

        public string DifficultyLabel => GetDifficultyLabel(Difficulty);
    }

    [Serializable]
    public class Entry
    {
        [Header("基础信息")]
        [Tooltip("副本唯一 ID，供传送门等系统引用")]
        public int Id;

        [Tooltip("副本显示名称")]
        public string DisplayName;

        [TextArea(3, 8)]
        [Tooltip("副本介绍，显示在详情面板")]
        public string Description;

        [Header("展示资源")]
        [Tooltip("列表项小图标")]
        public Sprite Icon;

        [Tooltip("详情页背景图")]
        public Sprite BackgroundImage;

        [Header("场景")]
        [Tooltip("五档难度共用同一场景，难度差异由副本内逻辑处理")]
        public string SceneKey;

        [Header("难度档位")]
        [Tooltip("按需添加难度档位，不必凑齐五档。未添加的档位对该副本不可用。")]
        public List<DifficultyTier> DifficultyTiers = new List<DifficultyTier>();

        [Header("状态")]
        [Tooltip("关闭后整个副本不会出现在传送门列表中")]
        public bool IsEnabled = true;

        public bool HasDifficulty(DungeonDifficulty difficulty) =>
            GetDifficultyTier(difficulty) != null;

        public DifficultyTier GetDifficultyTier(DungeonDifficulty difficulty)
        {
            foreach (DifficultyTier tier in DifficultyTiers)
            {
                if (tier != null && tier.Difficulty == difficulty && tier.IsEnabled)
                    return tier;
            }

            return null;
        }

        public IReadOnlyList<DifficultyTier> GetEnabledDifficultyTiers()
        {
            var result = new List<DifficultyTier>();
            foreach (DifficultyTier tier in DifficultyTiers)
            {
                if (tier != null && tier.IsEnabled)
                    result.Add(tier);
            }

            result.Sort((a, b) => a.Difficulty.CompareTo(b.Difficulty));
            return result;
        }

        /// <summary>列表展示用：所有已启用档位中最低的建议等级。</summary>
        public int GetLowestRecommendedLevel()
        {
            int lowest = int.MaxValue;
            bool found = false;

            foreach (DifficultyTier tier in DifficultyTiers)
            {
                if (tier == null || !tier.IsEnabled) continue;

                found = true;
                if (tier.RecommendedLevel < lowest)
                    lowest = tier.RecommendedLevel;
            }

            return found ? lowest : 1;
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<int, Entry> cacheById;

    public IReadOnlyList<Entry> Entries => entries;

    public static string GetDifficultyLabel(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Easy      => "简单",
        DungeonDifficulty.Normal    => "普通",
        DungeonDifficulty.Hard      => "困难",
        DungeonDifficulty.Nightmare => "噩梦",
        DungeonDifficulty.Hell      => "地狱",
        _                           => difficulty.ToString()
    };

    public Entry GetById(int id)
    {
        EnsureCache();
        return cacheById.TryGetValue(id, out Entry entry) ? entry : null;
    }

    public bool TryGetById(int id, out Entry entry)
    {
        EnsureCache();
        return cacheById.TryGetValue(id, out entry);
    }

    public IReadOnlyList<Entry> GetEnabledEntries()
    {
        var result = new List<Entry>();
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.IsEnabled && entry.GetEnabledDifficultyTiers().Count > 0)
                result.Add(entry);
        }

        return result;
    }

    public IReadOnlyList<Entry> GetEntriesByIds(IEnumerable<int> ids)
    {
        var result = new List<Entry>();
        if (ids == null) return result;

        EnsureCache();
        foreach (int id in ids)
        {
            if (!cacheById.TryGetValue(id, out Entry entry)) continue;
            if (!entry.IsEnabled || entry.GetEnabledDifficultyTiers().Count == 0) continue;

            result.Add(entry);
        }

        return result;
    }

    private void EnsureCache()
    {
        if (cacheById != null) return;
        BuildCache();
    }

    private void BuildCache()
    {
        cacheById = new Dictionary<int, Entry>();
        foreach (Entry entry in entries)
        {
            if (entry == null) continue;
            cacheById[entry.Id] = entry;
        }
    }

    private void OnValidate()
    {
        cacheById = null;

        var seenIds = new HashSet<int>();
        foreach (Entry entry in entries)
        {
            if (entry == null) continue;

            SortDifficultyTiers(entry);

            if (!seenIds.Add(entry.Id))
            {
                Debug.LogWarning(
                    $"[DungeonDatabase] 检测到重复副本 ID: {entry.Id}（{entry.DisplayName}）",
                    this);
            }

            ValidateDuplicateDifficulties(entry);

            if (entry.IsEnabled && entry.GetEnabledDifficultyTiers().Count == 0)
            {
                Debug.LogWarning(
                    $"[DungeonDatabase] 副本「{entry.DisplayName}」已启用但未配置任何难度档位",
                    this);
            }
        }
    }

    private static void SortDifficultyTiers(Entry entry)
    {
        if (entry.DifficultyTiers == null) return;
        entry.DifficultyTiers.Sort((a, b) => a.Difficulty.CompareTo(b.Difficulty));
    }

    private static void ValidateDuplicateDifficulties(Entry entry)
    {
        if (entry.DifficultyTiers == null) return;

        var seenDifficulties = new HashSet<DungeonDifficulty>();
        foreach (DifficultyTier tier in entry.DifficultyTiers)
        {
            if (tier == null) continue;

            if (!seenDifficulties.Add(tier.Difficulty))
            {
                Debug.LogWarning(
                    $"[DungeonDatabase] 副本「{entry.DisplayName}」存在重复难度档位: {tier.DifficultyLabel}");
            }
        }
    }
}
