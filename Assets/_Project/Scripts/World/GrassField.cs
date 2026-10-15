// 지형 디테일(풀) 밀도 데이터를 읽어 카메라 주변의 풀 다발을 GPU 인스턴싱으로 직접 그리는 컴포넌트다. URP에서 지형 기본 디테일 렌더링이 그려지지 않아 대신 쓴다. 상태나 충돌은 없고 표현만 맡는다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain))]
public class GrassField : MonoBehaviour
{
    // 이 거리(m) 안의 풀만 그린다.
    [SerializeField] private float drawDistance = 50f;
    // 칸마다 정해 둔 풀 수 중 실제로 그릴 비율이다(0.5면 절반으로 성기게).
    [SerializeField, Range(0.1f, 1f)] private float densityScale = 0.75f;
    // 한 덩어리가 가로세로 몇 칸인지다(칸은 디테일 해상도 한 칸).
    [SerializeField] private int chunkCells = 16;
    // 한 프레임에 새로 만들 덩어리 수다(이동할 때 끊기지 않게 나눠 만든다).
    [SerializeField] private int chunksPerFrame = 3;

    private const int MaxInstancesPerBatch = 1023;

    private class Layer
    {
        public Mesh mesh;
        public Material material;
        public int[,] density;
        public float minWidth, maxWidth, minHeight, maxHeight;
    }

    private class Chunk
    {
        public readonly List<Matrix4x4[]>[] batches;
        public readonly List<int>[] counts;
        public Chunk(int layers)
        {
            batches = new List<Matrix4x4[]>[layers];
            counts = new List<int>[layers];
            for (int i = 0; i < layers; i++) { batches[i] = new List<Matrix4x4[]>(); counts[i] = new List<int>(); }
        }
    }

    private Terrain terrain;
    private TerrainData data;
    private readonly List<Layer> layers = new List<Layer>();
    private readonly Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();
    private float cellSize;
    private int detailResolution;

    private void Start()
    {
        terrain = GetComponent<Terrain>();
        data = terrain.terrainData;
        if (data == null) return;

        detailResolution = data.detailResolution;
        cellSize = data.size.x / Mathf.Max(1, detailResolution);
        DetailPrototype[] prototypes = data.detailPrototypes;
        for (int i = 0; i < prototypes.Length; i++)
        {
            GameObject prefab = prototypes[i].prototype;
            MeshFilter filter = prefab != null ? prefab.GetComponent<MeshFilter>() : null;
            MeshRenderer renderer = prefab != null ? prefab.GetComponent<MeshRenderer>() : null;
            if (filter == null || renderer == null) continue;

            layers.Add(new Layer
            {
                mesh = filter.sharedMesh,
                material = renderer.sharedMaterial,
                density = data.GetDetailLayer(0, 0, data.detailWidth, data.detailHeight, i),
                minWidth = prototypes[i].minWidth, maxWidth = prototypes[i].maxWidth,
                minHeight = prototypes[i].minHeight, maxHeight = prototypes[i].maxHeight,
            });
        }
        // 지형 기본 디테일 렌더링은 끈다(이 컴포넌트가 대신 그린다).
        terrain.detailObjectDistance = 0f;
    }

    private void Update()
    {
        if (layers.Count == 0) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 origin = terrain.transform.position;
        float chunkSize = chunkCells * cellSize;
        Vector3 local = camera.transform.position - origin;
        int centerX = Mathf.FloorToInt(local.x / chunkSize);
        int centerZ = Mathf.FloorToInt(local.z / chunkSize);
        int radius = Mathf.CeilToInt(drawDistance / chunkSize);
        int chunkCount = Mathf.CeilToInt(detailResolution / (float)chunkCells);

        int built = 0;
        for (int dz = -radius; dz <= radius; dz++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int cx = centerX + dx, cz = centerZ + dz;
                if (cx < 0 || cz < 0 || cx >= chunkCount || cz >= chunkCount) continue;

                // 덩어리 중심이 그리는 거리 안인지(조금 여유를 두고) 본다.
                Vector2 center = new Vector2((cx + 0.5f) * chunkSize, (cz + 0.5f) * chunkSize);
                if (Vector2.Distance(center, new Vector2(local.x, local.z)) > drawDistance + chunkSize * 0.7f) continue;

                long key = ((long)cx << 32) | (uint)cz;
                if (!chunks.TryGetValue(key, out Chunk chunk))
                {
                    if (built >= chunksPerFrame) continue;
                    chunk = BuildChunk(cx, cz);
                    chunks[key] = chunk;
                    built++;
                }
                Draw(chunk);
            }

        if (chunks.Count > 600) chunks.Clear();
    }

    private void Draw(Chunk chunk)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            for (int b = 0; b < chunk.batches[i].Count; b++)
                Graphics.DrawMeshInstanced(layers[i].mesh, 0, layers[i].material, chunk.batches[i][b], chunk.counts[i][b], null, ShadowCastingMode.Off, false);
        }
    }

    // 덩어리 안의 모든 칸에서 칸의 풀 수만큼 다발을 흩뿌린다(같은 칸은 항상 같은 모양이 되게 해시로 위치를 정한다).
    private Chunk BuildChunk(int cx, int cz)
    {
        Chunk chunk = new Chunk(layers.Count);
        Vector3 origin = terrain.transform.position;
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            Matrix4x4[] batch = new Matrix4x4[MaxInstancesPerBatch];
            int count = 0;
            for (int dz = 0; dz < chunkCells; dz++)
                for (int dx = 0; dx < chunkCells; dx++)
                {
                    int cellX = cx * chunkCells + dx, cellZ = cz * chunkCells + dz;
                    if (cellX >= detailResolution || cellZ >= detailResolution) continue;

                    int n = layer.density[cellZ, cellX];
                    for (int k = 0; k < n; k++)
                    {
                        uint h = Hash(cellX, cellZ, k, i);
                        float rx = (h & 1023u) / 1024f;
                        float rz = ((h >> 10) & 1023u) / 1024f;
                        float rs = ((h >> 20) & 255u) / 255f;
                        float rh = ((h >> 28) & 15u) / 15f;
                        // 비율에 못 든 다발은 건너뛴다(같은 칸은 항상 같은 다발이 남는다).
                        if (((h >> 3) & 1023u) / 1024f >= densityScale) continue;
                        Vector3 position = origin + new Vector3((cellX + rx) * cellSize, 0f, (cellZ + rz) * cellSize);
                        // 연못/웅덩이 같은 비워진 자리에는 풀을 심지 않는다.
                        if (GrassClearing.Contains(position)) continue;
                        position.y = terrain.SampleHeight(position) + origin.y;
                        float width = Mathf.Lerp(layer.minWidth, layer.maxWidth, rs);
                        float height = Mathf.Lerp(layer.minHeight, layer.maxHeight, Mathf.Clamp01(rs * 0.6f + rh * 0.4f));
                        batch[count++] = Matrix4x4.TRS(position, Quaternion.Euler(0f, h % 360u, 0f), new Vector3(width, height, width));

                        if (count < MaxInstancesPerBatch) continue;
                        chunk.batches[i].Add(batch);
                        chunk.counts[i].Add(count);
                        batch = new Matrix4x4[MaxInstancesPerBatch];
                        count = 0;
                    }
                }
            if (count > 0)
            {
                chunk.batches[i].Add(batch);
                chunk.counts[i].Add(count);
            }
        }
        return chunk;
    }

    private static uint Hash(int x, int z, int k, int layer)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)z * 668265263u + (uint)k * 2246822519u + (uint)layer * 3266489917u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }
}
