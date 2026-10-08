using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class Weapon : MonoBehaviour
{
    [SerializeField] private SpellProjectile spellPrefab;
    [SerializeField] private Vector3 heldLocalScale = Vector3.one;

    [Header("Weapon Animations")]
    [SerializeField] private string idleAnimationName = "Weapon_Idle";
    [SerializeField] private string castAnimationName = "Weapon_Cast";
    private Collider2D pickupCollider;
    private Player owner;
    private float pickupAllowedAt;

    public SpellProjectile SpellPrefab => spellPrefab;
    public Player Owner => owner;
    public Animator Animator { get; private set; }
    public WeaponStateMachine StateMachine { get; private set; }
    public WeaponIdleState IdleState { get; private set; }
    public WeaponCastState CastState { get; private set; }
    public string IdleAnimationName => idleAnimationName;
    public string CastAnimationName => castAnimationName;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
        Animator = GetComponent<Animator>();
        StateMachine = new WeaponStateMachine();
        IdleState = new WeaponIdleState(this, StateMachine);
        CastState = new WeaponCastState(this, StateMachine);
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        if (owner == null || owner.Health.IsDead)
            CancelCast();
        StateMachine.CurrentState?.Update();
    }

    private void OnDisable()
    {
        CancelCast();
    }

    public bool BeginCast(float duration)
    {
        if (!isActiveAndEnabled || owner == null || owner.Health.IsDead || owner.EquippedWeapon != this)
            return false;

        CastState.SetDuration(duration);
        StateMachine.ChangeState(CastState);
        return true;
    }

    public void CancelCast()
    {
        if (StateMachine != null && StateMachine.CurrentState != IdleState)
            StateMachine.ChangeState(IdleState);
    }

    public void PlayAnimation(string stateName)
    {
        if (Animator == null || !Animator.isActiveAndEnabled || Animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
            return;

        int stateHash = UnityEngine.Animator.StringToHash(stateName);
        if (Animator.HasState(0, stateHash))
            Animator.CrossFadeInFixedTime(stateHash, 0.03f, 0, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null || Time.time < pickupAllowedAt)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player != null)
            player.EquipWeapon(this);
    }

    public bool EquipTo(Player player, Transform holder, Vector2 localOffset)
    {
        if (player == null || (owner != null && owner != player) || spellPrefab == null)
            return false;

        owner = player;
        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();
        pickupCollider.enabled = false;
        transform.SetParent(holder, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = heldLocalScale;
        CancelCast();
        return true;
    }

    public void Drop()
    {
        CancelCast();
        owner = null;
        transform.SetParent(null, true);
        pickupAllowedAt = Time.time + 0.5f;
        pickupCollider.enabled = true;
    }
}
