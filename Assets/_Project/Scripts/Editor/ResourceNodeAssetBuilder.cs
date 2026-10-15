// 음식/물을 얻는 채집 노드(열매 덤불, 물통, 보급 상자)의 아이템, 노드 정의, 프리팹을 만들어 주는 에디터 도구다. 벤더 프리팹은 중첩해서 참조만 한다.
using UnityEditor;
using UnityEngine;

public static class ResourceNodeAssetBuilder
{
    private const string ItemDir = "Assets/_Project/Data/Items/";
    private const string NodeDir = "Assets/_Project/Data/World/";
    private const string PrefabDir = "Assets/_Project/Prefabs/World/";

    private struct NodeSpec
    {
        public string fileName, vendorPath, yieldItem;
        public int yieldPerGather, maxYield;
        public float respawnSeconds;
        public ImpactSurface surface;
        public string[] sounds;
        public float soundVolume;
        public Vector2 soundPitch;
        // 마지막 수량까지 채집돼 사라질 때 겹쳐 내는 소리(부서지는 소리)다.
        public string[] depleteSounds;
        public float depleteVolume;
        public Vector2 depletePitch;
        // 지나갈 수 있는 노드(덤불)는 콜라이더를 트리거로 둔다. 채집 판정(겹침 검사)은 트리거도 잡는다.
        public bool passable;
    }

    private const string SoundDir = "Assets/Devion Games/Third Person Controller/Example/Sounds/";
    private const string AirBurst = "Assets/Devion Games/Inventory System/Examples/Sounds/Air Burst.ogg";
    private const string PuffPath = "Assets/_Project/Prefabs/Effects/Effect_DepletePuff.prefab";

    private static readonly NodeSpec[] Nodes =
    {
        new NodeSpec { fileName = "BerryBush", vendorPath = "Assets/Devion Games/Inventory System/Examples/Models/Bush/Bush.prefab", yieldItem = "Item_Food_Berries", yieldPerGather = 1, maxYield = 4, respawnSeconds = 90f, surface = ImpactSurface.Dirt, sounds = new[] { "Swing.wav" }, soundVolume = 0.55f, soundPitch = new Vector2(1.5f, 1.8f), depleteSounds = new[] { AirBurst }, depleteVolume = 0.5f, depletePitch = new Vector2(1.2f, 1.4f), passable = true },
        new NodeSpec { fileName = "WaterBarrel", vendorPath = "Assets/RPG_FPS_game_assets_industrial/Barrels/Barrel_v2/Barrel_v2_single.prefab", yieldItem = "Item_Drink_Water", yieldPerGather = 1, maxYield = 3, respawnSeconds = 120f, surface = ImpactSurface.Metal, sounds = new[] { "Water Splash01.wav", "Water Splash02.ogg" }, soundVolume = 0.8f, soundPitch = new Vector2(0.95f, 1.05f), depleteSounds = new[] { SoundDir + "Land.wav", AirBurst }, depleteVolume = 0.8f, depletePitch = new Vector2(1.0f, 1.15f) },
        new NodeSpec { fileName = "SupplyCrate", vendorPath = "Assets/Mine/Prefabs/Props/Crate.prefab", yieldItem = "Item_Food_CannedBeans", yieldPerGather = 1, maxYield = 2, respawnSeconds = 300f, surface = ImpactSurface.Wood, sounds = new[] { "Hammering.mp3" }, soundVolume = 0.9f, soundPitch = new Vector2(0.7f, 0.8f), depleteSounds = new[] { SoundDir + "Land.wav", AirBurst }, depleteVolume = 0.9f, depletePitch = new Vector2(0.8f, 0.95f) },
    };

    [MenuItem("ZGV01/World/Build Food And Water Nodes")]
    public static void Build()
    {
        BuildPuffEffect();
        BuildBerryItem();
        foreach (NodeSpec spec in Nodes)
        {
            ResourceNodeDefinition definition = BuildDefinition(spec);
            BuildPrefab(spec, definition);
        }
        BuildTreeSounds();
        AssetDatabase.SaveAssets();
        RegisterItems();
        AssetDatabase.SaveAssets();
    }

    // 프리팹은 건드리지 않고(씬에 배치한 인스턴스가 끊기지 않게) 노드 정의의 값과 채집 소리만 다시 적용한다.
    [MenuItem("ZGV01/World/Apply Node Definitions")]
    public static void ApplyDefinitions()
    {
        BuildPuffEffect();
        foreach (NodeSpec spec in Nodes) BuildDefinition(spec);
        BuildTreeSounds();
        AssetDatabase.SaveAssets();
    }

    // 노드가 사라질 때 피어오르는 짧은 연기(약 1초)다. 부드러운 원형 텍스처를 쓰는 URP 파티클이다.
    private static void BuildPuffEffect()
    {
        const string materialPath = "Assets/_Project/Materials/Effects/Mat_DepletePuff.mat";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials/Effects")) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Effects");
        AssetDatabase.DeleteAsset(materialPath);
        Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Texture2D soft = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        material.SetTexture("_BaseMap", soft);
        material.mainTexture = soft;
        AssetDatabase.CreateAsset(material, materialPath);

        GameObject root = new GameObject("Effect_DepletePuff");
        ParticleSystem system = root.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.6f, 0.55f, 0.75f), new Color(0.45f, 0.44f, 0.4f, 0.75f));
        main.gravityModifier = -0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 14) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 1.5f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        ParticleSystemRenderer particleRenderer = root.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        AssetDatabase.SaveAssets();
        AssetDatabase.DeleteAsset(PuffPath);
        PrefabUtility.SaveAsPrefabAsset(root, PuffPath);
        Object.DestroyImmediate(root);
    }

    // 열매는 허기를 조금 채우는 소비 아이템이다. 이미 있으면 필드만 갱신한다.
    private static void BuildBerryItem()
    {
        string path = ItemDir + "Item_Food_Berries.asset";
        ConsumableDefinition item = AssetDatabase.LoadAssetAtPath<ConsumableDefinition>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ConsumableDefinition>();
            AssetDatabase.CreateAsset(item, path);
        }

        SerializedObject so = new SerializedObject(item);
        so.FindProperty("stableId").stringValue = "food_berries";
        so.FindProperty("displayName").stringValue = "열매";
        so.FindProperty("category").intValue = (int)ItemCategory.Consumable;
        so.FindProperty("maxStack").intValue = 20;
        so.FindProperty("hungerRestore").floatValue = 12f;
        so.FindProperty("thirstRestore").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    private static ResourceNodeDefinition BuildDefinition(NodeSpec spec)
    {
        string path = NodeDir + "ResourceNodeDef_" + spec.fileName + ".asset";
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("yieldItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.yieldItem + ".asset");
        so.FindProperty("yieldPerGather").intValue = spec.yieldPerGather;
        so.FindProperty("maxYield").intValue = spec.maxYield;
        so.FindProperty("respawnSeconds").floatValue = spec.respawnSeconds;
        so.FindProperty("impactSurface").enumValueIndex = (int)spec.surface;
        ApplySounds(so, "gatherSounds", spec.sounds, spec.soundVolume, spec.soundPitch, SoundDir);
        ApplySounds(so, "depleteSounds", spec.depleteSounds, spec.depleteVolume, spec.depletePitch, "");
        so.FindProperty("depleteEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PuffPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    // directory가 비어 있으면 files를 전체 경로로 본다.
    private static void ApplySounds(SerializedObject so, string field, string[] files, float volume, Vector2 pitch, string directory)
    {
        SerializedProperty group = so.FindProperty(field);
        SerializedProperty clips = group.FindPropertyRelative("clips");
        clips.arraySize = files.Length;
        for (int i = 0; i < files.Length; i++)
            clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(directory + files[i]);
        group.FindPropertyRelative("volume").floatValue = volume;
        group.FindPropertyRelative("pitchRange").vector2Value = pitch;
    }

    // 기존 나무 노드 정의에도 채집 소리(나무 베는 타격음)를 지정한다.
    private static void BuildTreeSounds()
    {
        ResourceNodeDefinition tree = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(NodeDir + "ResourceNodeDef_Tree.asset");
        if (tree == null) return;

        SerializedObject so = new SerializedObject(tree);
        ApplySounds(so, "gatherSounds", new[] { "Hammering.mp3" }, 0.9f, new Vector2(0.95f, 1.05f), SoundDir);
        ApplySounds(so, "depleteSounds", new[] { SoundDir + "Land.wav", AirBurst }, 0.9f, new Vector2(0.7f, 0.85f), "");
        so.FindProperty("depleteEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PuffPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tree);
    }

    // 채집은 콜라이더가 붙은 오브젝트에서 ResourceNode를 찾으므로 루트에 박스 콜라이더와 노드를 단다. 벤더의 콜라이더는 끈다.
    private static void BuildPrefab(NodeSpec spec, ResourceNodeDefinition definition)
    {
        GameObject root = new GameObject("Resource_" + spec.fileName);
        GameObject vendorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.vendorPath);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(vendorAsset, root.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        Bounds bounds = RendererBounds(model);
        model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

        foreach (Collider vendorCollider in model.GetComponentsInChildren<Collider>()) vendorCollider.enabled = false;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
        box.size = bounds.size;
        box.isTrigger = spec.passable;

        ResourceNode node = root.AddComponent<ResourceNode>();
        SerializedObject so = new SerializedObject(node);
        so.FindProperty("definition").objectReferenceValue = definition;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "Resource_" + spec.fileName + ".prefab";
        AssetDatabase.SaveAssets();
        AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Bounds RendererBounds(GameObject go)
    {
        bool any = false;
        Bounds bounds = new Bounds();
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    // 저장 파일이 id로 아이템을 찾을 수 있게 아이템 DB에 열매를 등록한다.
    private static void RegisterItems()
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/ItemDatabase.asset");
        ItemDefinition berries = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + "Item_Food_Berries.asset");
        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("items");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == berries) return;

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = berries;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }
}
