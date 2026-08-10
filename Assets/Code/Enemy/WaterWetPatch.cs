using System.Collections.Generic;
using UnityEngine;

public class WaterWetPatch : MonoBehaviour
{
    private static Sprite solidCircleSprite;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;
    private Vector3 finalScale;
    private float lifetime;
    private float elapsed;
    private readonly HashSet<CharacterMovement> slipperyCharacters = new();

    public void Initialize(
        Sprite sprite,
        float worldSize,
        float patchLifetime,
        float alpha,
        string sortingLayer,
        int sortingOrder)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        Sprite circleSprite = GetSolidCircleSprite();
        spriteRenderer.sprite = circleSprite;
        spriteRenderer.sortingLayerName = sortingLayer;
        spriteRenderer.sortingOrder = sortingOrder;
        initialColor = new Color(0.08f, 0.48f, 1f, Mathf.Clamp01(alpha));
        spriteRenderer.color = initialColor;

        transform.rotation = Quaternion.Euler(-40f, 0f, 0f);
        float spriteSize = circleSprite != null
            ? Mathf.Max(circleSprite.bounds.size.x, circleSprite.bounds.size.y)
            : 1f;
        float scale = Mathf.Max(0.05f, worldSize) / Mathf.Max(0.001f, spriteSize);
        finalScale = Vector3.one * scale;
        transform.localScale = finalScale * 0.38f;
        lifetime = Mathf.Max(0.1f, patchLifetime);

        CircleCollider2D slipperyArea = gameObject.AddComponent<CircleCollider2D>();
        slipperyArea.isTrigger = true;
        slipperyArea.radius = circleSprite != null
            ? Mathf.Max(circleSprite.bounds.extents.x, circleSprite.bounds.extents.y) * 0.92f
            : 0.5f;
    }

    private static Sprite GetSolidCircleSprite()
    {
        if (solidCircleSprite != null)
            return solidCircleSprite;

        const int resolution = 64;
        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Runtime Solid Water Circle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color clear = Color.clear;
        Color solid = Color.white;
        Vector2 center = new((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
        float radius = resolution * 0.47f;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float coverage = 1f - Mathf.Clamp01(distance - radius + 1f);
                texture.SetPixel(x, y, coverage > 0f
                    ? new Color(solid.r, solid.g, solid.b, coverage)
                    : clear);
            }
        }

        texture.Apply(false, true);
        solidCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f), resolution);
        solidCircleSprite.name = "Runtime Solid Water Circle";
        solidCircleSprite.hideFlags = HideFlags.HideAndDontSave;
        return solidCircleSprite;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(elapsed / lifetime);
        float expand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / 0.16f));
        transform.localScale = Vector3.Lerp(finalScale * 0.38f, finalScale, expand);

        if (spriteRenderer != null && normalized >= 0.72f)
        {
            float fade = 1f - Mathf.InverseLerp(0.72f, 1f, normalized);
            Color color = initialColor;
            color.a = initialColor.a * fade;
            spriteRenderer.color = color;
        }

        if (normalized >= 1f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CharacterMovement movement = other.GetComponentInParent<CharacterMovement>();
        if (movement != null && slipperyCharacters.Add(movement))
            movement.EnterSlipperyArea();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        CharacterMovement movement = other.GetComponentInParent<CharacterMovement>();
        if (movement != null && slipperyCharacters.Remove(movement))
            movement.ExitSlipperyArea();
    }

    private void OnDestroy()
    {
        foreach (CharacterMovement movement in slipperyCharacters)
        {
            if (movement != null)
                movement.ExitSlipperyArea();
        }
        slipperyCharacters.Clear();
    }
}
