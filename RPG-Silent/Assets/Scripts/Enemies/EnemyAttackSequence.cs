using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Enemies/Attack Sequence")]
public sealed class EnemyAttackSequence : ScriptableObject
{
    public string DisplayName;
    [Min(1)] public int RequiredPhase = 2;
    [Min(0f)] public float Cooldown = 9f;
    [Min(0.1f)] public float BetweenAttacks = 0.3f;
    [Min(0.1f)] public float RecoveryDuration = 1.8f;
    [Tooltip("顺序执行。每段重新检查射程与视线；离开攻击范围会提前结束连招。")]
    public EnemySkillData[] Steps;
}
