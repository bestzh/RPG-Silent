using UnityEngine;

public class DeadState : PlayerState
{
    public DeadState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("Player died.", player);
        // 清除动作计时，避免旧技能结束时把死亡动画切回 MoveTree。
        player.AnimationController?.StopAnimation();
        player.animator.ResetTrigger("Attack");
        player.animator.SetBool("IsDead", true);
        player.animator.SetTrigger("Die");
        if (player.rb != null && !player.rb.isKinematic)
        {
            player.rb.linearVelocity = new Vector3(0f, player.rb.linearVelocity.y, 0f);
            player.rb.angularVelocity = Vector3.zero;
        }
        player.enabled = false;
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
        base.Exit();
    }
}
