using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    private const float Duration = 0.48f;
    private const int SparkCount = 16;

    private readonly List<SpriteRenderer> ghosts = new();
    private readonly List<Vector3> ghostInitialScales = new();
    private readonly List<Spark> sparks = new();
    private float elapsed;
    private static Sprite solidParticleSprite;
    private static readonly Dictionary<Sprite, Color[]> SpritePalettes = new();

    private sealed class Spark
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float Spin;
        public Vector3 InitialScale;
    }

    public static void Spawn(EnemyDeathNotifier enemy)
    {
        if (enemy == null)
            return;

        SpriteRenderer[] sources = enemy.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer primary = null;
        foreach (SpriteRenderer source in sources)
        {
            if (source != null && source.enabled && source.sprite != null)
            {
                primary = source;
                break;
            }
        }

        if (primary == null)
            return;

        GameObject effectObject = new("Enemy Death Effect");
        effectObject.transform.position = enemy.transform.position;
        EnemyDeathEffect effect = effectObject.AddComponent<EnemyDeathEffect>();
        effect.Initialize(sources, primary);
    }

    private void Initialize(SpriteRenderer[] sources, SpriteRenderer primary)
    {
        foreach (SpriteRenderer source in sources)
        {
            if (source == null || !source.enabled || source.sprite == null)
                continue;

            GameObject ghostObject = new("Death Flash");
            Transform ghostTransform = ghostObject.transform;
            ghostTransform.SetParent(transform, true);
            ghostTransform.position = source.transform.position;
            ghostTransform.rotation = source.transform.rotation;
            ghostTransform.localScale = source.transform.lossyScale;

            SpriteRenderer ghost = ghostObject.AddComponent<SpriteRenderer>();
            ghost.sprite = source.sprite;
            ghost.flipX = source.flipX;
            ghost.flipY = source.flipY;
            ghost.sharedMaterial = source.sharedMaterial;
            ghost.sortingLayerID = source.sortingLayerID;
            ghost.sortingOrder = source.sortingOrder + 2;
            ghost.color = Color.white;
            ghosts.Add(ghost);
            ghostInitialScales.Add(ghostTransform.localScale);
        }

        Color[] palette = GetSpritePalette(primary);
        for (int i = 0; i < SparkCount; i++)
            CreateSpark(primary, palette, i);
    }

    private void CreateSpark(SpriteRenderer source, Color[] palette, int index)
    {
        GameObject sparkObject = new("Death Spark");
        Transform sparkTransform = sparkObject.transform;
        sparkTransform.SetParent(transform, false);
        sparkTransform.position = source.bounds.center;
        sparkTransform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        SpriteRenderer sparkRenderer = sparkObject.AddComponent<SpriteRenderer>();
        sparkRenderer.sprite = GetSolidParticleSprite();
        sparkRenderer.sharedMaterial = source.sharedMaterial;
        sparkRenderer.sortingLayerID = source.sortingLayerID;
        sparkRenderer.sortingOrder = source.sortingOrder + 3;
        sparkRenderer.color = palette[Random.Range(0, palette.Length)];

        float size = Random.Range(0.16f, 0.28f);
        Vector3 initialScale = new(size * Random.Range(0.75f, 1.25f), size, 1f);
        sparkTransform.localScale = initialScale;
        float angle = (360f / SparkCount) * index + Random.Range(-12f, 12f);
        Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;

        sparks.Add(new Spark
        {
            Transform = sparkTransform,
            Renderer = sparkRenderer,
            Velocity = direction * Random.Range(2.6f, 4.8f),
            Spin = Random.Range(-540f, 540f),
            InitialScale = initialScale
        });
    }

    private static Color[] GetSpritePalette(SpriteRenderer source)
    {
        Sprite sprite = source.sprite;
        if (SpritePalettes.TryGetValue(sprite, out Color[] cached))
            return cached;

        List<Color> colors = new();
        const int sampleResolution = 64;
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            sampleResolution,
            sampleResolution,
            0,
            RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D readable = null;

        try
        {
            Graphics.Blit(sprite.texture, renderTexture);
            RenderTexture.active = renderTexture;
            readable = new Texture2D(sampleResolution, sampleResolution, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, sampleResolution, sampleResolution), 0, 0);
            readable.Apply();

            Vector2 uvMin = Vector2.one;
            Vector2 uvMax = Vector2.zero;
            foreach (Vector2 uv in sprite.uv)
            {
                uvMin = Vector2.Min(uvMin, uv);
                uvMax = Vector2.Max(uvMax, uv);
            }

            for (int attempt = 0; attempt < 80 && colors.Count < 10; attempt++)
            {
                float u = Random.Range(uvMin.x, uvMax.x);
                float v = Random.Range(uvMin.y, uvMax.y);
                Color sampled = readable.GetPixelBilinear(u, v) * source.color;
                float brightness = Mathf.Max(sampled.r, Mathf.Max(sampled.g, sampled.b));
                if (sampled.a < 0.35f || brightness < 0.12f)
                    continue;

                sampled.a = 1f;
                colors.Add(sampled);
            }
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            if (readable != null)
                Destroy(readable);
        }

        if (colors.Count == 0)
        {
            Color fallback = source.color;
            fallback.a = 1f;
            colors.Add(fallback);
        }

        Color[] palette = colors.ToArray();
        SpritePalettes[sprite] = palette;
        return palette;
    }

    private static Sprite GetSolidParticleSprite()
    {
        if (solidParticleSprite != null)
            return solidParticleSprite;

        const int resolution = 24;
        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Solid Death Particle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = Color.clear;
        Color solid = Color.white;
        Vector2 center = Vector2.one * ((resolution - 1) * 0.5f);
        float radius = resolution * 0.46f;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? solid : clear);
        }

        texture.Apply(false, true);
        solidParticleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution);
        solidParticleSprite.name = "Solid Death Particle";
        return solidParticleSprite;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Duration);
        float ghostScale = progress < 0.28f
            ? Mathf.Lerp(1f, 1.3f, progress / 0.28f)
            : Mathf.Lerp(1.3f, 0.35f, (progress - 0.28f) / 0.72f);

        for (int i = 0; i < ghosts.Count; i++)
        {
            SpriteRenderer ghost = ghosts[i];
            if (ghost == null)
                continue;

            ghost.transform.localScale = ghostInitialScales[i] * ghostScale;
            Color color = Color.white;
            color.a = 1f - Mathf.SmoothStep(0f, 1f, progress);
            ghost.color = color;
        }

        foreach (Spark spark in sparks)
        {
            if (spark.Transform == null)
                continue;

            spark.Velocity += Vector3.down * (7f * Time.deltaTime);
            spark.Transform.position += spark.Velocity * Time.deltaTime;
            spark.Transform.Rotate(0f, 0f, spark.Spin * Time.deltaTime);
            float sparkScale = progress < 0.16f
                ? Mathf.Lerp(0.65f, 1.15f, progress / 0.16f)
                : Mathf.Lerp(1.15f, 0f, (progress - 0.16f) / 0.84f);
            spark.Transform.localScale = spark.InitialScale * sparkScale;
            Color color = spark.Renderer.color;
            color.a = 1f - progress;
            spark.Renderer.color = color;
        }

        if (progress >= 1f)
            Destroy(gameObject);
    }
}
