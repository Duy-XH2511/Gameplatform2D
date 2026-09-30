using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;

    [Header("Collision Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [FormerlySerializedAs("groundCheckDistance")]
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private float wallCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }

    public Transform Player { get; private set; }

    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;

    public int FacingDirection { get; private set; } = 1;
    public SpriteRenderer Sr { get; private set; }
    private Collider2D bodyCollider;
    private float groundCheckOffsetX;
    private float wallCheckOffsetX;

    // State Machine
    public EnemyStateMachine StateMachine { get; private set; }

    public EnemyIdleState IdleState { get; private set; }
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
        Sr = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();

        if (groundCheck != null)
            groundCheckOffsetX = Mathf.Abs(groundCheck.localPosition.x - bodyCollider.offset.x);

        if (wallCheck != null)
            wallCheckOffsetX = Mathf.Abs(wallCheck.localPosition.x - bodyCollider.offset.x);

        StateMachine = new EnemyStateMachine();

        IdleState = new EnemyIdleState(this, StateMachine);
        PatrolState = new EnemyPatrolState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            Player = playerObject.transform;
        }

        StateMachine.Initialize(PatrolState);
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState?.Update();
    }

    public bool CanSeePlayer()
    {
        if (Player == null)
            return false;

        float distance = Vector2.Distance(transform.position, Player.position);

        return distance <= detectionRange;
    }

    public void Flip(float direction)
    {
        int newFacingDirection = FacingDirection;

        if (direction > 0)
            newFacingDirection = 1;
        else if (direction < 0)
            newFacingDirection = -1;

        if (newFacingDirection == FacingDirection)
            return;

        FacingDirection = newFacingDirection;
        Sr.flipX = FacingDirection == -1;
        UpdateCheckPositions();
    }

    public bool IsGroundAhead()
    {
        return groundCheck != null &&
               Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public bool IsWallAhead()
    {
        if (wallCheck == null)
            return false;

        Vector2 direction = Vector2.right * FacingDirection;
        return Physics2D.Raycast(wallCheck.position, direction, wallCheckDistance, groundLayer);
    }
    public void TurnAround()
    {
        FacingDirection *= -1;

        Sr.flipX = FacingDirection == -1;

        UpdateCheckPositions();
    }

    private void UpdateCheckPositions()
    {
        if (groundCheck != null)
        {
            Vector3 groundPos = groundCheck.localPosition;
            groundPos.x = bodyCollider.offset.x + groundCheckOffsetX * FacingDirection;
            groundCheck.localPosition = groundPos;
        }

        if (wallCheck != null)
        {
            Vector3 wallPos = wallCheck.localPosition;
            wallPos.x = bodyCollider.offset.x + wallCheckOffsetX * FacingDirection;
            wallCheck.localPosition = wallPos;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position,detectionRange);

        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position,groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position + Vector3.right * FacingDirection * wallCheckDistance
            );
        }
    }
    public void StopMovement()
    {
        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
    }

    public void PlayAnimation(string stateName, float transitionDuration = 0.1f)
    {
        if (Animator.runtimeAnimatorController != null)
            Animator.CrossFade(stateName, transitionDuration);
    }

    public void StepBack()
    {
        Rb.position += Vector2.left * FacingDirection * 0.1f;
    }
}
