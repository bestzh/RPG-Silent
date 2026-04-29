using UnityEngine;

public class RollState : PlayerState
{
    private const string RollAnimationName = "RollTree";
    private const float DiveRollBlendY = 2f;
    private readonly PlayerState previousState;

    public RollState(PlayerController player) : base(player)
    {
        previousState = player.StateMachine.CurrentState;
    }

    public override void Enter()
    {
        Vector2 rollInput = player.InputDir.sqrMagnitude > 0.01f ? player.InputDir.normalized : Vector2.up;
        Vector2 rollBlendInput = player.CanDiveRoll && rollInput.y > 0.5f
            ? new Vector2(0f, DiveRollBlendY)
            : rollInput;

        player.animator.SetFloat("RollX", rollBlendInput.x);
        player.animator.SetFloat("RollY", rollBlendInput.y);
        player.animator.SetBool("IsRolling", true);

        if (player.AnimationController.PlayAnimation(RollAnimationName))
        {
            player.SetIsRolling(true);

            Vector3 rollDirection = (player.transform.forward * rollInput.y + player.transform.right * rollInput.x).normalized;
            player.rb.linearVelocity = new Vector3(
                rollDirection.x * player.RollForce,
                player.rb.linearVelocity.y,
                rollDirection.z * player.RollForce
            );
        }
        else
        {
            player.animator.SetBool("IsRolling", false);
            ReturnToPreviousState();
        }
    }

    public override void Update()
    {
        if (!player.AnimationController.IsPlayingAnimation())
        {
            player.SetIsRolling(false);
            player.animator.SetBool("IsRolling", false);
            ReturnToPreviousState();
        }
    }

    public override void Exit()
    {
        player.SetIsRolling(false);
        player.animator.SetBool("IsRolling", false);
    }

    private void ReturnToPreviousState()
    {
        if (previousState != null && previousState.GetType() != typeof(RollState))
        {
            player.StateMachine.ChangeState(previousState);
            return;
        }

        if (player.InputDir.magnitude > 0.1f)
        {
            player.StateMachine.ChangeState(new MoveState(player));
        }
        else
        {
            player.StateMachine.ChangeState(new IdleState(player));
        }
    }
}
