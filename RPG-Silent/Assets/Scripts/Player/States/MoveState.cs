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
        }

        Vector3 dir = new Vector3(player.InputDir.x, 0, player.InputDir.y);
        player.transform.position += dir * player.MoveSpeed * Time.deltaTime;
    }
}
