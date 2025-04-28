using UnityEngine;

public class RollState : PlayerState
{
    private readonly string RollAnimationName = "Roll"; // 这里是你 AnimationData 里配置的名字

    public RollState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        if (player.GetComponent<PlayerAnimationController>().PlayAnimation(RollAnimationName))
        {
            player.SetIsRolling(true);

            // 给一个向前冲的力
            Vector3 rollDirection = player.transform.forward;
            player.rb.linearVelocity = rollDirection * player.RollForce;
        }
        else
        {
            // 如果播放失败（可能在别的不可中断动画中），强制回到Idle
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }

    public override void Update()
    {
        // 等动作播放结束后自动切回Idle
        if (!player.GetComponent<PlayerAnimationController>().IsPlayingAnimation())
        {
            player.SetIsRolling(false);
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }

    public override void Exit()
    {
        player.SetIsRolling(false);
    }
}
