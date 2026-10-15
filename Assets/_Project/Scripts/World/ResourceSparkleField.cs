// 플레이어 근처의 채집 가능한 자원 노드(보급 상자, 물통, 열매 덤불, 연못, 웅덩이)에 은은한 반짝임을 그리는 표현 컴포넌트다. 노드마다 파티클을 달지 않고 근처 노드를 모아 메시 하나로 그린다. 시작할 때 스스로 만든다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class ResourceSparkleField : MonoBehaviour
{
    // 이 이름이 들어간 노드만 반짝인다(나무 같은 흔한 노드는 제외한다). 색조는 종류별이다.
    private struct Kind
    {
        public string key;
        public Color tint;
    }

    private static readonly Kind[] Kinds =
    {
        new Kind { key = "SupplyCrate", tint = new Color(1f, 0.82f, 0.35f) },
        new Kind { key = "WaterBarrel", tint = new Color(0.45f, 0.9f, 1f) },
        new Kind { key = "Pond", tint = new Color(0.55f, 0.95f, 1f) },
        new Kind { key = "DirtyPuddle", tint = new Color(0.6f, 0.85f, 0.8f) },
        new Kind { key = "BerryBush", tint = new Color(1f, 0.5f, 0.75f) },
        new Kind { key = "ScrapPile", tint = new Color(0.75f, 0.85f, 1f) },
    };

    private const string MaterialPath = "ResourceSparkle";
    private const float ShowDistance = 14f;
    private const float RefreshSeconds = 0.4f;
    private const int GlintsPerNode = 1;
    // 메시 하나에 담을 수 있는 최대 반짝임 수다(꼭짓점 4개씩, 16비트 인덱스 한도 안).
    private const int MaxGlints = 4000;

    private Material material;
    private Mesh mesh;
    private float nextRefresh;
    private Transform player;

    private readonly Dictionary<ResourceNode, Vector3[]> anchors = new Dictionary<ResourceNode, Vector3[]>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> corners = new List<Vector2>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> indices = new List<int>();

    // 첫 씬이 메인 메뉴여도 게임 씬이 열릴 때마다 만들어지게 씬 로드마다 다시 부른다(메뉴 씬에서는 만들지 않는다).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Create();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Create();

    private static void Create()
    {
        if (GameSession.InMenuScene) return;
        if (FindFirstObjectByType<ResourceSparkleField>() != null) return;
        new GameObject("ResourceSparkleField").AddComponent<ResourceSparkleField>();
    }

    private void Awake()
    {
        material = Resources.Load<Material>(MaterialPath);
        mesh = new Mesh { name = "ResourceSparkle", indexFormat = IndexFormat.UInt32 };
        mesh.MarkDynamic();
        // 월드 좌표로 담으므로 컬링이 잘못 걸러 내지 않게 아주 큰 경계를 준다.
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    private void LateUpdate()
    {
        if (material == null) return;

        // 그래픽 품질이 낮음이면 반짝임을 그리지 않는다.
        if (GraphicsPresets.ShaderCut >= 2)
        {
            if (indices.Count > 0) { indices.Clear(); mesh.Clear(); }
            return;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshSeconds;
            Rebuild();
        }
        if (indices.Count > 0)
            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, null, 0, null, ShadowCastingMode.Off, false);
    }

    private void Rebuild()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        vertices.Clear();
        corners.Clear();
        colors.Clear();
        indices.Clear();
        if (player == null) { mesh.Clear(); return; }

        float sqrShow = ShowDistance * ShowDistance;
        IReadOnlyList<ResourceNode> nodes = ResourceNode.Active;
        for (int i = 0; i < nodes.Count && vertices.Count / 4 < MaxGlints - GlintsPerNode; i++)
        {
            ResourceNode node = nodes[i];
            if (node == null || !node.HasYield) continue;
            if ((node.transform.position - player.position).sqrMagnitude > sqrShow) continue;
            if (!TryKind(node, out Color tint)) continue;

            Vector3[] points = AnchorsOf(node);
            for (int g = 0; g < points.Length; g++)
                AddGlint(points[g], tint, Phase(node, g));
        }

        mesh.Clear();
        if (indices.Count == 0) return;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, corners);
        mesh.SetColors(colors);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
    }

    private static bool TryKind(ResourceNode node, out Color tint)
    {
        string name = node.name;
        foreach (Kind kind in Kinds)
        {
            if (!name.Contains(kind.key)) continue;
            tint = kind.tint;
            return true;
        }
        tint = default;
        return false;
    }

    // 노드마다 반짝임 자리를 한 번만 구한다(렌더러 경계 윗면 주변의 무작위 점, 노드마다 고정).
    private Vector3[] AnchorsOf(ResourceNode node)
    {
        if (anchors.TryGetValue(node, out Vector3[] cached)) return cached;

        Bounds bounds = new Bounds(node.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        var random = new System.Random(node.GetInstanceID());
        var points = new Vector3[GlintsPerNode];
        for (int i = 0; i < points.Length; i++)
        {
            float x = Mathf.Lerp(-0.45f, 0.45f, (float)random.NextDouble()) * bounds.size.x;
            float z = Mathf.Lerp(-0.45f, 0.45f, (float)random.NextDouble()) * bounds.size.z;
            float y = Mathf.Lerp(0.55f, 1.05f, (float)random.NextDouble()) * bounds.size.y + 0.1f;
            points[i] = new Vector3(bounds.center.x + x, bounds.min.y + y, bounds.center.z + z);
        }
        anchors[node] = points;
        return points;
    }

    private static float Phase(ResourceNode node, int glint)
    {
        return Mathf.Repeat(node.GetInstanceID() * 0.6180339f + glint * 0.37f, 1f);
    }

    private void AddGlint(Vector3 center, Color tint, float phase)
    {
        int start = vertices.Count;
        Color color = new Color(tint.r, tint.g, tint.b, phase);
        for (int c = 0; c < 4; c++)
        {
            vertices.Add(center);
            colors.Add(color);
        }
        corners.Add(new Vector2(-1f, -1f));
        corners.Add(new Vector2(1f, -1f));
        corners.Add(new Vector2(1f, 1f));
        corners.Add(new Vector2(-1f, 1f));
        indices.Add(start); indices.Add(start + 2); indices.Add(start + 1);
        indices.Add(start); indices.Add(start + 3); indices.Add(start + 2);
    }
}
