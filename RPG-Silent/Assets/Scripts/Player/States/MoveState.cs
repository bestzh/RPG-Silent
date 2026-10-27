using UnityEngine;
public class MoveState : PlayerState
{
    public MoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
    }

    public override void Update()
    {
        player.UpdateLocomotionAnimation();
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

    }

    public override void FixedUpdate() => player.Motor.Move(
        player.MovementVelocity(player.CurrentMoveSpeed), Time.fixedDeltaTime);

    public override void Exit()
    {
        base.Exit();
    }
}

