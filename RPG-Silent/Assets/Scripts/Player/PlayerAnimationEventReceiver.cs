using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    private PlayerSkillController  skillController;

    private void Awake()
    {
        skillController = GetComponent<PlayerSkillController>();
        if (skillController == null)
            skillController = GetComponentInParent<PlayerSkillController>();
    }

    public void FootL()
    {
        // Left footstep animation event.
    }

    public void FootR()
    {
        // Right footstep animation event.
    }

    public void Land()
    {
        // Landing animation event.
    }

    public void AttackStart()
    {
        // 普攻由 AttackState 的 Profile 时间线驱动；技能在释放时打开自己的 hitbox。
    }

    public void AttackRelease(AnimationEvent animationEvent = null)
    {
        skillController?.NotifySkillHitEvent(animationEvent);
    }

    public void AttackEnd(AnimationEvent animationEvent = null)
    {
        skillController?.NotifySkillAttackEnd(animationEvent);
    }

    public void Hit(AnimationEvent animationEvent = null)
    {
        AttackRelease(animationEvent);
    }

    public void Shoot(AnimationEvent animationEvent = null)
    {
        AttackRelease(animationEvent);
    }

    public void Aoe(AnimationEvent animationEvent = null)
    {
        AttackRelease(animationEvent);
    }

    public void WeaponSwitch()
    {
        // Weapon switch animation event.
    }
}
