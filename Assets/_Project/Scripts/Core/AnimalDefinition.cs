// 사냥감 동물 한 종류의 정적 정의다(이동 속도, 경계 거리, 도주 시간, 고기 드랍, 애니메이터 상태 이름). 런타임 상태는 담지 않는다.
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalDefinition", menuName = "ZGV01/Animals/Animal Definition")]
public class AnimalDefinition : ScriptableObject
{
    [SerializeField] private string stableId;
    [SerializeField] private string displayName;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float runSpeed = 4.5f;
    // 배회할 때 현재 위치에서 고르는 목표까지의 최대 거리다.
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private Vector2 idleSeconds = new Vector2(2f, 6f);
    [SerializeField] private Vector2 wanderSeconds = new Vector2(3f, 7f);

    [Header("Fear")]
    // 플레이어가 이 거리 안으로 들어오면 놀라 도주한다. 0이면 피해를 입었을 때만 도주한다.
    [SerializeField] private float alertDistance = 9f;
    // 위협이 사라진 뒤에도 이 시간 동안은 계속 도망친다.
    [SerializeField] private float fleeSeconds = 5f;
    // 도주할 때 한 번에 고르는 목표까지의 거리다.
    [SerializeField] private float fleeDistance = 14f;

    [Header("Loot")]
    [SerializeField] private ItemDefinition meatItem;
    [SerializeField] private int meatMin = 1;
    [SerializeField] private int meatMax = 2;
    // 고기와 함께 떨어뜨리는 가죽이다(아이템이 없으면 떨어뜨리지 않는다).
    [SerializeField] private ItemDefinition hideItem;
    [SerializeField] private int hideMin = 1;
    [SerializeField] private int hideMax = 1;
    // 죽은 뒤 시체를 치울 때까지의 시간이다.
    [SerializeField] private float corpseSeconds = 60f;

    [Header("Animator state names")]
    [SerializeField] private string idleState = "idle";
    [SerializeField] private string walkState = "walk";
    [SerializeField] private string runState = "run";
    [SerializeField] private string eatState = "eat";
    [SerializeField] private string dieState = "die";
    // 대기 중에 풀을 뜯는(eat) 동작을 섞는 확률이다. 0이면 쓰지 않는다.
    [SerializeField, Range(0f, 1f)] private float eatChance = 0.3f;

    public string StableId => stableId;
    public string DisplayName => displayName;
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float WanderRadius => wanderRadius;
    public Vector2 IdleSeconds => idleSeconds;
    public Vector2 WanderSeconds => wanderSeconds;
    public float AlertDistance => alertDistance;
    public float FleeSeconds => fleeSeconds;
    public float FleeDistance => fleeDistance;
    public ItemDefinition MeatItem => meatItem;
    public int MeatMin => meatMin;
    public int MeatMax => meatMax;
    public ItemDefinition HideItem => hideItem;
    public int HideMin => hideMin;
    public int HideMax => hideMax;
    public float CorpseSeconds => corpseSeconds;
    public string IdleState => idleState;
    public string WalkState => walkState;
    public string RunState => runState;
    public string EatState => eatState;
    public string DieState => dieState;
    public float EatChance => eatChance;

    // 고기 수량을 [min, max] 안에서 굴린다. rangeInclusive는 (최소, 최대) 사이 정수를 돌려준다(테스트에서 값을 고정한다).
    public int RollMeat(Func<int, int, int> rangeInclusive) => Roll(meatItem, meatMin, meatMax, rangeInclusive);

    public int RollMeat() => RollMeat((min, max) => UnityEngine.Random.Range(min, max + 1));

    // 가죽 수량도 같은 방식으로 굴린다.
    public int RollHide(Func<int, int, int> rangeInclusive) => Roll(hideItem, hideMin, hideMax, rangeInclusive);

    public int RollHide() => RollHide((min, max) => UnityEngine.Random.Range(min, max + 1));

    private static int Roll(ItemDefinition item, int minimum, int maximum, Func<int, int, int> rangeInclusive)
    {
        if (item == null) return 0;
        int min = Math.Max(0, minimum);
        int max = Math.Max(min, maximum);
        return rangeInclusive(min, max);
    }

    public AnimalBrain CreateBrain() => new AnimalBrain(
        alertDistance, fleeSeconds, idleSeconds.x, idleSeconds.y, wanderSeconds.x, wanderSeconds.y,
        (min, max) => UnityEngine.Random.Range(min, max));
}
