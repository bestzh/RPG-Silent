
using UnityEngine;
public class AttackState : PlayerState
{
    private float attackTime = 0.5f;
    private float timer;

    public AttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("攻击！");
        timer = attackTime;
        // 播放攻击动画等
         // 检测敌人（简单近距离检测）
        Collider[] hits = Physics.OverlapSphere(player.transform.position + player.transform.forward, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<EnemyController>()?.TakeDamage(20);
            }
        }
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }
}
