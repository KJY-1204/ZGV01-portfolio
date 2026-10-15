// 건축물의 서버 권위 동기화 컴포넌트다. 설치, 철거, 수리, 문 여닫기는 참가자가 서버에 요청하고 서버가 검증해 확정하며, 생기고 사라지고 체력과 문 상태가 바뀐 것을 모든 접속자에게 알린다. 늦게 들어온 접속자는 지금 있는 건축물을 한 번에 받는다.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BuildSync : NetworkBehaviour
{
    // 서버 월드에서 만든 새 건축물이 설치를 마친 뒤(받침 등이 정해진 뒤) 알리도록 모아 두는 목록이다.
    private readonly List<BuildableInstance> pending = new List<BuildableInstance>();
    // 참가자별 수리 요청 간격 제한(초)이다. 정상 수리 간격보다 느슨하게 둔다.
    private const float RepairMinInterval = 0.4f;
    private readonly Dictionary<ulong, float> nextRepair = new Dictionary<ulong, float>();

    public static BuildSync Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer)
        {
            BuildableInstance.Added += OnAdded;
            BuildableInstance.Removed += OnRemoved;
            BuildableHealth.Modified += OnHealthModified;
            BuildableDoor.StateChanged += OnDoorChanged;
        }
        else
        {
            RequestStateRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
        BuildableInstance.Added -= OnAdded;
        BuildableInstance.Removed -= OnRemoved;
        BuildableHealth.Modified -= OnHealthModified;
        BuildableDoor.StateChanged -= OnDoorChanged;
        pending.Clear();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsSpawned && !IsServer) RunClientDebug();
#endif
        if (!IsServer || !IsSpawned || pending.Count == 0) return;

        foreach (BuildableInstance piece in pending)
        {
            if (piece == null || !piece.gameObject.activeInHierarchy || !IsReplicated(piece)) continue;
            PlacedRpc(piece.BuildableId, piece.WorldId, piece.transform.position, piece.transform.eulerAngles.y, piece.SupportId, HealthOf(piece), DoorOpen(piece), DoorSide(piece));
        }
        pending.Clear();
    }

    // 월드 id가 있는 설치된 건축물(보관함과 시체 가방 포함)만 알린다. 미리보기(고스트)는 월드 id가 없다.
    private static bool IsReplicated(BuildableInstance piece)
    {
        return !string.IsNullOrEmpty(piece.WorldId);
    }

    private static float HealthOf(BuildableInstance piece)
    {
        BuildableHealth health = piece.GetComponent<BuildableHealth>();
        return health != null ? health.Current : -1f;
    }

    private static bool DoorOpen(BuildableInstance piece)
    {
        BuildableDoor door = piece.GetComponent<BuildableDoor>();
        return door != null && door.IsOpen;
    }

    private static float DoorSide(BuildableInstance piece)
    {
        BuildableDoor door = piece.GetComponent<BuildableDoor>();
        return door != null ? door.Side : 1f;
    }

    // ---- 서버 → 참가자: 실시간 변화 ----

    private void OnAdded(BuildableInstance piece)
    {
        if (IsSpawned && !pending.Contains(piece)) pending.Add(piece);
    }

    private void OnRemoved(string worldId)
    {
        if (IsSpawned) RemovedRpc(worldId);
    }

    private void OnHealthModified(BuildableHealth health)
    {
        BuildableInstance piece = health != null ? health.GetComponent<BuildableInstance>() : null;
        if (IsSpawned && piece != null && IsReplicated(piece)) HealthRpc(piece.WorldId, health.Current);
    }

    private void OnDoorChanged(BuildableDoor door)
    {
        BuildableInstance piece = door != null ? door.GetComponent<BuildableInstance>() : null;
        if (IsSpawned && piece != null && IsReplicated(piece)) DoorRpc(piece.WorldId, door.IsOpen, door.Side);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void PlacedRpc(string buildableId, string worldId, Vector3 position, float yaw, string supportId, float health, bool doorOpen, float doorSide)
    {
        SpawnReplica(buildableId, worldId, position, yaw, supportId, health, doorOpen, doorSide);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void RemovedRpc(string worldId)
    {
        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        if (piece == null) return;
        // 시체 가방은 비거나 수명이 다하면 서버에서 조용히 사라지므로 복제본도 파편 없이 지운다.
        if (piece.BuildableId == BuildableInstance.CorpseBagId)
        {
            Destroy(piece.gameObject);
            return;
        }
        // 서버가 무너뜨리는 때에 맞춰 파편과 소리를 낸다.
        BuildableDebris.Crumble(piece, 0f);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void HealthRpc(string worldId, float current)
    {
        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        BuildableHealth health = piece != null ? piece.GetComponent<BuildableHealth>() : null;
        if (health != null) health.ApplyReplicated(current);
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    private void DoorRpc(string worldId, bool open, float side)
    {
        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        BuildableDoor door = piece != null ? piece.GetComponent<BuildableDoor>() : null;
        if (door != null) door.ApplyReplicated(open, side);
    }

    // 서버가 알린 건축물을 참가자 월드에 복제본으로 세운다(저장에서 불러오는 경로와 같은 방식이고, 소리와 자재 차감은 없다).
    private static void SpawnReplica(string buildableId, string worldId, Vector3 position, float yaw, string supportId, float health, bool doorOpen, float doorSide)
    {
        if (BuildableInstance.FindByWorldId(worldId) != null) return;

        BuildableDefinition definition = BuildableDatabase.Instance != null ? BuildableDatabase.Instance.GetById(buildableId) : null;
        if (definition == null || definition.Prefab == null) return;

        GameObject spawned = Instantiate(definition.Prefab, position, Quaternion.Euler(0f, yaw, 0f));
        BuildableInstance instance = spawned.GetComponent<BuildableInstance>();
        if (instance == null) instance = spawned.AddComponent<BuildableInstance>();
        instance.Restore(buildableId, worldId);
        instance.SetSupport(supportId);

        BuildableDoor door = spawned.GetComponent<BuildableDoor>();
        if (door != null) door.RestoreState(doorOpen, doorSide);
        if (health >= 0f) instance.RestoreHealth(health);
    }

    // ---- 늦게 들어온 접속자: 지금 있는 건축물 ----

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStateRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        foreach (BuildableInstance piece in BuildableInstance.All)
        {
            if (piece == null || !piece.gameObject.activeInHierarchy || !IsReplicated(piece)) continue;
            PlacedToRpc(piece.BuildableId, piece.WorldId, piece.transform.position, piece.transform.eulerAngles.y, piece.SupportId, HealthOf(piece), DoorOpen(piece), DoorSide(piece),
                RpcTarget.Single(sender, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void PlacedToRpc(string buildableId, string worldId, Vector3 position, float yaw, string supportId, float health, bool doorOpen, float doorSide, RpcParams rpcParams = default)
    {
        SpawnReplica(buildableId, worldId, position, yaw, supportId, health, doorOpen, doorSide);
    }

    // ---- 참가자 → 서버: 요청 ----

    public void RequestPlace(string buildableId, Vector3 position, float yaw, int level, string wallId) => PlaceRpc(buildableId, position, yaw, level, wallId ?? "");

    public void RequestDemolish(string worldId) => DemolishRpc(worldId);

    public void RequestRepair(string worldId) => RepairRpc(worldId);

    public void RequestDoor(string worldId) => DoorRequestRpc(worldId);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlaceRpc(string buildableId, Vector3 position, float yaw, int level, string wallId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        BuildingSystem building = FindFirstObjectByType<BuildingSystem>();
        if (building == null || !TryGetAvatar(sender, out NetworkObject avatar, out NetworkInventory inventory)) return;

        Reply(sender, building.ServerPlace(inventory.ServerInventory, avatar.transform.position, buildableId, position, yaw, level, wallId));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DemolishRpc(string worldId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        BuildingSystem building = FindFirstObjectByType<BuildingSystem>();
        if (building == null || !TryGetAvatar(sender, out NetworkObject avatar, out NetworkInventory inventory)) return;

        BuildPlacementStatus status = building.ServerDemolish(avatar.transform.position, worldId, (item, quantity) =>
        {
            inventory.ServerGrant(item, quantity);
            inventory.NotifyPickup(item, quantity);
        });
        Reply(sender, status);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RepairRpc(string worldId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        StructureInspector inspector = FindFirstObjectByType<StructureInspector>();
        if (inspector == null || !TryGetAvatar(sender, out NetworkObject avatar, out NetworkInventory inventory)) return;

        if (nextRepair.TryGetValue(sender, out float allowed) && Time.time < allowed) return;
        nextRepair[sender] = Time.time + RepairMinInterval;

        inspector.ServerRepair(avatar.transform.position, BuildableInstance.FindByWorldId(worldId), inventory.ServerInventory);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DoorRequestRpc(string worldId, RpcParams rpcParams = default)
    {
        if (!TryGetAvatar(rpcParams.Receive.SenderClientId, out NetworkObject avatar, out _)) return;

        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        BuildableDoor door = piece != null ? piece.GetComponent<BuildableDoor>() : null;
        if (door == null || (door.transform.position - avatar.transform.position).sqrMagnitude > DoorReach * DoorReach) return;

        door.Toggle(avatar.transform.position);
    }

    private const float DoorReach = 6f;

    // 요청을 보낸 접속자의 살아 있는 아바타와 서버 인벤토리다(호스트 자신의 아바타는 요청을 보내지 않는다).
    public static bool TryGetAvatar(ulong clientId, out NetworkObject avatar, out NetworkInventory inventory)
    {
        avatar = null;
        inventory = null;
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || clientId == manager.LocalClientId
            || !manager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) || client.PlayerObject == null)
            return false;

        avatar = client.PlayerObject;
        NetworkCombat combat = avatar.GetComponent<NetworkCombat>();
        if (combat != null && combat.IsDead) return false;

        inventory = avatar.GetComponent<NetworkInventory>();
        return inventory != null && inventory.ServerInventory != null;
    }

    // ---- 서버 → 요청자: 거부 이유 ----

    private void Reply(ulong clientId, BuildPlacementStatus status)
    {
        // 조용히 거부하는 경우(Dead)와 성공은 알리지 않는다.
        if (status == BuildPlacementStatus.Ok || status == BuildPlacementStatus.Dead) return;
        RejectedRpc((int)status, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void RejectedRpc(int status, RpcParams rpcParams = default)
    {
        BuildingSystem building = FindFirstObjectByType<BuildingSystem>();
        if (building != null) building.NotifyRejected((BuildPlacementStatus)status);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[mp-build] 서버가 거부함: {(BuildPlacementStatus)status}");
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-build-log면 참가자가 보는 건축물 요약을 2초마다 남기고, -mp-build-test면 설치 → 중복 설치 → 철거 → 범위 밖 설치 요청을 차례로 보낸다.
    private static readonly bool buildLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-build-log") >= 0;
    private static readonly bool buildTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-build-test") >= 0;
    private float nextDebugLog;
    private float nextTestStep;
    private int testStep;

    private void RunClientDebug()
    {
        if (buildLog && Time.time >= nextDebugLog)
        {
            nextDebugLog = Time.time + 2f;
            int count = 0, open = 0;
            float healthSum = 0f;
            foreach (BuildableInstance piece in BuildableInstance.All)
            {
                if (piece == null || !piece.gameObject.activeInHierarchy) continue;
                count++;
                BuildableHealth health = piece.GetComponent<BuildableHealth>();
                if (health != null) healthSum += health.Current;
                BuildableDoor door = piece.GetComponent<BuildableDoor>();
                if (door != null && door.IsOpen) open++;
            }
            Debug.Log($"[mp-build] 건축물={count} 체력합={healthSum:F0} 열린문={open}");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!buildTest || player == null || !NetworkInventory.ReplicaActive || Time.time < nextTestStep) return;
        nextTestStep = Time.time + 1.5f;

        BuildableDefinition floor = BuildableDatabase.Instance != null ? BuildableDatabase.Instance.GetById("floor_stone") : null;
        Inventory own = player.GetComponent<Inventory>();
        // 서버가 재료를 줄 때까지 기다린다(호스트가 ServerGrant로 지급한다).
        if (floor == null || own == null || own.GetQuantity(floor.Costs[0].item) < 2) return;
        Vector3 ahead = player.transform.position + player.transform.forward * 3f;
        Vector3 snapped = BuildingSystem.SnapPosition(ahead, floor, 0f);
        if (!Physics.Raycast(snapped + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore)) return;
        snapped.y = hit.point.y;

        switch (testStep++)
        {
            case 0: RequestPlace("floor_stone", snapped, 0f, 0, ""); break;
            case 1: RequestPlace("floor_stone", snapped, 0f, 0, ""); break;
            case 2:
                BuildableInstance placed = null;
                float best = 4f * 4f;
                foreach (BuildableInstance piece in BuildableInstance.All)
                {
                    if (piece == null || piece.BuildableId != "floor_stone") continue;
                    float sqr = (piece.transform.position - snapped).sqrMagnitude;
                    if (sqr < best) { best = sqr; placed = piece; }
                }
                Debug.Log($"[mp-build] 철거 요청 대상={(placed != null ? placed.WorldId : "없음")}");
                if (placed != null) RequestDemolish(placed.WorldId);
                break;
            case 3: RequestPlace("floor_stone", snapped + Vector3.right * 40f, 0f, 0, ""); break;
            case 4:
            case 5:
                foreach (BuildableInstance piece in BuildableInstance.All)
                {
                    if (piece == null || !piece.gameObject.activeInHierarchy) continue;
                    if (testStep == 5 && piece.GetComponent<BuildableDoor>() != null) { RequestDoor(piece.WorldId); break; }
                    BuildableHealth health = piece.GetComponent<BuildableHealth>();
                    if (testStep == 6 && health != null && health.Current < health.Max) { RequestRepair(piece.WorldId); break; }
                }
                break;
        }
    }
#endif
}
