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
        Vector2 direction = aim.AimDirection;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + (Vector3)(direction * 0.55f);
        float projectileLifetime = projectileSpeed > 0.01f
            ? projectileRange / projectileSpeed
            : 0.1f;

        Projectile prefab = GetProjectilePrefab();
        Projectile projectile = Instantiate(prefab, origin, prefab.transform.rotation);
        projectile.gameObject.SetActive(true);
        projectile.Launch(direction, projectileSpeed, projectileDamage, CombatFaction.Player, projectileLifetime);
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
