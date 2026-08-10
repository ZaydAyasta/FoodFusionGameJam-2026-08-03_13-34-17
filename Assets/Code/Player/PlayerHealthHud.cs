using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthHud : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text healthText;

    private void Awake()
    {
        BuildFallbackHud();
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
            healthText.text = $"Vida {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
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
}
