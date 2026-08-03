using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(DamageDealer))]
[RequireComponent(typeof(FactionMember))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private Rigidbody2D rb;
    private float despawnAt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 direction, float speed, float damage, CombatFaction faction, float duration)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.right;

        lifetime = duration;
        despawnAt = Time.time + lifetime;
        GetComponent<FactionMember>().SetFaction(faction);
        GetComponent<DamageDealer>().Configure(faction, damage, true);
        rb.gravityScale = 0f;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (Time.time >= despawnAt)
            Destroy(gameObject);
    }
}
