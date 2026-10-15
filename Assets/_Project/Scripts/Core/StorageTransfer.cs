// 두 인벤토리 사이로 아이템을 옮기는 규칙이다. 받는 쪽 칸 상한과 보내는 쪽 보유량을 지켜 옮길 수 있는 만큼만 옮긴다.
using UnityEngine;

public static class StorageTransfer
{
    // from의 모든 아이템을 to로 옮긴다(받는 쪽 상한이 있으면 들어가는 만큼만). 옮긴 총수량을 돌려준다.
    public static int MoveAll(Inventory from, Inventory to)
    {
        if (from == null || to == null || from == to) return 0;

        // 옮기는 동안 목록이 바뀌므로 먼저 스냅샷을 만든다.
        var snapshot = new System.Collections.Generic.List<(ItemDefinition item, int quantity)>();
        foreach (ItemStack stack in from.Stacks)
            if (stack.Definition != null) snapshot.Add((stack.Definition, stack.Quantity));

        int total = 0;
        foreach ((ItemDefinition item, int quantity) entry in snapshot)
            total += Move(from, to, entry.item, entry.quantity);
        return total;
    }

    // 실제로 옮긴 수량을 돌려준다(0이면 아무것도 옮기지 않았다).
    public static int Move(Inventory from, Inventory to, ItemDefinition item, int quantity)
    {
        if (from == null || to == null || from == to || item == null || quantity <= 0) return 0;

        int amount = Mathf.Min(quantity, from.GetQuantity(item), to.MaxAddable(item));
        if (amount <= 0) return 0;

        from.Remove(item, amount);
        to.Add(item, amount);
        return amount;
    }
}
