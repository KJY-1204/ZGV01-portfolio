// 멀티플레이 참가자 인벤토리의 서버 권위 처리 컴포넌트다. 서버(호스트)는 접속자 아바타의 Inventory를 원본으로 갖고, 참가자의 로컬 Inventory는 서버가 보낸 내용을 받는 복제본이다. 제작, 소비, 버리기, 재장전, 채집, 사망 시 가방은 모두 요청으로 서버가 확정한다.
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class NetworkInventory : NetworkBehaviour
{
    // 채집 요청의 노드 위치가 이 거리 안의 노드만 찾고, 아바타와 노드는 이 거리 안이어야 한다.
    private const float NodeMatchRadius = 1.5f;
    private const float GatherReach = 7f;

    // 이 컴퓨터가 소유한 아바타(참가자 쪽에서만 있다)다. 로컬 인벤토리가 복제본이 된 뒤에는 ReplicaActive가 참이다.
    public static NetworkInventory Local { get; private set; }
    public static bool ReplicaActive => Local != null && Local.localInventory != null && Local.localInventory.IsReplica;

    private Inventory serverInventory;
    private CraftingSystem serverCrafting;
    private Inventory localInventory;
    private PlayerRespawn localRespawn;
    private bool dirty;
    // 서버가 알려 준 침대 월드 id의 복제본이 아직 없을 수 있어 생길 때까지 기다린다(소유자 쪽).
    private string pendingBedId;
    private float pendingBedUntil;
    private const float PendingBedSeconds = 30f;
    // 서버가 침대 지정 요청을 받아들이는 아바타와 침대의 최대 거리다.
    private const float BedReach = 8f;
    // 소유자 쪽: 서버의 식별 응답을 받은 뒤 저장된 상태를 적용하고 나서야 현재 상태를 보고한다(접속 직후의 기본 상태가 저장된 상태를 덮어쓰지 않게).
    private bool identifiedReceived;
    private bool reporting;
    private string pendingState;
    private string lastReportedState;
    private float nextStateReport;
    private const float StateReportInterval = 2f;
    private const int MaxStateJsonLength = 4096;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-inv-test 인자가 있으면 참가자가 스냅샷을 받은 뒤 제작/소비/버리기/채집 요청을 차례로 보낸다.
    private static readonly bool selfTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-inv-test") >= 0;
    private float nextTestStep;
    private int testStep;
#endif

    public Inventory ServerInventory => serverInventory;

    public override void OnNetworkSpawn()
    {
        serverInventory = GetComponent<Inventory>();
        serverCrafting = GetComponent<CraftingSystem>();

        // 서버 원본은 호스트 자신의 아바타가 아니라 참가자 아바타의 것만 쓴다(호스트의 인벤토리는 호스트 플레이어가 직접 가진다).
        if (IsServer && !IsOwner)
        {
            serverInventory.Modified += MarkDirty;
            MarkDirty();
        }
        if (IsOwner && !IsServer)
        {
            Local = this;
            // 접속마다 바뀌는 clientId 대신 이 컴퓨터의 고유 id를 알려 서버가 저장해 둔 인벤토리와 침대를 찾게 한다.
            IdentifyRpc(LocalPlayerIdentity.Id);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (serverInventory != null) serverInventory.Modified -= MarkDirty;
        // 접속이 끝난 참가자의 마지막 상태를 서버 기록부에 남긴다(저장할 때 함께 기록된다).
        if (IsServer && !IsOwner) ParticipantStore.Leave(OwnerClientId);
        if (localRespawn != null) localRespawn.BedChanged -= OnLocalBedChanged;
        localRespawn = null;
        if (Local == this)
        {
            if (localInventory != null) localInventory.IsReplica = false;
            Local = null;
        }
    }

    private void MarkDirty() => dirty = true;

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsServer && !IsOwner && dirty)
        {
            dirty = false;
            SendSnapshot();
        }

        if (IsOwner && !IsServer)
        {
            // 로컬 플레이어를 찾으면 그 인벤토리를 복제본으로 바꾼다(서버 스냅샷만 받는다).
            if (localInventory == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    localInventory = player.GetComponent<Inventory>();
                    if (localInventory != null) localInventory.IsReplica = true;
                    // 침대 지정이 바뀔 때마다 서버에 알려 저장에 반영되게 한다.
                    localRespawn = player.GetComponent<PlayerRespawn>();
                    if (localRespawn != null) localRespawn.BedChanged += OnLocalBedChanged;
                }
            }
            TryApplyPendingBed();
            UpdateStateSync();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (selfTest) RunSelfTest();
#endif
        }
    }

    // ---- 서버 → 소유자: 인벤토리 스냅샷 ----

    // 서버가 접속자 아바타의 인벤토리 전체를 소유자에게 보낸다(종류별 합산 수량).
    private void SendSnapshot() => SnapshotRpc(EncodeSnapshot(serverInventory));

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void SnapshotRpc(string snapshot)
    {
        if (localInventory == null || ItemDatabase.Instance == null) return;
        localInventory.ReplaceAll(DecodeSnapshot(snapshot));
    }

    // 인벤토리 내용을 종류별 합산 수량으로 묶는다. 배열은 RPC로 보낼 수 없어 "id:수량" 항목을 ';'로 이은 한 문자열로 보낸다(보관함 동기화도 같은 형식을 쓴다).
    public static string EncodeSnapshot(Inventory inventory)
    {
        var totals = new Dictionary<string, int>();
        foreach (ItemStack stack in inventory.Stacks)
        {
            if (stack.Definition == null) continue;
            string id = stack.Definition.StableId;
            totals[id] = (totals.TryGetValue(id, out int have) ? have : 0) + stack.Quantity;
        }

        var parts = new List<string>(totals.Count);
        foreach (KeyValuePair<string, int> pair in totals) parts.Add(pair.Key + ":" + pair.Value);
        return string.Join(";", parts);
    }

    // EncodeSnapshot의 문자열을 아이템과 수량 목록으로 푼다(모르는 아이템이나 깨진 항목은 건너뛴다).
    public static List<(ItemDefinition, int)> DecodeSnapshot(string snapshot)
    {
        var contents = new List<(ItemDefinition, int)>();
        if (string.IsNullOrEmpty(snapshot) || ItemDatabase.Instance == null) return contents;

        foreach (string part in snapshot.Split(';'))
        {
            int colon = part.LastIndexOf(':');
            if (colon <= 0 || !int.TryParse(part.Substring(colon + 1), out int quantity)) continue;
            ItemDefinition item = ItemDatabase.Instance.GetByStableId(part.Substring(0, colon));
            if (item != null) contents.Add((item, quantity));
        }
        return contents;
    }

    // ---- 참가자 식별과 침대 저장 ----

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void IdentifyRpc(string playerId)
    {
        if (IsOwner) return;
        if (!ParticipantStore.Identify(OwnerClientId, playerId, this))
            Debug.LogWarning($"참가자 id를 저장 대상에 올리지 못했다(비었거나 이미 다른 접속이 쓰는 id): client {OwnerClientId}");
    }

    private void OnLocalBedChanged(RespawnPoint bed, bool removed)
    {
        BuildableInstance instance = bed != null ? bed.GetComponent<BuildableInstance>() : null;
        BedRpc(instance != null ? instance.WorldId : "");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void BedRpc(string worldId)
    {
        if (IsOwner) return;
        if (!string.IsNullOrEmpty(worldId))
        {
            // 서버 월드에 실제로 있는 침대이고 아바타 가까이에 있을 때만 받아들인다.
            BuildableInstance piece = BuildableInstance.FindByWorldId(worldId);
            if (piece == null || piece.GetComponent<RespawnPoint>() == null || (piece.transform.position - transform.position).sqrMagnitude > BedReach * BedReach) return;
        }
        ParticipantStore.SetBed(OwnerClientId, worldId);
    }

    // 서버가 식별된 참가자에게 저장된 상태를 보낸다(saved가 null이면 저장된 것이 없다). 인벤토리 스냅샷을 먼저 보내고 침대, 장착 상태와 생존 수치를 잇달아 보내
    // RPC 순서대로 소유자가 인벤토리를 받은 뒤 장착을 되돌리게 한다.
    public void ServerRestoreClient(ParticipantSaveData saved)
    {
        if (!IsServer || IsOwner) return;
        if (saved == null)
        {
            IdentifiedRpc(false, "");
            return;
        }

        dirty = false;
        SendSnapshot();
        if (saved.hasPosition) RestorePositionRpc(new Vector3(saved.positionX, saved.positionY, saved.positionZ), saved.rotationY);
        if (!string.IsNullOrEmpty(saved.respawnBedId)) RestoreBedRpc(saved.respawnBedId);
        IdentifiedRpc(saved.hasState, saved.hasState ? JsonUtility.ToJson(saved) : "");
    }

    // 소유자: 저장된 위치로 자기 플레이어를 옮긴다. 지금 맵에서 땅속인 좌표(맵이 바뀌기 전의 옛 좌표)면 무시하고 시작 칸에 남는다.
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void RestorePositionRpc(Vector3 position, float yaw)
    {
        GameObject player = FindLocalPlayer();
        if (player == null || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return;
        if (GameSaveSystem.IsBuried(position))
        {
            Debug.LogWarning($"Saved participant position {position} is underground in this map, keeping the start slot position.");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (controller != null) controller.enabled = true;
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void IdentifiedRpc(bool hasState, string stateJson)
    {
        pendingState = hasState ? stateJson : "";
        identifiedReceived = true;
        // 호스트가 불러오기를 해서 다시 올 때도 저장된 상태를 새로 적용한다.
        reporting = false;
    }

    // 소유자 쪽: 식별 응답을 받으면 저장된 상태를 적용하고, 그 뒤로 2초마다 바뀐 상태를 서버에 보고한다.
    private void UpdateStateSync()
    {
        if (localInventory == null || !identifiedReceived) return;
        GameObject player = localInventory.gameObject;

        if (!reporting)
        {
            ApplyState(player, pendingState);
            pendingState = null;
            reporting = true;
            nextStateReport = Time.time + StateReportInterval;
            return;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (stateTest) RunStateTest(player);
        RunMoveTest(player);
#endif

        if (Time.time < nextStateReport) return;
        nextStateReport = Time.time + StateReportInterval;

        string json = JsonUtility.ToJson(CaptureState(player));
        if (json == lastReportedState) return;
        lastReportedState = json;
        StateRpc(json);
    }

    private static ParticipantSaveData CaptureState(GameObject player)
    {
        var state = new ParticipantSaveData { hasState = true };
        GameSaveSystem.CaptureEquipment(player, out state.equippedWeaponId, out state.lastWeaponId, state.magazines, state.equippedArmorIds);
        GameSaveSystem.CaptureSurvival(player, out state.hasSurvival, out state.hunger, out state.thirst);
        // 죽어 있는 동안은 체력을 보고하지 않는다(다시 접속하면 가득 찬 체력으로 시작한다).
        Health health = player.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            state.hasHealth = true;
            state.health = health.CurrentHealth;
        }
        return state;
    }

    private static void ApplyState(GameObject player, string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson)) return;

        ParticipantSaveData state;
        try { state = JsonUtility.FromJson<ParticipantSaveData>(stateJson); }
        catch (System.ArgumentException) { return; }
        if (state == null) return;

        GameSaveSystem.ApplyEquipment(player, state.equippedWeaponId, state.lastWeaponId, state.magazines, state.equippedArmorIds);
        GameSaveSystem.ApplySurvival(player, state.hasSurvival, state.hunger, state.thirst);

        // 저장된 체력을 이벤트 없이 되돌린다(0 이하는 가장 작은 양수로 올려 살아 있게 한다).
        Health health = player.GetComponent<Health>();
        if (state.hasHealth && health != null && !health.IsDead) health.RestoreState(state.health);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void StateRpc(string json)
    {
        if (IsOwner || json == null || json.Length > MaxStateJsonLength) return;
        ParticipantStore.SetState(OwnerClientId, json);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void RestoreBedRpc(string worldId)
    {
        pendingBedId = worldId;
        pendingBedUntil = Time.time + PendingBedSeconds;
    }

    // 침대 복제본이 생기면 알림 없이 지정한다. 제한 시간 안에 생기지 않으면 포기한다(침대가 사라졌을 수 있다).
    private void TryApplyPendingBed()
    {
        if (string.IsNullOrEmpty(pendingBedId) || localRespawn == null) return;
        if (Time.time > pendingBedUntil) { pendingBedId = null; return; }

        BuildableInstance piece = BuildableInstance.FindByWorldId(pendingBedId);
        RespawnPoint point = piece != null ? piece.GetComponent<RespawnPoint>() : null;
        if (point == null) return;

        localRespawn.RestoreBed(point);
        pendingBedId = null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: -mp-state-test면 서버가 준 무기와 방어구를 장착하고 허기/갈증을 바꿔 상태 보고와 저장을 시험한다.
    private static readonly bool stateTest = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-mp-state-test") >= 0;
    private bool stateTestDone;
    private float nextStateTest;

    // 테스트용: -mp-move=dx,dz면 접속 후 한 번 씬 시작 위치 기준 그만큼 떨어진 지면으로 이동해 위치 저장을 시험한다.
    private static readonly string moveArg = FindArg("-mp-move=");
    private bool moveDone;
    private float moveReadyAt = -1f;

    private static string FindArg(string prefix)
    {
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith(prefix)) return arg.Substring(prefix.Length);
        return "";
    }

    private void RunMoveTest(GameObject player)
    {
        if (moveDone || string.IsNullOrEmpty(moveArg)) return;
        // 복원과 시작 칸 이동이 끝나도록 처음 호출한 뒤 4초 기다린다.
        if (moveReadyAt < 0f) moveReadyAt = Time.time + 4f;
        if (Time.time < moveReadyAt) return;
        string[] parts = moveArg.Split(',');
        if (parts.Length != 2 || !float.TryParse(parts[0], out float dx) || !float.TryParse(parts[1], out float dz)) { moveDone = true; return; }

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        Vector3 origin = respawn != null ? respawn.StartPosition : player.transform.position;
        Vector3 target = GroundSnap.OnGround(origin + new Vector3(dx, 0f, dz));
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = target;
        if (controller != null) controller.enabled = true;
        moveDone = true;
        Debug.Log($"[mp-move-test] 이동 완료: {target}");
    }

    private void RunStateTest(GameObject player)
    {
        if (stateTestDone || Time.time < nextStateTest) return;
        nextStateTest = Time.time + 1.5f;

        WeaponEquipment weapons = player.GetComponent<WeaponEquipment>();
        Equipment armor = player.GetComponent<Equipment>();
        SurvivalStats survival = player.GetComponent<SurvivalStats>();
        foreach (ItemStack stack in localInventory.Stacks)
        {
            if (stack.Definition == null) continue;
            WeaponDefinition weapon = weapons != null ? weapons.FindWeapon(stack.Definition) : null;
            if (weapon != null && weapons.Current == null) weapons.TryEquip(weapon);
            if (stack.Definition is ArmorDefinition worn && armor != null) armor.TryEquip(worn);
        }
        if (weapons == null || weapons.Current == null || survival == null) return;

        survival.Restore(40f, 25f);
        stateTestDone = true;
        Debug.Log($"[mp-state-test] 장착 완료: 무기={weapons.Current.WeaponId} 허기=40 갈증=25");
    }
#endif

    // ---- 서버가 월드에서 접속자에게 아이템을 줄 때(전리품, 보상 등 이후 단계용) ----

    // 서버가 참가자 인벤토리에 아이템을 더한다. 호스트 자신의 아바타에는 쓰지 않는다.
    public bool ServerGrant(ItemDefinition item, int quantity)
    {
        if (!IsServer || IsOwner || item == null || quantity <= 0) return false;
        serverInventory.Add(item, quantity);
        return true;
    }

    // 서버가 참가자에게 준 아이템의 획득 알림을 소유자에게 띄운다(전리품 줍기 등).
    public void NotifyPickup(ItemDefinition item, int quantity)
    {
        if (IsServer && !IsOwner && item != null) PickupRpc(item.StableId, quantity);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void PickupRpc(string itemId, int quantity)
    {
        ItemDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(itemId) : null;
        if (localInventory != null && item != null) localInventory.NotifyPickedUp(item, quantity);
    }

    // ---- 참가자 → 서버: 제작 ----

    public void RequestCraft(string recipeName) => CraftRpc(recipeName);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void CraftRpc(string recipeName)
    {
        HudController hud = FindFirstObjectByType<HudController>();
        RecipeDefinition recipe = hud != null ? hud.FindRecipe(recipeName) : null;
        if (recipe == null || serverCrafting == null) return;

        bool ok = serverCrafting.TryCraft(recipe);
        string message = ok ? recipe.GetCompletionMessage()
            : !serverCrafting.IsStationAvailable(recipe) ? "작업대 근처에서만 제작할 수 있습니다" : "재료가 부족합니다";
        NoticeRpc(message, ok);
    }

    // ---- 참가자 → 서버: 소비 ----

    public void RequestConsume(string itemId) => ConsumeRpc(itemId);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ConsumeRpc(string itemId)
    {
        ConsumableDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(itemId) as ConsumableDefinition : null;
        if (item == null || !serverInventory.Remove(item, 1)) return;
        ConsumeAppliedRpc(itemId);
    }

    // 서버가 차감을 확정한 뒤 소유자에게 효과(허기, 갈증, 체력)를 적용하게 한다.
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ConsumeAppliedRpc(string itemId)
    {
        ConsumableDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(itemId) as ConsumableDefinition : null;
        SurvivalStats survival = FindLocalPlayer()?.GetComponent<SurvivalStats>();
        if (item != null && survival != null) survival.ApplyConsumable(item);
    }

    // ---- 참가자 → 서버: 버리기 ----

    public void RequestDrop(string itemId, int quantity) => DropRpc(itemId, quantity);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DropRpc(string itemId, int quantity)
    {
        ItemDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(itemId) : null;
        ItemDropper dropper = FindFirstObjectByType<ItemDropper>();
        if (item == null || quantity <= 0 || dropper == null || serverInventory.GetQuantity(item) < quantity) return;
        if (!serverInventory.Remove(item, quantity)) return;

        // 호스트 월드의 아바타 앞에 놓는다(월드가 동기화되기 전에는 호스트에게만 보인다).
        dropper.PlaceLoot(item, quantity, transform.position, transform.forward);
        NoticeRpc($"{item.DisplayName} {quantity}개를 버렸습니다", true);
    }

    // ---- 참가자 → 서버: 재장전 ----

    public void RequestReload(string weaponId, int needed) => ReloadRpc(weaponId, needed);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReloadRpc(string weaponId, int needed)
    {
        WeaponEquipment catalogSource = FindFirstObjectByType<WeaponEquipment>();
        WeaponDefinition weapon = catalogSource != null ? catalogSource.FindById(weaponId) : null;
        if (weapon == null || weapon.AmmoItem == null || needed <= 0) return;

        int take = Mathf.Min(Mathf.Min(needed, weapon.MagazineSize), serverInventory.GetQuantity(weapon.AmmoItem));
        if (take <= 0 || !serverInventory.Remove(weapon.AmmoItem, take)) return;
        ReloadGrantRpc(weaponId, take);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void ReloadGrantRpc(string weaponId, int rounds)
    {
        WeaponEquipment equipment = FindLocalPlayer()?.GetComponent<WeaponEquipment>();
        if (equipment != null) equipment.GrantReload(equipment.FindById(weaponId), rounds);
    }

    // ---- 참가자 → 서버: 채집 ----

    public bool RequestGather(Vector3 nodePosition, int treeIndex)
    {
        GatherRpc(nodePosition, treeIndex);
        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void GatherRpc(Vector3 nodePosition, int treeIndex)
    {
        // 지형 나무는 번호로 서버의 나무 상태에서 처리한다(대역 노드는 호스트 플레이어 주변에만 있다).
        if (treeIndex >= 0)
        {
            TerrainTreeGather trees = TerrainTreeGather.Instance;
            if (trees != null && trees.ServerGatherTree(treeIndex, transform.position, serverInventory, out ItemDefinition treeItem, out int treeGained))
                GatherResultRpc(nodePosition, treeIndex, treeItem.StableId, treeGained);
            return;
        }

        // 호스트 월드에서 요청한 위치에 있는 노드를 찾는다. 아바타 가까이에 있어야 한다.
        ResourceNode node = null;
        float best = NodeMatchRadius * NodeMatchRadius;
        foreach (ResourceNode candidate in ResourceNode.Active)
        {
            if (candidate == null || !candidate.HasYield) continue;
            float sqr = (candidate.transform.position - nodePosition).sqrMagnitude;
            if (sqr < best) { best = sqr; node = candidate; }
        }
        if (node == null || (node.transform.position - transform.position).sqrMagnitude > GatherReach * GatherReach) return;

        ItemDefinition item = node.Definition != null ? node.Definition.YieldItem : null;
        int before = item != null ? serverInventory.GetQuantity(item) : 0;
        if (!node.TryGather(serverInventory)) return;

        int gained = item != null ? serverInventory.GetQuantity(item) - before : 0;
        GatherResultRpc(node.transform.position, -1, item != null ? item.StableId : "", gained);
    }

    // 서버가 확정한 채집을 소유자 쪽 노드에 반영하고 획득 알림을 띄운다.
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void GatherResultRpc(Vector3 nodePosition, int treeIndex, string itemId, int amount)
    {
        ResourceNode node = null;
        float best = NodeMatchRadius * NodeMatchRadius;
        foreach (ResourceNode candidate in ResourceNode.Active)
        {
            if (candidate == null || !candidate.HasYield) continue;
            if (treeIndex >= 0)
            {
                if (candidate.TerrainTreeIndex == treeIndex) { node = candidate; break; }
                continue;
            }
            float sqr = (candidate.transform.position - nodePosition).sqrMagnitude;
            if (sqr < best) { best = sqr; node = candidate; }
        }
        if (node != null) node.ApplyRemoteGather();

        ItemDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(itemId) : null;
        if (localInventory != null && item != null) localInventory.NotifyPickedUp(item, amount);
    }

    // ---- 참가자 → 서버: 사망 시 가방 ----

    public void RequestDeathDrop(Vector3 position, float yaw) => DeathDropRpc(position, yaw);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DeathDropRpc(Vector3 position, float yaw)
    {
        PlayerRespawn respawn = FindFirstObjectByType<PlayerRespawn>();
        if (respawn == null || (position - transform.position).sqrMagnitude > 25f) return;
        respawn.CreateCorpseBag(serverInventory, position, yaw, OwnerClientId);
    }

    // ---- 공용 ----

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void NoticeRpc(string message, bool success) => SaveLoadDebugController.Notify(message, success);

    private static GameObject FindLocalPlayer() => GameObject.FindGameObjectWithTag("Player");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 스냅샷으로 재료를 받은 뒤 요청을 차례로 보내 서버 처리를 확인한다(결과는 서버 인벤토리에서 읽는다).
    private void RunSelfTest()
    {
        if (!ReplicaActive || Time.time < nextTestStep || localInventory.Stacks.Count == 0) return;
        nextTestStep = Time.time + 1.5f;

        switch (testStep++)
        {
            case 0: RequestCraft("Recipe_BasicBuildingMaterial"); break;
            case 1: RequestDrop("scrap_metal", 1); break;
            case 2: RequestConsume("food_berries"); break;
            case 3: RequestReload("weapon_ak47", 30); break;
            case 4: LocalMutationAttempt(); break;
        }
    }

    // 복제본에 직접 더하거나 빼는 시도는 거부돼야 한다.
    private void LocalMutationAttempt()
    {
        ItemDefinition berries = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId("food_berries") : null;
        int before = berries != null ? localInventory.GetQuantity(berries) : -1;
        if (berries != null) localInventory.Add(berries, 50);
        int after = berries != null ? localInventory.GetQuantity(berries) : -1;
        Debug.Log($"[mp-inv-test] 복제본 직접 추가 시도: {before} -> {after} (같아야 정상)");
    }
#endif
}
