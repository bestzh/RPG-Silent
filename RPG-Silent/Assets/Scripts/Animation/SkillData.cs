using UnityEngine;

public enum SkillTriggerTiming
{
    /// <summary>等动画事件（Hit / Shoot / Aoe / AttackRelease）再结算。</summary>
    OnAnimationEvent = 0,

    /// <summary>释放开始时立刻结算（动画只负责表现）。</summary>
    OnCastStart = 1,
    /// <summary>在配置的动作进度结算，不修改共享源动画的事件。</summary>
    AtAnimationTime = 2
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Skill/Create New Skill Data")]
public class SkillData : ScriptableObject
{
    [SerializeField, Tooltip("存档使用的稳定标识；发布后请勿修改。")]
    private string persistentId;
    public string PersistentId => persistentId;
#if UNITY_EDITOR
    private void OnValidate()
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(path)) return;
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
        if (persistentId == guid) return;
        persistentId = guid;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
    public string SkillName;
    [Header("导师与成长")]
    [Min(1)] public int RequiredLevel = 1;
    [Min(0)] public int LearnCost = 50;
    [Min(1)] public int UpgradeCost = 60;
    [Range(1, 5)] public int MaxRank = 5;
    [Min(1)] public int LevelsPerRank = 2;
    [Range(0f, 1f)] public float DamagePerRank = 0.2f;
    public int LevelForRank(int rank) => RequiredLevel + (rank - 1) * LevelsPerRank;
    public int CostForRank(int rank) => rank == 1 ? LearnCost : UpgradeCost * rank;

    [Header("UI")]
    public Sprite Icon;

    [Header("动画")]
    public AnimationData Animation;
    public float CastTime = 0f;

    [Header("冷却与消耗")]
    public float CooldownTime = 5f;
    public float EnergyCost = 20f;
    public bool CanBeInterrupted = true;

    [Header("执行")]
    [Tooltip("复用 AttackExecutor 的命中/伤害配置")]
    public AttackProfile AttackProfile;
    public CombatElement Element;
    public EnemyController SummonPrefab;
    [Min(1)] public float SummonDuration = 20f;
    public SkillTriggerTiming TriggerTiming = SkillTriggerTiming.OnAnimationEvent;
    [Range(.05f, .95f)] public float ReleaseNormalizedTime = .45f;

    [Header("技能特效")]
    public GameObject EffectPrefab;
    public Vector3 EffectOffset;

    [Header("命中特效（可选，覆盖 AttackProfile）")]
    public GameObject HitEffectPrefab;
    public Vector3 HitEffectOffset;

    [Header("连锁特效（后续扩展）")]
    public bool HasChainEffect = false;
    public int ChainCount = 3;
    public float ChainRadius = 5f;
    public GameObject ChainEffectPrefab;

    [Header("Combo配置（后续扩展）")]
    public bool CanCombo = false;
    public SkillData NextComboSkill;
    public float ComboInputWindow = 0.5f;
}
