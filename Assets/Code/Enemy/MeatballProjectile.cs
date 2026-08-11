using System.Collections.Generic;
using UnityEngine;

public class MeatballProjectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float spinDegreesPerSecond = 540f;

    [Header("Puddle")]
    [SerializeField] private MeatPuddle puddlePrefab;
    [SerializeField] private float puddleRadius = 1.15f;
    [SerializeField] private float puddleDamage = 4f;
    [SerializeField] private float puddleDamageInterval = 0.65f;
    [SerializeField] private float puddleLifetime = 4.5f;

    private Vector3 start;
    private Vector3 destination;
    private float duration;
    private float arcHeight;
    private float damage;
    private float explosionRadius;
    private CombatFaction ownerFaction;
    private float launchedAt;
    private bool launched;

    public void Launch(Vector3 targetPosition, float flightDuration, float height, float impactDamage,
        float impactRadius, CombatFaction faction)
    {
        start = transform.position;
        destination = targetPosition;
        destination.z = start.z;
        duration = Mathf.Max(0.1f, flightDuration);
        arcHeight = Mathf.Max(0f, height);
        damage = Mathf.Max(0f, impactDamage);
        explosionRadius = Mathf.Max(0.05f, impactRadius);
        ownerFaction = faction;
        launchedAt = Time.time;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        float progress = Mathf.Clamp01((Time.time - launchedAt) / duration);
        Vector3 position = Vector3.Lerp(start, destination, progress);
        position.z = start.z - Mathf.Sin(progress * Mathf.PI) * arcHeight;
        transform.position = position;
        transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime, Space.Self);

        if (progress >= 1f)
            Impact();
    }

    private void Impact()
    {
        launched = false;
        GameAudio.PlayMeatballFall();
        Collider2D[] hits = Physics2D.OverlapCircleAll(destination, explosionRadius);
        HashSet<Health> damagedTargets = new();
        foreach (Collider2D hit in hits)
        {
            FactionMember faction = hit.GetComponentInParent<FactionMember>();
            if (faction != null && faction.Faction == ownerFaction)
                continue;

            Health health = hit.GetComponentInParent<Health>();
            if (health != null && !health.IsDead && damagedTargets.Add(health))
                health.TakeDamage(damage);
        }

        SpawnPuddle();
        Destroy(gameObject);
    }

    private void SpawnPuddle()
    {
        MeatPuddle puddle;
        if (puddlePrefab != null)
            puddle = Instantiate(puddlePrefab, destination, puddlePrefab.transform.rotation);
        else
        {
            GameObject puddleObject = new("Meat Puddle");
            puddleObject.transform.position = destination;
            puddleObject.AddComponent<CircleCollider2D>();
            SpriteRenderer renderer = puddleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateFallbackSprite();
            renderer.color = new Color(0.45f, 0.04f, 0.04f, 0.72f);
            puddle = puddleObject.AddComponent<MeatPuddle>();
        }

        puddle.Configure(puddleRadius, puddleDamage, puddleDamageInterval, puddleLifetime, ownerFaction);
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
