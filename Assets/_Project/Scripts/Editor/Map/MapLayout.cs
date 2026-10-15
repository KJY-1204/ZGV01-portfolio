// 플레이 맵(폐허 산업지대 약 1km x 1km)의 구역, 도로, 시작점, 지형 수치를 모아 둔 데이터다. 지형 도구와 배치 도구가 같은 값을 읽는다. 좌표는 월드 xz이고 지형 중심이 (0,0)이다.
using UnityEngine;

public static class MapLayout
{
    public const float Size = 1000f;
    // 지형이 가질 수 있는 높이 범위(m)다. 실제 지형은 이 중 아래쪽 일부를 쓴다.
    public const float Height = 90f;
    public const int HeightResolution = 1025;
    public const int AlphaResolution = 512;

    // 원본 하이트맵(4033x4033, 16비트)에서 잘라 쓸 구간이다. 원본 파일은 저장소에 넣지 않는다.
    public const string HeightmapPath = "Assets/All In One - Heightmaps/Heightmaps/ValleyLandscape/T_HeightMap4k.png";
    public const int CropX = 700;
    public const int CropY = 700;
    public const int CropSize = 2049;
    // 자른 구간의 가장 낮은 곳~높은 곳이 이 높이 차(m)가 되게 늘린다. 기본 바닥 높이는 BaseHeight다.
    public const float ReliefMeters = 36f;
    public const float BaseHeight = 8f;

    // 가장자리를 올리는 너비와 높이(m)다.
    public const float RimWidth = 110f;
    public const float RimHeight = 34f;
    public const float RimWallDistance = 6f;

    public const int Seed = 20261010;

    public const string TerrainAssetPath = "Assets/_Project/Data/Map/Terrain_Industrial.asset";
    public const string ScenePath = "Assets/_Project/Scenes/Main/Main_Map.unity";

    public enum ZoneKind { Camp, Warehouse, Factory, School, Yard, Quarry }

    public struct Zone
    {
        public string name;
        public ZoneKind kind;
        public Vector2 center;
        public Vector2 half;   // 가로/세로 반길이(m)
    }

    public static readonly Zone[] Zones =
    {
        new Zone { name = "Camp",      kind = ZoneKind.Camp,      center = new Vector2(-40f, -380f), half = new Vector2(55f, 45f) },
        new Zone { name = "Warehouse", kind = ZoneKind.Warehouse, center = new Vector2(220f, -120f), half = new Vector2(105f, 80f) },
        new Zone { name = "Factory",   kind = ZoneKind.Factory,   center = new Vector2(240f, 250f),  half = new Vector2(125f, 105f) },
        new Zone { name = "School",    kind = ZoneKind.School,    center = new Vector2(-250f, 200f), half = new Vector2(85f, 75f) },
        new Zone { name = "Yard",      kind = ZoneKind.Yard,      center = new Vector2(-230f, -130f), half = new Vector2(80f, 70f) },
        new Zone { name = "Quarry",    kind = ZoneKind.Quarry,    center = new Vector2(20f, 400f),   half = new Vector2(85f, 55f) },
    };

    // 도로가 지나는 제어점이다(닫힌 순환). 각 구역 중심 근처를 지난다.
    public static readonly Vector2[] RoadControlPoints =
    {
        new Vector2(-40f, -380f),
        new Vector2(70f, -340f),
        new Vector2(170f, -250f),
        new Vector2(220f, -120f),
        new Vector2(225f, 40f),
        new Vector2(240f, 250f),
        new Vector2(130f, 345f),
        new Vector2(20f, 400f),
        new Vector2(-120f, 335f),
        new Vector2(-250f, 200f),
        new Vector2(-265f, 40f),
        new Vector2(-230f, -130f),
        new Vector2(-190f, -260f),
        new Vector2(-110f, -345f),
    };

    public const float RoadHalfWidth = 4.5f;

    // 광산: 채석장 북쪽 산자락을 파서 만든 평지(광장)와 그 뒤 산 안쪽 갱도다. 입구 정면은 -Z쪽을 보고, 안쪽은 +Z쪽으로 이어진다.
    public static readonly Vector2 MinePortal = new Vector2(20f, 452f);
    public const float MinePlazaHalfWidth = 32f;
    public const float MinePlazaDepth = 32f;
    // 광산 내부가 차지하는 범위(포털 기준 좌우 반폭, 안쪽 깊이)다.
    public const float MineHalfWidth = 82f;
    public const float MineDepth = 42f;

    // 점이 광산 광장(입구 앞 평지, 여유 margin 포함) 안인지 본다.
    public static bool InMinePlaza(float x, float z, float margin)
    {
        return Mathf.Abs(x - MinePortal.x) <= MinePlazaHalfWidth + margin && z >= MinePortal.y - MinePlazaDepth - margin && z <= MinePortal.y + margin;
    }

    // 점이 광산 광장 또는 산 안쪽 범위(여유 margin 포함) 안인지 본다.
    public static bool InMine(float x, float z, float margin)
    {
        return Mathf.Abs(x - MinePortal.x) <= MineHalfWidth + margin && z >= MinePortal.y - MinePlazaDepth - margin && z <= MinePortal.y + MineDepth + margin;
    }

    // 플레이어 시작점(야영지 중심)이다. 높이는 지형에서 구한다.
    public static Vector2 PlayerStart => Zones[0].center;

    // 월드 xz를 지형 로컬 좌표(0~1)로 바꾼다.
    public static Vector2 ToNormalized(float worldX, float worldZ) => new Vector2((worldX + Size * 0.5f) / Size, (worldZ + Size * 0.5f) / Size);

    // 주어진 점이 구역(여유 margin 포함) 안인지 본다.
    public static bool InZone(Zone zone, float x, float z, float margin)
    {
        return Mathf.Abs(x - zone.center.x) <= zone.half.x + margin && Mathf.Abs(z - zone.center.y) <= zone.half.y + margin;
    }

    // Catmull-Rom으로 부드럽게 이은 도로 중심선을 spacing(m) 간격 점으로 돌려준다.
    public static Vector2[] SampleRoad(float spacing)
    {
        int n = RoadControlPoints.Length;
        var points = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = RoadControlPoints[(i - 1 + n) % n];
            Vector2 p1 = RoadControlPoints[i];
            Vector2 p2 = RoadControlPoints[(i + 1) % n];
            Vector2 p3 = RoadControlPoints[(i + 2) % n];
            float length = Vector2.Distance(p1, p2);
            int steps = Mathf.Max(2, Mathf.CeilToInt(length / spacing));
            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                float t2 = t * t, t3 = t2 * t;
                Vector2 point = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                points.Add(point);
            }
        }
        return points.ToArray();
    }
}
