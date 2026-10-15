// 지형에 심은 모든 나무를 채집 가능하게 만드는 컴포넌트다. 나무마다 오브젝트를 만들지 않고, 플레이어 둘레의 가까운 나무 몇 그루에만 보이지 않는 ResourceNode 대역을 붙여 기존 채집 흐름(InteractionFocus, ResourceGatherer)을 그대로 쓴다.
// 다 채집한 나무는 지형에서 숨기고(크기 0) 재생 시간이 지나면 되살린다. 종료할 때 숨긴 나무를 모두 원래대로 돌려 지형 에셋이 바뀐 채 남지 않게 한다. 시작할 때 스스로 만든다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TerrainTreeGather : MonoBehaviour
{
    // 채집용 정의(수확량, 재생 시간, 소리)다. 맵 빌더가 씬에 둔 오브젝트에 연결해 두고, 없으면 Resources에서 찾는다.
    [SerializeField] private ResourceNodeDefinition definition;
    private const string DefinitionPath = "ResourceNodeDef_Tree";
    // 대역을 붙일 나무를 찾는 반경(m)과 동시에 붙일 수 있는 대역 수다. 반경은 채집 범위에 줄기 굵기를 더한 정도다.
    private const float SearchRadius = 4.5f;
    private const int ProxyCount = 5;
    private const float RefreshSeconds = 0.2f;
    private const float CellSize = 8f;
    private const float TrunkRadius = 0.4f;
    private const float TrunkHeight = 3.2f;

    private sealed class Proxy
    {
        public GameObject gameObject;
        public ResourceNode node;
        public int treeIndex = -1;
    }

    private Terrain terrain;
    private TerrainData data;
    private TreeInstance[] originals;
    private Vector3[] positions;
    private readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();

    // 숨긴 나무의 재생 시각과, 일부만 채집한 나무의 남은 수량이다.
    private readonly Dictionary<int, float> respawnAt = new Dictionary<int, float>();
    private readonly Dictionary<int, int> remaining = new Dictionary<int, int>();

    private readonly List<Proxy> proxies = new List<Proxy>();
    private readonly List<int> nearby = new List<int>();
    private Transform player;
    private float nextRefresh;

    // 멀티플레이: 서버가 참가자의 채집 요청을 처리하고, 나무가 사라질 때 모든 접속자에게 알릴 수 있게 한다.
    public static TerrainTreeGather Instance { get; private set; }
    public static event System.Action<int> TreeDepleted;
    public IReadOnlyCollection<int> DepletedTrees => respawnAt.Keys;
    private const float ServerGatherReach = 7f;

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
        if (FindFirstObjectByType<TerrainTreeGather>() != null) return;
        new GameObject("TerrainTreeGather").AddComponent<TerrainTreeGather>();
    }

    private void Start()
    {
        Instance = this;
        terrain = Terrain.activeTerrain;
        if (definition == null) definition = Resources.Load<ResourceNodeDefinition>(DefinitionPath);
        if (terrain == null || terrain.terrainData == null || definition == null)
        {
            if (definition == null) Debug.LogWarning("TerrainTreeGather: 나무 채집 정의가 없다. 맵 빌더(4 Bounds And Build Settings)로 씬에 연결한다.");
            enabled = false;
            return;
        }

        data = terrain.terrainData;
        originals = data.treeInstances;
        positions = new Vector3[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            positions[i] = Vector3.Scale(originals[i].position, data.size) + terrain.transform.position;
            long key = CellKey(positions[i]);
            if (!grid.TryGetValue(key, out List<int> cell)) grid[key] = cell = new List<int>();
            cell.Add(i);
        }

        for (int i = 0; i < ProxyCount; i++) proxies.Add(CreateProxy(i));
    }

    // 종료할 때 숨긴 나무를 원래대로 되돌린다(에디터에서 지형 에셋이 바뀐 채 남지 않게).
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (data == null || originals == null) return;
        foreach (int index in respawnAt.Keys) data.SetTreeInstance(index, originals[index]);
        respawnAt.Clear();
    }

    private Proxy CreateProxy(int number)
    {
        var go = new GameObject("TreeProxy_" + number);
        go.transform.SetParent(transform, false);
        // 트리거라 걸리지 않으면서 채집 범위 판정(콜라이더의 가장 가까운 점)에는 쓰인다.
        CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.radius = TrunkRadius;
        capsule.height = TrunkHeight;
        capsule.center = new Vector3(0f, TrunkHeight * 0.5f, 0f);
        ResourceNode node = go.AddComponent<ResourceNode>();
        go.SetActive(false);

        var proxy = new Proxy { gameObject = go, node = node };
        node.Gathered += _ => OnGathered(proxy);
        node.Depleted += () => OnDepleted(proxy);
        return proxy;
    }

    private void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + RefreshSeconds;

        RestoreRespawned();
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (player == null) return;

        FindNearby();
        Assign();
    }

    // 재생 시간이 지난 나무를 지형에 되살린다.
    private void RestoreRespawned()
    {
        if (respawnAt.Count == 0) return;

        List<int> due = null;
        foreach (KeyValuePair<int, float> pair in respawnAt)
        {
            if (Time.time < pair.Value) continue;
            if (due == null) due = new List<int>();
            due.Add(pair.Key);
        }
        if (due == null) return;

        foreach (int index in due)
        {
            data.SetTreeInstance(index, originals[index]);
            respawnAt.Remove(index);
        }
    }

    // 플레이어 둘레 반경 안에서 숨겨지지 않은 나무를 가까운 순으로 모은다.
    private void FindNearby()
    {
        nearby.Clear();
        Vector3 center = player.position;
        int cx = Mathf.FloorToInt(center.x / CellSize), cz = Mathf.FloorToInt(center.z / CellSize);
        float sqr = SearchRadius * SearchRadius;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (!grid.TryGetValue(Key(cx + dx, cz + dz), out List<int> cell)) continue;
                foreach (int index in cell)
                {
                    if (respawnAt.ContainsKey(index)) continue;
                    Vector3 d = positions[index] - center;
                    d.y = 0f;
                    if (d.sqrMagnitude <= sqr) nearby.Add(index);
                }
            }
        nearby.Sort((a, b) => FlatSqr(positions[a], center).CompareTo(FlatSqr(positions[b], center)));
    }

    // 가까운 나무에 대역을 붙인다. 이미 붙어 있는 나무는 그대로 두고, 멀어진 나무의 대역을 거두어 새 나무에 쓴다.
    private void Assign()
    {
        int wanted = Mathf.Min(nearby.Count, proxies.Count);
        foreach (Proxy proxy in proxies)
        {
            bool keep = proxy.treeIndex >= 0 && nearby.IndexOf(proxy.treeIndex) >= 0 && nearby.IndexOf(proxy.treeIndex) < wanted && proxy.node.HasYield;
            if (keep) continue;
            Release(proxy);
        }

        for (int i = 0; i < wanted; i++)
        {
            int tree = nearby[i];
            if (IsAssigned(tree)) continue;
            Proxy free = proxies.Find(p => p.treeIndex < 0);
            if (free == null) break;
            Attach(free, tree);
        }
    }

    private bool IsAssigned(int tree)
    {
        foreach (Proxy proxy in proxies) if (proxy.treeIndex == tree) return true;
        return false;
    }

    private void Attach(Proxy proxy, int tree)
    {
        proxy.treeIndex = tree;
        proxy.node.TerrainTreeIndex = tree;
        proxy.gameObject.transform.position = positions[tree];
        proxy.gameObject.SetActive(true);
        proxy.node.InitializeRuntime(definition, remaining.TryGetValue(tree, out int left) ? left : definition.MaxYield);
    }

    private static void Release(Proxy proxy)
    {
        proxy.treeIndex = -1;
        proxy.node.TerrainTreeIndex = -1;
        proxy.gameObject.SetActive(false);
    }

    // 일부만 채집한 나무의 남은 수량을 기억해 두었다가, 다시 가까이 가면 이어서 채집하게 한다.
    private void OnGathered(Proxy proxy)
    {
        if (proxy.treeIndex < 0) return;
        if (proxy.node.RemainingYield > 0) remaining[proxy.treeIndex] = proxy.node.RemainingYield;
    }

    // 마지막 수량까지 채집하면 지형에서 나무를 숨기고 재생 시각을 정한다. 대역은 다음 갱신에 거둔다.
    private void OnDepleted(Proxy proxy)
    {
        int tree = proxy.treeIndex;
        if (tree < 0) return;

        HideTree(tree);
        TreeDepleted?.Invoke(tree);
    }

    private void HideTree(int tree)
    {
        TreeInstance hidden = originals[tree];
        hidden.widthScale = 0f;
        hidden.heightScale = 0f;
        data.SetTreeInstance(tree, hidden);
        respawnAt[tree] = Time.time + definition.RespawnSeconds;
        remaining.Remove(tree);
    }

    // 서버: 참가자가 요청한 나무를 채집한다. 아직 서 있고 채집자가 가까울 때만 인벤토리에 수확물을 주며, 다 채집되면 모두에게 알린다.
    public bool ServerGatherTree(int tree, Vector3 gathererPosition, Inventory inventory, out ItemDefinition item, out int gained)
    {
        item = definition != null ? definition.YieldItem : null;
        gained = 0;
        if (data == null || definition == null || item == null || inventory == null) return false;
        if (tree < 0 || tree >= positions.Length || respawnAt.ContainsKey(tree)) return false;
        if (FlatSqr(positions[tree], gathererPosition) > ServerGatherReach * ServerGatherReach) return false;

        int left = remaining.TryGetValue(tree, out int stored) ? stored : definition.MaxYield;
        gained = Mathf.Min(definition.YieldPerGather, left);
        inventory.AddPickup(item, gained);
        left -= gained;

        if (left > 0)
        {
            remaining[tree] = left;
            return true;
        }

        HideTree(tree);
        TreeDepleted?.Invoke(tree);
        return true;
    }

    // 참가자: 서버가 알린 사라진 나무를 지형에서 숨긴다(재생은 이 컴퓨터의 타이머로 한다). 그 나무에 붙은 대역은 거둔다.
    public void ApplyRemoteTreeDepleted(int tree)
    {
        if (data == null || definition == null || tree < 0 || tree >= positions.Length || respawnAt.ContainsKey(tree)) return;

        HideTree(tree);
        foreach (Proxy proxy in proxies) if (proxy.treeIndex == tree) Release(proxy);
    }

    private static float FlatSqr(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
    private static long CellKey(Vector3 position) => Key(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
}
