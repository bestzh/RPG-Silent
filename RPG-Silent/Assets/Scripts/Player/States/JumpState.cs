using UnityEngine;
public class JumpState : PlayerState
{
    public JumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        Debug.Log("跳跃！");
        player.AddJumpForce();
    }

    public override void Update()
    {
        if (player.IsGrounded && player.rb.linearVelocity.y <= 0.1f)
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }
}

