// 설치된 건축물의 최소 영속 식별 정보다. StableWorldId는 이후 저장/네트워크 동기화에 쓴다.
using UnityEngine;

public class BuildableInstance : MonoBehaviour
{
    [SerializeField] private string buildableId;
    [SerializeField] private string worldId;
    // 이 건축물이 붙어 있는 건축물의 식별자다(벽에 붙은 가구/사다리). 비어 있으면 땅이나 아래층이 받친다.
    [SerializeField] private string supportId;

    // 죽은 자리에 남는 시체 가방의 식별자다. 건축물과 같은 구성요소를 쓰지만 거점으로 세지 않는다.
    public const string CorpseBagId = "corpse_bag";

    // 실제로 설치되거나 불러온 건축물 목록이다(미리보기는 들어오지 않는다). 부서져 사라진 것은 센 때 걸러 낸다.
    private static readonly System.Collections.Generic.List<BuildableInstance> placed = new System.Collections.Generic.List<BuildableInstance>();

    // 거점으로 칠 건축물(시체 가방 제외)이 하나라도 남아 있는지다.
    public static bool AnyBasePiece
    {
        get
        {
            for (int i = placed.Count - 1; i >= 0; i--)
            {
                BuildableInstance piece = placed[i];
                if (piece == null || !piece.gameObject.activeInHierarchy) { placed.RemoveAt(i); continue; }
                if (piece.buildableId != CorpseBagId) return true;
            }
            return false;
        }
    }

    // 서버가 건축물 생성/삭제를 접속자에게 알리는 데 쓴다. 미리보기(고스트)는 월드 id가 없어 알리지 않는다.
    public static event System.Action<BuildableInstance> Added;
    public static event System.Action<string> Removed;
    public static System.Collections.Generic.IReadOnlyList<BuildableInstance> All => placed;

    // 월드 id로 설치된(활성) 건축물을 찾는다.
    public static BuildableInstance FindByWorldId(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (BuildableInstance piece in placed)
        {
            if (piece != null && piece.worldId == id && piece.gameObject.activeInHierarchy) return piece;
        }
        return null;
    }

    public string BuildableId => buildableId;
    public string WorldId => worldId;
    public string SupportId => supportId;

    public void SetSupport(string supporterWorldId) => supportId = supporterWorldId;

    public void Initialize(string newBuildableId)
    {
        buildableId = newBuildableId;
        worldId = System.Guid.NewGuid().ToString("N");
        Register();
        AttachNavBlocker();
    }

    private void Register()
    {
        if (placed.Contains(this)) return;
        placed.Add(this);
        Added?.Invoke(this);
    }

    private void OnDestroy()
    {
        placed.Remove(this);
        if (!string.IsNullOrEmpty(worldId)) Removed?.Invoke(worldId);
    }

    // 철거/내구도 판정이 쓰는 건축물 콜라이더 월드 경계(트리거 제외)다.
    public static bool TryGetColliderBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger) continue;
            if (!any) { bounds = collider.bounds; any = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        return any;
    }

    // 저장된 내구도를 되돌린다(내구도가 없는 건축물이면 무시한다).
    public void RestoreHealth(float current)
    {
        BuildableHealth health = GetComponent<BuildableHealth>();
        BuildableDefinition definition = BuildableDatabase.Instance != null ? BuildableDatabase.Instance.GetById(buildableId) : null;
        if (health != null && definition != null) health.Initialize(definition, current);
    }

    public void Restore(string existingBuildableId, string existingWorldId)
    {
        buildableId = existingBuildableId;
        worldId = existingWorldId;
        Register();
        AttachNavBlocker();
    }

    // 길을 막는 분류(벽, 창문, 문, 방어)는 좀비 길찾기에 장애물로 등록하고, 내구도가 있으면 부서질 수 있게 한다. 미리보기(고스트)는 이 경로를 거치지 않는다.
    private void AttachNavBlocker()
    {
        BuildableDatabase database = BuildableDatabase.Instance;
        BuildableDefinition definition = database != null ? database.GetById(buildableId) : null;
        if (definition == null) return;

        if (BuildableNavBlocker.Blocks(definition.Category))
        {
            BuildableNavBlocker blocker = GetComponent<BuildableNavBlocker>();
            if (blocker == null) blocker = gameObject.AddComponent<BuildableNavBlocker>();
            blocker.Build();
        }

        if (definition.MaxHealth > 0f)
        {
            BuildableHealth health = GetComponent<BuildableHealth>();
            if (health == null) health = gameObject.AddComponent<BuildableHealth>();
            health.Initialize(definition, -1f);
            if (GetComponent<BuildableDamageVisual>() == null) gameObject.AddComponent<BuildableDamageVisual>();
        }
    }
}
