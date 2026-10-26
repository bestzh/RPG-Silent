using RPGSilent.Domain;
using RPGSilent.Enemies;
using System.Collections;
using UnityEngine;

// 保留旧枚举值，避免已有资产/工具将状态值解释成其他状态。
public enum EnemyState { Idle, Chase, Attack, Hurt, Return, Dead, Retreat, PhaseTransition }

/// <summary>生命周期与动作状态；感知、导航、选招及命中交给独立能力。</summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    public EnemyDefinition Definition;
    public int MaxHealth = 60;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public EnemyState State { get; private set; }
    public bool HasAggro { get; private set; }
    public int RewardGold => rewardGold;
    public int RewardExp => rewardExp;
    public string DisplayName => Definition != null ? Definition.DisplayName : gameObject.name;
    public bool IsBoss => phases != null;
    public int Phase => phases != null ? phases.Phase : 1;
    public EnemySkillData CurrentSkill => skills?.Current;
    public bool ReturningHome { get; private set; }
    public Transform Target => perception?.Target;
    public float DamageMultiplier { get; private set; } = 1f;
    public event System.Action<EnemyController> Died;
    public event System.Action<EnemyController> Changed;

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

    public bool CanDecide => initialized && isActiveAndEnabled && !IsDead && !combatLocked &&
        State != EnemyState.Attack && State != EnemyState.Hurt && State != EnemyState.PhaseTransition;
    public int DecisionCount { get; private set; }

    public bool WantsRetreat => Definition != null && Definition.RetreatDistance > 0f &&
        perception.IsAlive && perception.Distance <
        (State == EnemyState.Retreat ? Definition.PreferredDistance - 0.5f : Definition.RetreatDistance);

    private void Awake()
    {
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
        motor.Initialize();
        perception.Refresh();
        initialized = true;
        nextAttack = Time.time + 0.8f;
        EnterState(EnemyState.Idle, true);
        StartCoroutine(StartBrain());
    }

    private void OnEnable()
    {
        if (!initialized || IsDead || combatLocked) return;
        executor.ResetCombat();
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
        StopAllCoroutines();
        if (brain != null && brain.IsActive()) brain.StopBehavior();
        skills?.Cancel();
        motor?.Stop();
        executor?.ResetCombat(true);
        phases?.ClearEffects();
        HasAggro = false;
        if (!IsDead) State = EnemyState.Idle;
    }

    private void OnDestroy()
    {
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

    public void LockForDungeonResult()
    {
        combatLocked = true;
        enabled = false;
    }

    private void Update()
    {
        if (IsDead || combatLocked) return;
        stateElapsed += Time.deltaTime;
        perception.Refresh();
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
            skills.Cancel();
            executor.ResetCombat();
            HasAggro = false;
            ReturningHome = true;
            EnterState(EnemyState.Idle);
        }
        if (State == EnemyState.Attack && !ReturningHome)
        {
            if (skills.Tick(Time.deltaTime) && !IsDead && !combatLocked && isActiveAndEnabled)
            { nextAttack = Time.time + 0.35f; EnterState(EnemyState.Idle); }
            return;
        }
    }

    /// <summary>行为树的节拍节点调用；命令执行后重新等待，避免 Repeater 在同一帧空转。</summary>
    public bool TryBeginDecision()
    {
        if (!CanDecide || Time.time < nextDecision) return false;
        nextDecision = Time.time + 0.15f;
        if (!HasAggro && !ReturningHome && perception.Distance <= detectionDistance && perception.HasLineOfSight()) HasAggro = true;
        DecisionCount++;
        return true;
    }

    public bool TryChangeBossPhase()
    {
        if (!CanDecide || ReturningHome || phases == null || !phases.TryAdvance((float)CurrentHealth / MaxHealth)) return false;
        skills.Cancel();
        EnterState(EnemyState.PhaseTransition);
        Changed?.Invoke(this);
        return true;
    }

    private void FixedUpdate() => motor?.FixedTick(!IsDead &&
        (State == EnemyState.Chase || State == EnemyState.Return || State == EnemyState.Retreat));

    public EnemyActionResult DecideReturn()
    {
        if (Vector3.Distance(transform.position, motor.Home) <= homeTolerance + 0.1f)
        {
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
        if (!perception.IsAlive || Time.time < nextAttack || !perception.HasLineOfSight()) return EnemyActionResult.Failure;
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
        motor.Face(perception.Target.position - transform.position, true);
        skills.Begin(selected, perception.Target, DamageMultiplier);
        EnterState(EnemyState.Attack, true);
        return EnemyActionResult.Running;
    }

    private bool CanSelect(EnemySkillData skill) => skills.IsReady(skill) && skill.RequiredPhase <= Phase &&
        perception.Distance >= skill.MinRange && perception.Distance <= skill.MaxRange;

    public EnemyActionResult DecideApproach()
    {
        if (!perception.IsAlive) return DecideIdle();
        float range = Definition != null ? Definition.PreferredDistance : attackDistance;
        if (perception.Distance > range + 0.15f || !perception.HasLineOfSight())
        {
            EnterState(EnemyState.Chase);
            if (!motor.MoveTo(perception.Target.position, range)) { ReturningHome = true; HasAggro = false; }
        }
        else { EnterState(EnemyState.Idle); motor.Face(perception.Target.position - transform.position); }
        return EnemyActionResult.Running;
    }

    public EnemyActionResult DecideIdle() { EnterState(EnemyState.Idle); return EnemyActionResult.Success; }

    private void EnterState(EnemyState state, bool restart = false)
    {
        if (State == state && !restart) return;
        State = state;
        stateElapsed = 0f;
        bool moving = state == EnemyState.Chase || state == EnemyState.Return || state == EnemyState.Retreat;
        if (!moving) motor.Stop();
        string animation = moving ? "Chase" : state == EnemyState.Attack ? skills.Current.AnimationState :
            state == EnemyState.PhaseTransition ? phases.TransitionAnimation : state.ToString();
        int hash = Animator.StringToHash("Base Layer." + animation);
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, 0.08f, 0, 0f);
    }

    public void TakeDamage(int amount)
    {
        if (combatLocked || IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EnemySkillRunner.SpawnEffect(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity, effectLifetime);
        if (CurrentHealth == 0) { Die(); return; }
        HasAggro = perception.IsAlive;
        lastDamageTime = Time.time;
        staggerDamage += amount;
        bool armor = State == EnemyState.PhaseTransition || (skills.Current != null && skills.Current.SuperArmor);
        int threshold = Definition != null ? Definition.StaggerThreshold : 0;
        if (!armor && staggerDamage >= threshold)
        {
            staggerDamage = 0;
            skills.Cancel();
            EnterState(EnemyState.Hurt, true);
            nextAttack = Time.time + 0.6f;
            if (perception.Target != null) motor.Knockback(perception.Target.position, knockbackDistance);
        }
        Changed?.Invoke(this);
    }

    public void Die()
    {
        if (combatLocked || IsDead) return;
        CurrentHealth = 0;
        IsDead = true;
        StopAllCoroutines();
        if (brain != null && brain.IsActive()) brain.StopBehavior();
        HasAggro = false;
        skills.Cancel();
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
