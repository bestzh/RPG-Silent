using UnityEngine;
public class IdleState : PlayerState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        player.animator.SetFloat("Horizontal", 0);
        player.animator.SetFloat("Vertical", 0);
        player.animator.SetFloat("MoveSpeed", 0);
    }

    public override void Update()
    {
        if (!player.IsGrounded)
        {
            player.StateMachine.ChangeState(new JumpState(player, startFalling: true));
            return;
        }

        if (player.InputDir.magnitude > 0.1f)
        {
            player.StateMachine.ChangeState(new MoveState(player));
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}

