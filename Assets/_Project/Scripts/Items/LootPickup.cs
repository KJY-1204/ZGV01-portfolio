// 월드에 스폰된 전리품이다. 아이템 모델(있으면)이나 아이콘 빌보드로 보이고, 플레이어와 접촉하면 인벤토리에 추가되고 스스로 제거된다.
// 멀티플레이에서는 서버가 만든 네트워크 오브젝트라 모든 접속자에게 보이고, 줍기는 서버만 판정한다(호스트 플레이어는 접촉, 참가자는 아바타 근접).
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LootPickup : NetworkBehaviour
{
    // 참가자 아바타가 이 거리(수평) 안에 들어오면 줍는다.
    private const float AvatarPickupRadius = 1.3f;

    private readonly NetworkVariable<FixedString64Bytes> networkItemId = new NetworkVariable<FixedString64Bytes>();
    private readonly NetworkVariable<int> networkQuantity = new NetworkVariable<int>();

    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity = 1;
    // 아이콘/모델의 큰 쪽 길이(m)와 지면에서 떠 있는 높이(m)다.
    [SerializeField] private float iconSize = 0.7f;
    [SerializeField] private float modelSize = 0.9f;
    [SerializeField] private float hoverHeight = 0.5f;
    [SerializeField] private float bobAmount = 0.06f;
    [SerializeField] private float modelSpinDegreesPerSecond = 40f;

    private Transform visual;
    private bool isModel;
    private float phase;
    // 이 시각 전에는 줍지 못한다(플레이어가 버린 아이템을 바로 다시 줍지 않게 한다).
    private float pickupAvailableAt;

    public ItemDefinition Item => item;

    // pickupDelaySeconds 동안은 플레이어가 닿아도 줍지 못한다(기본 0이면 바로 줍는다).
    public void Initialize(ItemDefinition definition, int amount, float pickupDelaySeconds = 0f)
    {
        item = definition;
        quantity = amount;
        pickupAvailableAt = pickupDelaySeconds > 0f ? Time.time + pickupDelaySeconds : 0f;
        BuildVisual();
    }

    // 전리품을 만든다. 멀티플레이 호스트면 네트워크 오브젝트로 소환해 참가자에게도 보인다. 호스트가 아니면 지금처럼 로컬 오브젝트다.
    public static LootPickup Spawn(GameObject prefab, Vector3 position, ItemDefinition item, int quantity, float pickupDelaySeconds = 0f)
    {
        if (prefab == null) return null;

        GameObject loot = Instantiate(prefab, position, Quaternion.identity);
        LootPickup pickup = loot.GetComponent<LootPickup>();
        if (pickup == null) return null;

        pickup.Initialize(item, quantity, pickupDelaySeconds);
        NetworkObject networkObject = loot.GetComponent<NetworkObject>();
        if (networkObject != null && MultiplayerSession.IsHost) networkObject.Spawn(true);
        return pickup;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkItemId.Value = item != null ? item.StableId : "";
            networkQuantity.Value = quantity;
            return;
        }

        // 참가자 쪽: 서버가 정한 아이템으로 모습을 만들고, 줍기는 서버가 하므로 접촉 판정은 끈다.
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        ApplyReplicatedItem();
    }

    private void ApplyReplicatedItem()
    {
        string id = networkItemId.Value.ToString();
        item = ItemDatabase.Instance != null && id.Length > 0 ? ItemDatabase.Instance.GetByStableId(id) : null;
        quantity = networkQuantity.Value;
        BuildVisual();
    }

    private void Start()
    {
        phase = Random.value * Mathf.PI * 2f;
        if (visual == null) BuildVisual();
    }

    private void Update()
    {
        if (IsSpawned && IsServer) PickupByAvatars();
        if (visual == null) return;

        Vector3 position = visual.localPosition;
        position.y = hoverHeight + Mathf.Sin(Time.time * 2f + phase) * bobAmount;
        visual.localPosition = position;

        if (isModel)
        {
            visual.Rotate(0f, modelSpinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
        else if (Camera.main != null)
        {
            // 아이콘은 카메라 방향으로 세워(수직 축만 돌려) 어느 쪽에서도 읽힌다.
            visual.rotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
        }
    }

    // 아이템의 월드 모델이 있으면 모델, 없으면 아이콘을 세워 보여 준다. 프리팹의 기본 도형(구체)은 쓰지 않는다.
    private void BuildVisual()
    {
        MeshRenderer placeholder = GetComponent<MeshRenderer>();
        if (placeholder != null) placeholder.enabled = false;
        if (visual != null) Destroy(visual.gameObject);
        visual = null;
        if (item == null) return;

        if (item.WorldPrefab != null)
        {
            visual = BuildModel(item.WorldPrefab);
            isModel = true;
        }
        else if (item.Icon != null)
        {
            visual = BuildIcon(item.Icon);
            isModel = false;
        }
        if (visual != null) visual.localPosition = Vector3.up * hoverHeight;
    }

    private Transform BuildIcon(Sprite icon)
    {
        GameObject go = new GameObject("Icon");
        go.transform.SetParent(transform, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = icon;
        float width = Mathf.Max(0.0001f, icon.bounds.size.x);
        go.transform.localScale = Vector3.one * (iconSize / width);
        return go.transform;
    }

    private Transform BuildModel(GameObject prefab)
    {
        GameObject holder = new GameObject("Model");
        holder.transform.SetParent(transform, false);
        GameObject model = Instantiate(prefab, holder.transform);
        // 월드 모델은 보이기만 한다(충돌/스크립트/파티클은 쓰지 않는다).
        foreach (Collider collider in model.GetComponentsInChildren<Collider>()) Destroy(collider);
        foreach (ParticleSystem system in model.GetComponentsInChildren<ParticleSystem>()) system.gameObject.SetActive(false);

        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (any)
        {
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.0001f);
            holder.transform.localScale = Vector3.one * (modelSize / largest);
            model.transform.position += holder.transform.position - bounds.center;
        }
        return holder.transform;
    }

    private void OnTriggerEnter(Collider other) => TryPickup(other);

    // 지연 시간이 끝났을 때 이미 겹쳐 있어도 줍도록 머무는 동안에도 확인한다.
    private void OnTriggerStay(Collider other) => TryPickup(other);

    private void TryPickup(Collider other)
    {
        if (Time.time < pickupAvailableAt || !other.CompareTag("Player")) return;
        // 참가자 쪽 오브젝트는 서버가 줍기를 판정한다.
        if (IsSpawned && !IsServer) return;

        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return;

        inventory.AddPickup(item, quantity);
        Consume();
    }

    // 서버: 참가자 아바타가 가까이 오면 그 사람의 서버 인벤토리에 넣고 사라진다(호스트 플레이어는 접촉으로 줍는다).
    private void PickupByAvatars()
    {
        if (item == null || Time.time < pickupAvailableAt) return;

        NetworkManager manager = NetworkManager.Singleton;
        foreach (System.Collections.Generic.KeyValuePair<ulong, NetworkClient> pair in manager.ConnectedClients)
        {
            if (pair.Key == manager.LocalClientId || pair.Value.PlayerObject == null) continue;

            Vector3 delta = pair.Value.PlayerObject.transform.position - transform.position;
            if (Mathf.Abs(delta.y) > 3f || new Vector2(delta.x, delta.z).sqrMagnitude > AvatarPickupRadius * AvatarPickupRadius) continue;

            NetworkInventory inventory = pair.Value.PlayerObject.GetComponent<NetworkInventory>();
            NetworkCombat combat = pair.Value.PlayerObject.GetComponent<NetworkCombat>();
            if (inventory == null || (combat != null && combat.IsDead)) continue;
            if (!inventory.ServerGrant(item, quantity)) continue;

            inventory.NotifyPickup(item, quantity);
            Consume();
            return;
        }
    }

    // 주워져 사라진다. 네트워크 오브젝트면 서버가 소환 해제(모든 접속자에게서 사라짐)하고 아니면 그냥 지운다.
    private void Consume()
    {
        if (IsSpawned && IsServer) NetworkObject.Despawn(true);
        else if (!IsSpawned) Destroy(gameObject);
    }
}
