using UnityEngine;

/// <summary>Hold the existing attack action; release snapshots one attack, cancellation never fires it.</summary>
public sealed class ChargeAttackState : PlayerState
{
    float heldTime;
    readonly PlayerStance stance;
    public float Ratio => WeaponCombatRules.ChargeRatio(stance, heldTime);
    public ChargeAttackState(PlayerController player) : base(player) { stance = player.CurrentStance; }
    public override void Enter()
    {
        player.AnimationController?.CancelAnimation();
        player.animator.CrossFadeInFixedTime(stance == PlayerStance.TwoHandBow ? "ChargeBow" : "ChargeHeavy", .06f, 0,
            stance == PlayerStance.TwoHandSword ? .18f : 0);
    }
    public override void Update()
    {
        if (!player.CanRequestCombatAction || Cursor.lockState != CursorLockMode.Locked || player.CurrentStance != stance)
        { player.CancelHeldAttack(); player.ReturnToLocomotion(); return; }
        heldTime += Time.deltaTime;
        if (!player.AttackHeld) Release();
    }
    public void Release()
    {
        if (!player.CanRequestCombatAction || Cursor.lockState != CursorLockMode.Locked)
        { player.CancelHeldAttack(); player.ReturnToLocomotion(); return; }
        player.StateMachine.ChangeState(new AttackState(player, Ratio));
    }
    public override void FixedUpdate() => player.Motor.Move(player.MovementVelocity(player.WalkSpeed * .35f), Time.fixedDeltaTime);
}
