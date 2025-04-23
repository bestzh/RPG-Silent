using UnityEngine;

public class DeadState : PlayerState
{
    public DeadState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("死亡！");
        // 播放死亡动画，禁用输入等
    }

    public override void Update()
    {
        // 死亡后不能操作
    }
}
