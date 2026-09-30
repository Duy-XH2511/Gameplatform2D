using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private int maxJumpCount = 2;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;


    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }

    // Input
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public int JumpCount { get; set; }
    public float NormalGravityScale { get; private set; }
    public bool DashPressed { get; private set; }
    private float dashCooldownTimer;

    // State Machine
    public PlayerStateMachine StateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerFallState FallState { get; private set; }


    public float MoveSpeed => moveSpeed;
    public float JumpForce => jumpForce;
    public int MaxJumpCount => maxJumpCount;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public bool CanDash => dashCooldownTimer <= 0f;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();

        NormalGravityScale = Rb.gravityScale;
        StateMachine = new PlayerStateMachine();

        IdleState = new PlayerIdleState(this, StateMachine);
        MoveState = new PlayerMoveState(this, StateMachine);
        JumpState = new PlayerJumpState(this, StateMachine);
        DashState = new PlayerDashState(this, StateMachine);
        FallState = new PlayerFallState(this, StateMachine);
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        StateMachine.CurrentState.Update();
    }

    // Nhận A/D từ Player Input
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    // Nhận Space từ Player Input
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            JumpPressed = true;
    }

    public void ConsumeJumpInput()
    {
        JumpPressed = false;
    }
    public bool IsGrounded()
    {
        bool grounded = Physics2D.OverlapCircle(groundCheck.position,groundCheckRadius,groundLayer);

        return grounded;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(groundCheck.position,groundCheckRadius);
    }
    public int FacingDirection { get; private set; } = 1;

    public void Flip(float xInput)
    {
        if (xInput > 0 && FacingDirection == -1)
        {
            FacingDirection = 1;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x),transform.localScale.y,transform.localScale.z);
        }
        else if (xInput < 0 && FacingDirection == 1)
        {
            FacingDirection = -1;
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),transform.localScale.y,transform.localScale.z);
        }
    }
    public void ResetJumpCount()
    {
        JumpCount = 0;
    }
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && CanDash)
        {
            DashPressed = true;
        }
    }
    public void ConsumeDashInput()
    {
        DashPressed = false;
    }
    public void StartDashCooldown()
    {
        dashCooldownTimer = dashCooldown;
    }
}