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
    [SerializeField] private bool randomizeOnAwake = true;

    public ShopType CurrentType { get; private set; } = ShopType.Type1;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (randomizeOnAwake)
            RandomizeOpenShop();
        else
            ApplySprite();
    }

    public void RandomizeOpenShop()
    {
        CurrentType = Random.value < 0.5f ? ShopType.Type1 : ShopType.Type2;
        ApplySprite();
    }

    public void SetShopType(ShopType shopType)
    {
        CurrentType = shopType;
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
