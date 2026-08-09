using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeatPuddle : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private float expandDuration = 0.35f;
    [SerializeField] private float holdBeforeFade = 0.3f;
    [SerializeField, Range(0f, 1f)] private float startingScale = 0.08f;

    private readonly Dictionary<Health, float> nextDamageAt = new();
    private SpriteRenderer visual;
    private Color initialColor;
    private Vector3 targetVisualScale;
    private CircleCollider2D damageArea;
    private float targetRadius;
    private float damage;
    private float interval;
    private float lifetime;
    private float spawnedAt;
    private CombatFaction ownerFaction;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ResolveSingleVisual();
        if (visual != null)
            initialColor = visual.color;
    }

    public void Configure(float radius, float damageAmount, float damageInterval, float duration,
        CombatFaction faction)
    {
        damageArea = GetComponent<CircleCollider2D>();
        if (damageArea == null)
            damageArea = gameObject.AddComponent<CircleCollider2D>();
        damageArea.isTrigger = true;
        targetRadius = Mathf.Max(0.05f, radius);
        damageArea.radius = targetRadius * startingScale;

        damage = Mathf.Max(0f, damageAmount);
        interval = Mathf.Max(0.05f, damageInterval);
        lifetime = Mathf.Max(0.1f, duration);
        ownerFaction = faction;
        spawnedAt = Time.time;

        ResolveSingleVisual();
        if (visual != null)
        {
            initialColor = visual.color;
            Vector2 spriteSize = visual.sprite != null ? visual.sprite.bounds.size : Vector2.one;
            float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            float diameter = targetRadius * 2f;
            float scale = largestSide > Mathf.Epsilon ? diameter / largestSide : diameter;
            targetVisualScale = Vector3.one * scale;
            visual.transform.localScale = targetVisualScale * startingScale;
        }
    }

    private void Update()
    {
        float age = Time.time - spawnedAt;
        float expansion = Mathf.Clamp01(age / Mathf.Max(0.01f, expandDuration));
        float easedExpansion = 1f - Mathf.Pow(1f - expansion, 3f);
        float currentScale = Mathf.Lerp(startingScale, 1f, easedExpansion);

        if (visual != null)
            visual.transform.localScale = targetVisualScale * currentScale;
        if (damageArea != null)
            damageArea.radius = targetRadius * currentScale;

        float fadeStartsAt = Mathf.Min(lifetime, Mathf.Max(0f, expandDuration + holdBeforeFade));
        float fadeDuration = Mathf.Max(0.01f, lifetime - fadeStartsAt);
        float fadeProgress = Mathf.Clamp01((age - fadeStartsAt) / fadeDuration);
        if (visual != null)
        {
            Color color = initialColor;
            color.a = initialColor.a * (1f - fadeProgress);
            visual.color = color;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }

    private void ResolveSingleVisual()
    {
        if (visual != null)
            return;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer source = null;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.sprite != null)
            {
                source = renderer;
                if (renderer.transform == transform)
                    break;
            }
        }

        if (source == null)
            return;

        if (source.transform == transform)
        {
            GameObject visualObject = new("PuddleVisual");
            visualObject.transform.SetParent(transform, false);
            visual = visualObject.AddComponent<SpriteRenderer>();
            visual.sprite = source.sprite;
            visual.color = source.color;
            visual.flipX = source.flipX;
            visual.flipY = source.flipY;
            visual.drawMode = source.drawMode;
            visual.size = source.size;
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = source.sortingOrder;
            visual.sharedMaterial = source.sharedMaterial;
        }
        else
        {
            visual = source;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer != visual)
                renderer.enabled = false;
        }

        visual.enabled = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        FactionMember faction = other.GetComponentInParent<FactionMember>();
        if (faction != null && faction.Faction == ownerFaction)
            return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead)
            return;

        if (nextDamageAt.TryGetValue(health, out float allowedAt) && Time.time < allowedAt)
            return;

        health.TakeDamage(damage);
        nextDamageAt[health] = Time.time + interval;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null)
            nextDamageAt.Remove(health);
    }
}
