using UnityEngine;
public class IdleState : PlayerState
{
    public IdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Update()
    {
        player.UpdateLocomotionAnimation();
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

    public override void FixedUpdate() => player.Motor.Move(Vector3.zero, Time.fixedDeltaTime);

    public override void Exit()
    {
        base.Exit();
    }
}

