using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FusionKitchenHud : MonoBehaviour
{
    private const string MaxHealthMilkId = "maxHealthMilk";
    private const string MilkshakeId = "milkshake";
    private const string BologneseId = "bolognese";
    private const string SandwitchId = "sandwitch";
    private const string WaterId = "water";
    private const string EnergyBitesId = "energyBites";
    private const string FruitRiceId = "fruitRice";
    private const string CheeseSteakId = "cheeseSteak";
    private const string RisottoId = "risotto";
    private const float MaxHealthMilkBonus = 50f;
    private const float MaxHealthCap = 250f;
    private const float MilkshakeSpeedBonus = 1f;
    private const float BologneseDamageBonus = 1f;
    private const float SandwitchCooldownMultiplier = 0.8f;
    private const float WaterHealAmount = 20f;
    private const float ProjectileRangeBonus = 1.5f;
    private const float DamageReductionBonus = 0.1f;
    private const float DamageReductionCap = 0.5f;

    private static FusionKitchenHud instance;

    private IngredientInventory inventory;
    private readonly IngredientData[] selectedIngredients = new IngredientData[2];
    private readonly Image[] selectedSlotIcons = new Image[2];
    private readonly Text[] selectedSlotCounts = new Text[2];
    private Image resultIcon;
    private Text resultCount;
    private Text descriptionText;
    private Transform inventoryGrid;
    private Button consumeButton;
    private GameObject root;
    private int selectedSlotIndex;
    private bool open;
    private bool suppressInventoryRefresh;
    private bool inventoryGridDirty;

    public static void Open(IngredientInventory sourceInventory)
    {
        sourceInventory = ResolveBestInventory(sourceInventory);
        if (sourceInventory == null)
            return;

        if (!IsUsable(instance))
            instance = new GameObject("FusionKitchenHUD").AddComponent<FusionKitchenHud>();

        instance.OpenInternal(sourceInventory);
    }

    private void Awake()
    {
        if (IsUsable(instance) && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUi();
        EnsureEventSystem();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (!open || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void LateUpdate()
    {
        if (!open)
            return;

        if (!inventoryGridDirty && !InventoryGridNeedsRepaint())
            return;

        inventoryGridDirty = false;
        RefreshInventoryGrid();
        Canvas.ForceUpdateCanvases();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void OpenInternal(IngredientInventory sourceInventory)
    {
        EnsureUiReferences();
        SetInventory(ResolveBestInventory(sourceInventory));

        selectedSlotIndex = 0;
        open = true;
        inventoryGridDirty = true;
        Time.timeScale = 0f;
        root.SetActive(true);
        RefreshAll();
    }

    private void EnsureUiReferences()
    {
        if (IsUsable(root) && IsUsable(inventoryGrid) && IsUsable(consumeButton) && IsUsable(descriptionText))
            return;

        bool shouldStayOpen = open;
        BuildUi();
        root.SetActive(shouldStayOpen);
    }

    private void Close()
    {
        open = false;
        root.SetActive(false);
        Time.timeScale = 1f;
    }

    private void HandleInventoryChanged()
    {
        if (open && !suppressInventoryRefresh)
            RefreshAll();
    }

    private void RefreshAll()
    {
        EnsureUiReferences();
        if (open && IsUsable(root))
            root.SetActive(true);

        ValidateSelectedIngredients();
        RefreshSelectedSlots();
        RefreshResult();
        RefreshInventoryGrid();
        inventoryGridDirty = false;
        Canvas.ForceUpdateCanvases();
    }

    private void SetInventory(IngredientInventory newInventory)
    {
        if (newInventory == null || inventory == newInventory)
            return;

        if (inventory != null)
            inventory.InventoryChanged -= HandleInventoryChanged;

        inventory = newInventory;
        inventory.InventoryChanged += HandleInventoryChanged;
        inventory.SetAsActivePlayerInventory();
    }

    private void ValidateSelectedIngredients()
    {
        for (int i = 0; i < selectedIngredients.Length; i++)
        {
            IngredientData ingredient = selectedIngredients[i];
            if (ingredient != null && inventory.GetCount(ingredient) < GetSelectedCount(ingredient))
                selectedIngredients[i] = null;
        }
    }

    private int GetSelectedCount(IngredientData ingredient)
    {
        if (ingredient == null)
            return 0;

        int count = 0;
        for (int i = 0; i < selectedIngredients.Length; i++)
        {
            if (selectedIngredients[i] == ingredient)
                count++;
        }

        return count;
    }

    private void SelectInputSlot(int index)
    {
        selectedSlotIndex = Mathf.Clamp(index, 0, selectedIngredients.Length - 1);
        RefreshSelectedSlots();
    }

    private void SelectInventoryIngredient(IngredientData ingredient)
    {
        if (ingredient == null || inventory == null)
            return;

        int alreadySelected = GetSelectedCount(ingredient);
        if (inventory.GetCount(ingredient) <= alreadySelected)
            return;

        selectedIngredients[selectedSlotIndex] = ingredient;
        selectedSlotIndex = selectedSlotIndex == 0 ? 1 : 0;
        RefreshAll();
    }

    private void ClearInputSlot(int index)
    {
        selectedIngredients[index] = null;
        selectedSlotIndex = Mathf.Clamp(index, 0, selectedIngredients.Length - 1);
        RefreshAll();
    }

    private void ConsumeFusion()
    {
        IngredientData result = GetFusionResult();
        if (result == null || selectedIngredients[0] == null || selectedIngredients[1] == null)
            return;

        IngredientData first = selectedIngredients[0];
        IngredientData second = selectedIngredients[1];
        if (inventory.GetCount(first) <= 0 || inventory.GetCount(second) <= (first == second ? 1 : 0))
            return;

        suppressInventoryRefresh = true;
        bool consumed = false;
        try
        {
            if (!inventory.RemoveIngredient(first))
                return;

            if (!inventory.RemoveIngredient(second))
            {
                inventory.AddIngredient(first);
                return;
            }

            inventory.AddIngredient(result);
            consumed = true;
        }
        finally
        {
            suppressInventoryRefresh = false;
        }

        if (!consumed)
        {
            RefreshAll();
            return;
        }

        selectedIngredients[0] = null;
        selectedIngredients[1] = null;
        selectedSlotIndex = 0;
        ApplyFusionResultEffect(result);
        GameAudio.PlayCrunch();
        RefreshAll();
    }

    private IngredientData GetFusionResult()
    {
        IngredientData first = selectedIngredients[0];
        IngredientData second = selectedIngredients[1];
        if (first == null || second == null)
            return null;

        IngredientData recipeResult = GetRecipeResult(first, second);
        if (recipeResult != null)
            return recipeResult;

        return LoadIngredientById(WaterId) ?? first;
    }

    private IngredientData GetRecipeResult(IngredientData first, IngredientData second)
    {
        if (IsRecipePair(first, second, "drop", "drop_0", "Ingredient_Drop", "dropMilk", "dropMilk_0", "Ingredient_DropMilk"))
            return LoadIngredientById(MaxHealthMilkId) ?? LoadIngredientById("dropMilk") ?? second;

        if (IsRecipePair(first, second, "dropMilk", "dropMilk_0", "Ingredient_DropMilk", "manzana", "MANZANAAA_0", "Ingredient_Manzana"))
            return LoadIngredientById(MilkshakeId) ?? LoadIngredientById("dropMilk") ?? first;

        if (IsRecipePair(first, second, "meat", "Meat_0", "Ingredient_Meat", "noodle", "noodle_0", "Ingredient_Noodle")
            || IsRecipePair(first, second, "fishdrop", "fishdrop_0", "Ingredient_Fishdrop", "noodle", "noodle_0", "Ingredient_Noodle"))
        {
            return LoadIngredientById(BologneseId) ?? LoadIngredientById("noodle") ?? second;
        }

        if (IsRecipePair(first, second, "bread", "bread_0", "Ingredient_Bread", "meat", "Meat_0", "Ingredient_Meat")
            || IsRecipePair(first, second, "bread", "bread_0", "Ingredient_Bread", "fishdrop", "fishdrop_0", "Ingredient_Fishdrop"))
        {
            return LoadIngredientById(SandwitchId) ?? LoadIngredientById("meat") ?? second;
        }

        if (IsRecipePair(first, second, "drop", "drop_0", "Ingredient_Drop", "manzana", "MANZANAAA_0", "Ingredient_Manzana"))
            return LoadIngredientById(EnergyBitesId) ?? first;

        if (IsRecipePair(first, second, "rice", "rice_0", "Ingredient_Rice", "manzana", "MANZANAAA_0", "Ingredient_Manzana"))
            return LoadIngredientById(FruitRiceId) ?? first;

        if (IsRecipePair(first, second, "cheese", "cheese_0", "Ingredient_Cheese", "meat", "Meat_0", "Ingredient_Meat"))
            return LoadIngredientById(CheeseSteakId) ?? first;

        if (IsRecipePair(first, second, "rice", "rice_0", "Ingredient_Rice", "cheese", "cheese_0", "Ingredient_Cheese"))
            return LoadIngredientById(RisottoId) ?? first;

        return null;
    }

    private static bool IsRecipePair(
        IngredientData first,
        IngredientData second,
        string firstId,
        string firstSpriteId,
        string firstAssetName,
        string secondId,
        string secondSpriteId,
        string secondAssetName)
    {
        return MatchesIngredient(first, firstId, firstSpriteId, firstAssetName) && MatchesIngredient(second, secondId, secondSpriteId, secondAssetName)
            || MatchesIngredient(second, firstId, firstSpriteId, firstAssetName) && MatchesIngredient(first, secondId, secondSpriteId, secondAssetName);
    }

    private static bool MatchesIngredient(IngredientData ingredient, params string[] ids)
    {
        if (ingredient == null)
            return false;

        string id = ingredient.Id;
        string assetName = ingredient.name;
        string iconName = ingredient.Icon != null ? ingredient.Icon.name : string.Empty;
        foreach (string candidate in ids)
        {
            if (string.Equals(id, candidate, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(assetName, candidate, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(iconName, candidate, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IngredientData LoadIngredientById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/GameData/Ingredients" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IngredientData ingredient = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
            if (MatchesIngredient(ingredient, id))
                return ingredient;
        }
#endif

        IngredientData[] resourceIngredients = Resources.LoadAll<IngredientData>("Ingredients");
        foreach (IngredientData ingredient in resourceIngredients)
        {
            if (MatchesIngredient(ingredient, id))
                return ingredient;
        }

        IngredientInventory bestInventory = ResolveBestInventory(null);
        if (bestInventory == null)
            return null;

        IReadOnlyList<IngredientInventory.IngredientStack> stacks = bestInventory.GetStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            if (MatchesIngredient(stacks[i].Ingredient, id))
                return stacks[i].Ingredient;
        }

        return null;
    }

    private void ApplyFusionResultEffect(IngredientData result)
    {
        if (result == null)
            return;

        Health playerHealth = GetPlayerComponent<Health>();
        if (MatchesIngredient(result, MaxHealthMilkId))
        {
            playerHealth?.IncreaseMaxHealthUpTo(MaxHealthMilkBonus, MaxHealthCap, true);
            return;
        }

        if (MatchesIngredient(result, WaterId))
        {
            playerHealth?.Heal(GetCurrentWaterHealAmount());
            return;
        }

        if (MatchesIngredient(result, MilkshakeId))
        {
            GetPlayerComponent<CharacterMovement>()?.AddSpeedBonus(MilkshakeSpeedBonus);
            return;
        }

        if (MatchesIngredient(result, BologneseId))
        {
            GetPlayerComponent<PlayerAttack>()?.AddDamageBonus(BologneseDamageBonus);
            return;
        }

        if (MatchesIngredient(result, SandwitchId))
        {
            GetPlayerComponent<PlayerAttack>()?.MultiplyCooldown(SandwitchCooldownMultiplier);
            return;
        }

        if (MatchesIngredient(result, EnergyBitesId))
        {
            GetPlayerComponent<PlayerDash>()?.AddDashCharge();
            return;
        }

        if (MatchesIngredient(result, FruitRiceId))
        {
            GetPlayerComponent<PlayerAttack>()?.AddRangeBonus(ProjectileRangeBonus);
            return;
        }

        if (MatchesIngredient(result, CheeseSteakId))
        {
            playerHealth?.AddDamageReduction(DamageReductionBonus, DamageReductionCap);
            return;
        }

        if (MatchesIngredient(result, RisottoId))
            GetPlayerComponent<PlayerAttack>()?.EnableAutoAim();
    }

    private static float GetCurrentWaterHealAmount()
    {
        RoomGenerationTestBootstrap roomGeneration = FindAnyObjectByType<RoomGenerationTestBootstrap>();
        int penaltyCount = roomGeneration != null ? roomGeneration.GetSoupHealingPenaltyCount() : 0;
        return Mathf.Max(5f, WaterHealAmount - penaltyCount * 5f);
    }

    private T GetPlayerComponent<T>() where T : Component
    {
        T component = inventory != null ? inventory.GetComponentInParent<T>() : null;
        if (component != null)
            return component;

        CharacterInput playerInput = FindAnyObjectByType<CharacterInput>();
        return playerInput != null ? playerInput.GetComponentInParent<T>() : null;
    }

    private void RefreshSelectedSlots()
    {
        for (int i = 0; i < selectedSlotIcons.Length; i++)
        {
            IngredientData ingredient = selectedIngredients[i];
            selectedSlotIcons[i].sprite = ingredient != null ? ingredient.Icon : null;
            selectedSlotIcons[i].enabled = ingredient != null && ingredient.Icon != null;
            selectedSlotCounts[i].enabled = ingredient != null;
            selectedSlotCounts[i].text = ingredient != null ? "x1" : string.Empty;
        }
    }

    private void RefreshResult()
    {
        IngredientData result = GetFusionResult();
        resultIcon.sprite = result != null ? result.Icon : null;
        resultIcon.enabled = result != null && result.Icon != null;
        resultCount.enabled = result != null;
        resultCount.text = result != null ? "x1" : string.Empty;

        if (result == null)
        {
            descriptionText.text = "Selecciona dos ingredientes para ver la fusion.";
            consumeButton.interactable = false;
            return;
        }

        string name = GetIngredientName(result);
        string description = string.IsNullOrWhiteSpace(result.Description) ? "Sin descripcion." : result.Description;
        descriptionText.text = $"{name}\n\n{description}";
        consumeButton.interactable = selectedIngredients[0] != null && selectedIngredients[1] != null;
    }

    private void RefreshInventoryGrid()
    {
        Transform grid = GetInventoryGrid();
        if (grid == null)
            return;

        List<IngredientInventory.IngredientStack> stacks = GetVisibleInventoryStacks();
        SetEmptyInventoryTextVisible(grid, stacks.Count == 0);

        if (stacks.Count == 0)
        {
            SetExtraInventoryItemsInactive(grid, 0);
            return;
        }

        for (int i = 0; i < stacks.Count; i++)
            ConfigureInventoryItemButton(GetOrCreateInventoryItemButton(grid, i), stacks[i]);

        SetExtraInventoryItemsInactive(grid, stacks.Count);
    }

    private bool InventoryGridNeedsRepaint()
    {
        Transform grid = GetInventoryGrid();
        if (grid == null)
            return false;

        int expectedItems = GetVisibleInventoryStacks().Count;
        if (expectedItems == 0)
            return grid.Find("EmptyInventoryText") == null;

        return CountActiveInventoryItems(grid) < expectedItems;
    }

    private Transform GetInventoryGrid()
    {
        Transform grid = transform.Find("FusionMenu/FusionPanel/FusionInventory/ItemGrid");
        if (grid != null)
        {
            inventoryGrid = grid;
            return grid;
        }

        BuildUi();
        grid = transform.Find("FusionMenu/FusionPanel/FusionInventory/ItemGrid");
        inventoryGrid = grid;
        return grid;
    }

    private void SetEmptyInventoryTextVisible(Transform grid, bool visible)
    {
        Transform existing = grid.Find("EmptyInventoryText");
        Text empty = existing != null ? existing.GetComponent<Text>() : null;
        if (empty == null)
        {
            empty = CreateText("EmptyInventoryText", grid, "Sin ingredientes", 22, TextAnchor.MiddleLeft);
            RectTransform emptyRect = empty.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0f, 1f);
            emptyRect.anchorMax = new Vector2(1f, 1f);
            emptyRect.pivot = new Vector2(0f, 1f);
            emptyRect.anchoredPosition = Vector2.zero;
            emptyRect.sizeDelta = new Vector2(600f, 54f);
        }

        empty.gameObject.SetActive(visible);
    }

    private GameObject GetOrCreateInventoryItemButton(Transform grid, int itemIndex)
    {
        int found = 0;
        for (int i = 0; i < grid.childCount; i++)
        {
            Transform child = grid.GetChild(i);
            if (child.name != "FusionInventoryItem")
                continue;

            if (found == itemIndex)
                return child.gameObject;

            found++;
        }

        return CreateInventoryItemButton(grid);
    }

    private static void SetExtraInventoryItemsInactive(Transform grid, int activeCount)
    {
        int found = 0;
        for (int i = 0; i < grid.childCount; i++)
        {
            Transform child = grid.GetChild(i);
            if (child.name != "FusionInventoryItem")
                continue;

            child.gameObject.SetActive(found < activeCount);
            found++;
        }
    }

    private static int CountActiveInventoryItems(Transform grid)
    {
        int count = 0;
        for (int i = 0; i < grid.childCount; i++)
        {
            Transform child = grid.GetChild(i);
            if (child.name == "FusionInventoryItem" && child.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private GameObject CreateInventoryItemButton(Transform grid)
    {
        GameObject item = new("FusionInventoryItem");
        item.transform.SetParent(grid, false);
        RectTransform rect = item.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64f, 64f);

        Image background = item.AddComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

        Outline outline = item.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        item.AddComponent<Button>();

        GameObject iconObject = new("Icon");
        iconObject.transform.SetParent(item.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(52f, 52f);

        Image icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        Text count = CreateText("Count", item.transform, string.Empty, 18, TextAnchor.LowerRight);
        RectTransform countRect = count.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = new Vector2(2f, 2f);
        countRect.offsetMax = new Vector2(-4f, -4f);
        return item;
    }

    private void ConfigureInventoryItemButton(GameObject item, IngredientInventory.IngredientStack stack)
    {
        item.SetActive(true);
        IngredientData ingredient = stack.Ingredient;

        Button button = item.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectInventoryIngredient(ingredient));

        Image icon = item.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = ingredient != null ? ingredient.Icon : null;
            icon.enabled = icon.sprite != null;
        }

        Text count = item.transform.Find("Count")?.GetComponent<Text>();
        if (count != null)
            count.text = $"x{stack.Count}";
    }

    private List<IngredientInventory.IngredientStack> GetVisibleInventoryStacks()
    {
        List<IngredientInventory.IngredientStack> visibleStacks = new();
        if (inventory == null)
            SetInventory(ResolveBestInventory(null));

        IReadOnlyList<IngredientInventory.IngredientStack> sourceStacks = inventory != null
            ? inventory.GetStacks()
            : System.Array.Empty<IngredientInventory.IngredientStack>();
        for (int i = 0; i < sourceStacks.Count; i++)
        {
            if (sourceStacks[i].Ingredient != null && sourceStacks[i].Count > 0)
                visibleStacks.Add(sourceStacks[i]);
        }

        return visibleStacks;
    }

    private void BuildUi()
    {
        if (IsUsable(root))
            Destroy(root);

        root = null;
        inventoryGrid = null;
        resultIcon = null;
        resultCount = null;
        descriptionText = null;
        consumeButton = null;
        selectedSlotIcons[0] = null;
        selectedSlotIcons[1] = null;
        selectedSlotCounts[0] = null;
        selectedSlotCounts[1] = null;

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 360;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        root = CreateRoot("FusionMenu");
        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.32f);

        GameObject panel = CreatePanel("FusionPanel", root.transform, new Vector2(1000f, 560f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Button closeButton = CreateButton("CloseButton", panel.transform, "X", new Vector2(48f, 48f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeButton.onClick.AddListener(Close);

        CreateFormula(panel.transform);
        CreateInventoryArea(panel.transform);
        CreateDescriptionArea(panel.transform);
    }

    private void CreateFormula(Transform parent)
    {
        CreateFusionSlot(parent, 0, new Vector2(-340f, 135f));
        CreateTextAt(parent, "+", 48, new Vector2(-215f, 138f), new Vector2(54f, 70f));
        CreateFusionSlot(parent, 1, new Vector2(-95f, 135f));
        CreateTextAt(parent, "=", 48, new Vector2(30f, 138f), new Vector2(54f, 70f));
        CreateResultSlot(parent, new Vector2(150f, 135f));
    }

    private void CreateFusionSlot(Transform parent, int index, Vector2 position)
    {
        Button slot = CreateButton($"InputSlot_{index + 1}", parent, string.Empty, new Vector2(92f, 92f));
        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        slot.onClick.AddListener(() => SelectInputSlot(index));

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);

        Button clear = CreateButton("Clear", slot.transform, "-", new Vector2(26f, 26f));
        RectTransform clearRect = clear.GetComponent<RectTransform>();
        clearRect.anchorMin = new Vector2(1f, 1f);
        clearRect.anchorMax = new Vector2(1f, 1f);
        clearRect.pivot = new Vector2(1f, 1f);
        clearRect.anchoredPosition = new Vector2(-4f, -4f);
        clear.onClick.AddListener(() => ClearInputSlot(index));

        GameObject iconObject = new("Icon");
        iconObject.transform.SetParent(slot.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(72f, 72f);
        selectedSlotIcons[index] = iconObject.AddComponent<Image>();
        selectedSlotIcons[index].preserveAspect = true;
        selectedSlotIcons[index].raycastTarget = false;

        selectedSlotCounts[index] = CreateText("Count", slot.transform, string.Empty, 18, TextAnchor.LowerRight);
        RectTransform countRect = selectedSlotCounts[index].GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = new Vector2(4f, 4f);
        countRect.offsetMax = new Vector2(-6f, -6f);
    }

    private void CreateResultSlot(Transform parent, Vector2 position)
    {
        GameObject slot = CreatePanel("ResultSlot", parent, new Vector2(92f, 92f));
        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        GameObject iconObject = new("Icon");
        iconObject.transform.SetParent(slot.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(72f, 72f);

        resultIcon = iconObject.AddComponent<Image>();
        resultIcon.preserveAspect = true;
        resultIcon.raycastTarget = false;

        resultCount = CreateText("Count", slot.transform, string.Empty, 18, TextAnchor.LowerRight);
        RectTransform countRect = resultCount.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = new Vector2(4f, 4f);
        countRect.offsetMax = new Vector2(-6f, -6f);
    }

    private void CreateInventoryArea(Transform parent)
    {
        GameObject area = CreatePanel("FusionInventory", parent, new Vector2(690f, 230f));
        RectTransform areaRect = area.GetComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0.5f, 0.5f);
        areaRect.anchorMax = new Vector2(0.5f, 0.5f);
        areaRect.anchoredPosition = new Vector2(-150f, -130f);

        Text label = CreateText("Label", area.transform, "INV", 26, TextAnchor.UpperLeft);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(18f, -10f);
        labelRect.sizeDelta = new Vector2(-36f, 40f);

        GameObject grid = new("ItemGrid");
        grid.transform.SetParent(area.transform, false);
        inventoryGrid = grid.transform;
        RectTransform gridRect = grid.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 1f);
        gridRect.anchoredPosition = new Vector2(24f, -58f);
        gridRect.sizeDelta = new Vector2(630f, 150f);

        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(64f, 64f);
        layout.spacing = new Vector2(12f, 12f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 8;
    }

    private void CreateDescriptionArea(Transform parent)
    {
        GameObject area = CreatePanel("DescriptionPanel", parent, new Vector2(250f, 360f));
        RectTransform areaRect = area.GetComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0.5f, 0.5f);
        areaRect.anchorMax = new Vector2(0.5f, 0.5f);
        areaRect.anchoredPosition = new Vector2(350f, 40f);

        descriptionText = CreateText("Description", area.transform, string.Empty, 22, TextAnchor.UpperLeft);
        descriptionText.fontStyle = FontStyle.Bold;
        RectTransform textRect = descriptionText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 76f);
        textRect.offsetMax = new Vector2(-18f, -18f);

        consumeButton = CreateButton("ConsumeButton", parent, "CONSUMIR", new Vector2(250f, 54f));
        RectTransform consumeRect = consumeButton.GetComponent<RectTransform>();
        consumeRect.anchorMin = new Vector2(0.5f, 0.5f);
        consumeRect.anchorMax = new Vector2(0.5f, 0.5f);
        consumeRect.anchoredPosition = new Vector2(350f, -178f);
        consumeButton.onClick.AddListener(ConsumeFusion);
    }

    private GameObject CreateRoot(string objectName)
    {
        GameObject createdRoot = new(objectName);
        createdRoot.transform.SetParent(transform, false);
        RectTransform rect = createdRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return createdRoot;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 size)
    {
        GameObject panel = new(objectName);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.03f, 0.03f, 0.03f, 0.92f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.82f);
        outline.effectDistance = new Vector2(3f, -3f);
        return panel;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 size)
    {
        GameObject buttonObject = new(objectName);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.86f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.72f, 0.72f, 0.68f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.42f, 0.8f);
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            Text text = CreateText("Label", buttonObject.transform, label, 24, TextAnchor.MiddleCenter);
            text.color = Color.black;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        return button;
    }

    private Text CreateTextAt(Transform parent, string text, int fontSize, Vector2 position, Vector2 size)
    {
        Text label = CreateText("Text", parent, text, fontSize, TextAnchor.MiddleCenter);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return label;
    }

    private Text CreateText(string objectName, Transform parent, string text, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = Vector2.zero;

        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return label;
    }

    private static string GetIngredientName(IngredientData ingredient)
    {
        if (ingredient == null)
            return "-";

        return string.IsNullOrWhiteSpace(ingredient.DisplayName) ? ingredient.name : ingredient.DisplayName;
    }

    private static IngredientInventory ResolveBestInventory(IngredientInventory preferred)
    {
        IngredientInventory activeInventory = IngredientInventory.ActivePlayerInventory;
        IngredientInventory bestInventory = preferred != null ? preferred : activeInventory;
        int bestCount = GetTotalIngredientCount(bestInventory);
        IngredientInventory[] inventories = FindObjectsByType<IngredientInventory>(FindObjectsInactive.Include);
        foreach (IngredientInventory candidate in inventories)
        {
            if (candidate == null)
                continue;

            int count = GetTotalIngredientCount(candidate);
            if (count <= bestCount)
                continue;

            bestInventory = candidate;
            bestCount = count;
        }

        return bestInventory;
    }

    private static int GetTotalIngredientCount(IngredientInventory source)
    {
        if (source == null)
            return -1;

        int count = 0;
        IReadOnlyList<IngredientInventory.IngredientStack> stacks = source.GetStacks();
        for (int i = 0; i < stacks.Count; i++)
            count += stacks[i].Count;

        return count;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static bool IsUsable(UnityEngine.Object unityObject)
    {
        try
        {
            return unityObject != null && !string.IsNullOrEmpty(unityObject.name);
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }
}
