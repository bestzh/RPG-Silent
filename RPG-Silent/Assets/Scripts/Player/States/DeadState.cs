using UnityEngine;

public class DeadState : PlayerState
{
    public DeadState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("Player died.", player);
        player.animator.SetTrigger("Die");
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
