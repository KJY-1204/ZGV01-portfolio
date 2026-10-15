// 좀비 처치 시 어떤 아이템이 얼마나 드랍될지 가중치 기반으로 정의하는 데이터 에셋이다.
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTable", menuName = "ZGV01/Items/Drop Table")]
public class DropTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemDefinition item;
        public float weight = 1f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public bool TryRoll(out ItemDefinition item, out int quantity)
    {
        item = null;
        quantity = 0;
        if (entries == null || entries.Length == 0) return false;

        float totalWeight = 0f;
        foreach (Entry e in entries) totalWeight += Mathf.Max(0f, e.weight);
        if (totalWeight <= 0f) return false;

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        foreach (Entry e in entries)
        {
            cumulative += Mathf.Max(0f, e.weight);
            if (roll <= cumulative)
            {
                item = e.item;
                quantity = UnityEngine.Random.Range(e.minQuantity, e.maxQuantity + 1);
                return item != null;
            }
        }
        return false;
    }
}
