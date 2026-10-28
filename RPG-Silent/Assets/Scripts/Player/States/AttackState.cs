using UnityEngine;

public class AttackState : PlayerState
{
    private const string AttackTrigger = "Attack";
    private const string ComboIndexParameter = "ComboIndex";
    private const string AttackTag = "Attack";

    private int comboIndex;
    private int maxCombo;
    private int expectedAnimationHash;
    private bool attackAnimationSeen;
    private AttackExecutor attackExecutor;
    private AttackProfile currentProfile;
    private bool attackStarted;
    private bool attackReleased;
    private bool attackEnded;
    private float progress;
    private float enteredAt;
    public int ComboIndex => comboIndex;
    // Never skip the current profile's hit window, even if an input arrives early.
    public bool CanLink => attackAnimationSeen && attackEnded && !player.animator.IsInTransition(0);

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        comboIndex = 1;
        maxCombo = player.StanceController != null ? player.StanceController.MaxCombo : 1;
        attackAnimationSeen = false;
        attackExecutor = player.GetComponent<AttackExecutor>();

        PlayCurrentAttack();
    }

    public override void FixedUpdate() => player.Motor.Move(Vector3.zero, Time.fixedDeltaTime);

    /// <summary>首段与续段均由同一个 Attack InputAction 转发；下一次状态更新消费一次。</summary>
    public void RequestCombo() => player.RequestBasicAttack();

    public override void Update()
    {
        AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
        bool isAttackAnimation = stateInfo.IsTag(AttackTag);
        bool isTransitioning = player.animator.IsInTransition(0);
        // 使用本段的绝对进度，避免动画超过一轮后重新打开输入窗口。
        float normalizedTime = stateInfo.normalizedTime;
        if (isTransitioning) return;
        if (!isAttackAnimation)
        {
            // 输入回调先于 Animator 更新；必须等实际进入过攻击动画后才能判定结束。
            if (attackAnimationSeen || Time.time - enteredAt > 1f) ChangeToLocomotionState();
            return;
        }

        // 淡入到 Animator 真正切段之间，仍可能读到上一段动画。
        if (stateInfo.shortNameHash != expectedAnimationHash) return;

        attackAnimationSeen = true;
        progress = normalizedTime;
        // 先结算本段已到达的伤害时间，再切换下一段的 Profile。
        UpdateAttackTiming(normalizedTime);

        // 结束由 Animator 的 Attack -> MoveTree 过渡决定；不再维护第二份结束时间。
    }

    public bool TryContinue()
    {
        if (!CanLink || !player.CanAttack) return false;
        // Finishers (and single-shot stances) retain a recovery before starting a new chain.
        if (comboIndex >= maxCombo && progress < .9f) return false;
        comboIndex = comboIndex >= maxCombo ? 1 : comboIndex + 1;
        PlayCurrentAttack();
        return true;
    }

    private void PlayCurrentAttack()
    {
        attackExecutor?.AttackEnd();
        attackExecutor?.SetCurrentComboIndex(comboIndex);
        expectedAnimationHash = Animator.StringToHash($"Attack_{comboIndex}");
        currentProfile = attackExecutor != null ? attackExecutor.GetCurrentAttackProfile() : null;
        attackStarted = false;
        attackReleased = false;
        attackEnded = false;
        attackAnimationSeen = false;
        enteredAt = Time.time;
        progress = 0f;
        player.animator.SetInteger(ComboIndexParameter, comboIndex);
        player.animator.ResetTrigger(AttackTrigger);
        player.animator.CrossFadeInFixedTime($"Attack_{comboIndex}", .06f, 0, 0f);
    }

    private void UpdateAttackTiming(float normalizedTime)
    {
        if (currentProfile == null) { attackEnded = normalizedTime >= .8f; return; }

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

        if (!attackEnded && normalizedTime >= Mathf.Max(currentProfile.StartTime, Mathf.Max(currentProfile.ReleaseTime, currentProfile.EndTime)))
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
        player.animator.ResetTrigger(AttackTrigger);
        attackExecutor?.AttackEnd();
    }
}
