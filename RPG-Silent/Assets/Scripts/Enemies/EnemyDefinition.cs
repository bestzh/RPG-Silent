using UnityEngine;

public enum EnemyBehaviorStyle { Melee, Ranged, Boss }

[CreateAssetMenu(menuName = "RPG/Enemies/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject
{
    public string DisplayName;
    public EnemyBehaviorStyle Behavior;
    [Min(1)] public int Health = 60;
    [Min(0.1f)] public float MoveSpeed = 3f;
    public float DetectionDistance = 16f;
    public float LeashDistance = 35f;
    public float PreferredDistance = 2f;
    public float RetreatDistance = 0f;
    [Range(1f, 90f)] public float AttackFacingAngle = 25f;
    [Min(0f)] public float ApproachHysteresis = 0.5f;
    [Min(1f)] public float RecoveryDamageMultiplier = 1f;
    public int RewardGold = 10;
    public int RewardExp = 25;
    [Tooltip("累计伤害达到该值才硬直；0 表示每次受伤都可硬直。")]
    public int StaggerThreshold;
    public EnemySkillData[] Skills;
    public EnemyAttackSequence[] AttackSequences;
    public GameObject HitEffect;
    public GameObject DeathEffect;
}
