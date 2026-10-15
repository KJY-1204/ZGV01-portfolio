// 학교 건물(속이 빈 껍데기 메시) 안에 전 층(4층)과 계단, 복도, 교실, 조명, 가구, 보급 상자를 지어 주는 에디터 도구다. 건물 월드 경계에서 틀을 구해 어느 방향으로 놓인 건물에도 같은 평면을 만든다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SchoolInteriorBuilder
{
    // ---- 건물 치수(원본 메시를 측정한 값) ----
    private const float HalfWidth = 13.35f;      // 건물 가로 반폭
    private const float Depth = 15.1f;           // 정면 평면에서 뒷면까지
    private const float FloorHeight = 2.9f;      // 층 높이
    private const float Clear = 2.6f;            // 층 안쪽 높이(천장 아래)
    private const float Slab = FloorHeight - Clear;
    private const int Floors = 4;
    private const float Lining = 0.3f;           // 바깥 벽 안쪽에 대는 벽 두께
    private const float LiningGap = 0.05f;       // 원본 면과 겹쳐 깜빡이지 않게 띄우는 거리

    // ---- 평면(정면 평면에서 안으로 잰 거리 pd, 가로 위치 px는 건물 중심 기준) ----
    private const float HallHalf = 4.15f;        // 중앙 홀 반폭(현관 포치 폭과 같다)
    private const float WallThickness = 0.2f;
    private const float RoomSplit = 7.45f;       // 앞쪽 교실과 뒤쪽 방을 가르는 벽의 pd
    private const float StairLane = 1.7f;        // 계단 한 줄의 폭
    private const float StairEndPd = Depth - Lining - LiningGap;       // 계단 뒤 끝(벽 안쪽)
    private const float StairLaneAMin = StairEndPd - StairLane;        // 뒤쪽 줄(오르기 시작 1)
    private const float StairLaneBMin = StairLaneAMin - StairLane;     // 앞쪽 줄
    private const float StairXMin = -3.9f, StairXMax = 0.9f;
    private const int StepsPerFlight = 16;

    // 정면 창(건물 1 기준 측정값)의 가로 범위(px)다. 건물은 좌우 대칭이다.
    private static readonly Vector2[] Windows =
    {
        new Vector2(-11.14f, -9.55f), new Vector2(-8.55f, -6.96f), new Vector2(-5.97f, -4.38f),
        new Vector2(4.38f, 5.97f), new Vector2(6.97f, 8.56f), new Vector2(9.56f, 11.15f),
    };
    private const float WindowSill = 0.9f;
    private static readonly Vector2 Entrance = new Vector2(-4.1f, 4.1f);

    private const string MeshDir = "Assets/_Project/Data/Map/Meshes/";
    private const string Pack = "Assets/TirgamesAssets/SchoolScene/";
    private const string Interior = Pack + "Models/Interior/";

    private struct Frame
    {
        public float centerX, frontZ, sign, baseY;

        // px: 건물 중심에서 건물의 오른쪽(정면에서 봤을 때 오른쪽)으로, pd: 정면 평면에서 안쪽으로, y: 월드 높이.
        public Vector3 P(float px, float pd, float y) => new Vector3(centerX + sign * px, y, frontZ - sign * pd);
        public float Level(int floor) => baseY + FloorHeight * floor;
    }

    private static Material wallMaterial, floorMaterial, ceilingMaterial, trimMaterial, glassMaterial, railMaterial;
    private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    private static System.Random rng;

    // building: 배치된 학교 건물 인스턴스, parent: 내부 조각을 담을 묶음이다.
    public static void Build(GameObject building, Transform parent, int seed)
    {
        MeshFilter filter = building.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        rng = new System.Random(seed);
        System.IO.Directory.CreateDirectory(MeshDir);
        LoadMaterials();
        meshCache.Clear();

        Frame frame = MeasureFrame(building, filter.sharedMesh);
        Transform root = new GameObject("Interior").transform;
        root.SetParent(parent, false);
        OpenEntrance(building, frame);

        BuildShell(frame, root);
        for (int floor = 0; floor < Floors; floor++) BuildFloor(frame, root, floor);
        BuildStairs(frame, root);
        BuildRailings(frame, root);
        BuildFurniture(frame, root);
        BuildIndoorZone(frame, root);
        BuildLights(frame, root, seed);
        BuildCrates(frame, root);
        EnsureIndoorView();
    }

    // 열려 있는 씬의 플레이어에 실내 1인칭 전환 컴포넌트를 붙인다(이미 있으면 그대로 둔다).
    private static void EnsureIndoorView()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || player.GetComponent<IndoorView>() != null) return;
        player.AddComponent<IndoorView>();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
    }

    // ---- 틀 측정 ----

    // 본체 메시(벽 서브메시)의 월드 경계에서 가로 중심과 정면 평면을 구한다. 건물이 180도 돌아 있으면 정면은 -Z쪽이다.
    private static Frame MeasureFrame(GameObject building, Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Bounds bounds = new Bounds(building.transform.TransformPoint(vertices[mesh.GetIndices(0)[0]]), Vector3.zero);
        foreach (int index in mesh.GetIndices(0)) bounds.Encapsulate(building.transform.TransformPoint(vertices[index]));

        // 문이 있는 쪽이 정면이다. 문 위치가 건물 중심보다 +Z면 정면은 +Z다.
        float sign = 1f;
        foreach (Transform child in building.transform)
            if (child.name.StartsWith("Door01_Closed")) { sign = child.position.z >= bounds.center.z ? 1f : -1f; break; }

        return new Frame
        {
            centerX = bounds.center.x,
            frontZ = sign > 0f ? bounds.max.z : bounds.min.z,
            sign = sign,
            // 현관 문턱 높이가 1층 바닥이다(건물 피벗 아래 3.3m).
            baseY = building.transform.position.y - 3.3f,
        };
    }

    // 현관의 닫힌 문을 꺼서 입구를 연다(폐허라 문이 열려 있다). 본체 메시의 포치 뒷벽(문 자리 판)도 모양과 충돌에서 걷어낸다.
    private static void OpenEntrance(GameObject building, Frame f)
    {
        foreach (Transform child in building.transform)
            if (child.name.StartsWith("Door01_Closed")) child.gameObject.SetActive(false);

        MeshFilter filter = building.GetComponent<MeshFilter>();
        Mesh opened = OpenedShellMesh(building, filter.sharedMesh, f);
        filter.sharedMesh = opened;
        MeshCollider collider = building.GetComponent<MeshCollider>();
        if (collider != null) collider.sharedMesh = opened;
    }

    // 입구 구멍 안에 중심이 있는 삼각형을 뺀 본체 메시 사본이다(원본 에셋은 건드리지 않는다). 같은 건물이면 같은 결과라 한 번만 만든다.
    private static Mesh OpenedShellMesh(GameObject building, Mesh source, Frame f)
    {
        string path = MeshDir + "SchoolShell_Open.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null && existing.subMeshCount == source.subMeshCount) return existing;

        Vector3[] vertices = source.vertices;
        Mesh copy = Object.Instantiate(source);
        copy.name = "SchoolShell_Open";
        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] indices = source.GetIndices(sub);
            var kept = new List<int>(indices.Length);
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 centroid = (building.transform.TransformPoint(vertices[indices[i]]) + building.transform.TransformPoint(vertices[indices[i + 1]]) + building.transform.TransformPoint(vertices[indices[i + 2]])) / 3f;
                float px = (centroid.x - f.centerX) * f.sign;
                float pd = (f.frontZ - centroid.z) * f.sign;
                bool inDoorway = Mathf.Abs(px) <= Entrance.y + 0.1f && pd >= -0.6f && pd <= 0.7f && centroid.y >= f.baseY - 0.1f && centroid.y <= f.baseY + 2.8f;
                if (!inDoorway) { kept.Add(indices[i]); kept.Add(indices[i + 1]); kept.Add(indices[i + 2]); }
            }
            copy.SetTriangles(kept, sub);
        }
        copy.RecalculateBounds();
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    private static void LoadMaterials()
    {
        wallMaterial = LoadMaterial("Materials/Surfaces/Concrete/ConcreteStucco01a.mat");
        floorMaterial = LoadMaterial("Materials/Surfaces/Tile/TileFloor05Old.mat");
        ceilingMaterial = LoadMaterial("Materials/Surfaces/Tile/TileArmstrong01_04Old.mat");
        trimMaterial = LoadMaterial("Materials/Surfaces/Concrete/ConcreteClear07.mat");
        glassMaterial = LoadMaterial("Materials/Surfaces/Glass/GlassClear.mat");
        railMaterial = LoadMaterial("Materials/Surfaces/Metall/MetallRusted02Gray.mat");
    }

    private static Material LoadMaterial(string relative)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Pack + relative);
        if (material == null)
        {
            Debug.LogWarning("학교 내부 재질을 찾지 못했다: " + relative);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        return material;
    }

    // ---- 바깥 껍데기 안쪽 벽(정면 창/입구는 구멍으로 둔다) ----

    private static void BuildShell(Frame f, Transform root)
    {
        float y0 = f.Level(0), y1 = f.Level(Floors - 1) + Clear;

        // 좌/우/뒤 벽은 건물 전체 높이를 한 장으로 댄다.
        Wall(f, root, "LiningLeft", -HalfWidth + LiningGap, -HalfWidth + LiningGap + Lining, LiningGap, Depth - LiningGap, y0, y1, wallMaterial);
        Wall(f, root, "LiningRight", HalfWidth - LiningGap - Lining, HalfWidth - LiningGap, LiningGap, Depth - LiningGap, y0, y1, wallMaterial);
        Wall(f, root, "LiningBack", -HalfWidth + LiningGap, HalfWidth - LiningGap, Depth - LiningGap - Lining, Depth - LiningGap, y0, y1, wallMaterial);

        // 정면 벽은 층마다 창과 입구 구멍을 뚫는다.
        for (int floor = 0; floor < Floors; floor++)
        {
            float level = f.Level(floor);
            var gaps = new List<Vector2>(Windows);
            if (floor == 0) gaps.Add(Entrance);
            gaps.Sort((a, b) => a.x.CompareTo(b.x));

            float cursor = -HalfWidth + LiningGap;
            foreach (Vector2 gap in gaps)
            {
                if (gap.x > cursor) Wall(f, root, "FrontPier", cursor, gap.x, LiningGap, LiningGap + Lining, level, level + Clear, wallMaterial);
                // 창 아래 낮은 벽(입구는 비워 둔다).
                if (!(floor == 0 && gap == Entrance))
                {
                    Wall(f, root, "FrontSill", gap.x, gap.y, LiningGap, LiningGap + Lining, level, level + WindowSill, wallMaterial);
                    // 창 유리: 원본 창판(정면 평면 0.2m 안쪽)은 안에서 보이지 않으므로 그 안쪽에 얇은 유리를 댄다.
                    Wall(f, root, "WindowGlass", gap.x, gap.y, 0.26f, 0.30f, level + WindowSill, level + Clear, glassMaterial, 1f);
                }
                cursor = gap.y;
            }
            Wall(f, root, "FrontPier", cursor, HalfWidth - LiningGap, LiningGap, LiningGap + Lining, level, level + Clear, wallMaterial);
        }
    }

    // ---- 층: 바닥판(계단 구멍 포함), 중앙 홀 벽, 교실 칸막이 ----

    private static void BuildFloor(Frame f, Transform root, int floor)
    {
        float level = f.Level(floor);
        BuildSlab(f, root, floor, level);

        // 중앙 홀 벽(양쪽): 앞 교실 문과 뒤 방 문 구멍을 낸다.
        foreach (float side in new[] { -1f, 1f })
        {
            float px0 = side * HallHalf - WallThickness * 0.5f, px1 = side * HallHalf + WallThickness * 0.5f;
            if (px0 > px1) { float t = px0; px0 = px1; px1 = t; }
            WallWithDoors(f, root, "HallWall", px0, px1, level, new[] { new Vector2(2.95f, 4.25f), new Vector2(8.9f, 10.2f) });
        }

        // 날개 칸막이: 앞 교실과 뒤 방 사이.
        foreach (float side in new[] { -1f, 1f })
        {
            float a = side * (HallHalf + WallThickness * 0.5f), b = side * (HalfWidth - Lining - LiningGap);
            Wall(f, root, "Partition", Mathf.Min(a, b), Mathf.Max(a, b), RoomSplit - WallThickness * 0.5f, RoomSplit + WallThickness * 0.5f, level, level + Clear, wallMaterial);
        }
    }

    private static void WallWithDoors(Frame f, Transform root, string name, float px0, float px1, float level, Vector2[] doors)
    {
        float start = LiningGap + Lining;
        float end = Depth - LiningGap - Lining;
        float cursor = start;
        foreach (Vector2 door in doors)
        {
            if (door.x > cursor) Wall(f, root, name, px0, px1, cursor, door.x, level, level + Clear, wallMaterial);
            // 문 위 상인방(문 높이 2.2m).
            Wall(f, root, name + "Header", px0, px1, door.x, door.y, level + 2.2f, level + Clear, wallMaterial);
            cursor = door.y;
        }
        if (end > cursor) Wall(f, root, name, px0, px1, cursor, end, level, level + Clear, wallMaterial);
    }

    // 바닥판: 아래층 천장이 된다. 층 k의 바닥판은 윗면이 level(k)이다. 계단 구멍(층 1~3)은 비운다.
    private static void BuildSlab(Frame f, Transform root, int floor, float level)
    {
        float pxMin = -HalfWidth + LiningGap, pxMax = HalfWidth - LiningGap;
        float pdMin = LiningGap, pdMax = Depth - LiningGap;
        float bottom = level - Slab;

        Vector4? hole = null;   // x: px0, y: px1, z: pd0, w: pd1
        if (floor == 1) hole = new Vector4(StairXMin - 0.05f, StairXMax + 0.05f, StairLaneAMin, StairEndPd);
        else if (floor == 2) hole = new Vector4(StairXMin - 0.05f, StairXMax + 0.05f, StairLaneBMin, StairLaneAMin);
        else if (floor == 3) hole = new Vector4(StairXMin - 0.05f, StairXMax + 0.05f, StairLaneAMin, StairEndPd);

        if (!hole.HasValue)
        {
            SlabPiece(f, root, "Slab", pxMin, pxMax, pdMin, pdMax, bottom, level);
            return;
        }

        Vector4 h = hole.Value;
        // 구멍 둘레 네 조각(왼쪽, 오른쪽, 앞, 뒤)이다.
        SlabPiece(f, root, "SlabL", pxMin, h.x, pdMin, pdMax, bottom, level);
        SlabPiece(f, root, "SlabR", h.y, pxMax, pdMin, pdMax, bottom, level);
        SlabPiece(f, root, "SlabF", h.x, h.y, pdMin, h.z, bottom, level);
        if (pdMax > h.w) SlabPiece(f, root, "SlabB", h.x, h.y, h.w, pdMax, bottom, level);

        // 맨 위층 위에는 천장판을 한 번 더 덮는다(다음 호출에서 floor == Floors일 때 처리).
    }

    // 바닥판 한 조각: 몸체(아래층 천장 재질)와 윗면에 덮는 얇은 바닥 재질 층(충돌 없음)으로 나눠 위아래 무늬를 다르게 한다.
    private static void SlabPiece(Frame f, Transform root, string name, float px0, float px1, float pd0, float pd1, float bottom, float level)
    {
        const float cover = 0.03f;
        Wall(f, root, name, px0, px1, pd0, pd1, bottom, level - cover, ceilingMaterial, 2f);
        Wall(f, root, name + "Top", px0, px1, pd0, pd1, level - cover, level, floorMaterial, 2f, false);
    }

    // ---- 계단: 1층→2층(뒤쪽 줄, +px로 오름), 2→3층(앞쪽 줄, -px로 오름), 3→4층(뒤쪽 줄) ----

    private static void BuildStairs(Frame f, Transform root)
    {
        // 맨 위층 천장판(구멍 없음).
        float ceilingLevel = f.Level(Floors);
        SlabPiece(f, root, "CeilingSlab", -HalfWidth + LiningGap, HalfWidth - LiningGap, LiningGap, Depth - LiningGap, ceilingLevel - Slab, ceilingLevel);

        BuildFlight(f, root, 0, StairLaneAMin, StairEndPd, true);
        BuildFlight(f, root, 1, StairLaneBMin, StairLaneAMin, false);
        BuildFlight(f, root, 2, StairLaneAMin, StairEndPd, true);
    }

    private static void BuildFlight(Frame f, Transform root, int fromFloor, float pd0, float pd1, bool climbsTowardPlus)
    {
        float baseY = f.Level(fromFloor);
        float rise = FloorHeight / StepsPerFlight;
        float run = (StairXMax - StairXMin) / StepsPerFlight;
        for (int i = 0; i < StepsPerFlight; i++)
        {
            float x0 = climbsTowardPlus ? StairXMin + run * i : StairXMax - run * (i + 1);
            float top = baseY + rise * (i + 1);
            Wall(f, root, "Step", x0, x0 + run, pd0, pd1, baseY, top, trimMaterial, 1f);
        }
    }

    // ---- 난간(가는 손잡이 봉 + 촘촘한 세로 살대) ----

    private const float RailHeight = 0.95f;
    private const float HandrailSize = 0.05f;
    private const float BalusterSize = 0.035f;
    // 살대 간격: 플레이어 캡슐 지름(0.56m)보다 좁아 빠져나가지 못한다.
    private const float BalusterSpacing = 0.3f;

    private static void BuildRailings(Frame f, Transform root)
    {
        // 계단 옆면의 기울어진 난간. 처음 몇 단은 옆에서 올라설 수 있게 비워 둔다.
        SlopedRail(f, root, StairLaneAMin + 0.04f, f.Level(0), true, 4, StepsPerFlight - 1);          // 1->2층 앞쪽 옆면
        SlopedRail(f, root, StairLaneBMin + 0.04f, f.Level(1), false, 0, StepsPerFlight - 1);         // 2->3층 앞쪽 옆면
        SlopedRail(f, root, StairLaneAMin - 0.04f, f.Level(1), false, 0, StepsPerFlight - 4);         // 2->3층 뒤쪽 옆면(맨 위 3단은 3->4층으로 건너가는 길)
        SlopedRail(f, root, StairLaneAMin + 0.04f, f.Level(2), true, 4, StepsPerFlight - 1);          // 3->4층 앞쪽 옆면

        // 3층 바닥 구멍(앞쪽 줄) 둘레: 앞 가장자리와 동쪽 가장자리. 4층 바닥 구멍(뒤쪽 줄)의 앞 가장자리.
        float level3 = f.Level(2), level4 = f.Level(3);
        StraightRail(f, root, new Vector2(StairXMin - 0.05f, StairLaneBMin - 0.04f), new Vector2(StairXMax + 0.05f, StairLaneBMin - 0.04f), level3);
        StraightRail(f, root, new Vector2(StairXMax + 0.09f, StairLaneBMin), new Vector2(StairXMax + 0.09f, StairLaneAMin), level3);
        StraightRail(f, root, new Vector2(StairXMin - 0.05f, StairLaneAMin - 0.04f), new Vector2(StairXMax + 0.05f, StairLaneAMin - 0.04f), level4);
    }

    // 바닥 높이 level에서 평면의 두 점 사이에 손잡이 봉과 살대를 세운다.
    private static void StraightRail(Frame f, Transform root, Vector2 from, Vector2 to, float level)
    {
        Vector3 a = f.P(from.x, from.y, level), b = f.P(to.x, to.y, level);
        Transform group = new GameObject("Railing").transform;
        group.SetParent(root, false);
        Bar(group, a + Vector3.up * RailHeight, b + Vector3.up * RailHeight, HandrailSize);
        int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / BalusterSpacing));
        for (int i = 0; i <= count; i++) Baluster(group, Vector3.Lerp(a, b, i / (float)count), RailHeight);
    }

    // 계단 한 줄의 옆면을 따라 기울어진 손잡이 봉을 걸고 단마다 살대를 세운다(첫 단 firstStep부터 lastStep까지).
    private static void SlopedRail(Frame f, Transform root, float pd, float baseY, bool climbsTowardPlus, int firstStep, int lastStep)
    {
        if (lastStep <= firstStep) return;
        float rise = FloorHeight / StepsPerFlight;
        float run = (StairXMax - StairXMin) / StepsPerFlight;
        Transform group = new GameObject("StairRailing").transform;
        group.SetParent(root, false);

        Vector3 first = StepTopCenter(f, pd, baseY, rise, run, climbsTowardPlus, firstStep);
        Vector3 last = StepTopCenter(f, pd, baseY, rise, run, climbsTowardPlus, lastStep);
        Bar(group, first + Vector3.up * RailHeight, last + Vector3.up * RailHeight, HandrailSize);
        for (int step = firstStep; step <= lastStep; step++)
            Baluster(group, StepTopCenter(f, pd, baseY, rise, run, climbsTowardPlus, step), RailHeight);
    }

    private static Vector3 StepTopCenter(Frame f, float pd, float baseY, float rise, float run, bool climbsTowardPlus, int step)
    {
        float x0 = climbsTowardPlus ? StairXMin + run * step : StairXMax - run * (step + 1);
        return f.P(x0 + run * 0.5f, pd, baseY + rise * (step + 1));
    }

    // 두 점을 잇는 가는 상자 봉이다(단면은 정사각형).
    private static void Bar(Transform parent, Vector3 a, Vector3 b, float thickness)
    {
        Vector3 direction = b - a;
        float length = direction.magnitude;
        if (length < 0.01f) return;

        GameObject go = new GameObject("Handrail");
        go.transform.SetParent(parent, false);
        go.transform.position = (a + b) * 0.5f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
        Vector3 size = new Vector3(length + thickness, thickness, thickness);
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, 1f);
        go.AddComponent<MeshRenderer>().sharedMaterial = railMaterial;
        go.AddComponent<BoxCollider>().size = size;
    }

    // 바닥 점 bottom에서 height만큼 위로 선 세로 살대다.
    private static void Baluster(Transform parent, Vector3 bottom, float height)
    {
        GameObject go = new GameObject("Baluster");
        go.transform.SetParent(parent, false);
        go.transform.position = bottom + Vector3.up * (height * 0.5f);
        Vector3 size = new Vector3(BalusterSize, height, BalusterSize);
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, 1f);
        go.AddComponent<MeshRenderer>().sharedMaterial = railMaterial;
        go.AddComponent<BoxCollider>().size = size;
    }

    // ---- 실내 구역(1인칭 전환용 월드 경계) ----

    private static void BuildIndoorZone(Frame f, Transform root)
    {
        Vector3 a = f.P(-HalfWidth, 0f, f.Level(0) - 0.5f), b = f.P(HalfWidth, Depth, f.Level(Floors) + 0.3f);
        GameObject go = new GameObject("IndoorZone");
        go.transform.SetParent(root, false);
        IndoorZone zone = go.AddComponent<IndoorZone>();
        SerializedObject so = new SerializedObject(zone);
        so.FindProperty("center").vector3Value = (a + b) * 0.5f;
        so.FindProperty("size").vector3Value = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 가구 ----

    // 방향 약속(원본 모델을 위에서 찍어 확인): 책상은 학생이 +X쪽에 앉아 -X를 보고, 칠판과 사물함은 +Z쪽이 앞면이다. 의자는 등받이 위치로 앞을 구한다.
    private static readonly Vector2 DeskForward = new Vector2(-1f, 0f);
    private static readonly Vector2 FrontIsPlusZ = new Vector2(0f, 1f);

    private static void BuildFurniture(Frame f, Transform root)
    {
        GameObject desk = LoadProp("Tables/Prefabs/TableSchool01aOld.prefab");
        GameObject chair = LoadProp("Chairs/Prefabs/Chair01aOld.prefab");
        GameObject board = LoadProp("Boards/Prefabs/Board01_1aOld.prefab");
        GameObject locker = LoadProp("Furniture/Prefabs/SchoolLocker01_1Old.prefab");
        Transform props = new GameObject("Props").transform;
        props.SetParent(root, false);
        Vector2 chairForward = ChairForward(chair);

        // 평면 방향(dpx, dpd)을 월드 방향으로 바꾼다.
        Vector2 toBoard = new Vector2(0f, 1f);        // 앞 교실에서 칠판(칸막이 벽) 쪽 = pd 증가
        Vector2 toFront = new Vector2(0f, -1f);       // 창이 있는 정면 쪽

        for (int floor = 0; floor < Floors; floor++)
        {
            float level = f.Level(floor);
            foreach (float side in new[] { -1f, 1f })
            {
                float roomMin = HallHalf + WallThickness, roomMax = HalfWidth - Lining - LiningGap;
                // 앞 교실: 책상 2열 x 3줄(학생이 칠판을 본다). 책상 모델에 좌석이 붙어 있어 의자는 따로 놓지 않는다.
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 2; col++)
                    {
                        if (rng.NextDouble() < 0.15) continue;   // 군데군데 비운다
                        float px = side * Mathf.Lerp(roomMin + 1.5f, roomMax - 1.5f, col);
                        float pd = 1.7f + row * 1.7f;
                        PlaceProp(f, props, desk, new Vector2(px, pd), level, WorldDirection(f, toBoard), DeskForward);
                    }
                // 교탁 자리에 의자 하나(칠판 쪽을 향한다), 흐트러진 의자 한두 개.
                PlaceProp(f, props, chair, new Vector2(side * (roomMin + roomMax) * 0.5f, 6.4f), level, WorldDirection(f, toFront), chairForward);
                if (rng.NextDouble() < 0.6) PlaceProp(f, props, chair, new Vector2(side * (roomMin + 1.2f), 5.2f), level, RandomDirection(), chairForward);

                // 칠판: 칸막이 벽 앞면(앞 교실 쪽)에 건다. 앞면(+Z)이 창 쪽(학생)을 향한다.
                PlaceWallProp(f, props, board, side * (roomMin + roomMax) * 0.5f, RoomSplit - WallThickness * 0.5f - 0.06f, level + 1.3f, WorldDirection(f, toFront), FrontIsPlusZ);

                // 뒤 방: 바깥 벽을 따라 사물함(문이 방 안쪽 = 홀 쪽을 향한다)과 책상 몇 개.
                Vector2 toHall = new Vector2(-side, 0f);
                float wallPx = side * (HalfWidth - Lining - LiningGap - 0.15f);
                PlaceProp(f, props, locker, new Vector2(wallPx, 9.4f), level, WorldDirection(f, toHall), FrontIsPlusZ);
                PlaceProp(f, props, locker, new Vector2(wallPx, 10.3f), level, WorldDirection(f, toHall), FrontIsPlusZ);
                if (rng.NextDouble() < 0.7) PlaceProp(f, props, desk, new Vector2(side * 7f, 12.6f), level, RandomDirection(), DeskForward);
            }
        }
    }

    // 평면 방향 벡터(px, pd)를 월드 xz 방향으로 바꾼다(정면이 -Z인 건물은 좌우/앞뒤가 뒤집힌다).
    private static Vector2 WorldDirection(Frame f, Vector2 plan) => new Vector2(f.sign * plan.x, -f.sign * plan.y);

    private static Vector2 RandomDirection()
    {
        float angle = rng.Next(0, 4) * 90f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
    }

    // 모델의 앞면이 localForward(모델 좌표 xz)일 때, 그 앞면이 월드 방향 desired를 향하도록 y축 회전각(도)을 구한다.
    private static float YawFor(Vector2 localForward, Vector2 desired)
    {
        float from = Mathf.Atan2(localForward.x, localForward.y) * Mathf.Rad2Deg;
        float to = Mathf.Atan2(desired.x, desired.y) * Mathf.Rad2Deg;
        return to - from;
    }

    // 의자의 앞(앉는 사람이 보는 쪽)은 등받이의 반대쪽이다. 높은 정점(등받이)의 무게중심이 몸통 중심에서 어느 쪽인지로 구한다.
    private static Vector2 ChairForward(GameObject chair)
    {
        if (chair == null) return new Vector2(0f, 1f);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(chair);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        Bounds bounds = RendererBounds(instance);
        float highY = bounds.min.y + bounds.size.y * 0.65f;
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 world = filter.transform.TransformPoint(vertex);
                if (world.y < highY) continue;
                sum += world;
                count++;
            }
        }
        Object.DestroyImmediate(instance);
        if (count == 0) return new Vector2(0f, 1f);

        Vector3 back = sum / count - bounds.center;
        Vector2 forward = new Vector2(-back.x, -back.z);
        return forward.sqrMagnitude < 0.0001f ? new Vector2(0f, 1f) : forward.normalized;
    }

    private static GameObject LoadProp(string relative) => AssetDatabase.LoadAssetAtPath<GameObject>(Interior + relative);

    // 모델 앞면이 월드 방향 facing을 향하게 돌리고, 경계의 바닥 중앙이 (px, pd)에 오도록 놓는다(원본 피벗이 모서리라 경계로 맞춘다).
    private static void PlaceProp(Frame f, Transform parent, GameObject prefab, Vector2 plan, float level, Vector2 facing, Vector2 localForward)
    {
        if (prefab == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(f.P(plan.x, plan.y, level), Quaternion.Euler(0f, YawFor(localForward, facing), 0f));
        Bounds bounds = RendererBounds(instance);
        Vector3 target = f.P(plan.x, plan.y, level);
        instance.transform.position += new Vector3(target.x - bounds.center.x, level - bounds.min.y, target.z - bounds.center.z);
    }

    // 벽에 거는 물건: 높이 y 기준으로 경계 중심이 (px, pd)에 오게 한다.
    private static void PlaceWallProp(Frame f, Transform parent, GameObject prefab, float px, float pd, float y, Vector2 facing, Vector2 localForward)
    {
        if (prefab == null) return;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(f.P(px, pd, y), Quaternion.Euler(0f, YawFor(localForward, facing), 0f));
        Bounds bounds = RendererBounds(instance);
        Vector3 target = f.P(px, pd, y);
        instance.transform.position += new Vector3(target.x - bounds.center.x, y - bounds.center.y, target.z - bounds.center.z);
    }

    private static Bounds RendererBounds(GameObject go)
    {
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    // ---- 조명(항상 켜져 있고 고장/깜빡임이 섞인 천장 조명) ----

    private static void BuildLights(Frame f, Transform root, int seed)
    {
        Transform lights = new GameObject("Lights").transform;
        lights.SetParent(root, false);
        int index = 0;
        for (int floor = 0; floor < Floors; floor++)
        {
            float y = f.Level(floor) + Clear - 0.25f;
            var spots = new List<Vector2> { new Vector2(0f, 4f), new Vector2(0f, 8.5f), new Vector2(-8.6f, 3.7f), new Vector2(8.6f, 3.7f), new Vector2(-8.6f, 11f), new Vector2(8.6f, 11f) };
            foreach (Vector2 spot in spots)
            {
                GameObject go = new GameObject("InteriorLight");
                go.transform.SetParent(lights, false);
                go.transform.position = f.P(spot.x, spot.y, y);
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 8.5f;
                light.intensity = 1.5f;
                light.color = new Color(0.86f, 1f, 0.92f);
                light.shadows = LightShadows.None;

                NightLight night = go.AddComponent<NightLight>();
                SerializedObject so = new SerializedObject(night);
                SerializedProperty list = so.FindProperty("lights");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = light;
                so.FindProperty("alwaysOn").boolValue = true;
                so.FindProperty("fault").enumValueIndex = (int)LampFaultPattern.Assign((float)rng.NextDouble(), 0.3f, 0.25f);
                so.FindProperty("faultSeed").intValue = seed * 131 + index++;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    // ---- 보급 상자 ----

    private static void BuildCrates(Frame f, Transform root)
    {
        GameObject crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/World/Resource_SupplyCrate.prefab");
        if (crate == null) return;

        Transform crates = new GameObject("Crates").transform;
        crates.SetParent(root, false);
        for (int floor = 0; floor < Floors; floor++)
        {
            float level = f.Level(floor);
            // 층마다 뒤 방 구석 두 곳과 홀 한 곳.
            var spots = new[] { new Vector2(-12.2f, 13.8f), new Vector2(12.2f, 13.8f), new Vector2(-3.2f, 6f) };
            foreach (Vector2 spot in spots)
            {
                if (floor == 0 && spot.x == -3.2f) continue;   // 현관 안쪽은 비운다
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(crate, crates);
                instance.transform.position = f.P(spot.x, spot.y, level);
                instance.transform.rotation = Quaternion.Euler(0f, rng.Next(0, 4) * 90f + 20f, 0f);
            }
        }
    }

    // ---- 상자형 조각 만들기 ----

    // 평면 좌표(px 범위, pd 범위)와 월드 높이 범위로 벽/바닥 조각 하나를 만든다. 재질 무늬가 늘어나지 않게 월드 크기에 맞춘 UV를 가진 메시를 쓴다.
    private static GameObject Wall(Frame f, Transform parent, string name, float px0, float px1, float pd0, float pd1, float y0, float y1, Material material, float tile = 2f, bool collide = true)
    {
        if (px1 - px0 < 0.01f || pd1 - pd0 < 0.01f || y1 - y0 < 0.01f) return null;

        Vector3 a = f.P(px0, pd0, y0), b = f.P(px1, pd1, y1);
        Vector3 center = (a + b) * 0.5f;
        Vector3 size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, tile);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collide) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    // 월드 방향에 맞춘 UV(한 칸이 tile m)를 가진 상자 메시다. 같은 크기는 에셋으로 한 번만 만든다.
    internal static Mesh BoxMesh(Vector3 size, float tile)
    {
        string key = $"Box_{size.x:F2}_{size.y:F2}_{size.z:F2}_t{tile:F1}";
        if (meshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

        string path = MeshDir + key.Replace('.', '_') + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = CreateBox(size, tile);
            mesh.name = key;
            AssetDatabase.CreateAsset(mesh, path);
        }
        meshCache[key] = mesh;
        return mesh;
    }

    private static Mesh CreateBox(Vector3 size, float tile)
    {
        Vector3 h = size * 0.5f;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        void Face(Vector3 normal, Vector3 u, Vector3 v, float uLength, float vLength)
        {
            int start = vertices.Count;
            Vector3 origin = Vector3.Scale(normal, h);
            // 면의 네 모서리: origin ± u ± v (u, v는 반 길이 벡터).
            vertices.Add(origin - u - v); vertices.Add(origin + u - v); vertices.Add(origin + u + v); vertices.Add(origin - u + v);
            for (int i = 0; i < 4; i++) normals.Add(normal);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(uLength / tile, 0f)); uvs.Add(new Vector2(uLength / tile, vLength / tile)); uvs.Add(new Vector2(0f, vLength / tile));
            // 유니티는 앞면이 시계 방향이다: (b-a)x(c-a)가 바깥 법선과 같은 방향이 되게 순서를 정한다.
            bool aligned = Vector3.Dot(Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]), normal) > 0f;
            if (aligned) triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            else triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        // 법선이 바깥을 향하도록 (u × v) 방향을 맞춰 감는다.
        Face(Vector3.right, new Vector3(0, 0, h.z), new Vector3(0, h.y, 0), size.z, size.y);
        Face(Vector3.left, new Vector3(0, 0, -h.z), new Vector3(0, h.y, 0), size.z, size.y);
        Face(Vector3.up, new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), size.x, size.z);
        Face(Vector3.down, new Vector3(-h.x, 0, 0), new Vector3(0, 0, h.z), size.x, size.z);
        Face(Vector3.forward, new Vector3(-h.x, 0, 0), new Vector3(0, h.y, 0), size.x, size.y);
        Face(Vector3.back, new Vector3(h.x, 0, 0), new Vector3(0, h.y, 0), size.x, size.y);

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }
}
