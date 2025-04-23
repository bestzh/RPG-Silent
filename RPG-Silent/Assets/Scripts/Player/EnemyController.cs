using UnityEngine;
public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public int damage = 10;

    private Transform target;
    private float attackTimer;

    void Start()
    {
        target = GameObject.FindWithTag("Player")?.transform;
        attackTimer = attackCooldown;

        if (target == null)
            Debug.LogWarning("⚠️ 未找到玩家目标");
    }

    void Update()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // 水平方向

        if (direction.magnitude > attackDistance)
        {
            if (direction != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 5f);
            }

            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
    }

    void Attack()
    {
        Debug.Log("敌人攻击了！666");
        target.GetComponent<PlayerController>()?.TakeDamage(damage);
    }
    public void TakeDamage(int damage)
    {
        Debug.Log("受到攻击666");
        Destroy(this.gameObject);
    }
}
