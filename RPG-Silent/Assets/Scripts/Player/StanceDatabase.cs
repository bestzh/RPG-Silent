using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StanceDatabase", menuName = "Animation/Stance Database")]
public class StanceDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public PlayerStance Stance;

        [Tooltip("留空表示该姿态使用基础 AnimatorController（不替换 Clip）。\n比如 Relax 是基础 Controller 本身，无需 Override。")]
        public AnimatorOverrideController OverrideController;

        [Tooltip("可选，便于编辑器中快速识别")]
        public string DisplayName;

        [Header("能力配置")]
        [Tooltip("最大连击段数。1 表示只能单段攻击，0 表示完全不能攻击")]
        public int MaxComboCount = 1;

        [Tooltip("是否允许翻滚")]
        public bool CanRoll = true;

        [Tooltip("是否允许跳跃")]
        public bool CanJump = true;

        [Tooltip("最大跳跃次数（含起跳）。1=普通跳，2=可二段跳，0=本姿态禁跳。")]
        [Min(0)]
        public int MaxJumpCount = 1;

        [Tooltip("是否允许冲刺")]
        public bool CanSprint = true;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<PlayerStance, Entry> cache;

    public Entry GetEntry(PlayerStance stance)
    {
        if (cache == null)
        {
            BuildCache();
        }

        return cache.TryGetValue(stance, out var entry) ? entry : null;
    }

    public AnimatorOverrideController GetOverride(PlayerStance stance)
    {
        return GetEntry(stance)?.OverrideController;
    }

    public int GetMaxCombo(PlayerStance stance)
    {
        var entry = GetEntry(stance);
        return entry != null ? entry.MaxComboCount : 0;
    }

    public bool CanRoll(PlayerStance stance)
    {
        var entry = GetEntry(stance);
        return entry == null || entry.CanRoll;
    }

    public bool CanJump(PlayerStance stance)
    {
        var entry = GetEntry(stance);
        return entry == null || entry.CanJump;
    }

    public int GetMaxJumpCount(PlayerStance stance)
    {
        var entry = GetEntry(stance);
        if (entry == null)
        {
            return 1;
        }

        if (!entry.CanJump)
        {
            return 0;
        }

        return Mathf.Max(1, entry.MaxJumpCount);
    }

    public bool CanSprint(PlayerStance stance)
    {
        var entry = GetEntry(stance);
        return entry == null || entry.CanSprint;
    }

    // 按 Entries 在编辑器里配置的顺序循环切换，到末尾自动绕回开头
    public PlayerStance GetNextStance(PlayerStance current)
    {
        if (entries == null || entries.Count == 0)
        {
            return current;
        }

        int currentIndex = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].Stance == current)
            {
                currentIndex = i;
                break;
            }
        }

        // 当前姿态没在数据库里 → 直接跳到第一项
        if (currentIndex < 0)
        {
            return entries[0] != null ? entries[0].Stance : current;
        }

        for (int step = 1; step <= entries.Count; step++)
        {
            int nextIndex = (currentIndex + step) % entries.Count;
            if (entries[nextIndex] != null)
            {
                return entries[nextIndex].Stance;
            }
        }

        return current;
    }

    private void BuildCache()
    {
        cache = new Dictionary<PlayerStance, Entry>();
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            cache[entry.Stance] = entry;
        }
    }

    private void OnValidate()
    {
        cache = null;
    }
}
