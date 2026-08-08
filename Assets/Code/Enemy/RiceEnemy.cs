using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RiceEnemy : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float approachDistance = 3.2f;
    [SerializeField] private float retreatDistance = 2.2f;
    [SerializeField] private float moveSpeed = 1.45f;
    [SerializeField] private float strafeSpeed = 1.55f;
    [SerializeField] private float minStrafeSwitchInterval = 0.45f;
    [SerializeField] private float maxStrafeSwitchInterval = 0.85f;
    [SerializeField] private float preShotApproachDuration = 0.25f;
    [SerializeField] private float preShotApproachSpeed = 2.1f;
    [SerializeField] private float minShootCooldown = 1f;
    [SerializeField] private float maxShootCooldown = 2.3f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileLifetime = 2.1f;
    [SerializeField] private float followUpDelay = 0.2f;
    [SerializeField] private bool predictTargetMovement;
    [SerializeField] private float predictionStrength = 0.65f;

    private static Projectile fallbackProjectilePrefab;

    private Rigidbody2D rb;
    private float nextShotAt;
    private float nextStrafeSwitchAt;
    private float burstChance;
    private int maxBurstShots = 1;
    private int strafeDirection = 1;
    private bool attacking;

    private void Awake()
    {
        EnsureRigidbody();
    }

    private void OnEnable()
    {
        EnsureRigidbody();
        nextShotAt = Time.time + Random.Range(0.4f, maxShootCooldown);
        strafeDirection = Random.value < 0.5f ? -1 : 1;
        nextStrafeSwitchAt = Time.time + Random.Range(minStrafeSwitchInterval, maxStrafeSwitchInterval);
        attacking = false;
    }

    private void EnsureRigidbody()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        EnsureRigidbody();
        if (rb == null)
            return;

        if (target == null || attacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 forward = toTarget / distance;
        Vector2 strafe = new(-forward.y, forward.x);
        if (distance > approachDistance)
        {
            rb.linearVelocity = forward * moveSpeed;
            return;
        }

        if (Time.time >= nextStrafeSwitchAt)
        {
            strafeDirection = Random.value < 0.5f ? -1 : 1;
            nextStrafeSwitchAt = Time.time + Random.Range(minStrafeSwitchInterval, maxStrafeSwitchInterval);
        }

        Vector2 desiredVelocity = strafe * (strafeSpeed * strafeDirection);
        if (distance < retreatDistance)
            desiredVelocity -= forward * (moveSpeed * 0.65f);

        rb.linearVelocity = desiredVelocity;
    }

    private void Update()
    {
        if (target == null || attacking || Time.time < nextShotAt)
            return;

        StartCoroutine(AttackRoutine());
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void Configure(
        float approach,
        float retreat,
        float speed,
        float lateralSpeed,
        float cooldownMin,
        float cooldownMax,
        float shotSpeed,
        float shotDamage,
        float followUpProbability,
        int burstShotLimit,
        bool usesPrediction)
    {
        approachDistance = Mathf.Max(0.5f, approach);
        retreatDistance = Mathf.Clamp(retreat, 0.25f, approachDistance);
        moveSpeed = Mathf.Max(0f, speed);
        strafeSpeed = Mathf.Max(0f, lateralSpeed);
        minShootCooldown = Mathf.Max(0.1f, cooldownMin);
        maxShootCooldown = Mathf.Max(minShootCooldown, cooldownMax);
        projectileSpeed = Mathf.Max(0.5f, shotSpeed);
        projectileDamage = Mathf.Max(0f, shotDamage);
        burstChance = Mathf.Clamp01(followUpProbability);
        maxBurstShots = Mathf.Max(1, burstShotLimit);
        predictTargetMovement = usesPrediction;
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        float approachUntil = Time.time + preShotApproachDuration;
        while (Time.time < approachUntil && target != null)
        {
            Vector2 direction = target.position - transform.position;
            rb.linearVelocity = direction.sqrMagnitude > 0.001f
                ? direction.normalized * preShotApproachSpeed
                : Vector2.zero;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        int shotsToFire = RollShotCount();
        for (int i = 0; i < shotsToFire; i++)
        {
            Shoot();
            if (i < shotsToFire - 1)
                yield return new WaitForSeconds(followUpDelay);
        }

        strafeDirection *= -1;
        nextShotAt = Time.time + Random.Range(minShootCooldown, maxShootCooldown);
        attacking = false;
    }

    private int RollShotCount()
    {
        int shots = 1;
        while (shots < maxBurstShots && Random.value < burstChance)
            shots++;

        return shots;
    }

    private void Shoot()
    {
        Vector2 direction = GetShotDirection();
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.down;

        Projectile projectile = Instantiate(CreateProjectilePrefab(), transform.position, Quaternion.identity);
        projectile.gameObject.SetActive(true);
        projectile.Launch(direction, projectileSpeed, projectileDamage, CombatFaction.Enemy, projectileLifetime);
    }

    private Vector2 GetShotDirection()
    {
        if (target == null)
            return Vector2.down;

        Vector2 targetPosition = target.position;
        if (predictTargetMovement)
        {
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                float leadTime = distance / Mathf.Max(0.1f, projectileSpeed);
                targetPosition += targetBody.linearVelocity * (leadTime * predictionStrength);
            }
        }

        return targetPosition - (Vector2)transform.position;
    }

    private static Projectile CreateProjectilePrefab()
    {
        if (fallbackProjectilePrefab != null)
            return fallbackProjectilePrefab;

        GameObject projectileObject = new("Rice Enemy Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.14f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = new Color(1f, 0.82f, 0.35f);
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
