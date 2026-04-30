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

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        comboIndex = 1;
        maxCombo = player.StanceController != null ? player.StanceController.MaxCombo : 1;
        stateTimer = 0f;

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
        player.animator.SetInteger(ComboIndexParameter, comboIndex);
        player.animator.SetTrigger(AttackTrigger);
        DetectEnemies();
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

    private void DetectEnemies()
    {
        // 检测攻击范围内的敌人（简单的近距离检测）
        Collider[] hits = Physics.OverlapSphere(player.transform.position + player.transform.forward, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<EnemyController>()?.TakeDamage(20);  // 伤害值可以调整
            }
        }
    }
}
