// 자원 노드의 정적 정의다. 어떤 아이템을 한 번에 얼마나, 최대 얼마나, 얼마 만에 재생하는지 담는다.
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceNodeDefinition", menuName = "ZGV01/World/Resource Node Definition")]
public class ResourceNodeDefinition : ScriptableObject
{
    [SerializeField] private ItemDefinition yieldItem;
    [SerializeField] private int yieldPerGather = 1;
    [SerializeField] private int maxYield = 5;
    [SerializeField] private float respawnSeconds = 20f;
    // 총알이 이 노드에 맞았을 때 쓸 표면 종류다. 노드 오브젝트에 표식이 없으면 이 값으로 자동 표시한다.
    [SerializeField] private ImpactSurface impactSurface = ImpactSurface.Wood;
    // 채집에 성공했을 때 노드 위치에서 나는 소리다.
    [SerializeField] private SoundGroup gatherSounds = new SoundGroup();
    // 마지막 수량까지 채집해 노드가 사라질 때 나는 소리(그룹의 클립을 모두 겹쳐 재생한다)와 연기 이펙트다.
    [SerializeField] private SoundGroup depleteSounds = new SoundGroup();
    [SerializeField] private GameObject depleteEffect;
    // 채집에 성공할 때마다 확률로 함께 나오는 보너스 아이템이다(비우면 없음).
    [SerializeField] private ItemDefinition bonusItem;
    [SerializeField, Range(0f, 1f)] private float bonusChance;
    // 참이면 이미 그 아이템을 가진 플레이어에게는 보너스가 나오지 않는다(재생되는 상자에서 쌓이는 것을 막는다).
    [SerializeField] private bool bonusSkipIfOwned = true;

    public ImpactSurface ImpactSurface => impactSurface;
    public SoundGroup GatherSounds => gatherSounds;
    public SoundGroup DepleteSounds => depleteSounds;
    public GameObject DepleteEffect => depleteEffect;
    public ItemDefinition BonusItem => bonusItem;
    public float BonusChance => bonusChance;
    public bool BonusSkipIfOwned => bonusSkipIfOwned;
    public ItemDefinition YieldItem => yieldItem;
    public int YieldPerGather => yieldPerGather;
    public int MaxYield => maxYield;
    public float RespawnSeconds => respawnSeconds;
}
