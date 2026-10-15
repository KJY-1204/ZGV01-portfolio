// 월드에 배치된 자원 노드의 런타임 상태다. 채집하면 남은 채집량이 줄고, 고갈되면 일정 시간 뒤 재생한다.
using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [SerializeField] private ResourceNodeDefinition definition;

    // 상호작용 안내가 가까운 노드를 빠르게 찾을 수 있게 활성 노드를 모아 둔다.
    private static readonly List<ResourceNode> active = new List<ResourceNode>();
    public static IReadOnlyList<ResourceNode> Active => active;

    private int remainingYield;
    private float respawnAt;
    // 사라진 동안 꺼 둔 렌더러/콜라이더(다시 나타날 때 원래 켜져 있던 것만 켠다).
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Collider> hiddenColliders = new List<Collider>();

    // 채집에 성공했을 때 얻은 수량과 함께 알린다(소리 같은 표현이 구독한다).
    public event Action<int> Gathered;
    // 마지막 수량까지 채집돼 사라질 때, 재생 시간이 지나 다시 나타날 때 알린다.
    public event Action Depleted;
    public event Action Respawned;
    public ResourceNodeDefinition Definition => definition;

    // 지형 나무 대역이면 그 나무의 번호(아니면 -1)다. 멀티플레이 채집 요청이 어느 나무인지 서버에 알리는 데 쓴다.
    public int TerrainTreeIndex { get; set; } = -1;
    // 어떤 노드든 마지막 수량까지 채집돼 사라질 때 알린다(서버가 모든 접속자에게 알리는 데 쓴다).
    public static event Action<ResourceNode> AnyDepleted;

    public bool HasYield => remainingYield > 0;
    public int RemainingYield => remainingYield;
    public string Label => definition != null && definition.YieldItem != null ? definition.YieldItem.DisplayName : name;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    private void Awake()
    {
        remainingYield = definition != null ? definition.MaxYield : 0;

        // 오브젝트에 직접 단 표식이 있으면 그것을 존중하고, 없을 때만 정의의 표면으로 표시한다.
        if (definition != null && GetComponentInParent<ImpactSurfaceMarker>() == null)
            gameObject.AddComponent<ImpactSurfaceMarker>().Initialize(definition.ImpactSurface);

        if (GetComponent<ResourceNodeFeedback>() == null) gameObject.AddComponent<ResourceNodeFeedback>();
    }

    // 코드로 만든 노드(지형 나무 대역)를 정의와 남은 채집량으로 다시 쓸 수 있게 되돌린다. 숨겨 둔 렌더러/콜라이더도 복구한다.
    public void InitializeRuntime(ResourceNodeDefinition newDefinition, int remaining)
    {
        definition = newDefinition;
        remainingYield = definition != null ? Mathf.Clamp(remaining, 0, definition.MaxYield) : 0;
        SetPresent(true);
        if (definition != null && GetComponentInParent<ImpactSurfaceMarker>() == null)
            gameObject.AddComponent<ImpactSurfaceMarker>().Initialize(definition.ImpactSurface);
    }

    private void Update()
    {
        if (definition != null && remainingYield <= 0 && Time.time >= respawnAt)
        {
            remainingYield = definition.MaxYield;
            SetPresent(true);
            Respawned?.Invoke();
        }
    }

    public bool TryGather(Inventory inventory)
    {
        if (definition == null || remainingYield <= 0 || inventory == null) return false;

        int amount = Mathf.Min(definition.YieldPerGather, remainingYield);
        inventory.AddPickup(definition.YieldItem, amount);
        RollBonus(inventory);
        remainingYield -= amount;
        Gathered?.Invoke(amount);

        if (remainingYield <= 0)
        {
            respawnAt = Time.time + definition.RespawnSeconds;
            // 효과가 사라지기 전의 모습(위치/크기)을 읽을 수 있게 먼저 알린 뒤 숨긴다.
            Depleted?.Invoke();
            SetPresent(false);
            AnyDepleted?.Invoke(this);
        }

        return true;
    }

    // 멀티플레이 참가자 쪽: 서버가 이 노드가 다 채집됐다고 알렸을 때 남은 수량과 상관없이 사라진 모습으로 만든다.
    public void ApplyRemoteDepleted()
    {
        if (definition == null || remainingYield <= 0) return;

        Gathered?.Invoke(remainingYield);
        remainingYield = 0;
        respawnAt = Time.time + definition.RespawnSeconds;
        Depleted?.Invoke();
        SetPresent(false);
    }

    // 멀티플레이 참가자 쪽 노드를 서버가 확정한 채집대로 줄인다(아이템은 서버가 따로 준다). 모습과 재생 시간만 갱신한다.
    public void ApplyRemoteGather()
    {
        if (definition == null || remainingYield <= 0) return;

        int amount = Mathf.Min(definition.YieldPerGather, remainingYield);
        remainingYield -= amount;
        Gathered?.Invoke(amount);
        if (remainingYield > 0) return;

        respawnAt = Time.time + definition.RespawnSeconds;
        Depleted?.Invoke();
        SetPresent(false);
    }

    // 보너스 아이템이 설정돼 있으면 채집할 때마다 한 번 굴려 당첨되면 인벤토리에 넣는다.
    private void RollBonus(Inventory inventory)
    {
        ItemDefinition bonus = definition.BonusItem;
        if (bonus == null) return;
        bool owned = inventory.GetQuantity(bonus) > 0;
        if (LampFaultPattern.BonusDrops(definition.BonusChance, UnityEngine.Random.value, owned, definition.BonusSkipIfOwned))
            inventory.AddPickup(bonus, 1);
    }

    // 노드를 눈에 보이지 않고 닿지 않는 상태로 숨기거나 되돌린다. 컴포넌트는 켜 둬야 재생 시간을 셀 수 있다.
    private void SetPresent(bool present)
    {
        if (present)
        {
            foreach (Renderer renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            foreach (Collider collider in hiddenColliders) if (collider != null) collider.enabled = true;
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            return;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }
        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled) continue;
            collider.enabled = false;
            hiddenColliders.Add(collider);
        }
    }
}
