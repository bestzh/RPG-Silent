using UnityEngine;

public partial class PlayerController
{
    readonly CombatActionBuffer combatInput = new();
    bool attackHeld;
    bool autoQueued;
    float dodgeFollowupUntil;
    public bool AttackHeld => attackHeld;
    public float ChargeRatio => StateMachine.CurrentState is ChargeAttackState charge ? charge.Ratio : 0;
    public bool IsCharging => StateMachine.CurrentState is ChargeAttackState;
    public bool DodgeFollowupReady => CombatAllowed && Time.time < dodgeFollowupUntil && CurrentStance == PlayerStance.OneHandSword;
    public void CancelHeldAttack() { attackHeld = false; dodgeFollowupUntil = 0; }
    public void CompleteDodge() { if (CombatAllowed) dodgeFollowupUntil = Time.time + .65f; }
    public bool ConsumeDodgeFollowup() { bool ready = DodgeFollowupReady; dodgeFollowupUntil = 0; return ready; }
    public int BufferedCombatAction => combatInput.Peek(Time.time);
    public bool CanRequestCombatAction => isActiveAndEnabled && GameplayInputEnabled && CombatAllowed && !combatLocked
        && !IsDead && IsGrounded && !IsRolling && !IsJumping && !(Weapons != null && Weapons.IsBusy)
        && !(StateMachine.CurrentState is HurtState);

    public bool RequestBasicAttack()
    {
        if (!CanRequestCombatAction || StanceController != null && StanceController.MaxCombo <= 0) return false;
        if (IsCharging) return false;
        Defense?.SuppressGuard();
        autoQueued = false;
        combatInput.Request(CombatActionBuffer.BasicAttack, Time.time);
        return true;
    }
    public bool RequestSkill(int slot)
    {
        if (!CanRequestCombatAction || skillController == null || !skillController.CanQueueSlot(slot)) return false;
        CancelCharge();
        Defense?.SuppressGuard();
        combatInput.Request(slot, Time.time);
        return true;
    }
    public void ClearCombatInput() { combatInput.Clear(); autoQueued = false; }
    public void CancelCharge()
    {
        CancelHeldAttack();
        if (IsCharging) ReturnToLocomotion();
    }

    void PressAttack()
    {
        if (!CanRequestCombatAction || Cursor.lockState != CursorLockMode.Locked) return;
        attackHeld = true;
        RequestBasicAttack();
    }
    void ReleaseAttack(bool fire)
    {
        attackHeld = false;
        if (autoQueued) ClearCombatInput();
        if (!fire) { ClearCombatInput(); CancelCharge(); return; }
        // Resolve on the state update, after all input callbacks. A same-frame guard/skill
        // must be able to cancel before a button release commits the attack.
    }

    void ConsumeCombatInput()
    {
        if (!CanRequestCombatAction || Cursor.lockState != CursorLockMode.Locked)
        { ClearCombatInput(); CancelHeldAttack(); return; }
        if (IsCharging) return;
        // Staff sustains its three-bolt rhythm while held; explicit skills have priority.
        if (attackHeld && CurrentStance == PlayerStance.TwoHandStaff && combatInput.Peek(Time.time) == CombatActionBuffer.Empty)
        { combatInput.Request(CombatActionBuffer.BasicAttack, Time.time); autoQueued = true; }
        int action = combatInput.Peek(Time.time);
        if (action == CombatActionBuffer.Empty) return;
        if (action >= 0)
        {
            if (skillController.TryCastSlot(action)) combatInput.Clear();
            return;
        }
        if (skillController != null && !skillController.TryFinishForLink()) return;
        if (!CanAttack) return;
        if (StateMachine.CurrentState is AttackState attack)
        {
            if (!attack.TryContinue()) return;
        }
        else if (attackHeld && WeaponCombatRules.CanCharge(CurrentStance)) StateMachine.ChangeState(new ChargeAttackState(this));
        else StateMachine.ChangeState(new AttackState(this));
        ClearCombatInput();
    }
}
