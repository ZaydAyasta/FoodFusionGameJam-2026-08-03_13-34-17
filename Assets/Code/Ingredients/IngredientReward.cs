using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class IngredientReward : MonoBehaviour
{
    [SerializeField] private IngredientData ingredient;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool requireInteract;

    private RewardChoiceController owner;
    private IngredientInventory nearbyInventory;
    private bool claimed;

    public IngredientData Ingredient => ingredient;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        RefreshVisual();
    }

    private void Update()
    {
        if (!requireInteract || claimed || nearbyInventory == null)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Claim(nearbyInventory);
    }

    public void Initialize(IngredientData rewardIngredient, RewardChoiceController choiceOwner, Color fallbackColor, bool useInteract)
    {
        ingredient = rewardIngredient;
        owner = choiceOwner;
        requireInteract = useInteract;
        RefreshVisual(fallbackColor);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IngredientInventory inventory = other.GetComponentInParent<IngredientInventory>();
        if (inventory == null)
            return;

        nearbyInventory = inventory;

        if (!requireInteract)
            Claim(inventory);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IngredientInventory inventory = other.GetComponentInParent<IngredientInventory>();
        if (inventory != null && inventory == nearbyInventory)
            nearbyInventory = null;
    }

    private void Claim(IngredientInventory inventory)
    {
        if (claimed || inventory == null || ingredient == null)
            return;

        claimed = true;
        inventory.AddIngredient(ingredient);
        owner?.Claim(this);
    }

    private void RefreshVisual()
    {
        RefreshVisual(Color.white);
    }

    private void RefreshVisual(Color fallbackColor)
    {
        if (spriteRenderer == null)
            return;

        if (ingredient != null && ingredient.Icon != null)
        {
            spriteRenderer.sprite = ingredient.Icon;
            spriteRenderer.color = Color.white;
            return;
        }

        spriteRenderer.color = fallbackColor;
    }
}
