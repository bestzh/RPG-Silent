using UnityEngine;

public class DeadState : PlayerState
{
    public DeadState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("死亡！");
        player.animator.SetTrigger("Die"); // 播放死亡动画
        player.enabled = false; // 禁用 PlayerController 输入
    }

    public override void Update()
    {
        // 死亡后不能操作
    }

    public override void Exit()
    {
        base.Exit();
        // 可选：如果死亡后想要复活，解锁一些东西
        // player.enabled = true; 
    }
}
