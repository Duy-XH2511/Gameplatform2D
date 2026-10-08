using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
[RequireComponent(typeof(Health))]
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

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackRange = 1f;
    [SerializeField, Min(0f)] private float attackVerticalTolerance = 1f;
    [SerializeField, Min(0f)] private float attackWindup = 0.25f;
    [SerializeField, Min(0f)] private float attackDuration = 0.6f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;
    [SerializeField, Min(1)] private int attackDamage = 1;

    private float nextAttackTime;
    private Health playerHealth;

    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }
    public Health Health { get; private set; }

    public Transform Player { get; private set; }

    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;
    public float AttackWindup => attackWindup;
    public float AttackDuration => attackDuration;
    public bool CanAttack => Time.time >= nextAttackTime;

    // Cho các hiệu ứng đánh trúng hoặc âm thanh sử dụng về sau.
    public event System.Action<Transform> AttackHit;

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
    public EnemyAttackState AttackState { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();
        Health = GetComponent<Health>();
        Health.Died += HandleDeath;
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
        AttackState = new EnemyAttackState(this, StateMachine);
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            Player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
        }

        StateMachine.Initialize(PatrolState);
    }

    private void FixedUpdate()
    {
        if (Health.IsDead)
            return;
        StateMachine.CurrentState?.Update();
    }

    public void TakeDamage(int amount)
    {
        Health.TakeDamage(amount);
    }

    private void HandleDeath()
    {
        Rb.linearVelocity = Vector2.zero;
        Rb.simulated = false;
        bodyCollider.enabled = false;
        Animator.enabled = false;
    }

    private void OnDestroy()
    {
        if (Health != null)
            Health.Died -= HandleDeath;
    }

    public bool CanSeePlayer()
    {
        if (Player == null || (playerHealth != null && playerHealth.IsDead))
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

    public bool IsPlayerInAttackRange()
    {
        if (Player == null || (playerHealth != null && playerHealth.IsDead))
            return false;

        Vector2 offset = Player.position - transform.position;
        float forwardDistance = offset.x * FacingDirection;

        return forwardDistance >= 0f &&
               forwardDistance <= attackRange &&
               Mathf.Abs(offset.y) <= attackVerticalTolerance;
    }

    public void StartAttackCooldown()
    {
        nextAttackTime = Time.time + attackCooldown;
    }

    public void TryAttackHit()
    {
        if (!IsPlayerInAttackRange() || playerHealth == null)
            return;

        playerHealth.TakeDamage(Mathf.Max(1, attackDamage));
        AttackHit?.Invoke(Player);
    }
}
