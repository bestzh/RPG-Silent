using UnityEngine;
public class MoveState : PlayerState
{
    public MoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("进入 Move 状态");
    }

    public override void Update()
    {
        if (!player.IsGrounded)
        {
            player.StateMachine.ChangeState(new JumpState(player, startFalling: true));
            return;
        }

        if (player.InputDir == Vector2.zero)
        {
            player.StateMachine.ChangeState(new IdleState(player));
            return;
        }

        Vector2 input = player.InputDir;
        Vector3 moveDir = player.transform.forward * input.y + player.transform.right * input.x;
        player.rb.linearVelocity = new Vector3(moveDir.x * player.CurrentMoveSpeed, player.rb.linearVelocity.y, moveDir.z * player.CurrentMoveSpeed);

        // 设置动画参数（前进值、横向值）
        Vector2 animationInput = player.IsSprinting && input.y > 0.5f
            ? new Vector2(0f, 2f)
            : player.IsWalking
            ? input * 0.5f
            : input;

        player.animator.SetFloat("Horizontal", animationInput.x);
        player.animator.SetFloat("Vertical", animationInput.y);
        player.animator.SetFloat("MoveSpeed", player.IsSprinting ? 1f : player.IsWalking ? 0.5f : 0f);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

