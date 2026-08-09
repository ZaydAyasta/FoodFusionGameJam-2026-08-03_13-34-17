using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameMenuHud : MonoBehaviour
{
    [SerializeField] private IngredientInventory inventory;

    private Canvas canvas;
    private GameObject mainMenuRoot;
    private GameObject inventoryRoot;
    private Transform inventoryListRoot;
    private bool initialized;
    private bool mainMenuOpen;
    private bool inventoryOpen;

    private void Awake()
    {
        BuildUi();
        EnsureEventSystem();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (mainMenuOpen)
            return;

        if (inventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void Initialize(IngredientInventory playerInventory)
    {
        if (playerInventory != null)
            playerInventory.SetAsActivePlayerInventory();

        if (inventory != playerInventory)
        {
            if (inventory != null)
                inventory.InventoryChanged -= HandleInventoryChanged;

            inventory = playerInventory;
            if (inventory != null)
                inventory.InventoryChanged += HandleInventoryChanged;
        }

        if (!initialized)
        {
            initialized = true;
            ShowMainMenu();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (inventoryOpen)
            RefreshInventoryList();
    }

    private void ShowMainMenu()
    {
        mainMenuOpen = true;
        inventoryOpen = false;
        Time.timeScale = 0f;
        mainMenuRoot.SetActive(true);
        inventoryRoot.SetActive(false);
    }

    private void StartGame()
    {
        mainMenuOpen = false;
        Time.timeScale = 1f;
        mainMenuRoot.SetActive(false);
        inventoryRoot.SetActive(false);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenInventory()
    {
        ResolveBestInventoryReference();
        inventoryOpen = true;
        Time.timeScale = 0f;
        inventoryRoot.SetActive(true);
        RefreshInventoryList();
    }

    private void CloseInventory()
    {
        inventoryOpen = false;
        inventoryRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    private void BackToMainMenu()
    {
        ShowMainMenu();
    }

    private void BuildUi()
    {
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        mainMenuRoot = BuildMainMenu();
        inventoryRoot = BuildInventoryMenu();

        mainMenuRoot.SetActive(false);
        inventoryRoot.SetActive(false);
    }

    private GameObject BuildMainMenu()
    {
        GameObject root = CreateRoot("MainMenu");
        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = CreatePanel("MainMenuPanel", root.transform, new Vector2(420f, 320f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        Text title = CreateText("Title", panel.transform, "FOOD FUSION", 42, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(-36f, 70f);

        Button playButton = CreateButton("PlayButton", panel.transform, "Jugar", new Vector2(250f, 58f));
        RectTransform playRect = playButton.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.5f);
        playRect.anchorMax = new Vector2(0.5f, 0.5f);
        playRect.anchoredPosition = new Vector2(0f, -20f);
        playButton.onClick.AddListener(StartGame);

        Button exitButton = CreateButton("ExitButton", panel.transform, "Salir", new Vector2(250f, 58f));
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0.5f, 0.5f);
        exitRect.anchorMax = new Vector2(0.5f, 0.5f);
        exitRect.anchoredPosition = new Vector2(0f, -98f);
        exitButton.onClick.AddListener(ExitGame);

        return root;
    }

    private GameObject BuildInventoryMenu()
    {
        GameObject root = CreateRoot("InventoryMenu");
        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.18f);

        GameObject panel = CreatePanel("InventoryPanel", root.transform, new Vector2(720f, 420f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);

        Text title = CreateText("InventoryTitle", panel.transform, "INVENTARIO", 34, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-80f, 60f);

        Button closeButton = CreateButton("CloseButton", panel.transform, "X", new Vector2(44f, 44f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -12f);
        closeButton.onClick.AddListener(CloseInventory);

        GameObject list = new("InventoryList");
        list.transform.SetParent(panel.transform, false);
        inventoryListRoot = list.transform;
        RectTransform listRect = list.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 1f);
        listRect.anchorMax = new Vector2(0.5f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = new Vector2(0f, -92f);
        listRect.sizeDelta = new Vector2(620f, 260f);

        Button mainMenuButton = CreateButton("BackToMainButton", root.transform, "Salir al menu principal", new Vector2(520f, 56f));
        RectTransform mainRect = mainMenuButton.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.anchoredPosition = new Vector2(0f, -250f);
        mainMenuButton.onClick.AddListener(BackToMainMenu);

        return root;
    }

    private void RefreshInventoryList()
    {
        if (!EnsureInventoryListRoot())
            return;

        ResolveBestInventoryReference();

        for (int i = inventoryListRoot.childCount - 1; i >= 0; i--)
            Destroy(inventoryListRoot.GetChild(i).gameObject);

        IReadOnlyList<IngredientInventory.IngredientStack> stacks = inventory != null
            ? inventory.GetStacks()
            : System.Array.Empty<IngredientInventory.IngredientStack>();

        if (stacks.Count == 0)
        {
            CreateEmptyInventoryRow();
            return;
        }

        for (int i = 0; i < stacks.Count; i++)
            CreateInventoryRow(stacks[i], i);

        Canvas.ForceUpdateCanvases();
    }

    private bool EnsureInventoryListRoot()
    {
        if (inventoryListRoot != null)
            return true;

        Transform list = transform.Find("InventoryMenu/InventoryPanel/InventoryList");
        if (list == null && inventoryRoot != null)
        {
            RectTransform[] rects = inventoryRoot.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                if (rect.name == "InventoryList")
                {
                    list = rect.transform;
                    break;
                }
            }
        }

        if (list == null)
        {
            Debug.LogWarning("[GameMenuHud] InventoryList was not found, rebuilding menu UI.");
            BuildUi();
            list = transform.Find("InventoryMenu/InventoryPanel/InventoryList");
        }

        inventoryListRoot = list;
        return inventoryListRoot != null;
    }

    private void ResolveBestInventoryReference()
    {
        IngredientInventory activeInventory = IngredientInventory.ActivePlayerInventory;
        if (activeInventory != null && activeInventory != inventory)
        {
            Initialize(activeInventory);
            return;
        }

        if (inventory != null && inventory.GetStacks().Count > 0)
            return;

        IngredientInventory bestInventory = inventory;
        int bestCount = bestInventory != null ? bestInventory.GetStacks().Count : -1;
        IngredientInventory[] inventories = FindObjectsByType<IngredientInventory>(FindObjectsSortMode.None);
        foreach (IngredientInventory candidate in inventories)
        {
            if (candidate == null)
                continue;

            int count = candidate.GetStacks().Count;
            if (count <= bestCount)
                continue;

            bestInventory = candidate;
            bestCount = count;
        }

        if (bestInventory != null && bestInventory != inventory)
            Initialize(bestInventory);
    }

    private void CreateEmptyInventoryRow()
    {
        Text emptyText = CreateText("EmptyInventoryText", inventoryListRoot, "Sin ingredientes", 24, TextAnchor.MiddleCenter);
        RectTransform rect = emptyText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(620f, 54f);
    }

    private void CreateInventoryRow(IngredientInventory.IngredientStack stack, int index)
    {
        GameObject row = new("IngredientRow");
        row.transform.SetParent(inventoryListRoot, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -index * 58f);
        rowRect.sizeDelta = new Vector2(620f, 54f);

        Text nameText = CreateText("IngredientName", row.transform, GetIngredientName(stack.Ingredient), 24, TextAnchor.MiddleLeft);
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(0f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(410f, 54f);

        Text amount = CreateText("IngredientCount", row.transform, $"x{stack.Count}", 26, TextAnchor.MiddleRight);
        RectTransform amountRect = amount.GetComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1f, 0.5f);
        amountRect.anchorMax = new Vector2(1f, 0.5f);
        amountRect.pivot = new Vector2(1f, 0.5f);
        amountRect.anchoredPosition = new Vector2(-70f, 0f);
        amountRect.sizeDelta = new Vector2(80f, 54f);

        GameObject iconObject = new("IngredientIcon");
        iconObject.transform.SetParent(row.transform, false);
        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-2f, 0f);
        iconRect.sizeDelta = new Vector2(52f, 52f);

        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = stack.Ingredient != null ? stack.Ingredient.Icon : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private static string GetIngredientName(IngredientData ingredient)
    {
        if (ingredient == null)
            return "-";

        return string.IsNullOrWhiteSpace(ingredient.DisplayName)
            ? ingredient.name
            : ingredient.DisplayName;
    }

    private GameObject CreateRoot(string objectName)
    {
        GameObject root = new(objectName);
        root.transform.SetParent(transform, false);
        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return root;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 size)
    {
        GameObject panel = new(objectName);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.02f, 0.9f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.85f);
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
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.68f, 1f);
        button.colors = colors;

        Text text = CreateText("Label", buttonObject.transform, label, 24, TextAnchor.MiddleCenter);
        text.color = Color.black;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
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

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }
}
