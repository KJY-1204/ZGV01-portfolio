// 플레이 맵 씬(Main_Map)을 준비하고 지형을 만드는 에디터 메뉴다. 테스트 씬(Test_Combat)을 복사해 플레이어/HUD/시스템은 그대로 두고 바닥과 시험용 오브젝트를 걷어 낸다.
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapSceneBuilder
{
    private const string SourceScene = "Assets/_Project/Scenes/Test/Test_Combat.unity";

    // 씬을 연다(없으면 Test_Combat을 복사해 만든다). 맵에서 다시 만들 오브젝트는 걷어 낸다.
    public static Scene OpenOrCreateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MapLayout.ScenePath) == null)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MapLayout.ScenePath));
            if (!AssetDatabase.CopyAsset(SourceScene, MapLayout.ScenePath)) throw new System.InvalidOperationException("씬을 복사하지 못했다: " + MapLayout.ScenePath);
        }
        Scene scene = EditorSceneManager.OpenScene(MapLayout.ScenePath, OpenSceneMode.Single);

        // 시험용 바닥, 훈련 표적, 시험 나무와 기존 소규모 자원은 맵 배치 단계가 다시 만든다. 스폰너는 복제 원본으로 남긴다.
        foreach (string name in new[] { "Floor", "TrainingDummy", "Tree", "FoodAndWater" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }
        return scene;
    }

    // 지형 위에 구역 건물/소품/바위/채집 나무/자원 노드를 배치한다.
    [MenuItem("ZGV01/Map/2 Place Props")]
    public static void PlaceProps()
    {
        Scene scene = OpenOrCreateScene();
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null) throw new System.InvalidOperationException("지형이 없다. 먼저 1 Build Terrain을 실행한다.");
        System.Collections.Generic.HashSet<string> used = MapPropLayout.Build(terrain);
        Debug.Log($"맵 소품 배치 완료: 프리팹 {used.Count}종");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 순서대로 지형, 소품, 스폰너/NavMesh, 경계 벽, 빌드 설정 등록까지 한 번에 만든다.
    [MenuItem("ZGV01/Map/Build All")]
    public static void BuildAll()
    {
        BuildTerrain();
        PlaceProps();
        SpawnersAndNavMesh();
        FinishScene();
    }

    // 지형 가장자리 안쪽에 보이지 않는 벽을 두르고 이 씬을 빌드 설정의 첫 씬으로 등록한다.
    [MenuItem("ZGV01/Map/4 Bounds And Build Settings")]
    public static void FinishScene()
    {
        Scene scene = OpenOrCreateScene();
        GameObject old = GameObject.Find("MapBounds");
        if (old != null) Object.DestroyImmediate(old);
        GameObject bounds = new GameObject("MapBounds");
        float half = MapLayout.Size * 0.5f - MapLayout.RimWallDistance;
        float length = MapLayout.Size, thickness = 2f, height = 200f;
        AddWall(bounds.transform, "North", new Vector3(0f, height * 0.5f, half), new Vector3(length, height, thickness));
        AddWall(bounds.transform, "South", new Vector3(0f, height * 0.5f, -half), new Vector3(length, height, thickness));
        AddWall(bounds.transform, "East", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, length));
        AddWall(bounds.transform, "West", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, length));

        AddSkyCycle();
        AddTreeGather();

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MapLayout.ScenePath, true) };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 지형 나무를 모두 채집 가능하게 하는 TerrainTreeGather를 씬에 둔다(채집 정의를 직접 연결한다).
    private static void AddTreeGather()
    {
        TerrainTreeGather gather = Object.FindFirstObjectByType<TerrainTreeGather>();
        if (gather == null) gather = new GameObject("TerrainTreeGather").AddComponent<TerrainTreeGather>();
        SerializedObject so = new SerializedObject(gather);
        so.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Project/Data/World/ResourceNodeDef_Tree.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // GameClock 오브젝트에 시간대별 하늘(SkySeries Freebie) 컴포넌트를 붙인다. 에디터 화면에서는 낮 하늘이 보이게 씬 스카이박스도 맞춘다.
    private static void AddSkyCycle()
    {
        GameClock clock = Object.FindFirstObjectByType<GameClock>();
        if (clock == null) return;

        SkyCycle cycle = clock.GetComponent<SkyCycle>();
        if (cycle == null) cycle = clock.gameObject.AddComponent<SkyCycle>();

        System.Func<string, Cubemap> cubemapOf = name =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/SkySeries Freebie/" + name + ".mat")?.GetTexture("_Tex") as Cubemap;

        SerializedObject so = new SerializedObject(cycle);
        so.FindProperty("skyShader").objectReferenceValue = Shader.Find("ZGV/SkyBlend");
        // 낮음 품질의 단순 하늘(내장 셰이더). 직렬화해 두어야 빌드에서 셰이더가 빠지지 않는다.
        so.FindProperty("simpleSkyShader").objectReferenceValue = Shader.Find("Skybox/Cubemap");
        so.FindProperty("nightSky").objectReferenceValue = cubemapOf("CoriolisNight4k");
        so.FindProperty("glowSky").objectReferenceValue = cubemapOf("CloudedSunGlow");
        so.FindProperty("daySky").objectReferenceValue = cubemapOf("SunlessCirruscover");
        so.FindProperty("sun").objectReferenceValue = RenderSettings.sun != null ? RenderSettings.sun : Object.FindFirstObjectByType<Light>();
        so.ApplyModifiedPropertiesWithoutUndo();

        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/SkySeries Freebie/SunlessCirruscover.mat");
    }

    private static void AddWall(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = center;
        wall.AddComponent<BoxCollider>().size = size;
    }

    private const string NavMeshAssetPath = "Assets/_Project/Scenes/Main/Main_Map_NavMesh.asset";

    // 구역별 스폰너를 만들고 NavMesh를 굽는다. 스폰너 원본(ZombieSpawner/AnimalSpawner/PredatorSpawner)은 비활성으로 두고 복제한다.
    [MenuItem("ZGV01/Map/3 Spawners And NavMesh")]
    public static void SpawnersAndNavMesh()
    {
        Scene scene = OpenOrCreateScene();
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null) throw new System.InvalidOperationException("지형이 없다. 먼저 1 Build Terrain을 실행한다.");

        BuildSpawners(terrain);
        BakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private struct SpawnerSpec
    {
        public string template;   // 복제할 원본 이름
        public string name;
        public Vector2 position;
        public float radius;
        public int maxAlive, initialCount;
        public bool indoor;       // 광산처럼 지형 아래 실내 구역이다(바닥 높이는 광산 바닥과 같다)
    }

    private static void BuildSpawners(Terrain terrain)
    {
        GameObject oldRoot = GameObject.Find("Spawners");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        Transform root = new GameObject("Spawners").transform;

        // 원본은 한 번만 비활성으로 접어 두고(이미 접혀 있으면 그대로) 복제해 쓴다. GameObject.Find는 비활성 오브젝트를 못 찾으므로 루트를 직접 훑는다.
        GameObject templateRoot = null;
        foreach (GameObject root0 in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root0.name != "SpawnerTemplates") continue;
            if (templateRoot == null) templateRoot = root0;
            else if (root0.transform.childCount > templateRoot.transform.childCount) { Object.DestroyImmediate(templateRoot); templateRoot = root0; }
            else Object.DestroyImmediate(root0);
        }
        Transform templates = templateRoot != null ? templateRoot.transform : new GameObject("SpawnerTemplates").transform;
        foreach (string name in new[] { "ZombieSpawner", "AnimalSpawner", "PredatorSpawner", "HorseSpawner" })
        {
            foreach (GameObject candidate in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (candidate.name != name) continue;
                candidate.transform.SetParent(templates, false);
                candidate.SetActive(false);
            }
        }
        templates.gameObject.SetActive(false);

        var specs = new System.Collections.Generic.List<SpawnerSpec>
        {
            // 좀비: 위험한 구역일수록 많고 시작 야영지는 거의 없다. 플레이어 가까이(22~45m)에서만 태어난다.
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Camp", position = MapLayout.Zones[0].center, radius = 75f, maxAlive = 3, initialCount = 1 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Warehouse", position = MapLayout.Zones[1].center, radius = 150f, maxAlive = 14, initialCount = 8 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Factory", position = MapLayout.Zones[2].center, radius = 160f, maxAlive = 18, initialCount = 10 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_School", position = MapLayout.Zones[3].center, radius = 120f, maxAlive = 12, initialCount = 7 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Yard", position = MapLayout.Zones[4].center, radius = 120f, maxAlive = 12, initialCount = 7 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Quarry", position = MapLayout.Zones[5].center, radius = 110f, maxAlive = 8, initialCount = 4 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_Mine", position = new Vector2(MapLayout.MinePortal.x, MapLayout.MinePortal.y + 24f), radius = 85f, maxAlive = 8, initialCount = 4, indoor = true },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_RoadEast", position = new Vector2(225f, 40f), radius = 90f, maxAlive = 6, initialCount = 3 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_RoadWest", position = new Vector2(-265f, 40f), radius = 90f, maxAlive = 6, initialCount = 3 },
            new SpawnerSpec { template = "ZombieSpawner", name = "Zombies_RoadSouth", position = new Vector2(150f, -285f), radius = 80f, maxAlive = 4, initialCount = 2 },
            // 초식 동물: 야영지 근처 숲/들판과 외곽 숲 띠.
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_SouthWest", position = new Vector2(-200f, -400f), radius = 150f, maxAlive = 10, initialCount = 8 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_West", position = new Vector2(-420f, -40f), radius = 160f, maxAlive = 10, initialCount = 8 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_Center", position = new Vector2(60f, 110f), radius = 120f, maxAlive = 9, initialCount = 7 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_East", position = new Vector2(430f, 120f), radius = 150f, maxAlive = 9, initialCount = 7 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_NorthWest", position = new Vector2(-380f, 330f), radius = 130f, maxAlive = 8, initialCount = 6 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_NorthEast", position = new Vector2(380f, 340f), radius = 130f, maxAlive = 8, initialCount = 6 },
            new SpawnerSpec { template = "AnimalSpawner", name = "Animals_SouthEast", position = new Vector2(330f, -330f), radius = 130f, maxAlive = 8, initialCount = 6 },
            // 말(탈것): 야영지 근처에 두 마리, 나머지는 들판에 흩어 둔다(사냥감 분포와 따로 관리한다).
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_Camp", position = new Vector2(-110f, -320f), radius = 60f, maxAlive = 3, initialCount = 3 },
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_Center", position = new Vector2(60f, 110f), radius = 110f, maxAlive = 3, initialCount = 2 },
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_West", position = new Vector2(-410f, -40f), radius = 150f, maxAlive = 4, initialCount = 3 },
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_East", position = new Vector2(430f, 120f), radius = 150f, maxAlive = 4, initialCount = 3 },
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_NorthEast", position = new Vector2(370f, 330f), radius = 130f, maxAlive = 3, initialCount = 2 },
            new SpawnerSpec { template = "HorseSpawner", name = "Horses_SouthEast", position = new Vector2(330f, -330f), radius = 130f, maxAlive = 3, initialCount = 2 },
            // 맹수: 야영지에서 멀리 숲 깊은 곳에만 둔다(플레이어 35m 안에서는 태어나지 않는다).
            new SpawnerSpec { template = "PredatorSpawner", name = "Predators_West", position = new Vector2(-430f, 60f), radius = 140f, maxAlive = 4, initialCount = 3 },
            new SpawnerSpec { template = "PredatorSpawner", name = "Predators_East", position = new Vector2(440f, 40f), radius = 140f, maxAlive = 4, initialCount = 3 },
            new SpawnerSpec { template = "PredatorSpawner", name = "Predators_Center", position = new Vector2(30f, 140f), radius = 100f, maxAlive = 3, initialCount = 2 },
            new SpawnerSpec { template = "PredatorSpawner", name = "Predators_NorthWest", position = new Vector2(-400f, 330f), radius = 130f, maxAlive = 3, initialCount = 2 },
            new SpawnerSpec { template = "PredatorSpawner", name = "Predators_NorthEast", position = new Vector2(400f, 340f), radius = 130f, maxAlive = 3, initialCount = 2 },
        };

        foreach (SpawnerSpec spec in specs)
        {
            Transform source = templates.Find(spec.template);
            if (source == null) { Debug.LogWarning("스폰너 원본이 없다: " + spec.template); continue; }
            GameObject clone = Object.Instantiate(source.gameObject, root);
            clone.name = spec.name;
            float y = terrain.SampleHeight(new Vector3(spec.position.x, 0f, spec.position.y)) + terrain.transform.position.y;
            clone.transform.position = new Vector3(spec.position.x, y, spec.position.y);
            foreach (MonoBehaviour behaviour in clone.GetComponents<MonoBehaviour>())
            {
                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty radius = so.FindProperty("radius");
                if (radius == null) continue;
                radius.floatValue = spec.radius;
                so.FindProperty("maxAlive").intValue = spec.maxAlive;
                so.FindProperty("initialCount").intValue = spec.initialCount;
                SerializedProperty indoor = so.FindProperty("indoorFloor");
                if (indoor != null)
                {
                    indoor.boolValue = spec.indoor;
                    // 광산 바닥 높이(MineBuilder의 floorY와 같은 지점에서 잰다).
                    so.FindProperty("indoorFloorY").floatValue = terrain.SampleHeight(new Vector3(MapLayout.MinePortal.x, 0f, MapLayout.MinePortal.y - 12f)) + terrain.transform.position.y;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            clone.SetActive(true);
        }
    }

    // 지형과 모든 콜라이더를 모아 NavMesh를 굽는다. 플레이어는 구멍이 나지 않게 굽는 동안 잠시 끈다.
    private static void BakeNavMesh()
    {
        GameObject holder = GameObject.Find("NavMesh");
        if (holder != null) Object.DestroyImmediate(holder);
        holder = new GameObject("NavMesh");
        NavMeshSurface surface = holder.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.25f;
        surface.overrideTileSize = true;
        surface.tileSize = 256;
        surface.minRegionArea = 4f;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool wasActive = player != null && player.activeSelf;
        if (player != null) player.SetActive(false);
        try
        {
            surface.BuildNavMesh();
            if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
                AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
        }
        finally
        {
            if (player != null) player.SetActive(wasActive);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("ZGV01/Map/1 Build Terrain")]
    public static void BuildTerrain()
    {
        Scene scene = OpenOrCreateScene();
        MapTreeAdapterBuilder.Build();
        Terrain terrain = MapTerrainBuilder.Build();

        // 플레이어를 시작 야영지 중심의 지면 위에 놓는다.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 start = MapLayout.PlayerStart;
            float y = terrain.SampleHeight(new Vector3(start.x, 0f, start.y)) + terrain.transform.position.y;
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = new Vector3(start.x, y + 0.05f, start.y);
            player.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            if (controller != null) controller.enabled = true;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
