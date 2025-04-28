using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Animation/Create New Animation Data")]
public class AnimationData : ScriptableObject
{
    public string AnimationName;      // 动作名称，比如 "Jump", "Attack1"
    public string AnimatorStateName;  // Animator里State的名字，比如 "JumpTree"
    public bool CanInterrupt;          // 动作中途能不能被打断
    public float Duration;             // 动作预计时间 (秒)

    [Header("连击专用")]
    public string NextComboName;       // 连招的下一个动作名（比如 Attack1 -> Attack2）

    [Header("技能专用")]
    public bool IsSkill = false;       // 是否是技能动作
    public float SkillCastTime = 0f;   // 技能释放时间
}
