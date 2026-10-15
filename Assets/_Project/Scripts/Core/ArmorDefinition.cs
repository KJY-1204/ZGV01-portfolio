// 방어구 한 종류의 정적 정의다. 어느 슬롯에 입고 받는 피해를 얼마나 줄이는지만 정한다. 외형과 내구도는 없다.
using UnityEngine;

public enum ArmorSlot
{
    Head,
    Body,
    Legs,
}

[CreateAssetMenu(fileName = "ArmorDefinition", menuName = "ZGV01/Items/Armor Definition")]
public class ArmorDefinition : ItemDefinition
{
    [SerializeField] private ArmorSlot slot;
    // 이 방어구가 받는 피해에서 깎는 비율(0.1이면 10%)이다. 장착한 방어구끼리는 합산한다.
    [SerializeField, Range(0f, 0.6f)] private float damageReduction = 0.1f;

    public ArmorSlot Slot => slot;
    public float DamageReduction => damageReduction;

    public static string SlotLabel(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Head: return "머리";
            case ArmorSlot.Body: return "몸통";
            default: return "다리";
        }
    }
}
