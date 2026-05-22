using UnityEngine;

public class HurtState : PlayerState
{
    private const float HurtDuration = 0.3f;
    private float timer;

    public HurtState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("Player hurt.", player);
        timer = HurtDuration;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }
}
