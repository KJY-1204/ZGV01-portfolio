// 플레이어 사망 후 부활 흐름을 맡는 컴포넌트다. 죽으면 인벤토리를 시체 가방에 남기고 이동을 멈추며, 잠시 뒤 Enter로 지정한 침대 앞(없으면 시작 위치)에서 되살아난다.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
public class PlayerRespawn : MonoBehaviour
{
    // 죽은 직후 실수로 바로 부활하지 않도록 이 시간 동안은 부활 입력을 받지 않는다.
    [SerializeField] private float respawnDelay = 2.5f;
    // 부활할 때 체력과 허기/갈증을 최대치의 이 비율로 맞춘다.
    [SerializeField, Range(0.1f, 1f)] private float respawnHealth = 1f;
    [SerializeField, Range(0f, 1f)] private float respawnSurvival = 0.5f;
    // 쓰러질 때 모델(Geometry/Skeleton)을 눕히는 각도와 걸리는 시간이다. 카메라 루트는 건드리지 않는다.
    [SerializeField] private float fallAngle = -85f;
    [SerializeField] private float fallSeconds = 0.5f;
    [SerializeField] private Transform[] modelRoots = Array.Empty<Transform>();
    // 죽은 자리에 놓이는 시체 가방 프리팹이다(건축물 id "corpse_bag").
    [SerializeField] private GameObject corpseBagPrefab;

    private const string CorpseBagId = BuildableInstance.CorpseBagId;

    private Health health;
    private Inventory inventory;
    private WeaponEquipment weapons;
    private StarterAssets.ThirdPersonController movement;
    private StarterAssets.StarterAssetsInputs inputs;
    private CharacterController controller;
    private SurvivalStats survival;
    private Vector3 spawnPosition;
    private Vector3 startPosition;
    private Quaternion spawnRotation;
    private float diedAt;
    private Coroutine fall;
    private RespawnPoint bed;

    public bool IsDead => health != null && health.IsDead;
    // 죽은 지 respawnDelay가 지나 부활 입력을 받을 수 있는 상태다.
    public bool CanRespawn => IsDead && Time.time - diedAt >= respawnDelay;
    // 살아 있는 침대가 지정돼 있으면 참이다(부서졌으면 거짓).
    public bool HasBed => bed != null;
    public RespawnPoint Bed => bed;

    // 부활했을 때(침대 앞이든 시작 위치든 제자리든) 알린다.
    public event Action Respawned;
    // 침대 지정이 바뀔 때 알린다. 인자는 새 침대(지정이 풀렸으면 null)이고, removed가 참이면 지정한 침대가 사라져서 풀린 것이다.
    public event Action<RespawnPoint, bool> BedChanged;

    private void Awake()
    {
        health = GetComponent<Health>();
        inventory = GetComponent<Inventory>();
        weapons = GetComponent<WeaponEquipment>();
        movement = GetComponent<StarterAssets.ThirdPersonController>();
        inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
        controller = GetComponent<CharacterController>();
        survival = GetComponent<SurvivalStats>();
        spawnPosition = transform.position;
        startPosition = spawnPosition;
        spawnRotation = transform.rotation;
    }

    private void OnEnable() => health.Died += OnDied;

    private void OnDisable() => health.Died -= OnDied;

    private void Update()
    {
        if (!CanRespawn || Keyboard.current == null) return;
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) Respawn();
    }

    // ---- 침대 ----

    // 씬이 시작할 때의 플레이어 위치다(바뀌지 않는다). 멀티플레이 시작 위치 분산의 기준점이다.
    public Vector3 StartPosition => startPosition;

    // 침대가 없을 때 부활하는 위치를 바꾼다(멀티플레이에서 접속자마다 시작 위치를 나눌 때 쓴다).
    public void SetSpawnPoint(Vector3 position) => spawnPosition = position;

    // 침대를 부활 위치로 지정한다. 이미 지정한 침대면 false를 돌려준다.
    public bool SetBed(RespawnPoint point)
    {
        if (point == null || point == bed) return false;
        Assign(point, false);
        BedChanged?.Invoke(bed, false);
        return true;
    }

    // 저장 불러오기용이다. 알림 없이 지정을 바꾼다(null이면 시작 위치로 돌아간다).
    public void RestoreBed(RespawnPoint point) => Assign(point, true);

    private void Assign(RespawnPoint point, bool silent)
    {
        if (bed != null) bed.Removed -= OnBedRemoved;
        bed = point;
        if (bed != null) bed.Removed += OnBedRemoved;
    }

    private void OnBedRemoved()
    {
        if (bed != null) bed.Removed -= OnBedRemoved;
        bed = null;
        BedChanged?.Invoke(null, true);
    }

    // ---- 사망 ----

    private void OnDied()
    {
        diedAt = Time.time;
        DropCorpseBag();

        if (movement != null) movement.enabled = false;
        if (inputs != null)
        {
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.jump = false;
            inputs.sprint = false;
        }
        if (fall != null) StopCoroutine(fall);
        fall = StartCoroutine(Fall());
    }

    // 인벤토리 전부를 죽은 자리의 시체 가방으로 옮긴다. 비어 있으면 가방을 만들지 않는다.
    private void DropCorpseBag()
    {
        if (inventory == null || corpseBagPrefab == null || inventory.Stacks.Count == 0) return;

        // 장착 무기는 인벤토리에서 사라져도 스스로 풀리지 않으므로 먼저 해제한다(방어구는 자동으로 풀린다).
        if (weapons != null) weapons.Unequip();

        // 멀티플레이 참가자의 인벤토리는 서버가 가지고 있으므로 서버가 호스트 월드에 가방을 만든다.
        if (NetworkInventory.ReplicaActive)
        {
            NetworkInventory.Local.RequestDeathDrop(transform.position, transform.eulerAngles.y);
            return;
        }
        CreateCorpseBag(inventory, transform.position, transform.eulerAngles.y);
    }

    // source의 내용을 position의 시체 가방으로 옮긴다(서버가 참가자의 가방을 만들 때도 쓴다).
    public void CreateCorpseBag(Inventory source, Vector3 position, float yaw, ulong ownerClientId = 0)
    {
        if (corpseBagPrefab == null || source == null || source.Stacks.Count == 0) return;

        // 가방은 접속자마다 마지막 사망 기준으로 하나만 남긴다. 그 접속자의 이전 가방은 내용물째 지운다(비어서 가방을 만들지 않는 사망에서는 이 줄에 오지 않으므로 이전 가방이 남는다).
        RemoveOldCorpseBags(ownerClientId);

        GameObject bag = Instantiate(corpseBagPrefab, position, Quaternion.Euler(0f, yaw, 0f));
        BuildableInstance instance = bag.GetComponent<BuildableInstance>();
        if (instance == null) instance = bag.AddComponent<BuildableInstance>();
        instance.Initialize(CorpseBagId);

        StorageContainer storage = bag.GetComponent<StorageContainer>();
        if (storage == null) return;
        storage.OwnerClientId = ownerClientId;
        StorageTransfer.MoveAll(source, storage.Contents);
    }

    private static void RemoveOldCorpseBags(ulong ownerClientId)
    {
        // 순회 중 목록이 바뀌므로 먼저 모은다.
        var old = new System.Collections.Generic.List<GameObject>();
        foreach (StorageContainer storage in StorageContainer.Active)
        {
            BuildableInstance instance = storage.GetComponent<BuildableInstance>();
            if (instance != null && instance.BuildableId == CorpseBagId && storage.OwnerClientId == ownerClientId) old.Add(storage.gameObject);
        }
        foreach (GameObject bag in old) Destroy(bag);
    }

    // 모델을 천천히 눕힌다. 카메라 루트는 그대로라 시점은 서 있던 높이에 남는다.
    private IEnumerator Fall()
    {
        for (float t = 0f; t < fallSeconds; t += Time.deltaTime)
        {
            SetModelPitch(Mathf.Lerp(0f, fallAngle, Mathf.SmoothStep(0f, 1f, t / fallSeconds)));
            yield return null;
        }
        SetModelPitch(fallAngle);
        fall = null;
    }

    private void SetModelPitch(float pitch)
    {
        foreach (Transform root in modelRoots)
            if (root != null) root.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // 지정한 침대 앞(침대가 없거나 사라졌으면 시작 위치)으로 옮겨 되살린다.
    public void Respawn()
    {
        if (!IsDead) return;

        Vector3 position = spawnPosition;
        Quaternion rotation = spawnRotation;
        if (bed != null)
        {
            position = bed.SpawnPosition;
            rotation = bed.SpawnRotation;
        }

        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (controller != null) controller.enabled = true;
        ReviveInPlace();
    }

    // 자리를 옮기지 않고 되살린다. 죽은 채로 저장을 불러올 때 먼저 부른다(위치는 저장 파일이 정한다).
    public void ReviveInPlace()
    {
        if (!IsDead) return;

        if (fall != null)
        {
            StopCoroutine(fall);
            fall = null;
        }
        SetModelPitch(0f);
        if (movement != null) movement.enabled = true;

        health.Revive(respawnHealth);
        if (survival != null) survival.Revive(respawnSurvival);
        Respawned?.Invoke();
    }
}
