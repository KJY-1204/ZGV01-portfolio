// 허기/갈증의 최대값, 감소 속도, 바닥났을 때의 피해를 데이터로 관리하는 에셋이다.
using UnityEngine;

[CreateAssetMenu(fileName = "SurvivalStatsConfig", menuName = "ZGV01/Survival/Survival Stats Config")]
public class SurvivalStatsConfig : ScriptableObject
{
    [Header("Hunger")]
    [SerializeField] private float hungerMax = 100f;
    // 초당 감소량이다. 0.1이면 가득 찬 상태에서 약 17분 뒤 바닥난다.
    [SerializeField] private float hungerDrainPerSecond = 0.1f;

    [Header("Thirst")]
    [SerializeField] private float thirstMax = 100f;
    // 0.16이면 가득 찬 상태에서 약 10분 뒤 바닥난다.
    [SerializeField] private float thirstDrainPerSecond = 0.16f;

    [Header("Sprint")]
    // 달리는 동안 허기/갈증이 줄어드는 속도에 곱해지는 배율이다.
    [SerializeField, Min(1f)] private float sprintDrainMultiplier = 1.5f;

    [Header("Penalty")]
    // 수치 하나가 바닥난 동안 초당 입는 체력 피해다(둘 다 바닥나면 합산).
    [SerializeField] private float emptyDamagePerSecond = 1.5f;
    // 피해를 이 간격으로 모아서 준다(매 프레임 작은 피해가 쌓이지 않게).
    [SerializeField] private float damageInterval = 1f;

    [Header("Weakened")]
    // 허기나 갈증 중 하나라도 이 비율 이하면 허약 상태가 된다(HUD 경고 색도 같은 기준이다).
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.3f;
    // 허약 상태에서는 점프와 달리기가 막히고, 공격력과 공격 속도에 이 배율이 곱해진다.
    [SerializeField, Range(0.1f, 1f)] private float weakenedDamageMultiplier = 0.7f;
    [SerializeField, Range(0.1f, 1f)] private float weakenedAttackSpeedMultiplier = 0.7f;

    public float HungerMax => hungerMax;
    public float HungerDrainPerSecond => hungerDrainPerSecond;
    public float ThirstMax => thirstMax;
    public float ThirstDrainPerSecond => thirstDrainPerSecond;
    public float SprintDrainMultiplier => sprintDrainMultiplier;
    public float LowThreshold => lowThreshold;
    public float WeakenedDamageMultiplier => weakenedDamageMultiplier;
    public float WeakenedAttackSpeedMultiplier => weakenedAttackSpeedMultiplier;
    public float EmptyDamagePerSecond => emptyDamagePerSecond;
    public float DamageInterval => Mathf.Max(0.1f, damageInterval);
}
