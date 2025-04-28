using UnityEngine;
public abstract class PlayerState
{
    protected PlayerController player;

    protected PlayerState(PlayerController player)
    {
        this.player = player;
    }

    // 进入状态时调用
    public virtual void Enter() { }

    // 每帧更新时调用
    public virtual void Update() { }

    // 固定帧更新（如果有物理逻辑）
    public virtual void FixedUpdate() { }

    // 离开状态时调用
    public virtual void Exit() { }

    // 检查动画是否结束
    protected bool IsAnimationFinished(string animationName)
    {
        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1f;
    }

    // 检查动画 Tag 是否匹配
    protected bool IsAnimationTag(string tag)
    {
        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsTag(tag);
    }
}
