using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IngredientInventoryHud : MonoBehaviour
{
    [SerializeField] private IngredientInventory inventory;
    [SerializeField] private float rotationInterval = 2.5f;
    [SerializeField] private Image slotImage;
    [SerializeField] private Image ingredientImage;
    [SerializeField] private Text amountText;

    private float nextRotationAt;
    private int currentIndex;

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
        if (inventory != null)
            inventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void Update()
    {
        if (inventory == null || Time.time < nextRotationAt)
            return;

        IReadOnlyList<IngredientInventory.IngredientStack> stacks = inventory.GetStacks();
        if (stacks.Count > 0)
            currentIndex = (currentIndex + 1) % stacks.Count;

        nextRotationAt = Time.time + rotationInterval;
        Refresh();
    }

    public void Initialize(IngredientInventory newInventory)
    {
        if (inventory != null)
            inventory.InventoryChanged -= HandleInventoryChanged;

        inventory = newInventory;
        currentIndex = 0;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (inventory == null)
            return;

        inventory.InventoryChanged -= HandleInventoryChanged;
        inventory.InventoryChanged += HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        currentIndex = 0;
        nextRotationAt = Time.time + rotationInterval;
        Refresh();
    }

    private void Refresh()
    {
        if (ingredientImage == null || amountText == null)
            return;

        IReadOnlyList<IngredientInventory.IngredientStack> stacks = inventory != null
            ? inventory.GetStacks()
            : System.Array.Empty<IngredientInventory.IngredientStack>();

        if (stacks.Count == 0)
        {
            ingredientImage.enabled = false;
            amountText.enabled = false;
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, stacks.Count - 1);
        IngredientInventory.IngredientStack stack = stacks[currentIndex];

        ingredientImage.enabled = stack.Ingredient != null && stack.Ingredient.Icon != null;
        ingredientImage.sprite = stack.Ingredient != null ? stack.Ingredient.Icon : null;

        amountText.enabled = true;
        amountText.text = $"x{stack.Count}";
    }

    private void BuildFallbackHud()
    {
        if (slotImage != null && ingredientImage != null && amountText != null)
            return;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        GameObject slot = new("IngredientSlot");
        slot.transform.SetParent(transform, false);
        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(1f, 0.5f);
        slotRect.anchorMax = new Vector2(1f, 0.5f);
        slotRect.pivot = new Vector2(1f, 0.5f);
        slotRect.anchoredPosition = new Vector2(-28f, 0f);
        slotRect.sizeDelta = new Vector2(104f, 104f);

        slotImage = slot.AddComponent<Image>();
        slotImage.color = new Color(0.03f, 0.03f, 0.03f, 0.78f);

        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject icon = new("IngredientIcon");
        icon.transform.SetParent(slot.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(70f, 70f);

        ingredientImage = icon.AddComponent<Image>();
        ingredientImage.preserveAspect = true;
        ingredientImage.raycastTarget = false;

        GameObject count = new("IngredientAmount");
        count.transform.SetParent(slot.transform, false);
        RectTransform countRect = count.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-8f, 6f);
        countRect.sizeDelta = new Vector2(56f, 28f);

        amountText = count.AddComponent<Text>();
        amountText.alignment = TextAnchor.LowerRight;
        amountText.color = Color.white;
        amountText.fontSize = 22;
        amountText.fontStyle = FontStyle.Bold;
        amountText.raycastTarget = false;
        amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (amountText.font == null)
            amountText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
