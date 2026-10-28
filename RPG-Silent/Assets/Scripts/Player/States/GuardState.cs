using UnityEngine;

public sealed class GuardState : PlayerState
{
    public GuardState(PlayerController player) : base(player) { }
    public override void Enter() => player.Defense.EnterGuard();
    public override void Update() => player.Defense.UpdateGuardAnimation();
    // Existing block clips are stationary; a slow defensive step avoids foot skating at run speed.
    public override void FixedUpdate() => player.Motor.Move(player.MovementVelocity(player.WalkSpeed * .3f), Time.fixedDeltaTime);
    public override void Exit() => player.Defense.ExitGuard();
}
