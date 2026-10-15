// 방어구 아이템 6종(가죽 3, 철 3)과 제작 레시피를 만들고, 플레이어에 Equipment를 붙이고, DB와 제작 목록에 등록해 주는 에디터 도구다. 여러 번 실행해도 기존 에셋을 유지한다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ArmorAssetBuilder
{
    private const string ItemDir = "Assets/_Project/Data/Items/";
    private const string RecipeDir = "Assets/_Project/Data/Recipes/";

    private struct ArmorSpec
    {
        public string fileName, stableId, displayName;
        public ArmorSlot slot;
        public float reduction;
        public (string item, int quantity)[] cost;
    }

    // 수치는 임시 밸런스다. 가죽 한 벌(모자+조끼+각반) 합 32%, 철 한 벌 합 54%이고 상한은 60%다.
    private static readonly ArmorSpec[] Armors =
    {
        new ArmorSpec { fileName = "Item_Armor_LeatherCap", stableId = "armor_leather_cap", displayName = "가죽 모자", slot = ArmorSlot.Head, reduction = 0.08f, cost = new[] { ("Item_Resource_Leather", 2) } },
        new ArmorSpec { fileName = "Item_Armor_LeatherVest", stableId = "armor_leather_vest", displayName = "가죽 조끼", slot = ArmorSlot.Body, reduction = 0.14f, cost = new[] { ("Item_Resource_Leather", 4) } },
        new ArmorSpec { fileName = "Item_Armor_LeatherLeggings", stableId = "armor_leather_leggings", displayName = "가죽 각반", slot = ArmorSlot.Legs, reduction = 0.10f, cost = new[] { ("Item_Resource_Leather", 3) } },
        new ArmorSpec { fileName = "Item_Armor_IronHelmet", stableId = "armor_iron_helmet", displayName = "철 헬멧", slot = ArmorSlot.Head, reduction = 0.14f, cost = new[] { ("Item_ScrapMetal", 4), ("Item_Resource_Leather", 1) } },
        new ArmorSpec { fileName = "Item_Armor_IronPlate", stableId = "armor_iron_plate", displayName = "철 흉갑", slot = ArmorSlot.Body, reduction = 0.24f, cost = new[] { ("Item_ScrapMetal", 8), ("Item_Resource_Leather", 2) } },
        new ArmorSpec { fileName = "Item_Armor_IronGreaves", stableId = "armor_iron_greaves", displayName = "철 정강이", slot = ArmorSlot.Legs, reduction = 0.16f, cost = new[] { ("Item_ScrapMetal", 5), ("Item_Resource_Leather", 1) } },
    };

    [MenuItem("ZGV01/Items/Build Armor")]
    public static void Build()
    {
        List<RecipeDefinition> recipes = new List<RecipeDefinition>();
        foreach (ArmorSpec spec in Armors)
        {
            ArmorDefinition armor = BuildItem(spec);
            recipes.Add(BuildRecipe(spec, armor));
        }
        AssetDatabase.SaveAssets();

        RegisterItems();
        AttachEquipmentToPlayer();
        RegisterRecipes(recipes);
        AssetDatabase.SaveAssets();
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static ArmorDefinition BuildItem(ArmorSpec spec)
    {
        ArmorDefinition armor = LoadOrCreate<ArmorDefinition>(ItemDir + spec.fileName + ".asset");
        SerializedObject so = new SerializedObject(armor);
        so.FindProperty("stableId").stringValue = spec.stableId;
        so.FindProperty("displayName").stringValue = spec.displayName;
        so.FindProperty("category").intValue = (int)ItemCategory.Equipment;
        so.FindProperty("maxStack").intValue = 1;
        so.FindProperty("slot").enumValueIndex = (int)spec.slot;
        so.FindProperty("damageReduction").floatValue = spec.reduction;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(armor);
        return armor;
    }

    private static RecipeDefinition BuildRecipe(ArmorSpec spec, ArmorDefinition armor)
    {
        string name = spec.fileName.Replace("Item_Armor_", "Recipe_Armor_");
        RecipeDefinition recipe = LoadOrCreate<RecipeDefinition>(RecipeDir + name + ".asset");
        SerializedObject so = new SerializedObject(recipe);
        so.FindProperty("displayName").stringValue = spec.displayName + " 만들기";
        so.FindProperty("outputItem").objectReferenceValue = armor;
        so.FindProperty("outputQuantity").intValue = 1;
        so.FindProperty("requiredStation").enumValueIndex = (int)CraftingStationType.None;

        SerializedProperty inputs = so.FindProperty("inputs");
        inputs.arraySize = spec.cost.Length;
        for (int i = 0; i < spec.cost.Length; i++)
        {
            SerializedProperty input = inputs.GetArrayElementAtIndex(i);
            input.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.cost[i].item + ".asset");
            input.FindPropertyRelative("quantity").intValue = spec.cost[i].quantity;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    // 저장 파일이 id로 아이템을 찾을 수 있게 아이템 DB에 등록한다.
    private static void RegisterItems()
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/ItemDatabase.asset");
        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("items");
        foreach (ArmorSpec spec in Armors)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.fileName + ".asset");
            bool exists = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) { exists = true; break; }
            if (exists) continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    // 플레이어 프리팹과 열린 씬의 플레이어에 Equipment를 붙인다(이미 있으면 그대로 둔다).
    private static void AttachEquipmentToPlayer()
    {
        const string playerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        if (prefab.GetComponent<Equipment>() == null)
        {
            prefab.AddComponent<Equipment>();
            PrefabUtility.SaveAsPrefabAsset(prefab, playerPrefabPath);
        }
        PrefabUtility.UnloadPrefabContents(prefab);

        GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
        if (scenePlayer != null && scenePlayer.GetComponent<Equipment>() == null)
        {
            scenePlayer.AddComponent<Equipment>();
            EditorUtility.SetDirty(scenePlayer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scenePlayer.scene);
        }
    }

    // 열려 있는 씬의 HUD 제작 패널 목록에 레시피를 덧붙인다. 씬 저장은 호출한 쪽이 한다.
    private static void RegisterRecipes(List<RecipeDefinition> recipes)
    {
        HudController hud = Object.FindFirstObjectByType<HudController>();
        if (hud == null)
        {
            Debug.LogWarning("열린 씬에 HudController가 없어 레시피를 제작 패널에 등록하지 못했다.");
            return;
        }

        SerializedObject so = new SerializedObject(hud);
        SerializedProperty list = so.FindProperty("recipes");
        foreach (RecipeDefinition recipe in recipes)
        {
            bool exists = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == recipe) { exists = true; break; }
            if (exists) continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hud);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
    }
}
