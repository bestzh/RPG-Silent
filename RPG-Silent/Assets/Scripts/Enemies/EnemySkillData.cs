using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Enemies/Enemy Skill")]
public sealed class EnemySkillData : ScriptableObject
{
    public string DisplayName;
    public AttackProfile Attack;
    public string AnimationState = "Attack";
    [Min(0.1f)] public float Duration = 1.6f;
    [Range(0f, 1f)] public float ReleaseTime = 0.45f;
    [Min(0f)] public float Cooldown = 3f;
    public float MinRange;
    public float MaxRange = 2.5f;
    [Min(1)] public int RequiredPhase = 1;
    [Min(0.01f)] public float Weight = 1f;
    public bool SuperArmor;
    public bool AimAtTargetGround;
    public GameObject WindupEffect;
    public GameObject ReleaseEffect;
    public Material TelegraphMaterial;
    public Color TelegraphColor = new Color(1f, 0.25f, 0.08f, 0.9f);
}
