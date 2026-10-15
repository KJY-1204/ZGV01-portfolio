// stableId로 ItemDefinition을 찾을 수 있게 하는 실행 시점 조회용 데이터 에셋이다. 저장 파일은 에셋 참조 대신 이 id를 저장한다.
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ZGV01/Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

    private static ItemDatabase instance;

    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ItemDatabase>("ItemDatabase");
            }
            return instance;
        }
    }

    public ItemDefinition GetByStableId(string stableId)
    {
        foreach (ItemDefinition item in items)
        {
            if (item != null && item.StableId == stableId) return item;
        }
        return null;
    }
}
