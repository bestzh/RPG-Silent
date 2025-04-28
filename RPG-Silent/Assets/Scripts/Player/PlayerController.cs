using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Vector2 InputDir => InputManager.Instance.MoveInput;

    public Animator animator;
    public bool IsRunning => InputDir.magnitude > 0.5f;

    public float MoveSpeed = 5f;
    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, 1.1f);

    public int MaxHealth = 100;
    public int CurrentHealth { get; private set; }

    public Rigidbody rb;
    private bool isCursorVisible = true; 

    private float yaw;   // 水平旋转
    private float pitch; // 垂直旋转
    public float mouseSensitivity = 3f;
    public float RollForce = 3f;
    private float lastSpaceTime = -10f;
    private float doubleTapThreshold = 0.3f; // 双击最大时间间隔
    public bool IsJumping { get; private set; } = false;
    public bool IsRolling { get; private set; } = false;
    public PlayerAnimationController AnimationController { get; private set; }

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        AnimationController = GetComponent<PlayerAnimationController>();
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

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsRolling && !IsJumping) // 防止翻滚/跳跃中攻击
                StateMachine.ChangeState(new AttackState(this));
        }
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            ToggleCursorVisibility();
        }
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            StateMachine.ChangeState(new RollState(this));
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StateMachine.ChangeState(new JumpState(this));
        }

        

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -35f, 60f);
        this.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        
    }
    public void SetIsJumping(bool jumping)
    {
        IsJumping = jumping;
    }
    public void SetIsRolling(bool rolling)
    {
        IsRolling = rolling;
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
    private void OnGetHit()
    {
        GetComponent<SkillCastManager>()?.InterruptSkill();
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

