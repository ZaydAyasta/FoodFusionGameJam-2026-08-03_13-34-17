using UnityEngine;

public class ShopVisual : MonoBehaviour
{
    public enum ShopType
    {
        Closed,
        Type1,
        Type2
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite type1Sprite;
    [SerializeField] private Sprite type2Sprite;
    public ShopType CurrentType { get; private set; } = ShopType.Closed;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        CurrentType = ShopType.Closed;
        ApplySprite();
    }

    public void RandomizeOpenShop()
    {
        CurrentType = ShopType.Closed;
        ApplySprite();
    }

    public void SetShopType(ShopType shopType)
    {
        CurrentType = ShopType.Closed;
        ApplySprite();
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = CurrentType switch
        {
            ShopType.Closed => closedSprite,
            ShopType.Type2 => type2Sprite,
            _ => type1Sprite
        };
    }
}
