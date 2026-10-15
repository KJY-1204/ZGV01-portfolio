// Inventory.ItemAdded 이벤트를 콘솔에 출력하는 테스트 전용 표현 컴포넌트다.
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class InventoryDebugPrinter : MonoBehaviour
{
    private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        inventory.ItemAdded += OnItemAdded;
    }

    private void OnDestroy()
    {
        inventory.ItemAdded -= OnItemAdded;
    }

    private void OnItemAdded(ItemDefinition item, int quantity)
    {
        Debug.Log($"{name} picked up {quantity}x {item.DisplayName}");
    }
}
