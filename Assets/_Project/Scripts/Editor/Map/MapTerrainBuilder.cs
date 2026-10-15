// 하이트맵에서 구간을 잘라 구역 평탄화와 도로 정지를 적용하고, 지형 레이어를 칠하고 나무를 심어 TerrainData 에셋과 Terrain 오브젝트를 만드는 에디터 도구다.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapTerrainBuilder
{
    private const string TerrainLayerDir = "Assets/TirgamesAssets/SchoolScene/Environment/TerrainLayers/";

    // 칠하는 레이어 순서다(알파맵 채널 순서와 같다).
    private static readonly string[] LayerFiles =
    {
        "layer_Groundgrass.terrainlayer",     // 0 풀
        "layer_Ground2_5_2.terrainlayer",     // 1 흙
        "layer_TileFloor06_1.terrainlayer",    // 2 낡은 타일/잔해
        "layer_TileFloor11.terrainlayer",     // 3 콘크리트 바닥
        "layer_AsphaltRoad1_2.terrainlayer",  // 4 아스팔트(흰 줄 없는 변형)
    };

    private const int Grass = 0, Dirt = 1, Rubble = 2, Concrete = 3, Asphalt = 4;

    // 지형을 새로 만들고 씬에 Terrain 오브젝트를 돌려준다. 이미 있으면 같은 에셋 파일을 덮어써서 GUID를 유지한다.
    public static Terrain Build()
    {
        int res = MapLayout.HeightResolution;
        float cell = MapLayout.Size / (res - 1);

        float[,] raw = MapHeightmapReader.Read(MapLayout.HeightmapPath, MapLayout.CropX, MapLayout.CropY, MapLayout.CropSize, res);
        float[,] meters = ShapeRelief(raw, res);
        // 테두리 산등성이를 먼저 올려야 그 위에 구역 평탄화와 도로 정지가 적용돼 구역 바닥이 평평하게 남는다.
        RaiseRim(meters, res, cell);
        float[] zoneTargets = FlattenZones(meters, res, cell);
        GradeRoad(meters, res, cell, zoneTargets);
        Smooth(meters, res, 1);
        CarveMinePlaza(meters, res, cell);

        TerrainData data = LoadOrCreateData(res);
        float[,] normalized = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                normalized[z, x] = Mathf.Clamp01(meters[z, x] / MapLayout.Height);
        data.SetHeights(0, 0, normalized);
        CutMineEntranceHole(data);

        TerrainLayer[] layers = new TerrainLayer[LayerFiles.Length];
        for (int i = 0; i < layers.Length; i++) layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerDir + LayerFiles[i]);
        data.terrainLayers = layers;
        PaintLayers(data, meters, res, cell);
        PlantTrees(data, meters, res, cell);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        GameObject holder;
        if (terrain == null)
        {
            holder = Terrain.CreateTerrainGameObject(data);
            holder.name = "Terrain";
            terrain = holder.GetComponent<Terrain>();
        }
        else
        {
            terrain.terrainData = data;
            terrain.GetComponent<TerrainCollider>().terrainData = data;
            holder = terrain.gameObject;
        }
        holder.transform.position = new Vector3(-MapLayout.Size * 0.5f, 0f, -MapLayout.Size * 0.5f);
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 6f;
        terrain.basemapDistance = 500f;
        // 450m까지 나무 12555그루를 메시로 그리면 에디터 기준 프레임이 약 7ms 늘어서 250m로 줄였다(측정은 context-notes.md 참고).
        terrain.treeDistance = 250f;
        // 나무 프리팹은 Tree 컴포넌트가 없어서 빌보드로 바뀌면 검은 사각형이 된다. 컬링 거리까지 메시로 그리게 한다.
        terrain.treeBillboardDistance = terrain.treeDistance;
        // 풀은 URP에서 지형 기본 디테일이 그려지지 않아 GrassField가 밀도 데이터를 읽어 직접 그린다.
        terrain.detailObjectDistance = 0f;
        GrassField grass = terrain.GetComponent<GrassField>() != null ? terrain.GetComponent<GrassField>() : terrain.gameObject.AddComponent<GrassField>();
        SerializedObject grassSo = new SerializedObject(grass);
        grassSo.FindProperty("drawDistance").floatValue = 50f;
        grassSo.FindProperty("densityScale").floatValue = 0.75f;
        grassSo.ApplyModifiedPropertiesWithoutUndo();
        terrain.groupingID = 0;
        terrain.allowAutoConnect = false;
        return terrain;
    }

    private static TerrainData LoadOrCreateData(int res)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MapLayout.TerrainAssetPath));
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(MapLayout.TerrainAssetPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, MapLayout.TerrainAssetPath);
        }
        data.heightmapResolution = res;
        data.size = new Vector3(MapLayout.Size, MapLayout.Height, MapLayout.Size);
        data.alphamapResolution = MapLayout.AlphaResolution;
        data.baseMapResolution = 512;
        return data;
    }

    // 0~1 원본 높이를 구간 안의 최소/최대를 기준으로 정규화해 ReliefMeters 높이 차의 미터 높이로 바꾼다. 큰 굴곡은 유지하고 잔잔한 잡음은 눌러 준다.
    private static float[,] ShapeRelief(float[,] raw, int res)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (float v in raw) { if (v < min) min = v; if (v > max) max = v; }
        float[,] meters = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                meters[z, x] = MapLayout.BaseHeight + Mathf.InverseLerp(min, max, raw[z, x]) * MapLayout.ReliefMeters;
        Smooth(meters, res, 2);
        return meters;
    }

    // 지형 가장자리를 산등성이로 올려서 갈 수 있는 곳 바깥의 빈 공간이 보이지 않게 하고 가파른 경사로 나가는 길을 막는다.
    private static void RaiseRim(float[,] h, int res, float cell)
    {
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float d = Mathf.Min(Mathf.Min(x, z), Mathf.Min(res - 1 - x, res - 1 - z)) * cell;
                float t = 1f - Mathf.SmoothStep(0f, 1f, d / MapLayout.RimWidth);
                h[z, x] += t * MapLayout.RimHeight;
            }
    }

    private static void Smooth(float[,] h, int res, int passes)
    {
        float[,] tmp = new float[res, res];
        for (int p = 0; p < passes; p++)
        {
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx, zz = z + dz;
                            if (xx < 0 || zz < 0 || xx >= res || zz >= res) continue;
                            sum += h[zz, xx]; n++;
                        }
                    tmp[z, x] = sum / n;
                }
            System.Array.Copy(tmp, h, h.Length);
        }
    }

    private static Vector2 CellToWorld(int x, int z, float cell) => new Vector2(x * cell - MapLayout.Size * 0.5f, z * cell - MapLayout.Size * 0.5f);

    // 구역 안쪽을 구역 평균 높이로 평평하게 하고 바깥 여유 구간에서 원래 지형으로 부드럽게 잇는다. 채석장은 완전히 평평하지 않게 약하게만 한다.
    private static float[] FlattenZones(float[,] h, int res, float cell)
    {
        float[] targets = new float[MapLayout.Zones.Length];
        for (int zi = 0; zi < MapLayout.Zones.Length; zi++)
        {
            MapLayout.Zone zone = MapLayout.Zones[zi];
            // 경사가 완만하도록 구역마다 이음 구간을 길게 잡는다(야영지는 시작점이라 가장 길게).
            float blend = zone.kind == MapLayout.ZoneKind.Camp ? 90f : zone.kind == MapLayout.ZoneKind.Quarry ? 50f : 65f;
            float sum = 0f; int count = 0;
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    Vector2 w = CellToWorld(x, z, cell);
                    if (!MapLayout.InZone(zone, w.x, w.y, 0f)) continue;
                    sum += h[z, x]; count++;
                }
            if (count == 0) continue;
            float target = sum / count;
            targets[zi] = target;
            float strength = zone.kind == MapLayout.ZoneKind.Quarry ? 0.55f : 1f;

            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    Vector2 w = CellToWorld(x, z, cell);
                    float dx = Mathf.Max(0f, Mathf.Abs(w.x - zone.center.x) - zone.half.x);
                    float dz = Mathf.Max(0f, Mathf.Abs(w.y - zone.center.y) - zone.half.y);
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d >= blend) continue;
                    float weight = (1f - Mathf.SmoothStep(0f, 1f, d / blend)) * strength;
                    h[z, x] = Mathf.Lerp(h[z, x], target, weight);
                }
        }
        return targets;
    }

    // 도로 중심선의 높이 프로필을 이동 평균으로 매끈하게 만들고 도로 주변 지형을 그 높이로 당긴다.
    private static void GradeRoad(float[,] h, int res, float cell, float[] zoneTargets)
    {
        Vector2[] path = MapLayout.SampleRoad(2f);
        float[] profile = new float[path.Length];
        for (int i = 0; i < path.Length; i++) profile[i] = SampleHeight(h, res, cell, path[i]);

        // 구역 안(여유 포함)을 지나는 도로는 구역 바닥 높이로 고정해, 도로 보정이 구역 바닥을 기울이지 않게 한다.
        int[] zoneOf = new int[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            zoneOf[i] = -1;
            for (int zi = 0; zi < MapLayout.Zones.Length; zi++)
                if (MapLayout.InZone(MapLayout.Zones[zi], path[i].x, path[i].y, 12f)) { zoneOf[i] = zi; profile[i] = zoneTargets[zi]; break; }
        }

        float[] smooth = new float[path.Length];
        const int window = 14;
        for (int i = 0; i < path.Length; i++)
        {
            float sum = 0f;
            for (int k = -window; k <= window; k++) sum += profile[((i + k) % path.Length + path.Length) % path.Length];
            smooth[i] = zoneOf[i] >= 0 ? zoneTargets[zoneOf[i]] : sum / (2 * window + 1);
        }

        float[,] weight = new float[res, res];
        float[,] target = new float[res, res];
        float reach = MapLayout.RoadHalfWidth + 12f;
        int span = Mathf.CeilToInt(reach / cell);
        for (int i = 0; i < path.Length; i++)
        {
            int cx = Mathf.RoundToInt((path[i].x + MapLayout.Size * 0.5f) / cell);
            int cz = Mathf.RoundToInt((path[i].y + MapLayout.Size * 0.5f) / cell);
            for (int z = cz - span; z <= cz + span; z++)
                for (int x = cx - span; x <= cx + span; x++)
                {
                    if (x < 0 || z < 0 || x >= res || z >= res) continue;
                    Vector2 w = CellToWorld(x, z, cell);
                    float d = Vector2.Distance(w, path[i]);
                    if (d > reach) continue;
                    float wgt = d <= MapLayout.RoadHalfWidth + 1.5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - MapLayout.RoadHalfWidth - 1.5f) / (reach - MapLayout.RoadHalfWidth - 1.5f));
                    if (wgt <= weight[z, x]) continue;
                    weight[z, x] = wgt;
                    target[z, x] = smooth[i];
                }
        }

        // 구역 바닥은 평탄화가 정한 높이를 그대로 둔다(도로 보정이 구역 가장자리를 기울이지 않게).
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                if (weight[z, x] <= 0f) continue;
                Vector2 w = CellToWorld(x, z, cell);
                bool insideZone = false;
                foreach (MapLayout.Zone zone in MapLayout.Zones)
                    if (MapLayout.InZone(zone, w.x, w.y, 0f)) { insideZone = true; break; }
                if (!insideZone) h[z, x] = Mathf.Lerp(h[z, x], target[z, x], weight[z, x]);
            }
    }

    // 광산 입구 앞 광장: 채석장 바닥 높이로 평평하게 파내고, 입구 줄(MinePortal.y)부터 안쪽은 원래 산을 그대로 둬 한 칸 폭의 절벽이 생기게 한다. 옆과 앞쪽은 부드럽게 잇는다.
    private static void CarveMinePlaza(float[,] h, int res, float cell)
    {
        Vector2 portal = MapLayout.MinePortal;
        float target = SampleHeight(h, res, cell, new Vector2(portal.x, 400f));
        const float blend = 14f;
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                Vector2 w = CellToWorld(x, z, cell);
                if (w.y >= portal.y) continue;
                float dx = Mathf.Max(0f, Mathf.Abs(w.x - portal.x) - MapLayout.MinePlazaHalfWidth);
                float dz = Mathf.Max(0f, portal.y - MapLayout.MinePlazaDepth - w.y);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d >= blend) continue;
                h[z, x] = Mathf.Lerp(h[z, x], target, 1f - Mathf.SmoothStep(0f, 1f, d / blend));
            }
    }

    // 광산 입구 폭만큼 한 칸 절벽 면에 구멍을 뚫어 입구 아치를 통과하게 한다(구멍 뒤 산 지면은 갱도 천장 위에 그대로 남는다). 다른 곳의 구멍은 모두 메운다.
    private static void CutMineEntranceHole(TerrainData data)
    {
        int holesRes = data.holesResolution;
        float holeCell = MapLayout.Size / holesRes;
        bool[,] holes = new bool[holesRes, holesRes];
        for (int y = 0; y < holesRes; y++)
            for (int x = 0; x < holesRes; x++) holes[y, x] = true;

        Vector2 portal = MapLayout.MinePortal;
        // 절벽 칸은 광장 마지막 줄(z < MinePortal.y)과 첫 산 줄 사이의 칸이다.
        int row = Mathf.CeilToInt((portal.y + MapLayout.Size * 0.5f) / holeCell) - 1;
        int xFrom = Mathf.FloorToInt((portal.x - 2.8f + MapLayout.Size * 0.5f) / holeCell);
        int xTo = Mathf.FloorToInt((portal.x + 2.8f + MapLayout.Size * 0.5f) / holeCell);
        for (int x = xFrom; x <= xTo; x++) holes[row, x] = false;
        data.SetHoles(0, 0, holes);
    }

    private static float SampleHeight(float[,] h, int res, float cell, Vector2 world)
    {
        float fx = Mathf.Clamp((world.x + MapLayout.Size * 0.5f) / cell, 0f, res - 1.001f);
        float fz = Mathf.Clamp((world.y + MapLayout.Size * 0.5f) / cell, 0f, res - 1.001f);
        int x0 = (int)fx, z0 = (int)fz;
        float tx = fx - x0, tz = fz - z0;
        return Mathf.Lerp(Mathf.Lerp(h[z0, x0], h[z0, x0 + 1], tx), Mathf.Lerp(h[z0 + 1, x0], h[z0 + 1, x0 + 1], tx), tz);
    }

    // 도로 중심선까지의 거리(m)를 알파맵 칸마다 구한다.
    private static float[,] RoadDistance(int alphaRes)
    {
        Vector2[] path = MapLayout.SampleRoad(3f);
        float[,] distance = new float[alphaRes, alphaRes];
        float alphaCell = MapLayout.Size / alphaRes;
        for (int z = 0; z < alphaRes; z++)
            for (int x = 0; x < alphaRes; x++) distance[z, x] = 9999f;

        int span = Mathf.CeilToInt(14f / alphaCell);
        foreach (Vector2 p in path)
        {
            int cx = Mathf.FloorToInt((p.x + MapLayout.Size * 0.5f) / alphaCell);
            int cz = Mathf.FloorToInt((p.y + MapLayout.Size * 0.5f) / alphaCell);
            for (int z = cz - span; z <= cz + span; z++)
                for (int x = cx - span; x <= cx + span; x++)
                {
                    if (x < 0 || z < 0 || x >= alphaRes || z >= alphaRes) continue;
                    Vector2 w = new Vector2((x + 0.5f) * alphaCell - MapLayout.Size * 0.5f, (z + 0.5f) * alphaCell - MapLayout.Size * 0.5f);
                    float d = Vector2.Distance(w, p);
                    if (d < distance[z, x]) distance[z, x] = d;
                }
        }
        return distance;
    }

    // 풀을 바탕으로 경사/잡음에 따라 흙과 잔해, 구역 안 콘크리트·자갈, 도로 아스팔트를 칠한다.
    private static void PaintLayers(TerrainData data, float[,] meters, int res, float cell)
    {
        int ar = MapLayout.AlphaResolution;
        float[,] roadDistance = RoadDistance(ar);
        float[,,] alpha = new float[ar, ar, LayerFiles.Length];
        float alphaCell = MapLayout.Size / ar;

        for (int z = 0; z < ar; z++)
            for (int x = 0; x < ar; x++)
            {
                float wx = (x + 0.5f) * alphaCell - MapLayout.Size * 0.5f;
                float wz = (z + 0.5f) * alphaCell - MapLayout.Size * 0.5f;
                float slope = SlopeDegrees(meters, res, cell, wx, wz);

                float grass = 1f, dirt = 0f, rubble = 0f, concrete = 0f, asphalt = 0f;
                // 구역 안에서 콘크리트를 덮고 풀이 자란 정도(0~1)다. 아래 우선순위 계산 전에 콘크리트/잔해를 줄이는 데 쓴다.
                float grassCover = 0f;

                float patch = Mathf.PerlinNoise(wx * 0.021f + 31.7f, wz * 0.021f + 7.3f);
                dirt = Mathf.Max(dirt, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.8f, patch)) * 0.75f);
                float steep = Mathf.InverseLerp(22f, 38f, slope);
                dirt = Mathf.Max(dirt, steep);
                rubble = Mathf.Max(rubble, Mathf.InverseLerp(40f, 55f, slope) * 0.6f);

                foreach (MapLayout.Zone zone in MapLayout.Zones)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(wx - zone.center.x) - zone.half.x);
                    float dz = Mathf.Max(0f, Mathf.Abs(wz - zone.center.y) - zone.half.y);
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float inside = 1f - Mathf.SmoothStep(0f, 1f, d / 14f);
                    if (inside <= 0f) continue;
                    float noise = Mathf.PerlinNoise(wx * 0.09f, wz * 0.09f);
                    switch (zone.kind)
                    {
                        case MapLayout.ZoneKind.Camp:
                            dirt = Mathf.Max(dirt, inside * 0.9f);
                            break;
                        case MapLayout.ZoneKind.Quarry:
                            rubble = Mathf.Max(rubble, inside * (0.55f + 0.4f * noise));
                            dirt = Mathf.Max(dirt, inside * 0.7f);
                            break;
                        default:
                            // 산업 구역: 안쪽은 콘크리트와 균열 사이 흙, 가장자리는 자갈.
                            // 안쪽은 거의 콘크리트로 두고 잡음으로 낡은 타일과 흙이 군데군데 비치게 한다.
                            concrete = Mathf.Max(concrete, inside * Mathf.Lerp(0.7f, 1f, noise));
                            rubble = Mathf.Max(rubble, inside * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.85f, Mathf.PerlinNoise(wx * 0.05f + 5f, wz * 0.05f + 9f))) * 0.7f);
                            dirt = Mathf.Max(dirt, inside * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 0.9f, Mathf.PerlinNoise(wx * 0.03f + 40f, wz * 0.03f + 2f))) * 0.6f);
                            grassCover = Mathf.Max(grassCover, inside * OvergrowthCover(zone, wx, wz));
                            break;
                    }
                }

                float roadD = roadDistance[z, x];
                if (roadD < 12f)
                {
                    float shoulder = 1f - Mathf.SmoothStep(0f, 1f, (roadD - MapLayout.RoadHalfWidth) / 5f);
                    if (roadD > MapLayout.RoadHalfWidth) dirt = Mathf.Max(dirt, shoulder * 0.8f);
                    asphalt = 1f - Mathf.SmoothStep(0f, 1f, (roadD - MapLayout.RoadHalfWidth + 1.2f) / 1.8f);
                }

                // 풀이 자란 곳은 콘크리트와 잔해를 덜어 내 풀/흙이 비치게 한다(도로 아스팔트는 그대로).
                concrete *= 1f - grassCover;
                rubble *= 1f - grassCover * 0.6f;

                // 우선순위: 아스팔트 > 콘크리트 > 잔해 > 흙 > 풀. 위 층이 채운 만큼 아래 층이 줄어든다.
                float remaining = 1f;
                float wAsphalt = Mathf.Clamp01(asphalt) * remaining; remaining -= wAsphalt;
                float wConcrete = Mathf.Clamp01(concrete) * remaining; remaining -= wConcrete;
                float wRubble = Mathf.Clamp01(rubble) * remaining; remaining -= wRubble;
                float wDirt = Mathf.Clamp01(dirt) * remaining; remaining -= wDirt;
                float wGrass = Mathf.Max(0f, remaining) * grass;

                alpha[z, x, Grass] = wGrass;
                alpha[z, x, Dirt] = wDirt;
                alpha[z, x, Rubble] = wRubble;
                alpha[z, x, Concrete] = wConcrete;
                alpha[z, x, Asphalt] = wAsphalt;
            }

        data.SetAlphamaps(0, 0, alpha);
        PlantGrass(data, alpha);
    }

    // 산업 구역 바닥에 풀이 번진 정도(0~1)다. 큰 얼룩(잡음)과 구역 가장자리 침식이 합쳐지고, 학교는 건물 사이 마당을 가운데 길만 남기고 잔디밭으로 한다.
    private static float OvergrowthCover(MapLayout.Zone zone, float wx, float wz)
    {
        float dx = wx - zone.center.x, dz = wz - zone.center.y;
        // 구역 경계에서 안쪽으로 잰 거리다(밖이면 음수).
        float inner = Mathf.Min(zone.half.x - Mathf.Abs(dx), zone.half.y - Mathf.Abs(dz));
        float edge = (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inner / 26f))) * 0.65f;
        float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.62f, Mathf.PerlinNoise(wx * 0.026f + 80f, wz * 0.026f + 13f))) * 0.85f;
        float cover = Mathf.Max(edge, patch);

        if (zone.kind == MapLayout.ZoneKind.School)
        {
            // 두 건물 사이 마당(건물 A는 중심 +8, B는 -58): 가운데 폭 24m는 길로 두고 양옆은 잔디밭이다.
            float sideways = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 15f, Mathf.Abs(dx + 2f)));
            float acrossIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-50f, -44f, dz)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-6f, 0f, dz)));
            float lengthIn = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(64f, 72f, Mathf.Abs(dx)));
            cover = Mathf.Max(cover, sideways * acrossIn * lengthIn * 0.95f);
        }
        return Mathf.Clamp01(cover);
    }

    // 풀 알파가 높은 곳에 풀 다발(코드로 만든 텍스처)을 지형 디테일로 깐다. 길/구역 바닥(콘크리트, 아스팔트)에는 풀 알파가 낮아 자연히 비어 있다.
    private static void PlantGrass(TerrainData data, float[,,] alpha)
    {
        GameObject[] tufts = GrassTextureBuilder.EnsurePrefabs();
        const int detailResolution = 1024;
        data.SetDetailResolution(detailResolution, 16);
        data.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                prototype = tufts[0], usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit,
                minWidth = 0.9f, maxWidth = 1.5f, minHeight = 0.4f, maxHeight = 0.8f, noiseSpread = 0.35f,
            },
            new DetailPrototype
            {
                prototype = tufts[1], usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit,
                minWidth = 0.8f, maxWidth = 1.2f, minHeight = 0.25f, maxHeight = 0.5f, noiseSpread = 0.25f,
            },
        };

        int ar = alpha.GetLength(0);
        int[,] tall = new int[detailResolution, detailResolution];
        int[,] shortGrass = new int[detailResolution, detailResolution];
        for (int z = 0; z < detailResolution; z++)
            for (int x = 0; x < detailResolution; x++)
            {
                float u = (x + 0.5f) / detailResolution, v = (z + 0.5f) / detailResolution;
                float grass = alpha[Mathf.Min(ar - 1, (int)(v * ar)), Mathf.Min(ar - 1, (int)(u * ar)), Grass];
                if (grass < 0.5f) continue;

                // 큰 덩어리 잡음으로 풀이 우거진 곳과 성긴 곳을 나눈다.
                float clump = Mathf.PerlinNoise(u * 38f + 3.1f, v * 38f + 9.7f);
                float fine = Mathf.PerlinNoise(u * 210f + 1.3f, v * 210f + 4.4f);
                float amount = Mathf.Clamp01((grass - 0.4f) * 1.7f) * Mathf.Lerp(0.2f, 1.2f, clump) * Mathf.Lerp(0.55f, 1f, fine);
                tall[z, x] = Mathf.RoundToInt(amount * 1.6f * (clump > 0.55f ? 1f : 0.3f));
                shortGrass[z, x] = Mathf.RoundToInt(amount * 2.2f);
            }
        // 칸마다 풀 다발 수(인스턴스 개수)로 읽는다. 덮는 비율(커버리지)로 읽으면 한 자릿수 값은 거의 보이지 않는다.
        data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
        data.SetDetailLayer(0, 0, 0, tall);
        data.SetDetailLayer(0, 0, 1, shortGrass);
    }

    private static float SlopeDegrees(float[,] h, int res, float cell, float wx, float wz)
    {
        float e = cell * 2f;
        float hx = SampleHeight(h, res, cell, new Vector2(wx + e, wz)) - SampleHeight(h, res, cell, new Vector2(wx - e, wz));
        float hz = SampleHeight(h, res, cell, new Vector2(wx, wz + e)) - SampleHeight(h, res, cell, new Vector2(wx, wz - e));
        return Mathf.Atan(Mathf.Sqrt(hx * hx + hz * hz) / (2f * e)) * Mathf.Rad2Deg;
    }

    // 지터 격자에 잡음 밀도로 나무를 심는다. 구역과 도로, 가파른 곳은 피하고 숲 띠는 밀도를 높인다.
    private static void PlantTrees(TerrainData data, float[,] meters, int res, float cell)
    {
        string[] ids = MapTreeAdapterBuilder.TreeIds;
        var prototypes = new List<TreePrototype>();
        foreach (string id in ids)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapTreeAdapterBuilder.DecorPath(id));
            if (prefab != null) prototypes.Add(new TreePrototype { prefab = prefab });
        }
        data.treePrototypes = prototypes.ToArray();
        data.treeInstances = new TreeInstance[0];
        if (prototypes.Count == 0) return;

        Vector2[] road = MapLayout.SampleRoad(6f);
        Random.State saved = Random.state;
        Random.InitState(MapLayout.Seed);

        var instances = new List<TreeInstance>();
        // 간격을 줄이고 숲 밖 들판에도 나무를 조금 더 흩뜨려 지금보다 빽빽하게 한다.
        const float spacing = 5f;
        int n = Mathf.FloorToInt(MapLayout.Size / spacing);
        for (int gz = 0; gz < n; gz++)
            for (int gx = 0; gx < n; gx++)
            {
                float wx = -MapLayout.Size * 0.5f + (gx + Random.value) * spacing;
                float wz = -MapLayout.Size * 0.5f + (gz + Random.value) * spacing;

                float density = Mathf.Clamp01(ForestDensity(wx, wz) * 1.15f + 0.02f);
                if (Random.value > density) continue;

                bool blocked = MapLayout.InMinePlaza(wx, wz, 8f);
                foreach (MapLayout.Zone zone in MapLayout.Zones)
                    if (MapLayout.InZone(zone, wx, wz, 22f)) { blocked = true; break; }
                if (blocked) continue;
                foreach (Vector2 p in road)
                    if ((p.x - wx) * (p.x - wx) + (p.y - wz) * (p.y - wz) < 15f * 15f) { blocked = true; break; }
                if (blocked) continue;
                if (SlopeDegrees(meters, res, cell, wx, wz) > 34f) continue;

                Vector2 uv = MapLayout.ToNormalized(wx, wz);
                float pick = Random.value;
                int proto = pick < 0.3f ? 0 : pick < 0.55f ? 1 : pick < 0.75f ? 2 : 3;
                proto = Mathf.Min(proto, prototypes.Count - 1);
                float scale = Random.Range(0.75f, 1.15f);
                instances.Add(new TreeInstance
                {
                    position = new Vector3(uv.x, 0f, uv.y),
                    prototypeIndex = proto,
                    widthScale = scale,
                    heightScale = scale * Random.Range(0.9f, 1.1f),
                    rotation = Random.value * Mathf.PI * 2f,
                    color = Color.white,
                    lightmapColor = Color.white,
                });
            }

        data.SetTreeInstances(instances.ToArray(), true);
        Random.state = saved;
    }

    // 숲 밀도(0~1)다. 서쪽/남서/북동 숲 띠는 높게, 그 밖의 들판은 낮게 한다.
    public static float ForestDensity(float wx, float wz)
    {
        float noise = Mathf.PerlinNoise(wx * 0.006f + 91.1f, wz * 0.006f + 17.9f);
        float belts = Mathf.Max(
            BeltWeight(wx, wz, new Vector2(-420f, -40f), 190f),
            BeltWeight(wx, wz, new Vector2(-200f, -400f), 170f),
            BeltWeight(wx, wz, new Vector2(430f, 120f), 150f),
            BeltWeight(wx, wz, new Vector2(60f, 110f), 90f));
        // 테두리 산등성이는 숲으로 덮는다.
        float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(wx + MapLayout.Size * 0.5f, MapLayout.Size * 0.5f - wx), Mathf.Min(wz + MapLayout.Size * 0.5f, MapLayout.Size * 0.5f - wz)) / 140f);
        belts = Mathf.Max(belts, edge);
        float baseDensity = 0.04f + belts * 1.1f;
        return Mathf.Clamp01(baseDensity * Mathf.Lerp(0.5f, 1.3f, noise));
    }

    private static float BeltWeight(float x, float z, Vector2 center, float radius)
    {
        float d = Vector2.Distance(new Vector2(x, z), center);
        return 1f - Mathf.SmoothStep(0f, 1f, d / radius);
    }
}
