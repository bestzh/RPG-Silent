using UnityEngine;
public class IdleState : PlayerState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("进入 Idle 状态");
    }

    public override void Update()
    {
        if (player.InputDir != Vector2.zero)
        {
            player.StateMachine.ChangeState(new MoveState(player));
        }
    }
}
