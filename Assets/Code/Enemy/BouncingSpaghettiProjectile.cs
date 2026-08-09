using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(FactionMember))]
public class BouncingSpaghettiProjectile : MonoBehaviour
{
    [Header("Collision")]
    [Tooltip("The ring passes through these layers without dealing damage or consuming wall bounces.")]
    [SerializeField] private LayerMask ignoredLayers;

    [Header("Motion")]
    [SerializeField] private float spinDegreesPerSecond = 240f;
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.35f;

    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private Vector2 travelDirection;
    private float speed;
    private float damage;
    private float despawnAt;
    private int maximumBounces;
    private int bounceCount;
    private CombatFaction ownerFaction;
    private bool launched;
    private bool collisionDisabledForFade;
    private SpriteRenderer[] visuals;
    private Color[] baseColors;
    private Health ownerHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        ownCollider.isTrigger = false;
        ApplyIgnoredLayers();
        CacheVisualColors();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnValidate()
    {
        Collider2D projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider != null)
            projectileCollider.excludeLayers = ignoredLayers;
    }

    private void ApplyIgnoredLayers()
    {
        if (ownCollider != null)
            ownCollider.excludeLayers = ignoredLayers;
    }

    public void Launch(Vector2 direction, float projectileSpeed, float projectileDamage, float lifetime,
        int wallBounces, CombatFaction faction, GameObject owner)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.right;

        travelDirection = direction.normalized;
        speed = Mathf.Max(0.1f, projectileSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        maximumBounces = Mathf.Max(0, wallBounces);
        ownerFaction = faction;
        bounceCount = 0;
        despawnAt = Time.time + Mathf.Max(0.1f, lifetime);
        launched = true;
        collisionDisabledForFade = false;
        ownCollider.enabled = true;
        RestoreVisualColors();

        GetComponent<FactionMember>().SetFaction(faction);
        rb.linearVelocity = travelDirection * speed;

        if (owner != null)
        {
            ownerHealth = owner.GetComponentInParent<Health>();
            if (ownerHealth != null)
            {
                ownerHealth.Died -= HandleOwnerDied;
                ownerHealth.Died += HandleOwnerDied;
            }

            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D ownerCollider in ownerColliders)
            {
                if (ownerCollider != null)
                    Physics2D.IgnoreCollision(ownCollider, ownerCollider, true);
            }
        }
    }

    private void HandleOwnerDied()
    {
        BeginFadeOut();
    }

    private void FixedUpdate()
    {
        if (!launched)
            return;

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            travelDirection = rb.linearVelocity.normalized;
        rb.linearVelocity = travelDirection * speed;
    }

    private void Update()
    {
        if (!launched)
            return;

        transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime, Space.Self);
        UpdateFade();
        if (Time.time >= despawnAt)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!launched || collision.collider == null)
            return;

        FactionMember targetFaction = collision.collider.GetComponentInParent<FactionMember>();
        if (targetFaction != null && targetFaction.Faction == ownerFaction)
        {
            Physics2D.IgnoreCollision(ownCollider, collision.collider, true);
            rb.linearVelocity = travelDirection * speed;
            return;
        }

        Health targetHealth = collision.collider.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            BeginFadeOut();
            return;
        }

        if (maximumBounces <= 0 || bounceCount >= maximumBounces)
        {
            // Once its bounce allowance is exhausted, the ring keeps its
            // current trajectory and passes through subsequent walls until
            // its normal lifetime expires.
            Physics2D.IgnoreCollision(ownCollider, collision.collider, true);
            rb.linearVelocity = travelDirection * speed;
            return;
        }

        Vector2 normal = collision.contactCount > 0
            ? collision.GetContact(0).normal
            : -travelDirection;
        travelDirection = Vector2.Reflect(travelDirection, normal).normalized;
        bounceCount++;
        rb.position += normal * 0.02f;
        rb.linearVelocity = travelDirection * speed;
    }

    private void BeginFadeOut()
    {
        if (collisionDisabledForFade)
            return;

        collisionDisabledForFade = true;
        ownCollider.enabled = false;
        despawnAt = Mathf.Min(despawnAt, Time.time + Mathf.Max(0.05f, fadeOutDuration));
    }

    private void UpdateFade()
    {
        if (visuals == null || baseColors == null)
            CacheVisualColors();

        float remaining = despawnAt - Time.time;
        float alphaMultiplier = Mathf.Clamp01(remaining / Mathf.Max(0.05f, fadeOutDuration));
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null)
                continue;

            Color color = baseColors[i];
            color.a *= alphaMultiplier;
            visuals[i].color = color;
        }
    }

    private void CacheVisualColors()
    {
        visuals = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
                baseColors[i] = visuals[i].color;
        }
    }

    private void RestoreVisualColors()
    {
        if (visuals == null || baseColors == null)
            CacheVisualColors();

        int count = Mathf.Min(visuals.Length, baseColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (visuals[i] != null)
                visuals[i].color = baseColors[i];
        }
    }

    private void OnDestroy()
    {
        if (ownerHealth != null)
            ownerHealth.Died -= HandleOwnerDied;
    }
}
