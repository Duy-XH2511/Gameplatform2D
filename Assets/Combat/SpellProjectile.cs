using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class SpellProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10f;
    [SerializeField, Min(0.01f)] private float maxDistance = 8f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private LayerMask blockingLayers = (1 << 6) | (1 << 7);

    private Rigidbody2D body;
    private CircleCollider2D hitCollider;
    private ProjectileTravel travel;
    private Player owner;
    private Vector2 direction;
    private bool launched;
    private bool spent;

    public float MaxDistance => maxDistance;
    public int Damage => damage;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<CircleCollider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        hitCollider.isTrigger = true;
    }

    public void Launch(Player caster, int facingDirection)
    {
        owner = caster;
        direction = facingDirection < 0 ? Vector2.left : Vector2.right;
        GetComponent<SpriteRenderer>().flipX = facingDirection < 0;
        travel = new ProjectileTravel(Mathf.Max(0.01f, maxDistance));
        launched = true;

        if (owner != null)
        {
            Vector2 origin = new Vector2(owner.transform.position.x, body.position.y);
            Vector2 offset = body.position - origin;
            if (offset.sqrMagnitude > 0f)
                Sweep(origin + (Vector2)transform.TransformVector(hitCollider.offset), offset.normalized, offset.magnitude);
        }
    }

    private void FixedUpdate()
    {
        if (!launched || spent)
            return;

        float distance = travel.Advance(Mathf.Max(0.01f, speed), Time.fixedDeltaTime);
        Vector2 center = body.position + (Vector2)transform.TransformVector(hitCollider.offset);
        if (Sweep(center, direction, distance))
            return;

        body.position += direction * distance;
        if (travel.IsComplete)
            Expire();
    }

    private bool Sweep(Vector2 origin, Vector2 castDirection, float distance)
    {
        Vector3 scale = transform.lossyScale;
        float radius = hitCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        // Check both the muzzle offset and each flight step, including thin blockers.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, castDirection, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (!TryHit(hit.collider))
                continue;

            body.position = origin + castDirection * hit.distance - (Vector2)transform.TransformVector(hitCollider.offset);
            return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (launched)
            TryHit(other);
    }

    private bool TryHit(Collider2D other)
    {
        if (spent || other == null || other == hitCollider || other.GetComponentInParent<Player>() != null ||
            other.GetComponentInParent<Weapon>() != null || other.GetComponentInParent<SpellProjectile>() != null)
            return false;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (enemy.Health.IsDead)
                return false;

            spent = true;
            enemy.TakeDamage(damage);
            Expire();
            return true;
        }

        if (!other.isTrigger && (blockingLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Expire();
            return true;
        }
        return false;
    }

    private void Expire()
    {
        spent = true;
        hitCollider.enabled = false;
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0.01f, speed);
        maxDistance = Mathf.Max(0.01f, maxDistance);
        damage = Mathf.Max(1, damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * maxDistance);
    }
}
