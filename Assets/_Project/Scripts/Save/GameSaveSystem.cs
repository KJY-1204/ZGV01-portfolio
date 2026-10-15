// 싱글플레이 저장/불러오기를 처리하는 로컬 권위 시스템이다. 세이브 파일 포맷(GameSaveData)만 다루고 UI를 모른다.
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GameSaveSystem
{
    public const int SlotCount = 3;
    private const string LegacyFileName = "save_default.json";

    // 지금 저장/불러오기에 쓰는 슬롯(1~SlotCount)이다. 메인 메뉴에서 고르고, 게임 안 저장(F5, 일시정지 메뉴)이 이 슬롯에 쓴다.
    public static int CurrentSlot
    {
        get => currentSlot;
        set => currentSlot = Mathf.Clamp(value, 1, SlotCount);
    }
    private static int currentSlot = 1;
    private static bool legacyChecked;

    // 다음 불러오기가 수동 저장 대신 자동 저장 파일(autosave_slotN.json)을 읽을지다. 불러오기를 시도하면 수동으로 되돌아간다.
    public static bool LoadFromAuto { get; set; }

    // 지금 슬롯에 불러올 저장 파일(LoadFromAuto면 자동 저장 파일)이 있는지다.
    public static bool HasSave => SlotExists(CurrentSlot, LoadFromAuto);

    public static bool HasAnySave
    {
        get
        {
            for (int slot = 1; slot <= SlotCount; slot++) if (SlotExists(slot)) return true;
            return false;
        }
    }

    public static bool SlotExists(int slot, bool auto = false) => File.Exists(PathOf(slot, auto));

    public static bool AnyAutoSave
    {
        get
        {
            for (int slot = 1; slot <= SlotCount; slot++) if (SlotExists(slot, true)) return true;
            return false;
        }
    }

    // 새 게임으로 슬롯을 비울 때는 수동 저장과 자동 저장을 모두 지운다.
    public static void DeleteSlot(int slot)
    {
        foreach (bool auto in new[] { false, true })
        {
            string path = PathOf(slot, auto);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // 슬롯 목록에 보여 줄 요약이다. 파일이 없거나 깨졌으면 비어 있는 슬롯으로 돌려준다.
    public static SaveSlotInfo GetSlotInfo(int slot, bool auto = false)
    {
        string path = PathOf(slot, auto);
        if (!File.Exists(path)) return SaveSlotInfo.Empty;
        try
        {
            return SaveSlotInfo.FromData(JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path)));
        }
        catch (System.ArgumentException)
        {
            return SaveSlotInfo.Empty;
        }
    }

    private static string PathOf(int slot, bool auto = false)
    {
        MigrateLegacy();
        return Path.Combine(Application.persistentDataPath, $"{(auto ? "autosave_slot" : "save_slot")}{Mathf.Clamp(slot, 1, SlotCount)}.json");
    }

    // 슬롯이 생기기 전의 단일 저장 파일은 슬롯 1로 옮긴다(한 번만).
    private static void MigrateLegacy()
    {
        if (legacyChecked) return;
        legacyChecked = true;
        string legacy = Path.Combine(Application.persistentDataPath, LegacyFileName);
        string slotOne = Path.Combine(Application.persistentDataPath, "save_slot1.json");
        if (File.Exists(legacy) && !File.Exists(slotOne)) File.Move(legacy, slotOne);
    }

    // auto가 참이면 수동 저장을 건드리지 않고 슬롯의 자동 저장 파일에 쓴다.
    public static void Save(GameObject player, bool auto = false)
    {
        var data = new GameSaveData();
        Health health = player.GetComponent<Health>();
        Inventory inventory = player.GetComponent<Inventory>();

        data.player.positionX = player.transform.position.x;
        data.player.positionY = player.transform.position.y;
        data.player.positionZ = player.transform.position.z;
        data.player.rotationY = player.transform.eulerAngles.y;
        data.player.currentHealth = health != null ? health.CurrentHealth : 0f;

        if (inventory != null)
        {
            foreach (ItemStack stack in inventory.Stacks)
            {
                if (stack.Definition == null) continue;
                data.player.inventory.Add(new ItemStackSaveData
                {
                    definitionId = stack.Definition.StableId,
                    quantity = stack.Quantity
                });
            }
        }

        CaptureEquipment(player, out data.player.equippedWeaponId, out data.player.lastWeaponId, data.player.magazines, data.player.equippedArmorIds);

        LanternEquipment lantern = player.GetComponent<LanternEquipment>();
        if (lantern != null) data.player.lanternOn = lantern.IsOn;

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null && respawn.Bed != null)
        {
            BuildableInstance bedInstance = respawn.Bed.GetComponent<BuildableInstance>();
            if (bedInstance != null) data.player.respawnBedId = bedInstance.WorldId;
        }

        CaptureSurvival(player, out data.player.hasSurvival, out data.player.hunger, out data.player.thirst);

        foreach (BuildableInstance instance in Object.FindObjectsByType<BuildableInstance>(FindObjectsSortMode.None))
        {
            // 건축 미리보기(고스트)는 식별자가 없다. 저장하면 불러올 때 실제 건축물로 되살아난다.
            if (string.IsNullOrEmpty(instance.WorldId)) continue;

            BuildableSaveData saved = new BuildableSaveData
            {
                worldId = instance.WorldId,
                buildableId = instance.BuildableId,
                positionX = instance.transform.position.x,
                positionY = instance.transform.position.y,
                positionZ = instance.transform.position.z,
                rotationY = instance.transform.eulerAngles.y
            };

            BuildableDoor door = instance.GetComponent<BuildableDoor>();
            if (door != null)
            {
                saved.doorOpen = door.IsOpen;
                saved.doorSide = door.Side;
            }
            BuildableHealth durability = instance.GetComponent<BuildableHealth>();
            if (durability != null) saved.health = durability.Current;
            StorageContainer storage = instance.GetComponent<StorageContainer>();
            if (storage != null)
            {
                if (storage.RemainingSeconds >= 0f) saved.lifetimeRemaining = storage.RemainingSeconds;
                foreach (ItemStack stack in storage.Contents.Stacks)
                {
                    if (stack.Definition == null) continue;
                    saved.items.Add(new ItemStackSaveData { definitionId = stack.Definition.StableId, quantity = stack.Quantity });
                }
            }
            saved.supportId = instance.SupportId;
            data.buildables.Add(saved);
        }

        SaveAnimals(data);
        // 참가자(접속 중과 나간 사람)의 인벤토리와 침대. 호스트가 아니어도 불러올 때 받아 둔 나간 참가자 기록은 유지한다.
        data.participants = ParticipantStore.CollectForSave();

        if (GameClock.Current != null) data.timeOfDay = GameClock.Current.Hour;
        data.elapsedGameHours = BaseThreat.ElapsedGameHours;
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        string json = JsonUtility.ToJson(data, true);
        string savePath = PathOf(CurrentSlot, auto);
        File.WriteAllText(savePath, json);
        Debug.Log($"Game saved to {savePath} (slot {CurrentSlot}{(auto ? ", auto" : "")})");
    }

    public static bool Load(GameObject player)
    {
        string loadPath = PathOf(CurrentSlot, LoadFromAuto);
        LoadFromAuto = false;
        if (!File.Exists(loadPath))
        {
            Debug.LogWarning("No save file found at " + loadPath);
            return false;
        }

        string json = File.ReadAllText(loadPath);
        GameSaveData data;
        try
        {
            // 깨진 JSON은 null이 아니라 예외로 올라온다.
            data = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (System.ArgumentException e)
        {
            Debug.LogError($"Save file could not be parsed: {loadPath} ({e.Message})");
            return false;
        }
        if (data == null)
        {
            Debug.LogError("Save file could not be parsed: " + loadPath);
            return false;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        Vector3 savedPosition = new Vector3(data.player.positionX, data.player.positionY, data.player.positionZ);
        if (IsBuried(savedPosition))
        {
            // 맵이 바뀌기 전의 옛 좌표라 땅속이면 떨어지므로 현재(시작) 위치를 유지한다.
            Debug.LogWarning($"Saved player position {savedPosition} is underground in this map, keeping the current position.");
        }
        else
        {
            player.transform.position = savedPosition;
            player.transform.eulerAngles = new Vector3(0f, data.player.rotationY, 0f);
        }
        if (controller != null) controller.enabled = true;

        Health health = player.GetComponent<Health>();
        if (health != null && !health.IsDead)
        {
            // 저장된 체력 그대로 복원한다(피해로 깎으면 방어구 감소율이 섞이고 피격 반응이 일어난다). 0 이하로 저장된 상태는 사망으로 되돌린다.
            if (data.player.currentHealth <= 0f) health.ApplyTrueDamage(health.MaxHealth);
            else health.RestoreState(data.player.currentHealth);
        }

        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.Clear();
            foreach (ItemStackSaveData savedStack in data.player.inventory)
            {
                ItemDefinition definition = ItemDatabase.Instance != null
                    ? ItemDatabase.Instance.GetByStableId(savedStack.definitionId)
                    : null;

                if (definition == null)
                {
                    Debug.LogWarning($"Save data references unknown item id '{savedStack.definitionId}', skipping.");
                    continue;
                }

                inventory.Add(definition, savedStack.quantity);
            }
        }

        // 인벤토리를 먼저 복원했으므로(가지고 있어야 장착된다) 이어서 방어구와 무기/탄창 장착을 되돌린다. 옛 세이브는 목록이 비어 아무것도 장착하지 않는다.
        ApplyEquipment(player, data.player.equippedWeaponId, data.player.lastWeaponId, data.player.magazines, data.player.equippedArmorIds);
        ApplySurvival(player, data.player.hasSurvival, data.player.hunger, data.player.thirst);

        foreach (BuildableInstance existing in Object.FindObjectsByType<BuildableInstance>(FindObjectsSortMode.None))
        {
            Object.Destroy(existing.gameObject);
        }

        foreach (BuildableSaveData savedBuildable in data.buildables)
        {
            BuildableDefinition definition = BuildableDatabase.Instance != null
                ? BuildableDatabase.Instance.GetById(savedBuildable.buildableId)
                : null;

            if (definition == null || definition.Prefab == null)
            {
                Debug.LogWarning($"Save data references unknown buildable id '{savedBuildable.buildableId}', skipping.");
                continue;
            }

            Vector3 position = new Vector3(savedBuildable.positionX, savedBuildable.positionY, savedBuildable.positionZ);
            Quaternion rotation = Quaternion.Euler(0f, savedBuildable.rotationY, 0f);
            GameObject spawned = Object.Instantiate(definition.Prefab, position, rotation);

            BuildableInstance instance = spawned.GetComponent<BuildableInstance>();
            if (instance == null) instance = spawned.AddComponent<BuildableInstance>();
            instance.Restore(savedBuildable.buildableId, savedBuildable.worldId);
            instance.SetSupport(savedBuildable.supportId);

            BuildableDoor door = spawned.GetComponent<BuildableDoor>();
            if (door != null) door.RestoreState(savedBuildable.doorOpen, savedBuildable.doorSide);
            if (savedBuildable.health >= 0f) instance.RestoreHealth(savedBuildable.health);
            StorageContainer storage = spawned.GetComponent<StorageContainer>();
            if (storage != null)
            {
                storage.Restore(ToStacks(savedBuildable.items));
                if (savedBuildable.lifetimeRemaining >= 0f) storage.RestoreRemainingSeconds(savedBuildable.lifetimeRemaining);
            }
        }

        // 건축물이 복원된 뒤에 지정한 침대를 다시 찾는다(없거나 옛 세이브면 시작 위치로 돌아간다).
        LanternEquipment lanternEquipment = player.GetComponent<LanternEquipment>();
        if (lanternEquipment != null) lanternEquipment.RestoreOn(data.player.lanternOn);

        PlayerRespawn playerRespawn = player.GetComponent<PlayerRespawn>();
        if (playerRespawn != null) playerRespawn.RestoreBed(FindRespawnPoint(data.player.respawnBedId));

        // 동물 목록이 없는 옛 세이브는 지금 있는 동물을 그대로 둔다.
        if (data.hasAnimals) LoadAnimals(data.animals);

        // 참가자 기록을 저장 내용으로 바꾸고 이미 접속해 있는 참가자에게도 적용한다(옛 세이브는 빈 목록이다).
        ParticipantStore.LoadFromSave(data.participants);

        if (data.timeOfDay >= 0f && GameClock.Current != null) GameClock.Current.SetHour(data.timeOfDay);
        BaseThreat.Restore(data.elapsedGameHours);

        Debug.Log($"Game loaded from {loadPath}");
        return true;
    }

    private static RespawnPoint FindRespawnPoint(string worldId)
    {
        if (string.IsNullOrEmpty(worldId)) return null;
        // 불러오기 직전에 지운 옛 건축물이 이번 프레임 끝까지 남아 있어 같은 id가 둘일 수 있다. 나중에 켜진 쪽(새로 만든 것)을 고른다.
        RespawnPoint found = null;
        foreach (RespawnPoint point in RespawnPoint.Active)
        {
            BuildableInstance instance = point != null ? point.GetComponent<BuildableInstance>() : null;
            if (instance != null && instance.WorldId == worldId) found = point;
        }
        return found;
    }

    // 저장된 아이템 목록을 런타임 스택으로 바꾼다. 알 수 없는 id는 경고하고 건너뛴다.
    private static List<ItemStack> ToStacks(List<ItemStackSaveData> saved)
    {
        List<ItemStack> stacks = new List<ItemStack>();
        if (saved == null) return stacks;
        foreach (ItemStackSaveData entry in saved)
        {
            ItemDefinition definition = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(entry.definitionId) : null;
            if (definition == null)
            {
                Debug.LogWarning($"Save data references unknown item id '{entry.definitionId}', skipping.");
                continue;
            }
            stacks.Add(new ItemStack(definition, entry.quantity));
        }
        return stacks;
    }

    // ---- 장착 상태와 생존 수치(호스트 저장과 멀티플레이 참가자 저장이 같이 쓴다) ----

    // 플레이어의 장착 무기, 마지막 무기, 탄창, 장착 방어구를 읽는다. 해당 컴포넌트가 없으면 비워 둔다.
    public static void CaptureEquipment(GameObject player, out string equippedWeaponId, out string lastWeaponId, List<MagazineSaveData> magazines, List<string> armorIds)
    {
        equippedWeaponId = "";
        lastWeaponId = "";

        WeaponEquipment equipment = player.GetComponent<WeaponEquipment>();
        if (equipment != null)
        {
            equippedWeaponId = equipment.Current != null ? equipment.Current.WeaponId : "";
            lastWeaponId = equipment.LastWeapon != null ? equipment.LastWeapon.WeaponId : "";
            foreach (KeyValuePair<WeaponDefinition, int> pair in equipment.Magazines)
            {
                magazines.Add(new MagazineSaveData { weaponId = pair.Key.WeaponId, rounds = pair.Value });
            }
        }

        Equipment armorEquipment = player.GetComponent<Equipment>();
        if (armorEquipment != null)
        {
            foreach (ArmorSlot slot in System.Enum.GetValues(typeof(ArmorSlot)))
            {
                ArmorDefinition worn = armorEquipment.Get(slot);
                if (worn != null) armorIds.Add(worn.StableId);
            }
        }
    }

    public static void CaptureSurvival(GameObject player, out bool hasSurvival, out float hunger, out float thirst)
    {
        hasSurvival = false;
        hunger = 0f;
        thirst = 0f;

        SurvivalStats survival = player.GetComponent<SurvivalStats>();
        if (survival == null) return;
        hasSurvival = true;
        hunger = survival.Hunger.Current;
        thirst = survival.Thirst.Current;
    }

    // 인벤토리를 먼저 복원한 뒤 부른다(가지고 있는 아이템만 장착된다). 방어구를 먼저, 이어서 무기와 탄창을 되돌린다.
    public static void ApplyEquipment(GameObject player, string equippedWeaponId, string lastWeaponId, List<MagazineSaveData> magazines, List<string> armorIds)
    {
        Equipment armorEquipment = player.GetComponent<Equipment>();
        if (armorEquipment != null)
        {
            List<ArmorDefinition> worn = new List<ArmorDefinition>();
            foreach (string id in armorIds)
            {
                ArmorDefinition armor = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetByStableId(id) as ArmorDefinition : null;
                if (armor == null) Debug.LogWarning($"Save data references unknown armor id '{id}', skipping.");
                else worn.Add(armor);
            }
            armorEquipment.RestoreState(worn);
        }

        WeaponEquipment equipment = player.GetComponent<WeaponEquipment>();
        if (equipment != null)
        {
            var restored = new List<KeyValuePair<WeaponDefinition, int>>();
            foreach (MagazineSaveData saved in magazines)
            {
                WeaponDefinition weapon = equipment.FindById(saved.weaponId);
                if (weapon == null)
                {
                    Debug.LogWarning($"Save data references unknown weapon id '{saved.weaponId}', skipping.");
                    continue;
                }
                restored.Add(new KeyValuePair<WeaponDefinition, int>(weapon, saved.rounds));
            }

            equipment.RestoreState(equipment.FindById(equippedWeaponId), equipment.FindById(lastWeaponId), restored);
        }
    }

    public static void ApplySurvival(GameObject player, bool hasSurvival, float hunger, float thirst)
    {
        SurvivalStats survival = player.GetComponent<SurvivalStats>();
        if (survival != null && hasSurvival) survival.Restore(hunger, thirst);
    }

    // 저장된 위치가 지형 아래이고 그 아래에 받쳐 주는 바닥도 없으면 묻힌 위치다(규칙은 SavedPositionRule).
    public static bool IsBuried(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        float terrainHeight = terrain != null ? GroundSnap.OnGround(position).y : float.NaN;
        bool hasSupport = Physics.Raycast(position + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        return SavedPositionRule.IsBuried(position.y, terrainHeight, hasSupport);
    }

    // 살아 있는 동물과 맹수만 저장한다(시체와 떨어진 전리품, 행동 상태는 저장하지 않는다).
    private static void SaveAnimals(GameSaveData data)
    {
        data.hasAnimals = true;
        foreach (AnimalAI animal in Object.FindObjectsByType<AnimalAI>(FindObjectsSortMode.None))
        {
            if (animal.IsDead) continue;
            data.animals.Add(MakeAnimalData(animal.Definition, animal.transform, animal.GetComponent<Health>()));
        }
        foreach (PredatorAI predator in Object.FindObjectsByType<PredatorAI>(FindObjectsSortMode.None))
        {
            if (predator.IsDead) continue;
            data.animals.Add(MakeAnimalData(predator.Definition, predator.transform, predator.GetComponent<Health>()));
        }
    }

    private static AnimalSaveData MakeAnimalData(AnimalDefinition definition, Transform transform, Health health)
    {
        return new AnimalSaveData
        {
            definitionId = definition.StableId,
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            rotationY = transform.eulerAngles.y,
            health = health.CurrentHealth
        };
    }

    // 지금 있는 동물(시체 포함)을 치우고 저장된 목록대로 다시 만든다. 불러온 동물은 대기 상태로 시작하고, 프리팹을 맡은 스포너가 살아 있는 수로 센다.
    private static void LoadAnimals(List<AnimalSaveData> saved)
    {
        foreach (AnimalAI existing in Object.FindObjectsByType<AnimalAI>(FindObjectsSortMode.None)) Object.Destroy(existing.gameObject);
        foreach (PredatorAI existing in Object.FindObjectsByType<PredatorAI>(FindObjectsSortMode.None)) Object.Destroy(existing.gameObject);

        AnimalSpawner[] spawners = Object.FindObjectsByType<AnimalSpawner>(FindObjectsSortMode.None);
        foreach (AnimalSaveData entry in saved)
        {
            GameObject prefab = AnimalDatabase.Instance != null ? AnimalDatabase.Instance.GetPrefab(entry.definitionId) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"Save data references unknown animal id '{entry.definitionId}', skipping.");
                continue;
            }

            Vector3 position = new Vector3(entry.positionX, entry.positionY, entry.positionZ);
            GameObject animal = Object.Instantiate(prefab, position, Quaternion.Euler(0f, entry.rotationY, 0f));
            Health health = animal.GetComponent<Health>();
            if (health != null) health.RestoreState(entry.health);
            // 멀티플레이 호스트에서는 불러온 동물도 네트워크 오브젝트로 소환해 참가자에게 보인다.
            Unity.Netcode.NetworkObject networkObject = animal.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObject != null && MultiplayerSession.IsHost) networkObject.Spawn(true);

            foreach (AnimalSpawner spawner in spawners)
            {
                if (!spawner.Owns(prefab)) continue;
                spawner.Adopt(animal);
                break;
            }
        }
    }
}
