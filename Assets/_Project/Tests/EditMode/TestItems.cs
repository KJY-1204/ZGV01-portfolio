// EditMode 테스트에서 ItemDefinition/RecipeDefinition 에셋을 메모리에 만드는 도우미다.
using UnityEditor;
using UnityEngine;

public static class TestItems
{
    public static ItemDefinition Item(string id, int maxStack)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        var so = new SerializedObject(item);
        so.FindProperty("stableId").stringValue = id;
        so.FindProperty("maxStack").intValue = maxStack;
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    public static RecipeDefinition Recipe(ItemDefinition output, int outputQty, params (ItemDefinition item, int qty)[] inputs)
    {
        var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        var so = new SerializedObject(recipe);
        so.FindProperty("outputItem").objectReferenceValue = output;
        so.FindProperty("outputQuantity").intValue = outputQty;
        SerializedProperty arr = so.FindProperty("inputs");
        arr.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("item").objectReferenceValue = inputs[i].item;
            e.FindPropertyRelative("quantity").intValue = inputs[i].qty;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return recipe;
    }
}
