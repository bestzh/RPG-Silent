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
    private AttackProfile currentProfile;
    private bool attackStarted;
    private bool attackReleased;
    private bool attackEnded;

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

        UpdateAttackTiming(normalizedTime);

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
        currentProfile = attackExecutor != null ? attackExecutor.GetCurrentAttackProfile() : null;
        attackStarted = false;
        attackReleased = false;
        attackEnded = false;
        player.animator.SetInteger(ComboIndexParameter, comboIndex);
        player.animator.SetTrigger(AttackTrigger);
    }

    private void UpdateAttackTiming(float normalizedTime)
    {
        if (currentProfile == null)
        {
            return;
        }

        if (!attackStarted && normalizedTime >= currentProfile.StartTime)
        {
            attackStarted = true;
            attackExecutor?.AttackStart();
        }

        if (!attackReleased && normalizedTime >= currentProfile.ReleaseTime)
        {
            attackReleased = true;
            attackExecutor?.AttackRelease();
        }

        if (!attackEnded && normalizedTime >= currentProfile.EndTime)
        {
            attackEnded = true;
            attackExecutor?.AttackEnd();
        }
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
