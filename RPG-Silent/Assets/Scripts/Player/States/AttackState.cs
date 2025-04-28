
using UnityEngine;
public class AttackState : PlayerState
{
    private float attackTime = 0.5f;  // 攻击持续时间
    private float timer;

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("攻击！");
        timer = attackTime;

        player.animator.SetTrigger("Attack"); // 播放攻击动画
        DetectEnemies(); // 执行敌人检测
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
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
