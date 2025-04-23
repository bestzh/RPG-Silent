using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Vector2 InputDir => InputManager.Instance.MoveInput;

    public float MoveSpeed = 5f;
    public float JumpForce = 5f;
    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public int MaxHealth = 100;
    public int CurrentHealth { get; private set; }

    public Rigidbody rb;

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        rb = GetComponent<Rigidbody>();
        CurrentHealth = MaxHealth;
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        StateMachine.Update();

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
        {
            StateMachine.ChangeState(new JumpState(this));
        }

        if (Input.GetMouseButtonDown(0))
        {
            StateMachine.ChangeState(new AttackState(this));
        }
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

    public void AddJumpForce()
    {
        rb.linearVelocity  = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // 重置垂直速度
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
}

