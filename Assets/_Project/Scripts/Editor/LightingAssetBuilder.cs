// 휴대 랜턴(아이템, 레시피, 허리 장착 프리팹), 밤에 켜지는 맵 가로등 래퍼 프리팹을 만들고 플레이어에 랜턴 컴포넌트를 붙이는 에디터 도구다. 여러 번 실행해도 기존 에셋을 유지한다.
using UnityEditor;
using UnityEngine;

public static class LightingAssetBuilder
{
    private const string ItemPath = "Assets/_Project/Data/Items/Item_Lantern.asset";
    private const string RecipePath = "Assets/_Project/Data/Recipes/Recipe_Lantern.asset";
    private const string BeltPrefabPath = "Assets/_Project/Prefabs/Items/Lantern_Belt.prefab";
    // 오래된 형광등처럼 살짝 푸르스름한 흰빛이다.
    private const float LampSpotAngle = 125f;
    private const float LampSpotRange = 45f;
    private const float LampPointRange = 12f;
    public static readonly Color OldFluorescentColor = new Color(0.86f, 1f, 0.92f);
    private const string LampDir = "Assets/_Project/Prefabs/Map/";
    private const string KatsLantern = "Assets/KatsBits-fantasy-base/Prefabs/Props/Prop.Lantern.prefab";
    private const string LampsDir = "Assets/TirgamesAssets/SchoolScene/Models/Exterior/Lamps/Prefabs/";

    public const string PostLampPath = LampDir + "MapLamp_Post.prefab";
    public const string SmallLampPath = LampDir + "MapLamp_Small.prefab";

    [MenuItem("ZGV01/Items/Build Lantern And Lamps")]
    public static void Build()
    {
        ItemDefinition item = BuildItem();
        BuildRecipe(item);
        GameObject belt = BuildBeltPrefab();
        BuildMapLamp(PostLampPath, LampsDir + "RoadLamp1aNight.prefab", LampsDir + "RoadLamp1a.prefab", 1.75f, 0.5f);
        BuildMapLamp(SmallLampPath, LampsDir + "RoadLamp2On.prefab", LampsDir + "RoadLamp2Off.prefab", 2f, 1f);
        AssetDatabase.SaveAssets();

        RegisterItem(item);
        AttachToPlayer(item, belt);
        AssetDatabase.SaveAssets();
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static ItemDefinition BuildItem()
    {
        ItemDefinition item = LoadOrCreate<ItemDefinition>(ItemPath);
        SerializedObject so = new SerializedObject(item);
        so.FindProperty("stableId").stringValue = "lantern";
        so.FindProperty("displayName").stringValue = "랜턴";
        so.FindProperty("category").intValue = (int)ItemCategory.Equipment;
        so.FindProperty("maxStack").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void BuildRecipe(ItemDefinition item)
    {
        RecipeDefinition recipe = LoadOrCreate<RecipeDefinition>(RecipePath);
        SerializedObject so = new SerializedObject(recipe);
        so.FindProperty("displayName").stringValue = "랜턴 만들기";
        so.FindProperty("outputItem").objectReferenceValue = item;
        so.FindProperty("outputQuantity").intValue = 1;
        so.FindProperty("requiredStation").enumValueIndex = (int)CraftingStationType.None;

        (string path, int quantity)[] inputs =
        {
            ("Assets/_Project/Data/Items/Item_ScrapMetal.asset", 3),
            ("Assets/_Project/Data/Items/Item_Resource_Leather.asset", 1),
        };
        SerializedProperty list = so.FindProperty("inputs");
        list.arraySize = inputs.Length;
        for (int i = 0; i < inputs.Length; i++)
        {
            SerializedProperty input = list.GetArrayElementAtIndex(i);
            input.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(inputs[i].path);
            input.FindPropertyRelative("quantity").intValue = inputs[i].quantity;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
    }

    // 허리 장착 프리팹: 랜턴 모델(충돌체 없음)과 따뜻한 점광원이다. 모델 원점은 허리 본 기준 오프셋으로 맞춘다.
    private static GameObject BuildBeltPrefab()
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(BeltPrefabPath));
        GameObject root = new GameObject("Lantern_Belt");
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(KatsLantern), root.transform);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = model.transform.localScale * 0.9f;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);

        GameObject lightObject = new GameObject("Light");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 15f;
        light.intensity = 2.4f;
        light.color = new Color(1f, 0.72f, 0.42f);
        light.shadows = LightShadows.None;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, BeltPrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 맵 가로등 래퍼: 켜진 변형 프리팹을 그대로 쓰고 NightLight로 시각에 맞춰 조명과 발광 유리를 켜고 끈다. 꺼진 재질은 꺼진 변형 프리팹의 같은 칸에서 가져온다.
    private static void BuildMapLamp(string path, string litPath, string unlitPath, float lightScale, float heightScale)
    {
        System.IO.Directory.CreateDirectory(LampDir);
        GameObject lit = AssetDatabase.LoadAssetAtPath<GameObject>(litPath);
        GameObject unlit = AssetDatabase.LoadAssetAtPath<GameObject>(unlitPath);
        if (lit == null || unlit == null) { Debug.LogWarning("가로등 원본을 찾지 못했다: " + litPath); return; }

        GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(lit, root.transform);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // 큰 가로등은 원본이 너무 높아 높이만 줄인다(조명도 모델의 자식이라 함께 낮아진다).
        model.transform.localScale = new Vector3(1f, heightScale, 1f);

        Light[] lights = model.GetComponentsInChildren<Light>(true);
        foreach (Light light in lights)
        {
            light.shadows = LightShadows.None;
            light.intensity *= lightScale;
            light.color = OldFluorescentColor;
            // 조명 범위: 큰 가로등은 원뿔을 넓히고 멀리 보내며, 작은 가로등은 반경을 늘린다.
            if (light.type == LightType.Spot)
            {
                light.spotAngle = LampSpotAngle;
                light.innerSpotAngle = LampSpotAngle * 0.8f;
                light.range = LampSpotRange;
            }
            else light.range = LampPointRange;
            light.enabled = false;
        }

        // 발광 유리 칸을 찾는다(켜진 변형의 'Illum' 재질).
        Renderer glass = null;
        int glassIndex = -1;
        Material onMaterial = null;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null && materials[i].name.Contains("Illum")) { glass = renderer; glassIndex = i; onMaterial = materials[i]; }
        }

        Material offMaterial = null;
        if (glass != null)
        {
            // 꺼진 변형에서 같은 이름의 렌더러/칸의 재질을 가져온다.
            string rendererName = glass.name;
            foreach (Renderer renderer in unlit.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name != rendererName) continue;
                Material[] materials = renderer.sharedMaterials;
                if (glassIndex < materials.Length) offMaterial = materials[glassIndex];
            }
        }

        NightLight night = root.AddComponent<NightLight>();
        SerializedObject so = new SerializedObject(night);
        SerializedProperty lightList = so.FindProperty("lights");
        lightList.arraySize = lights.Length;
        for (int i = 0; i < lights.Length; i++) lightList.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
        so.FindProperty("glass").objectReferenceValue = glass;
        so.FindProperty("glassMaterialIndex").intValue = Mathf.Max(0, glassIndex);
        so.FindProperty("onMaterial").objectReferenceValue = onMaterial;
        so.FindProperty("offMaterial").objectReferenceValue = offMaterial;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void RegisterItem(ItemDefinition item)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/ItemDatabase.asset");
        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("items");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    // 플레이어 프리팹에 LanternEquipment를 붙여 아이템/모델을 연결한다. 열려 있는 씬의 HUD 제작 목록과 시작 아이템(에디터 시험용)에도 넣는다.
    private static void AttachToPlayer(ItemDefinition item, GameObject belt)
    {
        const string playerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        LanternEquipment lantern = prefab.GetComponent<LanternEquipment>();
        if (lantern == null) lantern = prefab.AddComponent<LanternEquipment>();
        SerializedObject so = new SerializedObject(lantern);
        so.FindProperty("lanternItem").objectReferenceValue = item;
        so.FindProperty("beltPrefab").objectReferenceValue = belt;
        // 골반 본 기준 왼쪽 허리 위치와 회전(LanternEquipment의 기본값과 같다). 프리팹에 직렬화된 옛 값을 덮어쓴다.
        so.FindProperty("beltLocalPosition").vector3Value = new Vector3(0.064f, 0.01f, -0.249f);
        so.FindProperty("beltLocalEuler").vector3Value = new Vector3(87.7f, 189.1f, 280f);
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(prefab, playerPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(RecipePath);
        HudController hud = Object.FindFirstObjectByType<HudController>();
        if (hud != null)
        {
            SerializedObject hudSo = new SerializedObject(hud);
            SerializedProperty list = hudSo.FindProperty("recipes");
            bool exists = false;
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == recipe) exists = true;
            if (!exists)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
                hudSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hud);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            }
        }

        StartingItems starting = Object.FindFirstObjectByType<StartingItems>();
        if (starting != null)
        {
            SerializedObject startSo = new SerializedObject(starting);
            SerializedProperty grants = startSo.FindProperty("grants");
            bool exists = false;
            for (int i = 0; i < grants.arraySize; i++)
                if (grants.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue == item) exists = true;
            if (!exists)
            {
                grants.arraySize++;
                SerializedProperty grant = grants.GetArrayElementAtIndex(grants.arraySize - 1);
                grant.FindPropertyRelative("item").objectReferenceValue = item;
                grant.FindPropertyRelative("quantity").intValue = 1;
                startSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(starting);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(starting.gameObject.scene);
            }
        }
    }
}
