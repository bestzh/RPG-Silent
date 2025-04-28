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
        if (player.InputDir == Vector2.zero)
        {
            player.StateMachine.ChangeState(new IdleState(player));
            return;
        }

        Vector2 input = player.InputDir;
        Vector3 moveDir = player.transform.forward * input.y + player.transform.right * input.x;
        player.rb.linearVelocity = new Vector3(moveDir.x * player.MoveSpeed, player.rb.linearVelocity.y, moveDir.z * player.MoveSpeed);

        // 设置动画参数（前进值、横向值）
        player.animator.SetFloat("Horizontal", input.x);
        player.animator.SetFloat("Vertical", input.y);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

