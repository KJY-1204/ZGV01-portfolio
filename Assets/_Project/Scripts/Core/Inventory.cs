// 인벤토리 슬롯 컬렉션과 아이템 추가/제거의 유일한 진입점이다. UI는 이 컴포넌트를 통해서만 아이템을 변경한다.
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private readonly List<ItemStack> stacks = new List<ItemStack>();

    public IReadOnlyList<ItemStack> Stacks => stacks;

    // 쓸 수 있는 스택(칸) 수 상한이다. 0이면 제한이 없다(플레이어). 보관함처럼 칸이 정해진 곳은 StorageTransfer가 이 값을 지켜 옮긴다.
    // Add 자체는 상한을 검사하지 않는다. 호출하는 쪽이 MaxAddable로 먼저 확인한다.
    public int MaxSlots { get; set; }

    // 참이면 이 인벤토리는 서버가 가진 원본의 복제본이다(멀티플레이 참가자). 게임 코드의 Add/Remove/Clear는 거부되고 서버가 보낸 내용을 ReplaceAll로만 받는다.
    public bool IsReplica { get; set; }

    public event Action<ItemDefinition, int> ItemAdded;
    // 아이템이 늘거나 줄거나 비워질 때마다 알린다(장비가 가진 아이템인지 다시 확인하는 데 쓴다).
    public event Action Modified;
    // 월드에서 주웠을 때(전리품, 채집)만 알린다. 제작이나 저장 불러오기로 늘어난 경우는 알리지 않는다.
    public event Action<ItemDefinition, int> ItemPickedUp;

    public void AddPickup(ItemDefinition item, int quantity)
    {
        if (IsReplica || item == null || quantity <= 0) return;
        Add(item, quantity);
        ItemPickedUp?.Invoke(item, quantity);
    }

    public void Add(ItemDefinition item, int quantity)
    {
        if (IsReplica || item == null || quantity <= 0) return;

        int remaining = quantity;
        foreach (ItemStack stack in stacks)
        {
            if (stack.Definition != item || stack.Quantity >= item.MaxStack) continue;
            int space = item.MaxStack - stack.Quantity;
            int add = Mathf.Min(space, remaining);
            stack.Quantity += add;
            remaining -= add;
            if (remaining <= 0) break;
        }

        while (remaining > 0)
        {
            int add = Mathf.Min(item.MaxStack, remaining);
            stacks.Add(new ItemStack(item, add));
            remaining -= add;
        }

        ItemAdded?.Invoke(item, quantity);
        Modified?.Invoke();
    }

    public bool Remove(ItemDefinition item, int quantity)
    {
        if (IsReplica || item == null || quantity <= 0) return false;

        int total = 0;
        foreach (ItemStack stack in stacks)
            if (stack.Definition == item) total += stack.Quantity;
        if (total < quantity) return false;

        int remaining = quantity;
        for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (stacks[i].Definition != item) continue;
            int take = Mathf.Min(stacks[i].Quantity, remaining);
            stacks[i].Quantity -= take;
            remaining -= take;
            if (stacks[i].Quantity <= 0) stacks.RemoveAt(i);
        }
        Modified?.Invoke();
        return true;
    }

    // 상한을 넘지 않고 지금 더 담을 수 있는 item의 수량이다(제한이 없으면 int.MaxValue).
    public int MaxAddable(ItemDefinition item)
    {
        if (item == null) return 0;
        if (MaxSlots <= 0) return int.MaxValue;

        int space = 0;
        foreach (ItemStack stack in stacks)
            if (stack.Definition == item && stack.Quantity < item.MaxStack) space += item.MaxStack - stack.Quantity;

        int freeSlots = Mathf.Max(0, MaxSlots - stacks.Count);
        long total = (long)space + (long)freeSlots * item.MaxStack;
        return (int)Mathf.Min(total, int.MaxValue);
    }

    public void Clear()
    {
        if (IsReplica) return;
        stacks.Clear();
        Modified?.Invoke();
    }

    // 서버가 보낸 내용으로 통째로 바꾼다(복제본이 서버 상태를 받는 유일한 길이다). 알 수 없는 항목은 호출하는 쪽이 걸러서 넘긴다.
    public void ReplaceAll(IEnumerable<(ItemDefinition item, int quantity)> contents)
    {
        stacks.Clear();
        foreach ((ItemDefinition item, int quantity) in contents)
        {
            if (item == null || quantity <= 0) continue;
            int remaining = quantity;
            while (remaining > 0)
            {
                int add = Mathf.Min(item.MaxStack, remaining);
                stacks.Add(new ItemStack(item, add));
                remaining -= add;
            }
        }
        Modified?.Invoke();
    }

    // 서버가 확정한 획득(채집 등)을 복제본에서 "주웠다" 알림으로 낸다(획득 표시용). 내용은 건드리지 않는다.
    public void NotifyPickedUp(ItemDefinition item, int quantity)
    {
        if (item != null && quantity > 0) ItemPickedUp?.Invoke(item, quantity);
    }

    public int GetQuantity(ItemDefinition item)
    {
        int total = 0;
        foreach (ItemStack stack in stacks)
            if (stack.Definition == item) total += stack.Quantity;
        return total;
    }
}
