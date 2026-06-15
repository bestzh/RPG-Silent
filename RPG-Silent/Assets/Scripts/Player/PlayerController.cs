using RPGSilent.Application;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class PlayerController : MonoBehaviour, IDamageable, IRewardable
{
    // ── VContainer 注入 ────────────────────────────────────────────────────────
    [Inject] private IInputService              _inputService;
    [Inject] private IPlayerStatsReader         _statsReader;
    [Inject] private PlayerTakeDamageUseCase    _takeDamageUseCase;
    [Inject] private PlayerAddRewardUseCase     _addRewardUseCase;
    [Inject] private IControllerSettingsService _controllerSettings;

    // ── FSM ────────────────────────────────────────────────────────────────────
    public PlayerStateMachine StateMachine { get; private set; }

    // ── Unity 组件引用（Inspector 或 GetComponent 获取）────────────────────────
    public Animator animator;
    public Rigidbody rb;

    // ── IDamageable 实现 ───────────────────────────────────────────────────────
    public bool IsDead => _statsReader?.IsDead ?? false;

    // ── 移动状态（本地，无需持久化）────────────────────────────────────────────
    public bool IsJumping  { get; private set; }
    public bool IsRolling  { get; private set; }

    public Vector2 InputDir => _inputService?.MoveInput ?? Vector2.zero;

    public bool IsRunning  => InputDir.magnitude > 0.5f;
    public bool IsSprinting => sprintInputActive && CanSprint;
    public bool IsWalking  => walkInputActive && CanWalk;

    public bool CanRoll => !IsDead
        && !IsJumping
        && !IsRolling
        && (StanceController == null || StanceController.CanRoll);

    public bool CanDiveRoll => CanRoll && InputDir.magnitude > 0.1f;

    private bool CanSprint => !IsDead
        && InputDir.magnitude > 0.1f
        && !IsRolling
        && !IsJumping
        && (StanceController == null || StanceController.CanSprint);

    private bool CanWalk => !IsDead && InputDir.magnitude > 0.1f
        && !IsSprinting && !IsRolling && !IsJumping;

    public float WalkSpeed   = 2.5f;
    public float MoveSpeed   = 5f;
    public float SprintSpeed = 8f;
    public float CurrentMoveSpeed => IsSprinting ? SprintSpeed : IsWalking ? WalkSpeed : MoveSpeed;

    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public bool CanAttack => !IsDead
        && !IsRolling
        && !IsJumping
        && (StanceController == null || StanceController.MaxCombo > 0);

    // ── 子系统组件引用 ─────────────────────────────────────────────────────────
    public PlayerAnimationController AnimationController { get; private set; }
    public PlayerStanceController    StanceController    { get; private set; }

    public PlayerStance CurrentStance =>
        StanceController != null ? StanceController.CurrentStance : PlayerStance.Relax;

    // ── 输入参数 ───────────────────────────────────────────────────────────────
    public float mouseSensitivity    = 3f;
    public float RollForce           = 5f;
    public float SprintHoldThreshold = 0.5f;

    private float yaw;
    private float pitch;

    private InputAction sprintAction;
    private InputAction rollAction;
    private InputAction jumpAction;
    private InputAction walkAction;
    private InputAction stanceToggleAction;
    private bool sprintInputActive;
    private bool walkInputActive;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        StateMachine        = new PlayerStateMachine();
        AnimationController = GetComponent<PlayerAnimationController>();
        StanceController    = GetComponent<PlayerStanceController>();
        rb                  = GetComponent<Rigidbody>();
        animator            = GetComponent<Animator>();
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
        DisposeAction(ref sprintAction,      OnSprintPerformed,      OnSprintCanceled);
        DisposeAction(ref rollAction,        OnRollPerformed);
        DisposeAction(ref jumpAction,        OnJumpPerformed);
        DisposeAction(ref walkAction,        OnWalkPerformed,        OnWalkCanceled);
        DisposeAction(ref stanceToggleAction, OnStanceTogglePerformed);
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();

        if (IsDead) return;

        if (Input.GetMouseButtonDown(0) && CanAttack)
        {
            StateMachine.ChangeState(new AttackState(this));
        }

        float sensitivity = _controllerSettings?.CurrentSettings.MouseSensitivity ?? mouseSensitivity;
        bool  invertY     = _controllerSettings?.CurrentSettings.InvertY ?? false;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
        yaw   += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch  = Mathf.Clamp(pitch, -35f, 60f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // ── IDamageable ────────────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;

        _takeDamageUseCase.Execute(damage);
        GetComponent<SkillCastManager>()?.InterruptSkill();

        if (IsDead)
            StateMachine.ChangeState(new DeadState(this));
        else
            StateMachine.ChangeState(new HurtState(this));
    }

    // ── IRewardable ────────────────────────────────────────────────────────────

    public void AddReward(int gold, int exp)
    {
        _addRewardUseCase.Execute(gold, exp);
    }

    // ── 状态辅助方法 ───────────────────────────────────────────────────────────

    public void SetIsJumping(bool jumping) => IsJumping = jumping;
    public void SetIsRolling(bool rolling) => IsRolling = rolling;

    // ── 输入配置 ───────────────────────────────────────────────────────────────

    private void SetupInputActions()
    {
        // 优先使用持久化的冲刺持定时间，未注入时回退到本地字段
        float holdDuration = _controllerSettings?.CurrentSettings.SprintHoldTime
                             ?? SprintHoldThreshold;

        sprintAction = new InputAction("SprintHold", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift")
                    .WithInteraction($"hold(duration={holdDuration})");
        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled  += OnSprintCanceled;

        rollAction = new InputAction("RollTap", InputActionType.Button);
        rollAction.AddBinding("<Keyboard>/leftShift")
                  .WithInteraction($"tap(duration={holdDuration})");
        rollAction.performed += OnRollPerformed;

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.performed += OnJumpPerformed;

        walkAction = new InputAction("WalkHold", InputActionType.Button);
        walkAction.AddBinding("<Keyboard>/leftAlt");
        walkAction.performed += OnWalkPerformed;
        walkAction.canceled  += OnWalkCanceled;

        stanceToggleAction = new InputAction("StanceToggle", InputActionType.Button);
        stanceToggleAction.AddBinding("<Keyboard>/tab");
        stanceToggleAction.performed += OnStanceTogglePerformed;
    }

    private void OnStanceTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) StanceController?.ToggleStance();
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => sprintInputActive = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx)  => sprintInputActive = false;

    private void OnRollPerformed(InputAction.CallbackContext ctx)
    {
        if (CanRoll) StateMachine.ChangeState(new RollState(this));
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (IsRolling) return;

        if (StateMachine.CurrentState is JumpState jumpState)
        {
            jumpState.TryAirJump();
            return;
        }

        if (StanceController != null && !StanceController.CanJump) return;

        StateMachine.ChangeState(new JumpState(this));
    }

    private void OnWalkPerformed(InputAction.CallbackContext ctx) => walkInputActive = true;
    private void OnWalkCanceled(InputAction.CallbackContext ctx)  => walkInputActive = false;

    // ── 辅助工具 ───────────────────────────────────────────────────────────────

    private static void DisposeAction(ref InputAction action,
        System.Action<InputAction.CallbackContext> performed,
        System.Action<InputAction.CallbackContext> canceled = null)
    {
        if (action == null) return;
        action.performed -= performed;
        if (canceled != null) action.canceled -= canceled;
        action.Dispose();
        action = null;
    }
}
