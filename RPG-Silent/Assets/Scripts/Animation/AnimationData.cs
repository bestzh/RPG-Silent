using UnityEngine;

[CreateAssetMenu(fileName = "AnimationData", menuName = "Animation/Create New Animation Data")]
public class AnimationData : ScriptableObject
{
    public string AnimationName;
    public string AnimatorStateName;
    public bool CanInterrupt;
    public float Duration;

    [Header("Skill")]
    public bool IsSkill = false;
    public float SkillCastTime = 0f;
}
