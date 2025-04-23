using UnityEngine;
public class HurtState : PlayerState
{
    private float hurtDuration = 0.3f;
    private float timer;

    public HurtState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("受击！");
        timer = hurtDuration;
        // 播放受击动画/震动等
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
