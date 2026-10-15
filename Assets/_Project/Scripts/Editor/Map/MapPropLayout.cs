// 구역별 건물, 컨테이너, 소품, 바위와 채집 가능한 나무/자원 노드의 배치 규칙이다. 시드가 고정돼 있어서 같은 지형이면 같은 배치가 나온다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapPropLayout
{
    private const string R = "Assets/RPG_FPS_game_assets_industrial/";
    private const string T = "Assets/TirgamesAssets/SchoolScene/Models/Exterior/";
    private const string WorldPrefabs = "Assets/_Project/Prefabs/World/";

    private static readonly string[] BigHangars =
    {
        R + "Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v5/Hangar_v5_2.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v5/Hangar_v5_3.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v7/Hangar_v7.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v3/Hangar_v3.prefab",
    };
    private const string Hangar4 = R + "Buildings/Industrial/Hangars/Hangar_v4/Hangar_v4.prefab";
    private const string Shed = R + "Buildings/Industrial/Hangars/Hangar_v1/Hangar_v1_full.prefab";
    private static readonly string[] SmallBuildings =
    {
        R + "Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2_outbuilding.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v3/Hangar_v3_outbuilding_1.prefab",
        R + "Buildings/Industrial/Hangars/Hangar_v2/Hangar_v2_outbuilding2.prefab",
    };
    private static readonly string[] Containers =
    {
        R + "Containers/Cargo_container_v1/Cargo_container_v1_LD1close.prefab",
        R + "Containers/Cargo_container_v1/Cargo_container_v1_LD2close.prefab",
        R + "Containers/Cargo_container_v1/Cargo_container_v1_LD1open.prefab",
    };
    private const string OilTank = R + "Oil_tanks/Oil_tank_v1/Oil_tank_v1.prefab";
    private const string Pipe = R + "Other_props/Pipes/Industrial_pipes/Industrial_pipe_v1/Industrial_pipe_v1.prefab";
    private const string FenceConcrete = R + "Fences/Concrete_fences/Concrete_fence_v2/Concrete_fence_v2_S.prefab";
    private const string RoadBlock = R + "Fences/Road_blocks/Road_block_v1/Road_block_v1.prefab";
    private static readonly string[] Dumpsters =
    {
        R + "Dumpsters/Dumpsters_v1/Dumpsters_v1_garbadge.prefab",
        R + "Dumpsters/Dumpsters_v1/Dumpsters_v1_empty.prefab",
    };
    private static readonly string[] Barrels =
    {
        R + "Barrels/Barrel_v2/Barrel_v2_single.prefab",
        R + "Barrels/Barrel_v2/Barrel_v2_quadro.prefab",
        R + "Barrels/Barrel_v3/Barrel_v3_single.prefab",
    };
    private const string WoodenBox = R + "Boxes/Wooden_box_v1/Wooden_box_v1_LD1.prefab";
    private const string Generator = R + "Other_props/Generators/Generator_v1/Generator_v1.prefab";
    private const string ElectricBox = R + "Other_props/Electric_box/Electric_box_v1/Electric_box_v1.prefab";
    private const string Pallet = R + "Other_props/Palets/Palet_v1/Palet_v1_set.prefab";
    private const string SchoolBuilding = T + "Buildings/BuildingsBig/Prefabs/BuildingBig02_1.prefab";
    private const string OldSchoolBuilding = T + "Buildings/BuildingsBig/Prefabs/BuildingBig02_1Old.prefab";
    private const string Booth = T + "Buildings/BuildingsKPP/Prefabs/BuildingKPP2.prefab";
    private const string FenceMetal = T + "Fences/FencesMetal/Prefabs/FenceMetal01_1.prefab";
    private const string Bench = T + "Benches/Prefabs/Bench01_1.prefab";
    // 켜진 변형을 래핑한 가로등(밤에 서서히 켜짐)과 작은 가로등이다.
    private const string Lamp = LightingAssetBuilder.PostLampPath;
    private const string SmallLamp = LightingAssetBuilder.SmallLampPath;
    private const string Hedge = T + "Plants/Prefabs/Hedge01_02.prefab";
    private static readonly string[] Rocks = { "Assets/Mine/Prefabs/Rocks/Rock1.prefab", "Assets/Mine/Prefabs/Rocks/Rock2.prefab" };

    // 밀도 보강용 소품이다(구역마다 군집과 줄로 배치한다).
    private const string Op = R + "Other_props/";
    private static readonly string[] PalletStuff =
    {
        Op + "Palets/Bags_on_pallet_v1/Bags_on_pallet_v1_1.prefab", Op + "Palets/Bags_on_pallet_v1/Bags_on_pallet_v1_2.prefab",
        Op + "Palets/Palet_v1/Palet_v1_single.prefab", WoodenBox,
    };
    private static readonly string[] BarrelClusters =
    {
        R + "Barrels/Barrel_v1/Barrel_v1_LD1.prefab", R + "Barrels/Barrel_v1/Barrel_v1_LD2.prefab", R + "Barrels/Barrel_v2/Barrel_v2_quadro.prefab",
        R + "Barrels/Barrel_v3/Barrel_v3_quadro.prefab", R + "Barrels/Barrel_v2/Barrel_v2_single.prefab",
    };
    private const string OilTank2 = R + "Oil_tanks/Oil_tank_v2/Oil_tank_v2.prefab";
    private static readonly string[] Machines =
    {
        Generator, ElectricBox, Op + "Electric_box/Electric_box_v2/Electric_box_v2.prefab", Op + "Electric_box/Electric_box_v3/Electric_box_v3.prefab",
        Op + "Conditioners/Conditioner_v1/Conditioner_v1.prefab",
    };
    private static readonly string[] PipeRack = { Op + "Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_U_set_v1.prefab", Op + "Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_U_set_v2.prefab" };
    private static readonly string[] PipeTall = { Op + "Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_H_set_v1.prefab", Op + "Pipes/Pipe_sets/Pipes_set_v1/Pipes_set_v1_H_set_v2.prefab" };
    private const string ConcreteWall = R + "Fences/Concrete_fences/Concrete_fence_v1/Concrete_fence_v1_wall_set_v1.prefab";
    private const string ConcreteBlock = R + "Roads/Floor_elevation_sets/UNIFloor_elevation_set_v1/UNIConcrete_wall_v1_S_single.prefab";
    private static readonly string[] SchoolJunk = { T + "ConcreteBoxes/Prefabs/ConcreteBox01_4.prefab", T + "ConcreteBoxes/Prefabs/ConcreteBox01.prefab", T + "ConcreteBoxes/Prefabs/ConcreteBox01_2.prefab" };
    private const string SchoolConditioner = T + "Conditioners/Prefabs/Conditioner01Old.prefab";
    private const string GuardHouseOld = T + "Buildings/BuildingsKPP/Prefabs/BuildingKPP2Old.prefab";
    private static readonly string[] OldMetalFences =
    {
        T + "Fences/FencesMetal/Prefabs/FenceMetal01_4Old.prefab", T + "Fences/FencesMetal/Prefabs/FenceMetal01_6Old.prefab", T + "Fences/FencesMetal/Prefabs/FenceMetal01_7Old.prefab",
    };
    private static readonly string[] BenchVariants = { Bench, T + "Benches/Prefabs/Bench01_2.prefab", T + "Benches/Prefabs/Bench01_3.prefab" };
    private const string Mi = "Assets/Mine/Prefabs/";
    private static readonly string[] MiningCamp =
    {
        Mi + "Props/Crate.prefab", Mi + "Props/Planks.prefab", Mi + "Props/Support1.prefab", Mi + "Props/Ladder.prefab", Mi + "Props/Lantern.prefab",
    };
    private static readonly string[] Ores = { Mi + "Rocks/OreBlue.prefab", Mi + "Rocks/OreGreen.prefab", Mi + "Rocks/OreRed.prefab" };

    private static Vector2[] roadPoints;
    // 지형 나무의 월드 xz 위치다(연못이 나무 위에 놓이지 않게 거를 때 쓴다).
    private static Vector2[] terrainTreePoints = new Vector2[0];

    // 지형 위에 모든 소품과 자원 노드를 새로 배치한다. 이미 있는 MapProps/MapResources는 지우고 다시 만든다.
    public static HashSet<string> Build(Terrain terrain)
    {
        foreach (string name in new[] { "MapProps", "MapResources" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        roadPoints = MapLayout.SampleRoad(4f);
        TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
        terrainTreePoints = new Vector2[treeInstances.Length];
        for (int i = 0; i < treeInstances.Length; i++)
            terrainTreePoints[i] = new Vector2((treeInstances[i].position.x - 0.5f) * MapLayout.Size, (treeInstances[i].position.z - 0.5f) * MapLayout.Size);
        MapPropPlacer placer = new MapPropPlacer(terrain, MapLayout.Seed);
        Transform props = new GameObject("MapProps").transform;
        Transform resources = new GameObject("MapResources").transform;

        placer.Reserve(MapLayout.PlayerStart, 9f);
        // 광산 광장은 구역 소품(채석장이 광장과 겹친다)이 막지 않게 먼저 비운다. 광산은 소품이 다 놓인 뒤 지형 위에 짓는다.
        placer.Reserve(new Vector2(MapLayout.MinePortal.x, MapLayout.MinePortal.y - 18f), 38f);
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            Transform parent = new GameObject(zone.name).transform;
            parent.SetParent(props, false);
            switch (zone.kind)
            {
                case MapLayout.ZoneKind.Camp: BuildCamp(placer, zone, parent); break;
                case MapLayout.ZoneKind.Warehouse: BuildWarehouse(placer, zone, parent); break;
                case MapLayout.ZoneKind.Factory: BuildFactory(placer, zone, parent); break;
                case MapLayout.ZoneKind.School: BuildSchool(placer, zone, parent); break;
                case MapLayout.ZoneKind.Yard: BuildYard(placer, zone, parent); break;
                case MapLayout.ZoneKind.Quarry: BuildQuarry(placer, zone, parent); break;
            }
        }

        BuildRoadside(placer, props);
        BuildFieldScatter(placer, props);
        BuildResources(placer, resources);
        BuildLamps(placer, props);
        MineBuilder.Build(terrain, props, 303);
        // 구역 외벽/컨테이너에 오염과 이끼를 입힌다(건축/실내/광산은 제외).
        MapGrimeApplier.Apply(props);

        MarkStatic(props);
        return placer.UsedPrefabs;
    }

    // ---- 구역 ----

    private static void BuildCamp(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        p.Place(Shed, c + new Vector2(32f, 24f), 200f, parent);
        p.Place(Containers[0], c + new Vector2(-30f, 22f), 90f, parent);
        p.Place(Containers[1], c + new Vector2(-30f, 11f), 92f, parent);
        p.Place(Containers[2], c + new Vector2(36f, -22f), 5f, parent);
        ScatterProps(p, zone, parent, Barrels, 7, 0.5f);
        ScatterProps(p, zone, parent, new[] { WoodenBox }, 5, 0.5f);
        ScatterProps(p, zone, parent, new[] { Pallet }, 3, 0.5f);
        ScatterProps(p, zone, parent, new[] { SmallLamp }, 4, 0.5f);
        // 시작 야영지 둘레에 자재 더미를 조금 더해 버려진 작업장 느낌을 낸다(시작점 9m는 비워 둔다).
        ScatterClusters(p, zone, parent, PalletStuff, 3, 4, 3.5f, 0.4f, 8f);
        ScatterClusters(p, zone, parent, BarrelClusters, 2, 4, 2.5f, 0.4f, 8f);
        BesideRoad(p, zone, parent, new[] { RoadBlock, WoodenBox }, 4, 5f, 5f);
        Perimeter(p, zone, parent, FenceConcrete, 5.7f, 3f, 0.55f, 9f, 0.6f);
    }

    private static void BuildWarehouse(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        float[] xs = { -68f, 0f, 68f };
        float[] zs = { -42f, 44f };
        foreach (float z in zs)
            foreach (float x in xs)
            {
                string hangar = p.Pick(BigHangars);
                p.Place(hangar, c + new Vector2(x + p.Range(-4f, 4f), z + p.Range(-4f, 4f)), z < 0f ? 0f : 180f, parent, 1f, 0f, true, 2f);
            }
        ContainerRows(p, c + new Vector2(-70f, 0f), 6, 2, parent, 0.55f);
        ContainerRows(p, c + new Vector2(20f, 0f), 5, 2, parent, 0.5f);
        ScatterProps(p, zone, parent, new[] { OilTank }, 2, 0.5f, 8f);
        ScatterProps(p, zone, parent, Dumpsters, 5, 0.5f);
        ScatterProps(p, zone, parent, Barrels, 14, 0.5f);
        ScatterProps(p, zone, parent, new[] { Pallet, WoodenBox }, 10, 0.5f);
        ScatterProps(p, zone, parent, new[] { Generator, ElectricBox }, 4, 0.5f);
        BuildWarehouseYard(p, zone, parent);
        Perimeter(p, zone, parent, FenceConcrete, 5.7f, 4f, 0.25f, 8f, 0.7f);
    }

    // 창고 마당: 하역 구역 느낌으로 팔레트/자루 더미, 드럼통 더미, 흩어진 컨테이너, 콘크리트 벽 줄과 배관 구조물을 더한다.
    private static void BuildWarehouseYard(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        ScatterClusters(p, zone, parent, PalletStuff, 12, 5, 4.5f);
        ScatterClusters(p, zone, parent, BarrelClusters, 8, 6, 3f);
        ScatterClusters(p, zone, parent, Containers, 5, 2, 7f, 0.6f);
        ScatterClusters(p, zone, parent, Machines, 5, 3, 4f);
        ScatterClusters(p, zone, parent, Dumpsters, 4, 2, 4f);
        // 하역 차선을 나누는 콘크리트 벽 줄.
        Row(p, parent, c + new Vector2(-30f, -8f), c + new Vector2(30f, -8f), new[] { ConcreteWall }, 3f, 0.3f);
        Row(p, parent, c + new Vector2(-95f, 5f), c + new Vector2(-60f, 5f), new[] { ConcreteBlock }, 0.4f, 0.15f);
        // 격납고 사이 배관 구조물.
        ScatterProps(p, zone, parent, PipeTall, 3, 0.5f, 2f);
        ScatterProps(p, zone, parent, PipeRack, 3, 0.5f, 2f);
        BesideRoad(p, zone, parent, new[] { RoadBlock, RoadBlock, WoodenBox, Pallet }, 10, 5f, 6f);
    }

    private static void BuildFactory(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        float[] xs = { -85f, -30f, 30f, 85f };
        foreach (float x in xs)
        {
            p.Place(Hangar4, c + new Vector2(x, -52f), 90f, parent, 1f, 0f, true, 2f);
            p.Place(p.Pick(BigHangars), c + new Vector2(x, 56f), 180f, parent, 1f, 0f, true, 2f);
        }
        ScatterProps(p, zone, parent, new[] { OilTank }, 5, 0.5f, 10f);
        ScatterProps(p, zone, parent, new[] { Pipe }, 6, 0.5f, 6f);
        ContainerRows(p, c + new Vector2(-40f, 0f), 7, 1, parent, 0.5f);
        ScatterProps(p, zone, parent, Dumpsters, 4, 0.5f);
        ScatterProps(p, zone, parent, Barrels, 18, 0.5f);
        ScatterProps(p, zone, parent, new[] { Generator, ElectricBox }, 8, 0.5f);
        ScatterProps(p, zone, parent, SmallBuildings, 3, 0.5f, 6f);
        // 부속 건물은 원본이 벽/지붕을 한쪽 면만 그리고 일부는 충돌도 없다(다른 쪽에서 보면 뚫려 있고 들어갈 수 있다). 양면 재질과 충돌체를 더한다.
        foreach (Transform child in parent)
            if (child.name.StartsWith("Hangar_v2_outbuilding") || child.name.StartsWith("Hangar_v3_outbuilding")) MakeSolidBuilding(child.gameObject);
        BuildFactoryPlant(p, zone, parent);
        Perimeter(p, zone, parent, FenceConcrete, 5.7f, 4f, 0.12f, 8f, 0.8f);
    }

    // 공장 부지: 배관 줄(파이프 랙), 탱크 군집, 기계 군집과 자재 더미를 더해 설비가 이어진 느낌을 준다.
    private static void BuildFactoryPlant(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        // 건물 줄 사이를 가로지르는 파이프 랙(아치가 이어진 배관).
        Row(p, parent, c + new Vector2(-100f, 18f), c + new Vector2(-20f, 18f), PipeRack, 1.5f, 0.1f);
        Row(p, parent, c + new Vector2(30f, -20f), c + new Vector2(105f, -20f), PipeRack, 1.5f, 0.1f);
        ScatterProps(p, zone, parent, PipeTall, 5, 0.5f, 2f);
        // 탱크 군집(둘~넷씩 모여 있다).
        for (int i = 0; i < 4; i++)
            Cluster(p, c + new Vector2(p.Range(-90f, 90f), p.Range(-30f, 30f)), parent, new[] { OilTank, OilTank2 }, 3, 7f, 1.2f);
        ScatterClusters(p, zone, parent, Machines, 10, 3, 4.5f);
        ScatterClusters(p, zone, parent, PalletStuff, 10, 5, 4.5f);
        ScatterClusters(p, zone, parent, BarrelClusters, 8, 6, 3f);
        ScatterClusters(p, zone, parent, Containers, 4, 2, 7f, 0.6f);
        Row(p, parent, c + new Vector2(-110f, -5f), c + new Vector2(-70f, -5f), new[] { ConcreteWall }, 3f, 0.25f);
        BesideRoad(p, zone, parent, new[] { RoadBlock, RoadBlock, WoodenBox, Pallet }, 12, 5f, 6f);
    }

    private static void BuildSchool(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        GameObject schoolA = p.Place(SchoolBuilding, c + new Vector2(0f, 8f), 0f, parent, 1f, 0f, true, 3f);
        GameObject schoolB = p.Place(SchoolBuilding, c + new Vector2(-4f, -58f), 180f, parent, 1f, 0f, true, 3f);
        AddWallCollider(schoolA);
        AddWallCollider(schoolB);
        // 앞줄 양옆에 낡은 교사동 두 채(속은 지어 넣지 않은 통짜 건물)를 세워 학교 부지를 건물로 채운다. 다른 소품이 자리를 차지하기 전에 놓는다.
        foreach (Vector2 offset in new[] { new Vector2(-52f, -48f), new Vector2(48f, -48f) })
            AddWallCollider(p.Place(OldSchoolBuilding, c + offset, 180f, parent, 1f, 0f, true, 3f));
        p.Place(Booth, c + new Vector2(-30f, -35f), 90f, parent);
        p.Place(Booth, c + new Vector2(32f, -30f), 270f, parent);
        ScatterProps(p, zone, parent, new[] { Bench }, 8, 0.5f);
        ScatterProps(p, zone, parent, new[] { Lamp }, 10, 0.5f);
        BreakLamps(parent, 40, 0.4f, 0.25f);
        ScatterProps(p, zone, parent, new[] { Hedge }, 12, 0.5f);
        ScatterProps(p, zone, parent, Dumpsters, 3, 0.5f);
        ScatterProps(p, zone, parent, Barrels, 5, 0.5f);
        BuildSchoolGrounds(p, zone, parent);
        Perimeter(p, zone, parent, FenceMetal, 18.7f, 5f, 0.2f, 8f, 0f);
        ClearFencesAroundBuildings(parent, 8f);

        // 학교 건물 안(전 층, 계단, 교실, 조명, 가구, 보급 상자). 구역 가로등 고장 배정 뒤에 지어 실내 조명은 건드리지 않는다.
        if (schoolA != null) SchoolInteriorBuilder.Build(schoolA, parent, 101);
        if (schoolB != null) SchoolInteriorBuilder.Build(schoolB, parent, 202);
    }

    // 학교 부지: 두 건물 사이 마당은 가운데 길 양옆을 산울타리와 벤치 줄로 두르고(바닥은 잔디밭), 건물 뒤와 옆에는 운동장/화단 울타리, 경비실, 방치된 잡동사니를 둔다.
    private static void BuildSchoolGrounds(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        string[] hedge = { Hedge };

        // 마당: 길(중심 x-2) 양옆 잔디밭 가장자리를 산울타리가 두르고, 안쪽에 벤치 줄을 길 쪽으로 놓는다.
        foreach (float x in new[] { -17f, 13f })
            Row(p, parent, c + new Vector2(x, -44f), c + new Vector2(x, -6f), hedge, 0.2f, 0.12f);
        foreach (float x in new[] { -66f, 62f })
            Row(p, parent, c + new Vector2(x, -44f), c + new Vector2(x, -6f), hedge, 0.2f, 0.2f);
        foreach (float z in new[] { -44f, -6f })
        {
            Row(p, parent, c + new Vector2(-66f, z), c + new Vector2(-17f, z), hedge, 0.2f, 0.2f);
            Row(p, parent, c + new Vector2(13f, z), c + new Vector2(62f, z), hedge, 0.2f, 0.2f);
        }
        foreach (float x in new[] { -28f, 24f, -52f, 48f })
            Row(p, parent, c + new Vector2(x, -38f), c + new Vector2(x, -12f), BenchVariants, 6f, 0.25f);
        Cluster(p, c + new Vector2(-40f, -25f), parent, SchoolJunk, 7, 6f);
        Cluster(p, c + new Vector2(36f, -25f), parent, SchoolJunk, 7, 6f);

        // 건물 뒤 운동장: 낡은 금속 울타리로 두른 사각형(일부는 무너짐) 안에 벤치와 상자를 둔다.
        FencedLot(p, parent, c + new Vector2(30f, 28f), c + new Vector2(70f, 62f), 0.18f);
        Row(p, parent, c + new Vector2(38f, 45f), c + new Vector2(62f, 45f), BenchVariants, 6f, 0.2f);
        Cluster(p, c + new Vector2(50f, 38f), parent, SchoolJunk, 6, 5f);
        // 건물 서쪽 뒤뜰: 화단 울타리 구역.
        FencedLot(p, parent, c + new Vector2(-72f, 26f), c + new Vector2(-32f, 58f), 0.3f);
        Cluster(p, c + new Vector2(-52f, 42f), parent, new[] { Hedge, WoodenBox, Pallet }, 8, 9f);

        // 도로가 학교를 지나는 곳에 경비실과 방치된 바리케이드를 놓는다.
        BesideRoad(p, zone, parent, new[] { GuardHouseOld }, 2, 7f);
        BesideRoad(p, zone, parent, new[] { RoadBlock, RoadBlock, Pallet, WoodenBox }, 12, 5f, 6f);

        ScatterClusters(p, zone, parent, Dumpsters, 4, 2, 4f);
        ScatterClusters(p, zone, parent, BarrelClusters, 4, 5, 2.5f);
        ScatterClusters(p, zone, parent, PalletStuff, 5, 4, 3.5f);
        ScatterClusters(p, zone, parent, new[] { SchoolConditioner, ElectricBox }, 3, 3, 3f);
        p.Place(Shed, c + new Vector2(73f, 6f), 270f, parent);
    }

    // 교사동(원본과 낡은 것 모두) 둘레 clearance(m) 안에 걸리는 울타리를 치운다. 입구 계단 앞을 울타리가 막지 않게 한다(남쪽 건물은 계단이 앞 울타리에서 2m였다).
    private static void ClearFencesAroundBuildings(Transform parent, float clearance)
    {
        var buildings = new List<Bounds>();
        foreach (Transform child in parent)
        {
            if (!child.name.StartsWith("BuildingBig02_1")) continue;
            Bounds b = RendererBounds(child);
            b.Expand(new Vector3(clearance * 2f, 0f, clearance * 2f));
            buildings.Add(b);
        }

        var remove = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (!child.name.StartsWith("FenceMetal") && !child.name.StartsWith("FenceConcrete")) continue;
            Bounds fence = RendererBounds(child);
            foreach (Bounds b in buildings)
                if (b.Intersects(fence)) { remove.Add(child.gameObject); break; }
        }
        foreach (GameObject fence in remove) Object.DestroyImmediate(fence);
    }

    private static Bounds RendererBounds(Transform t)
    {
        Bounds bounds = new Bounds(t.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in t.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    // 낡은 금속 울타리로 사각형(from~to)을 두른다. gapChance만큼은 무너진 구간으로 비운다.
    private static void FencedLot(MapPropPlacer p, Transform parent, Vector2 from, Vector2 to, float gapChance)
    {
        Row(p, parent, from, new Vector2(to.x, from.y), OldMetalFences, 0f, gapChance);
        Row(p, parent, new Vector2(from.x, to.y), to, OldMetalFences, 0f, gapChance);
        Row(p, parent, from, new Vector2(from.x, to.y), OldMetalFences, 0f, gapChance);
        Row(p, parent, new Vector2(to.x, from.y), to, OldMetalFences, 0f, gapChance);
    }

    // 구역 안 도로 가장자리에 소품을 놓는다(경비실, 바리케이드). offset은 도로 가장자리에서의 거리다.
    private static void BesideRoad(MapPropPlacer p, MapLayout.Zone zone, Transform parent, IList<string> paths, int count, float offset, float jitter = 2f)
    {
        var inside = new List<int>();
        for (int i = 0; i < roadPoints.Length; i++)
            if (MapLayout.InZone(zone, roadPoints[i].x, roadPoints[i].y, -8f)) inside.Add(i);
        if (inside.Count == 0) return;

        int placed = 0;
        for (int attempt = 0; attempt < count * 10 && placed < count; attempt++)
        {
            int i = p.Pick(inside);
            Vector2 a = roadPoints[i], b = roadPoints[(i + 1) % roadPoints.Length];
            Vector2 forward = (b - a).normalized;
            Vector2 side = new Vector2(forward.y, -forward.x) * (p.Chance(0.5f) ? 1f : -1f);
            Vector2 xz = a + side * (MapLayout.RoadHalfWidth + offset + p.Range(0f, jitter));
            if (!MapLayout.InZone(zone, xz.x, xz.y, -6f)) continue;
            float yaw = Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg + (p.Chance(0.5f) ? 0f : 90f) + p.Range(-14f, 14f);
            if (p.Place(p.Pick(paths), xz, yaw, parent, 1f, 0f, true, 0.6f) != null) placed++;
        }
    }

    private const string DoubleSidedDir = "Assets/_Project/Materials/Map/DoubleSided/";

    // 한쪽 면만 그리는 건물을 양면으로 그리고(재질 복사본, 원본 재질은 그대로), 콜라이더가 없는 메시에는 메시 콜라이더를 붙인다.
    private static void MakeSolidBuilding(GameObject building)
    {
        System.IO.Directory.CreateDirectory(DoubleSidedDir);
        foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = DoubleSidedCopy(materials[i]);
            renderer.sharedMaterials = materials;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && renderer.GetComponent<Collider>() == null)
                renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }
    }

    private static Material DoubleSidedCopy(Material source)
    {
        if (source == null) return null;
        string path = DoubleSidedDir + source.name + "_2S.mat";
        Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (copy != null) return copy;

        copy = new Material(source);
        copy.SetFloat("_Cull", 0f);
        if (copy.HasProperty("_CullMode")) copy.SetFloat("_CullMode", 0f);
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    // 학교 건물 본체 메시에는 원본 콜라이더가 없어 통과된다. 본체 메시에 충돌체를 더한다(문/유리/조명 콜라이더는 원본 그대로).
    private static void AddWallCollider(GameObject building)
    {
        if (building == null) return;
        MeshFilter body = building.GetComponent<MeshFilter>();
        if (body == null || body.sharedMesh == null || building.GetComponent<MeshCollider>() != null) return;
        building.AddComponent<MeshCollider>().sharedMesh = body.sharedMesh;
    }

    private static void BuildYard(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        Vector2 c = zone.center;
        ContainerRows(p, c + new Vector2(-48f, -42f), 8, 3, parent, 0.8f);
        ContainerRows(p, c + new Vector2(-48f, 18f), 8, 2, parent, 0.8f);
        p.Place(Shed, c + new Vector2(55f, 45f), 180f, parent);
        ScatterProps(p, zone, parent, Dumpsters, 6, 0.5f);
        ScatterProps(p, zone, parent, Barrels, 10, 0.5f);
        ScatterProps(p, zone, parent, new[] { RoadBlock }, 8, 0.5f);
        // 폐차장처럼 컨테이너를 제멋대로 쌓아 둔 더미와 방벽, 자재 더미를 더한다.
        ScatterClusters(p, zone, parent, Containers, 7, 3, 8f, 0.6f);
        ScatterClusters(p, zone, parent, PalletStuff, 8, 5, 4f);
        ScatterClusters(p, zone, parent, BarrelClusters, 6, 6, 3f);
        ScatterClusters(p, zone, parent, Machines, 3, 3, 4f);
        Row(p, parent, c + new Vector2(30f, -50f), c + new Vector2(75f, -50f), new[] { ConcreteWall }, 3f, 0.2f);
        Row(p, parent, c + new Vector2(-70f, 52f), c + new Vector2(-20f, 52f), new[] { ConcreteBlock }, 0.4f, 0.15f);
        BesideRoad(p, zone, parent, new[] { RoadBlock, RoadBlock, WoodenBox, Pallet }, 10, 5f, 6f);
        Perimeter(p, zone, parent, FenceConcrete, 5.7f, 3f, 0.3f, 8f, 0.6f);
    }

    private static void BuildQuarry(MapPropPlacer p, MapLayout.Zone zone, Transform parent)
    {
        ScatterProps(p, zone, parent, Rocks, 38, 0.5f, 0f, 0.9f, 2.4f, 0.35f);
        ScatterProps(p, zone, parent, new[] { WoodenBox, Pallet }, 8, 0.5f);
        ScatterProps(p, zone, parent, Dumpsters, 2, 0.5f);
        ScatterProps(p, zone, parent, Barrels, 6, 0.5f);
        p.Place(Shed, zone.center + new Vector2(-50f, -25f), 90f, parent);
        // 채굴 캠프: 광석 더미 곁에 상자, 널빤지, 지지대, 사다리, 랜턴이 모여 있다.
        for (int i = 0; i < 5; i++)
        {
            Vector2 center = zone.center + new Vector2(p.Range(-65f, 65f), p.Range(-35f, 35f));
            if (NearRoad(center, MapLayout.RoadHalfWidth + 8f)) continue;
            Cluster(p, center, parent, Ores, 3, 4f, 0.6f, 0.9f, 1.4f);
            Cluster(p, center, parent, MiningCamp, 6, 5f, 0.3f);
        }
        ScatterClusters(p, zone, parent, PalletStuff, 3, 4, 4f);
    }

    // ---- 도우미 ----

    // 중심 둘레에 count개를 흩어 놓는다(군집). 도로 위와 겹침은 건너뛴다.
    private static int Cluster(MapPropPlacer p, Vector2 center, Transform parent, IList<string> paths, int count, float radius, float margin = 0.4f, float minScale = 1f, float maxScale = 1f)
    {
        int placed = 0;
        for (int attempt = 0; attempt < count * 4 && placed < count; attempt++)
        {
            float angle = p.Range(0f, Mathf.PI * 2f), distance = radius * Mathf.Sqrt(p.Range(0f, 1f));
            Vector2 xz = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            if (NearRoad(xz, MapLayout.RoadHalfWidth + 2.5f)) continue;
            if (p.Place(p.Pick(paths), xz, p.Range(0f, 360f), parent, p.Range(minScale, maxScale), 0f, true, margin) != null) placed++;
        }
        return placed;
    }

    // 구역 안 여러 곳(도로에서 떨어진 곳)에 군집을 만든다.
    private static void ScatterClusters(MapPropPlacer p, MapLayout.Zone zone, Transform parent, IList<string> paths, int clusters, int perCluster, float radius, float margin = 0.4f, float inset = 12f)
    {
        int made = 0;
        for (int attempt = 0; attempt < clusters * 20 && made < clusters; attempt++)
        {
            Vector2 center = new Vector2(
                zone.center.x + p.Range(-zone.half.x + inset, zone.half.x - inset),
                zone.center.y + p.Range(-zone.half.y + inset, zone.half.y - inset));
            if (NearRoad(center, MapLayout.RoadHalfWidth + radius + 4f)) continue;
            if (Cluster(p, center, parent, paths, perCluster, radius, margin) > 0) made++;
        }
    }

    // 두 점 사이에 줄로 놓는다(울타리, 산울타리, 벤치 줄). 긴 변이 줄 방향이 되게 돌리고, 조각 길이 + gap 간격으로 놓는다.
    private static void Row(MapPropPlacer p, Transform parent, Vector2 from, Vector2 to, IList<string> paths, float gap, float skipChance = 0f, float margin = 0.1f)
    {
        Vector2 direction = to - from;
        float length = direction.magnitude;
        if (length < 0.5f) return;
        direction /= length;
        float travelled = 0f;
        while (travelled < length)
        {
            string path = p.Pick(paths);
            Vector2 size = p.FootprintSize(path);
            bool longX = size.x > size.y;
            float piece = Mathf.Max(size.x, size.y);
            float yaw = longX ? Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg : Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            Vector2 xz = from + direction * (travelled + piece * 0.5f);
            if (!p.Chance(skipChance) && !NearRoad(xz, MapLayout.RoadHalfWidth + 2f)) p.Place(path, xz, yaw, parent, 1f, 0f, true, margin);
            travelled += piece + gap;
        }
    }

    // 구역 안 임의 위치에 count개를 놓는다(겹치면 다시 시도). minFootprint보다 작은 것도 최소 간격을 갖는다.
    private static void ScatterProps(MapPropPlacer p, MapLayout.Zone zone, Transform parent, IList<string> paths, int count, float fillMargin, float extraMargin = 0f, float minScale = 1f, float maxScale = 1f, float sink = 0f)
    {
        int placed = 0;
        for (int attempt = 0; attempt < count * 14 && placed < count; attempt++)
        {
            string path = p.Pick(paths);
            float scale = p.Range(minScale, maxScale);
            Vector2 size = p.FootprintSize(path, scale);
            float reach = Mathf.Max(size.x, size.y) * 0.5f + 6f;
            Vector2 xz = new Vector2(
                zone.center.x + p.Range(-zone.half.x + reach, zone.half.x - reach),
                zone.center.y + p.Range(-zone.half.y + reach, zone.half.y - reach));
            if (NearRoad(xz, MapLayout.RoadHalfWidth + 3f)) continue;
            float yaw = p.Range(0f, 360f);
            if (p.Place(path, xz, yaw, parent, scale, -sink * scale, true, fillMargin + extraMargin) != null) placed++;
        }
    }

    // 폐허 연출: 구역의 가로등 일부를 고장 낸다(시드 고정, 같은 맵이면 항상 같은 가로등이 고장 난다).
    private static void BreakLamps(Transform parent, int seed, float deadChance, float flickerChance)
    {
        System.Random rng = new System.Random(seed);
        int index = 0;
        foreach (NightLight night in parent.GetComponentsInChildren<NightLight>(true))
            SetLampFault(night, LampFaultPattern.Assign((float)rng.NextDouble(), deadChance, flickerChance), seed * 31 + index++);
    }

    private static void SetLampFault(NightLight night, LampFault fault, int faultSeed)
    {
        SerializedObject so = new SerializedObject(night);
        so.FindProperty("fault").enumValueIndex = (int)fault;
        so.FindProperty("faultSeed").intValue = faultSeed;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 가로등 ----

    // 구역과 도로에 가로등을 더한다. 다른 소품이 다 놓인 뒤 빈 곳에만 놓으므로 기존 배치는 그대로다(야영지와 학교의 가로등은 구역 단계에서 이미 놓였다).
    // 야영지는 모두 멀쩡하고, 구역은 구역마다 고장 비율이 다르며, 도로는 야영지에서 멀수록 고장이 늘어난다(어두운 곳이 위험한 곳이라는 길잡이가 된다).
    private static void BuildLamps(MapPropPlacer p, Transform root)
    {
        AddZoneLamps(p, root, "Warehouse", 6, 0.25f, 0.2f, 61);
        AddZoneLamps(p, root, "Factory", 8, 0.35f, 0.25f, 62);
        AddZoneLamps(p, root, "Yard", 4, 0.3f, 0.2f, 63);
        AddZoneLamps(p, root, "Quarry", 2, 0.45f, 0.25f, 64);
        BuildRoadLamps(p, root);
    }

    private static void AddZoneLamps(MapPropPlacer p, Transform root, string zoneName, int count, float deadChance, float flickerChance, int seed)
    {
        Transform zoneGroup = root.Find(zoneName);
        if (zoneGroup == null) return;
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            if (zone.name != zoneName) continue;
            Transform group = new GameObject("Lamps").transform;
            group.SetParent(zoneGroup, false);
            ScatterProps(p, zone, group, new[] { Lamp }, count, 0.5f);
            BreakLamps(group, seed, deadChance, flickerChance);
        }
    }

    // 도로 가장자리에 일정 간격으로 양쪽을 번갈아 가로등을 세운다. 도로를 따라 야영지에서 먼 정도로 고장 확률이 0에서 약 75%까지 커진다.
    private const float RoadLampSpacing = 55f;
    private const float RoadLampSafeDistance = 60f;
    private const float RoadLampFarDistance = 650f;
    private const float RoadLampMaxFaultChance = 0.75f;

    private static void BuildRoadLamps(MapPropPlacer p, Transform root)
    {
        Transform parent = new GameObject("RoadLamps").transform;
        parent.SetParent(root, false);

        int n = roadPoints.Length;
        float total = 0f;
        for (int i = 0; i < n; i++) total += Vector2.Distance(roadPoints[i], roadPoints[(i + 1) % n]);

        MapLayout.Zone camp = MapLayout.Zones[0];
        float travelled = 0f, next = RoadLampSpacing * 0.5f;
        float side = 1f;
        int index = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = roadPoints[i], b = roadPoints[(i + 1) % n];
            float segment = Vector2.Distance(a, b);
            if (travelled + segment >= next)
            {
                next += RoadLampSpacing;
                Vector2 forward = (b - a).normalized;
                Vector2 toSide = new Vector2(forward.y, -forward.x) * side;
                side = -side;
                Vector2 xz = a + toSide * (MapLayout.RoadHalfWidth + 2f);
                // 야영지 안은 야영지 가로등이 맡는다.
                if (!MapLayout.InZone(camp, xz.x, xz.y, 0f))
                {
                    float yaw = Mathf.Atan2(-toSide.x, -toSide.y) * Mathf.Rad2Deg;
                    GameObject lamp = p.Place(Lamp, xz, yaw, parent, 1f, 0f, true, 0.6f);
                    if (lamp != null)
                    {
                        // 도로를 따라 야영지(시작점)에서 가까운 쪽 거리다.
                        float distance = Mathf.Min(travelled, total - travelled);
                        float faultChance = RoadLampMaxFaultChance * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RoadLampSafeDistance, RoadLampFarDistance, distance));
                        LampFault fault = p.Chance(faultChance) ? (p.Chance(0.55f) ? LampFault.Dead : LampFault.Flicker) : LampFault.None;
                        SetLampFault(lamp.GetComponent<NightLight>(), fault, 7000 + index++);
                    }
                }
            }
            travelled += segment;
        }
    }

    // 컨테이너를 rows x perRow 격자로 놓는다. 일부는 2단으로 쌓는다.
    private static void ContainerRows(MapPropPlacer p, Vector2 origin, int perRow, int rows, Transform parent, float stackChance)
    {
        for (int r = 0; r < rows; r++)
            for (int i = 0; i < perRow; i++)
            {
                Vector2 xz = origin + new Vector2(i * 9.2f + p.Range(-0.4f, 0.4f), r * 6.4f + p.Range(-0.3f, 0.3f));
                if (NearRoad(xz, MapLayout.RoadHalfWidth + 6f)) continue;
                float yaw = p.Chance(0.1f) ? 180f : 0f;
                string path = p.Pick(Containers);
                GameObject first = p.Place(path, xz, yaw + p.Range(-2f, 2f), parent, 1f, 0f, true, 0.4f);
                if (first != null && p.Chance(stackChance))
                    p.Place(Containers[0], xz + new Vector2(p.Range(-0.3f, 0.3f), 0f), yaw + p.Range(-3f, 3f), parent, 1f, 3.0f, false, 0f, false);
            }
    }

    // 구역 둘레(안쪽으로 inset)를 따라 울타리 조각을 놓는다. 도로 가까이는 비워 출입구를 만들고, gapChance만큼은 무너진 구간으로 비운다.
    private static void Perimeter(MapPropPlacer p, MapLayout.Zone zone, Transform parent, string fence, float segment, float inset, float gapChance, float roadGap, float unused)
    {
        float hx = zone.half.x - inset, hz = zone.half.y - inset;
        for (float x = -hx + segment * 0.5f; x < hx; x += segment)
        {
            PlaceFence(p, zone.center + new Vector2(x, -hz), 90f, parent, fence, gapChance, roadGap);
            PlaceFence(p, zone.center + new Vector2(x, hz), 90f, parent, fence, gapChance, roadGap);
        }
        for (float z = -hz + segment * 0.5f; z < hz; z += segment)
        {
            PlaceFence(p, zone.center + new Vector2(-hx, z), 0f, parent, fence, gapChance, roadGap);
            PlaceFence(p, zone.center + new Vector2(hx, z), 0f, parent, fence, gapChance, roadGap);
        }
    }

    private static void PlaceFence(MapPropPlacer p, Vector2 xz, float yaw, Transform parent, string fence, float gapChance, float roadGap)
    {
        if (p.Chance(gapChance)) return;
        if (NearRoad(xz, MapLayout.RoadHalfWidth + roadGap)) return;
        p.Place(fence, xz, yaw, parent, 1f, 0f, false, 0f, false);
    }

    private static bool NearRoad(Vector2 xz, float distance)
    {
        float sqr = distance * distance;
        foreach (Vector2 r in roadPoints)
            if ((r - xz).sqrMagnitude < sqr) return true;
        return false;
    }

    private static bool InAnyZone(Vector2 xz, float margin)
    {
        foreach (MapLayout.Zone zone in MapLayout.Zones)
            if (MapLayout.InZone(zone, xz.x, xz.y, margin)) return true;
        return false;
    }

    // ---- 구역 밖 ----

    // 도로 가장자리에 도로 장애물과 버려진 컨테이너를 흩어 놓는다.
    private static void BuildRoadside(MapPropPlacer p, Transform root)
    {
        Transform parent = new GameObject("Roadside").transform;
        parent.SetParent(root, false);
        for (int i = 0; i < roadPoints.Length; i += 6)
        {
            if (!p.Chance(0.75f)) continue;
            Vector2 point = roadPoints[i];
            Vector2 next = roadPoints[(i + 1) % roadPoints.Length];
            Vector2 forward = (next - point).normalized;
            Vector2 side = new Vector2(forward.y, -forward.x) * (p.Chance(0.5f) ? 1f : -1f);
            Vector2 xz = point + side * (MapLayout.RoadHalfWidth + p.Range(1f, 3.5f));
            if (InAnyZone(xz, 10f)) continue;
            float yaw = Mathf.Atan2(forward.x, forward.y) * Mathf.Rad2Deg + 90f;
            if (p.Chance(0.7f)) p.Place(RoadBlock, xz, yaw + p.Range(-12f, 12f), parent);
            else p.Place(p.Pick(Containers), xz + side * 3f, yaw + p.Range(-20f, 20f), parent);
        }
    }

    // 들판에 바위와 버려진 컨테이너, 쓰레기통을 흩어 놓는다.
    private static void BuildFieldScatter(MapPropPlacer p, Transform root)
    {
        Transform parent = new GameObject("Field").transform;
        parent.SetParent(root, false);
        int rocks = 0, junk = 0;
        for (int attempt = 0; attempt < 3000 && (rocks < 130 || junk < 18); attempt++)
        {
            Vector2 xz = new Vector2(p.Range(-480f, 480f), p.Range(-480f, 480f));
            if (InAnyZone(xz, 20f) || NearRoad(xz, MapLayout.RoadHalfWidth + 6f)) continue;
            if (p.SlopeDegrees(xz) > 35f) continue;
            if (rocks < 130 && (junk >= 18 || p.Chance(0.88f)))
            {
                float scale = p.Range(0.5f, 1.5f);
                if (p.Place(p.Pick(Rocks), xz, p.Range(0f, 360f), parent, scale, -0.35f * scale, true, 1f) != null) rocks++;
            }
            else if (junk < 18)
            {
                string[] pool = { Containers[0], Containers[1], Dumpsters[0], Dumpsters[1], RoadBlock };
                if (p.Place(p.Pick(pool), xz, p.Range(0f, 360f), parent) != null) junk++;
            }
        }
    }

    // ---- 자원 노드 ----

    private static void BuildResources(MapPropPlacer p, Transform root)
    {
        // 구역 안 채집 나무를 담는 그룹이다.
        Transform trees = new GameObject("GatherTrees").transform;
        trees.SetParent(root, false);
        string[] treeIds = MapTreeAdapterBuilder.TreeIds;
        // 숲의 나무는 지형 나무이고 모두 TerrainTreeGather가 채집 가능하게 만든다. 여기서는 구역 안(잔디밭, 가장자리 띠) 채집 나무만 심는다.

        PlantZoneTrees(p, trees, treeIds);

        // 구역 안: 보급 상자, 물통. 숲 가장자리와 들판: 열매 덤불. 도로 근처 저지대: 더러운 물 웅덩이.
        Transform nodes = new GameObject("Nodes").transform;
        nodes.SetParent(root, false);
        Dictionary<MapLayout.ZoneKind, int> crates = new Dictionary<MapLayout.ZoneKind, int>
        {
            { MapLayout.ZoneKind.Camp, 8 }, { MapLayout.ZoneKind.Warehouse, 20 }, { MapLayout.ZoneKind.Factory, 26 },
            { MapLayout.ZoneKind.School, 12 }, { MapLayout.ZoneKind.Yard, 15 }, { MapLayout.ZoneKind.Quarry, 11 },
        };
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            ScatterNodes(p, zone, nodes, WorldPrefabs + "Resource_SupplyCrate.prefab", crates[zone.kind]);
            ScatterNodes(p, zone, nodes, WorldPrefabs + "Resource_WaterBarrel.prefab", zone.kind == MapLayout.ZoneKind.Camp ? 7 : 6);
            ScatterNodes(p, zone, nodes, ScrapNodeBuilder.PrefabPath, zone.kind == MapLayout.ZoneKind.Camp ? 4 : zone.kind == MapLayout.ZoneKind.School ? 5 : 10);
        }

        int bushes = 0, puddles = 0, ponds = 0;
        for (int attempt = 0; attempt < 24000 && (bushes < 260 || puddles < 14 || ponds < 8); attempt++)
        {
            Vector2 xz = new Vector2(p.Range(-480f, 480f), p.Range(-480f, 480f));
            if (InAnyZone(xz, 6f) && !MapLayout.InZone(MapLayout.Zones[0], xz.x, xz.y, 30f)) continue;
            if (NearRoad(xz, MapLayout.RoadHalfWidth + 2f) || p.SlopeDegrees(xz) > 25f) continue;
            float forest = MapTerrainBuilder.ForestDensity(xz.x, xz.y);
            if (bushes < 260 && forest > 0.12f && forest < 0.7f && p.Chance(0.5f))
            {
                if (p.Place(WorldPrefabs + "Resource_BerryBush.prefab", xz, p.Range(0f, 360f), nodes, 1f, 0f, true, 1.5f) != null) bushes++;
            }
            else if (ponds < 8 && p.Chance(0.05f) && IsLevel(p, xz, 4.8f, 0.15f) && !NearTerrainTree(xz, 5.2f))
            {
                // 연못은 평평한 곳에 크게 놓아 맵을 돌아다니다 만나는 물 보급처가 되게 한다.
                if (p.Place(WorldPrefabs + "Resource_Pond.prefab", xz, p.Range(0f, 360f), nodes, 1f, 0.17f, true, 3f) != null) ponds++;
            }
            else if (puddles < 14 && p.Chance(0.15f) && IsLevel(p, xz, 1.5f, 0.1f) && !NearTerrainTree(xz, 2.2f))
            {
                if (p.Place(WorldPrefabs + "Resource_DirtyPuddle.prefab", xz, p.Range(0f, 360f), nodes, 1f, 0f, true, 1.5f) != null) puddles++;
            }
        }
    }

    // 구역 안 풀이 번진 곳(학교 마당 잔디밭, 구역 가장자리 띠)에 채집 나무를 심어 넓은 콘크리트 판이 덜 휑하게 한다. 지형 나무는 구역 안에 심지 않는다.
    private static void PlantZoneTrees(MapPropPlacer p, Transform parent, string[] treeIds)
    {
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            int want = zone.kind == MapLayout.ZoneKind.School ? 20 : zone.kind == MapLayout.ZoneKind.Quarry ? 0 : zone.kind == MapLayout.ZoneKind.Camp ? 5 : 10;
            int placed = 0;
            for (int attempt = 0; attempt < want * 40 && placed < want; attempt++)
            {
                Vector2 xz = zone.center + new Vector2(p.Range(-zone.half.x + 6f, zone.half.x - 6f), p.Range(-zone.half.y + 6f, zone.half.y - 6f));
                Vector2 d = xz - zone.center;
                float inner = Mathf.Min(zone.half.x - Mathf.Abs(d.x), zone.half.y - Mathf.Abs(d.y));
                // 울타리(가장자리 4~5m)는 비키고 그 안쪽 띠에 심는다.
                bool edgeBand = inner > 9f && inner < 24f;
                bool lawn = zone.kind == MapLayout.ZoneKind.School && Mathf.Abs(d.x + 2f) > 18f && Mathf.Abs(d.x) < 62f && d.y > -42f && d.y < -8f;
                if (!edgeBand && !lawn) continue;
                if (NearRoad(xz, MapLayout.RoadHalfWidth + 7f) || p.SlopeDegrees(xz) > 30f) continue;
                string path = MapTreeAdapterBuilder.GatherPath(p.Pick(treeIds));
                if (p.Place(path, xz, p.Range(0f, 360f), parent, p.Range(0.8f, 1.1f), 0f, true, 3f) != null) placed++;
            }
        }
    }

    private static bool NearTerrainTree(Vector2 xz, float distance)
    {
        float sqr = distance * distance;
        foreach (Vector2 tree in terrainTreePoints)
            if ((tree - xz).sqrMagnitude < sqr) return true;
        return false;
    }

    // 중심과 둘레 여덟 점의 지면 높이 차가 maxDelta 이하인 평평한 곳인지다(연못이 비탈에 반쯤 묻히지 않게 한다).
    private static bool IsLevel(MapPropPlacer p, Vector2 center, float radius, float maxDelta)
    {
        float middle = p.GroundHeight(center);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (Mathf.Abs(p.GroundHeight(point) - middle) > maxDelta) return false;
        }
        return true;
    }

    private static void ScatterNodes(MapPropPlacer p, MapLayout.Zone zone, Transform parent, string path, int count)
    {
        int placed = 0;
        for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
        {
            Vector2 xz = new Vector2(
                zone.center.x + p.Range(-zone.half.x + 8f, zone.half.x - 8f),
                zone.center.y + p.Range(-zone.half.y + 8f, zone.half.y - 8f));
            if (NearRoad(xz, MapLayout.RoadHalfWidth + 2f)) continue;
            if (p.Place(path, xz, p.Range(0f, 360f), parent, 1f, 0f, true, 1.2f) != null) placed++;
        }
    }

    // 움직이지 않는 소품은 정적 배칭 대상으로 표시한다(자원 노드는 상태가 바뀌므로 제외).
    private static void MarkStatic(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            // 밤에 재질을 바꾸는 가로등은 정적 배칭에서 뺀다.
            if (t.GetComponentInParent<NightLight>() != null) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }
}
