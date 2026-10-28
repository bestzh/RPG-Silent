using UnityEngine;

public partial class PlayerController
{
    readonly CombatActionBuffer combatInput = new();
    public int BufferedCombatAction => combatInput.Peek(Time.time);
    public bool CanRequestCombatAction => isActiveAndEnabled && GameplayInputEnabled && CombatAllowed && !combatLocked
        && !IsDead && IsGrounded && !IsRolling && !IsJumping && !(Weapons != null && Weapons.IsBusy)
        && !(StateMachine.CurrentState is HurtState);

    public bool RequestBasicAttack()
    {
        if (!CanRequestCombatAction || StanceController != null && StanceController.MaxCombo <= 0) return false;
        combatInput.Request(CombatActionBuffer.BasicAttack, Time.time);
        return true;
    }
    public bool RequestSkill(int slot)
    {
        if (!CanRequestCombatAction || skillController == null || !skillController.CanQueueSlot(slot)) return false;
        combatInput.Request(slot, Time.time);
        return true;
    }
    public void ClearCombatInput() => combatInput.Clear();

    void ConsumeCombatInput()
    {
        if (!CanRequestCombatAction || Cursor.lockState != CursorLockMode.Locked) { ClearCombatInput(); return; }
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
        else StateMachine.ChangeState(new AttackState(this));
        combatInput.Clear();
    }
}
