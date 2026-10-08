using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
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

    [Header("Combat")]
    [SerializeField] private Weapon startingWeaponPrefab;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Vector2 heldWeaponOffset = new Vector2(1.1f, 0.1f);
    [SerializeField] private Vector2 spellSpawnOffset = new Vector2(0.65f, 0.1f);
    [SerializeField, Min(0f)] private float spellCooldown = 0.5f;
    [SerializeField, Min(0f)] private float castDuration = 0.15f;

    private Weapon equippedWeapon;
    private CombatCooldown combatCooldown;
    private bool attackPressed;
    public bool HasWeapon => equippedWeapon != null && equippedWeapon.isActiveAndEnabled && equippedWeapon.Owner == this;
    public Weapon EquippedWeapon => HasWeapon ? equippedWeapon : null;
    public float SpellCooldownRemaining => combatCooldown != null ? combatCooldown.Remaining : 0f;
    public float CastDuration => castDuration;
    public bool CanCastSpell => combatCooldown != null && combatCooldown.CanCast(HasWeapon, Health.IsDead) && equippedWeapon.SpellPrefab != null;


    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }
    public Health Health { get; private set; }

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
    public PlayerCombatState CombatState { get; private set; }


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
        Health = GetComponent<Health>();

        NormalGravityScale = Rb.gravityScale;
        StateMachine = new PlayerStateMachine();

        IdleState = new PlayerIdleState(this, StateMachine);
        MoveState = new PlayerMoveState(this, StateMachine);
        JumpState = new PlayerJumpState(this, StateMachine);
        DashState = new PlayerDashState(this, StateMachine);
        FallState = new PlayerFallState(this, StateMachine);
        CombatState = new PlayerCombatState(this, StateMachine);
        combatCooldown = new CombatCooldown(spellCooldown);
        Health.Died += HandleDeath;
        if (startingWeaponPrefab != null)
            EquipWeapon(Instantiate(startingWeaponPrefab));
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        combatCooldown.Tick(Time.deltaTime);
        if (Health.IsDead)
        {
            attackPressed = false;
            Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
            return;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (attackPressed)
        {
            attackPressed = false;
            TryEnterCombat();
        }
        StateMachine.CurrentState?.Update();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && CanCastSpell)
            attackPressed = true;
    }

    public bool TryEnterCombat()
    {
        if (!CanCastSpell || StateMachine.CurrentState == null ||
            StateMachine.CurrentState == DashState || StateMachine.CurrentState == CombatState)
            return false;

        StateMachine.changeState(CombatState);
        return true;
    }

    public bool EquipWeapon(Weapon weapon)
    {
        if (weapon == null || Health.IsDead || !weapon.EquipTo(this, weaponHolder != null ? weaponHolder : transform, heldWeaponOffset))
            return false;

        if (equippedWeapon != null && equippedWeapon != weapon)
            equippedWeapon.Drop();
        equippedWeapon = weapon;
        attackPressed = false;
        return true;
    }

    public void UnequipWeapon()
    {
        if (equippedWeapon != null)
            equippedWeapon.Drop();
        equippedWeapon = null;
        attackPressed = false;
    }

    public bool TryCastSpell()
    {
        if (!CanCastSpell || !combatCooldown.TryStart(HasWeapon, Health.IsDead))
            return false;

        Vector3 position = transform.position + new Vector3(spellSpawnOffset.x * FacingDirection, spellSpawnOffset.y, 0f);
        SpellProjectile spell = Instantiate(equippedWeapon.SpellPrefab, position, Quaternion.identity);
        spell.Launch(this, FacingDirection);
        equippedWeapon.BeginCast(castDuration);
        return true;
    }

    private void HandleDeath()
    {
        attackPressed = false;
        JumpPressed = false;
        DashPressed = false;
        EquippedWeapon?.CancelCast();
        // Exiting DashState restores gravity even when death interrupts the dash.
        StateMachine.changeState(new PlayerState(this, StateMachine));
        Rb.gravityScale = NormalGravityScale;
        Rb.linearVelocity = Vector2.zero;
        Animator.enabled = false;
    }

    private void OnDestroy()
    {
        if (Health != null)
            Health.Died -= HandleDeath;
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
