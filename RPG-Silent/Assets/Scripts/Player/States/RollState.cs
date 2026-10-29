using UnityEngine;

public class RollState : PlayerState
{
    private const string RollAnimationName = "RollTree";
    private Vector3 velocity;

    public RollState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Vector2 input = player.InputDir.sqrMagnitude > 0.01f ? player.InputDir.normalized : Vector2.up;
        bool dive = player.CanDiveRoll && input.y > 0.9f;
        player.animator.SetFloat("RollX", dive ? 0f : input.x);
        player.animator.SetFloat("RollY", dive ? 2f : input.y);
        player.animator.SetBool("IsRolling", true);
        // Imported clips last 0.8 s for a roll and 1.2 s for a forward dive.
        if (!player.AnimationController.PlayAnimation(RollAnimationName, dive ? 1.2f : 0.8f))
        {
            player.ReturnToLocomotion();
            return;
        }
        if (player.Defense != null && !player.Defense.BeginRoll()) { player.ReturnToLocomotion(); return; }
        player.SetIsRolling(true);
        player.Progress?.RecordTutorialAction(RPGSilent.Domain.TutorialStep.Dodge);
        velocity = player.FacingRotation * new Vector3(input.x, 0f, input.y) * player.RollForce;
    }

    public override void Update()
    {
        if (!player.AnimationController.IsPlayingAnimation()) { player.CompleteDodge(); player.ReturnToLocomotion(); }
    }

    public override void FixedUpdate() => player.Motor.SetHorizontalVelocity(
        player.GameplayInputEnabled ? velocity : Vector3.zero);

    public override void Exit()
    {
        player.Defense?.EndRoll();
        player.SetIsRolling(false);
        player.animator.SetBool("IsRolling", false);
        if (player.AnimationController.CurrentAnimationName() == RollAnimationName)
            player.AnimationController.CancelAnimation();
        player.Motor.SetHorizontalVelocity(Vector3.zero);
    }
}
