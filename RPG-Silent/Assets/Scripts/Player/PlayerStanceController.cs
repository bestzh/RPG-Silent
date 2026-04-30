using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StanceAnimatorEntry
{
    public PlayerStance Stance;
    public AnimatorOverrideController OverrideController;
}

[RequireComponent(typeof(Animator))]
public class PlayerStanceController : MonoBehaviour
{
    [Header("默认姿态")]
    [SerializeField] private PlayerStance defaultStance = PlayerStance.Relax;

    [Header("姿态数据库（推荐，大量姿态时使用）")]
    [SerializeField] private StanceDatabase stanceDatabase;

    [Header("额外覆盖 / 临时姿态（少量时直接在这里配也行）")]
    [SerializeField] private List<StanceAnimatorEntry> extraOverrides = new List<StanceAnimatorEntry>();

    private Animator animator;
    private RuntimeAnimatorController baseController;
    private readonly Dictionary<PlayerStance, AnimatorOverrideController> overrideMap = new Dictionary<PlayerStance, AnimatorOverrideController>();

    public PlayerStance CurrentStance { get; private set; }

    public event Action<PlayerStance, PlayerStance> OnStanceChanged;

    public int MaxCombo => stanceDatabase != null ? stanceDatabase.GetMaxCombo(CurrentStance) : 1;
    public bool CanRoll => stanceDatabase == null || stanceDatabase.CanRoll(CurrentStance);
    public bool CanJump => stanceDatabase == null || stanceDatabase.CanJump(CurrentStance);
    public int MaxJumpCount => stanceDatabase != null ? stanceDatabase.GetMaxJumpCount(CurrentStance) : 1;
    public bool CanSprint => stanceDatabase == null || stanceDatabase.CanSprint(CurrentStance);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        baseController = animator.runtimeAnimatorController;

        foreach (var entry in extraOverrides)
        {
            if (entry == null || entry.OverrideController == null)
            {
                continue;
            }

            overrideMap[entry.Stance] = entry.OverrideController;
        }

        SetStance(defaultStance, force: true);
    }

    public void SetStance(PlayerStance stance, bool force = false)
    {
        if (!force && stance == CurrentStance)
        {
            return;
        }

        PlayerStance previous = CurrentStance;
        CurrentStance = stance;

        bool isConfigured;
        AnimatorOverrideController overrideController = ResolveOverride(stance, out isConfigured);

        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            // OverrideController 为空 = 该姿态使用基础 Controller（合法，比如 Relax）
            animator.runtimeAnimatorController = baseController;

            if (!isConfigured)
            {
                Debug.LogWarning($"姿态 {stance} 在 StanceDatabase 中没有任何 Entry，已退回基础 Animator。");
            }
        }

        OnStanceChanged?.Invoke(previous, stance);
    }

    // 切到下一个姿态：
    // 1. 配了 StanceDatabase 就按数据库里 Entries 的顺序循环（编辑器拖拽即可改顺序）
    // 2. 没配数据库则回落到 Relax → Unarmed → Armed → Relax 的硬编码循环
    public void ToggleStance()
    {
        PlayerStance next;

        if (stanceDatabase != null)
        {
            next = stanceDatabase.GetNextStance(CurrentStance);
        }
        else
        {
            next = CurrentStance switch
            {
                PlayerStance.Relax => PlayerStance.Unarmed,
                PlayerStance.Unarmed => PlayerStance.Armed,
                PlayerStance.Armed => PlayerStance.Relax,
                _ => PlayerStance.Relax
            };
        }

        SetStance(next);
    }

    private AnimatorOverrideController ResolveOverride(PlayerStance stance, out bool isConfigured)
    {
        if (overrideMap.TryGetValue(stance, out var controller))
        {
            isConfigured = true;
            return controller;
        }

        if (stanceDatabase != null)
        {
            var entry = stanceDatabase.GetEntry(stance);
            if (entry != null)
            {
                isConfigured = true;
                return entry.OverrideController;
            }
        }

        isConfigured = false;
        return null;
    }
}
