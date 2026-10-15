// 호스트 서버가 멀티플레이 참가자의 저장 데이터(인벤토리, 부활 침대)를 들고 있는 정적 기록부다. 접속 중인 참가자는 서버 인벤토리를 그대로 읽고, 나간 참가자는 마지막 상태를 id별로 보관해 저장할 때 함께 기록한다. 참가자 id는 접속마다 바뀌는 clientId가 아니라 참가자 컴퓨터의 고유 id다.
using System.Collections.Generic;
using UnityEngine;

public static class ParticipantStore
{
    private class Live
    {
        public string playerId;
        public NetworkInventory inventory;
        public string bedId = "";
        // 참가자가 마지막으로 보고한(또는 저장에서 불러온) 장착 상태와 생존 수치다. 아직 없으면 null이다.
        public ParticipantSaveData state;
    }

    // 접속 중인 참가자(clientId 기준)와 나간 참가자의 마지막 상태(playerId 기준)다.
    private static readonly Dictionary<ulong, Live> live = new Dictionary<ulong, Live>();
    private static readonly Dictionary<string, ParticipantSaveData> offline = new Dictionary<string, ParticipantSaveData>();

    // 새 게임이나 세션 종료 때 모두 비운다.
    public static void Clear()
    {
        live.Clear();
        offline.Clear();
    }

    // 참가자가 자기 id를 알렸다. 저장된 데이터가 있으면 서버 인벤토리를 그 내용으로 채우고 침대 id를 돌려준다(없으면 빈 문자열).
    // 같은 id가 이미 다른 접속에 쓰이고 있으면 false를 돌려주고 이 접속은 저장 대상에서 뺀다.
    public static bool Identify(ulong clientId, string playerId, NetworkInventory inventory)
    {
        if (string.IsNullOrWhiteSpace(playerId) || inventory == null || inventory.ServerInventory == null) return false;
        foreach (KeyValuePair<ulong, Live> pair in live)
            if (pair.Key != clientId && pair.Value.playerId == playerId) return false;

        Live entry = new Live { playerId = playerId, inventory = inventory };
        live[clientId] = entry;

        if (offline.TryGetValue(playerId, out ParticipantSaveData saved))
        {
            offline.Remove(playerId);
            Apply(entry, saved);
        }
        else
        {
            // 저장된 것이 없는 새 참가자도 응답을 받아야 상태 보고를 시작한다.
            inventory.ServerRestoreClient(null);
        }
        return true;
    }

    // 참가자가 보고한 장착 상태와 생존 수치(JSON)를 걸러서 기록한다. 깨졌거나 상태가 없는 보고는 무시한다.
    public static void SetState(ulong clientId, string json)
    {
        if (!live.TryGetValue(clientId, out Live entry) || string.IsNullOrEmpty(json)) return;

        ParticipantSaveData data;
        try { data = JsonUtility.FromJson<ParticipantSaveData>(json); }
        catch (System.ArgumentException) { return; }

        if (ParticipantStateRule.Sanitize(data)) entry.state = data;
    }

    // 참가자가 침대를 지정했다(빈 문자열이면 지정을 풀었다).
    public static void SetBed(ulong clientId, string worldId)
    {
        if (live.TryGetValue(clientId, out Live entry)) entry.bedId = worldId ?? "";
    }

    // 참가자 아바타가 사라질 때(접속 종료) 마지막 상태를 남긴다.
    public static void Leave(ulong clientId)
    {
        if (!live.TryGetValue(clientId, out Live entry)) return;
        live.Remove(clientId);
        if (entry.inventory != null && entry.inventory.ServerInventory != null) offline[entry.playerId] = MakeData(entry, false);
    }

    // 저장할 참가자 목록이다. 접속 중인 참가자는 지금 상태, 나간 참가자는 마지막 상태를 합쳐 돌려준다.
    public static List<ParticipantSaveData> CollectForSave()
    {
        var result = new List<ParticipantSaveData>();
        foreach (Live entry in live.Values)
            if (entry.inventory != null && entry.inventory.ServerInventory != null) result.Add(MakeData(entry, true));
        foreach (ParticipantSaveData saved in offline.Values) result.Add(saved);
        return result;
    }

    // 세이브를 불러왔다. 나간 참가자 기록은 저장 내용으로 바꾸고, 이미 접속해 있는 참가자에게는 저장된 상태를 바로 적용한다.
    public static void LoadFromSave(List<ParticipantSaveData> saved)
    {
        offline.Clear();
        if (saved == null) return;

        foreach (ParticipantSaveData data in saved)
        {
            if (data == null || string.IsNullOrEmpty(data.playerId)) continue;
            Live connected = null;
            foreach (Live entry in live.Values)
                if (entry.playerId == data.playerId) { connected = entry; break; }

            if (connected != null) Apply(connected, data);
            else offline[data.playerId] = data;
        }
    }

    // ---- 내부 ----

    private static void Apply(Live entry, ParticipantSaveData saved)
    {
        Inventory serverInventory = entry.inventory.ServerInventory;
        serverInventory.Clear();
        foreach (ItemStackSaveData stack in saved.inventory)
        {
            ItemDefinition item = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(stack.definitionId) : null;
            if (item == null)
            {
                Debug.LogWarning($"Save data references unknown item id '{stack.definitionId}' for a participant, skipping.");
                continue;
            }
            serverInventory.Add(item, stack.quantity);
        }

        entry.bedId = saved.respawnBedId ?? "";
        entry.state = ParticipantStateRule.Sanitize(saved) ? saved : null;
        // 인벤토리 스냅샷, 침대, 장착 상태와 생존 수치를 소유자에게 순서대로 보낸다(침대 복제본은 늦게 생길 수 있어 소유자가 기다린다).
        entry.inventory.ServerRestoreClient(saved);
    }

    // 지금 존재하지 않는 침대 id는 저장하지 않는다(접속 중일 때만 확인한다. 나간 참가자의 id는 마지막 상태 그대로 둔다).
    private static ParticipantSaveData MakeData(Live entry, bool validateBed)
    {
        var data = new ParticipantSaveData { playerId = entry.playerId, respawnBedId = entry.bedId };
        if (validateBed && data.respawnBedId.Length > 0)
        {
            BuildableInstance bed = BuildableInstance.FindByWorldId(data.respawnBedId);
            if (bed == null || bed.GetComponent<RespawnPoint>() == null) data.respawnBedId = "";
        }

        foreach (ItemStack stack in entry.inventory.ServerInventory.Stacks)
        {
            if (stack.Definition == null) continue;
            data.inventory.Add(new ItemStackSaveData { definitionId = stack.Definition.StableId, quantity = stack.Quantity });
        }

        // 마지막 위치와 방향은 서버의 아바타에서 읽는다(소유자 권위 위치 동기화로 이미 있다). 죽어 있는 동안은 기록하지 않아 시작 칸에서 시작하게 한다.
        Transform avatar = entry.inventory.transform;
        NetworkCombat combat = entry.inventory.GetComponent<NetworkCombat>();
        if (avatar != null && (combat == null || !combat.IsDead))
        {
            data.hasPosition = true;
            data.positionX = avatar.position.x;
            data.positionY = avatar.position.y;
            data.positionZ = avatar.position.z;
            data.rotationY = avatar.eulerAngles.y;
        }

        ParticipantSaveData state = entry.state;
        if (state != null)
        {
            data.hasState = true;
            data.equippedWeaponId = state.equippedWeaponId;
            data.lastWeaponId = state.lastWeaponId;
            data.magazines = new List<MagazineSaveData>(state.magazines);
            data.equippedArmorIds = new List<string>(state.equippedArmorIds);
            data.hasSurvival = state.hasSurvival;
            data.hunger = state.hunger;
            data.thirst = state.thirst;
            data.hasHealth = state.hasHealth;
            data.health = state.health;
        }
        return data;
    }
}
