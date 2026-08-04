using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallbackSprite : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float pixelsPerUnit = 4f;

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite != null)
            return;

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        spriteRenderer.color = color;
    }
}
