using UnityEngine;

public class JumpState : PlayerState
{
    private enum Phase
    {
        Rising,   // 起跳上升（播放 JumpTree）
        Falling,  // 滞空下落（播放 Fall）
        Landing   // 落地缓冲（播放 Land）
    }

    // Animator Base Layer 状态名须与 PlayerAnim.controller 完全一致（clip 由 OverrideController 替换）
    private const string JumpTreeState = "JumpTree";
    private const string MoveTreeState = "MoveTree";
    private const string FallState = "Relax-Fall";
    private const string LandState = "Relax-Land";
    private const string DoubleJumpState = "Unarmed-Jump-Flip";

    // 与 Animator 中 JumpTree 的 BlendParameter 一致
    private const string MoveSpeedParam = "MoveSpeed";

    // 物理 / 时间参数
    private const float JumpForce = 6f;
    private const float DoubleJumpForce = 6.5f; // 二段跳力度（独立可调）
    private const float MinRisingDuration = 0.15f; // 起跳起手时间，避免起步那一帧就被判定为已落地
    private const float ApexVelocityThreshold = 0.1f; // 当 y 方向速度小于该值时视为到达跳跃顶点
    private const float LandDuration = 0.35f; // 落地动作总时长，到时即可恢复地面状态

    // 过渡时间（秒）
    private const float JumpFadeTime = 0.08f;
    private const float FallFadeTime = 0.10f;
    private const float LandFadeTime = 0.05f;
    private const float DoubleJumpFadeTime = 0.05f;
    private const float ReturnToLocomotionFadeTime = 0.08f;

    // BlendTree 阈值
    private const float StandingJumpBlend = 0f;
    private const float RunningJumpBlend = 0.1f;

    private Phase phase;
    private float phaseTimer;
    private bool hasAppliedJumpForce;
    private readonly bool startFalling;

    private int maxJumpCount;
    private int jumpsUsed;

    public JumpState(PlayerController player, bool startFalling = false) : base(player)
    {
        this.startFalling = startFalling;
    }

    public override void Enter()
    {
        maxJumpCount = player.StanceController != null ? player.StanceController.MaxJumpCount : 1;
        if (!startFalling && maxJumpCount < 1)
        {
            BackToGroundedState();
            return;
        }
        jumpsUsed = startFalling ? 0 : 1;
        player.SetIsJumping(true);

        if (startFalling)
        {
            phase = Phase.Falling;
            phaseTimer = 0f;
            hasAppliedJumpForce = true;
            player.animator.CrossFadeInFixedTime(FallState, FallFadeTime);
            return;
        }

        player.animator.SetFloat(MoveSpeedParam, player.IsRunning ? RunningJumpBlend : StandingJumpBlend);

        player.animator.CrossFadeInFixedTime(JumpTreeState, JumpFadeTime);

        phase = Phase.Rising;
        phaseTimer = 0f;
        hasAppliedJumpForce = false;
    }

    public override void Update()
    {
        phaseTimer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Rising:
                TickRising();
                break;
            case Phase.Falling:
                TickFalling();
                break;
            case Phase.Landing:
                TickLanding();
                break;
        }

        ApplyAirMovement();
    }

    public override void Exit()
    {
        player.SetIsJumping(false);
    }

    // 由 PlayerController 在跳跃键再次按下、且当前已处于 JumpState 时调用
    public bool TryAirJump()
    {
        if (jumpsUsed >= maxJumpCount)
        {
            return false;
        }

        // 二段跳触发条件：必须等首跳的"起跳段（Rising）"结束。
        // Falling（下落途中）和 Landing（落地缓冲）都允许触发，给玩家"补救跳"的容错。
        if (phase == Phase.Rising)
        {
            return false;
        }

        jumpsUsed++;

        // 二段跳：清空当前 y 速度后再施力，跳跃感更"脆"
        Vector3 vel = player.rb.linearVelocity;
        vel.y = 0f;
        player.rb.linearVelocity = vel;
        player.rb.AddForce(Vector3.up * DoubleJumpForce, ForceMode.Impulse);

        // 切到 DoubleJump 状态（各姿态用 OverrideController 替换具体 clip）
        player.animator.CrossFadeInFixedTime(DoubleJumpState, DoubleJumpFadeTime);

        // 重新进入上升阶段，但不再施加首跳力
        phase = Phase.Rising;
        phaseTimer = 0f;
        hasAppliedJumpForce = true;
        return true;
    }

    private void TickRising()
    {
        if (!hasAppliedJumpForce && player.IsGrounded)
        {
            player.rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            hasAppliedJumpForce = true;
        }

        // 已经施加跳跃力 + 度过最小起跳时间 + 垂直速度回落 → 进入下落阶段
        if (hasAppliedJumpForce
            && phaseTimer >= MinRisingDuration
            && player.rb.linearVelocity.y <= ApexVelocityThreshold)
        {
            EnterFalling();
        }
    }

    private void TickFalling()
    {
        if (player.IsGrounded)
        {
            EnterLanding();
        }
    }

    private void TickLanding()
    {
        if (phaseTimer >= LandDuration)
        {
            BackToGroundedState();
        }
    }

    private void EnterFalling()
    {
        phase = Phase.Falling;
        phaseTimer = 0f;
        player.animator.CrossFadeInFixedTime(FallState, FallFadeTime);
    }

    private void EnterLanding()
    {
        phase = Phase.Landing;
        phaseTimer = 0f;
        player.animator.CrossFadeInFixedTime(LandState, LandFadeTime);
    }

    private void BackToGroundedState()
    {
        player.SetIsJumping(false);

        // 必须回到 MoveTree，否则 Animator 仍停在 Relax-Land，Horizontal/Vertical 不会驱动八向移动（Root Motion 时还会顶住脚本位移）
        player.animator.CrossFadeInFixedTime(MoveTreeState, ReturnToLocomotionFadeTime);

        if (player.InputDir.magnitude > 0.1f)
        {
            player.StateMachine.ChangeState(new MoveState(player));
        }
        else
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }

    // 空中允许玩家保持水平方向输入，让跳跃手感自然
    private void ApplyAirMovement()
    {
        Vector2 input = player.InputDir;
        if (input.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 moveDir = (player.transform.forward * input.y + player.transform.right * input.x).normalized;
        float airSpeed = player.CurrentMoveSpeed;
        Vector3 vel = player.rb.linearVelocity;
        player.rb.linearVelocity = new Vector3(moveDir.x * airSpeed, vel.y, moveDir.z * airSpeed);
    }
}
