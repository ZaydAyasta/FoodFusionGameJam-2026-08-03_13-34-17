using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient_New", menuName = "Food Fusion/Ingredient")]
public class IngredientData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private CuisineType cuisine;
    [Tooltip("Enemy type that guards this ingredient when it is selected at a door.")]
    [SerializeField] private EnemyDeathNotifier enemyPrefab;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public CuisineType Cuisine => cuisine;
    public EnemyDeathNotifier EnemyPrefab => enemyPrefab;
}
