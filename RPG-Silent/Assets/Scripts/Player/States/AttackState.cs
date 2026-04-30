using UnityEngine;

public class AttackState : PlayerState
{
    private const string AttackTrigger = "Attack";
    private const string ComboIndexParameter = "ComboIndex";
    private const string AttackTag = "Attack";

    private const float ComboInputStart = 0.35f;
    private const float ComboInputEnd = 0.8f;
    private const float AttackEndTime = 0.95f;
    private const float MinStateTime = 0.1f;

    private int comboIndex;
    private int maxCombo;
    private float stateTimer;
    private AttackExecutor attackExecutor;

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        comboIndex = 1;
        maxCombo = player.StanceController != null ? player.StanceController.MaxCombo : 1;
        stateTimer = 0f;
        attackExecutor = player.GetComponent<AttackExecutor>();

        PlayCurrentAttack();
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
        bool isAttackAnimation = stateInfo.IsTag(AttackTag);
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (Input.GetMouseButtonDown(0))
        {
            TryCombo(normalizedTime);
        }

        if (stateTimer < MinStateTime)
        {
            return;
        }

        if (isAttackAnimation && stateInfo.normalizedTime < AttackEndTime)
        {
            return;
        }

        if (!isAttackAnimation || stateInfo.normalizedTime >= AttackEndTime)
        {
            ChangeToLocomotionState();
        }
    }

    private void TryCombo(float normalizedTime)
    {
        if (comboIndex >= maxCombo)
        {
            return;
        }

        if (normalizedTime < ComboInputStart || normalizedTime > ComboInputEnd)
        {
            return;
        }

        comboIndex++;
        PlayCurrentAttack();
    }

    private void PlayCurrentAttack()
    {
        Debug.Log($"攻击 {comboIndex}");
        attackExecutor?.SetCurrentComboIndex(comboIndex);
        player.animator.SetInteger(ComboIndexParameter, comboIndex);
        player.animator.SetTrigger(AttackTrigger);
    }

    private void ChangeToLocomotionState()
    {
        if (player.InputDir.magnitude > 0.1f)
        {
            player.StateMachine.ChangeState(new MoveState(player));
            return;
        }

        player.StateMachine.ChangeState(new IdleState(player));
    }

    public override void Exit()
    {
        attackExecutor?.AttackEnd();
    }
}
