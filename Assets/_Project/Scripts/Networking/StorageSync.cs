// 보관함(시체 가방 포함) 내용의 서버 권위 동기화 컴포넌트다. 원본은 호스트 월드의 StorageContainer이고, 참가자는 열기/닫기/옮기기/모두 가져오기를 서버에 요청하며 서버가 검증해 아바타 서버 인벤토리와 보관함 사이로 옮긴다. 참가자 쪽 보관함은 서버 스냅샷을 받아 보여 주는 복제본이다.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StorageSync : NetworkBehaviour
{
    // 아바타가 보관함에서 이 거리 안이어야 요청이 받아들여진다(HUD가 닫히는 거리보다 느슨하게 둔다).
    private const float Reach = 6f;

    // 서버가 지금 누가 어느 보관함을 보고 있는지 기억해 내용이 바뀔 때마다 보는 사람에게만 스냅샷을 보낸다.
    private class Watch
    {
        public StorageContainer container;
        public readonly HashSet<ulong> viewers = new HashSet<ulong>();
        public bool dirty;
        public System.Action handler;
    }

    private readonly Dictionary<string, Watch> watches = new Dictionary<string, Watch>();
    private readonly List<string> staleBuffer = new List<string>();

    public static StorageSync Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (!IsServer) return;
        StorageContainer.OwnerExpired += OnOwnerExpired;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
        StorageContainer.OwnerExpired -= OnOwnerExpired;
        if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        foreach (Watch watch in watches.Values) Unwatch(watch);
        watches.Clear();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned || watches.Count == 0) return;

        staleBuffer.Clear();
        foreach (KeyValuePair<string, Watch> pair in watches)
        {
            Watch watch = pair.Value;
            // 보관함이 사라졌으면 기록을 지운다(참가자 쪽 복제본은 건축물 제거 알림으로 사라진다).
            if (watch.container == null) { staleBuffer.Add(pair.Key); continue; }
            if (!watch.dirty) continue;

            watch.dirty = false;
            string snapshot = NetworkInventory.EncodeSnapshot(watch.container.Contents);
            foreach (ulong viewer in watch.viewers) SnapshotRpc(pair.Key, snapshot, watch.container.RemainingSeconds, RpcTarget.Single(viewer, RpcTargetUse.Temp));
        }
        foreach (string id in staleBuffer) watches.Remove(id);
    }

    // ---- 참가자 → 서버: 요청 ----

    public void RequestOpen(string worldId) => OpenRpc(worldId);

    public void RequestClose(string worldId) => CloseRpc(worldId);

    public void RequestMove(string worldId, string itemId, int quantity, bool intoStorage) => MoveRpc(worldId, itemId, quantity, intoStorage);

    public void RequestTakeAll(string worldId) => TakeAllRpc(worldId);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OpenRpc(string worldId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!TryResolve(sender, worldId, out StorageContainer container, out _)) return;

        Watch watch = GetOrCreateWatch(worldId, container);
        watch.viewers.Add(sender);
        // 열자마자 현재 내용을 한 번 보낸다.
        SnapshotRpc(worldId, NetworkInventory.EncodeSnapshot(container.Contents), container.RemainingSeconds, RpcTarget.Single(sender, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CloseRpc(string worldId, RpcParams rpcParams = default)
    {
        if (!watches.TryGetValue(worldId, out Watch watch)) return;
        watch.viewers.Remove(rpcParams.Receive.SenderClientId);
        if (watch.viewers.Count == 0)
        {
            Unwatch(watch);
            watches.Remove(worldId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MoveRpc(string worldId, string itemId, int quantity, bool intoStorage, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (quantity <= 0 || ItemDatabase.Instance == null || !TryResolve(sender, worldId, out StorageContainer container, out Inventory inventory)) return;

        ItemDefinition item = ItemDatabase.Instance.GetByStableId(itemId);
        if (item == null) return;

        int moved = intoStorage
            ? StorageTransfer.Move(inventory, container.Contents, item, quantity)
            : StorageTransfer.Move(container.Contents, inventory, item, quantity);

        if (intoStorage && moved < quantity)
            NoticeRpc(moved == 0 ? "보관함이 가득 찼습니다" : $"보관함이 가득 차서 {moved}개만 넣었습니다", false, RpcTarget.Single(sender, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeAllRpc(string worldId, RpcParams rpcParams = default)
    {
        if (!TryResolve(rpcParams.Receive.SenderClientId, worldId, out StorageContainer container, out Inventory inventory)) return;
        StorageTransfer.MoveAll(container.Contents, inventory);
    }

    // 요청한 접속자가 살아 있고 보관함이 거리 안인지 확인하고 보관함과 그 접속자의 서버 인벤토리를 찾는다.
    private static bool TryResolve(ulong sender, string worldId, out StorageContainer container, out Inventory inventory)
    {
        container = null;
        inventory = null;
        if (!BuildSync.TryGetAvatar(sender, out NetworkObject avatar, out NetworkInventory networkInventory)) return false;

        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        container = piece != null ? piece.GetComponent<StorageContainer>() : null;
        if (container == null || (container.transform.position - avatar.transform.position).sqrMagnitude > Reach * Reach) return false;

        inventory = networkInventory.ServerInventory;
        return true;
    }

    // ---- 보는 사람 관리 ----

    private Watch GetOrCreateWatch(string worldId, StorageContainer container)
    {
        if (watches.TryGetValue(worldId, out Watch existing) && existing.container == container) return existing;

        // 같은 id의 옛 기록이 다른 보관함을 가리키면(사라졌다 다시 생긴 경우) 새로 만든다.
        if (existing != null) Unwatch(existing);
        Watch watch = new Watch { container = container };
        watch.handler = () => watch.dirty = true;
        container.Contents.Modified += watch.handler;
        watches[worldId] = watch;
        return watch;
    }

    private static void Unwatch(Watch watch)
    {
        if (watch.container != null && watch.handler != null) watch.container.Contents.Modified -= watch.handler;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        staleBuffer.Clear();
        foreach (KeyValuePair<string, Watch> pair in watches)
        {
            pair.Value.viewers.Remove(clientId);
            if (pair.Value.viewers.Count == 0) staleBuffer.Add(pair.Key);
        }
        foreach (string id in staleBuffer)
        {
            Unwatch(watches[id]);
            watches.Remove(id);
        }
    }

    // 다른 접속자의 시체 가방이 수명이 다해 사라지면 그 접속자에게 알린다(호스트 자신의 가방은 HUD가 직접 알린다).
    private void OnOwnerExpired(ulong ownerClientId, string name)
    {
        if (!IsSpawned || ownerClientId == NetworkManager.LocalClientId) return;
        NoticeRpc($"{name}이(가) 시간이 지나 사라졌습니다", false, RpcTarget.Single(ownerClientId, RpcTargetUse.Temp));
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-storage-log면 이 접속자가 보는 보관함/가방/침대 요약을 2초마다 남기고, -mp-storage-test면 보관함 열기 → 넣기 → 꺼내기 → 모두 가져오기 → 침대 지정을 차례로 요청한다.
    private static readonly bool storageLog = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-storage-log") >= 0;
    private static readonly bool storageTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-storage-test") >= 0;
    private float nextDebugLog;
    private float nextTestStep;
    private int testStep;
    private StorageContainer testChest;

    private void LateUpdate()
    {
        if (!IsSpawned || IsServer) return;
        if (storageLog && Time.time >= nextDebugLog)
        {
            nextDebugLog = Time.time + 2f;
            int chests = 0, bags = 0;
            var contents = new System.Text.StringBuilder();
            foreach (StorageContainer storage in StorageContainer.Active)
            {
                BuildableInstance instance = storage.GetComponent<BuildableInstance>();
                bool bag = instance != null && instance.BuildableId == BuildableInstance.CorpseBagId;
                if (bag) bags++; else chests++;
                contents.Append(bag ? "가방[" : "보관함[").Append(NetworkInventory.EncodeSnapshot(storage.Contents)).Append("] ");
            }
            GameObject me = GameObject.FindGameObjectWithTag("Player");
            Inventory myInventory = me != null ? me.GetComponent<Inventory>() : null;
            PlayerRespawn myRespawn = me != null ? me.GetComponent<PlayerRespawn>() : null;
            string gear = "";
            if (me != null)
            {
                var mags = new List<MagazineSaveData>();
                var armorIds = new List<string>();
                GameSaveSystem.CaptureEquipment(me, out string weaponId, out _, mags, armorIds);
                GameSaveSystem.CaptureSurvival(me, out _, out float hunger, out float thirst);
                gear = $" 무기={weaponId} 방어구={string.Join(",", armorIds)} 허기={hunger:F0} 갈증={thirst:F0}";
            }
            Debug.Log($"[mp-storage] 보관함={chests} 가방={bags} 침대={RespawnPoint.Active.Count} 내용={contents} 내인벤토리=[{(myInventory != null ? NetworkInventory.EncodeSnapshot(myInventory) : "")}] 침대지정={(myRespawn != null && myRespawn.HasBed)}{gear} 위치={(me != null ? me.transform.position.ToString("F1") : "")} 체력={(me != null && me.GetComponent<Health>() != null ? me.GetComponent<Health>().CurrentHealth.ToString("F0") : "")}");
        }
        if (storageTest) RunClientTest();
    }

    private void RunClientTest()
    {
        if (Time.time < nextTestStep || !NetworkInventory.ReplicaActive) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Inventory mine = player != null ? player.GetComponent<Inventory>() : null;
        if (mine == null) return;

        // 처음에는 서버가 지급한 재료와 근처의 일반 보관함 복제본이 보일 때까지 기다린다.
        if (testChest == null)
        {
            foreach (StorageContainer storage in StorageContainer.Active)
            {
                BuildableInstance instance = storage.GetComponent<BuildableInstance>();
                if (instance != null && instance.BuildableId != BuildableInstance.CorpseBagId && !string.IsNullOrEmpty(instance.WorldId)) { testChest = storage; break; }
            }
            if (testChest == null || mine.Stacks.Count == 0) return;
        }

        nextTestStep = Time.time + 1.5f;
        string worldId = testChest.WorldId;
        ItemStack first = mine.Stacks.Count > 0 ? mine.Stacks[0] : null;
        switch (testStep++)
        {
            case 0: RequestOpen(worldId); break;
            case 1: if (first != null) RequestMove(worldId, first.Definition.StableId, 2, true); break;
            case 2: if (testChest.Contents.Stacks.Count > 0) RequestMove(worldId, testChest.Contents.Stacks[0].Definition.StableId, 1, false); break;
            case 3: RequestTakeAll(worldId); break;
            case 4: DirectMutationAttempt(); break;
            case 5: BedAttempt(player); break;
            case 6: RequestClose(worldId); break;
            case 7: testStep = 7; WaitForDeathThenLootBags(player); break;
        }
    }

    // 호스트가 이 참가자를 죽여 시체 가방이 보이면 부활(침대 앞)해서 모든 가방을 열고 모두 가져온다.
    private int bagPhase;

    private void WaitForDeathThenLootBags(GameObject player)
    {
        Health health = player.GetComponent<Health>();
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        var bags = new List<StorageContainer>();
        foreach (StorageContainer storage in StorageContainer.Active)
        {
            BuildableInstance instance = storage.GetComponent<BuildableInstance>();
            if (instance != null && instance.BuildableId == BuildableInstance.CorpseBagId) bags.Add(storage);
        }

        switch (bagPhase)
        {
            case 0:
                if (health != null && health.IsDead && bags.Count > 0) bagPhase = 1;
                break;
            case 1:
                Debug.Log($"[mp-storage-test] 사망 확인, 가방 {bags.Count}개 보임. 부활 시도");
                if (respawn != null) respawn.Respawn();
                bagPhase = 2;
                break;
            case 2:
                foreach (StorageContainer bag in bags) RequestOpen(bag.WorldId);
                bagPhase = 3;
                break;
            case 3:
                foreach (StorageContainer bag in bags) Debug.Log($"[mp-storage-test] 가방 열림: 내용=[{NetworkInventory.EncodeSnapshot(bag.Contents)}] 남은초={bag.RemainingSeconds:F0}");
                foreach (StorageContainer bag in bags) RequestTakeAll(bag.WorldId);
                bagPhase = 4;
                break;
            case 4:
                Inventory mine = player.GetComponent<Inventory>();
                Debug.Log($"[mp-storage-test] 가방 비움 후: 내 인벤토리=[{NetworkInventory.EncodeSnapshot(mine)}] 남은 가방={bags.Count}");
                bagPhase = 5;
                break;
        }
    }

    // 복제본 보관함에 직접 더하는 시도는 거부돼야 한다.
    private void DirectMutationAttempt()
    {
        ItemDefinition any = testChest.Contents.Stacks.Count > 0 ? testChest.Contents.Stacks[0].Definition : null;
        if (any == null && ItemDatabase.Instance != null) any = ItemDatabase.Instance.GetByStableId("food_berries");
        int before = any != null ? testChest.Contents.GetQuantity(any) : -1;
        if (any != null) testChest.Contents.Add(any, 50);
        Debug.Log($"[mp-storage-test] 복제본 보관함 직접 추가 시도: {before} -> {(any != null ? testChest.Contents.GetQuantity(any) : -1)} (같아야 정상)");
    }

    private static void BedAttempt(GameObject player)
    {
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        RespawnPoint bed = RespawnPoint.Active.Count > 0 ? RespawnPoint.Active[0] : null;
        Debug.Log($"[mp-storage-test] 침대 지정: 침대복제본={(bed != null)} 결과={(respawn != null && bed != null && respawn.SetBed(bed))} 지정됨={(respawn != null && respawn.HasBed)}");
    }
#endif

    // ---- 서버 → 참가자 ----

    [Rpc(SendTo.SpecifiedInParams)]
    private void SnapshotRpc(string worldId, string snapshot, float remainingSeconds, RpcParams rpcParams = default)
    {
        BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
        StorageContainer container = piece != null ? piece.GetComponent<StorageContainer>() : null;
        if (container == null) return;

        container.Contents.ReplaceAll(NetworkInventory.DecodeSnapshot(snapshot));
        if (remainingSeconds >= 0f) container.RestoreRemainingSeconds(remainingSeconds);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NoticeRpc(string message, bool success, RpcParams rpcParams = default)
    {
        SaveLoadDebugController.Notify(message, success);
    }
}
