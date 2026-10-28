using RPGSilent.Application;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public partial class PlayerController : MonoBehaviour, IDamageable, IRewardable
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

    public Vector2 InputDir => !GameplayInputEnabled ? Vector2.zero
        : Vector2.ClampMagnitude(_playerInputActions?.MoveInput ?? _inputService?.MoveInput ?? Vector2.zero, 1f);
    public bool GameplayInputEnabled => _playerInputActions?.GameplayInputEnabled ?? false;
    public bool    IsRunning => InputDir.magnitude > 0.5f;

    public bool IsSprinting => _sprintActive && CanSprint;
    public bool IsWalking   => _walkActive   && CanWalk;

    public bool CanRoll => GameplayInputEnabled && !IsDead && IsGrounded && !IsJumping && !IsRolling
        && !(Weapons != null && Weapons.IsBusy)
        && !(StateMachine.CurrentState is HurtState)
        && (StanceController == null || StanceController.CanRoll);

    public bool CanDiveRoll => CanRoll && InputDir.magnitude > 0.1f;

    private bool CanSprint => !IsDead && InputDir.y > 0.5f
        && !IsRolling && !IsJumping
        && (StanceController == null || StanceController.CanSprint);

    private bool CanWalk => !IsDead && InputDir.magnitude > 0.1f
        && !IsSprinting && !IsRolling && !IsJumping;

    public float WalkSpeed   = 2.5f;
    public float MoveSpeed   = 5f;
    public float SprintSpeed = 8f;
    public float CurrentMoveSpeed => (IsSprinting ? SprintSpeed : IsWalking ? WalkSpeed : MoveSpeed) * (Statuses?.State.MovementMultiplier ?? 1f);
    public CombatStatusController Statuses { get; private set; }
    public bool CanReceiveCombatEffects => CombatAllowed && !combatLocked && !IsDead;
    public bool CanUseConsumables => isActiveAndEnabled && GameplayInputEnabled && !combatLocked && !IsDead && !(Weapons != null && Weapons.IsBusy);

    public bool IsGrounded => Motor != null && Motor.IsGrounded;
    public PlayerMotor Motor { get; private set; }
    public bool CombatAllowed { get; private set; } = true;
    public bool CanChangeEquipment => CanStartWeaponTransition && !(Weapons != null && Weapons.IsBusy);
    public bool CanStartWeaponTransition => isActiveAndEnabled && GameplayInputEnabled && !combatLocked && !IsDead && IsGrounded
        && !IsJumping && !IsRolling && (skillController == null || !skillController.IsCasting)
        && (StateMachine.CurrentState is IdleState || StateMachine.CurrentState is MoveState);
    public Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0f);
    public Quaternion FacingRotation => Quaternion.Euler(0f, yaw, 0f);

    public bool CanAttack => CombatAllowed && IsGrounded && !IsDead && !IsRolling && !IsJumping
        && !(Weapons != null && Weapons.IsBusy)
        && !(StateMachine.CurrentState is HurtState)
        && (skillController == null || !skillController.IsCasting)
        && (StanceController == null || StanceController.MaxCombo > 0);

    // ── 子系统组件引用 ─────────────────────────────────────────────────────────
    public PlayerAnimationController AnimationController { get; private set; }
    public PlayerStanceController    StanceController    { get; private set; }
    public PlayerWeaponEquipment Weapons { get; private set; }

    public PlayerStance CurrentStance =>
        StanceController != null ? StanceController.EffectiveStance : PlayerStance.Relax;

    // ── 输入参数（Inspector 可调，在无注入时作兜底默认值）────────────────────────
    public float mouseSensitivity = 3f;
    public float RollForce        = 5f;

    [Header("移动手感")]
    [SerializeField] private PlayerMovementSettings movement = new PlayerMovementSettings();
    [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.12f;
    private float jumpRequestedUntil = float.NegativeInfinity;

    private float yaw;
    private float pitch;

    private bool _sprintActive;
    private bool _walkActive;
    private PlayerSkillController skillController;
    private bool combatLocked;
    private bool wasKinematic;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        StateMachine        = new PlayerStateMachine();
        AnimationController = GetComponent<PlayerAnimationController>();
        StanceController    = GetComponent<PlayerStanceController>();
        Weapons             = GetComponent<PlayerWeaponEquipment>();
        Statuses = GetComponent<CombatStatusController>() ?? gameObject.AddComponent<CombatStatusController>();
        rb                  = GetComponent<Rigidbody>();
        animator            = GetComponent<Animator>();
        skillController     = GetComponent<PlayerSkillController>();
        Motor = new PlayerMotor(rb, GetComponent<CapsuleCollider>(), movement);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        yaw = transform.eulerAngles.y;
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
        ClearCombatInput();
        jumpRequestedUntil = float.NegativeInfinity;
        _sprintActive = _walkActive = false;
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
        Motor.RefreshGrounded(Time.time);
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();
        ConsumeCombatInput();

        if (IsDead || !GameplayInputEnabled)
        {
            jumpRequestedUntil = float.NegativeInfinity;
            return;
        }

        TryConsumeJump();
        // Releasing the cursor for UI must not keep rotating the player/camera.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // 鼠标视角（灵敏度与反转Y轴实时读取）
        float sensitivity = _controllerSettings?.CurrentSettings.MouseSensitivity ?? mouseSensitivity;
        bool  invertY     = _controllerSettings?.CurrentSettings.InvertY ?? false;

        Vector2 look = _playerInputActions.LookInput;
        float mouseX = look.x * sensitivity;
        float mouseY = look.y * sensitivity;
        yaw   += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch  = Mathf.Clamp(pitch, -35f, 60f);
    }

    private void FixedUpdate()
    {
        Motor.RefreshGrounded(Time.time);
        if (!rb.isKinematic) rb.MoveRotation(FacingRotation);
        StateMachine.FixedUpdate();
        if (!GameplayInputEnabled || skillController != null && skillController.IsCasting) Motor.SetHorizontalVelocity(Vector3.zero);
    }

    public Vector3 MovementVelocity(float speed)
    {
        Vector2 input = InputDir;
        return FacingRotation * new Vector3(input.x, 0f, input.y) * speed;
    }

    public void UpdateLocomotionAnimation()
    {
        Vector3 localVelocity = Quaternion.Inverse(FacingRotation) * rb.linearVelocity;
        Vector2 planar = new Vector2(localVelocity.x, localVelocity.z);
        float speed = planar.magnitude;
        float blend = speed <= MoveSpeed ? speed / Mathf.Max(0.01f, MoveSpeed)
            : 1f + (speed - MoveSpeed) / Mathf.Max(0.01f, SprintSpeed - MoveSpeed);
        Vector2 target = planar.normalized * Mathf.Min(2f, blend);
        animator.SetFloat("Horizontal", target.x, 0.08f, Time.deltaTime);
        animator.SetFloat("Vertical", target.y, 0.08f, Time.deltaTime);
        animator.SetFloat("MoveSpeed", blend, 0.08f, Time.deltaTime);
    }

    public void ReturnToLocomotion()
    {
        animator.CrossFadeInFixedTime("MoveTree", 0.08f);
        StateMachine.ChangeState(InputDir.sqrMagnitude > 0.01f
            ? (PlayerState)new MoveState(this) : new IdleState(this));
    }

    public void SetCombatAllowed(bool allowed)
    {
        ClearCombatInput();
        CombatAllowed = allowed;
        if (!allowed)
        {
            skillController?.InterruptSkill(force: true);
            GetComponent<AttackExecutor>()?.ResetCombat();
            if (StateMachine.CurrentState is AttackState) ReturnToLocomotion();
        }
        if (Weapons != null) Weapons.SetCombatAllowed(allowed);
        else StanceController?.SetCombatAllowed(allowed);
    }

    // ── IDamageable ────────────────────────────────────────────────────────────

    public void TakeDamage(int damage) => ReceiveDamage(damage, true);
    public void TakePeriodicDamage(int damage) => ReceiveDamage(damage, false);
    void ReceiveDamage(int damage, bool interrupt)
    {
        if (!CombatAllowed || combatLocked || IsDead || damage <= 0) return;

        int scaledDamage = _gameSettings?.ScaleIncomingDamage(damage) ?? damage;
        scaledDamage = Mathf.Max(1, Mathf.CeilToInt(scaledDamage * (Statuses?.State.IncomingMultiplier ?? 1f)));
        _takeDamageUseCase.Execute(scaledDamage);
        if (interrupt || IsDead) ClearCombatInput();
        if (interrupt || IsDead) skillController?.InterruptSkill(force: IsDead);

        if (IsDead)
            StateMachine.ChangeState(new DeadState(this));
        else if (interrupt)
            StateMachine.ChangeState(new HurtState(this));
    }

    // ── IRewardable ────────────────────────────────────────────────────────────

    public void AddReward(int gold, int exp) => _addRewardUseCase.Execute(gold, exp);

    public void LockForDungeonResult()
    {
        if (combatLocked) return;
        combatLocked = true;
        Weapons?.CancelTransition();
        skillController?.InterruptSkill(force: true);
        GetComponent<AttackExecutor>()?.ResetCombat(suspended: true);
        if (!IsDead)
        {
            StateMachine.ChangeState(new IdleState(this));
            AnimationController?.StopAnimation();
        }
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
        }
        enabled = false;
    }

    public void ResetForScene(Vector3 position, Quaternion rotation)
    {
        ClearCombatInput();
        Statuses?.State.Clear();
        Weapons?.CancelTransition();
        skillController?.ResetForScene();
        GetComponent<AttackExecutor>()?.ResetCombat();
        StateMachine.ChangeState(new IdleState(this));
        IsJumping = IsRolling = _sprintActive = _walkActive = false;
        jumpRequestedUntil = float.NegativeInfinity;
        animator.SetBool("IsDead", false);
        animator.SetBool("IsRolling", false);
        animator.ResetTrigger("Die");
        animator.ResetTrigger("Attack");
        animator.SetInteger("ComboIndex", 0);
        AnimationController?.StopAnimation();
        if (rb != null && combatLocked) rb.isKinematic = wasKinematic;
        combatLocked = false;
        Teleport(position, rotation);
        enabled = true;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        yaw = rotation.eulerAngles.y;
        pitch = 0f;
        jumpRequestedUntil = float.NegativeInfinity;
        Motor?.ResetGrounding();
        transform.SetPositionAndRotation(position, rotation);
        if (rb == null) return;
        if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.position = position;
        rb.rotation = rotation;
    }

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
        if (Cursor.lockState == CursorLockMode.Locked) RequestBasicAttack();
    }

    private void OnRollTriggered()
    {
        if (!CanRoll || skillController != null && !skillController.TryInterruptSkill()) return;
        ClearCombatInput();
        StateMachine.ChangeState(new RollState(this));
    }

    private void OnJumpTriggered()
    {
        if (Weapons != null && Weapons.IsBusy) return;
        if (!GameplayInputEnabled || IsDead || IsRolling || StateMachine.CurrentState is HurtState) return;
        if (StanceController != null && !StanceController.CanJump) return;
        jumpRequestedUntil = Time.time + jumpBufferTime;
        TryConsumeJump();
    }

    private void TryConsumeJump()
    {
        if (Weapons != null && Weapons.IsBusy) { jumpRequestedUntil = float.NegativeInfinity; return; }
        if (Time.time > jumpRequestedUntil || IsRolling || StateMachine.CurrentState is HurtState) return;
        if (skillController != null && !skillController.TryInterruptSkill()) return;

        if (StateMachine.CurrentState is JumpState jumpState)
        {
            if (jumpState.TryAirJump()) jumpRequestedUntil = float.NegativeInfinity;
            return;
        }

        if (!IsGrounded && !Motor.CanCoyoteJump(Time.time)) return;
        jumpRequestedUntil = float.NegativeInfinity;
        ClearCombatInput();
        StateMachine.ChangeState(new JumpState(this));
    }

    private void OnStanceToggleTriggered()
    {
        if (Weapons != null) { Weapons.TrySwitchNext(); return; }
        if (!CombatAllowed || !GameplayInputEnabled || !IsGrounded || IsDead) return;
        if (!(StateMachine.CurrentState is IdleState) && !(StateMachine.CurrentState is MoveState)) return;
        if (skillController == null || !skillController.IsCasting) StanceController?.ToggleStance();
    }
}
