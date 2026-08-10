using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthHud : MonoBehaviour
{
    private const float LowHealthThreshold = 0.35f;

    [SerializeField] private Health playerHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text healthText;

    private Image lowHealthVignette;
    private Color normalFillColor;
    private Color normalTextColor;
    private bool lowHealthMusicActive;

    private void Awake()
    {
        BuildFallbackHud();
        normalFillColor = fillImage != null ? fillImage.color : new Color(0.1f, 0.8f, 0.25f);
        normalTextColor = healthText != null ? healthText.color : Color.white;
        BuildLowHealthVignette();
    }

    private void Update()
    {
        UpdateLowHealthEffect();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= HandleHealthChanged;

        if (lowHealthMusicActive)
        {
            lowHealthMusicActive = false;
            GameAudio.SetLowHealthMusicIntensity(0f);
        }
    }

    public void Initialize(Health health)
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= HandleHealthChanged;

        playerHealth = health;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (playerHealth == null)
            return;

        playerHealth.HealthChanged -= HandleHealthChanged;
        playerHealth.HealthChanged += HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (playerHealth == null)
            return;

        float max = Mathf.Max(1f, playerHealth.MaxHealth);
        float current = Mathf.Clamp(playerHealth.CurrentHealth, 0f, max);

        if (fillImage != null)
        {
            float normalizedHealth = current / max;
            fillImage.fillAmount = normalizedHealth;
            bool lowHealth = normalizedHealth > 0f && normalizedHealth <= LowHealthThreshold;
            fillImage.color = lowHealth ? new Color(0.95f, 0.04f, 0.03f, 1f) : normalFillColor;

            // The fallback Image has no source sprite, so Unity's Filled mode
            // may continue drawing it as a full rectangle. Scaling its actual
            // rect from a left-side pivot makes the green bar reliable.
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            Vector3 scale = fillRect.localScale;
            scale.x = normalizedHealth;
            fillRect.localScale = scale;
        }

        if (healthText != null)
        {
            healthText.text = $"Vida {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            float normalizedHealth = current / max;
            healthText.color = normalizedHealth > 0f && normalizedHealth <= LowHealthThreshold
                ? new Color(1f, 0.18f, 0.12f, 1f)
                : normalTextColor;
        }
    }

    private void BuildFallbackHud()
    {
        if (fillImage != null && healthText != null)
            return;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new("HealthPanel");
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(320f, 44f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fill = new("HealthFill");
        fill.transform.SetParent(panel.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.1f, 0.8f, 0.25f);
        fillImage.type = Image.Type.Simple;

        GameObject label = new("HealthText");
        label.transform.SetParent(panel.transform, false);
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        healthText = label.AddComponent<Text>();
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        healthText.fontSize = 20;
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (healthText.font == null)
            healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void BuildLowHealthVignette()
    {
        if (lowHealthVignette != null)
            return;

        GameObject vignette = new("Low Health Vignette");
        vignette.transform.SetParent(transform, false);
        vignette.transform.SetAsFirstSibling();

        RectTransform rect = vignette.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        lowHealthVignette = vignette.AddComponent<Image>();
        lowHealthVignette.sprite = CreateVignetteSprite();
        lowHealthVignette.color = new Color(1f, 1f, 1f, 0f);
        lowHealthVignette.raycastTarget = false;
    }

    private void UpdateLowHealthEffect()
    {
        if (lowHealthVignette == null || playerHealth == null)
            return;

        float normalized = playerHealth.MaxHealth > 0f
            ? playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;
        if (normalized <= 0f || normalized > LowHealthThreshold)
        {
            lowHealthVignette.color = new Color(1f, 1f, 1f, 0f);
            SetLowHealthMusic(0f);
            return;
        }

        float danger = 1f - Mathf.Clamp01(normalized / LowHealthThreshold);
        SetLowHealthMusic(danger);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2.8f);
        float alpha = Mathf.Lerp(0.28f, 0.68f, pulse) * Mathf.Lerp(0.65f, 1f, danger);
        lowHealthVignette.color = new Color(1f, 1f, 1f, alpha);
    }

    private static Sprite CreateVignetteSprite()
    {
        const int width = 256;
        const int height = 144;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            name = "Low Health Red Vignette",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < height; y++)
        {
            float ny = Mathf.Abs((y + 0.5f) / height * 2f - 1f);
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / width * 2f - 1f);
                float edge = Mathf.Max(nx, ny);
                float corner = Mathf.Sqrt(nx * nx + ny * ny) * 0.72f;
                float alpha = Mathf.SmoothStep(0.78f, 1f, Mathf.Max(edge, corner));
                texture.SetPixel(x, y, new Color(0.45f, 0f, 0f, alpha));
            }
        }

        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void SetLowHealthMusic(float intensity)
    {
        bool active = intensity > 0f;
        if (lowHealthMusicActive == active)
        {
            if (active)
                GameAudio.SetLowHealthMusicIntensity(intensity);
            return;
        }

        lowHealthMusicActive = active;
        GameAudio.SetLowHealthMusicIntensity(active ? intensity : 0f);
    }
}
