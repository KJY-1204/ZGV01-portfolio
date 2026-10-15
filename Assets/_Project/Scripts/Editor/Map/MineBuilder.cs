// 채석장 북쪽 산자락에 광산을 짓는 에디터 도구다. 파낸 광장 앞에 바위 판 정면과 입구 아치를 세우고, 그 뒤 산 안쪽에 복도, 큰 홀, 좌우 갱도, 끝 방 두 개를 월드 UV 상자(암석/흙 재질)로 만든 뒤 외부 에셋(기둥, 바위, 나무 지지대, 레일, 광차)으로 꾸민다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MineBuilder
{
    private const string Loaf = "Assets/LoafbrrAssets/MInesAndCaveSet/prefabs/";
    private const float Wall = 0.9f;          // 벽 두께(방 바깥쪽으로)
    private const float Slab = 0.6f;          // 천장판 두께
    private const float GapHeight = 3.3f;     // 방 사이 통로 높이
    private const float TunnelWidth = 3.6f;

    private struct Room
    {
        public string name;
        public float u0, u1, v0, v1, height;
        // 그리는 벽(S: 입구 쪽 -V, N: 안쪽 +V, W: -U, E: +U)과 각 벽의 통로 구간이다.
        public bool south, north, west, east;
        public Vector2[] gapSouth, gapNorth, gapWest, gapEast;
    }

    private static Frame frame;
    private static Material rockMaterial, groundMaterial, woodMaterial;
    private static System.Random rng;

    // 포털 중심(입구 아래 바닥 높이 포함)을 기준으로 u(동), v(북, 안쪽)를 월드로 바꾼다.
    private struct Frame
    {
        public float x0, z0, floorY;
        public Vector3 P(float u, float v, float y) => new Vector3(x0 + u, y, z0 + v);
    }

    public static void Build(Terrain terrain, Transform parent, int seed)
    {
        rng = new System.Random(seed);
        System.IO.Directory.CreateDirectory("Assets/_Project/Data/Map/Meshes/");
        Vector2 portal = MapLayout.MinePortal;
        float floorY = terrain.SampleHeight(new Vector3(portal.x, 0f, portal.y - 12f)) + terrain.transform.position.y;
        frame = new Frame { x0 = portal.x, z0 = portal.y, floorY = floorY };

        LoadMaterials();
        Transform root = new GameObject("Mine").transform;
        root.SetParent(parent, false);

        Room[] rooms = Rooms();
        BuildFloor(root, rooms);
        foreach (Room room in rooms) BuildRoom(root, room);
        BuildFacade(root, terrain);
        BuildHallDressing(root);
        BuildTunnelSupports(root);
        BuildTracks(root);
        BuildRocks(root);
        BuildLights(root, seed);
        BuildCrates(root);
        BuildIndoorZone(root);
    }

    // ---- 평면 ----

    private static Room[] Rooms()
    {
        Vector2 doorUpper = new Vector2(-TunnelWidth * 0.5f, TunnelWidth * 0.5f);
        Vector2 sideGap = new Vector2(24f - TunnelWidth * 0.5f, 24f + TunnelWidth * 0.5f);
        return new[]
        {
            // 입구 복도(아치 뒤). 안쪽 벽은 큰 홀이 그린다.
            new Room { name = "Corridor", u0 = -1.3f, u1 = 1.3f, v0 = 0.4f, v1 = 14f, height = 3.4f, west = true, east = true },
            // 큰 홀: 입구 쪽 벽에 복도 통로, 양옆 벽에 갱도 통로.
            new Room { name = "Hall", u0 = -30f, u1 = 30f, v0 = 14f, v1 = 40f, height = 5.8f, south = true, north = true, west = true, east = true,
                gapSouth = new[] { new Vector2(-1.3f, 1.3f) }, gapWest = new[] { sideGap }, gapEast = new[] { sideGap } },
            // 서쪽/동쪽 갱도: 위아래 벽만 그린다(끝은 홀과 방이 그린다).
            new Room { name = "TunnelWest", u0 = -62f, u1 = -30f, v0 = 24f - TunnelWidth * 0.5f, v1 = 24f + TunnelWidth * 0.5f, height = 3.8f, south = true, north = true },
            new Room { name = "TunnelEast", u0 = 30f, u1 = 62f, v0 = 24f - TunnelWidth * 0.5f, v1 = 24f + TunnelWidth * 0.5f, height = 3.8f, south = true, north = true },
            // 끝 방: 갱도와 닿는 쪽 벽에 통로.
            new Room { name = "ChamberWest", u0 = -80f, u1 = -62f, v0 = 12f, v1 = 38f, height = 5.2f, south = true, north = true, west = true, east = true, gapEast = new[] { sideGap } },
            new Room { name = "ChamberEast", u0 = 62f, u1 = 80f, v0 = 12f, v1 = 38f, height = 5.2f, south = true, north = true, west = true, east = true, gapWest = new[] { sideGap } },
        };
    }

    private static void LoadMaterials()
    {
        rockMaterial = MaterialOf(Loaf + "Wall/Wall_Caves_A.prefab");
        groundMaterial = MaterialOf(Loaf + "Ground/Ground_Mines_A.prefab");
        woodMaterial = MaterialOf(Loaf + "Posts/Post_A.prefab");
    }

    private static Material MaterialOf(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Renderer renderer = prefab != null ? prefab.GetComponentInChildren<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;
        Debug.LogWarning("광산 재질을 찾지 못했다: " + prefabPath);
        return new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

    // ---- 바닥/방 ----

    // 바닥은 광산 전체를 한 장으로 깐다(벽 아래와 통로 문턱도 덮는다). 윗면이 광장 바닥과 같은 높이다.
    private static void BuildFloor(Transform root, Room[] rooms)
    {
        float u0 = float.MaxValue, u1 = float.MinValue, v1 = float.MinValue;
        foreach (Room r in rooms) { u0 = Mathf.Min(u0, r.u0); u1 = Mathf.Max(u1, r.u1); v1 = Mathf.Max(v1, r.v1); }
        Box(root, "MineFloor", u0 - Wall, u1 + Wall, 0f, v1 + Wall, frame.floorY - 0.8f, frame.floorY, groundMaterial, 4f);
        // 입구 아치 아래: 지형 구멍이 뚫린 절벽 칸을 덮는 바닥이다.
        Box(root, "MineThreshold", -3f, 3f, -2.4f, 0.2f, frame.floorY - 0.8f, frame.floorY, groundMaterial, 4f);
    }

    private static void BuildRoom(Transform root, Room r)
    {
        float top = frame.floorY + r.height + Slab;
        float bottom = frame.floorY - 0.8f;
        // 천장판: 방 안쪽 크기만큼(벽이 위까지 올라와 천장판 둘레를 덮는다).
        Box(root, r.name + "Ceiling", r.u0, r.u1, r.v0, r.v1, frame.floorY + r.height, top, rockMaterial, 3f);

        if (r.south) SideWall(root, r, false, r.v0 - Wall, r.v0, r.gapSouth, bottom, top);
        if (r.north) SideWall(root, r, false, r.v1, r.v1 + Wall, r.gapNorth, bottom, top);
        if (r.west) SideWall(root, r, true, r.u0 - Wall, r.u0, r.gapWest, bottom, top);
        if (r.east) SideWall(root, r, true, r.u1, r.u1 + Wall, r.gapEast, bottom, top);
    }

    // 벽 한 면을 통로 구간을 비우고 만든다. alongV가 참이면 벽이 V 방향으로 뻗는다(서/동 벽), 아니면 U 방향이다(남/북 벽).
    private static void SideWall(Transform root, Room r, bool alongV, float plane0, float plane1, Vector2[] gaps, float bottom, float top)
    {
        float start = alongV ? r.v0 - Wall : r.u0 - Wall;
        float end = alongV ? r.v1 + Wall : r.u1 + Wall;
        var list = new List<Vector2>(gaps ?? new Vector2[0]);
        list.Sort((a, b) => a.x.CompareTo(b.x));

        float cursor = start;
        foreach (Vector2 gap in list)
        {
            if (gap.x > cursor) WallPiece(root, r.name + "Wall", alongV, plane0, plane1, cursor, gap.x, bottom, top);
            // 통로 위 상인방.
            WallPiece(root, r.name + "Lintel", alongV, plane0, plane1, gap.x, gap.y, frame.floorY + GapHeight, top);
            cursor = gap.y;
        }
        if (end > cursor) WallPiece(root, r.name + "Wall", alongV, plane0, plane1, cursor, end, bottom, top);
    }

    private static void WallPiece(Transform root, string name, bool alongV, float plane0, float plane1, float a0, float a1, float y0, float y1)
    {
        if (alongV) Box(root, name, plane0, plane1, a0, a1, y0, y1, rockMaterial, 3f);
        else Box(root, name, a0, a1, plane0, plane1, y0, y1, rockMaterial, 3f);
    }

    // u/v 범위와 월드 높이 범위의 상자 하나(렌더러 + 콜라이더)를 만든다.
    private static GameObject Box(Transform parent, string name, float u0, float u1, float v0, float v1, float y0, float y1, Material material, float tile)
    {
        if (u1 - u0 < 0.01f || v1 - v0 < 0.01f || y1 - y0 < 0.01f) return null;
        Vector3 a = frame.P(u0, v0, y0), b = frame.P(u1, v1, y1);
        Vector3 size = new Vector3(b.x - a.x, b.y - a.y, b.z - a.z);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = (a + b) * 0.5f;
        go.AddComponent<MeshFilter>().sharedMesh = SchoolInteriorBuilder.BoxMesh(size, tile);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    // ---- 정면(산 절벽 앞 바위 판 + 입구 아치) ----

    private static void BuildFacade(Transform root, Terrain terrain)
    {
        Transform facade = new GameObject("Facade").transform;
        facade.SetParent(root, false);
        BuildFacadeMass(facade, terrain);
        GameObject[] normal =
        {
            Prefab("Wall/Wall_Caves_A"), Prefab("Wall/Wall_Caves_B"), Prefab("Wall/Wall_Caves_C"), Prefab("Wall/Wall_Caves_D"),
        };
        GameObject entrance = Prefab("Wall/Wall_Caves_Entrance_A");
        const float pitch = 4.7f;
        for (int i = -6; i <= 6; i++)
        {
            float u = i * pitch;
            // 아랫줄: 가운데는 입구 아치. 판 뒷면이 절벽(MinePortal.y)에 닿도록 z를 잡는다.
            GameObject lower = i == 0 ? entrance : normal[Mathf.Abs(i * 7 + 3) % normal.Length];
            Place(facade, lower, frame.P(u, -1.45f, frame.floorY), 0f, 1f);

            // 윗줄: 절벽 위 지면보다 살짝만 올라오게 높이를 맞춘다(절벽 높이가 위치마다 달라서).
            float ground = terrain.SampleHeight(new Vector3(frame.x0 + u, 0f, frame.z0 + 1.5f)) + terrain.transform.position.y;
            float upperY = Mathf.Max(frame.floorY + 2.4f, ground - 4.3f);
            Place(facade, normal[Mathf.Abs(i * 5 + 1) % normal.Length], frame.P(u, -1.55f, upperY), 0f, 1f);
        }
        // 바위 판은 충돌을 유지한다. 다만 입구 판의 콜라이더는 통로를 좁혀 막으므로 지우고 뒤의 바위 덩어리/상인방이 입구 둘레를 맡는다.
        foreach (Transform piece in facade)
            if (piece.name.StartsWith("Wall_Caves_Entrance"))
                foreach (Collider collider in piece.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
    }

    // 바위 판 뒤를 채우는 단단한 바위 덩어리다. 절벽 줄을 따라 입구 폭(통로)만 비우고 좌우로 뻗으며, 윗면은 산 지면 높이에 맞춘다. 판 사이 틈으로 하늘/지형 구멍이 보이거나 빠져나가지 못하게 한다.
    private static void BuildFacadeMass(Transform parent, Terrain terrain)
    {
        const float gap = TunnelWidth * 0.5f;
        const float piece = 4f;
        const int count = 12;
        float baseY = terrain.transform.position.y;
        float bottom = frame.floorY - 1f;

        float TopAt(float u0, float u1)
        {
            float top = float.MinValue;
            for (int i = 0; i <= 4; i++)
                for (float v = 0f; v <= 3.5f; v += 1.75f)
                {
                    float u = Mathf.Lerp(u0, u1, i / 4f);
                    top = Mathf.Max(top, terrain.SampleHeight(new Vector3(frame.x0 + u, 0f, frame.z0 + v)) + baseY);
                }
            return top + 0.3f;
        }

        float centerTop = TopAt(-gap, gap);
        foreach (float side in new[] { -1f, 1f })
            for (int k = 0; k < count; k++)
            {
                float a = side * (gap + piece * k), b = side * (gap + piece * (k + 1));
                float u0 = Mathf.Min(a, b), u1 = Mathf.Max(a, b);
                float top = TopAt(u0, u1);
                // 광장 옆으로 절벽이 낮아져 사라지는 곳은 막지 않는다.
                if (top < frame.floorY + 1.2f) break;
                Box(parent, "FacadeMass", u0, u1, -1f, 1.6f, bottom, top, rockMaterial, 3f);
            }
        // 통로 위 상인방(통로 아래는 비워 둔다).
        Box(parent, "FacadeLintel", -gap, gap, -1f, 1.6f, frame.floorY + GapHeight, centerTop, rockMaterial, 3f);
    }

    // ---- 꾸미기 ----

    // 큰 홀 안쪽 벽에 바위 판을 대고(앞면이 홀 안쪽을 향한다) 바닥에서 천장까지 닿는 바위 기둥을 세운다.
    private static void BuildHallDressing(Transform root)
    {
        Transform dressing = new GameObject("HallDressing").transform;
        dressing.SetParent(root, false);
        GameObject[] panels = { Prefab("Wall/Wall_Caves_A"), Prefab("Wall/Wall_Caves_B"), Prefab("Wall/Wall_Caves_C") };
        const float pitch = 4.7f;
        float uMin = -30f + pitch * 0.5f, uMax = 30f - pitch * 0.5f;
        // 안쪽(북) 벽: 앞면이 -Z(홀 쪽)를 향한다.
        for (float u = uMin; u <= uMax; u += pitch) Place(dressing, panels[rng.Next(panels.Length)], frame.P(u, 40f - 0.25f, frame.floorY), 0f, 1.12f);
        // 서쪽 벽(앞면이 +X): 갱도 통로 구간은 비운다.
        for (float v = 14f + pitch * 0.5f; v <= 40f - pitch * 0.5f; v += pitch)
        {
            if (Mathf.Abs(v - 24f) < 3.6f) continue;
            Place(dressing, panels[rng.Next(panels.Length)], frame.P(-30f + 0.25f, v, frame.floorY), -90f, 1.12f);
            Place(dressing, panels[rng.Next(panels.Length)], frame.P(30f - 0.25f, v, frame.floorY), 90f, 1.12f);
        }

        GameObject[] pillars = { Prefab("Wall/Cave_Pillar_A"), Prefab("Wall/Cave_Pillar_B"), Prefab("Wall/Cave_Pillar_C") };
        float[] us = { -20f, -10f, 10f, 20f };
        float[] vs = { 21f, 33f };
        foreach (float u in us)
            foreach (float v in vs)
                Place(dressing, pillars[rng.Next(pillars.Length)], frame.P(u + (float)(rng.NextDouble() - 0.5) * 2f, v + (float)(rng.NextDouble() - 0.5) * 2f, frame.floorY), rng.Next(0, 360), 1.1f);
    }

    // 갱도와 복도에 나무 기둥 한 쌍과 들보를 일정 간격으로 세운다(광산 지지대).
    private static void BuildTunnelSupports(Transform root)
    {
        Transform supports = new GameObject("Supports").transform;
        supports.SetParent(root, false);
        GameObject post = Prefab("Posts/Post_A");
        GameObject beam = Prefab("Posts/Beam_A");
        // 서/동 갱도(U 방향으로 뻗음): V 방향 들보와 양쪽 기둥.
        foreach (float side in new[] { -1f, 1f })
            for (float u = 34f; u < 62f; u += 6f)
            {
                float uu = side * u;
                float half = TunnelWidth * 0.5f - 0.2f;
                Place(supports, post, frame.P(uu, 24f - half, frame.floorY), 0f, 1f);
                Place(supports, post, frame.P(uu, 24f + half, frame.floorY), 0f, 1f);
                Place(supports, beam, frame.P(uu, 24f, frame.floorY + 3.7f), 90f, 1f);
            }
        // 입구 복도(V 방향으로 뻗음): U 방향 들보.
        for (float v = 3f; v < 14f; v += 5f)
        {
            Place(supports, post, frame.P(-1.0f, v, frame.floorY), 0f, 0.9f);
            Place(supports, post, frame.P(1.0f, v, frame.floorY), 0f, 0.9f);
            Place(supports, beam, frame.P(0f, v, frame.floorY + 3.3f), 0f, 0.6f);
        }
    }

    // 갱도와 홀 가운데로 레일을 깔고 끝에 광차를 둔다.
    private static void BuildTracks(Transform root)
    {
        Transform tracks = new GameObject("Tracks").transform;
        tracks.SetParent(root, false);
        GameObject rail = Prefab("Rails/Rail_A");
        GameObject[] carts = { Prefab("MineCart/Mine_Cart_AA"), Prefab("MineCart/Mine_Cart_BA"), Prefab("MineCart/Mine_Cart_CA") };
        // 복도와 홀을 지나 안쪽 벽까지(V 방향).
        for (float v = 1.5f; v < 38f; v += 4f) Place(tracks, rail, frame.P(0f, v, frame.floorY + 0.02f), 90f, 1f);
        // 서/동 갱도(U 방향).
        foreach (float side in new[] { -1f, 1f })
            for (float u = 32f; u < 62f; u += 4f)
                Place(tracks, rail, frame.P(side * u, 24f, frame.floorY + 0.02f), 0f, 1f);

        Place(tracks, carts[0], frame.P(0f, 36f, frame.floorY + 0.35f), 90f, 1.2f);
        Place(tracks, carts[1], frame.P(-58f, 24f, frame.floorY + 0.35f), 0f, 1.2f);
        Place(tracks, carts[2], frame.P(55f, 24f, frame.floorY + 0.35f), 0f, 1.2f);   // 기둥 쌍(u=52, 58) 사이에 둬 통로를 막지 않는다
        Place(tracks, carts[rng.Next(carts.Length)], frame.P(4f, 26f, frame.floorY + 0.35f), 40f, 1.2f);
    }

    private static void BuildRocks(Transform root)
    {
        Transform rocks = new GameObject("Rocks").transform;
        rocks.SetParent(root, false);
        GameObject[] pieces = { Prefab("Wall/Cave_Rocks_A"), Prefab("Wall/Cave_Rocks_B"), Prefab("Wall/Cave_Rocks_C"), Prefab("Wall/Cave_Rocks_D") };
        for (int i = 0; i < 26; i++)
        {
            // 홀과 끝 방 가장자리에 흩뿌린다(통로 가운데는 비운다).
            bool hall = i % 3 != 0;
            float u = hall ? Mathf.Lerp(-27f, 27f, (float)rng.NextDouble()) : (rng.Next(2) == 0 ? -1f : 1f) * Mathf.Lerp(64f, 78f, (float)rng.NextDouble());
            float v = hall ? Mathf.Lerp(15.5f, 38.5f, (float)rng.NextDouble()) : Mathf.Lerp(13f, 37f, (float)rng.NextDouble());
            if (hall && Mathf.Abs(u) < 3f) continue;
            if (hall && Mathf.Abs(v - 24f) < 3f && Mathf.Abs(u) > 25f) continue;
            Place(rocks, pieces[rng.Next(pieces.Length)], frame.P(u, v, frame.floorY), rng.Next(0, 360), Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble()));
        }
    }

    // 항상 켜진 따뜻한 조명을 일정 간격으로 단다(고장/깜빡임 포함).
    private static void BuildLights(Transform root, int seed)
    {
        Transform lights = new GameObject("Lights").transform;
        lights.SetParent(root, false);
        var spots = new List<Vector3>();
        for (float v = 4f; v < 14f; v += 5f) spots.Add(new Vector3(0f, v, 3.1f));
        foreach (float u in new[] { -16f, 0f, 16f })
            foreach (float v in new[] { 20f, 34f }) spots.Add(new Vector3(u, v, 5.2f));
        foreach (float side in new[] { -1f, 1f })
        {
            for (float u = 36f; u < 62f; u += 8f) spots.Add(new Vector3(side * u, 24f, 3.3f));
            foreach (float v in new[] { 18f, 32f }) spots.Add(new Vector3(side * 71f, v, 4.6f));
        }

        int index = 0;
        foreach (Vector3 spot in spots)
        {
            GameObject go = new GameObject("MineLight");
            go.transform.SetParent(lights, false);
            go.transform.position = frame.P(spot.x, spot.y, frame.floorY + spot.z);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 11f;
            light.intensity = 1.7f;
            light.color = new Color(1f, 0.74f, 0.45f);
            light.shadows = LightShadows.None;

            NightLight night = go.AddComponent<NightLight>();
            SerializedObject so = new SerializedObject(night);
            SerializedProperty list = so.FindProperty("lights");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = light;
            so.FindProperty("alwaysOn").boolValue = true;
            so.FindProperty("fault").enumValueIndex = (int)LampFaultPattern.Assign((float)rng.NextDouble(), 0.25f, 0.2f);
            so.FindProperty("faultSeed").intValue = seed * 97 + index++;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildCrates(Transform root)
    {
        GameObject crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/World/Resource_SupplyCrate.prefab");
        if (crate == null) return;
        Transform crates = new GameObject("Crates").transform;
        crates.SetParent(root, false);
        var spots = new List<Vector2>
        {
            new Vector2(-27f, 38f), new Vector2(27f, 38f), new Vector2(-27f, 16f), new Vector2(27f, 16f), new Vector2(0f, 39f),
            new Vector2(-78f, 14f), new Vector2(-78f, 36f), new Vector2(-66f, 36f), new Vector2(-70f, 14f),
            new Vector2(78f, 14f), new Vector2(78f, 36f), new Vector2(66f, 36f), new Vector2(70f, 14f),
        };
        foreach (Vector2 spot in spots)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(crate, crates);
            instance.transform.position = frame.P(spot.x, spot.y, frame.floorY);
            instance.transform.rotation = Quaternion.Euler(0f, rng.Next(0, 360), 0f);
        }
    }

    // 광산 안은 1인칭으로 본다(건물 안과 같다). 입구 뒤쪽부터 안쪽 끝까지, 바닥에서 가장 높은 천장 위까지다.
    private static void BuildIndoorZone(Transform root)
    {
        Vector3 a = frame.P(-MapLayout.MineHalfWidth + 2f, 0.6f, frame.floorY - 0.5f);
        Vector3 b = frame.P(MapLayout.MineHalfWidth - 2f, 41.5f, frame.floorY + 6.4f);
        GameObject go = new GameObject("IndoorZone");
        go.transform.SetParent(root, false);
        IndoorZone zone = go.AddComponent<IndoorZone>();
        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("center").vector3Value = (a + b) * 0.5f;
        so.FindProperty("size").vector3Value = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 도우미 ----

    private static GameObject Prefab(string relative) => AssetDatabase.LoadAssetAtPath<GameObject>(Loaf + relative + ".prefab");

    private static void Place(Transform parent, GameObject prefab, Vector3 position, float yaw, float scale)
    {
        if (prefab == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = instance.transform.localScale * scale;
    }
}
