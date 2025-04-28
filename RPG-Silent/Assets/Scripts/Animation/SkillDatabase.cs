using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "ScriptableObjects/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    public static SkillDatabase Instance;
    public SkillData NormalAttackSkill;

    private void OnEnable()
    {
        Instance = this;
    }

    public SkillData GetNormalAttackSkill()
    {
        return NormalAttackSkill;
    }
}
