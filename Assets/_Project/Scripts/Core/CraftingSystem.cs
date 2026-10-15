// 레시피 재료 확인과 차감, 결과물 지급을 처리하는 제작 권위 로직이다.
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class CraftingSystem : MonoBehaviour
{
    private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    // 레시피가 요구하는 작업대가 없거나, 요구하는 작업대 근처에 있으면 참이다.
    public bool IsStationAvailable(RecipeDefinition recipe)
    {
        return recipe == null || CraftingStation.IsNear(recipe.RequiredStation, transform.position);
    }

    public bool CanCraft(RecipeDefinition recipe)
    {
        if (recipe == null) return false;
        if (!IsStationAvailable(recipe)) return false;
        foreach (RecipeDefinition.Ingredient ingredient in recipe.Inputs)
        {
            if (ingredient.item == null) continue;
            if (inventory.GetQuantity(ingredient.item) < ingredient.quantity) return false;
        }
        return true;
    }

    public bool TryCraft(RecipeDefinition recipe)
    {
        if (!CanCraft(recipe)) return false;

        foreach (RecipeDefinition.Ingredient ingredient in recipe.Inputs)
        {
            if (ingredient.item == null) continue;
            inventory.Remove(ingredient.item, ingredient.quantity);
        }

        inventory.Add(recipe.OutputItem, recipe.OutputQuantity);
        return true;
    }
}
