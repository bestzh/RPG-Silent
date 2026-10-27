using UnityEngine;

/// <summary>Full-body equipment actions own locomotion until completion or interruption.</summary>
public sealed class WeaponTransitionState : PlayerState
{
    public WeaponTransitionState(PlayerController player) : base(player) { }

    public override void Update()
    {
        if (!player.Weapons.IsTransitioning)
        {
            player.ReturnToLocomotion();
            return;
        }
        player.UpdateLocomotionAnimation();
        if (!player.IsGrounded)
        {
            player.StateMachine.ChangeState(new JumpState(player, startFalling: true));
            return;
        }
        player.Weapons.TickTransition();
    }

    public override void FixedUpdate() => player.Motor.Move(Vector3.zero, Time.fixedDeltaTime);
    public override void Exit() => player.Weapons.CancelTransition();
}
