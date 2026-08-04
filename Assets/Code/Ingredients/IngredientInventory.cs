using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientInventory : MonoBehaviour
{
    private readonly List<IngredientData> ingredients = new();

    public event Action InventoryChanged;

    public bool HasIngredient(IngredientData ingredient)
    {
        return ingredient != null && ingredients.Contains(ingredient);
    }

    public void AddIngredient(IngredientData ingredient)
    {
        if (ingredient == null || HasIngredient(ingredient))
            return;

        ingredients.Add(ingredient);
        InventoryChanged?.Invoke();
    }

    public bool RemoveIngredient(IngredientData ingredient)
    {
        if (ingredient == null)
            return false;

        bool removed = ingredients.Remove(ingredient);
        if (removed)
            InventoryChanged?.Invoke();

        return removed;
    }

    public IReadOnlyList<IngredientData> GetIngredients()
    {
        return ingredients;
    }
}
