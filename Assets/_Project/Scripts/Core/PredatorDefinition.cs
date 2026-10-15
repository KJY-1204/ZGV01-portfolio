// 육식 맹수 한 종류의 정적 정의다. 이동 속도, 배회, 고기, 애니메이터 상태 이름은 AnimalDefinition을 그대로 쓰고(달리기 속도가 추격 속도다), 감지와 공격 수치를 더한다.
using UnityEngine;

[CreateAssetMenu(fileName = "PredatorDefinition", menuName = "ZGV01/Animals/Predator Definition")]
public class PredatorDefinition : AnimalDefinition
{
    [Header("Predator: detect & chase")]
    // 플레이어가 이 거리 안에 들어오면 추격을 시작한다.
    [SerializeField] private float detectDistance = 16f;
    // 이 거리보다 멀어진 채 포기 시간이 지나면 추격을 그만둔다.
    [SerializeField] private float loseDistance = 35f;
    [SerializeField] private float loseSeconds = 6f;

    [Header("Predator: attack")]
    // 몸 중심에서 목표까지의 거리가 이 안이면 공격을 시작한다.
    [SerializeField] private float attackRange = 2.4f;
    [SerializeField] private float attackDamage = 15f;
    // 공격이 시작된 시점부터 다음 공격이 가능해질 때까지의 시간이다.
    [SerializeField] private float attackCooldown = 1.8f;
    // 공격 동작이 끝나기까지의 시간과, 시작 뒤 피해가 들어가는 순간이다(공격 애니메이션의 타격에 맞춘다).
    [SerializeField] private float attackDuration = 1.0f;
    [SerializeField] private float attackHitDelay = 0.4f;
    // 공격 때마다 이 중 하나를 무작위로 재생한다.
    [SerializeField] private string[] attackStates = { "attack A", "attack B" };
    // 추격을 시작할 때 포효하는 애니메이션 상태다(비우면 쓰지 않는다).
    [SerializeField] private string roarState = "roar";
    [SerializeField] private float roarSeconds = 1.0f;

    public float DetectDistance => detectDistance;
    public float LoseDistance => loseDistance;
    public float LoseSeconds => loseSeconds;
    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public float AttackDuration => attackDuration;
    public float AttackHitDelay => attackHitDelay;
    public string[] AttackStates => attackStates;
    public string RoarState => roarState;
    public float RoarSeconds => roarSeconds;

    public string PickAttackState()
    {
        if (attackStates == null || attackStates.Length == 0) return string.Empty;
        return attackStates[Random.Range(0, attackStates.Length)];
    }

    public PredatorBrain CreatePredatorBrain() => new PredatorBrain(
        detectDistance, loseDistance, loseSeconds, attackRange, attackCooldown, attackDuration, attackHitDelay,
        IdleSeconds.x, IdleSeconds.y, WanderSeconds.x, WanderSeconds.y,
        (min, max) => Random.Range(min, max));
}
