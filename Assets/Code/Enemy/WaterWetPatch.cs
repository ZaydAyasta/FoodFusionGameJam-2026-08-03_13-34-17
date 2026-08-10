using UnityEngine;

public class WaterWetPatch : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color initialColor;
    private Vector3 finalScale;
    private float lifetime;
    private float elapsed;

    public void Initialize(
        Sprite sprite,
        float worldSize,
        float patchLifetime,
        float alpha,
        string sortingLayer,
        int sortingOrder)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = sortingLayer;
        spriteRenderer.sortingOrder = sortingOrder;
        initialColor = new Color(0.55f, 0.82f, 1f, Mathf.Clamp01(alpha));
        spriteRenderer.color = initialColor;

        transform.rotation = Quaternion.Euler(-40f, 0f, Random.Range(0f, 360f));
        float spriteSize = sprite != null
            ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y)
            : 1f;
        float scale = Mathf.Max(0.05f, worldSize) / Mathf.Max(0.001f, spriteSize);
        finalScale = Vector3.one * scale;
        transform.localScale = finalScale * 0.65f;
        lifetime = Mathf.Max(0.1f, patchLifetime);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(elapsed / lifetime);
        float expand = Mathf.Clamp01(normalized / 0.12f);
        transform.localScale = Vector3.Lerp(finalScale * 0.65f, finalScale, expand);

        if (spriteRenderer != null && normalized >= 0.55f)
        {
            float fade = 1f - Mathf.InverseLerp(0.55f, 1f, normalized);
            Color color = initialColor;
            color.a = initialColor.a * fade;
            spriteRenderer.color = color;
        }

        if (normalized >= 1f)
            Destroy(gameObject);
    }
}
