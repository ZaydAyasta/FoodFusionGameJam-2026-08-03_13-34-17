using System;
using System.Collections.Generic;
using UnityEngine;

public class IngredientInventory : MonoBehaviour
{
    public readonly struct IngredientStack
    {
        public IngredientStack(IngredientData ingredient, int count)
        {
            Ingredient = ingredient;
            Count = count;
        }

        public IngredientData Ingredient { get; }
        public int Count { get; }
    }

    private readonly List<IngredientStack> stacks = new();
    private readonly List<IngredientData> ingredientSnapshot = new();

    public event Action InventoryChanged;

    public bool HasIngredient(IngredientData ingredient)
    {
        return GetIngredientIndex(ingredient) >= 0;
    }

    public void AddIngredient(IngredientData ingredient)
    {
        if (ingredient == null)
            return;

        int index = GetIngredientIndex(ingredient);
        if (index >= 0)
        {
            IngredientStack stack = stacks[index];
            stacks[index] = new IngredientStack(stack.Ingredient, stack.Count + 1);
        }
        else
        {
            stacks.Add(new IngredientStack(ingredient, 1));
        }

        InventoryChanged?.Invoke();
    }

    public bool RemoveIngredient(IngredientData ingredient)
    {
        if (ingredient == null)
            return false;

        int index = GetIngredientIndex(ingredient);
        if (index < 0)
            return false;

        IngredientStack stack = stacks[index];
        if (stack.Count <= 1)
            stacks.RemoveAt(index);
        else
            stacks[index] = new IngredientStack(stack.Ingredient, stack.Count - 1);

        InventoryChanged?.Invoke();
        return true;
    }

    public IReadOnlyList<IngredientData> GetIngredients()
    {
        ingredientSnapshot.Clear();
        foreach (IngredientStack stack in stacks)
        {
            for (int i = 0; i < stack.Count; i++)
                ingredientSnapshot.Add(stack.Ingredient);
        }

        return ingredientSnapshot;
    }

    public IReadOnlyList<IngredientStack> GetStacks()
    {
        return stacks;
    }

    public int GetCount(IngredientData ingredient)
    {
        int index = GetIngredientIndex(ingredient);
        return index >= 0 ? stacks[index].Count : 0;
    }

    private int GetIngredientIndex(IngredientData ingredient)
    {
        if (ingredient == null)
            return -1;

        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].Ingredient == ingredient)
                return i;
        }

        return -1;
    }
}
