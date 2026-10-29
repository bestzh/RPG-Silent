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
    [Inject] private PlayerStats combatStats;
    public PlayerStats CombatStats => combatStats;
    [Inject] private PlayerProgressService progress;
    [Inject] private GameAudioService audio;
    public GameAudioService Audio => audio;
    public IGameSettingsService PresentationSettings => _gameSettings;
    public PlayerProgressService Progress => progress;
    public PlayerClassCombat ClassCombat { get; private set; }
    public PlayerCombatRecord CombatRecord { get; } = new();
    public TrainingGround Training { get; set; }
    public PlayerExplorationInteraction Exploration { get; private set; }

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
        && (StanceController == null || StanceController.CanRoll) && (Defense == null || Defense.CanAffordRoll);

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
    public PlayerTargetLock TargetLock { get; private set; }
    public PlayerDefense Defense { get; private set; }
    public PlayerHitFeedback HitFeedback { get; private set; }
    public PlayerEquipmentEffects EquipmentEffects { get; private set; }
    public bool CanReceiveCombatEffects => isActiveAndEnabled && CombatAllowed && !combatLocked && !IsDead;
    public bool CanUseConsumables => Training == null && isActiveAndEnabled && GameplayInputEnabled && !combatLocked && !IsDead && !(Weapons != null && Weapons.IsBusy);

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
        TargetLock = GetComponent<PlayerTargetLock>() ?? gameObject.AddComponent<PlayerTargetLock>();
        Defense = GetComponent<PlayerDefense>() ?? gameObject.AddComponent<PlayerDefense>();
        HitFeedback = GetComponent<PlayerHitFeedback>() ?? gameObject.AddComponent<PlayerHitFeedback>();
        ClassCombat = GetComponent<PlayerClassCombat>() ?? gameObject.AddComponent<PlayerClassCombat>();
        EquipmentEffects = GetComponent<PlayerEquipmentEffects>() ?? gameObject.AddComponent<PlayerEquipmentEffects>();
        Exploration = GetComponent<PlayerExplorationInteraction>() ?? gameObject.AddComponent<PlayerExplorationInteraction>();
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
        Exploration?.Initialize(this, _playerInputActions);

        _playerInputActions.SprintStarted        += OnSprintStarted;
        _playerInputActions.SprintEnded          += OnSprintEnded;
        _playerInputActions.RollTriggered        += OnRollTriggered;
        _playerInputActions.JumpTriggered        += OnJumpTriggered;
        _playerInputActions.WalkStarted          += OnWalkStarted;
        _playerInputActions.WalkEnded            += OnWalkEnded;
        _playerInputActions.AttackTriggered      += OnAttackTriggered;
        _playerInputActions.AttackReleased += ReleaseAttack;
        _playerInputActions.StanceToggleTriggered += OnStanceToggleTriggered;
        _playerInputActions.GuardStarted += OnGuardStarted;
        _playerInputActions.GuardEnded += OnGuardEnded;
        _playerInputActions.LockTargetTriggered += OnLockTargetTriggered;
        _playerInputActions.TargetCycleTriggered += OnTargetCycleTriggered;
    }

    private void OnDisable()
    {
        TargetLock?.Clear();
        Defense?.ResetDefense();
        CancelHeldAttack(); HitFeedback?.ResetFeedback();
        if (IsCharging || StateMachine.CurrentState is HurtState or AttackState)
            StateMachine.ChangeState(new IdleState(this));
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
        _playerInputActions.AttackReleased -= ReleaseAttack;
        _playerInputActions.StanceToggleTriggered -= OnStanceToggleTriggered;
        _playerInputActions.GuardStarted -= OnGuardStarted;
        _playerInputActions.GuardEnded -= OnGuardEnded;
        _playerInputActions.LockTargetTriggered -= OnLockTargetTriggered;
        _playerInputActions.TargetCycleTriggered -= OnTargetCycleTriggered;
    }

    private void Start()
    {
        Motor.RefreshGrounded(Time.time);
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        TrackTutorialMovement();
        TargetLock?.Tick();
        Defense?.Tick();
        if (TargetLock != null && TargetLock.IsLocked && !IsRolling && StateMachine.CurrentState is not HurtState)
        {
            Vector3 direction = TargetLock.AimPoint - transform.position;
            if (direction.x * direction.x + direction.z * direction.z > .01f)
                yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 540f * Time.deltaTime);
        }
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
        // Hard lock owns both camera axes; do not accumulate invisible pitch that jumps on unlock.
        if (TargetLock == null || !TargetLock.IsLocked)
        {
            yaw += mouseX;
            pitch = Mathf.Clamp(pitch + (invertY ? mouseY : -mouseY), -35f, 60f);
        }
    }

    private void FixedUpdate()
    {
        Motor.RefreshGrounded(Time.time);
        if (!rb.isKinematic && StateMachine.CurrentState is not HurtState) rb.MoveRotation(FacingRotation);
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
        CancelHeldAttack();
        CombatAllowed = allowed;
        if (!allowed && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main" && progress != null && progress.Tutorial.Has(RPGSilent.Domain.TutorialStep.EnterDungeon))
            progress.RecordTutorialAction(RPGSilent.Domain.TutorialStep.ReturnTown);
        if (!allowed)
        {
            EquipmentEffects?.ResetCombat();
            ClassCombat?.ResetCombat();
            TargetLock?.Clear();
            Defense?.ResetDefense();
            skillController?.InterruptSkill(force: true);
            GetComponent<AttackExecutor>()?.ResetCombat();
            HitFeedback?.ResetFeedback();
            if (StateMachine.CurrentState is AttackState or ChargeAttackState or HurtState) ReturnToLocomotion();
        }
        if (Weapons != null) Weapons.SetCombatAllowed(allowed);
        else StanceController?.SetCombatAllowed(allowed);
    }

    // ── IDamageable ────────────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (Defense != null && Defense.IsInvulnerable) { Defense.Resolve(damage, transform.position, null); return; }
        ReceiveDamage(damage, true);
    }
    // Returns whether on-hit statuses/impact VFX may be applied. Avoided and guarded hits are consumed.
    public bool ReceiveAttack(RPGSilent.Combat.AttackContext attack, Vector3 source)
    {
        if (attack == null || !CanReceiveCombatEffects || attack.Damage <= 0) return false;
        DefenseOutcome outcome = Defense != null ? Defense.Resolve(attack.Damage, source, attack) : DefenseOutcome.Hit;
        if (outcome == DefenseOutcome.Dodged || outcome == DefenseOutcome.PerfectDodge || outcome == DefenseOutcome.Parried) return false;
        bool blocked = outcome == DefenseOutcome.Blocked;
        bool broken = outcome == DefenseOutcome.GuardBroken;
        ReceiveDamage(blocked ? DefenseRules.BlockDamage(attack.Damage) : attack.Damage, !blocked,
            source, broken ? AttackImpact.Heavy : attack.Profile.Impact, broken);
        return !blocked;
    }
    public void TakePeriodicDamage(int damage) => ReceiveDamage(damage, false);
    void ReceiveDamage(int damage, bool interrupt, Vector3? source = null, AttackImpact impact = AttackImpact.Light, bool guardBroken = false)
    {
        if (!CanReceiveCombatEffects || damage <= 0) return;

        int scaledDamage = _gameSettings?.ScaleIncomingDamage(damage) ?? damage;
        scaledDamage = Mathf.Max(1, Mathf.CeilToInt(scaledDamage * (Statuses?.State.IncomingMultiplier ?? 1f)));
        int before = _statsReader.CurrentHealth;
        if (Training != null) { if (before <= 1) return; scaledDamage = Mathf.Min(scaledDamage, before - 1); }
        _takeDamageUseCase.Execute(scaledDamage);
        CombatRecord.Receive(before - _statsReader.CurrentHealth);
        Vector3 origin = source ?? transform.position + transform.forward;
        // Block already has its own feedback. DOT shows damage without causing flinch/knockback.
        if (interrupt || !source.HasValue) HitFeedback?.Show(before - _statsReader.CurrentHealth, origin, impact, !interrupt);

        if (IsDead)
        {
            EquipmentEffects?.ResetCombat();
            ClearCombatInput(); CancelHeldAttack(); skillController?.InterruptSkill(force: true);
            TargetLock?.Clear(); Defense?.ResetDefense();
            StateMachine.ChangeState(new DeadState(this));
        }
        else if (interrupt && StateMachine.CurrentState is not HurtState && (guardBroken || HitFeedback == null || HitFeedback.CanReact)
            && (skillController == null || skillController.TryInterruptSkill(force: impact == AttackImpact.Heavy)))
        {
            ClearCombatInput(); CancelHeldAttack(); Weapons?.CancelTransition();
            Defense?.SuppressGuard();
            StateMachine.ChangeState(new HurtState(this, origin, impact, guardBroken));
        }
    }

    // ── IRewardable ────────────────────────────────────────────────────────────

    public void AddReward(int gold, int exp) => _addRewardUseCase.Execute(gold, exp);

    public void LockForDungeonResult()
    {
        if (combatLocked) return;
        EquipmentEffects?.ResetCombat();
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
        EquipmentEffects?.ResetCombat();
        ClassCombat?.ResetCombat();
        CombatRecord.Reset();
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
        tutorialPosition = position; tutorialPositionSet = true;
        CancelHeldAttack(); HitFeedback?.ResetFeedback();
        if (IsCharging || StateMachine.CurrentState is HurtState) ReturnToLocomotion();
        TargetLock?.Clear();
        Defense?.ResetDefense();
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

    private void OnGuardStarted()
    {
        if (Cursor.lockState == CursorLockMode.Locked) { ClearCombatInput(); CancelCharge(); Defense?.PressGuard(); }
    }
    private void OnGuardEnded() => Defense?.ReleaseGuard();
    private void OnLockTargetTriggered() => TargetLock?.Toggle();
    private void OnTargetCycleTriggered(int direction) => TargetLock?.Cycle(direction);

    private void OnAttackTriggered()
    {
        PressAttack();
    }

    private void OnRollTriggered()
    {
        if (!CanRoll || skillController != null && !skillController.TryInterruptSkill()) return;
        CancelHeldAttack();
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
        Defense?.SuppressGuard();
        CancelHeldAttack();
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
