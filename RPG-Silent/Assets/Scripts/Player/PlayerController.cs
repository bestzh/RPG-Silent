using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Vector2 InputDir => InputManager.Instance != null ? InputManager.Instance.MoveInput : Vector2.zero;

    public Animator animator;
    public bool IsRunning => InputDir.magnitude > 0.5f;
    public bool IsSprinting => sprintInputActive && CanSprint;
    public bool IsWalking => walkInputActive && CanWalk;
    public bool CanRoll => !IsJumping
        && !IsRolling
        && (StanceController == null || StanceController.CanRoll);
    public bool CanDiveRoll => CanRoll && InputDir.magnitude > 0.1f;
    private bool CanSprint => InputDir.magnitude > 0.1f
        && !IsRolling
        && !IsJumping
        && (StanceController == null || StanceController.CanSprint);
    private bool CanWalk => InputDir.magnitude > 0.1f && !IsSprinting && !IsRolling && !IsJumping;

    public float WalkSpeed = 2.5f;
    public float MoveSpeed = 5f;
    public float SprintSpeed = 8f;
    public float CurrentMoveSpeed => IsSprinting ? SprintSpeed : IsWalking ? WalkSpeed : MoveSpeed;
    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public int MaxHealth = 100;
    public int CurrentHealth { get; private set; }

    public Rigidbody rb;

    private float yaw;
    private float pitch;
    private PlayerSkillController skillController;

    public float mouseSensitivity = 3f;
    public float RollForce = 5f;
    public float SprintHoldThreshold = 0.5f;

    public bool IsJumping { get; private set; } = false;
    public bool IsRolling { get; private set; } = false;
    public PlayerAnimationController AnimationController { get; private set; }
    public PlayerStanceController StanceController { get; private set; }
    public PlayerStance CurrentStance => StanceController != null ? StanceController.CurrentStance : PlayerStance.Relax;
    public bool CanAttack => !IsRolling
        && !IsJumping
        && (StanceController == null || StanceController.MaxCombo > 0);
    private InputAction sprintAction;
    private InputAction rollAction;
    private InputAction jumpAction;
    private InputAction walkAction;
    private InputAction stanceToggleAction;
    private bool sprintInputActive;
    private bool walkInputActive;

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        AnimationController = GetComponent<PlayerAnimationController>();
        StanceController = GetComponent<PlayerStanceController>();
        skillController = GetComponent<PlayerSkillController>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        CurrentHealth = MaxHealth;
        SetupInputActions();
    }

    private void OnEnable()
    {
        sprintAction?.Enable();
        rollAction?.Enable();
        jumpAction?.Enable();
        walkAction?.Enable();
        stanceToggleAction?.Enable();
    }

    private void OnDisable()
    {
        sprintAction?.Disable();
        rollAction?.Disable();
        jumpAction?.Disable();
        walkAction?.Disable();
        stanceToggleAction?.Disable();
    }

    private void OnDestroy()
    {
        if (sprintAction != null)
        {
            sprintAction.performed -= OnSprintPerformed;
            sprintAction.canceled -= OnSprintCanceled;
            sprintAction.Dispose();
        }
        if (rollAction != null)
        {
            rollAction.performed -= OnRollPerformed;
            rollAction.Dispose();
        }
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Dispose();
        }
        if (walkAction != null)
        {
            walkAction.performed -= OnWalkPerformed;
            walkAction.canceled -= OnWalkCanceled;
            walkAction.Dispose();
        }
        if (stanceToggleAction != null)
        {
            stanceToggleAction.performed -= OnStanceTogglePerformed;
            stanceToggleAction.Dispose();
        }
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();

        if (skillController == null && Input.GetMouseButtonDown(0))
        {
            if (CanAttack)
            {
                StateMachine.ChangeState(new AttackState(this));
            }
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -35f, 60f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public void SetIsJumping(bool jumping)
    {
        IsJumping = jumping;
    }

    public void SetIsRolling(bool rolling)
    {
        IsRolling = rolling;
    }

    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            StateMachine.ChangeState(new DeadState(this));
        }
        else
        {
            StateMachine.ChangeState(new HurtState(this));
        }
    }

    private void OnGetHit()
    {
        GetComponent<SkillCastManager>()?.InterruptSkill();
    }

    private void SetupInputActions()
    {
        sprintAction = new InputAction("SprintHold", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift").WithInteraction($"hold(duration={SprintHoldThreshold})");
        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;

        rollAction = new InputAction("RollTap", InputActionType.Button);
        rollAction.AddBinding("<Keyboard>/leftShift").WithInteraction($"tap(duration={SprintHoldThreshold})");
        rollAction.performed += OnRollPerformed;

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.performed += OnJumpPerformed;

        walkAction = new InputAction("WalkHold", InputActionType.Button);
        walkAction.AddBinding("<Keyboard>/leftAlt");
        walkAction.performed += OnWalkPerformed;
        walkAction.canceled += OnWalkCanceled;

        stanceToggleAction = new InputAction("StanceToggle", InputActionType.Button);
        stanceToggleAction.AddBinding("<Keyboard>/tab");
        stanceToggleAction.performed += OnStanceTogglePerformed;
    }

    private void OnStanceTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        StanceController?.ToggleStance();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        sprintInputActive = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        sprintInputActive = false;
    }

    private void OnRollPerformed(InputAction.CallbackContext context)
    {
        if (CanRoll)
        {
            StateMachine.ChangeState(new RollState(this));
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (IsRolling)
        {
            return;
        }

        // 已经在跳跃中：尝试触发空中跳（二段跳）
        if (StateMachine.CurrentState is JumpState jumpState)
        {
            jumpState.TryAirJump();
            return;
        }

        // 当前姿态禁跳直接忽略
        if (StanceController != null && !StanceController.CanJump)
        {
            return;
        }

        StateMachine.ChangeState(new JumpState(this));
    }

    private void OnWalkPerformed(InputAction.CallbackContext context)
    {
        walkInputActive = true;
    }

    private void OnWalkCanceled(InputAction.CallbackContext context)
    {
        walkInputActive = false;
    }
}
