// 동물이나 맹수가 죽을 때 고기와 가죽을 월드에 떨어뜨리는 공용 도우미다. 둘은 각각 LootPickup이고 겹치지 않게 조금 떨어뜨려 놓는다.
using UnityEngine;

public static class AnimalLoot
{
    // 고기와 가죽 사이 간격(m)이다.
    private const float Spacing = 0.5f;

    public static void Drop(AnimalDefinition definition, GameObject lootPickupPrefab, Transform animal)
    {
        if (definition == null || lootPickupPrefab == null) return;

        int meat = definition.RollMeat();
        int hide = definition.RollHide();
        Vector3 center = animal.position + Vector3.up * 0.2f;
        Vector3 side = animal.right * Spacing * 0.5f;

        bool both = meat > 0 && hide > 0;
        if (meat > 0) Spawn(lootPickupPrefab, definition.MeatItem, meat, both ? center - side : center);
        if (hide > 0) Spawn(lootPickupPrefab, definition.HideItem, hide, both ? center + side : center);
    }

    private static void Spawn(GameObject prefab, ItemDefinition item, int quantity, Vector3 position)
    {
        LootPickup.Spawn(prefab, position, item, quantity);
    }
}
