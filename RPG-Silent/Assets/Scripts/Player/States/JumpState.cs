using UnityEngine;

public class JumpState : PlayerState
{
    private enum Phase { Rising, Falling, Landing }
    private const float JumpForce = 6f;
    private const float DoubleJumpForce = 6.5f;
    private const float LandingDuration = 0.12f;
    private readonly bool startFalling;
    private Phase phase;
    private float phaseTimer;
    private float pendingImpulse;
    private int maxJumpCount;
    private int jumpsUsed;
    private bool leftGroundWithoutJump;

    public JumpState(PlayerController player, bool startFalling = false) : base(player)
    {
        this.startFalling = startFalling;
    }

    public override void Enter()
    {
        maxJumpCount = player.StanceController != null ? player.StanceController.MaxJumpCount : 1;
        player.SetIsJumping(true);
        // Walking off a ledge consumes the ground jump after the coyote window.
        jumpsUsed = 1;
        leftGroundWithoutJump = startFalling;
        if (startFalling)
            BeginFalling();
        else if (maxJumpCount > 0)
            BeginJump(JumpForce, "JumpTree");
        else
            player.ReturnToLocomotion();
    }

    public override void Update()
    {
        phaseTimer += Time.deltaTime;
        if (phase == Phase.Rising && pendingImpulse == 0f && phaseTimer >= 0.08f
            && player.rb.linearVelocity.y <= 0.1f)
            BeginFalling();
        else if (phase == Phase.Falling && player.IsGrounded && player.rb.linearVelocity.y <= 0.1f)
        {
            phase = Phase.Landing;
            phaseTimer = 0f;
            player.animator.CrossFadeInFixedTime("Relax-Land", 0.05f);
        }
        else if (phase == Phase.Landing)
        {
            if (!player.IsGrounded) BeginFalling();
            else if (phaseTimer >= LandingDuration) player.ReturnToLocomotion();
        }
    }

    public override void FixedUpdate()
    {
        if (pendingImpulse > 0f)
        {
            if (player.GameplayInputEnabled)
                player.Motor.Jump(pendingImpulse, Time.time);
            else
                BeginFalling();
            pendingImpulse = 0f;
        }
        player.Motor.Move(player.MovementVelocity(player.MoveSpeed), Time.fixedDeltaTime,
            airborne: !player.IsGrounded);
    }

    public bool TryAirJump()
    {
        if (maxJumpCount == 0 || pendingImpulse > 0f) return false;
        if (phase != Phase.Rising && (player.IsGrounded
            || leftGroundWithoutJump && player.Motor.CanCoyoteJump(Time.time)))
        {
            jumpsUsed = 1;
            BeginJump(JumpForce, "JumpTree");
            return true;
        }
        if (jumpsUsed >= maxJumpCount || phaseTimer < 0.08f) return false;
        jumpsUsed++;
        BeginJump(DoubleJumpForce, "Unarmed-Jump-Flip");
        return true;
    }

    private void BeginJump(float impulse, string animation)
    {
        pendingImpulse = impulse;
        leftGroundWithoutJump = false;
        phase = Phase.Rising;
        phaseTimer = 0f;
        player.animator.CrossFadeInFixedTime(animation, 0.06f);
    }

    private void BeginFalling()
    {
        phase = Phase.Falling;
        phaseTimer = 0f;
        player.animator.CrossFadeInFixedTime("Relax-Fall", 0.1f);
    }

    public override void Exit()
    {
        pendingImpulse = 0f;
        player.SetIsJumping(false);
    }
}
