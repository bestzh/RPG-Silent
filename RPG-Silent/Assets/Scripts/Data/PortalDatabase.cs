using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 传送门总表：配置传送门 ID 与其可进入的副本 ID 列表。
/// 创建方式：右键 → Create → RPG → Portal Database
/// </summary>
[CreateAssetMenu(fileName = "PortalDatabase", menuName = "RPG/Portal Database")]
public class PortalDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("传送门唯一 ID，场景内 PortalTrigger 引用此 ID")]
        public int Id;

        [Tooltip("该传送门可选择的副本 ID 列表，对应 DungeonDatabase 中的副本 Id")]
        public List<int> DungeonIds = new List<int>();

        [Tooltip("关闭后该传送门不可用")]
        public bool IsEnabled = true;
    }

    [SerializeField] private DungeonDatabase dungeonDatabase;
    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<int, Entry> cacheById;

    public IReadOnlyList<Entry> Entries => entries;

    public Entry GetById(int portalId)
    {
        EnsureCache();
        return cacheById.TryGetValue(portalId, out Entry entry) ? entry : null;
    }

    public bool TryGetById(int portalId, out Entry entry)
    {
        EnsureCache();
        return cacheById.TryGetValue(portalId, out entry);
    }

    public IReadOnlyList<int> GetDungeonIds(int portalId)
    {
        Entry entry = GetById(portalId);
        if (entry == null || !entry.IsEnabled || entry.DungeonIds == null)
            return Array.Empty<int>();

        return entry.DungeonIds;
    }

    public IReadOnlyList<DungeonDatabase.Entry> GetDungeonsForPortal(int portalId)
    {
        var result = new List<DungeonDatabase.Entry>();
        if (dungeonDatabase == null) return result;

        Entry portal = GetById(portalId);
        if (portal == null || !portal.IsEnabled || portal.DungeonIds == null)
            return result;

        foreach (int dungeonId in portal.DungeonIds)
        {
            if (!dungeonDatabase.TryGetById(dungeonId, out DungeonDatabase.Entry dungeon))
                continue;

            if (!dungeon.IsEnabled || dungeon.GetEnabledDifficultyTiers().Count == 0)
                continue;

            result.Add(dungeon);
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

        var seenPortalIds = new HashSet<int>();
        foreach (Entry entry in entries)
        {
            if (entry == null) continue;

            if (!seenPortalIds.Add(entry.Id))
            {
                Debug.LogWarning(
                    $"[PortalDatabase] 检测到重复传送门 ID: {entry.Id}",
                    this);
            }

            ValidateDungeonIds(entry);

            if (entry.IsEnabled && (entry.DungeonIds == null || entry.DungeonIds.Count == 0))
            {
                Debug.LogWarning(
                    $"[PortalDatabase] 传送门 ID {entry.Id} 已启用但未配置副本 ID",
                    this);
            }
        }
    }

    private void ValidateDungeonIds(Entry entry)
    {
        if (entry.DungeonIds == null) return;

        var seenDungeonIds = new HashSet<int>();
        foreach (int dungeonId in entry.DungeonIds)
        {
            if (!seenDungeonIds.Add(dungeonId))
            {
                Debug.LogWarning(
                    $"[PortalDatabase] 传送门 ID {entry.Id} 存在重复副本 ID: {dungeonId}",
                    this);
            }

            if (dungeonDatabase != null && !dungeonDatabase.TryGetById(dungeonId, out _))
            {
                Debug.LogWarning(
                    $"[PortalDatabase] 传送门 ID {entry.Id} 引用了不存在的副本 ID: {dungeonId}",
                    this);
            }
        }
    }
}
