using RPGSilent.Domain;
using RPGSilent.Enemies;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 保留旧枚举值，避免已有资产/工具将状态值解释成其他状态。
public enum EnemyState { Idle, Chase, Attack, Hurt, Return, Dead, Retreat, PhaseTransition, Recover, Reposition }

/// <summary>生命周期与动作状态；感知、导航、选招及命中交给独立能力。</summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    static readonly HashSet<EnemyController> activeEnemies = new();
    public static IEnumerable<EnemyController> ActiveEnemies => activeEnemies;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActiveEnemies() => activeEnemies.Clear();
    public EnemyDefinition Definition;
    public int MaxHealth = 60;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public EnemyState State { get; private set; }
    public bool HasAggro { get; private set; }
    public int RewardGold => rewardGold;
    public int RewardExp => rewardExp;
    public string DisplayName => (IsCompanion ? "兽灵 · " : IsElite ? "精英 · " : "") + (Definition != null ? Definition.DisplayName : gameObject.name);
    public PlayerController CompanionOwner { get; private set; }
    public bool IsCompanion { get; private set; }
    public bool IsElite { get; private set; }
    public bool IsTrainingTarget { get; private set; }
    public bool TrainingAttacks { get; private set; }
    float companionExpires;
    public bool IsBoss => phases != null;
    public int Phase => phases != null ? phases.Phase : 1;
    public string PhaseName => phases != null ? phases.PhaseName : "";
    public string PhaseHint => State == EnemyState.PhaseTransition ? "正在转阶段 · 拉开距离" :
        IsVulnerable ? "破绽出现 · 现在反击" : CurrentSkill != null ? CurrentSkill.DisplayName : phases != null ? phases.Hint : "";
    public EnemySkillData CurrentSkill => skills?.Current;
    public bool ReturningHome { get; private set; }
    public Transform Target => perception?.Target;
    public float DamageMultiplier { get; private set; } = 1f;
    public EnemyCombatGroup CombatGroup { get; private set; }
    public bool HasAttackPermit => CombatGroup != null && CombatGroup.Budget.HasPermit(this);
    public bool HasTacticalPosition { get; private set; }
    public Vector3 TacticalPosition { get; private set; }
    public float LeashRadius => leashDistance;
    public EnemyAttackSequence ActiveSequence { get; private set; }
    public int SequenceStep => sequenceStep + 1;
    public bool IsVulnerable => IsBoss && State == EnemyState.Recover && ActiveSequence == null;
    public string CombatHint => State == EnemyState.PhaseTransition ? "阶段变化" : IsVulnerable ? "反击机会" : ActiveSequence != null ?
        $"连招 {SequenceStep}/{ActiveSequence.Steps.Length}" : string.Empty;
    public event System.Action<EnemyController> Died;
    public event System.Action<EnemyController> Changed;
    public event System.Action<EnemyHit> Damaged;

    [Header("旧预制体默认战斗配置")]
    public float moveSpeed = 3f;
    public float turnSpeed = 8f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public int damage = 10;
    [Range(0f, 1f)] public float attackHitNormalizedTime = 0.2345f;
    public float attackDuration = 2.166667f;
    public float attackHalfAngle = 60f;
    public float detectionDistance = 12f;
    public float leashDistance = 20f;
    public float homeTolerance = 0.3f;
    [SerializeField] private int rewardGold = 10;
    [SerializeField] private int rewardExp = 25;
    [SerializeField] private bool useNavMeshAgent = true;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float hitStopDuration = 0.15f;
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float effectLifetime = 3f;

    private Animator animator;
    private EnemyMotor motor;
    private EnemyPerception perception;
    private EnemySkillRunner skills;
    private Opsive.BehaviorDesigner.Runtime.BehaviorTree brain;
    private AttackExecutor executor;
    private BossPhaseController phases;
    private EnemySkillData fallbackSkill;
    private AttackProfile fallbackProfile;
    private EnemySkillData[] availableSkills;
    private float stateElapsed, nextDecision, nextAttack, lastDamageTime;
    private int staggerDamage;
    private bool initialized, dungeonConfigured, combatLocked;
    public bool CombatLocked => combatLocked;
    public CombatStatusController Statuses { get; private set; }
    private readonly Dictionary<EnemyAttackSequence, float> sequenceReadyAt = new();
    private int sequenceStep;
    private float recoveryDuration;

    // Following owns locomotion while there is no usable combat target; the enemy tree must not reset its path to idle.
    bool ShouldFollowOwner => IsCompanion && CompanionOwner != null &&
        (!perception.IsAlive || Vector3.Distance(transform.position, CompanionOwner.transform.position) > 18f);
    public bool CanDecide => !ShouldFollowOwner && (!IsTrainingTarget || TrainingAttacks) && initialized && isActiveAndEnabled && !IsDead && !combatLocked &&
        State != EnemyState.Attack && State != EnemyState.Hurt && State != EnemyState.PhaseTransition && State != EnemyState.Recover;
    public int DecisionCount { get; private set; }

    public bool WantsRetreat => Definition != null && Definition.RetreatDistance > 0f &&
        perception.IsAlive && perception.Distance <
        (State == EnemyState.Retreat ? Definition.PreferredDistance - 0.5f : Definition.RetreatDistance);

    private void Awake()
    {
        Statuses = GetComponent<CombatStatusController>() ?? gameObject.AddComponent<CombatStatusController>();
        if (Definition != null)
        {
            MaxHealth = Definition.Health;
            moveSpeed = Definition.MoveSpeed;
            detectionDistance = Definition.DetectionDistance;
            leashDistance = Definition.LeashDistance;
            rewardGold = Definition.RewardGold;
            rewardExp = Definition.RewardExp;
            hitEffectPrefab = Definition.HitEffect;
            deathEffectPrefab = Definition.DeathEffect;
        }
        CurrentHealth = MaxHealth;
        animator = GetComponent<Animator>();
        phases = GetComponent<BossPhaseController>();
        executor = GetComponent<AttackExecutor>();
        if (executor == null) executor = gameObject.AddComponent<AttackExecutor>();
        executor.ConfigureEnemyTargeting();
        var faction = GetComponent<CombatFaction>();
        if (faction == null) faction = gameObject.AddComponent<CombatFaction>();
        faction.Team = CombatTeam.Enemy;
        motor = new EnemyMotor(gameObject, useNavMeshAgent, moveSpeed, turnSpeed);
        perception = new EnemyPerception(transform);
        skills = new EnemySkillRunner(transform, executor);
        availableSkills = Definition != null ? Definition.Skills : CreateLegacySkill();
        brain = GetComponent<Opsive.BehaviorDesigner.Runtime.BehaviorTree>();
        if (brain != null)
        {
            brain.StartWhenEnabled = false;
            brain.PauseWhenDisabled = false;
        }
    }

    private EnemySkillData[] CreateLegacySkill()
    {
        fallbackProfile = ScriptableObject.CreateInstance<AttackProfile>();
        fallbackProfile.Damage = damage;
        fallbackProfile.Range = 0f;
        fallbackProfile.Radius = attackDistance;
        fallbackProfile.Angle = attackHalfAngle * 2f;
        fallbackSkill = ScriptableObject.CreateInstance<EnemySkillData>();
        fallbackSkill.DisplayName = "近战攻击";
        fallbackSkill.Attack = fallbackProfile;
        fallbackSkill.Duration = attackDuration;
        fallbackSkill.ReleaseTime = attackHitNormalizedTime;
        fallbackSkill.Cooldown = attackCooldown;
        fallbackSkill.MaxRange = attackDistance;
        return new[] { fallbackSkill };
    }

    private void Start()
    {
        if (IsCompanion || IsElite) gameObject.AddComponent<EnemyAllegianceLabel>();
        motor.Initialize();
        perception.Refresh();
        initialized = true;
        CombatGroup?.Register(this);
        nextAttack = Time.time + 0.8f;
        EnterState(EnemyState.Idle, true);
        StartCoroutine(StartBrain());
    }

    private void OnEnable()
    {
        activeEnemies.Add(this);
        if (!initialized || IsDead || combatLocked) return;
        executor.ResetCombat();
        CombatGroup?.Register(this);
        phases?.RestoreEffects();
        EnterState(EnemyState.Idle, true);
        StartCoroutine(StartBrain());
    }

    private IEnumerator StartBrain()
    {
        // 下一帧再创建 Pro 执行实体；不依赖 Game View 渲染，切到行为树窗口也能启动。
        yield return null;
        if (!isActiveAndEnabled || IsDead || combatLocked) yield break;
        if (brain == null || brain.Subgraph == null)
        {
            Debug.LogError("敌人缺少 Behavior Designer Pro 行为树资源。", this);
            yield break;
        }
        if (!brain.IsActive()) brain.StartBehavior();
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
        StopAllCoroutines();
        if (brain != null && brain.IsActive()) brain.StopBehavior();
        CancelAction();
        CombatGroup?.Remove(this);
        HasTacticalPosition = false;
        motor?.Stop();
        executor?.ResetCombat(true);
        phases?.ClearEffects();
        HasAggro = false;
        if (!IsDead) State = EnemyState.Idle;
    }

    private void OnDestroy()
    {
        activeEnemies.Remove(this);
        if (fallbackSkill != null) Destroy(fallbackSkill);
        if (fallbackProfile != null) Destroy(fallbackProfile);
    }

    public void ConfigureDungeon(DungeonDatabase.DifficultyTier tier)
    {
        if (dungeonConfigured || IsDead || tier == null) return;
        dungeonConfigured = true;
        MaxHealth = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * Mathf.Max(0.1f, tier.EnemyHealthMultiplier)));
        CurrentHealth = MaxHealth;
        DamageMultiplier = Mathf.Max(0.1f, tier.EnemyDamageMultiplier);
        rewardGold = Mathf.Max(0, Mathf.RoundToInt(rewardGold * tier.RewardMultiplier));
        rewardExp = Mathf.Max(0, Mathf.RoundToInt(rewardExp * tier.RewardMultiplier));
    }

    public void ConfigureCompanion(PlayerController owner, float duration, float multiplier)
    {
        IsCompanion = true; CompanionOwner = owner; companionExpires = Time.time + duration;
        dungeonConfigured = true; rewardGold = rewardExp = 0;
        DamageMultiplier = Mathf.Max(.1f, multiplier); MaxHealth *= 2; CurrentHealth = MaxHealth;
        GetComponent<CombatFaction>().Team = CombatTeam.Player;
        executor.ConfigureCompanionTargeting(); perception.ConfigureCompanion(owner);
        BindCombatGroup(null);
    }
    public void ConfigureElite()
    {
        if (IsElite) return;
        IsElite = true; MaxHealth *= 2; CurrentHealth = MaxHealth;
        DamageMultiplier *= 1.25f; rewardGold *= 2; rewardExp *= 2;
        transform.localScale *= 1.15f;
    }
    public void ConfigureTraining(bool attacks)
    {
        IsTrainingTarget = true; TrainingAttacks = attacks; dungeonConfigured = true;
        rewardGold = rewardExp = 0; MaxHealth = CurrentHealth = 1000000;
        DamageMultiplier = .25f;
    }

    public void BindCombatGroup(EnemyCombatGroup group)
    {
        CombatGroup?.Remove(this);
        CombatGroup = group;
        CombatGroup?.Register(this);
    }

    public void AcceptSharedAggro(Transform target)
    {
        if (!CanDecide || ReturningHome || HasAggro || !perception.IsAlive || perception.Target != target ||
            Vector3.Distance(target.position, motor.Home) > leashDistance) return;
        HasAggro = true;
    }

    public void LockForDungeonResult()
    {
        Statuses?.State.Clear();
        combatLocked = true;
        enabled = false;
    }

    private void Update()
    {
        if (IsDead || combatLocked) return;
        stateElapsed += Time.deltaTime;
        if (IsCompanion)
        {
            if (CompanionOwner == null || !CompanionOwner.CanReceiveCombatEffects || Time.time >= companionExpires)
            { Destroy(gameObject); return; }
            motor.SetHome(CompanionOwner.transform.position);
        }
        perception.Refresh();
        if (ShouldFollowOwner)
        {
            CancelAction(); HasAggro = false; ReturningHome = false;
            if (Vector3.Distance(transform.position, motor.Home) > 2.5f) { EnterState(EnemyState.Chase); motor.MoveTo(motor.Home, 2); }
            else EnterState(EnemyState.Idle);
            return;
        }
        if (Time.time - lastDamageTime > 3f) staggerDamage = 0;
        if (State == EnemyState.PhaseTransition)
        {
            if (stateElapsed >= phases.TransitionDuration) EnterState(EnemyState.Idle);
            return;
        }
        if (State == EnemyState.Hurt)
        {
            if (stateElapsed >= Mathf.Max(hitStopDuration, 0.45f)) EnterState(EnemyState.Idle);
            return;
        }
        if (HasAggro && (!perception.IsAlive || Vector3.Distance(transform.position, motor.Home) > leashDistance ||
            Vector3.Distance(perception.Target.position, motor.Home) > leashDistance))
        {
            CancelAction();
            executor.ResetCombat();
            HasAggro = false;
            ReturningHome = true;
            HasTacticalPosition = false;
            EnterState(EnemyState.Idle);
        }
        if (State == EnemyState.Attack && !ReturningHome)
        {
            var finished = skills.Current;
            if (skills.Tick(Time.deltaTime) && !IsDead && !combatLocked && isActiveAndEnabled)
            {
                if (ActiveSequence != null && sequenceStep + 1 < ActiveSequence.Steps.Length)
                {
                    sequenceStep++;
                    BeginRecovery(ActiveSequence.BetweenAttacks);
                }
                else FinishSequence(finished != null ? finished.RecoveryDuration : 0.35f);
            }
            return;
        }
        if (State == EnemyState.Recover)
        {
            if (ActiveSequence != null)
            {
                var next = ActiveSequence.Steps[sequenceStep];
                if (!InSkillRange(next) || !perception.HasLineOfSight()) { FinishSequence(); return; }
                motor.Face(perception.Target.position - transform.position);
                if (stateElapsed < recoveryDuration) return;
                if (FacingTarget()) BeginSkill(next);
                else if (stateElapsed >= recoveryDuration + 0.5f) FinishSequence();
            }
            else if (stateElapsed >= recoveryDuration)
            {
                CombatGroup?.ReleaseAttack(this);
                EnterState(EnemyState.Idle);
            }
            return;
        }
        if (State == EnemyState.Idle && HasAggro && perception.IsAlive)
            motor.Face(perception.Target.position - transform.position);
    }

    private void BeginSkill(EnemySkillData skill)
    {
        skills.Begin(skill, perception.Target, DamageMultiplier * (phases != null ? phases.DamageMultiplier : 1f));
        EnterState(EnemyState.Attack, true);
    }

    private void BeginRecovery(float duration)
    {
        recoveryDuration = Mathf.Max(0f, duration);
        nextAttack = Mathf.Max(nextAttack, Time.time + recoveryDuration);
        EnterState(EnemyState.Recover, true);
    }

    private void FinishSequence(float defaultRecovery = 0.35f)
    {
        float duration = ActiveSequence != null ? ActiveSequence.RecoveryDuration : defaultRecovery;
        ActiveSequence = null;
        sequenceStep = 0;
        BeginRecovery(duration);
    }

    private void CancelAction()
    {
        skills?.Cancel();
        CombatGroup?.ReleaseAttack(this);
        ActiveSequence = null;
        sequenceStep = 0;
    }

    private bool FacingTarget()
    {
        if (!perception.IsAlive) return false;
        Vector3 delta = perception.Target.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, delta) <=
            (Definition != null ? Definition.AttackFacingAngle : 25f);
    }

    public bool TryBeginSequence()
    {
        if (!CanDecide || !HasAggro || ReturningHome || Definition == null || Definition.AttackSequences == null ||
            Time.time < nextAttack || !FacingTarget() || !perception.HasLineOfSight()) return false;
        foreach (var sequence in Definition.AttackSequences)
        {
            if (sequence == null || sequence.RequiredPhase > Phase || sequence.Steps == null || sequence.Steps.Length == 0 ||
                (sequenceReadyAt.TryGetValue(sequence, out float readyAt) && Time.time < readyAt)) continue;
            bool valid = true;
            foreach (var step in sequence.Steps) if (step == null || step.Attack == null || step.RequiredPhase > Phase) { valid = false; break; }
            if (!valid || !CanSelect(sequence.Steps[0])) continue;
            if (CombatGroup != null && !CombatGroup.TryAttack(this)) return false;
            sequenceReadyAt[sequence] = Time.time + Mathf.Max(sequence.Cooldown, 0.1f);
            ActiveSequence = sequence;
            sequenceStep = 0;
            BeginSkill(sequence.Steps[0]);
            return true;
        }
        return false;
    }

    /// <summary>行为树的节拍节点调用；命令执行后重新等待，避免 Repeater 在同一帧空转。</summary>
    public bool TryBeginDecision()
    {
        if (!CanDecide || Time.time < nextDecision) return false;
        nextDecision = Time.time + 0.15f;
        if (!HasAggro && !ReturningHome && perception.Distance <= detectionDistance && perception.HasLineOfSight()) HasAggro = true;
        if (HasAggro && !ReturningHome) CombatGroup?.AlertAllies(this);
        DecisionCount++;
        return true;
    }

    public bool TryChangeBossPhase()
    {
        if (!CanDecide || ReturningHome || phases == null || !phases.TryAdvance((float)CurrentHealth / MaxHealth)) return false;
        CancelAction();
        executor.ResetCombat(); // Old phase projectiles and persistent areas cannot survive the transition.
        EnterState(EnemyState.PhaseTransition);
        Changed?.Invoke(this);
        return true;
    }

    private void FixedUpdate() => motor?.FixedTick(!IsDead &&
        (State == EnemyState.Chase || State == EnemyState.Return || State == EnemyState.Retreat || State == EnemyState.Reposition));

    public EnemyActionResult DecideReturn()
    {
        if (Vector3.Distance(transform.position, motor.Home) <= homeTolerance + 0.1f)
        {
            if (ReturningHome && phases != null)
            {
                phases.ResetEncounter(); CurrentHealth = MaxHealth; staggerDamage = 0;
                Statuses?.State.Clear(); skills.ResetCooldowns(); sequenceReadyAt.Clear(); executor.ResetCombat();
                Changed?.Invoke(this);
            }
            ReturningHome = false;
            EnterState(EnemyState.Idle);
            return EnemyActionResult.Success;
        }
        EnterState(EnemyState.Return);
        if (!motor.MoveTo(motor.Home, homeTolerance)) motor.Stop();
        return EnemyActionResult.Running;
    }

    public EnemyActionResult DecideRetreat()
    {
        if (!motor.Retreat(perception.Target.position, Definition.PreferredDistance)) return EnemyActionResult.Failure;
        EnterState(EnemyState.Retreat);
        return EnemyActionResult.Running;
    }

    public EnemyActionResult DecideAttack()
    {
        if (!CanDecide || !perception.IsAlive || Time.time < nextAttack || !FacingTarget() || !perception.HasLineOfSight()) return EnemyActionResult.Failure;
        if (CombatGroup != null && Definition != null && Definition.Behavior == EnemyBehaviorStyle.Ranged && !perception.HasClearShot()) return EnemyActionResult.Failure;
        float total = 0f;
        if (availableSkills == null) return EnemyActionResult.Failure;
        foreach (var skill in availableSkills) if (CanSelect(skill)) total += skill.Weight;
        if (total <= 0f) return EnemyActionResult.Failure;
        float roll = Random.value * total;
        EnemySkillData selected = null;
        foreach (var skill in availableSkills)
        {
            if (!CanSelect(skill)) continue;
            selected = skill;
            roll -= skill.Weight;
            if (roll <= 0f) break;
        }
        if (CombatGroup != null && !CombatGroup.TryAttack(this)) return EnemyActionResult.Failure;
        BeginSkill(selected);
        return EnemyActionResult.Running;
    }

    private bool CanSelect(EnemySkillData skill) => skills.IsReady(skill) && InSkillRange(skill);

    private bool InSkillRange(EnemySkillData skill) => skill != null && skill.Attack != null && skill.RequiredPhase <= Phase &&
        perception.IsAlive && perception.Distance >= skill.MinRange && perception.Distance <= skill.MaxRange;

    public EnemyActionResult DecideApproach()
    {
        if (!perception.IsAlive) return DecideIdle();
        float range = Definition != null ? Definition.PreferredDistance : attackDistance;
        float margin = State == EnemyState.Chase ? 0.1f : (Definition != null ? Definition.ApproachHysteresis : 0.5f);
        if (perception.Distance > range + margin || !perception.HasLineOfSight())
        {
            EnterState(EnemyState.Chase);
            if (!motor.MoveTo(perception.Target.position, range)) { ReturningHome = true; HasAggro = false; }
        }
        else { EnterState(EnemyState.Idle); motor.Face(perception.Target.position - transform.position); }
        return EnemyActionResult.Running;
    }

    public EnemyActionResult DecideGroupPosition()
    {
        HasTacticalPosition = false;
        if (!CanDecide || CombatGroup == null || Definition == null || IsBoss || !perception.IsAlive) return EnemyActionResult.Failure;
        bool ranged = Definition.Behavior == EnemyBehaviorStyle.Ranged;
        if (ranged && perception.Distance <= Definition.PreferredDistance + Definition.ApproachHysteresis && perception.HasClearShot())
            return EnemyActionResult.Failure;
        if (!CombatGroup.TryGetPosition(this, motor, perception, Definition.PreferredDistance, ranged, out var point))
            return EnemyActionResult.Failure; // 窄路或缺少完整路径时沿用原追击，仍需申请攻击名额。
        TacticalPosition = point;
        HasTacticalPosition = true;
        Vector3 gap = point - transform.position; gap.y = 0f;
        if (gap.magnitude <= CombatGroup.PositionTolerance)
        {
            EnterState(EnemyState.Idle);
            motor.Face(perception.Target.position - transform.position);
            return EnemyActionResult.Failure;
        }
        EnterState(EnemyState.Reposition);
        if (!motor.MoveTo(point, 0.25f)) { HasTacticalPosition = false; return EnemyActionResult.Failure; }
        return EnemyActionResult.Running;
    }

    public EnemyActionResult DecideIdle() { EnterState(EnemyState.Idle); return EnemyActionResult.Success; }

    private void EnterState(EnemyState state, bool restart = false)
    {
        if (State == state && !restart) return;
        State = state;
        stateElapsed = 0f;
        bool moving = state == EnemyState.Chase || state == EnemyState.Return || state == EnemyState.Retreat || state == EnemyState.Reposition;
        if (!moving) motor.Stop();
        string animation = moving ? "Chase" : state == EnemyState.Attack ? skills.Current.AnimationState :
            state == EnemyState.PhaseTransition ? phases.TransitionAnimation : state == EnemyState.Recover ? "Idle" : state.ToString();
        int hash = Animator.StringToHash("Base Layer." + animation);
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, 0.08f, 0, 0f);
        Changed?.Invoke(this);
    }

    public void TakeDamage(int amount) => ReceiveDamage(amount, perception != null && perception.Target != null ? perception.Target.position : transform.position - transform.forward, false);
    public void TakePeriodicDamage(int amount) => ReceiveDamage(amount, transform.position, true);
    public void ReceiveHit(int amount, Vector3 source) => ReceiveDamage(amount, source, false);
    public void ReceiveHit(int amount, Vector3 source, float staggerMultiplier) => ReceiveDamage(amount, source, false, staggerMultiplier);

    void ReceiveDamage(int amount, Vector3 source, bool periodic, float staggerMultiplier = 1f)
    {
        if (combatLocked || IsDead || amount <= 0) return;
        bool vulnerable = IsVulnerable;
        if (vulnerable && Definition != null) amount = Mathf.RoundToInt(amount * Definition.RecoveryDamageMultiplier);
        amount = Mathf.Min(CurrentHealth, amount);
        CurrentHealth = Mathf.Max(IsTrainingTarget ? 1 : 0, CurrentHealth - amount);
        if (!periodic) EnemySkillRunner.SpawnEffect(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity, effectLifetime);
        if (CurrentHealth == 0) { Die(); Damaged?.Invoke(new EnemyHit(amount, false, false, vulnerable, true, periodic)); return; }
        HasAggro = perception.IsAlive;
        lastDamageTime = Time.time;
        if (!periodic) staggerDamage += Mathf.RoundToInt(amount * Mathf.Max(0, staggerMultiplier));
        bool armor = State == EnemyState.PhaseTransition || (skills.Current != null && skills.Current.SuperArmor);
        int threshold = Definition != null ? Definition.StaggerThreshold : 0;
        bool staggered = !periodic && !armor && staggerDamage >= threshold;
        if (staggered)
        {
            staggerDamage = 0;
            CancelAction();
            EnterState(EnemyState.Hurt, true);
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.6f);
            motor.Knockback(source, knockbackDistance);
        }
        Damaged?.Invoke(new EnemyHit(amount, staggered, armor, vulnerable, false, periodic));
        Changed?.Invoke(this);
    }

    // Parry disrupts ordinary melee attackers, preserving boss phases and armored skills.
    public bool ReceiveParry()
    {
        if (combatLocked || IsDead || IsBoss || State == EnemyState.PhaseTransition || skills.Current != null && skills.Current.SuperArmor) return false;
        CancelAction(); staggerDamage = 0;
        EnterState(EnemyState.Hurt, true);
        nextAttack = Mathf.Max(nextAttack, Time.time + .6f);
        Changed?.Invoke(this); return true;
    }

    public void Die()
    {
        if (combatLocked || IsDead || IsTrainingTarget) return;
        Statuses?.State.Clear();
        CurrentHealth = 0;
        IsDead = true;
        StopAllCoroutines();
        if (brain != null && brain.IsActive()) brain.StopBehavior();
        HasAggro = false;
        CancelAction();
        CombatGroup?.Remove(this);
        HasTacticalPosition = false;
        executor.ResetCombat(true);
        phases?.ClearEffects();
        EnterState(EnemyState.Dead, true);
        if (!dungeonConfigured) perception.Rewardable?.AddReward(rewardGold, rewardExp);
        var body = GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        EnemySkillRunner.SpawnEffect(deathEffectPrefab, transform.position + Vector3.up, Quaternion.identity, effectLifetime);
        if (destroyDelay >= 0f) Destroy(gameObject, destroyDelay);
        Changed?.Invoke(this);
        Died?.Invoke(this);
    }
}
