// 보관함 건축물에 붙는 컴포넌트다. 자기 Inventory(칸 수 상한 포함)를 들고, 내용물 저장/복원과 파괴 시 전리품 드랍을 맡는다. 시체 가방도 이 컴포넌트를 쓴다.
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class StorageContainer : MonoBehaviour
{
    // 상호작용 안내가 가까운 보관함을 찾을 수 있게 활성 인스턴스를 모아 둔다.
    private static readonly List<StorageContainer> active = new List<StorageContainer>();
    public static IReadOnlyList<StorageContainer> Active => active;

    [SerializeField] private string displayName = "보관함";
    // 0이면 칸 제한이 없다(시체 가방).
    [SerializeField] private int slotCount = 24;
    // 한 번이라도 물건이 들어갔다가 비면 스스로 사라진다(시체 가방).
    [SerializeField] private bool destroyWhenEmpty;
    // 0보다 크면 이 현실 시간(초)이 지나 못 찾으면 내용물째 사라진다(시체 가방). 0이면 사라지지 않는다.
    [SerializeField] private float lifetimeSeconds;
    [SerializeField] private GameObject lootPickupPrefab;

    // 수명이 다해 사라질 때 이름과 함께 알린다(HUD가 알림을 띄운다). 비었거나 철거돼 사라지는 경우에는 알리지 않는다.
    public static event Action<string> Expired;
    // 멀티플레이 서버에서 다른 접속자의 시체 가방이 수명이 다해 사라질 때 소유자 접속자 id와 이름을 알린다(그 접속자에게 알림을 보내는 쪽이 구독한다).
    public static event Action<ulong, string> OwnerExpired;

    private Inventory contents;
    private bool hadItems;
    private LifetimeTimer lifetime;

    public Inventory Contents
    {
        get
        {
            if (contents == null) Setup();
            return contents;
        }
    }

    // 남은 수명(초)이다. 수명이 없는 보관함은 -1이다.
    public float RemainingSeconds => lifetime != null ? lifetime.Remaining : -1f;
    public string DisplayName => displayName;
    public int SlotCount => slotCount;
    // 시체 가방을 만든 접속자 id다(멀티플레이에서 접속자마다 가방을 하나만 두는 데 쓴다). 호스트와 싱글플레이는 0이다.
    public ulong OwnerClientId { get; set; }

    // 이 건축물의 월드 id다(멀티플레이 요청이 보관함을 가리킬 때 쓴다). 설치되지 않은 미리보기는 빈 문자열이다.
    public string WorldId
    {
        get
        {
            BuildableInstance instance = GetComponent<BuildableInstance>();
            return instance != null ? instance.WorldId : "";
        }
    }

    private void Awake() => Setup();
    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    private void Setup()
    {
        if (contents != null) return;
        contents = GetComponent<Inventory>();
        contents.MaxSlots = slotCount;
        // 멀티플레이 참가자 월드의 보관함은 서버 내용을 받아 보여 주는 복제본이다(직접 바꾸지 않는다).
        contents.IsReplica = MultiplayerSession.IsClientOnly;
        contents.Modified += OnContentsModified;
        if (lifetimeSeconds > 0f) lifetime = new LifetimeTimer(lifetimeSeconds);
    }

    // 게임 시간이 아니라 현실 시간으로 센다(일시정지나 시간 배속과 무관).
    private void Update()
    {
        if (lifetime == null || !lifetime.Tick(Time.unscaledDeltaTime)) return;
        // 참가자 쪽 복제본은 남은 시간을 보여 주기만 한다. 지우는 일은 서버가 하고 서버가 지우면 복제본도 사라진다.
        if (contents.IsReplica) return;

        if (OwnerClientId == 0) Expired?.Invoke(displayName);
        else OwnerExpired?.Invoke(OwnerClientId, displayName);
        // 전리품으로 떨어뜨리지 않고 내용물째 사라진다.
        contents.Clear();
        Destroy(gameObject);
    }

    // 저장 불러오기용이다. 저장된 남은 초로 수명을 되돌린다(수명이 없는 보관함이면 무시한다).
    public void RestoreRemainingSeconds(float seconds)
    {
        if (lifetime == null) Setup();
        if (lifetime != null) lifetime.SetRemaining(seconds);
    }

    private void OnDestroy()
    {
        if (contents != null) contents.Modified -= OnContentsModified;
    }

    private void OnContentsModified()
    {
        if (contents.Stacks.Count > 0) hadItems = true;
        else if (destroyWhenEmpty && hadItems && !contents.IsReplica) Destroy(gameObject);
    }

    // 저장 불러오기용이다. 지금 내용물을 비우고 목록대로 채운다(칸 상한은 무시하지 않고 그대로 넘치지 않는 만큼만 들어간다).
    public void Restore(IEnumerable<ItemStack> stacks)
    {
        Inventory inventory = Contents;
        inventory.Clear();
        if (stacks == null) return;
        foreach (ItemStack stack in stacks)
        {
            if (stack == null || stack.Definition == null) continue;
            int amount = Mathf.Min(stack.Quantity, inventory.MaxAddable(stack.Definition));
            inventory.Add(stack.Definition, amount);
        }
    }

    // 건축물이 부서지거나 철거될 때 내용물을 그 자리에 전리품으로 흩뿌린다. 여러 번 불려도 한 번만 떨어뜨린다.
    public void DropContents()
    {
        Inventory inventory = Contents;
        // 참가자 쪽 복제본은 전리품을 만들지 않는다(서버가 네트워크 전리품으로 떨어뜨린다).
        if (inventory.IsReplica) return;
        if (lootPickupPrefab == null)
        {
            inventory.Clear();
            return;
        }

        List<ItemStack> snapshot = new List<ItemStack>();
        foreach (ItemStack stack in inventory.Stacks) snapshot.Add(new ItemStack(stack.Definition, stack.Quantity));
        inventory.Clear();

        Vector3 center = transform.position + Vector3.up * 0.3f;
        // 묶음이 많으면 원을 키워 겹치지 않게 한다.
        float radius = 0.6f + 0.04f * snapshot.Count;
        for (int i = 0; i < snapshot.Count; i++)
        {
            float angle = snapshot.Count > 1 ? i * Mathf.PI * 2f / snapshot.Count : 0f;
            Vector3 offset = snapshot.Count > 1 ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius : Vector3.zero;
            // 멀티플레이 호스트에서는 전리품이 참가자에게도 보이도록 네트워크 오브젝트로 소환된다.
            LootPickup.Spawn(lootPickupPrefab, center + offset, snapshot[i].Definition, snapshot[i].Quantity);
        }
    }
}
