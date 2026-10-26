using UnityEngine;

/// <summary>
/// 兼容旧入口：技能逻辑已收编到 PlayerSkillController。
/// 受击打断等旧调用可继续走本组件。
/// </summary>
[DisallowMultipleComponent]
public class SkillCastManager : MonoBehaviour
{
    private PlayerSkillController _skills;

    private void Awake()
    {
        _skills = GetComponent<PlayerSkillController>();
    }

    public void InterruptSkill()
    {
        if (_skills == null)
            _skills = GetComponent<PlayerSkillController>();
        _skills?.InterruptSkill();
    }

    public bool TryCastSkill(string skillName)
    {
        Debug.LogWarning(
            "[SkillCastManager] TryCastSkill(string) 已废弃，请改用 PlayerSkillController.TryCastSlot。",
            this);
        return false;
    }
}
