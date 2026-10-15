// 건축물의 정적 정의다. 필요한 자원과 스폰할 프리팹을 데이터로 관리한다.
using System;
using UnityEngine;

// 건축 UI 탭 분류. 조각 타입이 늘어나면(문, 지붕, 장식 등) 여기 추가한다.
public enum BuildableCategory
{
    Wall,
    Floor,
    Furniture,
    Defense,
    Door,
    // 위층(1층 이상)에만 놓을 수 있다. 아래에서도 보이도록 양면으로 만든다.
    Roof,
    Window,
}

// 배치 격자에서 조각이 놓이는 자리다. 칸 중심에 바닥을, 칸 경계에 벽을, 꼭짓점에 기둥과 이음 조각을 놓으면 서로 맞물린다.
public enum BuildSnapMode
{
    // 칸 중심(격자 배수). 바닥, 가구 등.
    Center,
    // 칸 경계의 중점. 조각의 긴 변(로컬 x)을 따라 격자 배수에, 수직 방향은 반 칸 어긋난 자리에 놓인다. 벽.
    Edge,
    // 칸의 꼭짓점(두 축 모두 반 칸 어긋난 자리). 기둥, 모서리/T/십자 벽, 긴 벽.
    Vertex,
}

// 벽에 붙여 놓는 조각이 어느 면을 앞으로 삼는지다. 벽 가까이에 조준하면 뒷면이 벽에 닿고 앞면이 벽에서 멀어지는 쪽을 보게 놓인다.
public enum WallAttach
{
    None,
    // 로컬 +Z가 앞이다(가구 대부분).
    FrontPlusZ,
    // 로컬 -Z가 앞이다(벽에 기대는 사다리).
    FrontMinusZ,
}

[CreateAssetMenu(fileName = "BuildableDefinition", menuName = "ZGV01/World/Buildable Definition")]
public class BuildableDefinition : ScriptableObject
{
    [Serializable]
    public class Cost
    {
        public ItemDefinition item;
        public int quantity = 1;
    }

    [SerializeField] private string buildableId;
    [SerializeField] private string displayName;
    [SerializeField] private BuildableCategory category;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Cost[] costs = Array.Empty<Cost>();
    // 배치 격자 크기(m)다. 0이면 건축 시스템의 기본 격자를 쓴다(가구처럼 촘촘히 놓을 조각에 쓴다).
    [SerializeField] private float snapSize;
    [SerializeField] private BuildSnapMode snapMode;
    [SerializeField] private WallAttach wallAttach;
    // 좀비가 부술 수 있는 내구도다. 0이면 부서지지 않는다(바닥, 가구 등).
    [SerializeField] private float maxHealth;
    [SerializeField] private SoundGroup hitSound = new SoundGroup();
    [SerializeField] private SoundGroup breakSound = new SoundGroup();

    public string BuildableId => buildableId;
    public string DisplayName => displayName;
    public BuildableCategory Category => category;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
    public Cost[] Costs => costs;
    public float SnapSize => snapSize;
    public BuildSnapMode SnapMode => snapMode;
    public WallAttach WallAttach => wallAttach;
    public float MaxHealth => maxHealth;

    // 수리할 때 한 번에 하나씩 쓰는 재료다(비용 목록의 첫 번째 재료).
    public ItemDefinition RepairItem
    {
        get
        {
            foreach (Cost cost in costs)
                if (cost != null && cost.item != null) return cost.item;
            return null;
        }
    }
    public SoundGroup HitSound => hitSound;
    public SoundGroup BreakSound => breakSound;
}
