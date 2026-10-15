// 플레이어가 인벤토리의 아이템을 월드에 버리는 컴포넌트다. 인벤토리에서 빼고 앞쪽 지면에 전리품(LootPickup)으로 놓으며, 바로 다시 줍지 않도록 잠깐 줍기를 막는다.
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class ItemDropper : MonoBehaviour
{
    [SerializeField] private GameObject lootPickupPrefab;
    // 플레이어 앞으로 이만큼 떨어진 곳에 놓는다.
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float pickupDelaySeconds = 2.5f;

    private Inventory inventory;

    private void Awake() => inventory = GetComponent<Inventory>();

    // quantity개를 버린다. 가진 수량보다 많거나 프리팹이 없으면 아무것도 하지 않고 false를 돌려준다.
    public bool Drop(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0 || lootPickupPrefab == null) return false;
        // 멀티플레이 참가자는 서버가 차감하고 월드(호스트)에 놓는다.
        if (NetworkInventory.ReplicaActive)
        {
            if (inventory.GetQuantity(item) < quantity) return false;
            NetworkInventory.Local.RequestDrop(item.StableId, quantity);
            return true;
        }
        if (inventory.GetQuantity(item) < quantity) return false;
        if (!inventory.Remove(item, quantity)) return false;

        PlaceLoot(item, quantity, transform.position, transform.forward);
        return true;
    }

    // 아이템을 origin 앞쪽 지면에 전리품으로 놓는다(서버가 참가자의 버리기를 처리할 때도 쓴다).
    public void PlaceLoot(ItemDefinition item, int quantity, Vector3 origin, Vector3 forward)
    {
        if (lootPickupPrefab == null) return;
        Vector3 position = GroundPosition(origin + forward * dropDistance);
        LootPickup.Spawn(lootPickupPrefab, position, item, quantity, pickupDelaySeconds);
    }

    // 지면(지형, 바닥 콜라이더) 위로 맞춘다. 맞는 곳이 없으면 플레이어 높이를 그대로 쓴다.
    private Vector3 GroundPosition(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 1.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
            return hit.point + Vector3.up * 0.2f;
        return position;
    }
}
