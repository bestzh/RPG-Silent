using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Health")]
    public int MaxHealth = 60;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    [Header("Combat")]
    public float moveSpeed = 3f;
    public float turnSpeed = 8f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public int damage = 10;

    [Header("Reward")]
    [SerializeField] private int rewardGold = 10;
    [SerializeField] private int rewardExp = 25;

    [Header("Navigation")]
    [SerializeField] private bool useNavMeshAgent = true;

    [Header("Feedback")]
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private float hitStopDuration = 0.15f;
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    private Transform target;
    private float attackTimer;
    private Animator animator;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine hurtRoutine;
    private Vector3 desiredMoveDirection;
    private bool warnedMissingNavMesh;

    private bool CanUseAgent => useNavMeshAgent
        && agent != null
        && agent.enabled
        && agent.isOnNavMesh;

    private void Start()
    {
        CurrentHealth = MaxHealth;
        target = GameObject.FindWithTag("Player")?.transform;
        attackTimer = attackCooldown;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        ConfigureAgent();
        ConfigureRigidbody();

        if (target == null)
        {
            Debug.LogWarning("EnemyController did not find a Player target.", this);
        }
    }

    private void Update()
    {
        if (IsDead || target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude > attackDistance)
        {
            desiredMoveDirection = direction.normalized;

            if (CanUseAgent)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
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
        if (CanUseAgent)
        {
            return;
        }

        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontalVelocity = IsDead
                ? Vector3.zero
                : desiredMoveDirection * moveSpeed;

            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
            rb.angularVelocity = Vector3.zero;
            return;
        }

        if (IsDead || desiredMoveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 movement = desiredMoveDirection * moveSpeed * Time.fixedDeltaTime;
        transform.position += movement;
    }

    private void Attack()
    {
        if (IsDead)
        {
            return;
        }

        Debug.Log($"Enemy attacked player for {damage} damage.", this);
        target.GetComponent<PlayerController>()?.TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        Debug.Log($"Enemy took {damage} damage. HP: {CurrentHealth}/{MaxHealth}", this);

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
        }

        hurtRoutine = StartCoroutine(HurtFeedback());
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        attackTimer = attackCooldown;
        StopAgent();
        GrantReward();

        if (hurtRoutine != null)
        {
            StopCoroutine(hurtRoutine);
            hurtRoutine = null;
        }

        animator?.SetTrigger("Die");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider enemyCollider in GetComponentsInChildren<Collider>())
        {
            enemyCollider.enabled = false;
        }

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        Debug.Log("Enemy died.", this);

        if (destroyDelay >= 0f)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void GrantReward()
    {
        PlayerController player = target != null ? target.GetComponent<PlayerController>() : null;
        player?.AddReward(rewardGold, rewardExp);
    }

    private IEnumerator HurtFeedback()
    {
        animator?.SetTrigger("Hit");

        if (target != null && knockbackDistance > 0f)
        {
            Vector3 knockbackDirection = transform.position - target.position;
            knockbackDirection.y = 0f;

            if (knockbackDirection.sqrMagnitude > 0.001f)
            {
                Vector3 knockback = knockbackDirection.normalized * knockbackDistance;
                if (CanUseAgent)
                {
                    agent.Warp(transform.position + knockback);
                }
                else if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = new Vector3(knockback.x / Time.fixedDeltaTime, rb.linearVelocity.y, knockback.z / Time.fixedDeltaTime);
                }
                else
                {
                    transform.position += knockback;
                }
            }
        }

        float originalSpeed = moveSpeed;
        moveSpeed = 0f;
        if (CanUseAgent)
        {
            agent.speed = 0f;
        }

        yield return new WaitForSeconds(hitStopDuration);

        moveSpeed = originalSpeed;
        if (CanUseAgent)
        {
            agent.speed = moveSpeed;
        }

        hurtRoutine = null;
    }

    private void RotateTowards(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion toRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, deltaTime * turnSpeed);
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
        {
            return;
        }

        if (useNavMeshAgent && agent != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.angularVelocity = Vector3.zero;

        foreach (Collider enemyCollider in GetComponentsInChildren<Collider>())
        {
            if (enemyCollider.isTrigger)
            {
                Debug.LogWarning($"{enemyCollider.name} is a Trigger collider. Enemy needs at least one non-trigger collider to stand on the ground.", enemyCollider);
            }
        }
    }

    private void ConfigureAgent()
    {
        if (agent == null)
        {
            return;
        }

        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed * 60f;
        agent.stoppingDistance = attackDistance;
        agent.updateRotation = true;
        agent.updatePosition = true;
        agent.autoBraking = true;
    }

    private void StopAgent()
    {
        if (!CanUseAgent)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void WarnIfAgentIsMissingNavMesh()
    {
        if (!useNavMeshAgent || agent == null || warnedMissingNavMesh)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            warnedMissingNavMesh = true;
            Debug.LogWarning("Enemy has a NavMeshAgent, but it is not on a baked NavMesh.", this);
        }
    }
}
