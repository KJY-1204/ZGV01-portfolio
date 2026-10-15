// 요리·정수에 필요한 아이템, 레시피, 모닥불 건축물, 더러운 물 웅덩이 노드를 만들고 등록해 주는 에디터 도구다. 여러 번 실행해도 기존 프리팹의 GUID를 유지한다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CookingAssetBuilder
{
    private const string ItemDir = "Assets/_Project/Data/Items/";
    private const string RecipeDir = "Assets/_Project/Data/Recipes/";
    private const string NodeDir = "Assets/_Project/Data/World/";
    private const string BuildingDataDir = "Assets/_Project/Data/Building/";
    private const string BuildingPrefabDir = "Assets/_Project/Prefabs/Building/";
    private const string WorldPrefabDir = "Assets/_Project/Prefabs/World/";
    private const string MaterialDir = "Assets/_Project/Materials/Cooking/";
    private const string SoundDir = "Assets/Devion Games/Third Person Controller/Example/Sounds/";
    private const string AirBurst = "Assets/Devion Games/Inventory System/Examples/Sounds/Air Burst.ogg";
    private const string PuffPath = "Assets/_Project/Prefabs/Effects/Effect_DepletePuff.prefab";

    private struct ItemSpec
    {
        public string fileName, stableId, displayName;
        public int maxStack;
        public float hunger, thirst, health;
        // true면 소비하지 않는 일반 자원(ItemDefinition)으로 만든다.
        public bool resource;
    }

    private struct RecipeSpec
    {
        public string fileName, displayName, output, completionMessage;
        public int outputQuantity;
        public (string item, int quantity)[] inputs;
        // true면 작업대 없이 어디서나 제작한다.
        public bool anywhere;
    }

    // 수치는 임시 밸런스다. 열매 12 -> 구운 열매 20, 통조림 35 -> 데운 통조림 50, 더러운 물은 갈증 15에 체력 -6이다.
    private static readonly ItemSpec[] Items =
    {
        new ItemSpec { fileName = "Item_Drink_DirtyWater", stableId = "drink_dirty_water", displayName = "더러운 물", maxStack = 10, thirst = 15f, health = -6f },
        new ItemSpec { fileName = "Item_Food_RoastedBerries", stableId = "food_roasted_berries", displayName = "구운 열매", maxStack = 20, hunger = 20f, thirst = 2f },
        new ItemSpec { fileName = "Item_Food_HeatedBeans", stableId = "food_heated_beans", displayName = "데운 통조림", maxStack = 10, hunger = 50f, health = 5f },
        // 사냥으로 얻는 고기다. 날것은 허기를 조금 채우지만 체력이 깎이고, 모닥불에서 구우면 가장 좋은 음식이 된다.
        new ItemSpec { fileName = "Item_Food_RawMeat", stableId = "food_raw_meat", displayName = "생고기", maxStack = 10, hunger = 6f, health = -5f },
        new ItemSpec { fileName = "Item_Food_CookedMeat", stableId = "food_cooked_meat", displayName = "구운 고기", maxStack = 10, hunger = 45f, health = 4f },
        // 동물에서 얻는 가죽과, 가죽으로 만드는 응급 치료품이다(장비 시스템이 없어 방어구는 만들지 않는다).
        new ItemSpec { fileName = "Item_Resource_Leather", stableId = "resource_leather", displayName = "가죽", maxStack = 20, resource = true },
        new ItemSpec { fileName = "Item_Medical_Bandage", stableId = "medical_bandage", displayName = "붕대", maxStack = 10, health = 35f },
    };

    // 연료로 나무를 한 개씩 쓴다.
    private static readonly RecipeSpec[] Recipes =
    {
        new RecipeSpec { fileName = "Recipe_PurifyWater", displayName = "물 정수", completionMessage = "물이 정제되었습니다", output = "Item_Drink_Water", outputQuantity = 2, inputs = new[] { ("Item_Drink_DirtyWater", 2), ("Item_Wood", 1) } },
        new RecipeSpec { fileName = "Recipe_RoastBerries", displayName = "열매 굽기", completionMessage = "{0} 요리가 완성되었습니다", output = "Item_Food_RoastedBerries", outputQuantity = 2, inputs = new[] { ("Item_Food_Berries", 2), ("Item_Wood", 1) } },
        new RecipeSpec { fileName = "Recipe_HeatBeans", displayName = "통조림 데우기", completionMessage = "{0} 요리가 완성되었습니다", output = "Item_Food_HeatedBeans", outputQuantity = 1, inputs = new[] { ("Item_Food_CannedBeans", 1), ("Item_Wood", 1) } },
        new RecipeSpec { fileName = "Recipe_Bandage", displayName = "붕대 만들기", anywhere = true, output = "Item_Medical_Bandage", outputQuantity = 1, inputs = new[] { ("Item_Resource_Leather", 2) } },
        new RecipeSpec { fileName = "Recipe_CookMeat", displayName = "고기 굽기", completionMessage = "{0} 요리가 완성되었습니다", output = "Item_Food_CookedMeat", outputQuantity = 1, inputs = new[] { ("Item_Food_RawMeat", 1), ("Item_Wood", 1) } },
    };

    [MenuItem("ZGV01/Survival/Build Cooking And Purification")]
    public static void Build()
    {
        EnsureFolder("Assets/_Project/Materials", "Cooking");

        foreach (ItemSpec spec in Items) BuildItem(spec);
        AssetDatabase.SaveAssets();

        List<RecipeDefinition> recipes = new List<RecipeDefinition>();
        foreach (RecipeSpec spec in Recipes) recipes.Add(BuildRecipe(spec));
        AssetDatabase.SaveAssets();

        RegisterItems();

        BuildableDefinition campfire = BuildCampfire();
        ResourceNodeDefinition puddle = BuildPuddleDefinition();
        BuildPuddlePrefab(puddle);
        AssetDatabase.SaveAssets();

        RegisterBuildable(campfire);
        RegisterRecipes(recipes);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void BuildItem(ItemSpec spec)
    {
        ItemDefinition item = spec.resource
            ? LoadOrCreate<ItemDefinition>(ItemDir + spec.fileName + ".asset")
            : LoadOrCreate<ConsumableDefinition>(ItemDir + spec.fileName + ".asset");
        SerializedObject so = new SerializedObject(item);
        so.FindProperty("stableId").stringValue = spec.stableId;
        so.FindProperty("displayName").stringValue = spec.displayName;
        so.FindProperty("category").intValue = (int)(spec.resource ? ItemCategory.Resource : ItemCategory.Consumable);
        so.FindProperty("maxStack").intValue = spec.maxStack;
        if (!spec.resource)
        {
            so.FindProperty("hungerRestore").floatValue = spec.hunger;
            so.FindProperty("thirstRestore").floatValue = spec.thirst;
            so.FindProperty("healthRestore").floatValue = spec.health;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    private static RecipeDefinition BuildRecipe(RecipeSpec spec)
    {
        RecipeDefinition recipe = LoadOrCreate<RecipeDefinition>(RecipeDir + spec.fileName + ".asset");
        SerializedObject so = new SerializedObject(recipe);
        so.FindProperty("displayName").stringValue = spec.displayName;
        so.FindProperty("outputItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.output + ".asset");
        so.FindProperty("outputQuantity").intValue = spec.outputQuantity;
        so.FindProperty("requiredStation").enumValueIndex = (int)(spec.anywhere ? CraftingStationType.None : CraftingStationType.Campfire);
        so.FindProperty("completionMessage").stringValue = spec.completionMessage;

        SerializedProperty inputs = so.FindProperty("inputs");
        inputs.arraySize = spec.inputs.Length;
        for (int i = 0; i < spec.inputs.Length; i++)
        {
            SerializedProperty input = inputs.GetArrayElementAtIndex(i);
            input.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.inputs[i].item + ".asset");
            input.FindPropertyRelative("quantity").intValue = spec.inputs[i].quantity;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    // 저장 파일이 id로 아이템을 찾을 수 있게 아이템 DB에 새 아이템을 등록한다.
    private static void RegisterItems()
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/ItemDatabase.asset");
        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("items");
        foreach (ItemSpec spec in Items)
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

    // ---- 재질 ----

    private static Material LitMaterial(string name, Color color, float smoothness)
    {
        string path = MaterialDir + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    // 불꽃은 부드러운 원형 텍스처를 더하기 혼합으로 그리는 URP 파티클 재질이다.
    private static Material FlameMaterial()
    {
        string path = MaterialDir + "Mat_Flame.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Texture2D soft = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        material.SetTexture("_BaseMap", soft);
        material.mainTexture = soft;
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---- 모닥불 ----

    private static BuildableDefinition BuildCampfire()
    {
        const string prefabPath = BuildingPrefabDir + "Buildable_Campfire.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        // 이미 있는 프리팹은 다시 만들지 않는다(GUID가 바뀌면 씬/세이브 참조가 끊긴다).
        if (prefab == null) prefab = BuildCampfirePrefab(prefabPath);

        BuildableDefinition definition = LoadOrCreate<BuildableDefinition>(BuildingDataDir + "Buildable_Campfire.asset");
        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("buildableId").stringValue = "furniture_campfire";
        so.FindProperty("displayName").stringValue = "모닥불";
        so.FindProperty("category").enumValueIndex = (int)BuildableCategory.Furniture;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("snapSize").floatValue = 0.5f;
        so.FindProperty("maxHealth").floatValue = 0f;

        (string item, int quantity)[] costs = { ("Item_Wood", 5), ("Item_ScrapMetal", 1) };
        SerializedProperty list = so.FindProperty("costs");
        list.arraySize = costs.Length;
        for (int i = 0; i < costs.Length; i++)
        {
            SerializedProperty cost = list.GetArrayElementAtIndex(i);
            cost.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + costs[i].item + ".asset");
            cost.FindPropertyRelative("quantity").intValue = costs[i].quantity;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static GameObject BuildCampfirePrefab(string path)
    {
        Material wood = LitMaterial("Mat_Campfire_Wood", new Color(0.30f, 0.19f, 0.10f), 0.15f);
        Material stone = LitMaterial("Mat_Campfire_Stone", new Color(0.42f, 0.42f, 0.40f), 0.2f);
        Material flame = FlameMaterial();

        GameObject root = new GameObject("Buildable_Campfire");

        // 돌 둘레
        const int stones = 8;
        for (int i = 0; i < stones; i++)
        {
            float angle = i * Mathf.PI * 2f / stones;
            GameObject rock = Primitive(PrimitiveType.Sphere, root.transform, "Stone" + i, stone);
            rock.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.42f, 0.08f, Mathf.Sin(angle) * 0.42f);
            rock.transform.localScale = new Vector3(0.24f, 0.17f, 0.22f);
            rock.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }

        // 통나무 네 개를 별 모양으로 눕혀 쌓는다.
        for (int i = 0; i < 4; i++)
        {
            GameObject log = Primitive(PrimitiveType.Cylinder, root.transform, "Log" + i, wood);
            log.transform.localPosition = new Vector3(0f, 0.11f + (i % 2) * 0.08f, 0f);
            log.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            log.transform.localScale = new Vector3(0.12f, 0.36f, 0.12f);
        }

        GameObject fire = new GameObject("Flame");
        fire.transform.SetParent(root.transform, false);
        fire.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        AddFlame(fire, flame);

        GameObject lightObject = new GameObject("FireLight");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        Light fireLight = lightObject.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.22f);
        fireLight.intensity = 1.8f;
        fireLight.range = 5f;
        fireLight.shadows = LightShadows.None;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.17f, 0f);
        box.size = new Vector3(1.0f, 0.34f, 1.0f);

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(ImpactSurface.Wood);
        CraftingStation station = root.AddComponent<CraftingStation>();
        SerializedObject stationSo = new SerializedObject(station);
        stationSo.FindProperty("type").enumValueIndex = (int)CraftingStationType.Campfire;
        stationSo.FindProperty("radius").floatValue = 3.5f;
        stationSo.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        AssetDatabase.DeleteAsset(path);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject Primitive(PrimitiveType type, Transform parent, string name, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void AddFlame(GameObject target, Material material)
    {
        ParticleSystem system = target.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.12f, 0.9f), new Color(1f, 0.82f, 0.3f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.16f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.6f), new GradientColorKey(new Color(0.4f, 0.05f, 0.0f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystemRenderer particleRenderer = target.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ---- 더러운 물 웅덩이 ----

    private static ResourceNodeDefinition BuildPuddleDefinition()
    {
        ResourceNodeDefinition definition = LoadOrCreate<ResourceNodeDefinition>(NodeDir + "ResourceNodeDef_DirtyPuddle.asset");
        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("yieldItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + "Item_Drink_DirtyWater.asset");
        so.FindProperty("yieldPerGather").intValue = 1;
        so.FindProperty("maxYield").intValue = 4;
        so.FindProperty("respawnSeconds").floatValue = 60f;
        so.FindProperty("impactSurface").enumValueIndex = (int)ImpactSurface.Dirt;
        ApplySounds(so, "gatherSounds", new[] { SoundDir + "Water Splash01.wav", SoundDir + "Water Splash02.ogg" }, 0.7f, new Vector2(0.85f, 0.95f));
        ApplySounds(so, "depleteSounds", new[] { AirBurst }, 0.5f, new Vector2(1.1f, 1.3f));
        so.FindProperty("depleteEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PuffPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void ApplySounds(SerializedObject so, string field, string[] files, float volume, Vector2 pitch)
    {
        SerializedProperty group = so.FindProperty(field);
        SerializedProperty clips = group.FindPropertyRelative("clips");
        clips.arraySize = files.Length;
        for (int i = 0; i < files.Length; i++)
            clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(files[i]);
        group.FindPropertyRelative("volume").floatValue = volume;
        group.FindPropertyRelative("pitchRange").vector2Value = pitch;
    }

    private static void BuildPuddlePrefab(ResourceNodeDefinition definition)
    {
        const string path = WorldPrefabDir + "Resource_DirtyPuddle.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        Material water = LitMaterial("Mat_DirtyWater", new Color(0.22f, 0.20f, 0.12f), 0.92f);

        GameObject root = new GameObject("Resource_DirtyPuddle");
        GameObject pool = Primitive(PrimitiveType.Cylinder, root.transform, "Pool", water);
        pool.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        pool.transform.localScale = new Vector3(1.6f, 0.012f, 1.2f);

        // 얕아서 지나다닐 수 있다. 채집 판정은 트리거도 잡는다.
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.1f, 0f);
        box.size = new Vector3(1.6f, 0.2f, 1.2f);
        box.isTrigger = true;

        ResourceNode node = root.AddComponent<ResourceNode>();
        SerializedObject so = new SerializedObject(node);
        so.FindProperty("definition").objectReferenceValue = definition;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetClearing(root, 1.5f);

        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    // 이 자리(반경 m)에는 풀을 심지 않게 표시한다.
    private static void SetClearing(GameObject root, float radius)
    {
        GrassClearing clearing = root.GetComponent<GrassClearing>() != null ? root.GetComponent<GrassClearing>() : root.AddComponent<GrassClearing>();
        SerializedObject so = new SerializedObject(clearing);
        so.FindProperty("radius").floatValue = radius;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 연못 ----

    // 웅덩이보다 훨씬 크고 많이 나오는 오염된 물 채집 지점이다. 맵의 물 보급처이고, 거점에서는 빗물 집수통을 쓴다.
    [MenuItem("ZGV01/Survival/Build Ponds")]
    public static void BuildPonds()
    {
        ResourceNodeDefinition definition = LoadOrCreate<ResourceNodeDefinition>(NodeDir + "ResourceNodeDef_Pond.asset");
        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("yieldItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + "Item_Drink_DirtyWater.asset");
        so.FindProperty("yieldPerGather").intValue = 2;
        so.FindProperty("maxYield").intValue = 12;
        so.FindProperty("respawnSeconds").floatValue = 45f;
        so.FindProperty("impactSurface").enumValueIndex = (int)ImpactSurface.Dirt;
        ApplySounds(so, "gatherSounds", new[] { SoundDir + "Water Splash01.wav", SoundDir + "Water Splash02.ogg" }, 0.7f, new Vector2(0.85f, 0.95f));
        ApplySounds(so, "depleteSounds", new[] { AirBurst }, 0.5f, new Vector2(1.1f, 1.3f));
        so.FindProperty("depleteEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PuffPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);

        const string path = WorldPrefabDir + "Resource_Pond.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            // 연못은 흙탕물 웅덩이보다 푸르스름하고 매끈하게 해 물로 읽히게 한다. 가장자리는 젖은 흙이다.
            Material water = LitMaterial("Mat_PondWater", new Color(0.09f, 0.2f, 0.17f), 0.97f);
            Material mud = LitMaterial("Mat_PondMud", new Color(0.25f, 0.19f, 0.11f), 0.1f);

            GameObject root = new GameObject("Resource_Pond");
            GameObject shore = Primitive(PrimitiveType.Cylinder, root.transform, "Shore", mud);
            shore.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            shore.transform.localScale = new Vector3(7.6f, 0.006f, 5.8f);
            GameObject pool = Primitive(PrimitiveType.Cylinder, root.transform, "Pool", water);
            pool.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            pool.transform.localScale = new Vector3(6.2f, 0.012f, 4.6f);

            // 얕아서 지나다닐 수 있다. 채집 판정은 트리거도 잡는다.
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.1f, 0f);
            box.size = new Vector3(6.2f, 0.2f, 4.6f);
            box.isTrigger = true;

            ResourceNode node = root.AddComponent<ResourceNode>();
            SerializedObject nodeSo = new SerializedObject(node);
            nodeSo.FindProperty("definition").objectReferenceValue = definition;
            nodeSo.ApplyModifiedPropertiesWithoutUndo();
            SetClearing(root, 4.6f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }

    // ---- 건축 목록 등록 ----

    private static void RegisterBuildable(BuildableDefinition definition)
    {
        BuildableDatabase database = AssetDatabase.LoadAssetAtPath<BuildableDatabase>("Assets/_Project/Resources/BuildableDatabase.asset");
        AddMissing(new SerializedObject(database), "buildables", definition);
        EditorUtility.SetDirty(database);

        const string playerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        AddMissing(new SerializedObject(playerPrefab.GetComponent<BuildingSystem>()), "buildables", definition);
        PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPrefabPath);
        PrefabUtility.UnloadPrefabContents(playerPrefab);

        GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
        if (scenePlayer != null && scenePlayer.GetComponent<BuildingSystem>() != null)
        {
            AddMissing(new SerializedObject(scenePlayer.GetComponent<BuildingSystem>()), "buildables", definition);
            EditorUtility.SetDirty(scenePlayer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scenePlayer.scene);
        }
    }

    private static void AddMissing(SerializedObject so, string field, BuildableDefinition definition)
    {
        SerializedProperty list = so.FindProperty(field);
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == definition) return;

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
