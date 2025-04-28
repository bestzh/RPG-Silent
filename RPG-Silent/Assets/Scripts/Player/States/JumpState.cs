using UnityEngine;

public class JumpState : PlayerState
{
    private string jumpAnimationName;
    private bool hasAppliedJumpForce = false;
    private float jumpLockTimer = 0f; 
    private float minJumpDuration = 0.5f; // 起跳动作锁定时间

    public JumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 根据是否在跑步选择跳跃动画
        jumpAnimationName = player.IsRunning ? "JumpWhileRunning" : "JumpAll";

        if (player.GetComponent<PlayerAnimationController>().PlayAnimation(jumpAnimationName))
        {
            player.SetIsJumping(true);
            hasAppliedJumpForce = false;
            jumpLockTimer = 0f;
        }
        else
        {
            // 动画播不了，回Idle
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }

    public override void Update()
    {
        jumpLockTimer += Time.deltaTime;

        if (!hasAppliedJumpForce)
        {
            if (player.IsGrounded)
            {
                player.rb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
                hasAppliedJumpForce = true;
            }
        }

        // 注意这里：
        // 1. 必须跳跃过一段时间 2. 落地后 才能切换状态
        if (hasAppliedJumpForce && player.IsGrounded && jumpLockTimer >= minJumpDuration)
        {
            player.SetIsJumping(false);

            if (player.InputDir.magnitude > 0.1f)
            {
                player.StateMachine.ChangeState(new MoveState(player));
            }
            else
            {
                player.StateMachine.ChangeState(new IdleState(player));
            }
        }
    }

    public override void Exit()
    {
        player.SetIsJumping(false);
    }
}
