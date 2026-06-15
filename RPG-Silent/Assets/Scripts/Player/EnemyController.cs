using System.Collections;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人控制器，实现 IDamageable 接口，通过 IDamageable / IRewardable 与玩家交互，不再直接依赖 PlayerController。
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("生命值")]
    public int MaxHealth = 60;
    public int  CurrentHealth { get; private set; }
    public bool IsDead        { get; private set; }

    [Header("战斗")]
    public float moveSpeed     = 3f;
    public float turnSpeed     = 8f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public int   damage         = 10;

    [Header("奖励")]
    [SerializeField] private int rewardGold = 10;
    [SerializeField] private int rewardExp  = 25;

    [Header("导航")]
    [SerializeField] private bool useNavMeshAgent = true;

    [Header("反馈")]
    [SerializeField] private float       destroyDelay      = 2f;
    [SerializeField] private float       hitStopDuration   = 0.15f;
    [SerializeField] private float       knockbackDistance = 0.25f;
    [SerializeField] private GameObject  hitEffectPrefab;
    [SerializeField] private GameObject  deathEffectPrefab;

    // 通过接口与玩家交互，不依赖 PlayerController 具体类
    private IDamageable  _targetDamageable;
    private IRewardable  _targetRewardable;
    private Transform    _targetTransform;

    private float        attackTimer;
    private Animator     animator;
    private Rigidbody    rb;
    private NavMeshAgent agent;
    private Coroutine    hurtRoutine;
    private Vector3      desiredMoveDirection;
    private bool         warnedMissingNavMesh;

    private bool CanUseAgent => useNavMeshAgent
        && agent != null
        && agent.enabled
        && agent.isOnNavMesh;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        attackTimer   = attackCooldown;
        animator      = GetComponent<Animator>();
        rb            = GetComponent<Rigidbody>();
        agent         = GetComponent<NavMeshAgent>();

        ConfigureAgent();
        ConfigureRigidbody();
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[EnemyController] 未找到 Player 对象。", this);
            return;
        }

        _targetTransform  = playerObj.transform;
        _targetDamageable = playerObj.GetComponent<IDamageable>();
        _targetRewardable = playerObj.GetComponent<IRewardable>();
    }

    private void Update()
    {
        if (IsDead || _targetTransform == null) return;

        Vector3 direction = _targetTransform.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude > attackDistance)
        {
            desiredMoveDirection = direction.normalized;

            if (CanUseAgent)
            {
                agent.isStopped = false;
                agent.SetDestination(_targetTransform.position);
            }
            else
            {
                WarnIfAgentIsMissingNavMesh();
                RotateTowards(desiredMoveDirection, Time.deltaTime);
            }
        }
        else
        {
            desiredMoveDirection = Vector3.zero;
            StopAgent();
            RotateTowards(direction, Time.deltaTime);

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
    }

    private void FixedUpdate()
    {
        if (CanUseAgent) return;

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity           = rb.linearVelocity;
            Vector3 horizontalVelocity = IsDead ? Vector3.zero : desiredMoveDirection * moveSpeed;
            rb.linearVelocity  = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
            rb.angularVelocity = Vector3.zero;
            return;
        }

        if (IsDead || desiredMoveDirection.sqrMagnitude <= 0.001f) return;
        transform.position += desiredMoveDirection * moveSpeed * Time.fixedDeltaTime;
    }

    // ── IDamageable 实现 ───────────────────────────────────────────────────────

    public void TakeDamage(int dmg)
    {
        if (IsDead || dmg <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - dmg);
        Debug.Log($"[Enemy] 受伤 -{dmg} HP。当前: {CurrentHealth}/{MaxHealth}", this);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity);

        if (CurrentHealth <= 0) { Die(); return; }

        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtFeedback());
    }

    public void Die()
    {
        if (IsDead) return;

        IsDead      = true;
        attackTimer = attackCooldown;
        StopAgent();
        GrantReward();

        if (hurtRoutine != null) { StopCoroutine(hurtRoutine); hurtRoutine = null; }

        animator?.SetTrigger("Die");

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position + Vector3.up, Quaternion.identity);

        Debug.Log("[Enemy] 死亡。", this);

        if (destroyDelay >= 0f) Destroy(gameObject, destroyDelay);
    }

    // ── 内部逻辑 ───────────────────────────────────────────────────────────────

    private void Attack()
    {
        if (IsDead) return;
        Debug.Log($"[Enemy] 攻击玩家，伤害 {damage}。", this);
        _targetDamageable?.TakeDamage(damage);
    }

    private void GrantReward()
    {
        _targetRewardable?.AddReward(rewardGold, rewardExp);
    }

    private IEnumerator HurtFeedback()
    {
        animator?.SetTrigger("Hit");

        if (_targetTransform != null && knockbackDistance > 0f)
        {
            Vector3 dir = transform.position - _targetTransform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Vector3 knockback = dir.normalized * knockbackDistance;
                if (CanUseAgent)
                    agent.Warp(transform.position + knockback);
                else if (rb != null && !rb.isKinematic)
                    rb.linearVelocity = new Vector3(
                        knockback.x / Time.fixedDeltaTime,
                        rb.linearVelocity.y,
                        knockback.z / Time.fixedDeltaTime);
                else
                    transform.position += knockback;
            }
        }

        float originalSpeed = moveSpeed;
        moveSpeed = 0f;
        if (CanUseAgent) agent.speed = 0f;

        yield return new WaitForSeconds(hitStopDuration);

        moveSpeed = originalSpeed;
        if (CanUseAgent) agent.speed = moveSpeed;
        hurtRoutine = null;
    }

    private void RotateTowards(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        Quaternion to = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, to, deltaTime * turnSpeed);
    }

    private void ConfigureRigidbody()
    {
        if (rb == null) return;

        if (useNavMeshAgent && agent != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        rb.isKinematic         = false;
        rb.useGravity          = true;
        rb.interpolation       = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints         &= ~RigidbodyConstraints.FreezePositionY;
        rb.constraints         |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.angularVelocity     = Vector3.zero;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger)
                Debug.LogWarning($"[Enemy] {col.name} 是 Trigger Collider，敌人至少需要一个非 Trigger Collider。", col);
        }
    }

    private void ConfigureAgent()
    {
        if (agent == null) return;
        agent.speed           = moveSpeed;
        agent.angularSpeed    = turnSpeed * 60f;
        agent.stoppingDistance = attackDistance;
        agent.updateRotation  = true;
        agent.updatePosition  = true;
        agent.autoBraking     = true;
    }

    private void StopAgent()
    {
        if (!CanUseAgent) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void WarnIfAgentIsMissingNavMesh()
    {
        if (!useNavMeshAgent || agent == null || warnedMissingNavMesh) return;
        if (!agent.isOnNavMesh)
        {
            warnedMissingNavMesh = true;
            Debug.LogWarning("[Enemy] NavMeshAgent 不在 NavMesh 上。", this);
        }
    }
}
