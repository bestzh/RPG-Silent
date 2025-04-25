using UnityEngine;
public class MoveState : PlayerState
{
    public MoveState(PlayerController player) : base(player) { }
    private float RotationSpeed = 200f; // 旋转速度，可以根据需要调整
    public override void Enter()
    {
        Debug.Log("进入 Move 状态");
    }

    public override void Update()
    {

         float mouseX = Input.GetAxis("Mouse X");
        player.transform.Rotate(Vector3.up, mouseX * RotationSpeed * Time.deltaTime);
        if (player.InputDir == Vector2.zero)
        {
            player.StateMachine.ChangeState(new IdleState(player));
            return;
        }
        // Vector3 dir = new Vector3(player.InputDir.x, 0, player.InputDir.y);
        // player.transform.position += dir * player.MoveSpeed * Time.deltaTime;
        Vector2 input = player.InputDir;
        Vector3 moveDir = player.transform.forward * input.y + player.transform.right * input.x;
        player.transform.position += moveDir.normalized * player.MoveSpeed * Time.deltaTime;

        // 设置动画参数（前进值、横向值）
        player.animator.SetFloat("Horizontal", player.InputDir.x);
        player.animator.SetFloat("Vertical", player.InputDir.y);
        // player.animator.SetBool("IsRunning", player.IsRunning);
    }
}
