using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Vector2 InputDir => InputManager.Instance.MoveInput;

    public Animator animator;
    public bool IsRunning => InputDir.magnitude > 0.5f;

    public float MoveSpeed = 5f;
    public float JumpForce = 5f;
    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public int MaxHealth = 100;
    private float RotationSpeed = 200f; // 旋转速度，可以根据需要调整
    public int CurrentHealth { get; private set; }

    public Rigidbody rb;
    private bool isCursorVisible = true; 

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
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
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            ToggleCursorVisibility();
        }
        
        float mouseX = Input.GetAxis("Mouse X");
        this.transform.Rotate(Vector3.up, mouseX * RotationSpeed * Time.deltaTime);
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

    private void ToggleCursorVisibility()
    {
        if (isCursorVisible)
        {
            Cursor.visible = false; // 隐藏光标
            Cursor.lockState = CursorLockMode.Locked; // 锁定光标
        }
        else
        {
            Cursor.visible = true; // 显示光标
            Cursor.lockState = CursorLockMode.None; // 解锁光标
        }

        // 切换状态
        isCursorVisible = !isCursorVisible;
    }
}

