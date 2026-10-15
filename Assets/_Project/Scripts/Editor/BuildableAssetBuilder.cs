// 외부 건축/가구 에셋을 감싸는 건축물 프리팹과 정의 에셋을 만들어 주는 에디터 도구다. 벤더 프리팹은 중첩해서 참조만 하고 수정하지 않는다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildableAssetBuilder
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Building";
    private const string DefinitionDir = "Assets/_Project/Data/Building";
    private const string ItemDir = "Assets/_Project/Data/Items/";
    private const string Kats = "Assets/KatsBits-fantasy-base/Prefabs/";
    private const string Cabin = "Assets/FurnishedCabin/Prefabs/";
    private const string Fortification = "Assets/Medieval Fortification/Prefabs/";

    private enum ColliderMode { Box, Mesh, Vendor }

    private struct Spec
    {
        public string id, fileName, displayName, vendorPath;
        public BuildableCategory category;
        public ImpactSurface surface;
        public float snap;
        public BuildSnapMode snapMode;
        public WallAttach wallAttach;
        public ColliderMode collider;
        public bool ladder;
        // 0보다 크면 이 칸 수의 보관함(StorageContainer)이 된다.
        public int storageSlots;
        // 참이면 부활 지점(RespawnPoint)이 된다(침대).
        public bool respawnPoint;
        // 참이면 오염된 물이 차오르는 빗물 집수통(RainCollector)이 된다(보관함 칸 수도 함께 정한다).
        public bool rainCollector;
        // 0보다 크면 밤에 켜지는 조명(NightLight)이 붙는다. 모델 꼭대기에 점광원을 둔다.
        public float lightRange, lightIntensity, lightFlicker;
        public Color lightColor;
        // 모델을 바닥에서 이만큼 띄워 놓는다(벽에 거는 조명).
        public float raise;
        // 벤더 프리팹의 원점이 이음매(교차점/모서리)라서 x,z는 원점을 그대로 두고 높이만 바닥에 맞춘다.
        public bool keepOrigin;
        // 한 면만 그려지는 평면 조각의 뒤집은 복사본을 겹쳐 아래에서도 보이게 한다.
        public bool doubleSided;
        public (string item, int quantity)[] costs;
    }

    private static readonly Spec[] Specs =
    {
        new Spec { id = "pillar_stone", fileName = "Pillar", snapMode = BuildSnapMode.Vertex, displayName = "석조 기둥", vendorPath = Kats + "Walls_plain/Pillar.prefab", category = BuildableCategory.Wall, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },
        new Spec { id = "wall_long", fileName = "LongWall", snapMode = BuildSnapMode.Vertex, displayName = "긴 석벽", vendorPath = Kats + "Walls_plain/Wall.Plain.4U.prefab", category = BuildableCategory.Wall, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, costs = new[] { ("Item_BasicBuildingMaterial", 5) } },

        new Spec { id = "wall_t", fileName = "WallT", snapMode = BuildSnapMode.Vertex, displayName = "T자 벽", vendorPath = Kats + "Walls_plain/Wall.T.prefab", category = BuildableCategory.Wall, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, keepOrigin = true, costs = new[] { ("Item_BasicBuildingMaterial", 5) } },
        new Spec { id = "wall_x", fileName = "WallX", snapMode = BuildSnapMode.Vertex, displayName = "십자 벽", vendorPath = Kats + "Walls_plain/Wall.X.prefab", category = BuildableCategory.Wall, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, keepOrigin = true, costs = new[] { ("Item_BasicBuildingMaterial", 6) } },
        new Spec { id = "pillar_corner", fileName = "PillarCorner", snapMode = BuildSnapMode.Vertex, displayName = "모서리 기둥", vendorPath = Kats + "Walls_plain/Pillar.Cnr.Outer.prefab", category = BuildableCategory.Wall, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },

        new Spec { id = "roof_slab", fileName = "RoofSlab", displayName = "석판 지붕", vendorPath = Kats + "Ground/Paving.Slabs [2x2].prefab", category = BuildableCategory.Roof, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, doubleSided = true, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },
        new Spec { id = "roof_mixed", fileName = "RoofMixed", displayName = "혼합 석판 지붕", vendorPath = Kats + "Ground/Paving.Mix [2x2].prefab", category = BuildableCategory.Roof, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, doubleSided = true, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },


        new Spec { id = "floor_stone", fileName = "FloorStone", displayName = "돌 바닥", vendorPath = Kats + "Ground/Paving.Stone [2x2].prefab", category = BuildableCategory.Floor, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },
        new Spec { id = "floor_mixed", fileName = "FloorMixed", displayName = "혼합 포장 바닥", vendorPath = Kats + "Ground/Paving.Mix [2x2].prefab", category = BuildableCategory.Floor, surface = ImpactSurface.Concrete, collider = ColliderMode.Box, costs = new[] { ("Item_BasicBuildingMaterial", 2) } },
        new Spec { id = "stairs_stone", fileName = "Stairs", displayName = "돌 계단", vendorPath = Kats + "Ground/Steps [1x2].prefab", category = BuildableCategory.Floor, surface = ImpactSurface.Concrete, collider = ColliderMode.Mesh, costs = new[] { ("Item_BasicBuildingMaterial", 3) } },

        new Spec { id = "furniture_bed", fileName = "Bed", respawnPoint = true, wallAttach = WallAttach.FrontPlusZ, displayName = "침대", vendorPath = Cabin + "PFB_Bed.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, costs = new[] { ("Item_Wood", 8) } },
        new Spec { id = "furniture_table", fileName = "Table", displayName = "식탁", vendorPath = Cabin + "PFB_DiningTable.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, costs = new[] { ("Item_Wood", 6) } },
        new Spec { id = "furniture_chair", fileName = "Chair", displayName = "의자", vendorPath = Cabin + "PFB_DiningChair.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, costs = new[] { ("Item_Wood", 3) } },
        new Spec { id = "furniture_sofa", fileName = "Sofa", wallAttach = WallAttach.FrontPlusZ, displayName = "소파", vendorPath = Cabin + "PFB_Sofa.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, costs = new[] { ("Item_Wood", 6) } },
        new Spec { id = "storage_drawers", fileName = "StorageDrawers", wallAttach = WallAttach.FrontPlusZ, displayName = "서랍장 보관함", vendorPath = Cabin + "PFB_ChestOfDraws.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, storageSlots = 20, costs = new[] { ("Item_Wood", 6) } },
        new Spec { id = "storage_closet", fileName = "StorageCloset", wallAttach = WallAttach.FrontPlusZ, displayName = "옷장 보관함", vendorPath = Cabin + "PFB_Closet.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, storageSlots = 40, costs = new[] { ("Item_Wood", 10), ("Item_ScrapMetal", 2) } },
        new Spec { id = "water_collector", fileName = "RainCollector", displayName = "빗물 집수통", vendorPath = "Assets/RPG_FPS_game_assets_industrial/Barrels/Barrel_v2/Barrel_v2_single.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Metal, snap = 0.5f, collider = ColliderMode.Box, storageSlots = 2, rainCollector = true, costs = new[] { ("Item_ScrapMetal", 4), ("Item_Wood", 2) } },
        new Spec { id = "light_torch", fileName = "Torch", displayName = "횃불", vendorPath = Kats + "Props/Prop.Lantern.Trch.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Box, lightRange = 11f, lightIntensity = 2.2f, lightFlicker = 0.14f, costs = new[] { ("Item_Wood", 2), ("Item_ScrapMetal", 1) } },
        new Spec { id = "light_wall_lantern", fileName = "WallLantern", wallAttach = WallAttach.FrontPlusZ, raise = 1.7f, displayName = "벽 랜턴", vendorPath = Kats + "Props/Prop.Lantern.Mnt.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Metal, snap = 0.5f, collider = ColliderMode.Box, lightRange = 10f, lightIntensity = 2f, lightFlicker = 0.08f, costs = new[] { ("Item_ScrapMetal", 3) } },
        new Spec { id = "light_lamp", fileName = "TableLamp", displayName = "탁상 램프", vendorPath = Cabin + "PFB_Lamp.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Box, lightRange = 8f, lightIntensity = 1.6f, lightColor = new Color(1f, 0.86f, 0.62f), costs = new[] { ("Item_ScrapMetal", 2), ("Item_BasicBuildingMaterial", 1) } },
        new Spec { id = "furniture_stove", fileName = "Stove", wallAttach = WallAttach.FrontPlusZ, displayName = "난로", vendorPath = Cabin + "PFB_Stove.prefab", category = BuildableCategory.Furniture, surface = ImpactSurface.Metal, snap = 0.5f, collider = ColliderMode.Vendor, costs = new[] { ("Item_ScrapMetal", 6), ("Item_BasicBuildingMaterial", 2) } },

        new Spec { id = "defense_gate", fileName = "FortGate", displayName = "방책 성문", vendorPath = Fortification + "Fortification_Gate.prefab", category = BuildableCategory.Defense, surface = ImpactSurface.Wood, collider = ColliderMode.Vendor, costs = new[] { ("Item_RefinedMaterial", 6) } },
        new Spec { id = "defense_ladder", fileName = "Ladder", wallAttach = WallAttach.FrontMinusZ, displayName = "사다리", vendorPath = Fortification + "Fortification_Ladder.prefab", category = BuildableCategory.Defense, surface = ImpactSurface.Wood, snap = 0.5f, collider = ColliderMode.Vendor, ladder = true, costs = new[] { ("Item_Wood", 4), ("Item_ScrapMetal", 2) } },
        new Spec { id = "defense_tower", fileName = "WatchTower", displayName = "감시탑", vendorPath = Fortification + "Fortification_Tower_Front.prefab", category = BuildableCategory.Defense, surface = ImpactSurface.Wood, collider = ColliderMode.Vendor, costs = new[] { ("Item_RefinedMaterial", 8), ("Item_Wood", 4) } },
    };

    [MenuItem("ZGV01/Building/Build Extra Buildables")]
    public static void Build()
    {
        List<BuildableDefinition> created = new List<BuildableDefinition>();
        foreach (Spec spec in Specs)
        {
            // 이미 있는 프리팹은 다시 만들지 않는다(GUID가 바뀌면 참조가 끊긴다).
            string existing = PrefabDir + "/Buildable_" + spec.fileName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(existing);
            if (prefab != null) continue;
            prefab = BuildPrefab(spec);
            created.Add(BuildDefinition(spec, prefab));
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + DoorSpec.fileName + ".prefab") == null)
            created.Add(BuildDefinition(DoorSpec, BuildDoorPrefab()));
        foreach (WindowSpec window in Windows)
        {
            // 이미 있는 창문 프리팹은 다시 만들지 않는다(GUID가 바뀌면 씬/세이브 참조가 끊긴다).
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + window.spec.fileName + ".prefab") != null) continue;
            created.Add(BuildDefinition(window.spec, BuildWindowPrefab(window)));
        }

        foreach (DoorVariant variant in DoorVariants)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + variant.spec.fileName + ".prefab") != null) continue;
            created.Add(BuildDefinition(variant.spec, BuildDoorVariantPrefab(variant)));
        }
        foreach ((Spec spec, bool gable) roof in new[] { (RoofSlopeSpec, false), (RoofGableSpec, true) })
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + roof.spec.fileName + ".prefab") != null) continue;
            created.Add(BuildDefinition(roof.spec, BuildRoofPrefab(roof.spec, roof.gable)));
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + BarricadeSpec.fileName + ".prefab") == null)
            created.Add(BuildDefinition(BarricadeSpec, BuildBarricadePrefab()));
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + FenceSpec.fileName + ".prefab") == null)
            created.Add(BuildDefinition(FenceSpec, BuildFencePrefab()));

        AssetDatabase.SaveAssets();
        Register(created);

        // 시체 가방은 저장/불러오기에서만 필요한 조각이라 건축 DB에만 넣고 플레이어의 건축 메뉴에는 넣지 않는다.
        GameObject bagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Buildable_" + CorpseBagSpec.fileName + ".prefab");
        if (bagPrefab == null)
        {
            bagPrefab = BuildCorpseBagPrefab();
            BuildableDefinition bagDefinition = BuildDefinition(CorpseBagSpec, bagPrefab);
            AddMissing(new SerializedObject(AssetDatabase.LoadAssetAtPath<BuildableDatabase>("Assets/_Project/Resources/BuildableDatabase.asset")), "buildables", new List<BuildableDefinition> { bagDefinition });
        }
        AttachCorpseBagToPlayer(bagPrefab);
        AssetDatabase.SaveAssets();
    }

    private static readonly Spec CorpseBagSpec = new Spec
    {
        id = "corpse_bag", fileName = "CorpseBag", displayName = "시체 가방", category = BuildableCategory.Furniture,
        surface = ImpactSurface.SoftBody, costs = new (string, int)[0],
    };

    // 군용 배낭 메시를 눕혀 놓은 가방이다. 트리거 콜라이더라 걸리적거리지 않고, 보관함 컴포넌트(칸 제한 없음, 비면 사라짐)를 단다.
    private static GameObject BuildCorpseBagPrefab()
    {
        GameObject root = new GameObject("Buildable_" + CorpseBagSpec.fileName);

        Mesh mesh = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Survivalist/Basemesh/SK_Military_Survivalist.fbx"))
            if (asset is Mesh candidate && candidate.name == "SK_Military_Backpack1") mesh = candidate;

        GameObject model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        MeshFilter filter = model.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Survivalist/Materials URP/URP_Backpack1_Skin1.mat");
        // 등판이 바닥에 닿도록 눕히고 1.4배로 키운 뒤 바닥 중앙이 원점에 오도록 옮긴다.
        model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        model.transform.localScale = Vector3.one * 1.4f;
        Bounds bounds = RendererBounds(model);
        model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        Bounds placed = RendererBounds(model);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = placed.center;
        box.size = placed.size;

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(CorpseBagSpec.surface);
        root.AddComponent<Inventory>();
        // 나침반에 위치를 띄운다(수명이 60초 미만이면 깜빡인다).
        root.AddComponent<CompassMarker>();
        StorageContainer storage = root.AddComponent<StorageContainer>();
        SerializedObject so = new SerializedObject(storage);
        so.FindProperty("displayName").stringValue = "시체 가방";
        so.FindProperty("slotCount").intValue = 0;
        so.FindProperty("destroyWhenEmpty").boolValue = true;
        // 현실 시간 10분(5~10분 범위의 상한) 안에 못 찾으면 사라진다.
        so.FindProperty("lifetimeSeconds").floatValue = 600f;
        so.FindProperty("lootPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/World/LootPickup.prefab");
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabDir + "/Buildable_" + CorpseBagSpec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 플레이어 프리팹(과 열린 씬의 인스턴스)의 PlayerRespawn에 시체 가방 프리팹을 연결한다.
    private static void AttachCorpseBagToPlayer(GameObject bagPrefab)
    {
        const string playerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        PlayerRespawn respawn = playerPrefab.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            SerializedObject so = new SerializedObject(respawn);
            so.FindProperty("corpseBagPrefab").objectReferenceValue = bagPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPrefabPath);
        }
        PrefabUtility.UnloadPrefabContents(playerPrefab);
    }

    private static readonly Spec RoofSlopeSpec = new Spec
    {
        id = "roof_slope", fileName = "RoofSlope", displayName = "경사 지붕", category = BuildableCategory.Roof,
        surface = ImpactSurface.Concrete, costs = new[] { ("Item_BasicBuildingMaterial", 3) },
    };

    private static readonly Spec RoofGableSpec = new Spec
    {
        id = "roof_gable", fileName = "RoofGable", displayName = "박공 지붕", category = BuildableCategory.Roof,
        surface = ImpactSurface.Concrete, costs = new[] { ("Item_BasicBuildingMaterial", 4) },
    };

    private static readonly Spec DoorSpec = new Spec
    {
        id = "door_arch", fileName = "ArchDoor", displayName = "아치 문", category = BuildableCategory.Door,
        surface = ImpactSurface.Concrete, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3), ("Item_Wood", 2) },
    };

    // 문 모양을 바꿀 때 다시 만든다(프리팹은 같은 경로에 덮어써 참조를 유지한다).
    [MenuItem("ZGV01/Building/Rebuild Arch Door")]
    public static void RebuildDoor()
    {
        BuildDefinition(DoorSpec, BuildDoorPrefab());
        AssetDatabase.SaveAssets();
    }

    // 창문은 벽 모델(폭 wallWidth, 높이 2m, 원점이 폭의 가운데·바닥)을 잘라 쌓아 만든다. 아랫단(sill)과 윗단(lintel) 사이를 열어 개구부로 둔다.
    // 사격은 개구부로 통과하고 사람은 지나지 못한다. pillarWidth가 0보다 크면 양옆에 그 폭의 기둥(전체 높이)을 세워 넓은 창을 만든다.
    private enum Infill { None, Bars, Shutter }

    private struct WindowSpec
    {
        public Infill infill;                     // 개구부를 채우는 것: 없음, 쇠창살, 여닫이 덧창
        public Spec spec;
        public string wall;                       // Kats + "Walls_plain/" 아래 벽 프리팹 이름
        public float wallWidth, sill, lintelBottom, pillarWidth;
    }

    private const float WindowWallHeight = 2.0f;

    // 개구부 높이: 낮은 창 1.0~1.5, 높은 창 1.2~1.7(눈높이 위 가는 창), 큰 창 0.7~1.8(트인 창), 넓은 창 0.9~1.6(폭 2.4m).
    private static readonly WindowSpec[] Windows =
    {
        new WindowSpec { wall = "Wall.Plain.2U.prefab", wallWidth = 2f, sill = 1.0f, lintelBottom = 1.5f, spec = new Spec
            { id = "window_low", fileName = "WindowLow", displayName = "낮은 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Concrete, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3) } } },
        new WindowSpec { wall = "Wall.Plain.2U.prefab", wallWidth = 2f, sill = 1.2f, lintelBottom = 1.7f, spec = new Spec
            { id = "window_high", fileName = "WindowHigh", displayName = "높은 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Concrete, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3) } } },
        new WindowSpec { wall = "Wall.Plain.2U.prefab", wallWidth = 2f, sill = 0.7f, lintelBottom = 1.8f, spec = new Spec
            { id = "window_large", fileName = "WindowLarge", displayName = "큰 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Concrete, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3) } } },
        new WindowSpec { wall = "Wall.Plain.4U.prefab", wallWidth = 4f, sill = 0.9f, lintelBottom = 1.6f, pillarWidth = 0.8f, spec = new Spec
            { id = "window_wide", fileName = "WindowWide", displayName = "넓은 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Concrete, snapMode = BuildSnapMode.Vertex, costs = new[] { ("Item_BasicBuildingMaterial", 5) } } },
        // 창살 창: 개구부 0.9~1.6m를 쇠창살이 채운다(사격은 창살 틈으로 통과, 사람은 못 지나간다).
        new WindowSpec { wall = "Wall.Plain.2U.prefab", wallWidth = 2f, sill = 0.9f, lintelBottom = 1.6f, infill = Infill.Bars, spec = new Spec
            { id = "window_barred", fileName = "WindowBarred", displayName = "창살 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Metal, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3), ("Item_ScrapMetal", 4) } } },
        // 여닫이 창: 개구부 0.9~1.6m를 양쪽 덧창이 닫는다(E로 열고 닫는다).
        new WindowSpec { wall = "Wall.Plain.2U.prefab", wallWidth = 2f, sill = 0.9f, lintelBottom = 1.6f, infill = Infill.Shutter, spec = new Spec
            { id = "window_shutter", fileName = "WindowShutter", displayName = "여닫이 창", category = BuildableCategory.Window,
              surface = ImpactSurface.Wood, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3), ("Item_Wood", 3) } } },
    };

    private static GameObject BuildWindowPrefab(WindowSpec window)
    {
        GameObject root = new GameObject("Buildable_" + window.spec.fileName);
        GameObject wallAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Kats + "Walls_plain/" + window.wall);

        // 기둥을 뺀 가운데 폭에 아랫단과 윗단을 쌓는다.
        float middleWidth = window.wallWidth - 2f * window.pillarWidth;
        AddWindowBlock(root, wallAsset, window.wallWidth, "Sill", 0f, middleWidth, 0f, window.sill);
        AddWindowBlock(root, wallAsset, window.wallWidth, "Lintel", 0f, middleWidth, window.lintelBottom, WindowWallHeight - window.lintelBottom);
        if (window.pillarWidth > 0f)
        {
            float pillarCenter = (window.wallWidth - window.pillarWidth) * 0.5f;
            AddWindowBlock(root, wallAsset, window.wallWidth, "PillarLeft", -pillarCenter, window.pillarWidth, 0f, WindowWallHeight);
            AddWindowBlock(root, wallAsset, window.wallWidth, "PillarRight", pillarCenter, window.pillarWidth, 0f, WindowWallHeight);
        }

        if (window.infill == Infill.Bars) AddBars(root, window);
        else if (window.infill == Infill.Shutter) AddShutters(root, window);

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(window.spec.surface);
        if (window.infill == Infill.Shutter) AddDoorComponents(root);

        string path = PrefabDir + "/Buildable_" + window.spec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 벽 모델을 가로 width, 세로 height로 줄여 중심 x, 높이 bottom에 놓고 같은 크기의 박스 콜라이더를 붙인다.
    private static void AddWindowBlock(GameObject root, GameObject wallAsset, float wallWidth, string name, float centerX, float width, float bottom, float height)
    {
        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(wallAsset, root.transform);
        block.name = name;
        block.transform.localPosition = new Vector3(centerX, bottom, 0f);
        Vector3 scale = wallAsset.transform.localScale;
        block.transform.localScale = new Vector3(scale.x * width / wallWidth, scale.y * height / WindowWallHeight, scale.z);

        Bounds bounds = RendererBounds(block);
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = bounds.center - root.transform.position;
        box.size = bounds.size;
    }

    // 문틀은 단면 메시인 아치를 앞뒤로 맞대 양면이 보이게 하고, 문짝 둘을 경첩 피벗에 달아 열고 닫을 수 있게 한다.
    // 원본 아치의 꼭대기가 1.75m라 플레이어(캡슐 1.83m)가 머리를 부딪친다. 문틀 전체를 키워 통과할 수 있게 한다.
    // 폭은 벽 한 장(2m, 두께 0.7m)과 같게 두고 높이만 키운다. 개구부가 1.75m * 1.12 = 1.96m라 캡슐(1.83m)이 지나간다.
    private static readonly Vector3 DoorScale = new Vector3(1f, 1.12f, 1f);
    private static readonly Vector3 LeafScale = new Vector3(1.7f, 1.7f * 1.12f, 1.7f);
    private const float JambX = 0.9f;

    private static GameObject BuildDoorPrefab()
    {
        GameObject root = new GameObject("Buildable_" + DoorSpec.fileName);
        GameObject archAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Kats + "Walls_plain/Wall.Arch.prefab");

        GameObject front = (GameObject)PrefabUtility.InstantiatePrefab(archAsset, root.transform);
        front.name = "ArchFront";
        // 벤더 프리팹 루트에는 임의의 위치가 저장돼 있어서 원점으로 맞춘다.
        front.transform.localPosition = Vector3.zero;
        front.transform.localScale = Vector3.Scale(archAsset.transform.localScale, DoorScale);
        GameObject back = (GameObject)PrefabUtility.InstantiatePrefab(archAsset, root.transform);
        back.name = "ArchBack";
        back.transform.localPosition = Vector3.zero;
        back.transform.localScale = Vector3.Scale(archAsset.transform.localScale, DoorScale);
        back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        foreach (MeshFilter filter in front.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }

        Transform leftHinge = BuildLeaf(root.transform, "HingeL", Kats + "Doors/Prop.Door.L.prefab", -JambX, true);
        Transform rightHinge = BuildLeaf(root.transform, "HingeR", Kats + "Doors/Prop.Door.R.prefab", JambX, false);

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(DoorSpec.surface);
        BuildableDoor door = root.AddComponent<BuildableDoor>();
        SerializedObject so = new SerializedObject(door);
        SerializedProperty leaves = so.FindProperty("leaves");
        leaves.arraySize = 2;
        SetLeaf(leaves.GetArrayElementAtIndex(0), leftHinge, -90f);
        SetLeaf(leaves.GetArrayElementAtIndex(1), rightHinge, 90f);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 열 때는 나무 상자 뚜껑 소리를 낮춰 삐걱이는 소리로, 닫힐 때는 망치 소리를 낮춰 쿵 하는 소리로 쓴다.
        BuildableDoorAudio audio = root.AddComponent<BuildableDoorAudio>();
        SerializedObject audioSo = new SerializedObject(audio);
        SetSound(audioSo.FindProperty("openSound"), "Assets/Devion Games/Inventory System/Examples/Sounds/Open Chest.wav", 0.7f, 0.75f, 0.85f);
        SetSound(audioSo.FindProperty("closeSound"), "Assets/Devion Games/Third Person Controller/Example/Sounds/Hammering.mp3", 0.8f, 0.55f, 0.65f);
        audioSo.ApplyModifiedPropertiesWithoutUndo();

        // 같은 경로에 덮어써서 GUID를 유지한다.
        string path = PrefabDir + "/Buildable_" + DoorSpec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // ---- 창살/덧창 ----

    // 쇠창살 문짝 둘을 개구부 폭에 맞춰 세운다. 메시 콜라이더라 창살 틈으로 사격이 지나가고 사람은 막힌다.
    private static void AddBars(GameObject root, WindowSpec window)
    {
        float opening = window.lintelBottom - window.sill;
        float half = window.wallWidth * 0.5f;
        foreach (string vendor in new[] { "Prop.Gate.Iron.L.prefab", "Prop.Gate.Iron.R.prefab" })
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(Kats + "Doors/" + vendor);
            GameObject bars = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            bars.name = vendor.Replace(".prefab", "");
            bars.transform.localPosition = new Vector3(0f, window.sill, 0f);
            // 문짝 하나가 0.5m 폭, 1m 높이다. 가운데에서 양옆 half까지 뻗도록 늘린다.
            bars.transform.localScale = Vector3.Scale(asset.transform.localScale, new Vector3(half / 0.5f, opening, 1.5f));
            foreach (MeshFilter filter in bars.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }
    }

    // 양쪽 가장자리 경첩에 덧창 둘을 달고 문 컴포넌트로 열고 닫게 한다.
    private static void AddShutters(GameObject root, WindowSpec window)
    {
        float opening = window.lintelBottom - window.sill;
        float half = window.wallWidth * 0.5f;
        Vector3 scale = new Vector3(half / 0.5f, opening, 1.5f);
        Transform left = BuildLeaf(root.transform, "HingeL", Kats + "Doors/Prop.Door.L.prefab", -half, true, scale, window.sill);
        Transform right = BuildLeaf(root.transform, "HingeR", Kats + "Doors/Prop.Door.R.prefab", half, false, scale, window.sill);
        BuildableDoor door = root.AddComponent<BuildableDoor>();
        SerializedObject so = new SerializedObject(door);
        SerializedProperty leaves = so.FindProperty("leaves");
        leaves.arraySize = 2;
        SetLeaf(leaves.GetArrayElementAtIndex(0), left, -90f);
        SetLeaf(leaves.GetArrayElementAtIndex(1), right, 90f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 문과 같은 열고 닫는 소리를 단다.
    private static void AddDoorComponents(GameObject root)
    {
        BuildableDoorAudio audio = root.AddComponent<BuildableDoorAudio>();
        SerializedObject audioSo = new SerializedObject(audio);
        SetSound(audioSo.FindProperty("openSound"), "Assets/Devion Games/Inventory System/Examples/Sounds/Open Chest.wav", 0.7f, 0.75f, 0.85f);
        SetSound(audioSo.FindProperty("closeSound"), "Assets/Devion Games/Third Person Controller/Example/Sounds/Hammering.mp3", 0.8f, 0.55f, 0.65f);
        audioSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 문 종류 ----

    // 철문: 아치 + 쇠창살 양문. 나무 문: 아치 + 나무 외문짝.
    private struct DoorVariant
    {
        public Spec spec;
        public string frame;           // Kats + "Walls_plain/" 아래 문틀
        public string leafL, leafR;    // Kats + "Doors/" 아래 문짝(leafR가 비면 외문짝)
        public float singleWidth;      // 외문짝의 원래 폭
    }

    private static readonly DoorVariant[] DoorVariants =
    {
        new DoorVariant { frame = "Wall.Arch.prefab", leafL = "Prop.Gate.Iron.L.prefab", leafR = "Prop.Gate.Iron.R.prefab", spec = new Spec
            { id = "door_iron", fileName = "IronDoor", displayName = "철문", category = BuildableCategory.Door,
              surface = ImpactSurface.Metal, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 3), ("Item_ScrapMetal", 8) } } },
        new DoorVariant { frame = "Wall.Arch.prefab", leafL = "Prop.Door.Static.prefab", singleWidth = 1.23f, spec = new Spec
            { id = "door_wood", fileName = "WoodDoor", displayName = "나무 문", category = BuildableCategory.Door,
              surface = ImpactSurface.Wood, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_BasicBuildingMaterial", 1), ("Item_Wood", 4) } } },
    };

    private static GameObject BuildDoorVariantPrefab(DoorVariant variant)
    {
        GameObject root = new GameObject("Buildable_" + variant.spec.fileName);
        GameObject archAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Kats + "Walls_plain/" + variant.frame);
        for (int i = 0; i < 2; i++)
        {
            GameObject arch = (GameObject)PrefabUtility.InstantiatePrefab(archAsset, root.transform);
            arch.name = i == 0 ? "ArchFront" : "ArchBack";
            arch.transform.localPosition = Vector3.zero;
            arch.transform.localScale = Vector3.Scale(archAsset.transform.localScale, DoorScale);
            if (i == 1) arch.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (i == 0)
            {
                foreach (MeshFilter filter in arch.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null) continue;
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }
        }

        BuildableDoor door = root.AddComponent<BuildableDoor>();
        SerializedObject so = new SerializedObject(door);
        SerializedProperty leaves = so.FindProperty("leaves");
        if (string.IsNullOrEmpty(variant.leafR))
        {
            // 외문짝: 왼쪽 경첩에서 오른쪽 문틀까지 한 장이 닫는다.
            Vector3 scale = new Vector3(2f * JambX / variant.singleWidth, LeafScale.y, LeafScale.z);
            Transform hinge = BuildLeaf(root.transform, "Hinge", Kats + "Doors/" + variant.leafL, -JambX, true, scale, 0f);
            leaves.arraySize = 1;
            SetLeaf(leaves.GetArrayElementAtIndex(0), hinge, -90f);
        }
        else
        {
            Transform left = BuildLeaf(root.transform, "HingeL", Kats + "Doors/" + variant.leafL, -JambX, true);
            Transform right = BuildLeaf(root.transform, "HingeR", Kats + "Doors/" + variant.leafR, JambX, false);
            leaves.arraySize = 2;
            SetLeaf(leaves.GetArrayElementAtIndex(0), left, -90f);
            SetLeaf(leaves.GetArrayElementAtIndex(1), right, 90f);
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(variant.spec.surface);
        AddDoorComponents(root);

        string path = PrefabDir + "/Buildable_" + variant.spec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // ---- 지붕 변형 ----

    private const float RoofAngle = 30f;

    // 경사 지붕: 2x2 슬래브를 한쪽이 오르게 기울인다(수평 투영이 정확히 2x2라 이웃과 이어진다). 박공 지붕: 가운데 용마루에서 두 면이 맞댄다.
    private static GameObject BuildRoofPrefab(Spec spec, bool gable)
    {
        GameObject root = new GameObject("Buildable_" + spec.fileName);
        GameObject slab = AssetDatabase.LoadAssetAtPath<GameObject>(Kats + "Ground/Paving.Slabs [2x2].prefab");
        float tan = Mathf.Tan(RoofAngle * Mathf.Deg2Rad);

        if (!gable)
        {
            AddRoofPlane(root, slab, "Slope", new Vector3(0f, tan, 0f), -RoofAngle, 2f, 2f);
        }
        else
        {
            // 각 면의 수평 폭은 1m, 낮은 쪽 가장자리가 바깥, 높은 쪽이 용마루(z=0)다.
            AddRoofPlane(root, slab, "SlopeBack", new Vector3(0f, 0.5f * tan, -0.5f), -RoofAngle, 2f, 1f);
            AddRoofPlane(root, slab, "SlopeFront", new Vector3(0f, 0.5f * tan, 0.5f), RoofAngle, 2f, 1f);
        }

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(spec.surface);

        string path = PrefabDir + "/Buildable_" + spec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 폭 width, 수평 깊이 run인 기울어진 슬래브 한 면(앞뒤 양면 렌더링)과 얇은 박스 콜라이더를 center에 만든다.
    private static void AddRoofPlane(GameObject root, GameObject slabAsset, string name, Vector3 center, float tilt, float width, float run)
    {
        float length = run / Mathf.Cos(RoofAngle * Mathf.Deg2Rad);

        GameObject node = new GameObject(name);
        node.transform.SetParent(root.transform, false);
        node.transform.localPosition = center;
        node.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);

        // 슬래브는 가로세로 2m의 평면이라 폭 width, 길이 length로 늘려 쓴다.
        Vector3 scale = new Vector3(width / 2f, 1f, length / 2f);
        for (int i = 0; i < 2; i++)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(slabAsset, node.transform);
            model.name = i == 0 ? "Top" : "Underside";
            model.transform.localScale = Vector3.Scale(slabAsset.transform.localScale, scale);
            model.transform.localRotation = Quaternion.Euler(i == 0 ? 0f : 180f, 0f, 0f);
            model.transform.localPosition = Vector3.zero;
            Bounds bounds = RendererBounds(model);
            model.transform.position += node.transform.position - bounds.center;
        }

        GameObject colliderHolder = new GameObject(name + "Collider");
        colliderHolder.transform.SetParent(root.transform, false);
        colliderHolder.transform.localPosition = center;
        colliderHolder.transform.localRotation = node.transform.localRotation;
        BoxCollider box = colliderHolder.AddComponent<BoxCollider>();
        box.size = new Vector3(width, 0.1f, length);
        box.center = new Vector3(0f, -0.05f, 0f);
    }

    // ---- 방어 건축 ----

    private static Material VendorMaterial(string vendorPath)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(vendorPath);
        Renderer renderer = asset.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.sharedMaterial : null;
    }

    private static void AddCube(Transform parent, string name, Vector3 center, Vector3 size, Material material, bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = center;
        cube.transform.localScale = size;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(cube.GetComponent<Collider>());
    }

    private static readonly Spec BarricadeSpec = new Spec
    {
        id = "defense_barricade", fileName = "Barricade", displayName = "나무 바리케이드", category = BuildableCategory.Defense,
        surface = ImpactSurface.Wood, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_Wood", 5) },
    };

    private static readonly Spec FenceSpec = new Spec
    {
        id = "defense_fence", fileName = "IronFence", displayName = "철책 울타리", category = BuildableCategory.Defense,
        surface = ImpactSurface.Metal, snapMode = BuildSnapMode.Edge, costs = new[] { ("Item_ScrapMetal", 6) },
    };

    // 폭 2m, 높이 1.2m의 판자 방벽이다(말뚝 둘과 엇갈린 판자 셋). 통째로 막혀 사격도 막는다.
    private static GameObject BuildBarricadePrefab()
    {
        GameObject root = new GameObject("Buildable_" + BarricadeSpec.fileName);
        Material wood = VendorMaterial(Kats + "Doors/Prop.Door.Static.prefab");
        AddCube(root.transform, "PostL", new Vector3(-0.9f, 0.6f, 0f), new Vector3(0.18f, 1.2f, 0.18f), wood, false);
        AddCube(root.transform, "PostR", new Vector3(0.9f, 0.6f, 0f), new Vector3(0.18f, 1.2f, 0.18f), wood, false);
        AddCube(root.transform, "Plank1", new Vector3(0f, 0.25f, 0.06f), new Vector3(2f, 0.28f, 0.06f), wood, false);
        AddCube(root.transform, "Plank2", new Vector3(0f, 0.6f, -0.06f), new Vector3(2f, 0.28f, 0.06f), wood, false);
        AddCube(root.transform, "Plank3", new Vector3(0f, 0.95f, 0.06f), new Vector3(2f, 0.28f, 0.06f), wood, false);
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.6f, 0f);
        box.size = new Vector3(2f, 1.2f, 0.3f);

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(BarricadeSpec.surface);
        string path = PrefabDir + "/Buildable_" + BarricadeSpec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 폭 2m, 높이 1.8m의 철책이다. 창살 사이 틈(약 0.2m)으로 사격이 지나가고 사람은 못 지나간다.
    private static GameObject BuildFencePrefab()
    {
        GameObject root = new GameObject("Buildable_" + FenceSpec.fileName);
        Material iron = VendorMaterial(Kats + "Doors/Prop.Door.L.prefab");
        AddCube(root.transform, "PostL", new Vector3(-0.95f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), iron, true);
        AddCube(root.transform, "PostR", new Vector3(0.95f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), iron, true);
        const int barCount = 7;
        for (int i = 0; i < barCount; i++)
        {
            float x = -0.75f + i * (1.5f / (barCount - 1));
            AddCube(root.transform, "Bar" + i, new Vector3(x, 0.85f, 0f), new Vector3(0.04f, 1.7f, 0.04f), iron, true);
        }
        AddCube(root.transform, "RailLow", new Vector3(0f, 0.35f, 0f), new Vector3(1.9f, 0.06f, 0.06f), iron, true);
        AddCube(root.transform, "RailHigh", new Vector3(0f, 1.45f, 0f), new Vector3(1.9f, 0.06f, 0.06f), iron, true);

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(FenceSpec.surface);
        string path = PrefabDir + "/Buildable_" + FenceSpec.fileName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 분류별 기본 내구도와 맞는 소리를 정의에 채운다. 개별 조각은 id로 덮어쓴다(0이면 부서지지 않는다).
    private static float DefaultHealth(string id, BuildableCategory category)
    {
        switch (id)
        {
            case "wall_reinforced": return 600f;
            case "wall_long": return 500f;
            case "defense_gate": return 500f;
            case "defense_tower": return 800f;
            case "defense_ladder": return 0f;
            case "door_iron": return 500f;
            case "window_barred": return 300f;
            case "defense_barricade": return 250f;
            case "defense_fence": return 400f;
            case "roof_slope": return 200f;
            case "roof_gable": return 200f;
        }

        switch (category)
        {
            case BuildableCategory.Wall: return 300f;
            case BuildableCategory.Window: return 200f;
            case BuildableCategory.Door: return 150f;
            case BuildableCategory.Defense: return 400f;
            default: return 0f;
        }
    }

    private static void ApplyDurability(SerializedObject so)
    {
        string id = so.FindProperty("buildableId").stringValue;
        BuildableCategory category = (BuildableCategory)so.FindProperty("category").enumValueIndex;
        so.FindProperty("maxHealth").floatValue = DefaultHealth(id, category);
        SetSound(so.FindProperty("hitSound"), "Assets/Devion Games/Third Person Controller/Example/Sounds/Hammering.mp3", 0.7f, 0.45f, 0.55f);
        SetSound(so.FindProperty("breakSound"), "Assets/Devion Games/Inventory System/Examples/Sounds/Metal Drop.wav", 0.9f, 0.6f, 0.7f);
    }

    // 이미 만들어 둔 모든 건축 정의에 내구도와 소리를 다시 채운다(수치를 조정한 뒤 실행한다).
    [MenuItem("ZGV01/Building/Apply Durability To All")]
    public static void ApplyDurabilityToAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:BuildableDefinition", new[] { DefinitionDir }))
        {
            BuildableDefinition definition = AssetDatabase.LoadAssetAtPath<BuildableDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition == null) continue;
            SerializedObject so = new SerializedObject(definition);
            ApplyDurability(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
        AssetDatabase.SaveAssets();
    }

    private static void SetSound(SerializedProperty group, string clipPath, float volume, float pitchMin, float pitchMax)
    {
        SerializedProperty clips = group.FindPropertyRelative("clips");
        clips.arraySize = 1;
        clips.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        group.FindPropertyRelative("volume").floatValue = volume;
        group.FindPropertyRelative("pitchRange").vector2Value = new Vector2(pitchMin, pitchMax);
    }

    private static void SetLeaf(SerializedProperty leaf, Transform hinge, float openAngle)
    {
        leaf.FindPropertyRelative("hinge").objectReferenceValue = hinge;
        leaf.FindPropertyRelative("openAngle").floatValue = openAngle;
    }

    // hingeX 위치의 경첩에 문짝을 단다. outerIsMin이면 문짝의 왼쪽 끝(최소 x)이 경첩에 닿고 오른쪽으로 뻗는다.
    private static Transform BuildLeaf(Transform parent, string name, string vendorPath, float hingeX, bool outerIsMin)
        => BuildLeaf(parent, name, vendorPath, hingeX, outerIsMin, LeafScale, 0f);

    private static Transform BuildLeaf(Transform parent, string name, string vendorPath, float hingeX, bool outerIsMin, Vector3 scale, float hingeY)
    {
        GameObject hinge = new GameObject(name);
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = new Vector3(hingeX, hingeY, 0f);

        GameObject vendor = AssetDatabase.LoadAssetAtPath<GameObject>(vendorPath);
        GameObject leaf = (GameObject)PrefabUtility.InstantiatePrefab(vendor, hinge.transform);
        leaf.transform.localScale = Vector3.Scale(vendor.transform.localScale, scale);
        leaf.transform.localPosition = Vector3.zero;

        Bounds bounds = RendererBounds(leaf);
        float edge = outerIsMin ? bounds.min.x : bounds.max.x;
        leaf.transform.localPosition = new Vector3(hingeX - edge, 0f, 0f);

        Bounds placed = RendererBounds(leaf);
        GameObject collider = new GameObject("LeafCollider");
        collider.transform.SetParent(hinge.transform, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = placed.center - hinge.transform.position;
        box.size = placed.size;
        return hinge.transform;
    }

    // 벤더 프리팹을 중첩 인스턴스로 두고, 바닥 중앙이 원점에 오도록 옮긴 래퍼 프리팹을 만든다.
    private static GameObject BuildPrefab(Spec spec)
    {
        GameObject root = new GameObject("Buildable_" + spec.fileName);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(spec.vendorPath), root.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        Bounds bounds = RendererBounds(model);
        Vector3 offset = spec.keepOrigin ? new Vector3(0f, -bounds.min.y, 0f) : new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        offset.y += spec.raise;
        model.transform.localPosition = offset;
        Bounds local = new Bounds(bounds.center + offset, bounds.size);
        if (spec.doubleSided) AddFlippedCopy(root, model, spec.vendorPath);

        switch (spec.collider)
        {
            case ColliderMode.Box:
                BoxCollider box = root.AddComponent<BoxCollider>();
                // 두께가 없는 바닥 조각은 밟을 수 있게 표면 아래로 얇은 판을 만든다.
                if (local.size.y < 0.05f)
                {
                    box.size = new Vector3(local.size.x, 0.1f, local.size.z);
                    box.center = new Vector3(0f, -0.05f, 0f);
                }
                else
                {
                    box.size = local.size;
                    box.center = local.center;
                }
                break;
            case ColliderMode.Mesh:
                foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null) continue;
                    MeshCollider mesh = filter.gameObject.AddComponent<MeshCollider>();
                    mesh.sharedMesh = filter.sharedMesh;
                }
                break;
        }

        root.AddComponent<BuildableInstance>();
        root.AddComponent<ImpactSurfaceMarker>().Initialize(spec.surface);
        if (spec.ladder) AddLadder(root, model, local.size.y);
        if (spec.storageSlots > 0) AddStorage(root, spec.storageSlots);
        if (spec.respawnPoint) root.AddComponent<RespawnPoint>();
        if (spec.rainCollector) AddRainCollector(root);
        if (spec.lightRange > 0f) AddNightLight(root, local, spec);

        string path = PrefabDir + "/Buildable_" + spec.fileName + ".prefab";
        AssetDatabase.DeleteAsset(path);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 모델 꼭대기에 점광원을 두고 밤에 서서히 켜지는 NightLight를 붙인다(그림자 없음).
    private static void AddNightLight(GameObject root, Bounds local, Spec spec)
    {
        GameObject lightObject = new GameObject("Light");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(local.center.x, local.max.y - 0.05f, local.center.z);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = spec.lightRange;
        light.intensity = spec.lightIntensity;
        light.color = spec.lightColor.a > 0f ? spec.lightColor : new Color(1f, 0.72f, 0.42f);
        light.shadows = LightShadows.None;
        light.enabled = false;

        NightLight night = root.AddComponent<NightLight>();
        SerializedObject so = new SerializedObject(night);
        SerializedProperty lights = so.FindProperty("lights");
        lights.arraySize = 1;
        lights.GetArrayElementAtIndex(0).objectReferenceValue = light;
        so.FindProperty("flicker").floatValue = spec.lightFlicker;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 보관함 컴포넌트(자체 인벤토리 + 칸 수 + 파괴 시 드랍할 전리품 프리팹)를 붙인다.
    private static void AddStorage(GameObject root, int slots)
    {
        root.AddComponent<Inventory>();
        StorageContainer storage = root.AddComponent<StorageContainer>();
        SerializedObject so = new SerializedObject(storage);
        so.FindProperty("slotCount").intValue = slots;
        so.FindProperty("lootPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/World/LootPickup.prefab");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 빗물 집수통: 보관함 이름을 "집수통"으로 하고 오염된 물이 차오르는 컴포넌트를 붙인다.
    private static void AddRainCollector(GameObject root)
    {
        StorageContainer storage = root.GetComponent<StorageContainer>();
        SerializedObject storageSo = new SerializedObject(storage);
        storageSo.FindProperty("displayName").stringValue = "집수통";
        storageSo.ApplyModifiedPropertiesWithoutUndo();

        RainCollector collector = root.AddComponent<RainCollector>();
        SerializedObject so = new SerializedObject(collector);
        so.FindProperty("waterItem").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + "Item_Drink_DirtyWater.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 평면 조각의 앞뒤를 뒤집은 복사본을 같은 자리에 겹쳐 둔다(X축으로 180도 돌려 같은 평면의 반대 면이 보이게 한다).
    private static void AddFlippedCopy(GameObject root, GameObject model, string vendorPath)
    {
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(vendorPath), root.transform);
        copy.name = model.name + "_Underside";
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        Bounds flipped = RendererBounds(copy);
        Bounds original = RendererBounds(model);
        copy.transform.localPosition = new Vector3(original.center.x - flipped.center.x, original.center.y - flipped.center.y, original.center.z - flipped.center.z);
    }

    // 사다리 메시의 발치/꼭대기 띠(아래 0.4m, 위 0.5m)의 평균 Z로 기울기를 구해 오르기 경로 값으로 넣는다.
    private static void AddLadder(GameObject root, GameObject model, float height)
    {
        MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
        Vector3[] vertices = filter.sharedMesh.vertices;
        float baseSum = 0f, topSum = 0f;
        int baseCount = 0, topCount = 0;
        foreach (Vector3 vertex in vertices)
        {
            Vector3 local = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
            if (local.y < 0.4f) { baseSum += local.z; baseCount++; }
            else if (local.y > height - 0.5f) { topSum += local.z; topCount++; }
        }

        BuildableLadder ladder = root.AddComponent<BuildableLadder>();
        SerializedObject so = new SerializedObject(ladder);
        so.FindProperty("height").floatValue = height;
        so.FindProperty("baseLocalZ").floatValue = baseCount > 0 ? baseSum / baseCount : 0f;
        so.FindProperty("topLocalZ").floatValue = topCount > 0 ? topSum / topCount : 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
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

    // 정의 에셋은 GUID를 유지하려고 이미 있으면 필드만 갱신한다.
    private static BuildableDefinition BuildDefinition(Spec spec, GameObject prefab)
    {
        string path = DefinitionDir + "/Buildable_" + spec.fileName + ".asset";
        BuildableDefinition definition = AssetDatabase.LoadAssetAtPath<BuildableDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<BuildableDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        SerializedObject so = new SerializedObject(definition);
        so.FindProperty("buildableId").stringValue = spec.id;
        so.FindProperty("displayName").stringValue = spec.displayName;
        so.FindProperty("category").enumValueIndex = (int)spec.category;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("snapSize").floatValue = spec.snap;
        so.FindProperty("snapMode").enumValueIndex = (int)spec.snapMode;
        so.FindProperty("wallAttach").enumValueIndex = (int)spec.wallAttach;
        ApplyDurability(so);

        SerializedProperty costs = so.FindProperty("costs");
        costs.arraySize = spec.costs.Length;
        for (int i = 0; i < spec.costs.Length; i++)
        {
            SerializedProperty cost = costs.GetArrayElementAtIndex(i);
            cost.FindPropertyRelative("item").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDir + spec.costs[i].item + ".asset");
            cost.FindPropertyRelative("quantity").intValue = spec.costs[i].quantity;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    // 건축 DB와 플레이어(프리팹, 열려 있는 씬의 인스턴스) 건축 목록에 없는 정의를 덧붙인다.
    private static void Register(List<BuildableDefinition> definitions)
    {
        BuildableDatabase database = AssetDatabase.LoadAssetAtPath<BuildableDatabase>("Assets/_Project/Resources/BuildableDatabase.asset");
        AddMissing(new SerializedObject(database), "buildables", definitions);
        EditorUtility.SetDirty(database);

        const string playerPrefabPath = "Assets/Survivalist/Prefab/PlayerArmature.prefab";
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        AddMissing(new SerializedObject(playerPrefab.GetComponent<BuildingSystem>()), "buildables", definitions);
        if (playerPrefab.GetComponent<LadderClimber>() == null) playerPrefab.AddComponent<LadderClimber>();
        PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPrefabPath);
        PrefabUtility.UnloadPrefabContents(playerPrefab);

        GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
        if (scenePlayer != null && scenePlayer.GetComponent<BuildingSystem>() != null)
        {
            AddMissing(new SerializedObject(scenePlayer.GetComponent<BuildingSystem>()), "buildables", definitions);
            EditorUtility.SetDirty(scenePlayer);
        }
    }

    private static void AddMissing(SerializedObject so, string field, List<BuildableDefinition> definitions)
    {
        SerializedProperty list = so.FindProperty(field);
        foreach (BuildableDefinition definition in definitions)
        {
            bool exists = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == definition) { exists = true; break; }
            if (exists) continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
