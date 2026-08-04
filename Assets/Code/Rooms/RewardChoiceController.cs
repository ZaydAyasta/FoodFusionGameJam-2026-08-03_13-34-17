using UnityEngine;

public class RewardChoiceController : MonoBehaviour
{
    [SerializeField] private IngredientReward rewardPrefab;
    [SerializeField] private float optionSpacing = 1.4f;
    [SerializeField] private bool requireInteract = true;

    private RoomController room;
    private IngredientReward firstReward;
    private IngredientReward secondReward;
    private bool claimed;

    public bool Claimed => claimed;

    public void Initialize(RoomController owner, IngredientData first, IngredientData second)
    {
        room = owner;
        SpawnReward(first, -optionSpacing * 0.5f, new Color(1f, 0.86f, 0.2f));
        SpawnReward(second, optionSpacing * 0.5f, new Color(0.35f, 0.85f, 1f));
    }

    public void Claim(IngredientReward selected)
    {
        if (claimed)
            return;

        claimed = true;

        if (firstReward != null && firstReward != selected)
            Destroy(firstReward.gameObject);

        if (secondReward != null && secondReward != selected)
            Destroy(secondReward.gameObject);

        room?.NotifyRewardClaimed();
        Destroy(gameObject);
    }

    private void SpawnReward(IngredientData ingredient, float xOffset, Color fallbackColor)
    {
        if (ingredient == null)
            return;

        IngredientReward reward = rewardPrefab != null
            ? Instantiate(rewardPrefab, transform)
            : CreateFallbackReward(transform);

        reward.transform.localPosition = new Vector3(xOffset, 0f, 0f);
        reward.Initialize(ingredient, this, fallbackColor, requireInteract);

        if (firstReward == null)
            firstReward = reward;
        else
            secondReward = reward;
    }

    private static IngredientReward CreateFallbackReward(Transform parent)
    {
        GameObject rewardObject = new("IngredientReward");
        rewardObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = rewardObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackSprite();
        CircleCollider2D collider = rewardObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        return rewardObject.AddComponent<IngredientReward>();
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 4f);
    }
}
