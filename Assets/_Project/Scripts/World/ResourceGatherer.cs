// 인벤토리에 자원 노드의 채집 결과를 담는 컴포넌트다. 어떤 노드를 채집할지는 상호작용 안내(InteractionFocus)가 고른다.
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class ResourceGatherer : MonoBehaviour
{
    [SerializeField] private float gatherRange = 2.5f;

    public float GatherRange => gatherRange;

    public bool GatherFrom(ResourceNode node)
    {
        if (node == null) return false;
        // 멀티플레이 참가자는 서버에 채집을 요청한다(아이템은 서버가 주고, 노드 모습은 서버 응답으로 줄어든다).
        if (NetworkInventory.ReplicaActive) return node.HasYield && NetworkInventory.Local.RequestGather(node.transform.position, node.TerrainTreeIndex);
        return node.TryGather(GetComponent<Inventory>());
    }
}
