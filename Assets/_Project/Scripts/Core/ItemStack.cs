// 인벤토리 한 슬롯의 런타임 상태다. 정적 정의(ItemDefinition)와 분리해서 수량만 들고 있는다.
using System;

[Serializable]
public class ItemStack
{
    public ItemDefinition Definition;
    public int Quantity;

    public ItemStack(ItemDefinition definition, int quantity)
    {
        Definition = definition;
        Quantity = quantity;
    }
}
