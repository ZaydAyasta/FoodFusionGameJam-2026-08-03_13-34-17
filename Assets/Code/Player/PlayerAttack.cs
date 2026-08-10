using UnityEngine;

[RequireComponent(typeof(CharacterInput))]
[RequireComponent(typeof(PlayerAim))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileRange = 4.5f;

    private CharacterInput input;
    private PlayerAim aim;
    private float nextFireTime;
    private bool autoAimEnabled;

    public float ProjectileRange => projectileRange;
    public bool AutoAimEnabled => autoAimEnabled;

    public void AddDamageBonus(float amount)
    {
        if (amount <= 0f)
            return;

        projectileDamage += amount;
    }

    public void MultiplyCooldown(float multiplier)
    {
        if (multiplier <= 0f)
            return;

        cooldown = Mathf.Max(0.05f, cooldown * multiplier);
    }

    public void AddRangeBonus(float amount)
    {
        if (amount > 0f)
            projectileRange += amount;
    }

    public void EnableAutoAim()
    {
        autoAimEnabled = true;
    }

    private void Awake()
    {
        input = GetComponent<CharacterInput>();
        aim = GetComponent<PlayerAim>();
    }

    private void Update()
    {
        if (!input.AttackHeld || Time.time < nextFireTime)
            return;

        Fire();
        nextFireTime = Time.time + cooldown;
    }

    private void Fire()
    {
        Vector2 direction = GetFireDirection();
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + (Vector3)(direction * 0.55f);
        float projectileLifetime = projectileSpeed > 0.01f
            ? projectileRange / projectileSpeed
            : 0.1f;

        Projectile prefab = GetProjectilePrefab();
        Projectile projectile = Instantiate(prefab, origin, prefab.transform.rotation);
        projectile.gameObject.SetActive(true);
        projectile.Launch(direction, projectileSpeed, projectileDamage, CombatFaction.Player, projectileLifetime);
        GameAudio.PlayShoot();
    }

    private Vector2 GetFireDirection()
    {
        Vector2 fallbackDirection = aim.AimDirection;
        if (!autoAimEnabled)
            return fallbackDirection;

        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        EnemyDeathNotifier[] enemies = FindObjectsByType<EnemyDeathNotifier>(FindObjectsInactive.Exclude);
        Transform closestEnemy = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (EnemyDeathNotifier enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null || enemyHealth.IsDead)
                continue;

            Vector2 targetPosition = GetTargetPosition(enemy.transform);
            float distanceSquared = (targetPosition - origin).sqrMagnitude;
            if (distanceSquared >= closestDistanceSquared)
                continue;

            closestDistanceSquared = distanceSquared;
            closestEnemy = enemy.transform;
        }

        if (closestEnemy == null)
            return fallbackDirection;

        Vector2 direction = GetTargetPosition(closestEnemy) - origin;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallbackDirection;
    }

    private static Vector2 GetTargetPosition(Transform target)
    {
        Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
        return targetCollider != null ? targetCollider.bounds.center : target.position;
    }

    private Projectile GetProjectilePrefab()
    {
        if (projectilePrefab != null)
            return projectilePrefab;

        GameObject projectileObject = new("Player Projectile");
        projectileObject.SetActive(false);
        projectileObject.AddComponent<CircleCollider2D>().radius = 0.12f;
        projectileObject.AddComponent<Rigidbody2D>();
        Projectile projectile = projectileObject.AddComponent<Projectile>();
        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        renderer.color = Color.yellow;
        projectilePrefab = projectile;
        return projectilePrefab;
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 8f);
    }
}
