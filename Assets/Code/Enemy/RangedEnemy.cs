using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RangedEnemy : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float preferredDistance = 4f;
    [SerializeField] private float moveSpeed = 1.75f;
    [SerializeField] private float shootCooldown = 1.25f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileLifetime = 2.2f;

    private Rigidbody2D rb;
    private float nextShotAt;
    private static Projectile fallbackProjectilePrefab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        nextShotAt = Time.time + Random.Range(0.25f, shootCooldown);
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        if (distance > preferredDistance + 0.4f)
            rb.linearVelocity = toTarget.normalized * moveSpeed;
        else if (distance < preferredDistance - 0.4f)
            rb.linearVelocity = -toTarget.normalized * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (target == null || Time.time < nextShotAt)
            return;

        Shoot();
        nextShotAt = Time.time + shootCooldown;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Configure(float distance, float speed, float cooldown, float shotSpeed, float shotDamage)
    {
        preferredDistance = Mathf.Max(0.5f, distance);
        moveSpeed = Mathf.Max(0f, speed);
        shootCooldown = Mathf.Max(0.1f, cooldown);
        projectileSpeed = Mathf.Max(0.5f, shotSpeed);
        projectileDamage = Mathf.Max(0f, shotDamage);
    }

    private void Shoot()
    {
        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        Projectile projectile = Instantiate(CreateProjectilePrefab(), transform.position, Quaternion.identity);
        projectile.gameObject.SetActive(true);
        projectile.Launch(direction, projectileSpeed, projectileDamage, CombatFaction.Enemy, projectileLifetime);
    }

    private static Projectile CreateProjectilePrefab()
    {
        if (fallbackProjectilePrefab != null)
            return fallbackProjectilePrefab;

        GameObject projectileObject = new("Enemy Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.14f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(1f, 0.25f, 0.2f);
        fallbackProjectilePrefab = projectile;
        return fallbackProjectilePrefab;
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 8f);
    }
}
