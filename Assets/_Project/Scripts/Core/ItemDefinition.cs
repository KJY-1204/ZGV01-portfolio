// 아이템의 정적 정의를 담는 데이터 에셋이다. 런타임 수량이나 소유 상태는 여기에 저장하지 않는다.
using UnityEngine;

public enum ItemCategory
{
    Resource,
    Ammo,
    Consumable,
    Weapon,
    Equipment,
    CraftingMaterial,
    BuildingMaterial,
}

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "ZGV01/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string stableId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemCategory category;
    [SerializeField] private int maxStack = 1;
    // 월드에 떨어졌을 때 보일 3D 모델이다(없으면 아이콘을 세워 보여 준다).
    [SerializeField] private GameObject worldPrefab;

    public string StableId => stableId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public ItemCategory Category => category;
    public int MaxStack => maxStack;
    public GameObject WorldPrefab => worldPrefab;
}
