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
        FitSquareColliderToSprite();
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
        FitSquareColliderToSprite();
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
        if (owner != null)
            owner.Claim(this);
        else
            Destroy(gameObject);
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

        spriteRenderer.color = Color.white;
    }

    private void FitSquareColliderToSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        float side = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);
        box.size = new Vector2(side, side);
        box.offset = spriteBounds.center;
        box.isTrigger = true;
    }
}
