// 플레이어가 장착한 방어구(머리, 몸통, 다리)를 들고 합산한 피해 감소율을 Health에 반영하는 도메인 컴포넌트다. 방어구 아이템은 인벤토리에 남아 있어야 효과가 있다.
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(Health))]
public class Equipment : MonoBehaviour
{
    // 방어구를 모두 껴도 이 이상은 줄이지 못한다.
    public const float MaxReduction = 0.6f;

    private static readonly int SlotCount = Enum.GetValues(typeof(ArmorSlot)).Length;

    private readonly ArmorDefinition[] slots = new ArmorDefinition[SlotCount];
    private Inventory inventory;
    private Health health;

    // 장착이 바뀌거나 가진 방어구가 사라져 풀릴 때 알린다(UI가 구독한다).
    public event Action Changed;

    // 장착한 방어구가 인벤토리에 있는 것만 센 피해 감소율(상한 적용)이다.
    public float TotalReduction
    {
        get
        {
            float total = 0f;
            foreach (ArmorDefinition armor in slots)
                if (armor != null && IsOwned(armor)) total += armor.DamageReduction;
            return Mathf.Min(total, MaxReduction);
        }
    }

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        health = GetComponent<Health>();
        inventory.Modified += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.Modified -= Refresh;
    }

    public ArmorDefinition Get(ArmorSlot slot) => slots[(int)slot];

    public bool IsEquipped(ArmorDefinition armor) => armor != null && slots[(int)armor.Slot] == armor;

    // item이 방어구면 그 정의를, 아니면 null을 돌려준다.
    public ArmorDefinition FindArmor(ItemDefinition item) => item as ArmorDefinition;

    // 장착하려면 인벤토리에 있어야 한다. 같은 슬롯에 입고 있던 방어구는 바뀐다.
    public bool TryEquip(ArmorDefinition armor)
    {
        if (armor == null || !IsOwned(armor)) return false;

        slots[(int)armor.Slot] = armor;
        Refresh();
        Changed?.Invoke();
        return true;
    }

    public void Unequip(ArmorSlot slot)
    {
        if (slots[(int)slot] == null) return;

        slots[(int)slot] = null;
        Refresh();
        Changed?.Invoke();
    }

    // 저장 불러오기용이다. 지금 장착을 모두 풀고 목록에서 가진 것만 장착한다.
    public void RestoreState(IEnumerable<ArmorDefinition> armors)
    {
        for (int i = 0; i < slots.Length; i++) slots[i] = null;
        if (armors != null)
        {
            foreach (ArmorDefinition armor in armors)
                if (armor != null && IsOwned(armor)) slots[(int)armor.Slot] = armor;
        }
        Refresh();
        Changed?.Invoke();
    }

    private bool IsOwned(ArmorDefinition armor) => inventory != null && inventory.GetQuantity(armor) > 0;

    // 인벤토리에서 사라진 방어구는 풀고, 합산한 감소율을 Health에 적용한다.
    private void Refresh()
    {
        bool released = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || IsOwned(slots[i])) continue;
            slots[i] = null;
            released = true;
        }

        health.DamageReduction = TotalReduction;
        if (released) Changed?.Invoke();
    }
}
