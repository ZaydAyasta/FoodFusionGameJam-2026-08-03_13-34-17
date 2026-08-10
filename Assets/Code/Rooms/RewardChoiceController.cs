using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RewardChoiceController : MonoBehaviour
{
    [SerializeField] private IngredientReward rewardPrefab;
    [SerializeField] private bool requireInteract;

    private RoomController room;
    private IngredientReward firstReward;
    private IngredientReward secondReward;
    private bool claimed;

    public bool Claimed => claimed;

    private void Awake()
    {
        if (rewardPrefab == null)
            rewardPrefab = Resources.Load<IngredientReward>("Rewards/RiceReward");

#if UNITY_EDITOR
        if (rewardPrefab == null)
            rewardPrefab = AssetDatabase.LoadAssetAtPath<IngredientReward>("Assets/Prefabs/Rewards/RiceReward.prefab");
#endif
    }

    public void Initialize(RoomController owner, IngredientData first, IngredientData second)
    {
        room = owner;
        SpawnReward(first);
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

    private void SpawnReward(IngredientData ingredient)
    {
        if (ingredient == null)
            return;

        IngredientReward reward = rewardPrefab != null
            ? Instantiate(rewardPrefab, transform)
            : CreateFallbackReward(transform);

        reward.transform.localPosition = Vector3.zero;
        reward.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
        reward.Initialize(ingredient, this, Color.white, requireInteract);

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
        renderer.sprite = LoadRiceSprite();
        if (renderer.sprite == null)
            renderer.sprite = CreateFallbackSprite();

        BoxCollider2D collider = rewardObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.isTrigger = true;
        return rewardObject.AddComponent<IngredientReward>();
    }

    private static Sprite LoadRiceSprite()
    {
        Sprite resourceSprite = Resources.Load<Sprite>("Images/rice");
        if (resourceSprite != null)
            return resourceSprite;

        Sprite[] resourceSprites = Resources.LoadAll<Sprite>("Images/rice");
        foreach (Sprite sprite in resourceSprites)
        {
            if (sprite != null && sprite.name == "rice_0")
                return sprite;
        }

        foreach (Sprite sprite in resourceSprites)
        {
            if (sprite != null)
                return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>("Images/rice");
        if (texture != null)
        {
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

#if UNITY_EDITOR
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/rice.png");
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == "rice_0")
                return sprite;
        }

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }
#endif

        return null;
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 4f);
    }
}
