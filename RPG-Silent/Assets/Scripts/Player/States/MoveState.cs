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
        player.transform.position += moveDir.normalized * player.MoveSpeed * Time.deltaTime;

        // 设置动画参数（前进值、横向值）
        player.animator.SetFloat("Horizontal", player.InputDir.x);
        player.animator.SetFloat("Vertical", player.InputDir.y);
        // player.animator.SetBool("IsRunning", player.IsRunning);
    }
}
