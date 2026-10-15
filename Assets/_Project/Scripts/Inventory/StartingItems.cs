// 게임을 시작할 때 인벤토리에 정해 둔 아이템을 지급한다. 테스트 씬에서 총과 탄약을 매번 직접 넣지 않기 위한 설정이라 에디터와 개발 빌드에서만 지급하고, 릴리스 빌드는 아이템 없이 시작한다.
using System;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class StartingItems : MonoBehaviour
{
    [Serializable]
    private struct Grant
    {
        public ItemDefinition item;
        public int quantity;
    }

    [SerializeField] private Grant[] grants = Array.Empty<Grant>();

    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Inventory inventory = GetComponent<Inventory>();
        foreach (Grant grant in grants) inventory.Add(grant.item, grant.quantity);
#endif
    }
}
