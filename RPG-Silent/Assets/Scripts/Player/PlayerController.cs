using RPGSilent.Application;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;

public class PlayerController : MonoBehaviour, IDamageable, IRewardable
{
    // ── VContainer 注入 ────────────────────────────────────────────────────────
    [Inject] private IInputService              _inputService;
    [Inject] private IPlayerStatsReader         _statsReader;
    [Inject] private PlayerTakeDamageUseCase    _takeDamageUseCase;
    [Inject] private PlayerAddRewardUseCase     _addRewardUseCase;
    [Inject] private IPlayerInputActions        _playerInputActions;
    [Inject] private IControllerSettingsService _controllerSettings;
    [Inject] private IGameSettingsService       _gameSettings;

    // ── FSM ────────────────────────────────────────────────────────────────────
    public PlayerStateMachine StateMachine { get; private set; }

    // ── Unity 组件引用 ─────────────────────────────────────────────────────────
    public Animator   animator;
    public Rigidbody  rb;

    // ── IDamageable 实现 ───────────────────────────────────────────────────────
    public bool IsDead => _statsReader?.IsDead ?? false;

    // ── 移动状态 ───────────────────────────────────────────────────────────────
    public bool IsJumping { get; private set; }
    public bool IsRolling { get; private set; }

    public Vector2 InputDir  => _inputService?.MoveInput ?? Vector2.zero;
    public bool    IsRunning => InputDir.magnitude > 0.5f;

    public bool IsSprinting => _sprintActive && CanSprint;
    public bool IsWalking   => _walkActive   && CanWalk;

    public bool CanRoll => !IsDead && !IsJumping && !IsRolling
        && (StanceController == null || StanceController.CanRoll);

    public bool CanDiveRoll => CanRoll && InputDir.magnitude > 0.1f;

    private bool CanSprint => !IsDead && InputDir.magnitude > 0.1f
        && !IsRolling && !IsJumping
        && (StanceController == null || StanceController.CanSprint);

    private bool CanWalk => !IsDead && InputDir.magnitude > 0.1f
        && !IsSprinting && !IsRolling && !IsJumping;

    public float WalkSpeed   = 2.5f;
    public float MoveSpeed   = 5f;
    public float SprintSpeed = 8f;
    public float CurrentMoveSpeed => IsSprinting ? SprintSpeed : IsWalking ? WalkSpeed : MoveSpeed;

    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public bool CanAttack => !IsDead && !IsRolling && !IsJumping
        && (StanceController == null || StanceController.MaxCombo > 0);

    // ── 子系统组件引用 ─────────────────────────────────────────────────────────
    public PlayerAnimationController AnimationController { get; private set; }
    public PlayerStanceController    StanceController    { get; private set; }

    public PlayerStance CurrentStance =>
        StanceController != null ? StanceController.CurrentStance : PlayerStance.Relax;

    // ── 输入参数（Inspector 可调，在无注入时作兜底默认值）────────────────────────
    public float mouseSensitivity = 3f;
    public float RollForce        = 5f;

    private float yaw;
    private float pitch;

    private bool _sprintActive;
    private bool _walkActive;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        StateMachine        = new PlayerStateMachine();
        AnimationController = GetComponent<PlayerAnimationController>();
        StanceController    = GetComponent<PlayerStanceController>();
        rb                  = GetComponent<Rigidbody>();
        animator            = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (_playerInputActions == null) return;

        _playerInputActions.SprintStarted        += OnSprintStarted;
        _playerInputActions.SprintEnded          += OnSprintEnded;
        _playerInputActions.RollTriggered        += OnRollTriggered;
        _playerInputActions.JumpTriggered        += OnJumpTriggered;
        _playerInputActions.WalkStarted          += OnWalkStarted;
        _playerInputActions.WalkEnded            += OnWalkEnded;
        _playerInputActions.AttackTriggered      += OnAttackTriggered;
        _playerInputActions.StanceToggleTriggered += OnStanceToggleTriggered;
    }

    private void OnDisable()
    {
        if (_playerInputActions == null) return;

        _playerInputActions.SprintStarted        -= OnSprintStarted;
        _playerInputActions.SprintEnded          -= OnSprintEnded;
        _playerInputActions.RollTriggered        -= OnRollTriggered;
        _playerInputActions.JumpTriggered        -= OnJumpTriggered;
        _playerInputActions.WalkStarted          -= OnWalkStarted;
        _playerInputActions.WalkEnded            -= OnWalkEnded;
        _playerInputActions.AttackTriggered      -= OnAttackTriggered;
        _playerInputActions.StanceToggleTriggered -= OnStanceToggleTriggered;
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();

        if (IsDead) return;

        // 鼠标视角（灵敏度与反转Y轴实时读取）
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

        int scaledDamage = _gameSettings?.ScaleIncomingDamage(damage) ?? damage;
        _takeDamageUseCase.Execute(scaledDamage);
        GetComponent<SkillCastManager>()?.InterruptSkill();

        if (IsDead)
            StateMachine.ChangeState(new DeadState(this));
        else
            StateMachine.ChangeState(new HurtState(this));
    }

    // ── IRewardable ────────────────────────────────────────────────────────────

    public void AddReward(int gold, int exp) => _addRewardUseCase.Execute(gold, exp);

    // ── 状态辅助方法 ───────────────────────────────────────────────────────────

    public void SetIsJumping(bool jumping) => IsJumping = jumping;
    public void SetIsRolling(bool rolling) => IsRolling = rolling;

    // ── 输入事件回调 ───────────────────────────────────────────────────────────

    private void OnSprintStarted()        => _sprintActive = true;
    private void OnSprintEnded()          => _sprintActive = false;
    private void OnWalkStarted()          => _walkActive   = true;
    private void OnWalkEnded()            => _walkActive   = false;

    private void OnAttackTriggered()
    {
        if (CanAttack) StateMachine.ChangeState(new AttackState(this));
    }

    private void OnRollTriggered()
    {
        if (CanRoll) StateMachine.ChangeState(new RollState(this));
    }

    private void OnJumpTriggered()
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

    private void OnStanceToggleTriggered() => StanceController?.ToggleStance();
}
