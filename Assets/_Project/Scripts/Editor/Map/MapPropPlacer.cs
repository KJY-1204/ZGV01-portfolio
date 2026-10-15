// 맵 프리팹을 지형 높이에 맞춰 놓고 겹침을 검사하는 배치 도우미다. 프리팹 피벗이 중심인 에셋이 많아서 바운즈로 바닥과 중심 오프셋을 계산한다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapPropPlacer
{
    private struct PieceInfo
    {
        public Vector3 size;
        public Vector2 centerOffset;   // 피벗에서 렌더 바운즈 중심까지(로컬 xz)
        public float minY;             // 피벗 기준 바닥 높이(로컬)
    }

    private struct Footprint
    {
        public Vector2 center, half;
        public float yaw;
    }

    private readonly Terrain terrain;
    private readonly System.Random random;
    private readonly Dictionary<string, PieceInfo> infoCache = new Dictionary<string, PieceInfo>();
    private readonly List<Footprint> occupied = new List<Footprint>();
    private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    // 지금까지 배치에 쓴 외부/프로젝트 프리팹 경로다(에셋 목록 파일을 만들 때 쓴다).
    public readonly HashSet<string> UsedPrefabs = new HashSet<string>();

    public MapPropPlacer(Terrain terrain, int seed)
    {
        this.terrain = terrain;
        random = new System.Random(seed);
    }

    // ---- 난수 도우미 ----
    public float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    public int Range(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
    public bool Chance(float probability) => random.NextDouble() < probability;
    public T Pick<T>(IList<T> list) => list[random.Next(list.Count)];

    public float GroundHeight(Vector2 xz) => terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + terrain.transform.position.y;

    public float SlopeDegrees(Vector2 xz)
    {
        Vector2 n = MapLayout.ToNormalized(xz.x, xz.y);
        return terrain.terrainData.GetSteepness(n.x, n.y);
    }

    private PieceInfo Info(string path)
    {
        if (infoCache.TryGetValue(path, out PieceInfo info)) return info;
        GameObject prefab = LoadPrefab(path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Bounds bounds = new Bounds();
        bool any = false;
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!any) bounds = new Bounds(Vector3.up * 0.5f, Vector3.one);
        info = new PieceInfo { size = bounds.size, centerOffset = new Vector2(bounds.center.x, bounds.center.z), minY = bounds.min.y };
        Object.DestroyImmediate(instance);
        infoCache[path] = info;
        return info;
    }

    private GameObject LoadPrefab(string path)
    {
        if (prefabCache.TryGetValue(path, out GameObject cached)) return cached;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new System.InvalidOperationException("프리팹을 찾지 못했다: " + path);
        prefabCache[path] = prefab;
        UsedPrefabs.Add(path);
        return prefab;
    }

    public Vector2 FootprintSize(string path, float scale = 1f)
    {
        PieceInfo info = Info(path);
        return new Vector2(info.size.x, info.size.z) * scale;
    }

    // 렌더 바운즈 중심이 xz에 오도록 yaw로 돌려 지형 위에 놓는다. 겹치면(checkOverlap) null을 돌려준다. lift는 바닥에서 띄울 높이(적재용)다.
    public GameObject Place(string path, Vector2 xz, float yawDegrees, Transform parent, float scale = 1f, float lift = 0f, bool checkOverlap = true, float margin = 0.6f, bool reserve = true)
    {
        PieceInfo info = Info(path);
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 offset = rotation * new Vector3(info.centerOffset.x, 0f, info.centerOffset.y) * scale;
        Vector2 pivotXz = xz - new Vector2(offset.x, offset.z);

        Footprint footprint = new Footprint { center = xz, half = new Vector2(info.size.x, info.size.z) * (0.5f * scale), yaw = yawDegrees };
        if (checkOverlap && Overlaps(footprint, margin)) return null;
        if (reserve && lift <= 0.01f) occupied.Add(footprint);

        float ground = GroundHeight(pivotXz);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(path), parent);
        instance.transform.SetPositionAndRotation(new Vector3(pivotXz.x, ground - info.minY * scale + lift - 0.05f, pivotXz.y), rotation);
        instance.transform.localScale = instance.transform.localScale * scale;
        return instance;
    }

    // 점이 이미 놓인 것과 겹치는지(반경 radius) 본다. 배치 전에 자리를 확인할 때 쓴다.
    public bool IsFree(Vector2 xz, float radius)
    {
        return !Overlaps(new Footprint { center = xz, half = new Vector2(radius, radius), yaw = 0f }, 0f);
    }

    // 자리를 비워 두기 위해 영역을 점유한다(시작점 주변 등).
    public void Reserve(Vector2 center, float radius)
    {
        occupied.Add(new Footprint { center = center, half = new Vector2(radius, radius), yaw = 0f });
    }

    private bool Overlaps(Footprint a, float margin)
    {
        foreach (Footprint b in occupied)
        {
            if ((a.center - b.center).sqrMagnitude > (a.half.magnitude + b.half.magnitude + margin * 2f) * (a.half.magnitude + b.half.magnitude + margin * 2f)) continue;
            if (OrientedOverlap(a, b, margin)) return true;
        }
        return false;
    }

    // 회전한 두 직사각형의 분리축 검사다.
    private static bool OrientedOverlap(Footprint a, Footprint b, float margin)
    {
        Vector2[] axes =
        {
            Rotate(Vector2.right, a.yaw), Rotate(Vector2.up, a.yaw),
            Rotate(Vector2.right, b.yaw), Rotate(Vector2.up, b.yaw),
        };
        Vector2 delta = b.center - a.center;
        foreach (Vector2 axis in axes)
        {
            float ra = Project(a, axis) + margin;
            float rb = Project(b, axis);
            if (Mathf.Abs(Vector2.Dot(delta, axis)) > ra + rb) return false;
        }
        return true;
    }

    private static Vector2 Rotate(Vector2 v, float yawDegrees)
    {
        // Unity의 yaw는 y축 회전이라 xz 평면에서 시계 방향이다(x=sin, z=cos 기준).
        float r = yawDegrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(r), c = Mathf.Cos(r);
        return new Vector2(v.x * c + v.y * s, -v.x * s + v.y * c);
    }

    private static float Project(Footprint f, Vector2 axis)
    {
        Vector2 ax = Rotate(Vector2.right, f.yaw) * f.half.x;
        Vector2 az = Rotate(Vector2.up, f.yaw) * f.half.y;
        return Mathf.Abs(Vector2.Dot(ax, axis)) + Mathf.Abs(Vector2.Dot(az, axis));
    }
}
